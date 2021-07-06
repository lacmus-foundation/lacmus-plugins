using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
        public string Url { get; }
        public Version Version { get; }
        public InferenceType InferenceType { get; }
        public HashSet<OperatingSystem> OperatingSystems { get; }
        public IObjectDetectionModel LoadModel(float threshold);
    }
}