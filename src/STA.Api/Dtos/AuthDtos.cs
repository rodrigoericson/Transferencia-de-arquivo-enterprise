using System.ComponentModel.DataAnnotations;

namespace STA.Api.Dtos;

public record LoginRequest(
    [Required] string Username,
    [Required] string Password);

public record LoginResponse(
    string Token,
    DateTime Expiration,
    string Username,
    string Role);
