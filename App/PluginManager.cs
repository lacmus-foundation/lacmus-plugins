using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LacmusPlugin;

namespace App
{
    public class PluginManager
    {
        private readonly string _baseDirectory;
        
        public PluginManager(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }
        public List<IObjectDetectionPlugin> FindPlugins()
        {
            var pluginPaths = Directory.GetFiles(_baseDirectory, "*.dll", new EnumerationOptions() {RecurseSubdirectories = true});
            return pluginPaths.SelectMany(pluginPath =>
            {
                try
                {
                    Assembly pluginAssembly = LoadPlugin(pluginPath);
                    return CreatePlugins(pluginAssembly);
                }
                catch
                {
                    Console.WriteLine("cannot load assembly from {0}", pluginPath);
                    return new List<IObjectDetectionPlugin>();
                }
            }).ToList();
        }
        public async Task<List<IObjectDetectionPlugin>> FindPluginsAsync()
        {
            return await Task.Run(FindPlugins);
        }
        private static Assembly LoadPlugin(string path)
        {
            PluginLoadContext loadContext = new PluginLoadContext(path);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(path)));
        }
        private static IEnumerable<IObjectDetectionPlugin> CreatePlugins(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IObjectDetectionPlugin).IsAssignableFrom((Type?) type))
                {
                    if (Activator.CreateInstance(type) is IObjectDetectionPlugin result)
                    {
                        yield return result;
                    }
                }
            }
        }
    }
}