using FlowSynx.PluginCore;
using FlowSynx.Plugins.Compression.Zip.Models;
using SharpCompress.Common;
using SharpCompress.Writers;
using System.Text;

namespace FlowSynx.Plugins.Compression.Zip.Services;

internal class CompressOperationHandler : IZipOperationHandler<PluginContext>, IZipOperationHandlerBase
{
    private readonly IGuidProvider _guidProvider;

    public CompressOperationHandler(IGuidProvider guidProvider)
    {
        _guidProvider = guidProvider ?? throw new ArgumentNullException(nameof(guidProvider));
    }

    public Task<PluginContext> HandleAsync(
        IEnumerable<PluginContext> inputs,
        InputParameter parameter,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(inputs);

        byte[] zipBytes = CreateZipBytes(inputs, ct);

        string filename = $"{_guidProvider.NewGuid().ToString()}.zip";

        var outCtx = new PluginContext(filename, "Data")
        {
            Format = "Zip",
            RawData = zipBytes
        };

        return Task.FromResult(outCtx);
    }

    private static byte[] CreateZipBytes(IEnumerable<PluginContext> inputs, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        using (var zipWriter = WriterFactory.Open(ms, ArchiveType.Zip, new WriterOptions(CompressionType.Deflate)))
        {
            foreach (var ctx in inputs)
            {
                ct.ThrowIfCancellationRequested();
                if (ctx.RawData is { Length: > 0 })
                {
                    using var entryStream = new MemoryStream(ctx.RawData);
                    zipWriter.Write(ctx.Id, entryStream, null);
                }
                else if (!string.IsNullOrEmpty(ctx.Content))
                {
                    var contentBytes = Encoding.UTF8.GetBytes(ctx.Content);
                    using var entryStream = new MemoryStream(contentBytes);
                    zipWriter.Write(ctx.Id, entryStream, null);
                }
                else if (ctx.StructuredData is { Count: > 0 })
                {
                    throw new NotSupportedException("StructuredData compression is not supported.");
                }
            }
        }
        return ms.ToArray();
    }
}