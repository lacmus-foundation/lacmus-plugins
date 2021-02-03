using CommandLine;

namespace App
{
    [Verb("show", HelpText = "Inference plugin")]
    public class ShowOptions
    {
        [Option('a', "all", Required = false, HelpText = "Show all info.")]
        public bool ShowAll { get; set; }
    }
}