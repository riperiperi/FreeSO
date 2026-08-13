using System.Collections.Generic;

namespace FSO.HouseGen
{
    /// <summary>
    /// The intermediate room-layout model: what a floor plan means, before it is a blueprint.
    ///
    /// This exists so the vision step and the XML step can fail separately. A vision model
    /// emits one of these and nothing else; BlueprintWriter turns it into XML deterministically,
    /// with no model in the loop. When a generated house comes out wrong, the layout says
    /// whether the model misread the plan or the converter mis-encoded it.
    ///
    /// Coordinates are tile indices on the lot grid. One tile is one metre (see
    /// ../../task_plan.md, "Scale mapping"). Origin of a room is its lowest (x,y) corner and
    /// Width/Height are inclusive extents, so a 4x4 room at (32,32) occupies 32..35 in both axes.
    /// </summary>
    public class HouseLayout
    {
        /// Lot grid dimension. 77 is what every stock FreeSO blueprint uses.
        public int Size = 77;

        public List<Room> Rooms = new List<Room>();

        public List<Door> Doors = new List<Door>();

        public List<Window> Windows = new List<Window>();
    }

    /// <summary>
    /// A door is not a property of a wall — it is an object with the ArchitectualDoor flag that
    /// clears the solidity of the wall it stands in. So a door is placed at the tile whose LOW
    /// edge carries the wall it cuts (see BlueprintWriter for why walls live on low edges).
    ///
    /// Edge "west" cuts the TopLeft segment of (X,Y); edge "north" cuts the TopRight segment.
    /// The door must sit on a tile that actually has that wall segment, or it cuts nothing.
    /// </summary>
    public class Door
    {
        public int X;
        public int Y;

        /// "west" (cuts TopLeft) or "north" (cuts TopRight).
        public string Edge = "west";

        /// A real base-game GUID. Default is "Door - Front" (0x23941850), found via the
        /// find_base_object MCP tool rather than guessed.
        public string Guid = "0x23941850";

        /// Room level, 0-based like Room.Level. The writer converts to the objects-are-1-based
        /// convention that VMWorldActivator expects.
        public int Level = 0;
    }

    /// <summary>
    /// Same wall-object placement as a door (multitile group straddling the low edge), but with
    /// ArchitectualWindow — it styles the wall without clearing solidity, so it does not open a
    /// pathing portal. Coordinates name the wall tile, not the XML anchor (see BlueprintWriter).
    /// </summary>
    public class Window
    {
        public int X;
        public int Y;

        /// "west" (TopLeft) or "north" (TopRight).
        public string Edge = "west";

        /// "Window - Single Pane" (0x44E8992A), from packingslips/objecttable.xml via the same
        /// path as find_base_object — never guess a GUID.
        public string Guid = "0x44E8992A";

        public int Level = 0;
    }

    public class Room
    {
        /// Free text, for diagnostics only — the engine never sees it.
        public string Name = "";

        public int X;
        public int Y;
        public int Width;
        public int Height;

        /// Floor pattern id. 3 is the one A1 proved loads.
        public int Floor = 3;

        /// 0-based; VMWorldActivator applies +1, so 0 is the ground floor.
        public int Level = 0;
    }
}
