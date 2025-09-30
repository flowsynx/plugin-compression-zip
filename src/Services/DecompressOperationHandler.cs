using FlowSynx.PluginCore;
using FlowSynx.Plugins.Compression.Zip.Models;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Readers;

namespace FlowSynx.Plugins.Compression.Zip.Services;

internal sealed class DecompressOperationHandler : IZipOperationHandler<IReadOnlyList<PluginContext>>, IZipOperationHandlerBase
{
    private readonly IGuidProvider _guidProvider;

    public DecompressOperationHandler(IGuidProvider guidProvider)
    {
        _guidProvider = guidProvider ?? throw new ArgumentNullException(nameof(guidProvider));
    }

    public Task<IReadOnlyList<PluginContext>> HandleAsync(
        IEnumerable<PluginContext> inputs,
        InputParameter parameter,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(inputs);

        var inputList = inputs as IList<PluginContext> ?? inputs.ToList();
        if (inputList.Count != 1)
            throw new NotSupportedException("Exactly one PluginContext is supported for decompression.");

        var input = inputList[0];
        if (input.RawData is null || input.RawData.Length == 0)
            throw new InvalidDataException("RawData must contain a valid ZIP archive.");

        var results = new List<PluginContext>();
        using var zipStream = new MemoryStream(input.RawData, writable: false);
        using var archive = ZipArchive.Open(zipStream);

        foreach (var entry in archive.Entries)
        {
            ct.ThrowIfCancellationRequested();
            if (entry.IsDirectory) continue;
            results.Add(CreateContextFromEntry(entry));
        }

        return Task.FromResult<IReadOnlyList<PluginContext>>(results);
    }

    private PluginContext CreateContextFromEntry(IArchiveEntry entry)
    {
        var ctx = new PluginContext(entry.Key, "Data")
        {
            Format = Path.GetExtension(entry.Key).TrimStart('.')
        };

        using var ms = new MemoryStream();
        entry.OpenEntryStream().CopyTo(ms);
        ctx.RawData = ms.ToArray();
        ctx.Metadata["FileName"] = Path.GetFileName(entry.Key);
        ctx.Metadata["CompressedSize"] = entry.CompressedSize;
        ctx.Metadata["UncompressedSize"] = entry.Size;
        return ctx;
    }
}