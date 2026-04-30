namespace PowerFitness.App.Services;

public static class ApiEndpointResolver
{
    private const string ApiBaseUrlKey = "api_base_url";
    private const string WindowsBaseAddress = "http://localhost:5004/";
    private const string AndroidEmulatorBaseAddress = "http://10.0.2.2:5004/";
    private const string AndroidLocalNetworkBaseAddress = "http://192.168.1.5:5004/";

    public static string GetBaseAddress()
    {
        var configured = Preferences.Default.Get(ApiBaseUrlKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Normalize(configured);
        }

        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            return DeviceInfo.DeviceType == DeviceType.Virtual
                ? AndroidEmulatorBaseAddress
                : AndroidLocalNetworkBaseAddress;
        }

        return WindowsBaseAddress;
    }

    public static void SetBaseAddress(string value)
    {
        var normalized = Normalize(value);
        Preferences.Default.Set(ApiBaseUrlKey, normalized);
    }

    public static Uri BuildUri(string relativePath)
    {
        return new Uri(new Uri(GetBaseAddress()), relativePath);
    }

    public static string ResolveAssetUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "https://placehold.co/160x160";
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        return BuildUri(value.TrimStart('/')).ToString();
    }

    private static string Normalize(string value)
    {
        value = value.Trim();
        if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            value = $"http://{value}";
        }

        if (!value.EndsWith('/'))
        {
            value += "/";
        }

        return value;
    }
}
