using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace FSO.PackCompiler
{
    /// <summary>
    /// Strict JSON object reader. Every field must be consumed by name; Done() reports
    /// any leftover (unknown) fields as errors with their full path.
    /// </summary>
    public class JsonObj
    {
        public JObject Raw;
        public string Path;
        private readonly Diagnostics D;
        private readonly HashSet<string> Used = new HashSet<string>();

        public JsonObj(JObject raw, string path, Diagnostics d)
        {
            Raw = raw;
            Path = path;
            D = d;
        }

        public static JsonObj From(JToken token, string path, Diagnostics d)
        {
            if (token is JObject o) return new JsonObj(o, path, d);
            d.Error(path, "expected an object");
            return new JsonObj(new JObject(), path, d);
        }

        public bool Has(string name)
        {
            return Raw.ContainsKey(name);
        }

        public JToken Opt(string name)
        {
            Used.Add(name);
            return Raw.TryGetValue(name, out var t) ? t : null;
        }

        public string OptString(string name, string def = null)
        {
            var t = Opt(name);
            if (t == null) return def;
            if (t.Type == JTokenType.String) return (string)t;
            D.Error(Path + "." + name, "expected a string");
            return def;
        }

        public string ReqString(string name)
        {
            var t = Opt(name);
            if (t == null)
            {
                D.Error(Path, "missing required field \"" + name + "\"");
                return null;
            }
            if (t.Type == JTokenType.String) return (string)t;
            D.Error(Path + "." + name, "expected a string");
            return null;
        }

        public int OptInt(string name, int def = 0)
        {
            var t = Opt(name);
            if (t == null) return def;
            if (t.Type == JTokenType.Integer) return (int)t;
            D.Error(Path + "." + name, "expected an integer");
            return def;
        }

        public int? OptIntN(string name)
        {
            var t = Opt(name);
            if (t == null) return null;
            if (t.Type == JTokenType.Integer) return (int)t;
            D.Error(Path + "." + name, "expected an integer");
            return null;
        }

        public double OptDouble(string name, double def = 0)
        {
            var t = Opt(name);
            if (t == null) return def;
            if (t.Type == JTokenType.Float || t.Type == JTokenType.Integer) return (double)t;
            D.Error(Path + "." + name, "expected a number");
            return def;
        }

        public bool OptBool(string name, bool def = false)
        {
            var t = Opt(name);
            if (t == null) return def;
            if (t.Type == JTokenType.Boolean) return (bool)t;
            D.Error(Path + "." + name, "expected a boolean");
            return def;
        }

        public JsonObj OptObj(string name)
        {
            var t = Opt(name);
            if (t == null) return null;
            return JsonObj.From(t, Path + "." + name, D);
        }

        public JsonObj ReqObj(string name)
        {
            var t = Opt(name);
            if (t == null)
            {
                D.Error(Path, "missing required field \"" + name + "\"");
                return null;
            }
            return JsonObj.From(t, Path + "." + name, D);
        }

        public JArray OptArr(string name)
        {
            var t = Opt(name);
            if (t == null) return null;
            if (t is JArray a) return a;
            D.Error(Path + "." + name, "expected an array");
            return null;
        }

        /// <summary>GUID field: either a "0x...." hex string or a nonnegative integer.</summary>
        public uint? OptGuid(string name)
        {
            var t = Opt(name);
            if (t == null) return null;
            if (t.Type == JTokenType.String)
            {
                var s = (string)t;
                if (s.StartsWith("0x") &&
                    uint.TryParse(s.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
                    return v;
                D.Error(Path + "." + name, "invalid GUID \"" + s + "\" (expected 0x-prefixed hex)");
                return null;
            }
            if (t.Type == JTokenType.Integer)
            {
                var l = (long)t;
                if (l >= 0 && l <= uint.MaxValue) return (uint)l;
                D.Error(Path + "." + name, "GUID out of range");
                return null;
            }
            D.Error(Path + "." + name, "expected a GUID (hex string or integer)");
            return null;
        }

        /// <summary>Report all unconsumed fields as unknown-field errors.</summary>
        public void Done()
        {
            foreach (var prop in Raw.Properties())
            {
                if (!Used.Contains(prop.Name))
                    D.Error(Path + "." + prop.Name, "unknown field");
            }
        }

        public IEnumerable<JProperty> Properties()
        {
            return Raw.Properties().ToList();
        }

        public void MarkAllUsed()
        {
            foreach (var prop in Raw.Properties()) Used.Add(prop.Name);
        }
    }
}
