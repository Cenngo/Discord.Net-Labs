namespace Discord.Interactions
{
    public struct CaptureGroupResult
    {
        public string Name { get; }
        public string Value { get; }

        internal CaptureGroupResult(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public static CaptureGroupResult Default => default;
    }
}
