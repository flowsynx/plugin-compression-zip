using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.Compression.Zip.Extensions;
using FlowSynx.Plugins.Compression.Zip.Models;
using FlowSynx.Plugins.Compression.Zip.Services;

namespace FlowSynx.Plugins.Compression.Zip;

/// <summary>
/// Provides ZIP compression and decompression operations for FlowSynx.
/// </summary>
public class ZipCompressionPlugin : IPlugin
{
    private IPluginLogger? _logger;
    private readonly IGuidProvider _guidProvider;
    private readonly IReflectionGuard _reflectionGuard;
    private bool _isInitialized;

    private const string CompressOperation = "compress";
    private const string DecompressOperation = "decompress";

    // Map operation -> handler factory
    private static readonly IReadOnlyDictionary<string, Func<IGuidProvider, IZipOperationHandlerBase>> OperationFactories =
        new Dictionary<string, Func<IGuidProvider, IZipOperationHandlerBase>>(StringComparer.OrdinalIgnoreCase)
        {
            [CompressOperation] = gp => new CompressOperationHandler(gp),
            [DecompressOperation] = gp => new DecompressOperationHandler(gp)
        };

    private readonly IReadOnlyDictionary<string, IZipOperationHandlerBase> _operationMap;

    /// <summary>
    /// Initializes a new instance of <see cref="ZipCompressionPlugin"/>.
    /// </summary>
    public ZipCompressionPlugin()
        : this(new GuidProvider(), new DefaultReflectionGuard()) { }

    /// <summary>
    /// Internal constructor for dependency injection.
    /// </summary>
    internal ZipCompressionPlugin(IGuidProvider guidProvider, IReflectionGuard reflectionGuard)
    {
        _guidProvider = guidProvider ?? throw new ArgumentNullException(nameof(guidProvider));
        _reflectionGuard = reflectionGuard ?? throw new ArgumentNullException(nameof(reflectionGuard));

        // Create concrete handler instances
        _operationMap = OperationFactories.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value(_guidProvider),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
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
        Tags = new List<string> { "zip", "compression", "decompression", "archive", "flowsynx" },
        MinimumFlowSynxVersion = new Version(1, 1, 1),
    };

    /// <inheritdoc/>
    public PluginSpecifications? Specifications { get; set; }

    /// <inheritdoc/>
    public Type SpecificationsType => typeof(ZipCompressionPluginSpecifications);

    /// <inheritdoc/>
    public IReadOnlyCollection<string> SupportedOperations => _operationMap.Keys.ToList();

    /// <inheritdoc/>
    public Task Initialize(IPluginLogger logger)
    {
        ThrowIfReflection();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _isInitialized = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfReflection();
        ThrowIfNotInitialized();

        var inputParameter = parameters.ToObject<InputParameter>();
        var operation = inputParameter.Operation.ToLowerInvariant();
        var contexts = NormalizeInputs(operation, inputParameter.Data);

        return operation switch
        {
            CompressOperation => await ((IZipOperationHandler<PluginContext>)GetHandler(CompressOperation)).HandleAsync(contexts, inputParameter, cancellationToken),
            DecompressOperation => await ((IZipOperationHandler<IReadOnlyList<PluginContext>>)GetHandler(DecompressOperation)).HandleAsync(contexts, inputParameter, cancellationToken),
            _ => throw new NotSupportedException($"Operation '{inputParameter.Operation}' is not supported.")
        };
    }

    /// <summary>
    /// Throws if called via reflection.
    /// </summary>
    private void ThrowIfReflection()
    {
        if (_reflectionGuard.IsCalledViaReflection())
            throw new InvalidOperationException(Resources.ReflectionBasedAccessIsNotAllowed);
    }

    /// <summary>
    /// Throws if plugin is not initialized.
    /// </summary>
    private void ThrowIfNotInitialized()
    {
        if (!_isInitialized)
            throw new InvalidOperationException($"Plugin '{Metadata.Name}' v{Metadata.Version} is not initialized.");
    }

    /// <summary>
    /// Gets the handler for the specified operation.
    /// </summary>
    private IZipOperationHandlerBase GetHandler(string operation)
    {
        if (!_operationMap.TryGetValue(operation, out var handler))
            throw new NotSupportedException($"Operation '{operation}' is not supported.");
        return handler;
    }

    /// <summary>
    /// Normalizes input data for the specified operation.
    /// For "compress" allows single or multiple PluginContext items.
    /// For "decompress" requires exactly one PluginContext (a zip archive).
    /// </summary>
    private IEnumerable<PluginContext> NormalizeInputs(string operation, object? data)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data), "Input data cannot be null.");

        IEnumerable<PluginContext> asList = data switch
        {
            PluginContext ctx => new[] { ctx },
            IEnumerable<PluginContext> ctxList => ctxList,
            string s => new[] { CreateContextFromStringData(_guidProvider.NewGuid().ToString(), s) },
            _ => throw new NotSupportedException(
                "Input must be PluginContext, IEnumerable<PluginContext>, or string.")
        };

        if (operation.Equals(DecompressOperation, StringComparison.OrdinalIgnoreCase))
        {
            var list = asList.ToList();
            if (list.Count != 1)
                throw new NotSupportedException("Decompress requires exactly one PluginContext containing the ZIP.");
            return list;
        }

        return asList;
    }

    /// <summary>
    /// Creates a PluginContext from string data, converting to bytes as needed.
    /// </summary>
    private PluginContext CreateContextFromStringData(string id, string data)
    {
        var bytes = data.IsBase64String() ? data.Base64ToByteArray() : data.ToByteArray();
        return new PluginContext(id, "Data") { RawData = bytes };
    }
}