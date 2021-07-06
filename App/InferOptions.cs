using System;
using System.Runtime.CompilerServices;
using CommandLine;
namespace App
{
    [Verb("infer", HelpText = "Inference plugin")]
    public class InferOptions
    {
        [Option('p', "plugin", Required = true, HelpText = "Plugin name.")]
        public int PluginName { get; set; }
        [Option('i', "input", Required = true, HelpText = "Directory with input images.")]
        public string InputDir { get; set; }
        [Option('o', "output", Required = true, HelpText = "Directory with output images.")]
        public string OutputDir { get; set; }
        [Option('t', "threshold", Required = false, HelpText = "Object detection threshold.", Default = 0.5f)]
        public float Threshold { get; set; }
    }
}