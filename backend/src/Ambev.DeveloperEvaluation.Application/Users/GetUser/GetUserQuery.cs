using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Users.GetUser;

/// <summary>
/// Query for retrieving a user by their ID
/// </summary>
public record GetUserQuery : IRequest<GetUserResult>
{
    /// <summary>
    /// The unique identifier of the user to retrieve
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Initializes a new instance of GetUserQuery
    /// </summary>
    /// <param name="id">The ID of the user to retrieve</param>
    public GetUserQuery(Guid id)
    {
        Id = id;
    }
}
