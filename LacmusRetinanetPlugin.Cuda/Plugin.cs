using System.Collections.Generic;
using LacmusPlugin;
using LacmusPlugin.Enums;

namespace LacmusRetinanetPlugin.Cuda
{
    public class Plugin : IObjectDetectionPlugin
    {
        public string Tag => "LacmusRetinanetPlugin.Cuda";
        public string Name => "Lacmus Retinanet";
        public string Description => "Resnet50+deepFPN neural network";
        public string Author => "gosha20777";
        public string Company => "Lacmus Foundation";
        public string Url => "https://github.com/lacmus-foundation/lacmus";
        public IEnumerable<string> Dependences => new[] {"CUDA == 10.1", "CuDNN == 7.6"};
        public Version Version => new Version(api: 2, major: 5, minor: 1);
        public InferenceType InferenceType => InferenceType.CudaGpu;
        public HashSet<OperatingSystem> OperatingSystems => new HashSet<OperatingSystem>()
        {
            OperatingSystem.LinuxAmd64, 
            OperatingSystem.WindowsAmd64
        };
        public IObjectDetectionModel LoadModel(float threshold)
        {
            return new Model(threshold);
        }
    }
}