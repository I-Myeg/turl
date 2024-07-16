using System.Net;
using AddCode.Practice.Turl.Models;
using Tomlyn;
using Tomlyn.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AddCode.Practice.Turl.Utils;

public class FileUtils
{
    public static TestsInfo LoadTestFile(string path)
    {
        var fileExtension = Path.GetExtension(path).ToLower();
        using var reader = new StreamReader(path);
        var content = reader.ReadToEnd();

        return fileExtension switch
        {
            ".yaml" or ".yml" => ReadFromYaml(content),
            ".toml" => ReadFromToml(content),
            _ => throw new NotSupportedException("Unsupported file format.")
        };
    }
    
    private static TestsInfo ReadFromYaml(string yamlContent)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<TestsInfo>(yamlContent);
    }

    private static TestsInfo ReadFromToml(string tomlContent)
    {
        var model = Toml.ToModel(tomlContent);
        var testsInfo = new TestsInfo
        {
            Tests = new List<Test>()
        };

        if (model is { } tomlTable && tomlTable["tests"] is TomlTableArray tests)
        {
            foreach (var test in tests.Cast<TomlTable>())
            {
                var requestFromFile = (TomlTable)test["request"];
                var request = new Request
                {
                    Method = requestFromFile.TryGetValue("method", out var method) ? method.ToString() : null,
                    Url = requestFromFile.TryGetValue("url", out var url) ? url.ToString() : null,
                    Headers = requestFromFile.TryGetValue("headers", out var headers)
                        ? headers.ToTomlDictionary()
                        : new Dictionary<string, string>(),
                    Queryparams = requestFromFile.TryGetValue("queryparams", out var queryparams)
                        ? queryparams.ToTomlDictionary()
                        : new Dictionary<string, string>(),
                    Timeout = requestFromFile.TryGetValue("timeout", out var timeout)
                        ? int.Parse(timeout.ToString()!)
                        : null,
                    Body = requestFromFile.TryGetValue("body", out var requestBody) ? requestBody.ToString() : null
                };

                var responseFromFile = (TomlTable)test["response"];
                var response = new Response
                {
                    Status = (HttpStatusCode)int.Parse(responseFromFile["status"].ToString()!),
                    Body = responseFromFile.TryGetValue("body", out var responseBody) ? responseBody.ToString() : null
                };

                testsInfo.Tests.Add(new Test { Request = request, Response = response });
            }
        }

        return testsInfo;
    }
}

public static class TomlExtensions
{
    public static Dictionary<string, string> ToTomlDictionary(this object tomlObject)
    {
        var dict = new Dictionary<string, string>();
        if (tomlObject is TomlTable table)
        {
            foreach (var key in table.Keys)
            {
                dict[key] = table[key]?.ToString() ?? string.Empty;
            }
        }
        return dict;
    }
}
