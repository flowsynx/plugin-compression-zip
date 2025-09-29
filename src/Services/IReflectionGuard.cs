namespace FlowSynx.Plugins.Compression.Zip.Services;

public interface IReflectionGuard
{
    bool IsCalledViaReflection();
}