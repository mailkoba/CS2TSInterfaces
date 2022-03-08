using CS2TSInterfaces;
using DemoWebSite;
using DemoWebSite.Dto;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace TsGenerator
{
    class Program
    {
        static void Main()
        {
            try
            {
                var configuration = new ConfigurationBuilder()
                                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                                    .AddJsonFile("appsettings.json")
                                    .Build();

                var tsDefinitionsFullPath = Path.GetFullPath(configuration.GetSection("TsDirectory").Value);

                Console.WriteLine($"Path for TS directory: {tsDefinitionsFullPath}");
                Console.WriteLine("Press any key to run TS Generator...");

                Console.ReadKey();

                GenerateTypeScript.GenerateTypeScriptInterfaces(
                    typeof(Startup).Assembly,
                    tsDefinitionsFullPath,
                    config =>
                    {
                        config.AddAssembly(typeof(Startup).Assembly)
                              .AddIncludeType<RequestDto>()
                              .AddIncludeType("DemoWebSite.Dto.*");
                    });

                Console.WriteLine("TS definitions generated!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
