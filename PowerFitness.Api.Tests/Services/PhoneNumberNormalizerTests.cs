using PowerFitness.Api.Services;
using Xunit;

namespace PowerFitness.Api.Tests.Services;

public sealed class PhoneNumberNormalizerTests
{
    [Theory]
    [InlineData("+7 (938) 531-78-43", "+79385317843")]
    [InlineData("89385317843", "+79385317843")]
    [InlineData("9385317843", "+79385317843")]
    public void Normalize_ReturnsExpectedPhone(string input, string expected)
    {
        var normalized = PhoneNumberNormalizer.Normalize(input);
        Assert.Equal(expected, normalized);
    }
}
