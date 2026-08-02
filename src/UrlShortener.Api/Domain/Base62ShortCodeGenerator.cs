using System.Security.Cryptography;

namespace UrlShortener.Api.Domain;

public class Base62ShortCodeGenerator : IShortCodeGenerator
{
    private const string Base62Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int CodeLength = 7;
    public string Generate()
    {
        var character = RandomNumberGenerator.GetItems<char>(Base62Alphabet, CodeLength);

        return new string(character);
    }
}
