using System.Collections.Generic;
using LacmusPlugin;
using LacmusPlugin.Enums;

namespace LacmusYolo5Plugin.DirectML
{
    public class Plugin : IObjectDetectionPlugin
    {
        public string Tag => "LacmusYolo5Plugin.DirectML";
        public string Name => "Lacmus YOLO v5";
        public string Description => "YOLO v5 neural network";
        public string Author => "Ivan";
        public string Company => "Lacmus Foundation";
        public string Url => "https://github.com/lacmus-foundation/lacmus-research";
        public IEnumerable<string> Dependences => new[] {"DirectX >= 12.1"};
        public Version Version => new Version(api: 2, major: 1, minor: 0);
        public InferenceType InferenceType => InferenceType.AnyGpu;
        public HashSet<OperatingSystem> OperatingSystems => new HashSet<OperatingSystem>()
        {
            OperatingSystem.WindowsAmd64
        };
        public IObjectDetectionModel LoadModel(float threshold)
        {
            return new Model(threshold);
        }
    }
}