using System.Collections.Generic;
using LacmusBasePlugin;
using LacmusBasePlugin.Enums;
using OperatingSystem = LacmusBasePlugin.OperatingSystem;
using Version = LacmusBasePlugin.Version;


namespace FakePlugin
{
    public class FakePlugin : IObjectDetectionPlugin
    {
        public string Name => "Fake Plugin";
        public string Description => "Fake Description";
        public string Author => "Fake Author";
        public string Url => "http://fake-url";
        public Version Version => new Version(api: 2, major: 1, minor: 0);
        public InferenceType InferenceType => InferenceType.Cpu;
        public HashSet<OperatingSystem> OperatingSystems => new HashSet<OperatingSystem>()
            {
                OperatingSystem.LinuxAmd64, 
                OperatingSystem.WindowsAmd64, 
                OperatingSystem.OsxAmd64
            };
        public IObjectDetectionModel LoadModel()
        {
            return new FakeModel();
        }
    }
}