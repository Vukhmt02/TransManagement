# Authentication and authorization

The API uses ASP.NET Core Identity, JWT bearer access tokens, and rotating refresh
tokens. Raw refresh tokens are returned to the client once; only SHA-256 hashes
are stored in PostgreSQL.

## Local JWT signing key

Create a cryptographically random signing key and save it to .NET user secrets:

```powershell
$bytes = New-Object byte[] 64
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$rng.Dispose()
$jwtSecret = [Convert]::ToBase64String($bytes)

dotnet user-secrets set --project TransManagement.API "Jwt:SecretKey" $jwtSecret

Remove-Variable jwtSecret, bytes
```

Do not store the real signing key in `appsettings.json`. Production deployments
should supply `Jwt__SecretKey` through their secret manager.

## Endpoints

| Method | Path | Authentication |
| --- | --- | --- |
| POST | `/api/auth/register` | Anonymous |
| POST | `/api/auth/login` | Anonymous |
| POST | `/api/auth/refresh-token` | Refresh token |
| POST | `/api/auth/logout` | Refresh token |
| POST | `/api/auth/change-password` | Bearer token |
| GET | `/api/auth/me` | Bearer token |
| PUT | `/api/users/{userId}/status` | Admin role |

Public registration always assigns the `Customer` role. Privileged roles must
be assigned by an authorized administrative workflow.

## Token behavior

- Access tokens expire after 15 minutes.
- Refresh tokens expire after 7 days and rotate on every use.
- Reusing a rotated or revoked refresh token returns HTTP 401.
- Changing a password revokes all refresh tokens and invalidates existing access
  tokens through the Identity security stamp.
- Deactivating or locking a user invalidates their access tokens.
- Five failed password attempts lock the account for 15 minutes.
