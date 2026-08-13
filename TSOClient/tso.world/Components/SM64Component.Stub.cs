#if FSO_NO_SM64
using System;
using System.Collections.Generic;
using FSO.LotView.Components.Model;
using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.Components
{
    /// <summary>
    /// Stub VisualMario when Mario.dll is excluded (BlazorGL / FSO_NO_SM64).
    /// MyMario stays null in practice; methods exist so call sites compile.
    /// </summary>
    internal class VisualMario : IDisposable
    {
        public AvatarComponent Avatar;

        public Vector3 GetMarioPosition() => Vector3.Zero;

        public sbyte DetermineLevel(bool forLight) => 1;

        public void Dispose() { }
    }

    /// <summary>
    /// No-op SM64Component: keeps LotView/SimAntics API without type-loading Mario.dll.
    /// </summary>
    public class SM64Component : IDisposable
    {
        public static bool Allowed;

        public SM64VisualState MyVisualState;
        public Queue<uint> SoundQueue = new Queue<uint>();
        public short MyID;

        public SM64Component(Blueprint bp) { }

        public static void SetAnimData(byte[] data) { }

        public void UpdateOtherMario(AvatarComponent avatar, SM64VisualState state) { }

        public void RemoveMario(AvatarComponent avatar) { }

        public void PlaySound(AvatarComponent avatar, uint sound) { }

        public void Update(GraphicsDevice device, WorldState world, bool visible) { }

        public void Draw(GraphicsDevice gd, WorldState state) { }

        public void UpdateTerrain() { }

        public void UpdateFloors() { }

        public void UpdateWalls() { }

        public void UpdateRoof() { }

        public void RemoveObject(EntityComponent obj) { }

        public void UpdateObject(EntityComponent obj) { }

        public void MigrateSM64(WorldState state, Blueprint blueprint) { }

        public void Dispose() { }
    }
}
#endif
