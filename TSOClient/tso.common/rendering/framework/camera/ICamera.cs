/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace FSO.Common.Rendering.Framework.Camera
{
    public interface ICamera
    {
        Matrix View { get; }
        Matrix Projection { get; }

        Vector3 Position { get; set; }
        Vector3 Target { get; set; }
        Vector3 Up { get; set; }
        Vector3 Translation { get; set; }

        Vector2 ProjectionOrigin { get; set; }
        float NearPlane { get; set; }
        float FarPlane { get; set; }
        float Zoom { get; set; }
        float AspectRatioMultiplier { get; set; }

    }
}
