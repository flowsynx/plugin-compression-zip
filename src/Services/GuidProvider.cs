namespace FlowSynx.Plugins.Compression.Zip.Services;

internal class GuidProvider : IGuidProvider
{
    public Guid NewGuid() => Guid.NewGuid();
}