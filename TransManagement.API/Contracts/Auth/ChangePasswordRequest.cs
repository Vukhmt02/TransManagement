using System.ComponentModel.DataAnnotations;

namespace TransManagement.API.Contracts.Auth;

public sealed record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword);

