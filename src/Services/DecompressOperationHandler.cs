using FlowSynx.PluginCore;
using FlowSynx.Plugins.Compression.Zip.Models;
using System.IO.Compression;

namespace FlowSynx.Plugins.Compression.Zip.Services;

internal sealed class DecompressOperationHandler : IZipOperationHandler
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
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);

        foreach (var entry in archive.Entries)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(entry.Name))
                continue; // skip directories

            results.Add(CreateContextFromEntry(entry));
        }

        return Task.FromResult<IReadOnlyList<PluginContext>>(results);
    }

    private PluginContext CreateContextFromEntry(ZipArchiveEntry entry)
    {
        var ctx = new PluginContext(entry.FullName, "Data")
        {
            Format = Path.GetExtension(entry.Name).TrimStart('.'),
        };

        using var entryStream = entry.Open();

        using var ms = new MemoryStream();
        entryStream.CopyTo(ms);
        ctx.RawData = ms.ToArray();

        ctx.Metadata["FileName"] = entry.Name;
        ctx.Metadata["CompressedSize"] = entry.CompressedLength;
        ctx.Metadata["UncompressedSize"] = entry.Length;

        return ctx;
    }
}