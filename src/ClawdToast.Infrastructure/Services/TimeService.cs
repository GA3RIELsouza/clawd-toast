using ClawdToast.Application.Interfaces;
using System.Diagnostics;

namespace ClawdToast.Infrastructure.Services;

public sealed class TimeService : ITimeService
{
    private DateTime? _startDateTimeUtc;
    public DateTime GetStartDateTimeUtc() => _startDateTimeUtc ??= Process.GetCurrentProcess().StartTime.ToUniversalTime();
}
