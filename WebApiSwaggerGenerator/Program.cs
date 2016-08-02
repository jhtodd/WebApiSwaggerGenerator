using System;
using System.IO;
using System.Linq;
using System.Reflection;

using CommandLine;
using CommandLine.Text;

namespace WebApiSwaggerGenerator
{
    public class Options
    {
        [Option('a', "assembly", Required = true, HelpText = "The assembly containing the WebAPI controllers for which to generate a swagger.json file.")]
        public string Assembly { get; set; }

        [Option('o', "output", Required = true, HelpText = "The filename to which to write the swagger.json file.")]
        public string Output { get; set; }

        [Option('t', "title", Required = false, HelpText = "The API title to be reflected in the generated swagger.json file. Default: the name of the specified assembly.")]
        public string Title { get; set; }

        [Option('v', "version", Required = false, HelpText = "The API version to be reflected in the generated swagger.json file. Default: v1.")]
        public string Version { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            var options = new Options();

            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                return;
            }

            var fullAssemblyPath = Path.GetFullPath(options.Assembly);

            // Load the specified assembly and all its references into a separate AppDomain,
            // to avoid any problems if their versions conflict with references loaded into
            // the current AppDomain.
            var appDomain = AppDomain.CreateDomain("TempDomain_" + Guid.NewGuid());

            var loader = new SimpleAssemblyLoader(fullAssemblyPath);
            appDomain.DoCallBack(loader.Load);

            var assembly = loader.Assembly;

            var swaggerJson = new SwaggerJsonGenerator().GenerateSwaggerJson(
                assembly,
                options.Version ?? "v1",
                options.Title);

            File.WriteAllText(options.Output, swaggerJson);

            AppDomain.Unload(appDomain);
        }
    }

    /// <summary>
    /// A simple <see cref="MarshalByRefObject" /> wrapper class to load
    /// an assembly and all its references into another <see cref="AppDomain" />.
    /// </summary>
    internal class SimpleAssemblyLoader : MarshalByRefObject
    {
        private readonly string _path;

        public SimpleAssemblyLoader(string path)
        {
            _path = path;
        }

        public Assembly Assembly { get; private set; }

        public void Load()
        {
            var fullAssemblyPath = Path.GetFullPath(_path);
            var directory = Path.GetDirectoryName(fullAssemblyPath);

            foreach (var assemblyFile in Directory.GetFiles(directory, "*.dll").Where(assemblyFile => assemblyFile != _path))
            {
                try { Assembly.LoadFile(assemblyFile); } catch { }
            }

            Assembly = Assembly.LoadFile(_path);
        }
    }
}