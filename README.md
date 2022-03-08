# CS2TSInterfaces

Library for creates TypeScript data models by ASP .Net Web API declaration

#### Features

* Creates TypeScript interfaces based on C# classes
* Creates TypeScript enums based on C# enums
* Supports C# nullable
* Two processing modes: store generated TS code in single file or separate TS type by files
* Can custom include or exclude C# types in/from processing
* Methods of ASP .Net Web API Controllers must be marked by one of standard C# http attribute:
  * HttpGet
  * HttpPost
  * HttpDelete
  * HttpHead
  * HttpOptions
  * HttpPatch
  * HttpPut
* Can process Api methods signature for extract class types (input parameters and return value)

#### Mapping rules

|C# type|TS type|
|:---|:---|
|bool|boolean|
|char, Guid, string|string|
|short, int, long, float, decimal|number|
|DateTime|any|
|Array|[]|
|IDictionary, IReadOnlyDictionary, IEnumerable|Map|
|enum|enum|
|others|any|

#### Usage

Run from custom console application. See TsGenerator sample project.
```csharp
var tsDefinitionsFullPath = Path.GetFullPath("../../../../models");
GenerateTypeScript.GenerateTypeScriptInterfaces(
    typeof(Startup).Assembly,
    tsDefinitionsFullPath,
    config =>
    {
        config.AddAssembly(typeof(Startup).Assembly)
              .AddIncludeType<RequestDto>();
});
```

#### Configuring options

Run main method:
```csharp
static void GenerateTypeScriptInterfaces(Assembly assembly,
                                         string path,
                                         Action<GenerateTypeScriptConfig> configAction = null);
```

- assembly - main assembly of web site application
- path - path for store generated TS file(s)
- configAction - for add custom settings

GenerateTypeScriptConfig contain several methods and properties:

| Method / Property | Type | Description |
|---|---|
| StoreInSingleFile | bool | If true all TS interfaces located in one single "data.model.ts" file. Overwise for each type creates single file with ".ts" extension. Default true |
| AddAssembly | Assembly | Register assembly contains additional classes for processing to TS |
| AddIncludeType | Type | Register class type for processing to TS |
| AddIncludeType | < T > | Register class type for processing to TS |
| AddIncludeType | string | Register class types for processing to TS by setting reqular expression |
| AddExcludeType | Type | Register class type for exclude in processing to TS |
| AddExcludeType | < T > | Register class type for exclude in processing to TS |
| AddExcludeType | string | Register class types for exclude in processing to TS by setting reqular expression |
| AddExcludeTypes | string[] | Register class types for exclude in processing to TS by setting reqular expression |

#### Note

Class types declared by generics (non standard like IEnumerable) is not supported and must by excluded by calling AddExcludeType.

```csharp
public class ContainerDto<TData>
{
    public TData Data { get; set; }
}

...
config.AddExcludeType("ContainerDto`1");
...
```

#### Platform

.Net Core 2.x, .Net Core 3.x, .Net Framework >= 4.7.

#### License

The software released under the terms of the MIT license.