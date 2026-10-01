using UrlShortener.Application.ShortCodes;

namespace UrlShortener.UnitTests.Fakes;

/// <summary>Returns predefined codes in order, so collisions can be reproduced deterministically.</summary>
internal sealed class SequenceShortCodeGenerator(params string[] codes) : IShortCodeGenerator
{
    private readonly Queue<string> _codes = new(codes);

    public int Calls { get; private set; }

    public string Generate(int length)
    {
        Calls++;
        return _codes.Dequeue();
    }
}
