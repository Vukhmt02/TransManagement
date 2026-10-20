using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using TransManagement.API.Controllers;
using TransManagement.Application.Common.Security;

namespace TransManagement.Tests.API;

public sealed class ChatAuthorizationTests
{
    [Fact]
    public void ChatController_ShouldAllowOnlyAdminAndCustomerRoles()
    {
        var authorize = typeof(ChatController)
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single();

        Assert.Equal($"{AppRoles.Admin},{AppRoles.Customer}", authorize.Roles);
    }

    [Fact]
    public void CreateConversation_ShouldRequireCustomerRole()
    {
        var authorize = GetAuthorizeAttribute(nameof(ChatController.CreateConversation));

        Assert.Equal(AppRoles.Customer, authorize.Roles);
    }

    [Theory]
    [InlineData(nameof(ChatController.Assign))]
    [InlineData(nameof(ChatController.Close))]
    [InlineData(nameof(ChatController.Reopen))]
    public void ConversationManagement_ShouldRequireAdminRole(string methodName)
    {
        var authorize = GetAuthorizeAttribute(methodName);

        Assert.Equal(AppRoles.Admin, authorize.Roles);
    }

    private static AuthorizeAttribute GetAuthorizeAttribute(string methodName) =>
        typeof(ChatController)
            .GetMethod(methodName)!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single();
}
