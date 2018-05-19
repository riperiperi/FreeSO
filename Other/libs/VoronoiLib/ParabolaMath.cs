using System;
using System.Runtime.CompilerServices;

namespace VoronoiLib
{
    public static class ParabolaMath
    {
        public const double EPSILON = double.Epsilon*1E100;

        public static double EvalParabola(double focusX, double focusY, double directrix, double x)
        {
            return .5*( (x - focusX) * (x - focusX) /(focusY - directrix) + focusY + directrix);
        }

        //gives the intersect point such that parabola 1 will be on top of parabola 2 slightly before the intersect
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double IntersectParabolaX(double focus1X, double focus1Y, double focus2X, double focus2Y,
            double directrix)
        {
            //admittedly this is pure voodoo.
            //there is attached documentation for this function
            return focus1Y.ApproxEqual(focus2Y)
                ? (focus1X + focus2X)/2
                : (focus1X*(directrix - focus2Y) + focus2X*(focus1Y - directrix) +
                   Math.Sqrt((directrix - focus1Y)*(directrix - focus2Y)*
                             ((focus1X - focus2X)*(focus1X - focus2X) +
                              (focus1Y - focus2Y)*(focus1Y - focus2Y))
                   )
                  )/(focus1Y - focus2Y);
        }

        public static bool ApproxEqual(this double value1, double value2)
        {
            return Math.Abs(value1 - value2) <= EPSILON;
        }

        public static bool ApproxGreaterThanOrEqualTo(this double value1, double value2)
        {
            return value1 > value2 || value1.ApproxEqual(value2);
        }

        public static bool ApproxLessThanOrEqualTo(this double value1, double value2)
        {
            return value1 < value2 || value1.ApproxEqual(value2);
        }
    }
}
