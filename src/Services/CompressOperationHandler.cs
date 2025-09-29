using FlowSynx.PluginCore;
using FlowSynx.Plugins.Compression.Zip.Models;
using System.IO.Compression;
using System.Text;

namespace FlowSynx.Plugins.Compression.Zip.Services;

internal class CompressOperationHandler : IZipOperationHandler
{
    private readonly IGuidProvider _guidProvider;

    public CompressOperationHandler(IGuidProvider guidProvider)
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

        // Create the zip directly into a byte array
        byte[] zipBytes = CreateZipBytes(inputs, ct);

        string filename = string.IsNullOrEmpty(parameter.FileName)
            ? _guidProvider.NewGuid().ToString()
            : parameter.FileName;

        var outCtx = new PluginContext($"{filename}.zip", "Data")
        {
            Format = "Zip",
            RawData = zipBytes
        };

        return Task.FromResult<IReadOnlyList<PluginContext>>(new[] { outCtx });
    }

    private static byte[] CreateZipBytes(IEnumerable<PluginContext> inputs, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var ctx in inputs)
            {
                ct.ThrowIfCancellationRequested();

                if (ctx.RawData is { Length: > 0 })
                {
                    var entry = archive.CreateEntry(ctx.Id, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    entryStream.Write(ctx.RawData, 0, ctx.RawData.Length);
                }
                else if (!string.IsNullOrEmpty(ctx.Content))
                {
                    var entry = archive.CreateEntry(ctx.Id, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    using var writer = new StreamWriter(entryStream, Encoding.UTF8, leaveOpen: true);
                    writer.Write(ctx.Content);
                    writer.Flush(); // <-- important
                }
                else if (ctx.StructuredData is { Count: > 0 })
                {
                    throw new NotSupportedException("StructuredData compression is not supported.");
                }
            }
        }

        return ms.ToArray(); // MemoryStream is already fully populated
    }
}