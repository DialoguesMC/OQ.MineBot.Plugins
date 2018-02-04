namespace TunnelPlugin.Pattern
{
    public class GapThree : IPattern
    {
        public string GetName() {
            return "3 Block Gap";
        }

        public int lenght { get; set; } = 5;
        public int gap { get; set; } = 4;
    }
}