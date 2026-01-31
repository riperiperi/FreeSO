using System.Collections.Generic;
using System.Linq;

namespace FSO.Content.TS1
{
    /// <summary>
    /// Essential controller objects for The Sims 1 lots.
    /// These system objects must exist on every lot to provide core functionality.
    ///
    /// In vanilla TS1, these are spawned automatically when a lot loads and saved into OBJM.
    /// Since TS1 IFF files don't use the Global==1 flag (that's a TSO mechanism),
    /// we maintain a hardcoded list of controller GUIDs that should be spawned if missing.
    ///
    /// These GUIDs were identified by comparing a fresh lot (never opened in vanilla TS1)
    /// with a lot that was loaded and saved in vanilla TS1 (House05.iff vs House06.iff).
    /// </summary>
    public static class TS1ControllerObjects
    {
        /// <summary>
        /// Critical core controllers that manage fundamental lot systems.
        /// These were found in the old commented-out hardcoded list in VMTS1Activator.cs
        /// </summary>
        public static readonly uint[] CriticalControllers = new uint[]
        {
            2933422533, // 0xAED879C5 - *Phone Line (CRITICAL - phone system)
            3797042898, // 0xE2627AD2 - *Visit Generator (visitor spawning)
        };

        /// <summary>
        /// Core controllers that manage fundamental lot systems.
        /// </summary>
        public static readonly uint[] CoreControllers = new uint[]
        {
            1432051485, // 0x55581B1D - *Controller - Grass Simulator
            2372790764, // 0x8D4A56EC - *Controller - Unleashed - Buy/Build Mode
            2429430312, // 0x90C6B228 - *Controller - Unleashed - Community Lot Phone Adder
            2426231743, // 0x90B56A9F - *Controller - Vacation Buy/Build Mode
        };

        /// <summary>
        /// Music and sound system controllers.
        /// </summary>
        public static readonly uint[] MusicControllers = new uint[]
        {
            1242322479, // 0x4A0E7AAF - *Stereo Speakers - Music Controller
            995768825,  // 0x3B5C6E79 - *Stereo Speakers - Superstar - Music Controller
        };

        /// <summary>
        /// Pet system controllers (Unleashed expansion).
        /// </summary>
        public static readonly uint[] PetControllers = new uint[]
        {
            2826720480, // 0xA8778860 - Pet Cat Sit Controller
            3505775070, // 0xD1159CDE - Pet Dog Sit Controller
            // Clown Catchers Plugin (prevents tragic clown on pet lots)
            2800949331, // 0xA6F48E53 - Clown Catchers Plugin
            2246099583, // 0x85E38DFF - Clown Catchers Plugin
            3323953410, // 0xC61A2202 - Clown Catchers Plugin
            2168195876, // 0x814A16A4 - Clown Catchers Plugin
            755918200,  // 0x2D0FDDF8 - Clown Catchers Plugin
        };

        /// <summary>
        /// NPC and fame system controllers (Superstar expansion).
        /// </summary>
        public static readonly uint[] NPCControllers = new uint[]
        {
            551758908,  // 0x20E17ABC - *Controller - Home Fame Decay
            2455508898, // 0x924E4B22 - *Controller - Studio NPC
            3726629274, // 0xDE0A1F9A - NPC Controller Superstar
            2572099052, // 0x994ECDEC - NPC Controller Superstar
            2517931412, // 0x96007194 - Superstar Help System
            3579253883, // 0xD562DCFB - Help System - Magic
        };

        /// <summary>
        /// Tragic Clown Generator - spawns tragic clown when Sims are very sad.
        /// </summary>
        public static readonly uint[] TragicClownGenerators = new uint[]
        {
            3066626089, // 0xB6D0F4A9 - Tragic Clown Generator
            483955778,  // 0x1CD89C42 - Tragic Clown Generator
            2487222872, // 0x944509D8 - Tragic Clown Generator
            3593295510, // 0xD6421016 - Tragic Clown Generator
            2597290823, // 0x9AECC6C7 - Tragic Clown Generator
            2053463021, // 0x7A6127ED - Tragic Clown Generator
        };

        /// <summary>
        /// Other system objects (plugins, helpers, pests, etc).
        /// GUIDs verified against actual vanilla TS1 lot saves.
        /// </summary>
        public static readonly uint[] OtherSystemObjects = new uint[]
        {
            // Pests
            1351646726, // 0x50908E06 - Flies
            2432201917, // 0x910BB4BD - Eek! Mice!

            // VR and interactive objects with system behavior
            2067581956, // 0x7B3A2004 - SSRI Virtual Reality Set
            459434614,  // 0x1B62F4F6 - SSRI Virtual Reality Set
            1651634163, // 0x626F3773 - SSRI Virtual Reality Set

            // Treasure Hunt
            1173313544, // 0x45F20E08 - Treasure Hunt

            // Expansion pack plugins
            2880036682, // 0xABAAF34A - Vacation Plugin
            3936853382, // 0xEAB31986 - Vacation Plugin
            2568581908, // 0x991C9F14 - Vacation Plugin
        };

        /// <summary>
        /// All essential controllers combined.
        /// </summary>
        public static readonly uint[] AllEssentialControllers = CriticalControllers
            .Concat(CoreControllers)
            .Concat(MusicControllers)
            .Concat(PetControllers)
            .Concat(NPCControllers)
            .Concat(TragicClownGenerators)
            .Concat(OtherSystemObjects)
            .ToArray();

        /// <summary>
        /// Check if a GUID is a known controller object.
        /// </summary>
        public static bool IsController(uint guid)
        {
            return AllEssentialControllers.Contains(guid);
        }
    }
}
