namespace AddCode.Practice.Turl.Models;

public class TestsInfo
{
    public List<Test> Tests { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public int? Timeout { get; set; }
    public string FileName { get; set; }
}