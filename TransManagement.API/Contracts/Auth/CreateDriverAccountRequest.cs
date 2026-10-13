using System.ComponentModel.DataAnnotations;

namespace TransManagement.API.Contracts.Auth;

public sealed record CreateDriverAccountRequest(
    [Required] Guid DriverId,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string TemporaryPassword);
