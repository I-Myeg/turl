using System.Net;

namespace AddCode.Practice.Turl.Models;

public class Request
{
    public string? Method { get; set; }
    public string? Url { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public Dictionary<string, string>? Queryparams { get; set; }
    public int? Timeout { get; set; }
    public string? Body { get; set; }
}