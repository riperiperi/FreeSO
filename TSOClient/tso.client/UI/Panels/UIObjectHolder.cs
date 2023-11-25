using System;
using System.Collections.Generic;
using FSO.LotView;
using FSO.SimAntics;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;
using FSO.LotView.Components;
using FSO.SimAntics.Entities;
using FSO.LotView.Model;
using FSO.Client.UI.Model;
using FSO.HIT;
using FSO.SimAntics.Model;
using Microsoft.Xna.Framework.Input;
using FSO.Client.UI.Framework;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.Common;
using FSO.Client.UI.Controls;
using FSO.Common.Rendering.Framework;
using FSO.SimAntics.Model.Platform;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.Client.UI.Panels
{
    public class UIObjectHolder //controls the object holder interface
    {
        public VM vm;
        public LotView.World World;
        public UILotControl ParentControl;

        public Direction Rotation;
        public int MouseDownX;
        public int MouseDownY;
        private bool MouseIsDown;
        private bool MouseWasDown;
        private bool MouseClicked;

        private int OldMX;
        private int OldMY;
        private UpdateState LastState; //state for access from Sellback and friends.
        public bool DirChanged;
        public bool ShowTooltip;
        public bool Roommate;

        public bool DonateMode;
        private bool Locked;

        public event HolderEventHandler BeforeRelease;
        public event HolderEventHandler OnPickup;
        public event HolderEventHandler OnDelete;
        public event HolderEventHandler OnPutDown;

        public UIObjectSelection Holding;

        public UIObjectHolder(VM vm, LotView.World World, UILotControl parent)
        {
            this.vm = vm;
            this.World = World;
            ParentControl = parent;
        }

        public void SetSelected(VMMultitileGroup Group)
        {
            if (Holding != null) ClearSelected();
            Holding = new UIObjectSelection();
            Holding.Group = Group;
            Holding.PreviousTile = Holding.Group.BaseObject.Position;
            Holding.Dir = Group.Objects[0].Direction;
            VMEntity[] CursorTiles = new VMEntity[Group.Objects.Count];
            for (int i = 0; i < Group.Objects.Count; i++)
            {
                var target = Group.Objects[i];
                target.ExecuteEntryPoint(10, vm.Context, true, target);
                target.SetRoom(65534);
                if (target is VMGameObject) ((ObjectComponent)target.WorldUI).ForceDynamic = true;
                CursorTiles[i] = vm.Context.CreateObjectInstance(0x00000437, new LotTilePos(target.Position), FSO.LotView.Model.Direction.NORTH, true).Objects[0];
                CursorTiles[i].SetPosition(new LotTilePos(0,0,1), Direction.NORTH, vm.Context);
                CursorTiles[i].SetRoom(65535);
                ((ObjectComponent)CursorTiles[i].WorldUI).ForceDynamic = true;
            }
            Holding.TilePosOffset = new Vector2(0, 0);
            Holding.CursorTiles = CursorTiles;

            uint guid;
            var bobj = Group.BaseObject;
            guid = bobj.Object.OBJ.GUID;
            if (bobj.MasterDefinition != null) guid = bobj.MasterDefinition.GUID;
            var catalogItem = Content.Content.Get().WorldCatalog.GetItemByGUID(guid);
            if (catalogItem != null)
            {
                var price = Group.InitialPrice; //(int)catalogItem.Value.Price;
                var dcPercent = VMBuildableAreaInfo.GetDiscountFor(catalogItem.Value, vm);
                var finalPrice = (price * (100 - dcPercent)) / 100;
                if (DonateMode) finalPrice -= (finalPrice * 2) / 3;
                Holding.Price = finalPrice;
                Group.InitialPrice = finalPrice;
                Group.BeforeDCPrice = price;
            }
        }

        public void MoveSelected(Vector2 pos, sbyte level)
        {
            Holding.TilePos = pos;
            Holding.Level = level;

            //first, eject the object from any slots
            for (int i = 0; i < Holding.Group.Objects.Count; i++)
            {
                var obj = Holding.Group.Objects[i];
                if (obj.Container != null)
                {
                    obj.Container.ClearSlot(obj.ContainerSlot);
                }
            }

            //rotate through to try all configurations
            var dir = Holding.Dir;
            VMPlacementError status = VMPlacementError.Success;

            if (!Holding.IsBought)
            {
                if (DonateMode && !vm.TSOState.CanPlaceNewDonatedObject(vm))
                    status = VMPlacementError.TooManyObjectsOnTheLot;
                else if (!DonateMode && !vm.TSOState.CanPlaceNewUserObject(vm))
                    status = VMPlacementError.TooManyObjectsOnTheLot;
            }
            
            if (status == VMPlacementError.Success)
            {
                for (int i = 0; i < 4; i++)
                {
                    status = Holding.Group.ChangePosition(LotTilePos.FromBigTile((short)pos.X, (short)pos.Y, level), dir, vm.Context, VMPlaceRequestFlags.UserPlacement).Status;
                    if (status != VMPlacementError.MustBeAgainstWall) break;
                    dir = (Direction)((((int)dir << 6) & 255) | ((int)dir >> 2));
                }
                if (Holding.Dir != dir) Holding.Dir = dir;
            }

            if (status != VMPlacementError.Success) 
            {
                Holding.Group.ChangePosition(LotTilePos.OUT_OF_WORLD, Holding.Dir, vm.Context, VMPlaceRequestFlags.UserPlacement);

                Holding.Group.SetVisualPosition(new Vector3(pos,
                (((Holding.Group.Objects[0].GetValue(VMStackObjectVariable.AllowedHeightFlags) & 1) == 1) ? 0 : 4f / 5f) + (level-1)*2.95f),
                    //^ if we can't be placed on the floor, default to table height.
                Holding.Dir, vm.Context);
            }

            for (int i = 0; i < Holding.Group.Objects.Count; i++)
            {
                var target = Holding.Group.Objects[i];
                var tpos = target.VisualPosition;
                tpos.Z = (level - 1)*2.95f;
                Holding.CursorTiles[i].MultitileGroup.SetVisualPosition(tpos, Holding.Dir, vm.Context);
            }
            Holding.CanPlace = status;
        }

        public void ClearSelected()
        {
            if (Holding != null) BeforeRelease?.Invoke(Holding, LastState);
            if (Holding != null)
            {
                RecursiveDelete(vm.Context, Holding.Group.BaseObject);

                for (int i = 0; i < Holding.CursorTiles.Length; i++) {
                    Holding.CursorTiles[i].Delete(true, vm.Context);
                    ((ObjectComponent)Holding.CursorTiles[i].WorldUI).ForceDynamic = false;
                }
            }
            Holding = null;
        }

        private void RecursiveDelete(VMContext context, VMEntity real)
        {
            var rgrp = real.MultitileGroup;
            for (int i = 0; i < rgrp.Objects.Count; i++)
            {
                var slots = rgrp.Objects[i].TotalSlots();
                var objs = new List<VMEntity>();
                for (int j = 0; j < slots; j++)
                {
                    var slot = rgrp.Objects[i].GetSlot(j);
                    if (slot != null)
                    {
                        objs.Add(slot);
                    }
                        
                }
                foreach (var obj in objs) RecursiveDelete(context, obj);
            }
            rgrp.Delete(context);
        }


        public void MouseDown(UpdateState state)
        {
            MouseIsDown = true;
            MouseDownX = state.MouseState.X;
            MouseDownY = state.MouseState.Y;
            if (Holding != null)
            {
                Rotation = Holding.Dir;
                DirChanged = false;
            }
        }

        private void InventoryPlaceHolding()
        {
            var pos = Holding.Group.BaseObject.Position;
            vm.SendCommand(new VMNetPlaceInventoryCmd
            {
                ObjectPID = Holding.InventoryPID,
                dir = Holding.Dir,
                level = pos.Level,
                x = pos.x,
                y = pos.y,

                Mode = (DonateMode) ? PurchaseMode.Donate : PurchaseMode.Normal
            });
        }

        private void BuyHolding()
        {
            var pos = Holding.Group.BaseObject.Position;
            var GUID = (Holding.Group.MultiTile) ? Holding.Group.BaseObject.MasterDefinition.GUID : Holding.Group.BaseObject.Object.OBJ.GUID;
            vm.SendCommand(new VMNetBuyObjectCmd
            {
                GUID = GUID,
                dir = Holding.Dir,
                level = pos.Level,
                x = pos.x,
                y = pos.y,
                TargetUpgradeLevel = (Holding.Group.BaseObject.PlatformState as VMTSOObjectState)?.UpgradeLevel ?? 0,

                Mode = (DonateMode) ? PurchaseMode.Donate : PurchaseMode.Normal
            });
        }

        public void MouseUp(UpdateState state)
        {
            MouseIsDown = false;
            if (Holding != null && Holding.Clicked)
            {
                if (Holding.CanPlace == VMPlacementError.Success)
                {
                    //ExecuteEntryPoint(11); //User Placement
                    var putDown = Holding;
                    var pos = Holding.Group.BaseObject.Position;
                    var badCategory = ((Holding.Group.BaseObject as VMGameObject)?.Disabled ?? 0).HasFlag(VMGameObjectDisableFlags.LotCategoryWrong);
                    if (Holding.IsBought)
                    {
                        HITVM.Get().PlaySoundEvent(UISounds.ObjectMovePlace);
                        vm.SendCommand(new VMNetMoveObjectCmd
                        {
                            ObjectID = Holding.MoveTarget,
                            dir = Holding.Dir,
                            level = pos.Level,
                            x = pos.x,
                            y = pos.y
                        });
                    }
                    else {
                        if (badCategory)
                        {
                            Locked = true;
                            UIAlert.YesNo(GameFacade.Strings.GetString("245", "5"), GameFacade.Strings.GetString("245", (Holding.InventoryPID > 0)?"7":"6"), true,
                                (confirm) =>
                                {
                                    Locked = false;
                                    if (!confirm) return;
                                    HITVM.Get().PlaySoundEvent(UISounds.ObjectPlace);
                                    if (Holding.InventoryPID > 0) InventoryPlaceHolding();
                                    else BuyHolding();
                                    ClearSelected();
                                    OnPutDown?.Invoke(putDown, state); //call this after so that buy mode etc can produce more.
                                });
                            return;
                        } else
                        {
                            HITVM.Get().PlaySoundEvent(UISounds.ObjectPlace);
                            if (Holding.InventoryPID > 0) InventoryPlaceHolding();
                            else BuyHolding();
                        }
                        
                    }
                    ClearSelected();
                    OnPutDown?.Invoke(putDown, state); //call this after so that buy mode etc can produce more.
                }
                else
                {
                    
                }
            }

            state.UIState.TooltipProperties.Show = false;
            state.UIState.TooltipProperties.Opacity = 0;
            ShowTooltip = false;
        }

        private void ExecuteEntryPoint(int num)
        {
            for (int i = 0; i < Holding.Group.Objects.Count; i++) Holding.Group.Objects[i].ExecuteEntryPoint(num, vm.Context, true); 
        }

        public void SellBack(UIElement button)
        {
            if (Holding == null || !Roommate) return;
            if (Holding.IsBought)
            {
                if (Holding.CanDelete)
                {
                    vm.SendCommand(new VMNetDeleteObjectCmd
                    {
                        ObjectID = Holding.MoveTarget,
                        CleanupAll = true
                    });
                    HITVM.Get().PlaySoundEvent(UISounds.MoneyBack);
                } else
                {
                    ShowErrorAtMouse(LastState, Holding.DeleteError);
                    return;
                }
            }
            OnDelete(Holding, null); //TODO: cleanup callbacks which don't need updatestate into another delegate.
            ClearSelected();
        }

        public void MoveToInventory(UIElement button)
        {
            if (Holding == null) return;
            if (Holding.IsBought)
            {
                var validator = vm.TSOState.Validator;
                var sendbackMode = validator.GetDeleteMode(DeleteMode.Sendback, (VMAvatar)ParentControl.ActiveEntity, Holding.RealEnt);
                if (sendbackMode != DeleteMode.Sendback) return;
                if (Holding.CanDelete)
                {
                    var obj = vm.GetObjectById(Holding.MoveTarget);
                    if (obj != null)
                    {
                        vm.SendCommand(new VMNetSendToInventoryCmd
                        {
                            ObjectPID = obj.PersistID,
                        });
                    }
                } else
                {
                    ShowErrorAtMouse(LastState, Holding.DeleteError);
                    return;
                }
            }
            OnDelete(Holding, null); //TODO: cleanup callbacks which don't need updatestate into another delegate.
            ClearSelected();
        }

        public void AsyncBuy(UIElement button)
        {
            if (Holding == null || !Holding.IsBought) return;
            var obj = vm.GetObjectById(Holding.MoveTarget);
            if (obj != null)
            {
                if (obj is VMAvatar || (((VMGameObject)obj).Disabled & VMGameObjectDisableFlags.ForSale) == 0)
                {
                    ShowErrorAtMouse(LastState, VMPlacementError.InUse);
                } else
                {
                    UIAlert alert = null;
                    alert = UIScreen.GlobalShowAlert(new UIAlertOptions
                    {
                        Title = GameFacade.Strings.GetString("206", "40"),
                        Message = GameFacade.Strings.GetString("206", "41") + obj.ToString() + "\r\n"
                        + GameFacade.Strings.GetString("206", "42", new string[] { "0" }) + "\r\n"
                        + GameFacade.Strings.GetString("206", "43") + "$" + obj.MultitileGroup.Price.ToString("##,#0") + "\r\n"
                        + GameFacade.Strings.GetString("206", "44") + "$" + obj.MultitileGroup.BaseObject.TSOState.Budget.Value.ToString("##,#0") + "\r\n"
                        + GameFacade.Strings.GetString("206", "46") + "$" + obj.MultitileGroup.SalePrice.ToString("##,#0") + "\r\n"
                        + GameFacade.Strings.GetString("206", "47"),
                        Buttons = new UIAlertButton[]
                        {
                            new UIAlertButton(UIAlertButtonType.Yes, (btn) => {
                                vm.SendCommand(new VMNetAsyncSaleCmd
                                {
                                    ObjectPID = obj.PersistID,
                                });
                                UIScreen.RemoveDialog(alert);
                            }),
                            new UIAlertButton(UIAlertButtonType.No),
                        }
                    }, true);
                }
            }

            OnDelete(Holding, null); //TODO: cleanup callbacks which don't need updatestate into another delegate.
            ClearSelected();
        }

        public void AsyncSale(UIElement button)
        {
            if (Holding == null || !Holding.IsBought) return;
            var obj = vm.GetObjectById(Holding.MoveTarget);
            if (obj != null)
            {
                var movable = obj.IsUserMovable(vm.Context, true);
                if (movable == VMPlacementError.Success)
                {
                    var dialog = new UIAsyncPriceDialog(obj.ToString(), (obj.MultitileGroup.Price<0)?0:(uint)obj.MultitileGroup.Price);
                    dialog.OnPriceChange += (uint salePrice) =>
                    {
                        vm.SendCommand(new VMNetAsyncPriceCmd
                        {
                            NewPrice = (int)Math.Min(int.MaxValue, salePrice),
                            ObjectPID = obj.PersistID
                        });

                        OnDelete(Holding, null);
                        ClearSelected();
                    };

                    UIScreen.GlobalShowDialog(dialog, true);
                }
                else ShowErrorAtMouse(LastState, movable);
            }
        }

        public void AsyncCancelSale(UIElement button)
        {
            if (Holding == null || !Holding.IsBought) return;
            var obj = vm.GetObjectById(Holding.MoveTarget);
            if (obj != null)
            {
                vm.SendCommand(new VMNetAsyncPriceCmd
                {
                    NewPrice = -1,
                    ObjectPID = obj.PersistID
                });
            }
        }

        private Point GetScaledPoint(Point TapPoint)
        {
            var screenMiddle = new Point(
                (int)(GameFacade.Screens.CurrentUIScreen.ScreenWidth / (2 / FSOEnvironment.DPIScaleFactor)),
                (int)(GameFacade.Screens.CurrentUIScreen.ScreenHeight / (2 / FSOEnvironment.DPIScaleFactor))
                );
            return ((TapPoint - screenMiddle).ToVector2() / World.BackbufferScale).ToPoint() + screenMiddle;
        }

        private short GetFloorBlockableHover(Point pt)
        {
            var tilePos = World.EstTileAtPosWithScroll3D(new Vector2(pt.X, pt.Y));
            var newHover = World.GetObjectIDAtScreenPos(pt.X,
                    pt.Y,
                    GameFacade.GraphicsDevice);

            var hobj = vm.GetObjectById(newHover);
            if (hobj == null || hobj.Position.Level < tilePos.Z) newHover = 0;
            return newHover;
        }

        public void Update(UpdateState state, bool scrolled)
        {
            LastState = state;
            if (ShowTooltip) state.UIState.TooltipProperties.UpdateDead = false;
            MouseClicked = (MouseIsDown && (!MouseWasDown));
            if (Locked) return;

            CursorType cur = CursorType.SimsMove;
            if (Holding != null && state.WindowFocused)
            {
                if (state.KeyboardState.IsKeyDown(Keys.Escape))
                {
                    OnDelete(Holding, null);
                    ClearSelected();
                } else if (state.InputManager.GetFocus() == null)
                {
                    if (state.KeyboardState.IsKeyDown(Keys.I))
                    {
                        MoveToInventory(null);
                    } else if (state.KeyboardState.IsKeyDown(Keys.Delete))
                    {
                        SellBack(null);
                    }
                }
            }
            if (Holding != null && Roommate)
            {
                cur = CursorType.SimsPlace;
                if (MouseClicked) Holding.Clicked = true;
                if (MouseIsDown && Holding.Clicked)
                {
                    bool updatePos = MouseClicked;
                    int xDiff = state.MouseState.X - MouseDownX;
                    int yDiff = state.MouseState.Y - MouseDownY;
                    cur = CursorType.SimsRotate;
                    if (Math.Sqrt(xDiff * xDiff + yDiff * yDiff) > 64)
                    {
                        var from = World.EstTileAtPosWithScroll(new Vector2(MouseDownX, MouseDownY) * FSOEnvironment.DPIScaleFactor, Holding.Level);
                        var target = World.EstTileAtPosWithScroll(state.MouseState.Position.ToVector2() * FSOEnvironment.DPIScaleFactor, Holding.Level);

                        var vec = target - from;
                        var dir = Math.Atan2(vec.Y, vec.X);
                        dir += Math.PI/2;
                        if (dir < 0) dir += Math.PI*2;
                        var newDir = (Direction)(1 << (((int)Math.Round(dir / (Math.PI / 2)) % 4) * 2));

                        if (newDir != Holding.Dir || MouseClicked)
                        {
                            updatePos = true;
                            HITVM.Get().PlaySoundEvent(UISounds.ObjectRotate);
                            Holding.Dir = newDir;
                            DirChanged = true;
                        }
                    }
                    if (updatePos)
                    {
                        MoveSelected(Holding.TilePos, Holding.Level);
                        if (!Holding.IsBought && Holding.CanPlace == VMPlacementError.Success && 
                            ParentControl.ActiveEntity != null && ParentControl.Budget < Holding.Price)
                            Holding.CanPlace = VMPlacementError.InsufficientFunds;
                        if (Holding.CanPlace != VMPlacementError.Success)
                        {
                            state.UIState.TooltipProperties.Show = true;
                            state.UIState.TooltipProperties.Color = Color.Black;
                            state.UIState.TooltipProperties.Opacity = 1;
                            state.UIState.TooltipProperties.Position = new Vector2(MouseDownX,
                                MouseDownY);
                            state.UIState.Tooltip = GameFacade.Strings.GetString("137", "kPErr" + Holding.CanPlace.ToString()
                                + ((Holding.CanPlace == VMPlacementError.CannotPlaceComputerOnEndTable) ? "," : ""));
                            // comma added to curcumvent problem with language file. We should probably just index these with numbers?
                            state.UIState.TooltipProperties.UpdateDead = false;
                            ShowTooltip = true;
                            HITVM.Get().PlaySoundEvent(UISounds.Error);
                        }
                        else
                        {
                            state.UIState.TooltipProperties.Show = false;
                            state.UIState.TooltipProperties.Opacity = 0;
                            ShowTooltip = false;
                        }
                    }
                }
                else
                {
                    var scaled = GetScaledPoint(state.MouseState.Position);
                    if ((Holding.Group.BaseObject.GetValue(VMStackObjectVariable.PlacementFlags) & (short)VMPlacementFlags.InAir) > 0)
                    {
                        //if this object can be placed in air, only consider the current level.
                        var tilePos = World.EstTileAtPosWithScroll(new Vector2(scaled.X, scaled.Y) + Holding.MousePosOffset * FSOEnvironment.DPIScaleFactor);
                        MoveSelected(new Vector2(tilePos.X, tilePos.Y), World.State.Level);
                    } else
                    {
                        //can place on any level below
                        var tilePos = World.EstTileAtPosWithScroll3D(new Vector2(scaled.X, scaled.Y) + Holding.MousePosOffset * FSOEnvironment.DPIScaleFactor);
                        MoveSelected(new Vector2(tilePos.X, tilePos.Y), (sbyte)tilePos.Z); // + Holding.TilePosOffset
                    }
                }
            }
            else if (MouseClicked)
            {
                //not holding an object, but one can be selected
                var scaled = GetScaledPoint(state.MouseState.Position);
                var newHover = GetFloorBlockableHover(scaled); //World.GetObjectIDAtScreenPos(scaled.X, scaled.Y, GameFacade.GraphicsDevice);
                if (MouseClicked && (newHover != 0) && (vm.GetObjectById(newHover) is VMGameObject))
                {
                    var objGroup = vm.GetObjectById(newHover).MultitileGroup;
                    var objBasePos = objGroup.BaseObject.Position;
                    var allowMove = vm.PlatformState.Validator.CanMoveObject((VMAvatar)ParentControl.ActiveEntity, objGroup.BaseObject);
                    var success = (Roommate || objGroup.SalePrice > -1)?objGroup.BaseObject.IsUserMovable(vm.Context, false): VMPlacementError.ObjectNotOwnedByYou;
                    if (GameFacade.EnableMod) success = VMPlacementError.Success;
                    //if (objBasePos.Level != World.State.Level) success = VMPlacementError.CantEffectFirstLevelFromSecondLevel;
                    if (success == VMPlacementError.Success)
                    {
                        var ghostGroup = vm.Context.GhostCopyGroup(objGroup);
                        var deleteAllowed = vm.PlatformState.Validator.GetDeleteMode(
                            DeleteMode.Delete, (VMAvatar)ParentControl.ActiveEntity, ghostGroup.BaseObject) != DeleteMode.Disallowed;
                        var canDelete = deleteAllowed && (objGroup.BaseObject.IsUserMovable(vm.Context, true)) == VMPlacementError.Success;
                        if (GameFacade.EnableMod) canDelete = true;
                        SetSelected(ghostGroup);

                        Holding.RealEnt = objGroup.BaseObject;
                        Holding.CanDelete = canDelete;
                        Holding.DeleteError = canDelete ? VMPlacementError.CannotDeleteObject : VMPlacementError.ObjectNotOwnedByYou;
                        Holding.MoveTarget = newHover;
                        Holding.MousePosOffset = (objGroup.BaseObject.WorldUI.GetScreenPos(World.State) - GetScaledPoint(state.MouseState.Position).ToVector2()) / FSOEnvironment.DPIScaleFactor;
                        Holding.TilePosOffset = new Vector2(objBasePos.x / 16f, objBasePos.y / 16f) - World.EstTileAtPosWithScroll(GetScaledPoint(state.MouseState.Position).ToVector2());
                        if (OnPickup != null) OnPickup(Holding, state);
                        //ExecuteEntryPoint(12); //User Pickup
                    }
                    else
                    {
                        ShowErrorAtMouse(state, success);
                    }
                }
            }

            if (ParentControl.MouseIsOn && !ParentControl.RMBScroll)
            {
                GameFacade.Cursor.SetCursor(cur);
            }

            MouseWasDown = MouseIsDown;
        }

        private void ShowErrorAtMouse(UpdateState state, VMPlacementError error)
        {
            state.UIState.TooltipProperties.Show = true;
            state.UIState.TooltipProperties.Color = Color.Black;
            state.UIState.TooltipProperties.Opacity = 1;
            state.UIState.TooltipProperties.Position = new Vector2(MouseDownX,
                MouseDownY);
            state.UIState.Tooltip = GameFacade.Strings.GetString("137", "kPErr" + error.ToString());
            state.UIState.TooltipProperties.UpdateDead = false;
            ShowTooltip = true;
            HITVM.Get().PlaySoundEvent(UISounds.Error);
        }

        public delegate void HolderEventHandler(UIObjectSelection holding, UpdateState state);
    }

    public class UIObjectSelection
    {
        public short MoveTarget = 0;

        public VMMultitileGroup Group;
        public VMEntity[] CursorTiles;
        public LotTilePos PreviousTile;
        public Direction Dir = Direction.NORTH;
        public Vector2 TilePos;
        public Vector2 TilePosOffset;
        public Vector2 MousePosOffset;
        public bool Clicked;
        public VMPlacementError CanPlace;
        public sbyte Level;
        public int Price;
        public uint InventoryPID = 0;
        public bool CanDelete;
        public VMPlacementError DeleteError;
        public VMEntity RealEnt;

        public bool IsBought
        {
            get
            {
                return (MoveTarget != 0);
            }
        }
    }
}
