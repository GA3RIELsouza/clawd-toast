using ClawdToast.Entities.HookOutput;

namespace ClawdToast;

internal static class Shared
{
    public const string MuteKeyword = "MUTE";

    public const string IgnoreArgument = "IGNORE";
    public const string SubmitArgument = "SUBMIT";

    public const string OtherInputOptionId = "__CLAWD-TOAST-OTHER__";
    public const string OtherInputOptionContent = "Outro";

    public const string MultiSelectId = "__CLAWD-TOAST-MULTISELECT__";

    public static bool ShouldPrintHookOutput { get; set; } = false;
    public static BaseHookOutput? HookOutput { get; set; } = null;
    public static int ReturnCode { get; set; } = 0;

    internal static class ClaudeCodeHooksEvents
    {
        public const string PreToolUse = "PreToolUse";
        public const string PermissionRequest = "PermissionRequest";
    }
}
