namespace BackendProjectTemplate.WebAPI;

public static class EndpointUrl
{
    public static class Versions
    {
        public const string V1 = "v1";
        public const string V1Route = "v{version:apiVersion}";
    }

    public static class Registrations
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/registrations";
        public static readonly string V1 = ToV1(Route);
    }

    public static class EmailConfirmations
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/email-confirmations";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Sessions
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/sessions";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Countries
    {
        public const string Route = $"api/{Versions.V1Route}/reference-data/countries";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Stakeholders
    {
        public const string Route = $"api/{Versions.V1Route}/stakeholders";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Providers
    {
        public const string Route = $"api/{Versions.V1Route}/providers";
        public static readonly string V1 = ToV1(Route);
    }

    private static string ToV1(string routeTemplate) =>
        routeTemplate.Replace(Versions.V1Route, Versions.V1, StringComparison.Ordinal);
}
