namespace BackendProjectTemplate.WebAPI.Features.Stakeholders.SecurityActivity;

public sealed record GetSecurityActivityRequest(int Limit = 20, string? Cursor = null);
