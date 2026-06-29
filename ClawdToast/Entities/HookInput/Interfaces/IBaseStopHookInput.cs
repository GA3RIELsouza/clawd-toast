namespace ClawdToast.Entities.HookInput.Interfaces;

internal interface IBaseStopHookInput
{
    public string TranscriptPath { get; set; }
    public string LastAssistantMessage { get; set; }
}
