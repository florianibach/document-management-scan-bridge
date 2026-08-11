using Bunit;
using PaperlessScanBridge.Web.Components;

namespace PaperlessScanBridge.ComponentTests;

public sealed class AppErrorSummaryTests : BunitContext
{
    [Fact]
    public void SingleErrorUsesReadableParagraphAndRecoveryHint()
    {
        var component = Render<AppErrorSummary>(parameters => parameters
            .Add(value => value.Messages, ["Paperless cannot be reached."])
            .Add(value => value.Title, "We couldn't complete that action")
            .Add(value => value.Hint, "Check the details below, then try again."));

        var alert = component.Find(".app-error-summary");
        Assert.Equal("alert", alert.GetAttribute("role"));
        Assert.Equal("assertive", alert.GetAttribute("aria-live"));
        Assert.Equal("Paperless cannot be reached.", component.Find(".app-error-summary__message").TextContent);
        Assert.Contains("try again", component.Find(".app-error-summary__hint").TextContent);
        Assert.Empty(component.FindAll("ul"));
    }

    [Fact]
    public void MultipleErrorsRemainAnAccessibleList()
    {
        var component = Render<AppErrorSummary>(parameters => parameters
            .Add(value => value.Messages, ["Check the URL.", "Check the token."]));

        Assert.Equal(2, component.FindAll(".app-error-summary__messages li").Count);
    }

    [Fact]
    public void EmptyErrorsRenderNoAlert()
    {
        var component = Render<AppErrorSummary>(parameters => parameters
            .Add(value => value.Messages, []));

        Assert.Empty(component.FindAll("[role=alert]"));
    }
}
