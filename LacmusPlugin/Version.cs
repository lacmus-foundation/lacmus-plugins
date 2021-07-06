namespace LacmusPlugin
{
    public readonly struct Version
    {
        public Version(int api, int major, int minor)
        {
            Api = api;
            Major = major;
            Minor = minor;
        }
        public int Api { get; }
        public int Major { get; }
        public int Minor { get; }

        public override string ToString()
        {
            return $"{Api}.{Major}.{Minor}";
        }
    }
}