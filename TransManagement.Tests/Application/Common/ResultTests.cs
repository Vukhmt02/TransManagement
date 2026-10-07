using TransManagement.Application.Common.Models;

namespace TransManagement.Tests.Application.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        var error = new Error("order.not_found", "The transport order was not found.");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void FailedGenericResult_ShouldThrowWhenValueIsAccessed()
    {
        var result = Result<string>.Failure(new Error("value.missing", "Value is missing."));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
