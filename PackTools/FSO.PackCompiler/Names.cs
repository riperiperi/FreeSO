using System.Collections.Generic;

namespace FSO.PackCompiler
{
    /// <summary>
    /// Name tables mapping schema names to engine byte values.
    /// Sources: tso.simantics/Engine/Scopes/VMVariableScope.cs, Model/VMMotive.cs,
    /// Primitives/*.cs enums. See PackTools/simantics-vocabulary.md.
    /// </summary>
    public static class Names
    {
        // VMVariableScope, snake_case of member names, plus schema alias my_attributes.
        public static readonly Dictionary<string, byte> Scopes = new Dictionary<string, byte>
        {
            { "my_object_attributes", 0 },
            { "my_attributes", 0 }, // schema alias
            { "stack_object_attributes", 1 },
            { "target_object_attributes", 2 },
            { "my_object", 3 },
            { "stack_object", 4 },
            { "target_object", 5 },
            { "global", 6 },
            { "literal", 7 },
            { "temps", 8 },
            { "parameters", 9 },
            { "stack_object_id", 10 },
            { "temp_by_temp", 11 },
            { "tree_ad_range", 12 },
            { "stack_object_temp", 13 },
            { "my_motives", 14 },
            { "stack_object_motives", 15 },
            { "stack_object_slot", 16 },
            { "stack_object_motive_by_temp", 17 },
            { "my_person_data", 18 },
            { "stack_object_person_data", 19 },
            { "my_slot", 20 },
            { "stack_object_definition", 21 },
            { "stack_object_attribute_by_parameter", 22 },
            { "room_by_temp_0", 23 },
            { "neighbor_in_stack_object", 24 },
            { "local", 25 },
            { "tuning", 26 },
            { "dyn_sprite_flag_for_temp_of_stack_object", 27 },
            { "tree_ad_personality_var", 28 },
            { "tree_ad_min", 29 },
            { "my_person_data_by_temp", 30 },
            { "stack_object_person_data_by_temp", 31 },
            { "neighbor_person_data", 32 },
            { "job_data", 33 },
            { "neighborhood_data", 34 },
            { "stack_object_function", 35 },
            { "my_type_attr", 36 },
            { "stack_object_type_attr", 37 },
            { "neighbors_object_definition", 38 },
            { "local_by_temp", 40 },
            { "stack_object_attribute_by_temp", 41 },
            { "temp_xl", 42 },
            { "city_time", 43 },
            { "tso_standard_time", 44 },
            { "game_time", 45 },
            { "my_list", 46 },
            { "stack_object_list", 47 },
            { "money_over_head_32_bit", 48 },
            { "my_lead_tile_attribute", 49 },
            { "stack_object_lead_tile_attribute", 50 },
            { "my_lead_tile", 51 },
            { "stack_object_lead_tile", 52 },
            { "stack_object_master_def", 53 },
            { "feature_enable_level", 54 },
            { "my_avatar_id", 59 },
        };

        // Scopes whose data value may be given by name, and which pack table resolves it.
        public const byte SCOPE_LITERAL = 7;
        public const byte SCOPE_TEMPS = 8;
        public const byte SCOPE_PARAMETERS = 9;
        public const byte SCOPE_MY_ATTRIBUTES = 0;
        public const byte SCOPE_STACK_OBJECT_ATTRIBUTES = 1;
        public const byte SCOPE_MY_MOTIVES = 14;
        public const byte SCOPE_STACK_OBJECT_MOTIVES = 15;
        public const byte SCOPE_LOCAL = 25;
        public const byte SCOPE_TUNING = 26;

        // VMMotive
        public static readonly Dictionary<string, byte> Motives = new Dictionary<string, byte>
        {
            { "happy_life", 0 },
            { "happy_week", 1 },
            { "happy_day", 2 },
            { "mood", 3 },
            { "unused_physical", 4 },
            { "energy", 5 },
            { "comfort", 6 },
            { "hunger", 7 },
            { "hygiene", 8 },
            { "bladder", 9 },
            { "unused_mental", 10 },
            { "sleep_state", 11 },
            { "unused_stress", 12 },
            { "room", 13 },
            { "social", 14 },
            { "fun", 15 },
        };

        // VMExpressionOperator, names per SCHEMA.md.
        public static readonly Dictionary<string, byte> Operators = new Dictionary<string, byte>
        {
            { ">", 0 },
            { "<", 1 },
            { "==", 2 },
            { "+=", 3 },
            { "-=", 4 },
            { "=", 5 },
            { "*=", 6 },
            { "/=", 7 },
            { "is_flag_set", 8 },
            { "set_flag", 9 },
            { "clear_flag", 10 },
            { "inc_and_less", 11 },
            { "%=", 12 },
            { ">=", 14 },
            { "<=", 15 },
            { "!=", 16 },
            { "dec_and_greater", 17 },
            { "push", 18 },
            { "pop", 19 },
        };

        // Operators that write to their LHS (mutations). Comparisons branch instead.
        public static readonly HashSet<byte> MutationOperators = new HashSet<byte>
        {
            3, 4, 5, 6, 7, 9, 10, 11, 12, 17, 18, 19
        };

        // VMGotoRelativeLocation
        public static readonly Dictionary<string, sbyte> GotoRelativeLocations = new Dictionary<string, sbyte>
        {
            { "on_top_of", -2 },
            { "anywhere_near", -1 },
            { "in_front_of", 0 },
            { "front_and_to_right_of", 1 },
            { "to_the_right_of", 2 },
            { "behind_and_to_right_of", 3 },
            { "behind", 4 },
            { "behind_and_to_the_left_of", 5 },
            { "to_the_left_of", 6 },
            { "in_front_and_to_the_left_of", 7 },
        };

        // VMGotoRelativeDirection
        public static readonly Dictionary<string, sbyte> GotoRelativeDirections = new Dictionary<string, sbyte>
        {
            { "facing", -2 },
            { "any_direction", -1 },
            { "same_direction", 0 },
            { "forty_five_degrees_right_of_same_direction", 1 },
            { "ninety_degrees_right_of_same_direction", 2 },
            { "forty_five_degrees_left_of_opposing_direction", 3 },
            { "opposing_direction", 4 },
            { "forty_five_degrees_right_of_opposing_direction", 5 },
            { "ninety_degrees_right_of_opposing_direction", 6 },
            { "forty_five_degrees_left_of_same_direction", 7 },
        };

        // VMAnimationScope
        public static readonly Dictionary<string, byte> AnimationSources = new Dictionary<string, byte>
        {
            { "object", 0 },
            { "global", 1 },
            { "person_stock", 2 },
            { "misc", 3 },
        };

        // VMSlotScope
        public static readonly Dictionary<string, ushort> SlotScopes = new Dictionary<string, ushort>
        {
            { "stack_variable", 0 },
            { "literal", 1 },
            { "global", 2 },
        };

        // VMPushPriority
        public static readonly Dictionary<string, byte> PushPriorities = new Dictionary<string, byte>
        {
            { "inherited", 0 },
            { "maximum", 1 },
            { "autonomous", 2 },
            { "user_driven", 3 },
            { "parent_idle", 4 },
            { "parent_exit", 5 },
            { "idle", 6 },
        };

        // VMDialogType (TSO values)
        public static readonly Dictionary<string, byte> DialogTypes = new Dictionary<string, byte>
        {
            { "message", 0 },
            { "yes_no", 1 },
            { "yes_no_cancel", 2 },
            { "text_entry", 3 },
            { "numeric_entry", 5 },
            { "image_mapped", 6 },
            { "custom", 7 },
            { "user_bitmap", 8 },
        };

        // Dialog icon type, packed into VMDialogFlags bits 1-3.
        public static readonly Dictionary<string, byte> DialogIcons = new Dictionary<string, byte>
        {
            { "auto", 0 },
            { "none", 1 },
            { "neighbour", 2 },
            { "indexed", 3 },
            { "named", 4 },
        };

        // VMCreateObjectPosition
        public static readonly Dictionary<string, byte> CreateObjectPositions = new Dictionary<string, byte>
        {
            { "in_front_of_me", 0 },
            { "on_top_of_me", 1 },
            { "in_my_hand", 2 },
            { "in_front_of_stack_object", 3 },
            { "in_slot_0_of_stack_object", 4 },
            { "underneath_me", 5 },
            { "out_of_world", 6 },
            { "below_object_in_stack_param_0", 7 },
            { "below_object_in_local", 8 },
            { "next_to_me_in_direction_of_local", 9 },
        };

        // VMSuitScope
        public static readonly Dictionary<string, byte> SuitScopes = new Dictionary<string, byte>
        {
            { "global", 0 },
            { "person", 1 },
            { "object", 2 },
        };

        // VMSetBalloonHeadlineOperandGroup
        public static readonly Dictionary<string, byte> BalloonGroups = new Dictionary<string, byte>
        {
            { "old_style", 0 },
            { "balloon", 1 },
            { "conversation", 2 },
            { "motive", 3 },
            { "relationship", 4 },
            { "headline", 5 },
            { "debug", 6 },
            { "algorithmic", 7 },
            { "route_failure", 8 },
            { "progress", 9 },
            { "magic", 10 },
            { "money", 255 },
        };

        // Buy Mode catalog categories -> catalog "s" index (ItemsByCategory[30]).
        // Source: tso.client/UI/Panels/UIBuyMode.cs InitCategoryMap().
        public static readonly Dictionary<string, sbyte> Categories = new Dictionary<string, sbyte>
        {
            { "seating", 12 },
            { "surfaces", 13 },
            { "appliances", 14 },
            { "electronics", 15 },
            { "skill", 16 },
            { "decorative", 17 },
            { "misc", 18 },
            { "lighting", 19 },
            { "pets", 20 },
        };

        public static string List<T>(Dictionary<string, T> dict)
        {
            return string.Join(", ", dict.Keys);
        }
    }
}
