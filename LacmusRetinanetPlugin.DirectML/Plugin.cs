using System.Collections.Generic;
using LacmusPlugin;
using LacmusPlugin.Enums;
using LacmusRetinanetPlugin.DirectML;

namespace LacmusRetinanetPlugin.DirectML
{
    public class Plugin : IObjectDetectionPlugin
    {
        public string Tag => "LacmusRetinanetPlugin.DirectML";
        public string Name => "Lacmus Retinanet";
        public string Description => "Resnet50+deepFPN neural network";
        public string Author => "gosha20777";
        public string Company => "Lacmus Foundation";
        public string Url => "https://github.com/lacmus-foundation/lacmus";
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