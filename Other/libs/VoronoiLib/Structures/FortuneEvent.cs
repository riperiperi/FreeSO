using System;

namespace VoronoiLib.Structures
{
    interface FortuneEvent : IComparable<FortuneEvent>
    {
        double X { get; }
        double Y { get; }
    }
}
