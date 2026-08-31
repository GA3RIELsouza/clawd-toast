using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ClawdToast.Infrastructure.Interop;

/// <summary>
/// Minimal, AOT friendly bindings for the UI Automation client API.
/// <para>
/// Only the vtable slots actually used are declared with real signatures. Every preceding slot
/// must still be declared, in order, so that the used ones land on the correct vtable index.
/// The placeholders are never called.
/// </para>
/// </summary>
internal static partial class UIAutomationInterop
{
    public const int UIA_TabItemControlTypeId = 50019;
    public const int UIA_SelectionItemPatternId = 10010;
    public const int TreeScope_Descendants = 4;

    private const uint CLSCTX_INPROC_SERVER = 1;
    private const uint COINIT_MULTITHREADED = 0;

    private static readonly Guid CLSID_CUIAutomation = new("FF48DBA4-60EF-4201-AA87-54103EEF594E");
    private static readonly Guid IID_IUIAutomation = new("30CBE57D-D9D0-452A-AB13-7AC5AC4825EE");

    public static readonly Guid IID_IUIAutomationSelectionItemPattern = new("A8EFA66A-0FDA-421A-9194-38021F3578EA");

    private static readonly StrategyBasedComWrappers ComWrappers = new();

    [LibraryImport("ole32.dll")]
    private static partial int CoInitializeEx(nint pvReserved, uint dwCoInit);

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(
        in Guid rclsid,
        nint pUnkOuter,
        uint dwClsContext,
        in Guid riid,
        out nint ppv);

    public static IUIAutomation? TryCreateAutomation()
    {
        _ = CoInitializeEx(nint.Zero, COINIT_MULTITHREADED);

        var hr = CoCreateInstance(
            in CLSID_CUIAutomation,
            nint.Zero,
            CLSCTX_INPROC_SERVER,
            in IID_IUIAutomation,
            out var automationPointer);

        if (hr < 0 || automationPointer == nint.Zero) return null;

        return (IUIAutomation)FromComPointer(automationPointer);
    }

    public static object FromComPointer(nint comPointer)
    {
        try
        {
            return ComWrappers.GetOrCreateObjectForComInstance(comPointer, CreateObjectFlags.None);
        }
        finally
        {
            _ = Marshal.Release(comPointer);
        }
    }
}

[GeneratedComInterface]
[Guid("30CBE57D-D9D0-452A-AB13-7AC5AC4825EE")]
internal partial interface IUIAutomation
{
    void CompareElements();
    void CompareRuntimeIds();
    void GetRootElement();

    void ElementFromHandle(nint hwnd, out IUIAutomationElement element);

    void ElementFromPoint();
    void GetFocusedElement();
    void GetRootElementBuildCache();
    void ElementFromHandleBuildCache();
    void ElementFromPointBuildCache();
    void GetFocusedElementBuildCache();
    void CreateTreeWalker();
    void GetControlViewWalker();
    void GetContentViewWalker();
    void GetRawViewWalker();
    void GetRawViewCondition();
    void GetControlViewCondition();
    void GetContentViewCondition();
    void CreateCacheRequest();

    void CreateTrueCondition(out IUIAutomationCondition condition);
}

[GeneratedComInterface]
[Guid("352FFBA8-0973-437C-A61F-F64CAFD81DF9")]
internal partial interface IUIAutomationCondition
{
}

[GeneratedComInterface]
[Guid("D22108AA-8AC5-49A5-837B-37BBB3D7591E")]
internal partial interface IUIAutomationElement
{
    void SetFocus();
    void GetRuntimeId();
    void FindFirst();

    void FindAll(int scope, IUIAutomationCondition condition, out IUIAutomationElementArray found);

    void FindFirstBuildCache();
    void FindAllBuildCache();
    void BuildUpdatedCache();
    void GetCurrentPropertyValue();
    void GetCurrentPropertyValueEx();
    void GetCachedPropertyValue();
    void GetCachedPropertyValueEx();

    void GetCurrentPatternAs(int patternId, in Guid riid, out nint patternObject);

    void GetCachedPatternAs();
    void GetCurrentPattern();
    void GetCachedPattern();
    void GetCachedParent();
    void GetCachedChildren();
    void GetCurrentProcessId(out int processId);

    void GetCurrentControlType(out int controlType);

    void GetCurrentLocalizedControlType();

    void GetCurrentName([MarshalUsing(typeof(BStrStringMarshaller))] out string name);
}

[GeneratedComInterface]
[Guid("14314595-B4BC-4055-95F2-58F2E42C9855")]
internal partial interface IUIAutomationElementArray
{
    void GetLength(out int length);

    void GetElement(int index, out IUIAutomationElement element);
}

[GeneratedComInterface]
[Guid("A8EFA66A-0FDA-421A-9194-38021F3578EA")]
internal partial interface IUIAutomationSelectionItemPattern
{
    void Select();
}
