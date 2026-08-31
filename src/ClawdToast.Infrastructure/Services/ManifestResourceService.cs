using ClawdToast.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ClawdToast.Infrastructure.Services;

public sealed partial class ManifestResourceService(ILogger<ManifestResourceService> logger) : IManifestResourceService
{
    #region Logging

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "The manifest resource \"{ResourceName}\" was already extracted into \"{OutputTempPath}\".")]
    private static partial void LogManifestResourceAlreadyExtracted(
        ILogger logger,
        string resourceName,
        string outputTempPath);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully extracted the manifest resource \"{ResourceName}\" into \"{OutputTempPath}\".")]
    private static partial void LogManifestResourceExtracted(
        ILogger logger,
        string resourceName,
        string outputTempPath);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Extraction of the manifest resource \"{ResourceName}\" from the executing assembly failed because it could not be found.")]
    private static partial void LogManifestResourceNotFound(
        ILogger logger,
        string resourceName);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Extraction of the manifest resource \"{ResourceName}\" from the executing assembly failed.")]
    private static partial void LogManifestResourceExtractionFailed(
        ILogger logger,
        string resourceName,
        Exception exception);

    #endregion

    public bool TryExtractIntoTemp(string resourceName, [NotNullWhen(true)] out string? outputTempPath)
    {
        var tempPath = Path.GetTempPath();
        var into = Path.Combine(tempPath, resourceName);

        if (File.Exists(into))
        {
            LogManifestResourceAlreadyExtracted(logger, resourceName, into);

            outputTempPath = into;
            return true;
        }

        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

            if (stream is null)
            {
                LogManifestResourceNotFound(logger, resourceName);

                outputTempPath = null;
                return false;
            }
            else
            {
                using var fileStream = new FileStream(into, FileMode.Create, FileAccess.Write);
                stream.CopyTo(fileStream);

                LogManifestResourceExtracted(logger, resourceName, into);

                outputTempPath = into;
                return true;
            }
        }
        catch (Exception ex)
        {
            LogManifestResourceExtractionFailed(logger, resourceName, ex);

            outputTempPath = null;
            return false;
        }
    }
}
