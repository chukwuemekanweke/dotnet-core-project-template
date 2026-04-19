namespace BackendProjectTemplate.Domain.Authentication.Services;

public interface IUserAgentParserService
{
    UserAgentInfo Parse(string userAgent);
}
