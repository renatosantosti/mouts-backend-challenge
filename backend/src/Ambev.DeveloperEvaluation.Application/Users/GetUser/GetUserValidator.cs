using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Users.GetUser;

/// <summary>
/// Validator for GetUserQuery
/// </summary>
public class GetUserValidator : AbstractValidator<GetUserQuery>
{
    /// <summary>
    /// Initializes validation rules for GetUserQuery
    /// </summary>
    public GetUserValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("User ID is required");
    }
}
