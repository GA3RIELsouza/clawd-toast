namespace ClawdToast.Domain.Models.HookInput.Interfaces;

public interface IBaseStopHookInput
{
    public string TranscriptPath { get; set; }
    public string LastAssistantMessage { get; set; }
}
