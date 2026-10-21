using Hakim.Pipelines.Results;

namespace Hakim.Pipelines.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_HasNoErrors()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_WithSingleError_ExposesTheError()
    {
        var error = new Error("Product.NotFound", "The product was not found.");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal([error], result.Errors);
    }

    [Fact]
    public void Failure_WithNoErrors_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Failure(new List<Error>()));
    }

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_ValueAccess_Throws()
    {
        var result = Result.Failure<int>(new Error("Some.Error", "Something went wrong."));

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Error_Equality_IsByValue()
    {
        Assert.Equal(new Error("A", "a"), new Error("A", "a"));
        Assert.NotEqual(new Error("A", "a"), new Error("B", "a"));
    }
}
