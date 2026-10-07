namespace TransManagement.Application.Common.Security;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Dispatcher = "Dispatcher";
    public const string Driver = "Driver";
    public const string Customer = "Customer";
    public const string Accountant = "Accountant";

    public static readonly string[] All =
    [
        Admin,
        Dispatcher,
        Driver,
        Customer,
        Accountant
    ];
}

