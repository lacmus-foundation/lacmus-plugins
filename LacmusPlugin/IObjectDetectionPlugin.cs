using System.Collections.Generic;
using LacmusPlugin.Enums;

namespace LacmusPlugin
{
    public interface IObjectDetectionPlugin
    {
        public string Tag { get; }
        public string Name { get; }
        public string Description { get; }
        public string Author { get; }
        public string Company { get; }
        public IEnumerable<string> Dependences { get; }
        public string Url { get; }
        public Version Version { get; }
        public InferenceType InferenceType { get; }
        public HashSet<OperatingSystem> OperatingSystems { get; }
        public IObjectDetectionModel LoadModel(float threshold);
    }
}