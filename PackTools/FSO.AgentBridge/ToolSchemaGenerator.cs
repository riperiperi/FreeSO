using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FSO.AgentBridge
{
    /// <summary>
    /// Turns PackToolHandlers' [McpServerTool] methods into Anthropic tool definitions by
    /// reflection, so the tool table has exactly one source of truth: the handler signatures
    /// and their [Description] attributes. The MCP attributes are reused rather than replaced
    /// — an in-process agent doesn't speak MCP, but the metadata MCP needs is the same
    /// metadata the Messages API needs, and duplicating it would guarantee drift.
    /// </summary>
    public static class ToolSchemaGenerator
    {
        public class ToolDefinition
        {
            public string Name;
            public string Description;
            public JsonObject InputSchema;
            public MethodInfo Method;
            /// <summary>Parameter order, so a tool call's named arguments can be positioned for Invoke.</summary>
            public ParameterInfo[] Parameters;
        }

        /// <summary>
        /// Reflects over <paramref name="handlerType"/> (normally PackToolHandlers).
        /// Ordered by tool name: prompt caching is a byte-prefix match, so a tool array whose
        /// order shifts between sessions invalidates the cached prefix for every player.
        /// Reflection order is not guaranteed stable, hence the explicit sort.
        /// </summary>
        public static List<ToolDefinition> Generate(Type handlerType)
        {
            var tools = new List<ToolDefinition>();

            foreach (var method in handlerType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var name = McpToolName(method);
                if (name == null) continue;

                tools.Add(new ToolDefinition
                {
                    Name = name,
                    Description = method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "",
                    InputSchema = BuildInputSchema(method),
                    Method = method,
                    Parameters = method.GetParameters(),
                });
            }

            return tools.OrderBy(t => t.Name, StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// Reads the Name property off [McpServerTool] without referencing the MCP assembly:
        /// AgentBridge deliberately doesn't depend on ModelContextProtocol, since the whole
        /// point is that the in-process path needs no MCP. Matching by attribute type name
        /// keeps that dependency out while still reading the one attribute that carries the
        /// wire-facing tool name.
        /// </summary>
        private static string McpToolName(MethodInfo method)
        {
            foreach (var attr in method.GetCustomAttributes())
            {
                if (attr.GetType().Name != "McpServerToolAttribute") continue;
                var nameProp = attr.GetType().GetProperty("Name");
                var value = nameProp?.GetValue(attr) as string;
                // A tool with no explicit Name would be exposed under its method name by the
                // MCP SDK; the handlers all set one, so treat a missing name as "not a tool"
                // rather than silently inventing a different name than MCP clients see.
                if (!string.IsNullOrEmpty(value)) return value;
            }
            return null;
        }

        private static JsonObject BuildInputSchema(MethodInfo method)
        {
            var properties = new JsonObject();
            var required = new JsonArray();

            foreach (var p in method.GetParameters())
            {
                var prop = new JsonObject { ["type"] = JsonTypeFor(p.ParameterType) };
                var desc = p.GetCustomAttribute<DescriptionAttribute>()?.Description;
                if (!string.IsNullOrEmpty(desc)) prop["description"] = desc;

                properties[SnakeCase(p.Name)] = prop;

                // A C# default (e.g. price = 0, scenarioJson = "") is exactly the signal that
                // the parameter is optional to the model too.
                if (!p.HasDefaultValue) required.Add(SnakeCase(p.Name));
            }

            return new JsonObject
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = required,
            };
        }

        private static string JsonTypeFor(Type t)
        {
            if (t == typeof(int) || t == typeof(long) || t == typeof(short)) return "integer";
            if (t == typeof(double) || t == typeof(float) || t == typeof(decimal)) return "number";
            if (t == typeof(bool)) return "boolean";
            return "string";
        }

        // The handlers use C# camelCase parameter names; the documented tool surface in
        // MCP-DESIGN.md is snake_case (pack_session_id, not packSessionId).
        internal static string SnakeCase(string camel)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < camel.Length; i++)
            {
                if (char.IsUpper(camel[i]))
                {
                    if (i > 0) sb.Append('_');
                    sb.Append(char.ToLowerInvariant(camel[i]));
                }
                else sb.Append(camel[i]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Maps the model's named arguments onto the handler's positional parameters and
        /// invokes it. Missing optional arguments fall back to the C# default, so the model
        /// omitting an optional field behaves the same as an MCP client omitting it.
        /// </summary>
        public static object Invoke(ToolDefinition tool, JsonObject arguments)
        {
            var args = new object[tool.Parameters.Length];
            for (int i = 0; i < tool.Parameters.Length; i++)
            {
                var p = tool.Parameters[i];
                var node = arguments?[SnakeCase(p.Name)];

                if (node == null)
                {
                    args[i] = p.HasDefaultValue ? p.DefaultValue : DefaultOf(p.ParameterType);
                    continue;
                }

                args[i] = p.ParameterType == typeof(int) ? node.GetValue<int>()
                    : p.ParameterType == typeof(bool) ? node.GetValue<bool>()
                    : p.ParameterType == typeof(double) ? node.GetValue<double>()
                    : (object)node.ToString();
            }

            return tool.Method.Invoke(null, args);
        }

        private static object DefaultOf(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;

        public static string SchemaJson(ToolDefinition tool) =>
            tool.InputSchema.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
