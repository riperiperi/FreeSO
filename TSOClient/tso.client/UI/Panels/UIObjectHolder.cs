﻿/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                var price = (int)catalogItem.Value.Price;
                var dcPercent = VMBuildableAreaInfo.GetDiscountFor(catalogItem.Value, vm);
                var finalPrice = (price * (100 - dcPercent)) / 100;
                Holding.Price = finalPrice;
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
            if (!Holding.IsBought && !vm.TSOState.CanPlaceNewUserObject(vm)) status = VMPlacementError.TooManyObjectsOnTheLot;
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    status = Holding.Group.ChangePosition(LotTilePos.FromBigTile((short)pos.X, (short)pos.Y, World.State.Level), dir, vm.Context, VMPlaceRequestFlags.UserPlacement).Status;
                    if (status != VMPlacementError.MustBeAgainstWall) break;
                    dir = (Direction)((((int)dir << 6) & 255) | ((int)dir >> 2));
                }
                if (Holding.Dir != dir) Holding.Dir = dir;
            }

            if (status != VMPlacementError.Success) 
            {
                Holding.Group.ChangePosition(LotTilePos.OUT_OF_WORLD, Holding.Dir, vm.Context, VMPlaceRequestFlags.UserPlacement);

                Holding.Group.SetVisualPosition(new Vector3(pos,
                (((Holding.Group.Objects[0].GetValue(VMStackObjectVariable.AllowedHeightFlags) & 1) == 1) ? 0 : 4f / 5f) + (World.State.Level-1)*2.95f),
                    //^ if we can't be placed on the floor, default to table height.
                Holding.Dir, vm.Context);
            }

            for (int i = 0; i < Holding.Group.Objects.Count; i++)
            {
                var target = Holding.Group.Objects[i];
                var tpos = target.VisualPosition;
                tpos.Z = (World.State.Level - 1)*2.95f;
                Holding.CursorTiles[i].MultitileGroup.SetVisualPosition(tpos, Holding.Dir, vm.Context);
            }
            Holding.CanPlace = status;
        }

        public void ClearSelected()
        {
            //TODO: selected items are only spooky ghosts of the items themselves.
            //      ...so that they dont cause serverside desyncs
            //      and so that clearing selections doesnt delete already placed objects.
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

        public void MouseUp(UpdateState state)
        {
            MouseIsDown = false;
            if (Holding != null && Holding.Clicked)
            {
                if (Holding.CanPlace == VMPlacementError.Success)
                {
                    HITVM.Get().PlaySoundEvent((Holding.IsBought) ? UISounds.ObjectMovePlace : UISounds.ObjectPlace);
                    //ExecuteEntryPoint(11); //User Placement
                    var putDown = Holding;
                    var pos = Holding.Group.BaseObject.Position;
                    if (Holding.IsBought)
                    {
                        vm.SendCommand(new VMNetMoveObjectCmd
                        {
                            ObjectID = Holding.MoveTarget,
                            dir = Holding.Dir,
                            level = pos.Level,
                            x = pos.x,
                            y = pos.y
                        });
                    }
                    else if (Holding.InventoryPID > 0)
                    {
                        vm.SendCommand(new VMNetPlaceInventoryCmd
                        {
                            ObjectPID = Holding.InventoryPID,
                            dir = Holding.Dir,
                            level = pos.Level,
                            x = pos.x,
                            y = pos.y
                        });
                    }
                    else
                    {
                        var GUID = (Holding.Group.MultiTile)? Holding.Group.BaseObject.MasterDefinition.GUID : Holding.Group.BaseObject.Object.OBJ.GUID;
                        vm.SendCommand(new VMNetBuyObjectCmd
                        {
                            GUID = GUID,
                            dir = Holding.Dir,
                            level = pos.Level,
                            x = pos.x,
                            y = pos.y
                        });
                    }
                    ClearSelected();
                    if (OnPutDown != null) OnPutDown(putDown, state); //call this after so that buy mode etc can produce more.
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
                    ShowErrorAtMouse(LastState, VMPlacementError.CannotDeleteObject);
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
                    ShowErrorAtMouse(LastState, VMPlacementError.CannotDeleteObject);
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

        public void Update(UpdateState state, bool scrolled)
        {
            LastState = state;
            if (ShowTooltip) state.UIState.TooltipProperties.UpdateDead = false;
            MouseClicked = (MouseIsDown && (!MouseWasDown));

            CursorType cur = CursorType.SimsMove;
            if (Holding != null)
            {
                if (Roommate) cur = CursorType.SimsPlace;
                if (state.KeyboardState.IsKeyDown(Keys.Delete))
                {
                    SellBack(null);
                } else if (state.KeyboardState.IsKeyDown(Keys.Escape))
                {
                    OnDelete(Holding, null);
                    ClearSelected();
                }
            }
            if (Holding != null && Roommate)
            {
                if (MouseClicked) Holding.Clicked = true;
                if (MouseIsDown && Holding.Clicked)
                {
                    bool updatePos = MouseClicked;
                    int xDiff = state.MouseState.X - MouseDownX;
                    int yDiff = state.MouseState.Y - MouseDownY;
                    cur = CursorType.SimsRotate;
                    if (Math.Sqrt(xDiff * xDiff + yDiff * yDiff) > 64)
                    {
                        var from = World.EstTileAtPosWithScroll(new Vector2(MouseDownX, MouseDownY));
                        var target = World.EstTileAtPosWithScroll(state.MouseState.Position.ToVector2());

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
                            ParentControl.ActiveEntity != null && ParentControl.ActiveEntity.TSOState.Budget.Value < Holding.Price)
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
                    var tilePos = World.EstTileAtPosWithScroll(new Vector2(scaled.X, scaled.Y) / FSOEnvironment.DPIScaleFactor) + Holding.TilePosOffset;
                    MoveSelected(tilePos, 1);
                }
            }
            else if (MouseClicked)
            {
                //not holding an object, but one can be selected
                var scaled = GetScaledPoint(state.MouseState.Position);
                var newHover = World.GetObjectIDAtScreenPos(scaled.X, scaled.Y, GameFacade.GraphicsDevice);
                if (MouseClicked && (newHover != 0) && (vm.GetObjectById(newHover) is VMGameObject))
                {
                    var objGroup = vm.GetObjectById(newHover).MultitileGroup;
                    var objBasePos = objGroup.BaseObject.Position;
                    var success = (Roommate || objGroup.SalePrice > -1)?objGroup.BaseObject.IsUserMovable(vm.Context, false): VMPlacementError.ObjectNotOwnedByYou;
                    if (GameFacade.EnableMod) success = VMPlacementError.Success;
                    if (objBasePos.Level != World.State.Level) success = VMPlacementError.CantEffectFirstLevelFromSecondLevel;
                    if (success == VMPlacementError.Success)
                    {
                        var ghostGroup = vm.Context.GhostCopyGroup(objGroup);
                        var canDelete = GameFacade.EnableMod || (objGroup.BaseObject.IsUserMovable(vm.Context, true)) == VMPlacementError.Success;
                        SetSelected(ghostGroup);

                        Holding.RealEnt = objGroup.BaseObject;
                        Holding.CanDelete = canDelete;
                        Holding.MoveTarget = newHover;
                        Holding.TilePosOffset = new Vector2(objBasePos.x / 16f, objBasePos.y / 16f) - World.EstTileAtPosWithScroll(GetScaledPoint(state.MouseState.Position).ToVector2() / FSOEnvironment.DPIScaleFactor);
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
        public bool Clicked;
        public VMPlacementError CanPlace;
        public sbyte Level;
        public int Price;
        public uint InventoryPID = 0;
        public bool CanDelete;
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
