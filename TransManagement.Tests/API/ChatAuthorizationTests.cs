using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using TransManagement.API.Controllers;
using TransManagement.API.Hubs;
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

    [Fact]
    public void ChatHub_ShouldAllowOnlyAdminAndCustomerRoles()
    {
        var authorize = typeof(ChatHub)
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single();

        Assert.Equal($"{AppRoles.Admin},{AppRoles.Customer}", authorize.Roles);
    }

    [Fact]
    public void ChatHub_ShouldCreateStableGroupNames()
    {
        var id = Guid.Parse("4ce2a338-6a93-49fb-9582-a63f63c2b8ca");

        Assert.Equal("chat-conversation-4ce2a3386a9349fb9582a63f63c2b8ca", ChatHub.ConversationGroupName(id));
        Assert.Equal("chat-user-4ce2a3386a9349fb9582a63f63c2b8ca", ChatHub.UserGroupName(id));
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
