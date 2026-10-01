namespace UrlShortener.Application.ShortCodes;

public interface IShortCodeGenerator
{
    /// <summary>Returns a new random code of the given length. Uniqueness is checked by the caller.</summary>
    string Generate(int length);
}
