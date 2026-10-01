using UrlShortener.Application.ShortCodes;
using UrlShortener.Domain.ShortUrls;

namespace UrlShortener.UnitTests.ShortCodes;

public sealed class RandomShortCodeGeneratorTests
{
    private readonly RandomShortCodeGenerator _generator = new();

    [Theory]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(32)]
    public void Generate_ReturnsCodeOfRequestedLength(int length)
    {
        Assert.Equal(length, _generator.Generate(length).Length);
    }

    [Fact]
    public void Generate_UsesOnlyUrlSafeBase62Characters()
    {
        for (var i = 0; i < 1_000; i++)
        {
            var code = _generator.Generate(10);

            Assert.All(code, c => Assert.Contains(c, ShortUrlRules.ShortCodeAlphabet));
        }
    }

    [Fact]
    public void Generate_ProducesCodesThatPassTheDomainRules()
    {
        Assert.Null(ShortUrlRules.ValidateShortCode(_generator.Generate(7)));
    }

    [Fact]
    public void Generate_IsRandomEnoughNotToRepeatInASmallSample()
    {
        // 10,000 codes out of 62^7 ≈ 3.5e12: the chance of any repetition is ~1.4e-5.
        var codes = Enumerable.Range(0, 10_000).Select(_ => _generator.Generate(7)).ToHashSet();

        Assert.Equal(10_000, codes.Count);
    }

    [Fact]
    public void Generate_UsesTheWholeAlphabet()
    {
        var seen = string.Concat(Enumerable.Range(0, 500).Select(_ => _generator.Generate(10))).ToHashSet();

        Assert.Equal(ShortUrlRules.ShortCodeAlphabet.Length, seen.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Generate_RejectsNonPositiveLength(int length)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _generator.Generate(length));
    }
}
