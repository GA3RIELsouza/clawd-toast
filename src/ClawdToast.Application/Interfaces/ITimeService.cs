namespace ClawdToast.Application.Interfaces;

/// <summary>
/// Responsible for providing the exact time where the program started.
/// </summary>
public interface ITimeService
{
    DateTime GetStartDateTimeUtc();
}
