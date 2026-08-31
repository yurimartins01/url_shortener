using System.Security.Cryptography;
using UrlShortener.Api.Domain;

namespace UrlShortener.Api.Tests.Integration
{
    public class FakeShortCodeGenerator : IShortCodeGenerator
    {
        private const string Base62Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const int CodeLength = 7;
        private readonly string _code;
        public FakeShortCodeGenerator()
        {
            var chars = RandomNumberGenerator.GetItems<char>(Base62Alphabet, CodeLength);
            _code = new string(chars);
        }
        public string Generate() => _code;
    }
}
