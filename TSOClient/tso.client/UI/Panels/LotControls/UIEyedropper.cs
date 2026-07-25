using System;
using System.Collections.Generic;
using FSO.Client.UI.Model;
using FSO.Client.UI.Panels;
using FSO.Common.Rendering.Framework.Model;
using FSO.Content;
using FSO.HIT;
using FSO.LotView;
using FSO.LotView.Model;
using FSO.SimAntics;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.Platform;
using Microsoft.Xna.Framework.Input;

namespace FSO.Client.UI.Panels.LotControls
{
    public class UIEyedropper : UICustomLotControl, IWallHoverTool
    {
        public ArchitectureHit Hover { get; private set; }

        private static MouseCursor _cursor;
        private readonly VM vm;
        private readonly LotView.World World;
        private readonly UILotControl Parent;
        private bool HoverPickable;

        public UIEyedropper(VM vm, LotView.World world, UILotControl parent, List<int> _)
        {
            this.vm = vm;
            this.World = parent.World;
            this.Parent = parent;
            Mouse.SetCursor(_cursor ??= UIArchitectureTools.LoadCursor("eyedropper.png", 2, 16, 2));
        }

        public void MouseDown(UpdateState state)
        {
            Hover = UIArchitectureTools.Pick(vm, World, Parent.GetScaledPoint(state.MouseState.Position));
            HoverPickable = CanPick(Hover);

            if (!HoverPickable)
            {
                if (Hover.Type == ArchitectureHitType.Object)
                    UIArchitectureTools.ShowError(state, VMPlacementError.CantBePickedup);
                HITVM.Get().PlaySoundEvent(UISounds.Error);
                return;
            }

            switch (Hover.Type)
            {
                case ArchitectureHitType.Object: PickObject(Hover.ObjectGuid); break;
                case ArchitectureHitType.Wall:   SwitchTool(typeof(UIWallPainter), Hover.Pattern); break;
                case ArchitectureHitType.Floor:  SwitchTool(typeof(UIFloorPainter), Hover.Pattern); break;
            }
        }

        public void MouseUp(UpdateState state) { }

        public void Update(UpdateState state, bool scrolled)
        {
            if (Parent.MouseIsOn && !Parent.RMBScroll) Mouse.SetCursor(_cursor);
            var leftHeld = state.MouseState.LeftButton == ButtonState.Pressed;
            Hover = UIArchitectureTools.Pick(vm, World, Parent.GetScaledPoint(state.MouseState.Position), includeObjects: leftHeld);
            HoverPickable = CanPick(Hover);
            UIArchitectureTools.UpdateObjectErrorTooltip(state, Hover, HoverPickable, VMPlacementError.CantBePickedup);
        }

        public void Release() => Mouse.SetCursor(MouseCursor.Arrow);

        private void PickObject(uint guid)
        {
            var ghost = vm.Context.CreateObjectInstance(guid, LotTilePos.OUT_OF_WORLD, Direction.NORTH, true);
            if (ghost?.BaseObject == null) return;
            Parent.CustomControl = null;
            Parent.ObjectHolder.SetSelected(ghost);
            HITVM.Get().PlaySoundEvent(UISounds.BuyPlace);
        }

        private void SwitchTool(Type toolType, ushort pattern)
        {
            Mouse.SetCursor(MouseCursor.Arrow);
            Parent.CustomControl = (UICustomLotControl)Activator.CreateInstance(
                toolType, vm, World, Parent, new List<int> { pattern });
            HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolPlace);
        }

        private bool CanPick(ArchitectureHit s) => s.Type switch
        {
            ArchitectureHitType.Wall or ArchitectureHitType.Floor => s.Pattern != 0,
            ArchitectureHitType.Object => CanPickObject(s.ObjectId, s.ObjectGuid),
            _ => false
        };

        private bool CanPickObject(short objectId, uint guid)
        {
            var entity = vm.GetObjectById(objectId);
            if (entity is null or VMAvatar) return false;
            if (entity.IsUserMovable(vm.Context, false) != VMPlacementError.Success) return false;
            var validator = vm.TSOState?.Validator;
            return validator == null
                || validator.GetPurchaseMode(PurchaseMode.Normal, Parent.ActiveEntity as VMAvatar, guid, false) != PurchaseMode.Disallowed;
        }
    }
}
