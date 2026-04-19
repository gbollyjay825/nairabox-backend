using Nairabox.Application.Common.Models;

namespace Nairabox.Api.Tests;

public class ApiResponseTests
{
    [Fact]
    public void ApiResponse_Ok_ReturnsSuccessTrue()
    {
        var response = ApiResponse<string>.Ok("test data");

        Assert.True(response.Success);
        Assert.Equal("test data", response.Data);
        Assert.Null(response.Message);
    }

    [Fact]
    public void ApiResponse_Ok_WithMessage_ReturnsSuccessAndMessage()
    {
        var response = ApiResponse<string>.Ok("test data", "Operation succeeded");

        Assert.True(response.Success);
        Assert.Equal("test data", response.Data);
        Assert.Equal("Operation succeeded", response.Message);
    }

    [Fact]
    public void ApiResponse_Fail_ReturnsSuccessFalse()
    {
        var response = ApiResponse<string>.Fail("Something went wrong");

        Assert.False(response.Success);
        Assert.Null(response.Data);
        Assert.Equal("Something went wrong", response.Message);
    }

    [Fact]
    public void ApiResponse_NonGeneric_Ok_ReturnsSuccessTrue()
    {
        var response = ApiResponse.Ok("Done");

        Assert.True(response.Success);
        Assert.Equal("Done", response.Message);
    }

    [Fact]
    public void ApiResponse_NonGeneric_Fail_ReturnsSuccessFalse()
    {
        var response = ApiResponse.Fail("Error occurred");

        Assert.False(response.Success);
        Assert.Equal("Error occurred", response.Message);
    }
}
