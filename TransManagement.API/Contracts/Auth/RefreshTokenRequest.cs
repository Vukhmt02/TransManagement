using System.ComponentModel.DataAnnotations;

namespace TransManagement.API.Contracts.Auth;

public sealed record RefreshTokenRequest([Required] string RefreshToken);

