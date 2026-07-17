namespace STA.Core.Services;

public interface ICurrentUser
{
    int? CnUsuario { get; }
    string? NmUsuario { get; }
    string? Role { get; }
}
