namespace PaperlessScanBridge.Web.Components;

public enum ActionFeedbackKind { None, Progress, Success, Error }

public sealed record ActionFeedback(ActionFeedbackKind Kind, string Message)
{
    public static readonly ActionFeedback None = new(ActionFeedbackKind.None, string.Empty);
    public static ActionFeedback Progress(string message) => new(ActionFeedbackKind.Progress, message);
    public static ActionFeedback Success(string message) => new(ActionFeedbackKind.Success, message);
    public static ActionFeedback Error(string message) => new(ActionFeedbackKind.Error, message);
    public string CssClass => Kind switch { ActionFeedbackKind.Success => "action-status-success", ActionFeedbackKind.Error => "action-status-error", _ => "action-status-progress" };
    public string Symbol => Kind switch { ActionFeedbackKind.Success => "✓", ActionFeedbackKind.Error => "!", _ => "…" };
}
