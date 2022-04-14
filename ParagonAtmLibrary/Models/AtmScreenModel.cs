namespace ParagonAtmLibrary.Models;

public class AtmScreenModel
{
    public string Name { get; set; }
    public List<string> Text { get; set; }
    public decimal MatchConfidence { get; set; }
}
