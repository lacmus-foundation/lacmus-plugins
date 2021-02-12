using System.Collections.Generic;
using LacmusPlugin;
using LacmusPlugin.Enums;

namespace LacmusRetinanetPlugin
{
    public class Plugin : IObjectDetectionPlugin
    {
        public string Name => "Lacmus Retinanet";
        public string Description => "Resnet50+deepFPN neural network";
        public string Author => "Lacmus Foundation";
        public string Url => "https://github.com/lacmus-foundation/lacmus";
        public Version Version => new Version(api: 2, major: 1, minor: 0);
        public InferenceType InferenceType => InferenceType.Cpu;
        public HashSet<OperatingSystem> OperatingSystems => new HashSet<OperatingSystem>()
        {
            OperatingSystem.LinuxAmd64, 
            OperatingSystem.WindowsAmd64, 
            OperatingSystem.OsxAmd64
        };
        public IObjectDetectionModel LoadModel(float threshold)
        {
            return new Model(threshold);
        }
    }
}