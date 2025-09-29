using FlowSynx.PluginCore.Helpers;

namespace FlowSynx.Plugins.Compression.Zip.Services;

internal class DefaultReflectionGuard : IReflectionGuard
{
    public bool IsCalledViaReflection() => ReflectionHelper.IsCalledViaReflection();
}