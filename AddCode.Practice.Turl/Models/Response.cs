using System.Net;

namespace AddCode.Practice.Turl.Models;

public class Response
{
    public HttpStatusCode Status { get; set; }
    public string? Body { get; set; }
}