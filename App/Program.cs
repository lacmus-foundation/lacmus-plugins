using System;
using System.IO;
using System.Linq;
using MetadataExtractor;
using Directory = System.IO.Directory;

namespace App
{
    static class Program
    {
        static void Main(string[] args)
        {
            var pluginsDir = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            var pm = new PluginManager(pluginsDir);
            Console.Write("Searching plugins in {0} ...", pluginsDir);
            var plugins = pm.FindPlugins();
            Console.WriteLine("Available plugins:");
            for (var i = 0; i < plugins.Count; i++)
            {
                Console.WriteLine("[{0}]: {1} - {2}", i, plugins[i].Name, plugins[i].InferenceType);
            }
            Console.Write("Chose plugin number: ");
            if (!int.TryParse(Console.ReadLine(), out var number))
                throw new Exception("cannot read the number");
            var plugin = plugins[number];
            Console.WriteLine("Plugin info:");
            Console.WriteLine("\tName: {0}", plugin.Name);
            Console.WriteLine("\tAuthor: {0}", plugin.Author);
            Console.WriteLine("\tDescription: {0}", plugin.Description);
            Console.WriteLine("\tUrl: {0}", plugin.Url);
            Console.WriteLine("\tVersion: {0}", plugin.Version.ToString());
            Console.WriteLine("\tInference Type: {0}", plugin.InferenceType);
            var operatingSystems = "";
            foreach (var os in plugin.OperatingSystems)
            {
                operatingSystems += os + " ";
            }
            Console.WriteLine("\tSupported Platforms: {0}", operatingSystems);
            
            Console.Write("Enter input dir with images: ");
            var inputDir = Console.ReadLine();
            if (!Directory.Exists(inputDir))
                throw new Exception("invalid input directory");
            string[] extensions = { ".jpg", ".jpeg", ".png", ".bmp" };
            using (var model = plugin.LoadModel())
            {
                foreach (var imagePath in Directory.GetFiles(inputDir).Where(f => extensions.Contains(new FileInfo(f).Extension.ToLower())).ToArray())
                {
                    var metadata = ImageMetadataReader.ReadMetadata(imagePath);
                    var height = 0;
                    var width = 0;
                    foreach (var dir in metadata)
                    {
                        foreach (var tag in dir.Tags)
                        {
                            if (tag.Name == "Image Height")
                                if (tag.Description != null)
                                    height = int.Parse(tag.Description.Split(' ').First());
                            if (tag.Name == "Image Width")
                                if (tag.Description != null)
                                    width = int.Parse(tag.Description.Split(' ').First());
                        }
                        if(height > 0 && width > 0)
                            break;
                    }
                    Console.WriteLine("Process image {0} [{1},{2},3]", imagePath, width, height);
                    var startTime = DateTime.Now;
                    var predictions = model.Infer(imagePath, width, height).ToList();
                    Console.WriteLine("Find {0} objects at {1} s", predictions.Count(), DateTime.Now - startTime);
                    foreach (var prediction in predictions)
                    {
                        Console.WriteLine("{0}: [{1}, {2}, {3}, {4}] @ {5}",
                            prediction.Label,
                            prediction.XMin,
                            prediction.YMin,
                            prediction.XMax,
                            prediction.YMax,
                            prediction.Score);
                    }
                }
            }
        }
    }
}