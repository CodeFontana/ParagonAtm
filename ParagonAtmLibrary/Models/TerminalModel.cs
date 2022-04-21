namespace ParagonAtmLibrary.Models;

public class TerminalModel
{
    public string Host { get; set; }
    public string HwProfile { get; set; }
    public List<string> StartupApps { get; set; }
}
