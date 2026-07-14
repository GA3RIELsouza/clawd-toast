using ClawdToast.Entities.HookInput;
using ClawdToast.Entities.HookOutput;
using ClawdToast.Services;
using ClawdToast.Visitors.Interfaces;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Windows.Foundation;
using Windows.UI.Notifications;
using WinRT;

namespace ClawdToast.Visitors;

internal readonly record struct ToastCallbacks(
    TypedEventHandler<ToastNotification, object> Activated,
    TypedEventHandler<ToastNotification, ToastDismissedEventArgs> Dismissed)
{
    public void Deconstruct(
        out TypedEventHandler<ToastNotification, object> activated,
        out TypedEventHandler<ToastNotification, ToastDismissedEventArgs> dismissed)
    {
        activated = Activated;
        dismissed = Dismissed;
    }
}

internal sealed class CreateToastCallbacksVisitor(ManualResetEventSlim WaitHandle) : IHookInputVisitor<ToastCallbacks>
{
    public ToastCallbacks Visit(StopHookInput hookInput) => Defaults;
    public ToastCallbacks Visit(StopFailureHookInput hookInput) => Defaults;
    public ToastCallbacks Visit(PermissionRequestHookInput hookInput) => Defaults;
    public ToastCallbacks Visit(PreToolUseHookInput hookInput)
    {
        void Activated(ToastNotification sender, object args)
        {
            try
            {
                if (TryAsToastActivatedEventArgs(args, out var activatedArgs))
                {
                    Trace.WriteLine("Toast activated with ToastActivatedEventArgs.");

                    if (activatedArgs.Arguments.Equals(Shared.IgnoreArgument, StringComparison.OrdinalIgnoreCase))
                    {
                        Trace.WriteLine($"The toast was ignored because of the {Shared.IgnoreArgument} argument.");
                        return;
                    }
                    else if (activatedArgs.Arguments.Equals(Shared.SubmitArgument, StringComparison.OrdinalIgnoreCase))
                    {
                        Trace.WriteLine($"Evaluating the answers, because of the {Shared.SubmitArgument} argument.");

                        var orderedInputs = activatedArgs
                            .UserInput
                            .OrderBy(kvp => kvp.Key.EndsWith(Shared.OtherInputOptionId));

                        var questionsThatSelectedOther = new HashSet<string>();
                        var allQuestionsAnswered = true;
                        var userInputDict = orderedInputs.ToDictionary();

                        foreach (var (key, value) in orderedInputs)
                        {
                            if (key.Contains(Shared.MultiSelectId))
                            {
                                var idxOf = key.IndexOf(Shared.MultiSelectId);

                                var keyWoSufix = key[..idxOf];
                                var valueWoPrefix = key[(idxOf+Shared.MultiSelectId.Length)..];

                                valueWoPrefix = string.IsNullOrWhiteSpace(valueWoPrefix)
                                    ? "false"
                                    : valueWoPrefix;

                                if (value is string str && str == "true")
                                {
                                    if (userInputDict.TryGetValue(keyWoSufix, out var valueObj))
                                    {
                                        if (valueObj is List<string> valueList)
                                        {
                                            valueList.Add(valueWoPrefix);
                                        }
                                        else
                                        {
                                            valueList = [valueWoPrefix];
                                            userInputDict[keyWoSufix] = valueList;
                                        }
                                    }
                                    else
                                    {
                                        userInputDict[keyWoSufix] = new List<string> { valueWoPrefix };
                                    }
                                }

                                userInputDict.Remove(key);
                            }
                            else if (key.EndsWith(Shared.OtherInputOptionId))
                            {
                                var keyWoSufix = key[..key.IndexOf(Shared.OtherInputOptionId)];
                                if (questionsThatSelectedOther.Contains(keyWoSufix))
                                {
                                    if (value is not string str || string.IsNullOrWhiteSpace(str))
                                    {
                                        allQuestionsAnswered = false;
                                        break;
                                    }

                                    userInputDict[keyWoSufix] = str;
                                }

                                userInputDict.Remove(key);
                            }
                            else
                            {
                                if (value is not string str || string.IsNullOrWhiteSpace(str) || str == Shared.OtherInputOptionId)
                                {
                                    questionsThatSelectedOther.Add(key);
                                }
                            }
                        }

                        userInputDict = userInputDict
                            .Select(kvp => kvp.Value is List<string> list ? new(kvp.Key, string.Join(", ", list)) : kvp)
                            .ToDictionary();

                        if (allQuestionsAnswered)
                        {
                            Trace.WriteLine("All questions were answered.");

                            var answersByHeader = userInputDict
                                .ToDictionary(
                                    kvp => kvp.Key,
                                    kvp => kvp.Value.ToString() ?? "");

                            var questions = hookInput.ToolInput.Questions;
                            var answers = questions
                                .Where(q => answersByHeader.ContainsKey(q.Header))
                                .ToDictionary(q => q.Question, q => answersByHeader[q.Header]);

                            Shared.ShouldPrintHookOutput = true;
                            Shared.HookOutput = new BaseHookOutput
                            {
                                HookSpecificOutput = new PreToolUseHookOutput
                                {
                                    PermissionDecision = "allow",
                                    UpdatedInput = new()
                                    {
                                        Questions = questions,
                                        Answers = answers
                                    }
                                }
                            };
                        }
                        else
                        {
                            Trace.WriteLine("One or more questions where not answered.");
                        }
                    }
                }
                else
                {
                    Trace.WriteLine($"Toast activated with unknown {args.GetType()}.");
                }

                if (FocusService.TryFocusTerminalWindow())
                {
                    Trace.WriteLine("Focused exact terminal via parent.");
                }
                else
                {
                    Trace.WriteLine("Could not attach to a parent console with a visible window.");
                }
            }
            finally
            {
                WaitHandle.Set();
            }
        }

        return new(Activated, DefaultDismissed);
    }

    private ToastCallbacks Defaults => new(DefaultActivated, DefaultDismissed);
    private readonly TypedEventHandler<ToastNotification, object> DefaultActivated = (sender, args) =>
    {
        try
        {
            if (TryAsToastActivatedEventArgs(args, out var activatedArgs))
            {
                Trace.WriteLine("Toast activated with ToastActivatedEventArgs.");

                if (activatedArgs.Arguments.Equals(Shared.IgnoreArgument, StringComparison.OrdinalIgnoreCase))
                {
                    Trace.WriteLine($"The toast was ignored because of the {Shared.IgnoreArgument} argument.");
                    return;
                }
                else
                {
                    Trace.WriteLine($"Argument \"{activatedArgs.Arguments}\" ignored.");
                }
            }
            else
            {
                Trace.WriteLine($"Toast activated with unknown {args.GetType()}.");
            }

            if (FocusService.TryFocusTerminalWindow())
            {
                Trace.WriteLine("Focused exact terminal via parent.");
            }
            else
            {
                Trace.WriteLine("Could not attach to a parent console with a visible window.");
            }
        }
        finally
        {
            WaitHandle.Set();
        }
    };
    private readonly TypedEventHandler<ToastNotification, ToastDismissedEventArgs> DefaultDismissed = (sender, args) =>
    {
        try
        {
            switch (args.Reason)
            {
                case ToastDismissalReason.TimedOut:
                    Trace.WriteLine("The toast went away by itself (timed out).");
                    break;

                case ToastDismissalReason.UserCanceled:
                    Trace.WriteLine("The user swiped the toast away or clicked the close button.");
                    break;

                case ToastDismissalReason.ApplicationHidden:
                    Trace.WriteLine("The app explicitly hid the toast, or it was cleared by the system.");
                    break;
            }
        }
        finally
        {
            WaitHandle.Set();
        }
    };

    private static bool TryAsToastActivatedEventArgs(object args, [NotNullWhen(true)] out ToastActivatedEventArgs? typedArgs)
    {
        if (args is ToastActivatedEventArgs typed)
        {
            typedArgs = typed;
            return true;
        }

        try
        {
            typedArgs = args.As<ToastActivatedEventArgs>();
            return true;
        }
        catch
        {
            typedArgs = null;
            return false;
        }
    }
}
