using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.Compression.Zip.Extensions;
using FlowSynx.Plugins.Compression.Zip.Models;
using FlowSynx.Plugins.Compression.Zip.Services;

namespace FlowSynx.Plugins.Compression.Zip;

public class ZipCompressionPlugin : IPlugin
{
    private IPluginLogger? _logger;
    private readonly IGuidProvider _guidProvider;
    private readonly IReflectionGuard _reflectionGuard;
    private bool _isInitialized;

    private static readonly IReadOnlyDictionary<string, Func<IGuidProvider, IZipOperationHandler>> OperationFactories =
        new Dictionary<string, Func<IGuidProvider, IZipOperationHandler>>(StringComparer.OrdinalIgnoreCase)
        {
            ["compress"] = guidProvider => new CompressOperationHandler(guidProvider),
            ["decompress"] = guidProvider => new DecompressOperationHandler(guidProvider)
        };

    private readonly IReadOnlyDictionary<string, IZipOperationHandler> _operationMap;

    public ZipCompressionPlugin() : this(new GuidProvider(), new DefaultReflectionGuard()) { }

    internal ZipCompressionPlugin(IGuidProvider guidProvider, IReflectionGuard reflectionGuard)
    {
        _guidProvider = guidProvider ?? throw new ArgumentNullException(nameof(guidProvider));
        _reflectionGuard = reflectionGuard ?? throw new ArgumentNullException(nameof(reflectionGuard));
        _operationMap = OperationFactories.ToDictionary(kvp => kvp.Key, kvp => kvp.Value(_guidProvider), StringComparer.OrdinalIgnoreCase);
    }

    public PluginMetadata Metadata => new()
    {
        Id = Guid.Parse("ba5d314f-375c-4743-9e84-191337ed83b6"),
        Name = "Zip",
        CompanyName = "FlowSynx",
        Description = Resources.PluginDescription,
        Version = new Version(1, 0, 0),
        Category = PluginCategory.Compression,
        Authors = new List<string> { "FlowSynx" },
        Copyright = "© FlowSynx. All rights reserved.",
        Icon = "flowsynx.png",
        ReadMe = "README.md",
        RepositoryUrl = "https://github.com/flowsynx/plugin-compression-zip",
        ProjectUrl = "https://flowsynx.io",
        Tags = new List<string>() { "zip", "compression", "decompression", "archive", "flowsynx" },
        MinimumFlowSynxVersion = new Version(1, 1, 1),
    };

    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(ZipCompressionPluginSpecifications);
    public IReadOnlyCollection<string> SupportedOperations => _operationMap.Keys.ToList();

    public Task Initialize(IPluginLogger logger)
    {
        ThrowIfReflection();
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _isInitialized = true;
        return Task.CompletedTask;
    }

    public async Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfReflection();
        ThrowIfNotInitialized();

        var inputParameter = parameters.ToObject<InputParameter>();
        var handler = GetHandler(inputParameter.Operation);
        var contexts = ParseDataToContext(inputParameter.Data);
        return await handler.HandleAsync(contexts, inputParameter, cancellationToken);
    }

    private void ThrowIfReflection()
    {
        if (_reflectionGuard.IsCalledViaReflection())
            throw new InvalidOperationException(Resources.ReflectionBasedAccessIsNotAllowed);
    }

    private void ThrowIfNotInitialized()
    {
        if (!_isInitialized)
            throw new InvalidOperationException($"Plugin '{Metadata.Name}' v{Metadata.Version} is not initialized.");
    }

    private IZipOperationHandler GetHandler(string operation)
    {
        if (!_operationMap.TryGetValue(operation, out var handler))
            throw new NotSupportedException($"Operation '{operation}' is not supported.");
        return handler;
    }

    private IEnumerable<PluginContext> ParseDataToContext(object? data)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data), "Input data cannot be null.");

        return data switch
        {
            PluginContext ctx => new[] { ctx },
            IEnumerable<PluginContext> ctxList => ctxList,
            string str => new[] { CreateContextFromStringData(_guidProvider.NewGuid().ToString(), str) },
            _ => throw new NotSupportedException("Unsupported input type. Must be PluginContext, IEnumerable<PluginContext>, or string.")
        };
    }

    private PluginContext CreateContextFromStringData(string id, string data)
    {
        var dataBytesArray = data.IsBase64String() ? data.Base64ToByteArray() : data.ToByteArray();
        return new PluginContext(id, "Data")
        {
            RawData = dataBytesArray,
        };
    }
}