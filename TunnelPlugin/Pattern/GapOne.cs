namespace TunnelPlugin.Pattern
{
    public class GapOne : IPattern
    {
        public string GetName() {
            return "1 Block Gap";
        }

        public int lenght { get; set; } = 5;
        public int gap { get; set; } = 2;
    }
}