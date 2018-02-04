namespace TunnelPlugin.Pattern
{
    public interface IPattern
    {
        string GetName();

        int lenght { get; set; }
        int gap { get; set; }
    }
}