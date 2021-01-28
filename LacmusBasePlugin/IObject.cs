namespace LacmusBasePlugin
{
    public interface IObject
    {
        public string Label { get; set; }
        public float Score { get; set; }
        public int XMin { get; set; }
        public int XMax { get; set; }
        public int YMin { get; set; }
        public int YMax { get; set; }
        public int Width => XMax - XMin;
        public int Height => YMax - YMin;
    }
}