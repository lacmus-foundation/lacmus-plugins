using System.Collections.Generic;
using LacmusPlugin;
using LacmusPlugin.Enums;

namespace LacmusYolo5Plugin.Cuda
{
    public class Plugin : IObjectDetectionPlugin
    {
        public string Tag => "LacmusYolo5Plugin.Cuda";
        public string Name => "Lacmus YOLO v5";
        public string Description => "YOLO v5 neural network";
        public string Author => "Ivan";
        public string Company => "Lacmus Foundation";
        public string Url => "https://github.com/lacmus-foundation/lacmus-research";
        public IEnumerable<string> Dependences => new[] {"CUDA == 11.6", "(windows) CuDNN == 8.5.0", "(linux) CuDNN == 8.2.4"};
        public Version Version => new Version(api: 2, major: 2, minor: 0);
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