using System.Security.Cryptography;
using UrlShortener.Domain.ShortUrls;

namespace UrlShortener.Application.ShortCodes;

/// <summary>
/// Generates Base62 codes with a cryptographically secure random number generator.
/// </summary>
/// <remarks>
/// Codes are independent of the database id, so they cannot be guessed or enumerated
/// (unlike sequential ids or a Base62 encoding of the id). <see cref="RandomNumberGenerator.GetString"/>
/// picks every character uniformly, without the modulo bias of <c>random.Next() % 62</c>.
/// </remarks>
public sealed class RandomShortCodeGenerator : IShortCodeGenerator
{
    public string Generate(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);

        return RandomNumberGenerator.GetString(ShortUrlRules.ShortCodeAlphabet, length);
    }
}
