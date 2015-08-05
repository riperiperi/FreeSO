/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Common.Utils
{
    public static class DirectionUtils
    {

        /// <summary>
        /// Finds the difference between two radian directions.
        /// </summary>
        /// <param name="a">The direction to subtract from.</param>
        /// <param name="b">The direction to subtract.</param>
        public static double Difference(double a, double b) {
            double value = PosMod(b-a, Math.PI*2);
            if (value > Math.PI) value -= Math.PI * 2;
            return value;
        }

        /// <summary>
        /// Normalizes a direction to the range -PI through PI.
        /// </summary>
        /// <param name="dir">The direction to normalize.</param>
        public static double Normalize(double dir)
        {
            dir = PosMod(dir, Math.PI * 2);
            if (dir > Math.PI) dir -= Math.PI * 2;
            return dir;
        }

        /// <summary>
        /// Normalizes a direction in degrees to the range -180 through 180.
        /// </summary>
        /// <param name="dir">The direction to normalize.</param>
        public static double NormalizeDegrees(double dir)
        {
            dir = PosMod(dir, 360);
            if (dir > 180) dir -= 360;
            return dir;
        }

        /// <summary>
        /// Calculates the mathematical modulus of a value.
        /// </summary>
        /// <param name="x">The number to mod.</param>
        /// <param name="x">The factor to use.</param>
        public static double PosMod(double x, double m)
        {
            return (x % m + m) % m;
        }
    }
}
