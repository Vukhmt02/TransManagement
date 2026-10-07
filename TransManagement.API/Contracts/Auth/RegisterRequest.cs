using System.ComponentModel.DataAnnotations;

namespace TransManagement.API.Contracts.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(200)] string FullName);

