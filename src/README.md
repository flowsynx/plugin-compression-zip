## Zip Compression Plugin

The **Zip Compression Plugin** is a built-in, plug-and-play integration for the FlowSynx automation engine. It enables compressing and decompressing files and folders using the ZIP format within workflows, with no custom coding required.

This plugin is automatically installed by the FlowSynx engine when selected in the workflow builder. It is not intended for standalone developer usage outside the FlowSynx platform.

---

## Purpose

The Zip Compression Plugin allows FlowSynx users to:

- Compress files or folders into a ZIP archive.
- Decompress ZIP archives to extract their contents.

It integrates seamlessly into FlowSynx no-code/low-code workflows for file management and data transfer tasks.

---

## Supported Operations

- **compress**: Compresses provided file or folder data into a ZIP archive.
- **decompress**: Extracts files and folders from provided ZIP archive data.

---

## Input Parameters

The plugin accepts the following parameters:

- `Operation` (string): **Required.** The type of operation to perform. Supported values are `compress` and `decompress`.
- `Data` (object): **Required.** The data to be compressed or decompressed. Supported types:
  - A string (raw or base64-encoded data)
  - A `PluginContext` object
  - An array of `PluginContext` objects

### Example input (string data)

```json
{
  "Operation": "compress",
  "Data": "<base64 or raw file data>"
}
```

---

## Operation Examples

### compress Operation

**Input Parameters:**

```json
{
  "Operation": "compress",
  "Data": "<base64 or raw file data>"
}
```

---

### decompress Operation

**Input Parameters:**

```json
{
  "Operation": "decompress",
  "Data": "<zip archive data>"
}
```

---

## Example Use Case in FlowSynx

1. Add the Zip Compression plugin to your FlowSynx workflow.
2. Set `Operation` to one of: `compress` or `decompress`.
3. Provide values for `Data` (as string, PluginContext, or array of PluginContext).
4. Use the plugin output downstream in your workflow for file management or data transfer.

---

## Debugging Tips

- Ensure `Data` contains valid file, folder, or archive data in one of the supported formats.
- For decompress operations, verify the ZIP archive data is not corrupted.

---

## Security Notes

- No data is persisted unless explicitly configured.
- All operations run in a secure sandbox within FlowSynx.
- Only authorized platform users can view or modify configurations.

---

## License

© FlowSynx. All rights reserved.