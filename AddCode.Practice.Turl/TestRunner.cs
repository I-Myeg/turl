using System.Diagnostics;
using System.Text.Json;
using AddCode.Practice.Turl.Models;
using AddCode.Practice.Turl.Utils;

namespace AddCode.Practice.Turl;

public class TestRunner
{
    private readonly HttpClient _client;
    private readonly Dictionary<string, string> _globalHeaders;
    private readonly int? _globalTimeout;
    private const int DefaultTimeout = 10000;

    public TestRunner(HttpClient client, Dictionary<string, string>? globalHeaders, int? globalTimeout)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _globalHeaders = globalHeaders ?? new Dictionary<string, string>();
        _globalTimeout = globalTimeout;
    }

    public async Task<(int TestCount, int SuccessCount)> RunTestsAsync(TestsInfo testsInfo)
    {
        int successCount = 0;
        int failCount = 0;
        var totalStopwatch = Stopwatch.StartNew();

        if (testsInfo.Tests != null)
        {
            for (int i = 0; i < testsInfo.Tests.Count; i++)
            {
                var test = testsInfo.Tests[i];
                var requestStopwatch = Stopwatch.StartNew();

                Console.WriteLine($"{testsInfo.FileName}: Running [{i + 1}/{testsInfo.Tests.Count}]");

                if (test.Request != null)
                {
                    var response = await SendAsync(test.Request, requestStopwatch);

                    {
                        var actualBody = await response?.Content.ReadAsStringAsync()!;
                        var actualStatus = (int)response.StatusCode;

                        var isStatusMatch = test.Response != null && actualStatus == (int)test.Response.Status;
                        var isBodyMatch = true;

                        if (test.Response != null && !string.IsNullOrEmpty(test.Response.Body))
                        {
                            try
                            {
                                var expectedJson = JsonDocument.Parse(test.Response.Body);
                                var actualJson = JsonDocument.Parse(actualBody);

                                isBodyMatch = JsonUtils.IsJsonMatch(expectedJson.RootElement, actualJson.RootElement);
                            }
                            catch (JsonException e)
                            {
                                isBodyMatch = false;
                                Console.WriteLine($"JSON parsing error: {e.Message}");
                            }
                        }

                        if (isStatusMatch && isBodyMatch)
                        {
                            successCount++;
                            Console.WriteLine($"{testsInfo.FileName}: Success [{i + 1} request in {requestStopwatch.ElapsedMilliseconds}ms]");
                        }
                        else
                        {
                            failCount++;
                            Console.WriteLine($"{testsInfo.FileName}: Failed [{i + 1} request in {requestStopwatch.ElapsedMilliseconds}ms]");
                            Console.WriteLine(
                                $"assert failed:\n  {test.Request.Method} {test.Request.Url}\n  actual: status: {actualStatus}\n  expected: status: {test.Response?.Status}");
                            if (!isBodyMatch)
                            {
                                Console.WriteLine($"actual: body: {actualBody}");
                                Console.WriteLine($"expected: body: {test.Response?.Body}");
                            }
                        }
                    }
                }
            }
        }

        totalStopwatch.Stop();
        Console.WriteLine(
            "------------------------------------------------------------------------------------");
        Console.WriteLine($"Execute tests: {testsInfo.Tests.Count}");
        Console.WriteLine($"Success tests: {successCount}");
        Console.WriteLine($"Failed tests: {failCount}");
        Console.WriteLine($"Duration: {totalStopwatch.ElapsedMilliseconds}ms");

        return (testsInfo.Tests.Count, successCount);
    }

    private async Task<HttpResponseMessage?> SendAsync(Request request, Stopwatch stopwatch)
    {
        var url = request.Url;

        if (request.Queryparams != null && request.Queryparams.Any())
        {
            var queryString = string.Join("&", request.Queryparams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            url = $"{url}?{queryString}";
        }

        if (request.Method != null)
        {
            var httpRequest = new HttpRequestMessage(new HttpMethod(request.Method), url);

            foreach (var header in _globalHeaders)
            {
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (request.Headers != null)
            {
                foreach (var header in request.Headers)
                {
                    httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            if (!string.IsNullOrEmpty(request.Body))
            {
                httpRequest.Content = new StringContent(request.Body, System.Text.Encoding.UTF8, "application/json");
            }

            var timeout = request.Timeout ?? _globalTimeout ?? DefaultTimeout;
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeout));

            HttpResponseMessage? response;
            stopwatch.Start();
            try
            {
                response = await _client.SendAsync(httpRequest, cts.Token);
                stopwatch.Stop();

                return response;
            }
            catch (HttpRequestException e)
            {
                stopwatch.Stop();
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
            catch (TaskCanceledException)
            {
                stopwatch.Stop();
                Console.WriteLine("Request timeout.");
                return null;
            }
        }

        return null;
    }
}
