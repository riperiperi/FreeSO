using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.world.model;
using Microsoft.Xna.Framework;
using tso.world.components;
using TSO.Simantics.model;

namespace TSO.Simantics.entities
{
    /// <summary>
    /// Ties multiple entities together with a common name and set of repositioning functions.
    /// </summary>
    public class VMMultitileGroup
    {
        public bool MultiTile;
        public List<VMEntity> Objects = new List<VMEntity>();

        public VMEntity BaseObject
        {
            get
            {
                for (int i = 0; i < Objects.Count(); i++)
                {
                    var sub = Objects[i];
                    if ((((ushort)sub.Object.OBJ.SubIndex) >> 8) == 0 && (((ushort)sub.Object.OBJ.SubIndex) & 0xFF) == 0) return sub;
                }
                return Objects[0];
            }
        }

        public Vector3[] GetBasePositions()
        {
            Vector3[] positions = new Vector3[Objects.Count];
            for (int i = 0; i < Objects.Count(); i++)
            {
                ushort sub = (ushort)Objects[i].Object.OBJ.SubIndex;
                positions[i] = new Vector3((sbyte)(sub >> 8), (sbyte)(sub & 0xFF), 0);
            }
            return positions;
        }

        public VMPlacementError ChangePosition(LotTilePos pos, Direction direction, VMContext context)
        {
            for (int i = 0; i < Objects.Count(); i++) Objects[i].PrePositionChange(context);

            int Dir = 0;
            switch (direction)
            {
                case Direction.NORTH:
                    Dir = 0; break;
                case Direction.EAST:
                    Dir = 2; break;
                case Direction.SOUTH:
                    Dir = 4; break;
                case Direction.WEST:
                    Dir = 6; break;
            }

            Matrix rotMat = Matrix.CreateRotationZ((float)(Dir * Math.PI / 4.0));
            VMPlacementResult[] places = new VMPlacementResult[Objects.Count()];

            //TODO: optimize so we don't have to recalculate all of this
            if (pos != LotTilePos.OUT_OF_WORLD)
            {
                for (int i = 0; i < Objects.Count(); i++)
                {
                    var sub = Objects[i];
                    var off = new Vector3((sbyte)(((ushort)sub.Object.OBJ.SubIndex) >> 8) * 16, (sbyte)(((ushort)sub.Object.OBJ.SubIndex) & 0xFF) * 16, 0);
                    off = Vector3.Transform(off, rotMat);

                    var offPos = new LotTilePos((short)Math.Round(pos.x + off.X), (short)Math.Round(pos.y + off.Y), pos.Level);
                    places[i] = sub.PositionValid(offPos, direction, context);
                    if (places[i].Status != VMPlacementError.Success)
                    {
                        //go back to where we started: we're no longer out of world.
                        for (int j = 0; j < Objects.Count(); j++) Objects[j].PositionChange(context);
                        return places[i].Status;
                    }
                }
            }
            
            //verification success

            for (int i = 0; i < Objects.Count(); i++)
            {
                var sub = Objects[i];
                var off = new Vector3((sbyte)(((ushort)sub.Object.OBJ.SubIndex) >> 8) * 16, (sbyte)(((ushort)sub.Object.OBJ.SubIndex) & 0xFF)*16, 0);
                off = Vector3.Transform(off, rotMat);

                var offPos = (pos==LotTilePos.OUT_OF_WORLD)?
                    LotTilePos.OUT_OF_WORLD :
                    new LotTilePos((short)Math.Round(pos.x + off.X), (short)Math.Round(pos.y + off.Y), pos.Level);

                sub.SetIndivPosition(offPos, direction, context, places[i]);
            }
            for (int i = 0; i < Objects.Count(); i++) Objects[i].PositionChange(context);
            return VMPlacementError.Success;
        }

        public void SetVisualPosition(Vector3 pos, Direction direction, VMContext context)
        {
            int Dir = 0;
            switch (direction)
            {
                case Direction.NORTH:
                    Dir = 0; break;
                case Direction.EAST:
                    Dir = 2; break;
                case Direction.SOUTH:
                    Dir = 4; break;
                case Direction.WEST:
                    Dir = 6; break;
            }

            Matrix rotMat = Matrix.CreateRotationZ((float)(Dir * Math.PI / 4.0));

            for (int i = 0; i < Objects.Count(); i++)
            {
                var sub = Objects[i];
                var off = new Vector3((sbyte)(((ushort)sub.Object.OBJ.SubIndex) >> 8), (sbyte)(((ushort)sub.Object.OBJ.SubIndex) & 0xFF), 0);
                off = Vector3.Transform(off, rotMat);

                sub.Direction = direction;
                sub.VisualPosition = pos + off;
            }
            //for (int i = 0; i < Objects.Count(); i++) Objects[i].PositionChange(context);
        }

        public void Delete(VMContext context)
        {
            for (int i = 0; i < Objects.Count(); i++)
            {
                var obj = Objects[i];
                obj.PrePositionChange(context);
                context.RemoveObjectInstance(obj);
            }
        }

        public void Init(VMContext context)
        {
            for (int i = 0; i < Objects.Count(); i++)
            {
                Objects[i].Init(context);
            }
        }
    }
}
