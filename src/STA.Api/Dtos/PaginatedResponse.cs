namespace STA.Api.Dtos;

public record PaginatedResponse<T>(
    IEnumerable<T> Items,
    int Total,
    int Page,
    int PageSize)
{
    public int PageCount => (int)Math.Ceiling((double)Total / PageSize);
}
