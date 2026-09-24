namespace BackendProjectTemplate.WebAPI.Features.Authentication.GoogleLinks;

public sealed record GoogleLinkRequest(string FlowToken, string Password);
