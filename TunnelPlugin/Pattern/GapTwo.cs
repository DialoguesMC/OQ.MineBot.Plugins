namespace TunnelPlugin.Pattern
{
    public class GapTwo : IPattern
    {
        public string GetName() {
            return "2 Block Gap";
        }

        public int lenght { get; set; } = 5;
        public int gap { get; set; } = 3;
    }
}