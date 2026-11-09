namespace ECommerce.Application.Common;

public record PagedResult<T>(int Page, int Size, int TotalElements, IReadOnlyList<T> Content)
{
    public int TotalPages => Size == 0 ? 0 : (int)Math.Ceiling(TotalElements / (double)Size);
}

public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
    Unavailable
}

public sealed record AppError(ErrorKind Kind, string Message);

public sealed record Result<T>(bool Succeeded, T? Value, AppError? Error)
{
    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> NotFound(string message) => new(false, default, new AppError(ErrorKind.NotFound, message));

    public static Result<T> Conflict(string message) => new(false, default, new AppError(ErrorKind.Conflict, message));

    public static Result<T> Invalid(string message) => new(false, default, new AppError(ErrorKind.Validation, message));

    public static implicit operator Result<T>(AppError error) => new(false, default, error);
}

public static class Errors
{
    public static AppError NotFound(string message) => new(ErrorKind.NotFound, message);

    public static AppError Conflict(string message) => new(ErrorKind.Conflict, message);

    public static AppError Invalid(string message) => new(ErrorKind.Validation, message);
}
