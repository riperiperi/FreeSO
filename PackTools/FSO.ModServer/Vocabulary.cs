using System.Collections.Generic;
using System.Linq;
using FSO.PackCompiler;

namespace FSO.ModServer
{
    /// <summary>
    /// Backs list_vocabulary. MCP-DESIGN.md §1 says to source this from
    /// simantics-vocabulary.md; we source it from FSO.PackCompiler.Names instead
    /// (in-process, same source the compiler's own validation uses) plus a primitive
    /// table mirrored from TreeCompiler.Primitive()'s switch — that pair is the actual
    /// ground truth for "what will this compiler accept", so an agent can't get an answer
    /// here that then fails at compile. See report to freeso-06 for the tradeoff.
    /// </summary>
    public static class Vocabulary
    {
        // Mirrors the opcode dispatch in TreeCompiler.Primitive() — the 18 primitives
        // the compiler currently emits. Kept in sync by hand; if this list and the
        // compiler's switch ever diverge, the compiler is authoritative.
        public static readonly Dictionary<string, string> Primitives = new()
        {
            { "expression", "0x02 — read/write/compare a variable; the assignment+branch workhorse" },
            { "sleep", "0x00 — wait N ticks (tick count held in a parameter)" },
            { "idle_for_input", "0x11 — idle N ticks, interruptible by queued interactions" },
            { "animate", "0x2C — play an animation on the sim" },
            { "play_sound", "0x17 — play a sound event on the object" },
            { "dialog_private", "0x24 — dialog using the object's private STR#301 strings" },
            { "dialog_global", "0x26 — dialog using global strings" },
            { "dialog_semiglobal", "0x27 — dialog using semiglobal strings" },
            { "goto_relative", "0x1B — route sim to a position relative to the stack object" },
            { "goto_routing_slot", "0x2D — route sim to a SLOT of the stack object" },
            { "push_interaction", "0x0D — queue an interaction on a sim" },
            { "find_best_object_for_function", "0x0E — find an object serving a function (eat, sit, ...) into the stack object" },
            { "set_motive_deltas", "0x1D — set a motive's per-tick change rate" },
            { "test_object_type", "0x20 — test whether an object variable holds a given GUID" },
            { "random_number", "0x08 — store a random number into a variable" },
            { "remove_object_instance", "0x12 — delete an object" },
            { "create_object_instance", "0x2A — spawn a new object by GUID" },
            { "change_suit_or_accessory", "0x06 — change the avatar's outfit/accessory" },
            { "show_string", "0x15 — debug string display" },
            { "set_balloon_headline", "0x29 — thought balloon / headline above the object" },
        };

        public static readonly IReadOnlyList<string> Kinds = new[]
        {
            "primitives", "scopes", "motives", "operators", "categories",
            "goto_relative_locations", "goto_relative_directions", "animation_sources",
            "slot_scopes", "push_priorities", "dialog_types", "dialog_icons",
            "create_object_positions", "suit_scopes", "balloon_groups",
        };

        public static object Get(string kind)
        {
            switch (kind)
            {
                case "primitives": return Primitives;
                case "scopes": return Names.Scopes;
                case "motives": return Names.Motives;
                case "operators": return Names.Operators;
                case "categories": return Names.Categories.ToList();
                case "goto_relative_locations": return Names.GotoRelativeLocations;
                case "goto_relative_directions": return Names.GotoRelativeDirections;
                case "animation_sources": return Names.AnimationSources;
                case "slot_scopes": return Names.SlotScopes;
                case "push_priorities": return Names.PushPriorities;
                case "dialog_types": return Names.DialogTypes;
                case "dialog_icons": return Names.DialogIcons;
                case "create_object_positions": return Names.CreateObjectPositions;
                case "suit_scopes": return Names.SuitScopes;
                case "balloon_groups": return Names.BalloonGroups;
                default: return null;
            }
        }
    }
}
