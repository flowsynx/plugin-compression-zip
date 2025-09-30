using FlowSynx.PluginCore;
using FlowSynx.Plugins.Compression.Zip.Models;

namespace FlowSynx.Plugins.Compression.Zip.Services;

internal interface IZipOperationHandler<TResult>
{
    Task<TResult> HandleAsync(
        IEnumerable<PluginContext> inputs,
        InputParameter parameter,
        CancellationToken ct);
}