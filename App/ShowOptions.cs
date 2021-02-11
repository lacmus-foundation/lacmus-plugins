using CommandLine;

namespace App
{
    [Verb("show", HelpText = "Show installed plugins")]
    public class ShowOptions
    {
        [Option('a', "all", Required = false, HelpText = "Show all info.")]
        public bool ShowAll { get; set; }
    }
}