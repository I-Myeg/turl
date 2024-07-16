using System.Text.Json;

namespace AddCode.Practice.Turl.Utils;

public class JsonUtils
{
    public static bool IsJsonMatch(JsonElement expected, JsonElement actual)
    {
        if (expected.ValueKind != actual.ValueKind)
            return false;

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                var expectedProperties = expected.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                var actualProperties = actual.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

                foreach (var kvp in expectedProperties)
                {
                    if (!actualProperties.TryGetValue(kvp.Key, out var actualValue) || !IsJsonMatch(kvp.Value, actualValue))
                    {
                        return false;
                    }
                }
                return true;

            case JsonValueKind.Array:
                var expectedArray = expected.EnumerateArray().ToArray();
                var actualArray = actual.EnumerateArray().ToArray();

                if (expectedArray.Length > actualArray.Length)
                    return false;

                for (int i = 0; i < expectedArray.Length; i++)
                {
                    if (!IsJsonMatch(expectedArray[i], actualArray[i]))
                        return false;
                }
                return true;

            default:
                return expected.ToString() == actual.ToString();
        }
    }
}