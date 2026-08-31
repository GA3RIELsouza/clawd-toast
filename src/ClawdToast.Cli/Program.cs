using ClawdToast.Application.Interfaces;
using ClawdToast.Cli.Configurations;
using ClawdToast.Infrastructure.Loggers;
using ClawdToast.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

ConsoleEncodingConfiguration.Initialize();
CultureInfoConfiguration.Initialize();

var services = new ServiceCollection();

services.AddLogging(
    builder =>
    {
        builder.ClearProviders();
        builder.AddProvider(new FileLoggerProvider("logs/clawd-toast.log"));
        builder.SetMinimumLevel(LogLevel.Information);
    });

services.AddScoped<IManifestResourceService, ManifestResourceService>();
services.AddScoped<ISettingsService, SettingsService>();
services.AddScoped<IAppRegistryService, AppRegistryService>();
services.AddScoped<ICustomSoundService, CustomSoundService>();
services.AddScoped<ITimeService, TimeService>();
services.AddScoped<IFocusService, FocusService>();
services.AddScoped<IHookInputService, HookInputService>();
services.AddScoped<ITranscriptService, TranscriptService>();
services.AddScoped<IFrontendService, FrontendService>();
services.AddScoped<IToastNotificationService, ToastNotificationService>();
services.AddScoped<ICliRunnerService, CliRunnerService>();

using var provider = services.BuildServiceProvider();
var cliRunnerService = provider.GetRequiredService<ICliRunnerService>();

using var hookInputStream = Console.OpenStandardInput();

var result = cliRunnerService.Run(hookInputStream);
return result;
