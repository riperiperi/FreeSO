using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.IDE.EditorComponent
{
    public static class PrimitiveRegistry
    {
        private static Dictionary<byte, Type> DescriptorById = new Dictionary<byte, Type>()
        {
            {2, typeof(ExpressionDescriptor) },
            {23, typeof(PlaySoundEventDescriptor) },
            {44, typeof(AnimateSimDescriptor) }
        };

        public static PrimitiveDescriptor GetDescriptor(ushort id)
        {
            if (id >= 256)
            {
                return new SubroutineDescriptor { PrimID = id };
            }
            else
            {
                if (!DescriptorById.ContainsKey((byte)id)) return new UnknownPrimitiveDescriptor { PrimID = id };
                else
                {
                    var desc = (PrimitiveDescriptor)Activator.CreateInstance(DescriptorById[(byte)id]);
                    desc.PrimID = id;
                    return desc;
                }
            }
        }
    }
}
