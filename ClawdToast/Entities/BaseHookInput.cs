using ClawdToast.Visitors;
using System.Text.Json.Serialization;

namespace ClawdToast.Entities;

[JsonPolymorphic(
    TypeDiscriminatorPropertyName = "hook_event_name",
    IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(StopHookInput), "Stop")]
internal abstract class BaseHookInput
{
    public abstract bool Apply(IHookInputVisitor visitor, out TimeSpan duration);
}
