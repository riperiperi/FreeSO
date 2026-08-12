using System.Collections.Generic;
using System.Linq;
using FSO.PackCompiler.ArtGen;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FSO.PackCompiler
{
    /// <summary>
    /// Parses pack JSON (SCHEMA.md v0.1) into the pack model. Strict: unknown fields,
    /// bad types, bad enum names all become errors with their JSON path.
    /// </summary>
    public class PackParser
    {
        public Diagnostics D;

        public PackParser(Diagnostics d)
        {
            D = d;
        }

        public PackFile Parse(string json)
        {
            JObject root;
            try
            {
                root = JObject.Parse(json, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            }
            catch (JsonException e)
            {
                D.Error("$", "invalid JSON: " + e.Message);
                return null;
            }

            var pack = new PackFile();
            var o = new JsonObj(root, "$", D);

            pack.Schema = o.ReqString("schema");
            if (pack.Schema != null && pack.Schema != "fso-pack/0.1")
                D.Error("$.schema", "unsupported schema \"" + pack.Schema + "\" (expected \"fso-pack/0.1\")");

            pack.Engine = o.ReqString("engine");
            if (pack.Engine != null && pack.Engine != "tso")
                D.Error("$.engine", "unsupported engine \"" + pack.Engine + "\" (v0.1 supports \"tso\" only)");

            var meta = o.ReqObj("pack");
            if (meta != null)
            {
                pack.Meta.Id = meta.ReqString("id");
                pack.Meta.Name = meta.ReqString("name");
                pack.Meta.Author = meta.OptString("author");
                pack.Meta.Version = meta.OptString("version");
                pack.Meta.Description = meta.OptString("description");
                meta.Done();
            }

            var objects = o.OptArr("objects");
            if (objects == null || objects.Count == 0)
            {
                D.Error("$.objects", "pack must contain at least one object");
            }
            else
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    var obj = ParseObject(JsonObj.From(objects[i], "$.objects[" + i + "]", D));
                    if (obj != null) pack.Objects.Add(obj);
                }
            }

            o.Done();

            // GUID collisions within the pack
            var guids = new Dictionary<uint, string>();
            foreach (var obj in pack.Objects)
            {
                if (guids.TryGetValue(obj.Guid, out var other))
                    D.Error(obj.Path + ".guid", "GUID collision with object \"" + other + "\"");
                else guids[obj.Guid] = obj.Id;
            }

            return pack;
        }

        private PackObject ParseObject(JsonObj o)
        {
            var obj = new PackObject { Path = o.Path };

            obj.Id = o.ReqString("id");
            obj.Name = o.ReqString("name");

            var guid = o.OptGuid("guid");
            if (guid == null && !o.Has("guid")) D.Error(o.Path, "missing required field \"guid\"");
            obj.Guid = guid ?? 0;

            obj.Price = o.OptInt("price");
            if (obj.Price < 0 || obj.Price > ushort.MaxValue)
                D.Error(o.Path + ".price", "price out of range 0-65535");

            obj.Category = o.OptString("category");
            if (obj.Category != null && !Names.Categories.ContainsKey(obj.Category))
                D.Error(o.Path + ".category", "unknown category \"" + obj.Category + "\" (expected one of: " + string.Join(", ", Names.Categories.Keys) + ")");

            var tags = o.OptArr("tags");
            if (tags != null)
            {
                for (int i = 0; i < tags.Count; i++)
                {
                    if (tags[i].Type != JTokenType.String)
                    {
                        D.Error(o.Path + ".tags[" + i + "]", "expected a string");
                        continue;
                    }
                    obj.Tags.Add((string)tags[i]);
                }
            }

            var appearance = o.OptObj("appearance");
            if (appearance != null)
            {
                obj.CloneFromGuid = appearance.OptGuid("clone_from_guid");
                var generated = appearance.OptObj("generated");
                if (generated != null) obj.Generated = ParseGeneratedAppearance(generated);
                var imported = appearance.OptObj("imported");
                if (imported != null) obj.Imported = ParseImportedAppearance(imported);
                appearance.Done();

                var modes = (obj.CloneFromGuid != null ? 1 : 0) + (obj.Generated != null ? 1 : 0) + (obj.Imported != null ? 1 : 0);
                if (modes > 1)
                    D.Error(appearance.Path, "appearance.clone_from_guid, appearance.generated, and appearance.imported are mutually exclusive");
            }

            if (obj.CloneFromGuid == null && obj.Generated == null && obj.Imported == null)
                D.Error(o.Path, "object has no appearance (\"appearance.clone_from_guid\", \"appearance.generated\", or \"appearance.imported\") — it would be invisible in the client");

            var attrs = o.OptArr("attributes");
            if (attrs != null)
            {
                for (int i = 0; i < attrs.Count; i++)
                {
                    if (attrs[i].Type != JTokenType.String)
                    {
                        D.Error(o.Path + ".attributes[" + i + "]", "expected a string");
                        continue;
                    }
                    var name = (string)attrs[i];
                    if (obj.Attributes.Contains(name))
                        D.Error(o.Path + ".attributes[" + i + "]", "duplicate attribute \"" + name + "\"");
                    else obj.Attributes.Add(name);
                }
                if (obj.Attributes.Count > ushort.MaxValue) D.Error(o.Path + ".attributes", "too many attributes");
            }

            var strings = o.OptObj("strings");
            if (strings != null)
            {
                var dialog = strings.OptObj("dialog");
                if (dialog != null)
                {
                    foreach (var prop in dialog.Properties())
                    {
                        var path = dialog.Path + "." + prop.Name;
                        if (!int.TryParse(prop.Name, out var id) || id < 1 || id > 255)
                        {
                            D.Error(path, "dialog string ids must be integers 1-255 (0 = none)");
                            continue;
                        }
                        if (prop.Value.Type != JTokenType.String)
                        {
                            D.Error(path, "expected a string");
                            continue;
                        }
                        obj.DialogStrings[id] = (string)prop.Value;
                    }
                    dialog.MarkAllUsed();
                    dialog.Done();
                }
                strings.Done();
            }

            var interactions = o.OptArr("interactions");
            if (interactions != null)
            {
                for (int i = 0; i < interactions.Count; i++)
                {
                    var inter = ParseInteraction(JsonObj.From(interactions[i], o.Path + ".interactions[" + i + "]", D));
                    if (inter != null)
                    {
                        if (obj.Interactions.Any(x => x.Name == inter.Name))
                            D.Error(inter.Path + ".name", "duplicate interaction name \"" + inter.Name + "\"");
                        obj.Interactions.Add(inter);
                    }
                }
            }

            // Boilerplate every pack was hand-writing: an idle main loop, and an init that
            // zeroes declared attributes. They were byte-identical across every example, so
            // the agent was paying to retype a template one node at a time. Injected as JSON
            // before parsing so they go through exactly the same validation as authored trees
            // — a synthesized tree that skipped validation would be a silent trap.
            InjectDefaultTrees(o);

            var trees = o.OptObj("trees");
            if (trees == null)
            {
                D.Error(o.Path, "missing required field \"trees\"");
            }
            else
            {
                foreach (var prop in trees.Properties())
                {
                    var tree = ParseTree(prop.Name, JsonObj.From(prop.Value, trees.Path + "." + prop.Name, D));
                    obj.Trees.Add(tree);
                }
                trees.MarkAllUsed();
                trees.Done();
            }

            var entry = o.OptObj("entry_points");
            if (entry != null)
            {
                obj.EntryMain = entry.OptString("main");
                obj.EntryInit = entry.OptString("init");
                entry.Done();
            }

            o.Done();

            // referenced tree names must exist
            var treeNames = new HashSet<string>(obj.Trees.Select(t => t.Name));
            if (obj.EntryMain != null && !treeNames.Contains(obj.EntryMain))
                D.Error(o.Path + ".entry_points.main", "unresolved tree name \"" + obj.EntryMain + "\"");
            if (obj.EntryInit != null && !treeNames.Contains(obj.EntryInit))
                D.Error(o.Path + ".entry_points.init", "unresolved tree name \"" + obj.EntryInit + "\"");
            foreach (var inter in obj.Interactions)
            {
                if (inter.ActionTree != null && !treeNames.Contains(inter.ActionTree))
                    D.Error(inter.Path + ".action", "unresolved tree name \"" + inter.ActionTree + "\"");
                if (inter.TestTree != null && !treeNames.Contains(inter.TestTree))
                    D.Error(inter.Path + ".test", "unresolved tree name \"" + inter.TestTree + "\"");
            }

            return obj;
        }

        private static readonly HashSet<string> KnownGenerators = new HashSet<string> { "chair", "table", "bed", "lamp", "storage", "sofa", "primitives" };

        private PackGeneratedAppearance ParseGeneratedAppearance(JsonObj o)
        {
            var gen = new PackGeneratedAppearance();
            gen.Generator = o.ReqString("generator");
            var p = o.OptObj("params");
            JsonObj P() => p ?? JsonObj.From(new JObject(), o.Path + ".params", D);

            if (gen.Generator != null && !KnownGenerators.Contains(gen.Generator))
            {
                D.Error(o.Path + ".generator", "unknown generator \"" + gen.Generator + "\" (expected one of: " + string.Join(", ", KnownGenerators) + ")");
            }
            else if (gen.Generator == "chair")
            {
                gen.ChairParams = ParseChairParams(P());
            }
            else if (gen.Generator == "table")
            {
                gen.TableParams = ParseTableParams(P());
            }
            else if (gen.Generator == "bed")
            {
                gen.BedParams = ParseBedParams(P());
            }
            else if (gen.Generator == "lamp")
            {
                gen.LampParams = ParseLampParams(P());
            }
            else if (gen.Generator == "storage")
            {
                gen.StorageParams = ParseStorageParams(P());
            }
            else if (gen.Generator == "sofa")
            {
                gen.SofaParams = ParseSofaParams(P());
            }
            else if (gen.Generator == "primitives")
            {
                gen.PartsParams = ParsePartsParams(P());
            }

            o.Done();
            return gen;
        }

        private PackImportedAppearance ParseImportedAppearance(JsonObj o)
        {
            var imp = new PackImportedAppearance();
            imp.Mesh = o.ReqString("mesh");
            imp.Height = o.OptDouble("height", 1.0);
            imp.Symmetric = o.OptBool("symmetric", false);
            if (imp.Height <= 0)
                D.Error(o.Path + ".height", "height must be > 0");

            var prov = o.OptObj("provenance");
            if (prov != null)
            {
                imp.Provenance.Source = prov.OptString("source");
                imp.Provenance.Url = prov.OptString("url");
                imp.Provenance.License = prov.OptString("license");
                imp.Provenance.Retrieved = prov.OptString("retrieved");
                imp.Provenance.Model = prov.OptString("model");
                prov.Done();
            }

            o.Done();
            return imp;
        }

        private ChairGenerator.Params ParseChairParams(JsonObj p)
        {
            var d = new ChairGenerator.Params(); // defaults
            var result = new ChairGenerator.Params
            {
                SeatWidth = p.OptDouble("seat_width", d.SeatWidth),
                SeatDepth = p.OptDouble("seat_depth", d.SeatDepth),
                SeatHeight = p.OptDouble("seat_height", d.SeatHeight),
                SeatThickness = p.OptDouble("seat_thickness", d.SeatThickness),
                BackHeight = p.OptDouble("back_height", d.BackHeight),
                BackThickness = p.OptDouble("back_thickness", d.BackThickness),
                BackAngleDeg = p.OptDouble("back_angle_deg", d.BackAngleDeg),
                LegTopWidth = p.OptDouble("leg_top_width", d.LegTopWidth),
                LegBottomWidth = p.OptDouble("leg_bottom_width", d.LegBottomWidth),
                Arms = p.OptBool("arms", d.Arms),
                ArmHeight = p.OptDouble("arm_height", d.ArmHeight),
                ArmThickness = p.OptDouble("arm_thickness", d.ArmThickness),
                WoodColor = ParseColor(p, "wood_color", d.WoodColor),
                UpholsteryColor = ParseColor(p, "upholstery_color", d.UpholsteryColor),
            };

            // Physically nonsensical geometry (<= 0) produces a degenerate/garbage mesh with
            // no error from the renderer, so the compiler must catch it here instead.
            void Positive(double v, string field)
            {
                if (v <= 0) D.Error(p.Path + "." + field, "must be > 0");
            }
            Positive(result.SeatWidth, "seat_width");
            Positive(result.SeatDepth, "seat_depth");
            Positive(result.SeatHeight, "seat_height");
            Positive(result.SeatThickness, "seat_thickness");
            Positive(result.BackHeight, "back_height");
            Positive(result.BackThickness, "back_thickness");
            Positive(result.LegTopWidth, "leg_top_width");
            Positive(result.LegBottomWidth, "leg_bottom_width");
            if (result.Arms)
            {
                Positive(result.ArmHeight, "arm_height");
                Positive(result.ArmThickness, "arm_thickness");
            }

            p.Done();
            return result;
        }

        private static readonly Dictionary<string, TableGenerator.TopShapeType> TopShapes = new Dictionary<string, TableGenerator.TopShapeType>
        {
            ["rectangular"] = TableGenerator.TopShapeType.Rectangular,
            ["round"] = TableGenerator.TopShapeType.Round,
        };
        private static readonly Dictionary<string, TableGenerator.BaseStyleType> BaseStyles = new Dictionary<string, TableGenerator.BaseStyleType>
        {
            ["four_leg"] = TableGenerator.BaseStyleType.FourLeg,
            ["pedestal"] = TableGenerator.BaseStyleType.Pedestal,
            ["tripod"] = TableGenerator.BaseStyleType.Tripod,
        };
        private static readonly Dictionary<string, StorageGenerator.KindType> StorageKinds = new Dictionary<string, StorageGenerator.KindType>
        {
            ["bookshelf"] = StorageGenerator.KindType.Bookshelf,
            ["dresser"] = StorageGenerator.KindType.Dresser,
        };

        private T ParseEnum<T>(JsonObj p, string name, Dictionary<string, T> options, T def) where T : struct
        {
            var s = p.OptString(name);
            if (s == null) return def;
            if (options.TryGetValue(s, out var v)) return v;
            D.Error(p.Path + "." + name, "unknown value \"" + s + "\" (expected one of: " + string.Join(", ", options.Keys) + ")");
            return def;
        }

        private TableGenerator.Params ParseTableParams(JsonObj p)
        {
            var d = new TableGenerator.Params();
            var result = new TableGenerator.Params
            {
                TopShape = ParseEnum(p, "top_shape", TopShapes, d.TopShape),
                BaseStyle = ParseEnum(p, "base_style", BaseStyles, d.BaseStyle),
                TopWidth = p.OptDouble("top_width", d.TopWidth),
                TopDepth = p.OptDouble("top_depth", d.TopDepth),
                TopDiameter = p.OptDouble("top_diameter", d.TopDiameter),
                TopThickness = p.OptDouble("top_thickness", d.TopThickness),
                Height = p.OptDouble("height", d.Height),
                LegTopWidth = p.OptDouble("leg_top_width", d.LegTopWidth),
                LegBottomWidth = p.OptDouble("leg_bottom_width", d.LegBottomWidth),
                PedestalTopRadius = p.OptDouble("pedestal_top_radius", d.PedestalTopRadius),
                PedestalBaseRadius = p.OptDouble("pedestal_base_radius", d.PedestalBaseRadius),
                TripodTopRadius = p.OptDouble("tripod_top_radius", d.TripodTopRadius),
                TripodBottomRadius = p.OptDouble("tripod_bottom_radius", d.TripodBottomRadius),
                WoodColor = ParseColor(p, "wood_color", d.WoodColor),
                TopColor = ParseColor(p, "top_color", d.TopColor),
            };

            void Positive(double v, string field) { if (v <= 0) D.Error(p.Path + "." + field, "must be > 0"); }
            Positive(result.TopThickness, "top_thickness");
            Positive(result.Height, "height");
            if (result.TopShape == TableGenerator.TopShapeType.Rectangular)
            {
                Positive(result.TopWidth, "top_width");
                Positive(result.TopDepth, "top_depth");
            }
            else
            {
                Positive(result.TopDiameter, "top_diameter");
            }
            if (result.BaseStyle == TableGenerator.BaseStyleType.FourLeg)
            {
                Positive(result.LegTopWidth, "leg_top_width");
                Positive(result.LegBottomWidth, "leg_bottom_width");
            }
            else if (result.BaseStyle == TableGenerator.BaseStyleType.Pedestal)
            {
                Positive(result.PedestalTopRadius, "pedestal_top_radius");
                Positive(result.PedestalBaseRadius, "pedestal_base_radius");
            }
            else // Tripod
            {
                Positive(result.LegTopWidth, "leg_top_width");
                Positive(result.LegBottomWidth, "leg_bottom_width");
                Positive(result.TripodTopRadius, "tripod_top_radius");
                Positive(result.TripodBottomRadius, "tripod_bottom_radius");
            }

            p.Done();
            return result;
        }

        private BedGenerator.Params ParseBedParams(JsonObj p)
        {
            var d = new BedGenerator.Params();
            var result = new BedGenerator.Params
            {
                MattressWidth = p.OptDouble("mattress_width", d.MattressWidth),
                MattressDepth = p.OptDouble("mattress_depth", d.MattressDepth),
                MattressThickness = p.OptDouble("mattress_thickness", d.MattressThickness),
                FrameThickness = p.OptDouble("frame_thickness", d.FrameThickness),
                LegHeight = p.OptDouble("leg_height", d.LegHeight),
                LegWidth = p.OptDouble("leg_width", d.LegWidth),
                HeadboardHeight = p.OptDouble("headboard_height", d.HeadboardHeight),
                HeadboardThickness = p.OptDouble("headboard_thickness", d.HeadboardThickness),
                Footboard = p.OptBool("footboard", d.Footboard),
                FootboardHeight = p.OptDouble("footboard_height", d.FootboardHeight),
                FrameColor = ParseColor(p, "frame_color", d.FrameColor),
                MattressColor = ParseColor(p, "mattress_color", d.MattressColor),
                HeadboardColor = ParseColor(p, "headboard_color", d.HeadboardColor),
            };

            void Positive(double v, string field) { if (v <= 0) D.Error(p.Path + "." + field, "must be > 0"); }
            Positive(result.MattressWidth, "mattress_width");
            Positive(result.MattressDepth, "mattress_depth");
            Positive(result.MattressThickness, "mattress_thickness");
            Positive(result.FrameThickness, "frame_thickness");
            Positive(result.LegHeight, "leg_height");
            Positive(result.LegWidth, "leg_width");
            Positive(result.HeadboardHeight, "headboard_height");
            Positive(result.HeadboardThickness, "headboard_thickness");
            if (result.Footboard) Positive(result.FootboardHeight, "footboard_height");

            p.Done();
            return result;
        }

        private LampGenerator.Params ParseLampParams(JsonObj p)
        {
            var d = new LampGenerator.Params();
            var result = new LampGenerator.Params
            {
                BaseRadius = p.OptDouble("base_radius", d.BaseRadius),
                BaseHeight = p.OptDouble("base_height", d.BaseHeight),
                StemRadius = p.OptDouble("stem_radius", d.StemRadius),
                StemHeight = p.OptDouble("stem_height", d.StemHeight),
                ShadeBottomRadius = p.OptDouble("shade_bottom_radius", d.ShadeBottomRadius),
                ShadeTopRadius = p.OptDouble("shade_top_radius", d.ShadeTopRadius),
                ShadeHeight = p.OptDouble("shade_height", d.ShadeHeight),
                BaseColor = ParseColor(p, "base_color", d.BaseColor),
                ShadeColor = ParseColor(p, "shade_color", d.ShadeColor),
            };

            void Positive(double v, string field) { if (v <= 0) D.Error(p.Path + "." + field, "must be > 0"); }
            Positive(result.BaseRadius, "base_radius");
            Positive(result.BaseHeight, "base_height");
            Positive(result.StemRadius, "stem_radius");
            Positive(result.StemHeight, "stem_height");
            Positive(result.ShadeBottomRadius, "shade_bottom_radius");
            Positive(result.ShadeTopRadius, "shade_top_radius");
            Positive(result.ShadeHeight, "shade_height");

            p.Done();
            return result;
        }

        private StorageGenerator.Params ParseStorageParams(JsonObj p)
        {
            var d = new StorageGenerator.Params();
            var result = new StorageGenerator.Params
            {
                Kind = ParseEnum(p, "kind", StorageKinds, d.Kind),
                Width = p.OptDouble("width", d.Width),
                Depth = p.OptDouble("depth", d.Depth),
                Height = p.OptDouble("height", d.Height),
                Sections = p.OptInt("sections", d.Sections),
                PanelThickness = p.OptDouble("panel_thickness", d.PanelThickness),
                LegHeight = p.OptDouble("leg_height", d.LegHeight),
                CarcassColor = ParseColor(p, "carcass_color", d.CarcassColor),
                AccentColor = ParseColor(p, "accent_color", d.AccentColor),
            };

            void Positive(double v, string field) { if (v <= 0) D.Error(p.Path + "." + field, "must be > 0"); }
            Positive(result.Width, "width");
            Positive(result.Depth, "depth");
            Positive(result.Height, "height");
            Positive(result.PanelThickness, "panel_thickness");
            if (result.Sections <= 0) D.Error(p.Path + ".sections", "must be > 0");
            if (result.LegHeight < 0) D.Error(p.Path + ".leg_height", "must be >= 0"); // 0 is valid (no feet)

            p.Done();
            return result;
        }

        private SofaGenerator.Params ParseSofaParams(JsonObj p)
        {
            var d = new SofaGenerator.Params();
            var result = new SofaGenerator.Params
            {
                Width = p.OptDouble("width", d.Width),
                SeatDepth = p.OptDouble("seat_depth", d.SeatDepth),
                SeatHeight = p.OptDouble("seat_height", d.SeatHeight),
                SeatThickness = p.OptDouble("seat_thickness", d.SeatThickness),
                CushionCount = p.OptInt("cushion_count", d.CushionCount),
                BackHeight = p.OptDouble("back_height", d.BackHeight),
                BackThickness = p.OptDouble("back_thickness", d.BackThickness),
                BackAngleDeg = p.OptDouble("back_angle_deg", d.BackAngleDeg),
                BackCapHeight = p.OptDouble("back_cap_height", d.BackCapHeight),
                ArmWidth = p.OptDouble("arm_width", d.ArmWidth),
                ArmHeight = p.OptDouble("arm_height", d.ArmHeight),
                ArmCapHeight = p.OptDouble("arm_cap_height", d.ArmCapHeight),
                LegHeight = p.OptDouble("leg_height", d.LegHeight),
                LegWidth = p.OptDouble("leg_width", d.LegWidth),
                WoodColor = ParseColor(p, "wood_color", d.WoodColor),
                UpholsteryColor = ParseColor(p, "upholstery_color", d.UpholsteryColor),
                SeamColor = ParseColor(p, "seam_color", d.SeamColor),
            };

            void Positive(double v, string field) { if (v <= 0) D.Error(p.Path + "." + field, "must be > 0"); }
            Positive(result.Width, "width");
            Positive(result.SeatDepth, "seat_depth");
            Positive(result.SeatHeight, "seat_height");
            Positive(result.SeatThickness, "seat_thickness");
            Positive(result.BackHeight, "back_height");
            Positive(result.BackThickness, "back_thickness");
            Positive(result.ArmWidth, "arm_width");
            Positive(result.ArmHeight, "arm_height");
            Positive(result.LegWidth, "leg_width");
            if (result.CushionCount <= 0) D.Error(p.Path + ".cushion_count", "must be > 0");
            if (result.LegHeight < 0) D.Error(p.Path + ".leg_height", "must be >= 0"); // 0 is valid (no feet)
            if (result.Width <= 2 * result.ArmWidth) D.Error(p.Path + ".width", "must be greater than 2x arm_width, or the arms overlap with no seat left between them");
            if (result.BackCapHeight < 0 || result.BackCapHeight >= result.BackHeight) D.Error(p.Path + ".back_cap_height", "must be >= 0 and less than back_height");
            if (result.ArmCapHeight < 0 || result.ArmCapHeight >= result.ArmHeight) D.Error(p.Path + ".arm_cap_height", "must be >= 0 and less than arm_height");

            p.Done();
            return result;
        }

        private static readonly HashSet<string> PartTypes = new HashSet<string> { "box", "cylinder", "cone", "sphere" };

        private PartsGenerator.Params ParsePartsParams(JsonObj p)
        {
            var result = new PartsGenerator.Params { Symmetric = p.OptBool("symmetric", false) };

            var partsArr = p.OptArr("parts");
            if (partsArr == null || partsArr.Count == 0)
            {
                D.Error(p.Path + ".parts", "at least one part is required");
            }
            else
            {
                for (int i = 0; i < partsArr.Count; i++)
                {
                    var po = JsonObj.From(partsArr[i], p.Path + ".parts[" + i + "]", D);
                    var part = new PartsGenerator.Part
                    {
                        Type = po.ReqString("type"),
                        Pos = ParseVec3(po, "pos", new Vec3(0, 0, 0)),
                        Size = ParseVec3(po, "size", new Vec3(0, 0, 0)),
                        Color = ParseColor(po, "color", (128, 128, 128)),
                    };

                    if (part.Type != null && !PartTypes.Contains(part.Type))
                        D.Error(po.Path + ".type", "unknown part type \"" + part.Type + "\" (expected one of: " + string.Join(", ", PartTypes) + ")");

                    // "cone" only uses size.x/size.y — see PartsGenerator.Build's matching exemption.
                    var checkZ = part.Type != "cone";
                    if (part.Size.X <= 0) D.Error(po.Path + ".size[0]", "must be > 0");
                    if (part.Size.Y <= 0) D.Error(po.Path + ".size[1]", "must be > 0");
                    if (checkZ && part.Size.Z <= 0) D.Error(po.Path + ".size[2]", "must be > 0");

                    po.Done();
                    result.Parts.Add(part);
                }
            }

            p.Done();
            return result;
        }

        private Vec3 ParseVec3(JsonObj p, string name, Vec3 def)
        {
            var arr = p.OptArr(name);
            if (arr == null) return def;
            if (arr.Count != 3)
            {
                D.Error(p.Path + "." + name, "expected an array of 3 numbers [x, y, z]");
                return def;
            }
            double At(int i)
            {
                var t = arr[i];
                if (t.Type == JTokenType.Float || t.Type == JTokenType.Integer) return (double)t;
                D.Error(p.Path + "." + name + "[" + i + "]", "expected a number");
                return 0;
            }
            return new Vec3(At(0), At(1), At(2));
        }

        private (byte, byte, byte) ParseColor(JsonObj p, string name, (byte, byte, byte) def)
        {
            var arr = p.OptArr(name);
            if (arr == null) return def;
            if (arr.Count != 3)
            {
                D.Error(p.Path + "." + name, "expected an array of 3 integers 0-255 [r, g, b]");
                return def;
            }
            var vals = new byte[3];
            for (int i = 0; i < 3; i++)
            {
                if (arr[i].Type != JTokenType.Integer || (int)arr[i] < 0 || (int)arr[i] > 255)
                {
                    D.Error(p.Path + "." + name + "[" + i + "]", "expected an integer 0-255");
                    continue;
                }
                vals[i] = (byte)(int)arr[i];
            }
            return (vals[0], vals[1], vals[2]);
        }

        private PackInteraction ParseInteraction(JsonObj o)
        {
            var inter = new PackInteraction { Path = o.Path };
            inter.Name = o.ReqString("name");
            inter.ActionTree = o.ReqString("action");
            inter.TestTree = o.OptString("test");

            var allow = o.OptObj("allow");
            if (allow != null)
            {
                inter.HasAllow = true;
                inter.AllowVisitors = allow.OptBool("visitors");
                inter.AllowOwner = allow.OptBool("owner");
                inter.AllowRoommates = allow.OptBool("roommates");
                inter.AllowFriends = allow.OptBool("friends");
                inter.AllowGhosts = allow.OptBool("ghosts");
                inter.AllowCSRs = allow.OptBool("csrs");
                inter.AllowCats = allow.OptBool("cats");
                inter.AllowDogs = allow.OptBool("dogs");
                allow.Done();
            }

            var flags = o.OptObj("flags");
            if (flags != null)
            {
                inter.FlagDebug = flags.OptBool("debug");
                inter.FlagAutoFirst = flags.OptBool("auto_first");
                inter.FlagRunImmediately = flags.OptBool("run_immediately");
                inter.FlagMustRun = flags.OptBool("must_run");
                inter.FlagAllowConsecutive = flags.OptBool("allow_consecutive");
                inter.FlagJoinable = flags.OptBool("joinable");
                inter.FlagLeapfrog = flags.OptBool("leapfrog");
                inter.FlagCarrying = flags.OptBool("carrying");
                inter.FlagRepair = flags.OptBool("repair");
                inter.FlagAlwaysCheck = flags.OptBool("always_check");
                inter.FlagWhenDead = flags.OptBool("when_dead");
                flags.Done();
            }

            var autonomy = o.OptObj("autonomy");
            if (autonomy != null)
            {
                var motives = autonomy.OptObj("advertised_motives");
                if (motives != null)
                {
                    foreach (var prop in motives.Properties())
                    {
                        var path = motives.Path + "." + prop.Name;
                        if (!Names.Motives.TryGetValue(prop.Name, out var motiveIdx))
                        {
                            D.Error(path, "unknown motive \"" + prop.Name + "\" (expected one of: " + Names.List(Names.Motives) + ")");
                            continue;
                        }
                        if (prop.Value.Type != JTokenType.Integer)
                        {
                            D.Error(path, "expected an integer");
                            continue;
                        }
                        var v = (int)prop.Value;
                        if (v < short.MinValue || v > short.MaxValue)
                        {
                            D.Error(path, "value out of int16 range");
                            continue;
                        }
                        inter.AdvertisedMotives[motiveIdx] = (short)v;
                    }
                    motives.MarkAllUsed();
                    motives.Done();
                }

                var threshold = autonomy.OptInt("threshold");
                if (threshold < 0) D.Error(autonomy.Path + ".threshold", "threshold must be >= 0");
                else inter.AutonomyThreshold = (uint)threshold;

                var atten = autonomy.Opt("attenuation");
                if (atten != null)
                {
                    if (atten.Type == JTokenType.String)
                    {
                        var name = (string)atten;
                        var codes = new Dictionary<string, uint> { { "none", 1 }, { "low", 2 }, { "medium", 3 }, { "high", 4 } };
                        if (codes.TryGetValue(name, out var code))
                        {
                            inter.AttenuationCode = code;
                            inter.AttenuationValue = FSO.Files.Formats.IFF.Chunks.TTAB.AttenuationValues[code];
                        }
                        else D.Error(autonomy.Path + ".attenuation", "unknown attenuation \"" + name + "\" (expected none, low, medium, high, or a number)");
                    }
                    else if (atten.Type == JTokenType.Float || atten.Type == JTokenType.Integer)
                    {
                        inter.AttenuationCode = 0; // custom
                        inter.AttenuationValue = (float)atten;
                    }
                    else D.Error(autonomy.Path + ".attenuation", "expected a name or number");
                }

                inter.JoiningIndex = autonomy.OptInt("joining_index");
                autonomy.Done();
            }

            o.Done();
            return inter;
        }

        private const string DefaultMainTree = "main_loop";
        private const string DefaultInitTree = "init";

        /// <summary>
        /// Adds a standard idle main loop when the object declares no main entry point, and
        /// an init tree zeroing declared attributes when it declares none. Only ever ADDS:
        /// an object that names its own main/init, or defines a tree of the same name, is
        /// left untouched — defaults must never silently override authored behaviour.
        /// </summary>
        private static void InjectDefaultTrees(JsonObj o)
        {
            var raw = o.Raw;
            if (raw["trees"] is not JObject trees) return;

            var entry = raw["entry_points"] as JObject;
            var hasMain = !string.IsNullOrEmpty((string)entry?["main"]);
            var hasInit = !string.IsNullOrEmpty((string)entry?["init"]);

            if (!hasMain && trees[DefaultMainTree] == null)
            {
                trees[DefaultMainTree] = JObject.Parse(@"{
                    ""args"": [], ""locals"": [],
                    ""nodes"": [ { ""id"": ""idle"", ""prim"": ""idle_for_input"",
                                   ""ticks_param"": 0, ""allow_push"": true,
                                   ""then"": ""idle"", ""else"": ""idle"" } ] }");
                entry ??= (JObject)(raw["entry_points"] = new JObject());
                entry["main"] = DefaultMainTree;
            }

            // Only worth generating when there is something to zero.
            var attributes = raw["attributes"] as JArray;
            if (!hasInit && trees[DefaultInitTree] == null && attributes is { Count: > 0 })
            {
                var nodes = new JArray();
                for (int i = 0; i < attributes.Count; i++)
                {
                    var name = (string)attributes[i];
                    if (string.IsNullOrEmpty(name)) continue;
                    var last = i == attributes.Count - 1;
                    nodes.Add(JObject.Parse($@"{{
                        ""id"": ""zero_{name}"",
                        ""prim"": ""expression"",
                        ""lhs"": {{ ""scope"": ""my_attributes"", ""name"": ""{name}"" }},
                        ""op"": ""="",
                        ""rhs"": {{ ""scope"": ""literal"", ""value"": 0 }},
                        ""then"": {(last ? @"""return true""" : $@"""zero_{(string)attributes[i + 1]}""")},
                        ""else"": ""error"" }}"));
                }
                if (nodes.Count > 0)
                {
                    trees[DefaultInitTree] = new JObject { ["args"] = new JArray(), ["locals"] = new JArray(), ["nodes"] = nodes };
                    entry ??= (JObject)(raw["entry_points"] = new JObject());
                    entry["init"] = DefaultInitTree;
                }
            }
        }

        private PackTree ParseTree(string name, JsonObj o)
        {
            var tree = new PackTree { Name = name, Path = o.Path };

            foreach (var field in new[] { "args", "locals" })
            {
                var arr = o.OptArr(field);
                var list = (field == "args") ? tree.Args : tree.Locals;
                if (arr == null) continue;
                for (int i = 0; i < arr.Count; i++)
                {
                    if (arr[i].Type != JTokenType.String)
                    {
                        D.Error(o.Path + "." + field + "[" + i + "]", "expected a string");
                        continue;
                    }
                    var n = (string)arr[i];
                    if (list.Contains(n)) D.Error(o.Path + "." + field + "[" + i + "]", "duplicate name \"" + n + "\"");
                    else list.Add(n);
                }
            }

            if (tree.Locals.Count > 255)
                D.Error(o.Path + ".locals", "too many locals (" + tree.Locals.Count + " > 255; TSO BHAV stores locals as a byte)");
            if (tree.Args.Count > 255)
                D.Error(o.Path + ".args", "too many args (" + tree.Args.Count + " > 255)");

            var nodes = o.OptArr("nodes");
            if (nodes == null || nodes.Count == 0)
            {
                D.Error(o.Path, "tree must have a non-empty \"nodes\" array");
            }
            else
            {
                if (nodes.Count > 253)
                    D.Error(o.Path + ".nodes", "tree has " + nodes.Count + " nodes; max is 253 (pointers 253-255 are reserved)");
                for (int i = 0; i < nodes.Count; i++)
                {
                    var no = JsonObj.From(nodes[i], o.Path + ".nodes[" + i + "]", D);
                    var node = new PackNode { Path = no.Path, Fields = no };
                    node.Id = no.ReqString("id");
                    if (no.Has("call"))
                    {
                        node.Call = no.OptString("call");
                    }
                    else
                    {
                        node.Prim = no.ReqString("prim");
                    }
                    node.Then = no.ReqString("then");
                    node.Else = no.ReqString("else");
                    if (node.Id != null && tree.Nodes.Any(x => x.Id == node.Id))
                        D.Error(no.Path + ".id", "duplicate node id \"" + node.Id + "\"");
                    tree.Nodes.Add(node);
                    // remaining fields consumed by the operand compiler; Done() called there.
                }
            }

            o.Done();
            return tree;
        }
    }
}
