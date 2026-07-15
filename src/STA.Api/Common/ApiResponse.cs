namespace STA.Api.Common;

public record ApiResponse<T>(bool Success, T? Data, string? Message = null);

public record ApiErrorResponse(bool Success, IEnumerable<ApiError> Errors)
{
    public ApiErrorResponse(params ApiError[] errors) : this(false, errors) { }
}

public record ApiError(string Field, string Message);
