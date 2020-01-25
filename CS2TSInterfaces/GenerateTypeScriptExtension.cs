using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace CS2TSInterfaces
{
    public static class GenerateTypeScriptExtension
    {
        public static void GenerateTypeScriptInterfaces(this IApplicationBuilder app,
                                                        Assembly assembly,
                                                        string path)
        {
            app.GenerateTypeScriptInterfaces(assembly, path, new GenerateTypeScriptConfig());
        }

        public static void GenerateTypeScriptInterfaces(this IApplicationBuilder app,
                                                        Assembly assembly,
                                                        string path,
                                                        GenerateTypeScriptConfig config)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var directory = Path.GetDirectoryName(path);

            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _config = config;

            FindAndProcess(assembly, path);
        }

        #region private

        private static void FindAndProcess(Assembly assembly, string path)
        {
            var controllerBaseType = typeof(ControllerBase);

            var controllers = assembly.GetTypes()
                                      .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(controllerBaseType));

            var actions = controllers.SelectMany(
                x => x.GetMethods()
                      .Where(m => m.IsPublic &&
                                  m.GetCustomAttributes()
                                   .Any(a => ActionAttributeTypes.Contains(a.GetType())))
            );

            var types = actions.SelectMany(
                                   x => new[] { x.ReturnType }.Concat(x.GetParameters()
                                                                     .Select(p => p.ParameterType)));

            if (_config.IncludeTypes.Any())
            {
                types = types.Concat(_config.IncludeTypes);
            }

            if (_config.IncludeTypeNames.Any())
            {
                var regexList = _config.IncludeTypeNames
                                       .Select(x => new Regex(x))
                                       .ToArray();

                var asmTypes = _config.Assemblies
                                      .SelectMany(x => x.GetTypes())
                                      .Concat(assembly.GetTypes())
                                      .Where(x => x.FullName != null)
                                      .Where(x => regexList.Any(r => r.IsMatch(x.FullName)))
                                      .ToArray();

                if (asmTypes.Any())
                {
                    types = types.Concat(asmTypes);
                }
            }

            if (_config.ExcludeTypeNames.Any())
            {
                var regexList = _config.ExcludeTypeNames
                                       .Select(x => new Regex(x))
                                       .ToArray();

                types = types.Where(x => x.FullName != null)
                             .Where(x => !regexList.Any(r => r.IsMatch(x.FullName)));
            }

            types = types.Select(FilterGenericResultType)
                         .Where(x => !IsExclusionType(x))
                         .Where(x => !KnownTypes.Contains(x));

            StreamWriter sw = null;

            if (_config.StoreInSingleFile)
            {
                sw = File.CreateText(Path.GetFullPath(Path.Combine(path, SingleFileName)));
            }

            foreach (var type in types)
            {
                ProcessTypes(type, processedInfo =>
                {
                    if (!_config.StoreInSingleFile)
                    {
                        sw = File.CreateText(Path.GetFullPath(Path.Combine(path,
                                                                           processedInfo.TypeName,
                                                                           DeclarationFileExtension)));
                    }

                    if (sw == null)
                    {
                        throw new NullReferenceException(nameof(sw));
                    }

                    // ReSharper disable PossibleNullReferenceException
                    if (!_config.StoreInSingleFile && processedInfo.Dependencies.Any())
                    {
                        foreach (var dependency in processedInfo.Dependencies)
                        {
                            sw.WriteLine($"import {{ {dependency} }} from \"./{dependency}{DeclarationFileExtension}\";");
                        }
                        sw.WriteLine();
                    }

                    sw.Write(processedInfo.Value);
                    if (!_config.StoreInSingleFile)
                    {
                        sw.Dispose();
                    }
                    // ReSharper restore PossibleNullReferenceException
                });
            }

            if (_config.StoreInSingleFile)
            {
                // ReSharper disable once PossibleNullReferenceException
                sw.Dispose();
            }
        }

        private static void FillProcessingTypes(Type initType, List<Info> infos)
        {
            if (IsExclusionType(initType) ||
                KnownTypes.Contains(initType) ||
                infos.Any(i => i.Type == initType || FilterType(i.Type) == FilterType(initType)))
            {
                return;
            }

            initType = FilterType(initType);

            if (IsExclusionType(initType) ||
                KnownTypes.Contains(initType) ||
                infos.Any(i => i.Type == initType || FilterType(i.Type) == FilterType(initType)))
            {
                return;
            }

            if (IsDictionary(initType))
            {
                return;
            }

            infos.Add(new Info
            {
                Type = initType,
                IsEnum = false
            });

            var fields = initType
                         .GetProperties()
                         .Select(x => x.PropertyType)
                         .Select(x => Nullable.GetUnderlyingType(x) ?? x)
                         .GroupBy(FilterType)
                         .Select(x => x.First())
                         .Where(x => !IsExclusionType(x))
                         .Where(x =>
                         {
                             var typeInfo = x.GetTypeInfo();
                             return typeInfo.IsEnum || typeInfo.IsClass || typeInfo.IsGenericType ||
                                    typeInfo.IsInterface;
                         })
                         .Where(x => !infos.Any(i => i.Type == x || FilterType(i.Type) == FilterType(x)))
                         .Where(x => !KnownTypes.Contains(FilterType(x)))
                         .Select(x => new Info
                         {
                             Type = x,
                             IsEnum = x.GetTypeInfo().IsEnum
                         })
                         .ToArray();

            infos.AddRange(fields.Where(x => x.IsEnum));

            foreach (var info in fields.Where(x => !x.IsEnum))
            {
                FillProcessingTypes(FilterType(info.Type), infos);
            }
        }

        private static void ProcessTypes(Type initType, Action<ProcessedInfo> writeAction)
        {
            var allTypes = new List<Info>();
            FillProcessingTypes(initType, allTypes);

            KnownTypes.Add(FilterType(initType));

            foreach (var type in allTypes.Where(x => !IsDictionary(x.Type))
                                         .Select(x => x.Type)
                                         .Where(x => !IsExclusionType(FilterType(x))))
            {
                KnownTypes.Add(type);
                KnownTypes.Add(FilterType(type));
            }

            foreach (var info in allTypes)
            {
                var data = info.IsEnum ? ProcessEnum(info.Type) : ProcessType(info.Type);
                if (data == null) continue;

                writeAction(data);
            }
        }

        private static Type FilterGenericResultType(Type t)
        {
            var type = t;

            while (type.IsConstructedGenericType)
            {
                var genericDefinition = type.GetGenericTypeDefinition();

                if (genericDefinition == typeof(Task<>) ||
                    genericDefinition == typeof(ActionResult<>))
                {
                    type = GetGenericArguments(type)[0];
                }
                else
                {
                    break;
                }
            }

            return type;
        }

        /// <summary>
        /// Filter type for Enumerable, Array and Task
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static Type FilterType(Type t)
        {
            if (t.GetTypeInfo().IsArray)
            {
                return t.GetElementType();
            }

            if ((typeof(IEnumerable).IsAssignableFrom(t) || typeof(Task<>).IsAssignableFrom(t)) &&
                !IsExclusionType(t))
            {
                return GetGenericArguments(t)[0];
            }

            return t;
        }

        /// <summary>
        /// Check type for forbidden types
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static bool IsExclusionType(Type t)
        {
            return t == typeof(string) ||
                   t == typeof(object) ||
                   t == typeof(DateTime) ||
                   t == typeof(ActionResult) ||
                   t == typeof(IActionResult) ||
                   t == typeof(Task) ||
                   t == typeof(void) ||
                   t == typeof(Guid) ||
                   Nullable.GetUnderlyingType(t) != null ||
                   IsCompilerGenerated(t) ||
                   _config.ExcludeTypes.Contains(t);
        }

        /// <summary>
        /// Check type for compiler generated type
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static bool IsCompilerGenerated(MemberInfo t)
        {
            try
            {
                return Attribute.GetCustomAttribute(t, typeof(CompilerGeneratedAttribute)) != null;
            }
            catch
            {
                // empty
            }

            return false;
        }

        private static bool IsDictionary(Type t)
        {
            var typeInfo = t.GetTypeInfo();

            var interfaces = typeInfo.GetInterfaces()
                                     .ToList();
            if (typeInfo.IsInterface)
            {
                interfaces.Add(typeInfo);
            }

            return interfaces.Any(
                x => DictionaryInterfaces.Any(i => i == x ||
                                                   x.GetTypeInfo().IsGenericType && i == x.GetGenericTypeDefinition()));
        }

        private static ProcessedInfo ProcessType(Type t)
        {
            var filteredType = FilterType(t);
            if (IsExclusionType(filteredType))
            {
                return null;
            }

            var processedInfo = new ProcessedInfo
            {
                TypeName = filteredType.Name
            };

            var sb = new StringBuilder();
            sb.AppendFormat("export interface {0} {{", filteredType.Name);
            sb.AppendLine();

            foreach (var mi in GetClassMembers(filteredType))
            {
                var innerType = Nullable.GetUnderlyingType(mi.PropertyType);
                var typeName = GetTypeName(innerType ?? mi.PropertyType);

                sb.AppendFormat("    {0}{1}: {2};",
                                ToCamelCase(mi.Name),
                                innerType != null ? "?" : string.Empty,
                                typeName);
                sb.AppendLine();

                if (KnownTypes.Contains(innerType ?? mi.PropertyType))
                {
                    processedInfo.Dependencies.Add(typeName);
                }
            }

            sb.AppendLine("}");

            processedInfo.Value = sb.ToString();

            return processedInfo;
        }

        private static ProcessedInfo ProcessEnum(Type t)
        {
            var sb = new StringBuilder();
            var values = (int[])Enum.GetValues(t);

            sb.AppendLine("export const enum " + t.Name + " {");

            for (var i = 0; i < values.Length; i++)
            {
                var name = Enum.GetName(t, values[i]);
                sb.AppendFormat("    {0} = {1}{2}", name, values[i], i < values.Length - 1 ? "," : string.Empty);
                sb.AppendLine();
            }

            sb.AppendLine("}");

            return new ProcessedInfo
            {
                TypeName = t.Name,
                Value = sb.ToString()
            };
        }

        private static string ToCamelCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length < 2) return s.ToLowerInvariant();
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        private static string GetTypeName(Type t)
        {
            while (true)
            {
                if (t == null)
                {
                    throw new ArgumentNullException();
                }

                if (t.GetTypeInfo().IsPrimitive)
                {
                    if (t == typeof(bool)) return "boolean";
                    if (t == typeof(char)) return "string";
                    return "number";
                }

                if (t == typeof(DateTime)) return "any";
                if (t == typeof(Guid)) return "string";
                if (t == typeof(decimal)) return "number";
                if (t == typeof(string)) return "string";
                if (t.GetTypeInfo().IsArray)
                {
                    var elemType = t.GetElementType();
                    return GetTypeName(elemType) + "[]";
                }

                if (IsDictionary(t))
                {
                    var arguments = GetGenericArguments(t);
                    return "Map<" + GetTypeName(arguments[0]) + ", " + GetTypeName(arguments[1]) + ">";
                }

                if (typeof(IEnumerable).IsAssignableFrom(t))
                {
                    var collectionType = GetGenericArguments(t)[0];
                    return GetTypeName(collectionType) + "[]";
                }

                if (Nullable.GetUnderlyingType(t) != null)
                {
                    t = Nullable.GetUnderlyingType(t);
                    continue;
                }

                if (KnownTypes.Contains(t)) return t.Name;
                return "any";
            }
        }

        private static Type[] GetGenericArguments(Type t)
        {
            var type = t;
            var args = new Type[] { };

            while (args.Length == 0 && type != null && type != typeof(object))
            {
                args = type.GetGenericArguments();
                type = type.GetTypeInfo().BaseType;
            }

            return args;
        }

        private static PropertyInfo[] GetClassMembers(Type type)
        {
            return type.GetProperties();
        }

        private static readonly HashSet<Type> ActionAttributeTypes = new HashSet<Type>
        {
            typeof (HttpGetAttribute),
            typeof (HttpPostAttribute),
            typeof (HttpDeleteAttribute),
            typeof (HttpHeadAttribute),
            typeof (HttpOptionsAttribute),
            typeof (HttpPatchAttribute),
            typeof (HttpPutAttribute),
        };

        private static readonly HashSet<Type> DictionaryInterfaces = new HashSet<Type>
        {
            typeof (IDictionary<,>),
            typeof (IReadOnlyDictionary<,>),
            typeof (IReadOnlyCollection<KeyValuePair<string, string>>),
            typeof (IEnumerable<KeyValuePair<string, string>>)
        };

        private static readonly HashSet<Type> KnownTypes = new HashSet<Type>();
        private const string SingleFileName = "models.d.ts";
        private const string DeclarationFileExtension = ".d.ts";

        private static GenerateTypeScriptConfig _config;

        private class Info
        {
            public Type Type { get; set; }

            public bool IsEnum { get; set; }
        }

        private class ProcessedInfo
        {
            public ProcessedInfo()
            {
                Dependencies = new HashSet<string>();
            }

            public string TypeName { get; set; }

            public string Value { get; set; }

            public ISet<string> Dependencies { get; }
        }

        #endregion private
    }
}
