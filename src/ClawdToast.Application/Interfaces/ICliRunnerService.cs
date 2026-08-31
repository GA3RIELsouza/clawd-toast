namespace ClawdToast.Application.Interfaces;

/// <summary>
/// The program itself.
/// </summary>
public interface ICliRunnerService
{
    int Run(Stream hookInputStream);
}
