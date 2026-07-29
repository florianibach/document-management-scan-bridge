using Bunit;
using PaperlessScanBridge.Web.Components.Pages;

namespace PaperlessScanBridge.ComponentTests;

public sealed class HomePageTests : BunitContext
{
    [Fact]
    public void ShowsHonestPlanningState()
    {
        var page = Render<Home>();

        Assert.Contains("Projektgrundlage bereit", page.Markup);
        Assert.Contains("nächsten Stories", page.Markup);
        Assert.NotNull(page.Find("[role=status]"));
    }
}
