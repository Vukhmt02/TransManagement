# PostgreSQL database setup

The application uses PostgreSQL through the Npgsql Entity Framework Core provider.

## Local development

Store the real connection string in .NET user secrets instead of committing the
database password:

```powershell
dotnet user-secrets set --project TransManagement.API `
  "ConnectionStrings:DefaultConnection" `
  "Host=localhost;Port=5432;Database=trans_management;Username=postgres;Password=YOUR_PASSWORD;Include Error Detail=true"
```

Restore the local EF tool and NuGet dependencies:

```powershell
dotnet tool restore
dotnet restore TransManagement.sln
```

Create a migration after changing the entity model:

```powershell
dotnet ef migrations add MigrationName `
  --project TransManagement.Infrastructure `
  --startup-project TransManagement.API `
  --output-dir Persistence/Migrations
```

Apply pending migrations:

```powershell
dotnet ef database update `
  --project TransManagement.Infrastructure `
  --startup-project TransManagement.API
```

Never commit a real PostgreSQL password to an appsettings file.
