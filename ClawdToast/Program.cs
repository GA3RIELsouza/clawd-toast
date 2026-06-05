using Microsoft.Win32;
using System.Reflection;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

const string AppId = $"GA3RIELsouza.ClawdToast";
const string DisplayName = "Clawd Toast";
const string RegistryKey = $@"SOFTWARE\Classes\AppUserModelId\{AppId}";

var iconUri = Path.Combine(Path.GetTempPath(), "ClawdToast_icon16.png");

if (!File.Exists(iconUri))
{
    using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ClawdToast.icon16.png");
    if (stream is not null)
    {
        using var fileStream = new FileStream(iconUri, FileMode.Create, FileAccess.Write);
        stream.CopyTo(fileStream);
    }
}

static void RegisterAppId(string registryKey, string displayName, string iconUri)
{
    using var reg = Registry.CurrentUser.CreateSubKey(registryKey);
    reg.SetValue("DisplayName", displayName);
    reg.SetValue("IconUri", iconUri);
}

RegisterAppId(RegistryKey, DisplayName, iconUri);

var xml = $"""
<toast duration="long">
    <visual>
        <binding template="ToastGeneric">
            <text>O Claude respondeu, confira seu Claude Code.</text>
        </binding>
    </visual>
    <commands scenario="alarm">
        <command id="dismiss" />
    </commands>
</toast>
""";

var doc = new XmlDocument();
doc.LoadXml(xml);
var toast = new ToastNotification(doc);
ToastNotificationManager.CreateToastNotifier(AppId).Show(toast);
