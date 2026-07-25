using System.Collections.Generic;
using FSO.Client.UI.Model;
using FSO.Client.UI.Panels;
using FSO.Common.Rendering.Framework.Model;
using FSO.HIT;
using FSO.LotView;
using FSO.SimAntics;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.Platform;
using FSO.SimAntics.NetPlay.Model.Commands;
using Microsoft.Xna.Framework.Input;

namespace FSO.Client.UI.Panels.LotControls
{
    public class UISledgehammer : UICustomLotControl, IWallHoverTool
    {
        public ArchitectureHit Hover { get; private set; }

        private static MouseCursor _cursor;
        private readonly VM vm;
        private readonly LotView.World World;
        private readonly UILotControl Parent;
        private bool HoverDeletable;
        private bool WasLeftDown;
        private ArchitectureHit LastDragHit;

        public UISledgehammer(VM vm, LotView.World world, UILotControl parent, List<int> _)
        {
            this.vm = vm;
            this.World = parent.World;
            this.Parent = parent;
            Mouse.SetCursor(_cursor ??= UIArchitectureTools.LoadCursor("sledgehammer.png", 4, 4, 2));
        }

        public void MouseDown(UpdateState state)
        {
            Hover = UIArchitectureTools.Pick(vm, World, Parent.GetScaledPoint(state.MouseState.Position));
            HoverDeletable = CanDelete(Hover);

            if (!HoverDeletable)
            {
                if (Hover.Type == ArchitectureHitType.Object)
                    UIArchitectureTools.ShowError(state, VMPlacementError.CannotDeleteObject);
                HITVM.Get().PlaySoundEvent(UISounds.Error);
                return;
            }
            ExecuteDelete(Hover);
            LastDragHit = Hover;
        }

        public void MouseUp(UpdateState state)
        {
            WasLeftDown = false;
            LastDragHit = default;
        }

        public void Update(UpdateState state, bool scrolled)
        {
            if (Parent.MouseIsOn && !Parent.RMBScroll) Mouse.SetCursor(_cursor);
            var leftHeld = state.MouseState.LeftButton == ButtonState.Pressed;
            Hover = UIArchitectureTools.Pick(vm, World, Parent.GetScaledPoint(state.MouseState.Position), includeObjects: leftHeld);
            HoverDeletable = CanDelete(Hover);

            if (leftHeld && WasLeftDown && HoverDeletable
                && Hover.Type is ArchitectureHitType.Wall or ArchitectureHitType.Floor
                && !SameDragTarget(Hover, LastDragHit))
            {
                ExecuteDelete(Hover);
                LastDragHit = Hover;
            }
            WasLeftDown = leftHeld;

            UpdateFloorPreview();
            UIArchitectureTools.UpdateObjectErrorTooltip(state, Hover, HoverDeletable, VMPlacementError.CannotDeleteObject);
        }

        public void Release()
        {
            vm.Context.Architecture.Commands.Clear();
            vm.Context.Architecture.SignalRedraw();
            Mouse.SetCursor(MouseCursor.Arrow);
        }

        private void ExecuteDelete(ArchitectureHit hit)
        {
            switch (hit.Type)
            {
                case ArchitectureHitType.Object:
                    vm.SendCommand(new VMNetDeleteObjectCmd { ObjectID = hit.ObjectId, CleanupAll = true });
                    HITVM.Get().PlaySoundEvent(UISounds.ObjectMoneyBack);
                    break;
                case ArchitectureHitType.Wall:
                    var (pos, dir) = UIArchitectureTools.SegmentToEraseLine(hit.Tile, hit.WallSegment);
                    SendArch(new VMArchitectureCommand
                    {
                        Type = VMArchitectureCommandType.WALL_DELETE,
                        level = hit.Level, x = pos.X, y = pos.Y, x2 = 1, y2 = dir
                    });
                    break;
                case ArchitectureHitType.Floor:
                    SendArch(FloorEraseCommand(hit));
                    break;
            }
        }

        private void UpdateFloorPreview()
        {
            var preview = vm.Context.Architecture.Commands;
            preview.Clear();
            if (HoverDeletable && Hover.Type == ArchitectureHitType.Floor)
                preview.Add(FloorEraseCommand(Hover));
            vm.Context.Architecture.SignalRedraw();
        }

        private static VMArchitectureCommand FloorEraseCommand(ArchitectureHit hit) => new()
        {
            Type = VMArchitectureCommandType.FLOOR_RECT,
            level = hit.Level, pattern = 0,
            x = hit.Tile.X, y = hit.Tile.Y, x2 = 0, y2 = 0
        };

        private void SendArch(VMArchitectureCommand cmd)
        {
            vm.SendCommand(new VMNetArchitectureCmd { Commands = new List<VMArchitectureCommand> { cmd } });
            HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolUp);
        }

        private static bool SameDragTarget(ArchitectureHit a, ArchitectureHit b) =>
            a.Type == b.Type && a.Level == b.Level && a.Tile == b.Tile && a.WallSegment == b.WallSegment;

        private bool CanDelete(ArchitectureHit s) => s.Type switch
        {
            ArchitectureHitType.Wall => true,
            ArchitectureHitType.Floor => s.Pattern != 0,
            ArchitectureHitType.Object => CanDeleteObject(s.ObjectId),
            _ => false
        };

        private bool CanDeleteObject(short objectId)
        {
            var entity = vm.GetObjectById(objectId);
            if (entity is null or VMAvatar) return false;
            if (entity.IsUserMovable(vm.Context, true) != VMPlacementError.Success) return false;
            var validator = vm.TSOState?.Validator;
            return validator == null
                || validator.GetDeleteMode(DeleteMode.Delete, Parent.ActiveEntity as VMAvatar, entity) != DeleteMode.Disallowed;
        }
    }
}
