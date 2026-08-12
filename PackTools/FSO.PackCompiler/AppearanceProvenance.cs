using System;
using System.Linq;
using System.Text;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using Newtonsoft.Json.Linq;

namespace FSO.PackCompiler
{
    /// <summary>
    /// Records how an object's appearance was authored — the clone source GUID, or the
    /// generator name and params — so Decompiler can recover it exactly instead of
    /// fabricating a placeholder appearance.generated (chair) with a warning. Without this,
    /// compile -> decompile -> compile is lossy: the second .iff can look nothing like the
    /// first, a silent transform wearing a success costume.
    ///
    /// Stored as a single JSON string in a reserved STR# chunk rather than a new binary
    /// chunk type, so this needs no change to FSO.Files' chunk registry (TSOClient/,
    /// out of scope for this change, owned elsewhere). The JSON is exactly the pack schema's
    /// own "appearance" shape, so Decompiler can drop it straight into its output with no
    /// reconstruction logic of its own.
    ///
    /// The client has never looked up this chunk id and ignores STR# chunks it doesn't
    /// reference by id — WorldObjectProvider/DGRP/etc. only ever resolve the specific ids
    /// PackBuilder assigns them (DIALOG_STR_ID, ANIM_TABLE_ID). An unreferenced STR# chunk
    /// just sits in the file unread.
    /// </summary>
    public static class AppearanceProvenance
    {
        /// <summary>
        /// Deliberately far outside PackBuilder's other chunk ids (OBJD=1, TTAB=128,
        /// ANIM_TABLE=129, DIALOG_STR=301, CTSS=2000, private trees from 4096) and outside
        /// any plausible private-tree count, so it can never collide with real content.
        /// </summary>
        public const ushort CHUNK_ID = 0xFFFE;
        private const string LABEL = "fso-pack-appearance-provenance";

        public static void Write(IffFile iff, PackObject obj)
        {
            JObject appearance;
            if (obj.CloneFromGuid != null)
            {
                appearance = new JObject { ["clone_from_guid"] = "0x" + obj.CloneFromGuid.Value.ToString("X8") };
            }
            else if (obj.Generated != null)
            {
                appearance = new JObject
                {
                    ["generated"] = new JObject
                    {
                        ["generator"] = obj.Generated.Generator,
                        ["params"] = SerializeParams(obj.Generated),
                    },
                };
            }
            else if (obj.Imported != null)
            {
                var imported = new JObject
                {
                    ["mesh"] = obj.Imported.Mesh,
                    ["height"] = obj.Imported.Height,
                    ["symmetric"] = obj.Imported.Symmetric,
                };
                if (obj.Imported.Provenance != null &&
                    (!string.IsNullOrEmpty(obj.Imported.Provenance.Source) ||
                     !string.IsNullOrEmpty(obj.Imported.Provenance.Model)))
                {
                    var prov = new JObject();
                    if (!string.IsNullOrEmpty(obj.Imported.Provenance.Source)) prov["source"] = obj.Imported.Provenance.Source;
                    if (!string.IsNullOrEmpty(obj.Imported.Provenance.Url)) prov["url"] = obj.Imported.Provenance.Url;
                    if (!string.IsNullOrEmpty(obj.Imported.Provenance.License)) prov["license"] = obj.Imported.Provenance.License;
                    if (!string.IsNullOrEmpty(obj.Imported.Provenance.Retrieved)) prov["retrieved"] = obj.Imported.Provenance.Retrieved;
                    if (!string.IsNullOrEmpty(obj.Imported.Provenance.Model)) prov["model"] = obj.Imported.Provenance.Model;
                    imported["provenance"] = prov;
                }
                appearance = new JObject { ["imported"] = imported };
            }
            else
            {
                return; // no appearance authored — nothing to record
            }

            var chunk = new STR
            {
                ChunkID = CHUNK_ID,
                ChunkLabel = LABEL,
                ChunkProcessed = true,
                ChunkParent = iff,
                ChunkType = "STR#",
            };
            chunk.LanguageSets[0].Strings = new[]
            {
                new STRItem { LanguageCode = 1, Value = appearance.ToString(Newtonsoft.Json.Formatting.None), Comment = "" },
            };
            iff.AddChunk(chunk);
        }

        /// <summary>Returns the recorded "appearance" JSON fragment, or null if this .iff
        /// predates provenance tracking (or the chunk is present but unreadable).</summary>
        public static JObject Read(IffFile iff)
        {
            var chunk = iff.Get<STR>(CHUNK_ID);
            if (chunk == null || chunk.ChunkLabel != LABEL) return null;

            var json = chunk.LanguageSets != null && chunk.LanguageSets.Length > 0
                ? chunk.LanguageSets[0]?.Strings?.FirstOrDefault()?.Value
                : null;
            if (string.IsNullOrEmpty(json)) return null;

            try { return JObject.Parse(json); }
            catch (Exception) { return null; } // corrupt/foreign data in our reserved id — treat as absent, don't throw
        }

        /// <summary>
        /// Reflects over whichever typed Params object matches Generated.Generator and
        /// produces the same snake_case JSON shape PackParser reads params from (see
        /// PackParser.ParseChairParams et al) — so what Write emits, PackParser can parse
        /// back unchanged.
        /// </summary>
        private static JObject SerializeParams(PackGeneratedAppearance g)
        {
            object p = g.Generator switch
            {
                "chair" => (object)g.ChairParams,
                "table" => g.TableParams,
                "bed" => g.BedParams,
                "lamp" => g.LampParams,
                "storage" => g.StorageParams,
                "sofa" => g.SofaParams,
                "primitives" => g.PartsParams,
                _ => null,
            };

            var result = new JObject();
            if (p == null) return result;

            foreach (var field in p.GetType().GetFields())
                result[ToSnakeCase(field.Name)] = ToJToken(field.GetValue(p));

            return result;
        }

        private static JToken ToJToken(object value)
        {
            if (value is ValueTuple<byte, byte, byte> c)
                return new JArray(c.Item1, c.Item2, c.Item3);
            return value == null ? JValue.CreateNull() : JToken.FromObject(value);
        }

        private static string ToSnakeCase(string pascalCase)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < pascalCase.Length; i++)
            {
                var ch = pascalCase[i];
                if (char.IsUpper(ch))
                {
                    if (i > 0) sb.Append('_');
                    sb.Append(char.ToLowerInvariant(ch));
                }
                else sb.Append(ch);
            }
            return sb.ToString();
        }
    }
}
