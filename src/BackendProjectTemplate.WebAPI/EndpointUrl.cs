namespace BackendProjectTemplate.WebAPI;

public static class EndpointUrl
{
    public static class Versions
    {
        public const string V1 = "v1";
        public const string V1Route = "v{version:apiVersion}";
    }

    public static class SignUp
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/sign-up";
        public static readonly string V1 = ToV1(Route);
    }

    public static class SignUpOtp
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/sign-up/otp";
        public static readonly string V1 = ToV1(Route);
    }

    public static class SignIn
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/sign-in";
        public static readonly string V1 = ToV1(Route);
    }

    public static class GetCountries
    {
        public const string Route = $"api/{Versions.V1Route}/reference-data/countries";
        public static readonly string V1 = ToV1(Route);
    }


    private static string ToV1(string routeTemplate) =>
        routeTemplate.Replace(Versions.V1Route, Versions.V1, StringComparison.Ordinal);
}
