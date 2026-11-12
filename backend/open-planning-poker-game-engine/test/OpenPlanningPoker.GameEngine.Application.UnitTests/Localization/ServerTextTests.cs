using System.Globalization;
using OpenPlanningPoker.GameEngine.Application.Localization;

namespace OpenPlanningPoker.GameEngine.Application.UnitTests.Localization;

/// <summary>OPP-24: error messages and report texts follow the request's UI culture.</summary>
public class ServerTextTests
{
    [Theory]
    [InlineData("en", "Only the game host can perform this action.")]
    [InlineData("sr-Latn", "Ovo može da uradi samo host igre.")]
    [InlineData("sr", "Ovo može da uradi samo host igre.")]
    [InlineData("de", "Nur der Host des Spiels kann das tun.")]
    [InlineData("fr", "Only the game host can perform this action.")]
    public void ErrorMessage_FollowsTheUiCulture(string culture, string expected)
    {
        using var _ = new UiCulture(culture);
        Assert.Equal(expected, ServerText.ErrorMessage("NotHost", "fallback"));
    }

    [Fact]
    public void ErrorMessage_UnknownCode_KeepsItsOwnMessage()
    {
        using var _ = new UiCulture("de");
        Assert.Equal("Something specific", ServerText.ErrorMessage("SomethingNew", "Something specific"));
    }

    [Fact]
    public void Get_FormatsArguments()
    {
        using var _ = new UiCulture("de");
        Assert.Equal("3 von 5", ServerText.Get("report.revealedOf", 3, 5));
    }

    private sealed class UiCulture : IDisposable
    {
        private readonly CultureInfo _previous = CultureInfo.CurrentUICulture;
        public UiCulture(string name) => CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
        public void Dispose() => CultureInfo.CurrentUICulture = _previous;
    }
}
