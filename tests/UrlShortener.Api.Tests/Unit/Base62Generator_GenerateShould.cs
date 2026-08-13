using System.Text;
using UrlShortener.Api.Domain;

namespace UrlShortener.Api.Tests;
public class Base62Generator_GenerateShould
{
    private readonly Base62ShortCodeGenerator _generator;
    private const string Base62Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public Base62Generator_GenerateShould()
    {
        _generator = new Base62ShortCodeGenerator();
    }

    [Fact]
    public void Generate_Always_ReturnSevenCharacters()
    {
        Assert.Equal(7, _generator.Generate().Length);
    }

    [Fact]
    public void Generate_Always_ReturnsOnlyBase62Characters()
    {
        StringBuilder characters = new();

        for (int i = 0; i < 300; i++)
        {

            characters.Append(_generator.Generate());

        }

        var codes = characters.ToString();

        Assert.All(codes, item => Assert.True(Base62Alphabet.Contains(item), $"{item} should match Base62 alphabet"));
    }

    [Fact]
    public void Generate_Sequentially_ReturnsDifferentCodes()
    {
        var firstCode = _generator.Generate();
        var secondCode = _generator.Generate();

        Assert.NotEqual(firstCode, secondCode);
    }

    [Fact]
    public void Generate_Over300Calls_UsesEveryBase62Character()
    {
        StringBuilder multipleCodes = new();

        for (int i = 0; i < 300; i++)
        {
            multipleCodes.Append(_generator.Generate());
        }

        var allCharacters = multipleCodes.ToString();

        Assert.All(Base62Alphabet, item => Assert.True(allCharacters.Contains(item), $"Generator should use character '{item}'"));
    }
}
