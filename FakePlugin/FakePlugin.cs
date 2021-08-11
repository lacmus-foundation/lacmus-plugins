using System.Collections.Generic;
using LacmusPlugin;
using LacmusPlugin.Enums;
using OperatingSystem = LacmusPlugin.OperatingSystem;
using Version = LacmusPlugin.Version;


namespace FakePlugin
{
    public class FakePlugin : IObjectDetectionPlugin
    {
        public string Tag => "FakePlugin.Cpu";
        public string Name => "Fake Plugin";
        public string Description => "Fake Description";
        public string Author => "Fake Author";
        public string Company => "Fake Company";
        public IEnumerable<string> Dependences => new[] {"Fake Dependency >= 1.0.0"};
        public string Url => "http://fake-url";
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
            return new FakeModel();
        }
    }
}