using System.ComponentModel.DataAnnotations;

namespace TransManagement.API.Contracts.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

