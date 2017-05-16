using FSO.Server.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Network
{
    public class Topics
    {
        public static ITopic For(MaskedStruct mask, uint entityId)
        {
            return new EntityMaskTopic(mask, entityId);
        }
    }

    public class EntityMaskTopic : ITopic
    {
        public MaskedStruct Mask { get; internal set; }
        public uint EntityId { get; internal set; }

        public EntityMaskTopic(MaskedStruct mask, uint entityId)
        {
            this.Mask = mask;
            this.EntityId = entityId;
        }

        public override bool Equals(object obj)
        {
            if(obj is EntityMaskTopic){
                var cast = (EntityMaskTopic)obj;
                return cast.Mask == Mask && cast.EntityId == EntityId;
            }
            return base.Equals(obj);
        }
    }

    public interface ITopic
    {
    }
}
