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

1. Run from custom console application
```csharp
var tsDefinitionsFullPath = Path.Combine(env.ContentRootPath, "./src/app/models/");
app.GenerateTypeScriptInterfaces(Assembly.GetExecutingAssembly(), tsDefinitionsFullPath);
```

2. Run from source ASP .Net Core Web application

2.1. Create custom launch settings profile in Visual Studio

|Parameter|Value|
|:---|:---|
|Profile|Generate TS interfaces|
|Launch|Project|
|Environment variables| Name: ASPNETCORE_ENVIRONMENT, Value: GENERATE_TS|

2.2 Modify Startup

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider, ILogger logger)
{
    ...
    if (env.EnvironmentName == "GENERATE_TS")
    {
        var tsDefinitionsFullPath = Path.Combine(env.ContentRootPath, "./ClientApp/src/app/models/");
        app.GenerateTypeScriptInterfaces(Assembly.GetExecutingAssembly(), tsDefinitionsFullPath);

        Environment.Exit(0);

        return;
    }
    ...
}
```

2.3. Run Web site with profile "Generate TS interfaces" from Visual Studio.

#### Configuring options

...

#### Platform

.Net Core 2.x, .Net Core 3.x, .Net Framework >= 4.7.

#### License

The software released under the terms of the MIT license.