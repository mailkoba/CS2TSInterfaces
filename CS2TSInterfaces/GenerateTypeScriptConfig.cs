using System;
using System.Collections.Generic;
using System.Reflection;

namespace CS2TSInterfaces
{
    public sealed class GenerateTypeScriptConfig
    {
        public bool StoreInSingleFile { get; set; } = true;

        public IReadOnlyCollection<Type> IncludeTypes => _includeTypes;

        public IReadOnlyCollection<string> IncludeTypeNames => _includeTypeNames;

        public IReadOnlyCollection<Type> ExcludeTypes => _excludeTypes;

        public IReadOnlyCollection<string> ExcludeTypeNames => _excludeTypeNames;

        public IReadOnlyCollection<Assembly> Assemblies => _assemblies;

        public GenerateTypeScriptConfig AddAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            _assemblies.Add(assembly);

            return this;
        }

        public GenerateTypeScriptConfig AddExcludeType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (_includeTypes.Contains(type))
            {
                throw new Exception($"Type {type.FullName} already added in IncludeTypes!");
            }

            _excludeTypes.Add(type);

            return this;
        }

        public GenerateTypeScriptConfig AddExcludeType<T>()
        {
            return AddExcludeType(typeof(T));
        }

        public GenerateTypeScriptConfig AddExcludeType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (_includeTypeNames.Contains(typeName))
            {
                throw new Exception($"Type {typeName} already added in IncludeTypeNames!");
            }

            _excludeTypeNames.Add(typeName);

            return this;
        }

        public GenerateTypeScriptConfig AddIncludeType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (_excludeTypes.Contains(type))
            {
                throw new Exception($"Type {type.FullName} already added in ExcludeTypes!");
            }

            _includeTypes.Add(type);

            return this;
        }

        public GenerateTypeScriptConfig AddIncludeType<T>()
        {
            return AddIncludeType(typeof(T));
        }

        public GenerateTypeScriptConfig AddIncludeType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (_excludeTypeNames.Contains(typeName))
            {
                throw new Exception($"Type {typeName} already added in ExcludeTypeNames!");
            }

            _includeTypeNames.Add(typeName);

            return this;
        }

        private readonly HashSet<Type> _includeTypes = new HashSet<Type>();
        private readonly HashSet<string> _includeTypeNames = new HashSet<string>();
        private readonly HashSet<Type> _excludeTypes = new HashSet<Type>();
        private readonly HashSet<string> _excludeTypeNames = new HashSet<string>();
        private readonly HashSet<Assembly> _assemblies = new HashSet<Assembly>();
    }
}
