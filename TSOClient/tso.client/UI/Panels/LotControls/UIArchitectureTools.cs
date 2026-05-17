using FSO.Client;
using FSO.Client.UI.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.LotView;
using FSO.LotView.Model;
using FSO.SimAntics;
using FSO.SimAntics.Model;
using FSO.SimAntics.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FSO.Client.UI.Panels.LotControls
{
    public enum ArchitectureHitType { None, Object, Wall, Floor }

    public struct ArchitectureHit
    {
        public ArchitectureHitType Type;
        public Point Tile;
        public sbyte Level;
        public short ObjectId;
        public uint ObjectGuid;
        public WallSegments WallSegment;
        public ushort Pattern;
        public (int Primary, int Alt) PaintDir;
    }

    public static class UIArchitectureTools
    {
        public static MouseCursor LoadCursor(string filename, int hotX, int hotY, int frameIndex = 0)
        {
            var gd = GameFacade.GraphicsDevice;
            var strip = Content.Content.Get().CustomUI.Get(filename).Get(gd);
            var size = strip.Height;
            var pixels = new Color[size * size];
            strip.GetData(0, new Rectangle(size * frameIndex, 0, size, size), pixels, 0, pixels.Length);
            var frame = new Texture2D(gd, size, size);
            frame.SetData(pixels);
            return MouseCursor.FromTexture2D(frame, hotX, hotY);
        }

        public static void ShowError(UpdateState state, VMPlacementError error)
        {
            state.UIState.TooltipProperties.Show = true;
            state.UIState.TooltipProperties.Color = Color.Black;
            state.UIState.TooltipProperties.Opacity = 1;
            state.UIState.TooltipProperties.Position = new Vector2(state.MouseState.X, state.MouseState.Y);
            state.UIState.Tooltip = GameFacade.Strings.GetString("137", "kPErr" + error);
            state.UIState.TooltipProperties.UpdateDead = false;
        }

        public static void UpdateObjectErrorTooltip(UpdateState state, ArchitectureHit hover, bool valid, VMPlacementError error)
        {
            if (state.MouseState.LeftButton == ButtonState.Pressed && !valid && hover.Type == ArchitectureHitType.Object)
            {
                ShowError(state, error);
            }
            else
            {
                state.UIState.TooltipProperties.Show = false;
                state.UIState.TooltipProperties.Opacity = 0;
            }
        }

        public static (Point pos, int direction) SegmentToEraseLine(Point tile, WallSegments segment) => segment switch
        {
            WallSegments.TopRight       => (tile, 0),
            WallSegments.TopLeft        => (tile, 2),
            WallSegments.BottomLeft     => (new Point(tile.X, tile.Y + 1), 0),
            WallSegments.BottomRight    => (new Point(tile.X + 1, tile.Y), 2),
            WallSegments.VerticalDiag   => (tile, 1),
            WallSegments.HorizontalDiag => (tile, 3),
            _                           => (tile, 0)
        };

        private static ushort GetSegmentPattern(WallTile wall, WallSegments segment) => segment switch
        {
            WallSegments.TopLeft     => wall.TopLeftPattern,
            WallSegments.TopRight    => wall.TopRightPattern,
            WallSegments.BottomRight => wall.BottomRightPattern,
            WallSegments.BottomLeft or WallSegments.HorizontalDiag or WallSegments.VerticalDiag => wall.BottomLeftPattern,
            _                        => 0
        };

        public static ArchitectureHit Pick(VM vm, World world, Point mouse, bool includeObjects = true)
        {
            var arch = vm.Context.Architecture;
            var mouseV = new Vector2(mouse.X, mouse.Y);
            var level = world.State.Level;

            var tilePos = world.EstTileAtPosWithScroll(mouseV);
            var cursor = new Point((int)tilePos.X, (int)tilePos.Y);
            var (dir, altDir) = FractToWallDir(new Vector2(tilePos.X - cursor.X, tilePos.Y - cursor.Y), world.State.CutRotation);

            var hit = world.GetWallAtScreenPos(mouseV);
            if (hit.HasValue && !arch.OutsideClip((short)hit.Value.Tile.X, (short)hit.Value.Tile.Y, hit.Value.Level)
                && !IsCutAway(vm, hit.Value.Tile))
            {
                var rDir = SegmentToDir(hit.Value.Segment, dir);
                return WallHit(arch, hit.Value.Tile, hit.Value.Level, hit.Value.Segment, rDir, rDir);
            }

            if (arch.OutsideClip((short)cursor.X, (short)cursor.Y, level)) return default;

            var objId = includeObjects ? world.GetObjectIDAtScreenPos(mouse.X, mouse.Y, GameFacade.GraphicsDevice) : (short)0;
            if (objId > 0)
            {
                var entity = vm.GetObjectById(objId);
                var root = entity?.MultitileGroup?.BaseObject ?? entity;
                if (root != null) return new ArchitectureHit
                {
                    Type = ArchitectureHitType.Object,
                    ObjectId = root.ObjectID,
                    ObjectGuid = root.MasterDefinition?.GUID ?? root.Object.OBJ.GUID,
                    Tile = new Point(root.Position.TileX, root.Position.TileY),
                    Level = root.Position.Level
                };
            }

            var matchedDir = VMArchitectureTools.GetPatternDirection(arch, cursor, 0, dir, altDir, level);
            if (matchedDir != -1)
            {
                var segs = arch.GetWall((short)cursor.X, (short)cursor.Y, level).Segments;
                var segment = (segs & WallSegments.AnyDiag) != 0
                    ? ((segs & WallSegments.HorizontalDiag) != 0 ? WallSegments.HorizontalDiag : WallSegments.VerticalDiag)
                    : (WallSegments)(1 << matchedDir);
                return WallHit(arch, cursor, level, segment, dir, altDir);
            }

            return new ArchitectureHit
            {
                Type = ArchitectureHitType.Floor,
                Tile = cursor,
                Level = level,
                Pattern = arch.GetFloor((short)cursor.X, (short)cursor.Y, level).Pattern
            };
        }

        private static bool IsCutAway(VM vm, Point tile)
        {
            var bp = vm.Context.Blueprint;
            if (bp?.Cutaway == null) return false;
            if ((uint)tile.X >= (uint)bp.Width || (uint)tile.Y >= (uint)bp.Height) return false;
            return bp.Cutaway[tile.Y * bp.Width + tile.X];
        }

        private static ArchitectureHit WallHit(VMArchitecture arch, Point tile, sbyte level, WallSegments segment, int primary, int alt)
        {
            var wall = arch.GetWall((short)tile.X, (short)tile.Y, level);
            return new ArchitectureHit
            {
                Type = ArchitectureHitType.Wall,
                Tile = tile,
                Level = level,
                WallSegment = segment,
                Pattern = GetSegmentPattern(wall, segment),
                PaintDir = (primary, alt)
            };
        }

        private static (int dir, int altDir) FractToWallDir(Vector2 fract, WorldRotation rotation) => rotation switch
        {
            WorldRotation.BottomRight => fract.X - fract.Y > 0 ? (2, 3) : (3, 2),
            WorldRotation.TopRight    => fract.X + fract.Y > 1 ? (3, 0) : (0, 3),
            WorldRotation.TopLeft     => fract.X - fract.Y > 0 ? (1, 0) : (0, 1),
            WorldRotation.BottomLeft  => fract.X + fract.Y > 1 ? (2, 1) : (1, 2),
            _                         => (0, 1)
        };

        private static int SegmentToDir(WallSegments segment, int diagFallback) => segment switch
        {
            WallSegments.TopLeft     => 0,
            WallSegments.TopRight    => 1,
            WallSegments.BottomRight => 2,
            WallSegments.BottomLeft  => 3,
            _                        => diagFallback
        };
    }
}
