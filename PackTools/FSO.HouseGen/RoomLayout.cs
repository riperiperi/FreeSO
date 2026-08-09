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
