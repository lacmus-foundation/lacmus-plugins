using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using LacmusPlugin;
using MetadataExtractor;
using Directory = System.IO.Directory;

namespace App
{
    static class Program
    {
        static void Infer(InferOptions options)
        {
            if (!Directory.Exists(options.InputDir))
            {
                Console.WriteLine("Invalid input directory {0}", options.InputDir);
                return;
            }

            if (!Directory.Exists(options.OutputDir))
            {
                Console.WriteLine("Invalid output directory {0}", options.OutputDir);
                return;
            }
            var pluginsDir = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            var pm = new PluginManager(pluginsDir);
            IObjectDetectionPlugin plugin;
            try
            {
                plugin = pm.FindPlugins()[options.PluginName];
            }
            catch
            {
                Console.WriteLine("Unable to load plugin with index {0}. Use `show` to show available plugins.", options.PluginName);
                return;
            }
            
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
            var inputDir = options.InputDir;
            var outputDir = options.OutputDir;
            string[] extensions = { ".jpg", ".jpeg", ".png", ".bmp" };
            using (var model = plugin.LoadModel(options.Threshold))
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
                    var imageBaseName = Path.GetFileName(imagePath);
                    var outImagePath = Path.Join(outputDir, imageBaseName);
                    var outXmlPath = outImagePath + ".xml";
                    var annotation = DetectionsToAnnotation(predictions, width, height, outImagePath);
                    File.Copy(imagePath, outImagePath, true);
                    annotation.SaveToXml(outXmlPath);
                }
            }
        }
        static void ShowPlugins(ShowOptions options)
        {
            var pluginsDir = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            var pm = new PluginManager(pluginsDir);
            Console.Write("Searching plugins in {0} ...", pluginsDir);
            var plugins = pm.FindPlugins();
            Console.WriteLine("Available plugins:");
            if (options.ShowAll == false)
                for (var i = 0; i < plugins.Count; i++)
                {
                    Console.WriteLine("[{0}]: {1} - {2}", i, plugins[i].Name, plugins[i].InferenceType);
                }
            else
            {
                for (var i = 0; i < plugins.Count; i++)
                {
                    var plugin = plugins[i];
                    Console.WriteLine("Plugin {0} info:", i);
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
                }
            }
        }

        static Annotation DetectionsToAnnotation(IEnumerable<IObject> detections, int width, int height, string outName)
        {
            var annotation = new Annotation
            {
                Filename = Path.GetFileName(outName),
                Folder = Path.GetDirectoryName(outName),
                Objects = new List<Object>(),
                Size = new Size
                {
                    Height = height,
                    Width = width
                }
            };
            foreach (var detection in detections)
            {
                var o = new Object
                {
                    Name = detection.Label,
                    Box = new Box
                    {
                        Xmax = detection.XMax,
                        Xmin = detection.XMin,
                        Ymax = detection.YMax,
                        Ymin = detection.YMin
                    }
                };
                annotation.Objects.Add(o);
            }
            return annotation;
        }
        static void Main(string[] args)
        {
            var options = new InferOptions();
            var showOptions = new ShowOptions();
            Parser.Default.ParseArguments<InferOptions, ShowOptions>(args)
                .WithParsed<InferOptions>(Infer)
                .WithParsed<ShowOptions>(ShowPlugins);
        }
    }
}