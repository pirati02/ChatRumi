namespace ChatRumi.Kernel.Results;

public sealed record ResponseError(string Code, string Description, ErrorType Type)
{
    public static ResponseError Failure(string code, string? description = null) =>
        new(code, description ?? code, ErrorType.Failure);

    public static ResponseError Unexpected(string code, string? description = null) =>
        new(code, description ?? code, ErrorType.Unexpected);

    public static ResponseError Validation(string code, string? description = null) =>
        new(code, description ?? code, ErrorType.Validation);

    public static ResponseError Conflict(string code, string? description = null) =>
        new(code, description ?? code, ErrorType.Conflict);

    public static ResponseError NotFound(string code, string? description = null) =>
        new(code, description ?? code, ErrorType.NotFound);

    public static ResponseError Unauthorized(string code, string? description = null) =>
        new(code, description ?? code, ErrorType.Unauthorized);

    public static ResponseError Forbidden(string code, string? description = null) =>
        new(code, description ?? code, ErrorType.Forbidden);
}
