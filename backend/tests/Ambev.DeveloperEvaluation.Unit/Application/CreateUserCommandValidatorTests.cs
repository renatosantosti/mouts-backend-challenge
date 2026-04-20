using Ambev.DeveloperEvaluation.Application.Users.CreateUser;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class CreateUserCommandValidatorTests
{
    [Fact]
    public async Task Validate_EmptyCommand_HasErrors()
    {
        var validator = new CreateUserCommandValidator();
        var command = new CreateUserCommand();

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}
