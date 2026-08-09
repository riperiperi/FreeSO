# FSO.ModServer

Stdio MCP server exposing the pack authoring tools (`create_pack`, `add_object`, `add_tree`, `edit_tree_node`, `validate`, `compile`, `test_in_vm`, `decompile_object`, `list_vocabulary`, ...) documented in `../MCP-DESIGN.md` and `../SCHEMA.md`.

## Build

```
dotnet build FSO.ModServer
```

Produces `FSO.ModServer/bin/Debug/net8.0/FSO.ModServer.dll`, run as `dotnet <path>/FSO.ModServer.dll`. It speaks MCP over stdio — stdout is the wire protocol, all logging goes to stderr.

## Connecting an agent

See `mcp-config.example.json` for the `mcpServers` entry shape (same format Claude Desktop's `claude_desktop_config.json` uses). Fill in the absolute path to the built DLL.

**Claude Code** (project- or user-scoped):

```
claude mcp add fso-modserver -- dotnet /absolute/path/to/FSO.ModServer.dll
```

Verify with `claude mcp list` / `claude mcp get fso-modserver`.

**Claude Desktop**: merge the contents of `mcp-config.example.json` into `claude_desktop_config.json`'s `mcpServers` object, then restart Desktop.

Once connected, an agent should see all tools via `tools/list` and can author an object purely conversationally — no pack JSON needs to be shown to the human.
