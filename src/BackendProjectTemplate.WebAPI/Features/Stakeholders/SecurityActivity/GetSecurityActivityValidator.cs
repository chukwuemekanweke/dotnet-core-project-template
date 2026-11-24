using BackendProjectTemplate.Application.Common.Pagination;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Stakeholders.SecurityActivity;

public sealed class GetSecurityActivityValidator : AbstractValidator<GetSecurityActivityRequest>
{
    public GetSecurityActivityValidator()
    {
        RuleFor(request => request.Limit)
            .InclusiveBetween(1, 100);
        RuleFor(request => request.Cursor)
            .Must(BeAValidCursor)
            .WithMessage("Cursor is invalid.");
    }

    private static bool BeAValidCursor(string? cursor)
    {
        try
        {
            CursorPagination.Decode(cursor);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
