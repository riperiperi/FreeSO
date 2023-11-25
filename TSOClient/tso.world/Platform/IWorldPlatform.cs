using FSO.LotView.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FSO.LotView.Platform
{
    /// <summary>
    /// An abstraction for functions which are different between LotView "platforms", that is 3D and 2D modes.
    /// Getting thumbnails, clicked object, updating wall geometry, drawing wall geometry...
    /// Note that some things are the same between modes (terrain/floors/avatars) or abstacted in different ways (objects)
    /// Walls are different in 2D as it is not possible to recreate their original aesthetic in 3D.
    /// </summary>
    public interface IWorldPlatform : IDisposable
    {

        /// <summary>
        /// Gets an object's ID given an object's screen position.
        /// </summary>
        /// <param name="x">The object's X position.</param>
        /// <param name="y">The object's Y position.</param>
        /// <param name="gd">GraphicsDevice instance.</param>
        /// <param name="state">WorldState instance.</param>
        /// <returns>Object's ID if the object was found at the given position.</returns>
        short GetObjectIDAtScreenPos(int x, int y, GraphicsDevice gd, WorldState state);

        /// <summary>
        /// Gets an object group's thumbnail provided an array of objects.
        /// </summary>
        /// <param name="objects">The object components to draw.</param>
        /// <param name="gd">GraphicsDevice instance.</param>
        /// <param name="state">WorldState instance.</param>
        /// <returns>Object's ID if the object was found at the given position.</returns>
        Texture2D GetObjectThumb(ObjectComponent[] objects, Vector3[] positions, GraphicsDevice gd, WorldState state);

        /// <summary>
        /// Gets the current lot's thumbnail.
        /// </summary>
        /// <param name="objects">The object components to draw.</param>
        /// <param name="gd">GraphicsDevice instance.</param>
        /// <param name="state">WorldState instance.</param>
        /// <returns>Object's ID if the object was found at the given position.</returns>
        Texture2D GetLotThumb(GraphicsDevice gd, WorldState state, Action<Texture2D> rooflessCallback);

        void RecacheWalls(GraphicsDevice gd, WorldState state, bool cutawayOnly);

    }
}
