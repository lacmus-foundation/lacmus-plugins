using LacmusBasePlugin;

namespace LacmusRetinanetPlugin.DirectML
{
    public class DetectedObject : IObject
    {
        public string Label { get; set; }
        public float Score { get; set; }
        public int XMin { get; set; }
        public int XMax { get; set; }
        public int YMin { get; set; }
        public int YMax { get; set; }
    }
}