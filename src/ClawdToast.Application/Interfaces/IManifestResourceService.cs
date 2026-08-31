using System.Diagnostics.CodeAnalysis;

namespace ClawdToast.Application.Interfaces;

/// <summary>
/// Responsible for extracting embedded manifest resources into the temp folder.
/// </summary>
public interface IManifestResourceService
{
    bool TryExtractIntoTemp(string resourceName, [NotNullWhen(true)] out string? outputTempPath);
}
