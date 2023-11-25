using System.Collections.Generic;

namespace FSO.Server.Database.DA.Relationships
{
    public interface IRelationships
    {
        int UpdateMany(List<DbRelationship> entries);
        List<DbRelationship> GetOutgoing(uint entity_id);
        List<DbRelationship> GetBidirectional(uint entity_id);
        int Delete(uint entity_id);
    }
}
