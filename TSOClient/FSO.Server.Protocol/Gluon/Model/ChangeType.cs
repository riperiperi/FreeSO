using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Gluon.Model
{
    public enum ChangeType
    {
        REMOVE_ROOMMATE,
        ADD_ROOMMATE,
        BECOME_OWNER,
        BECOME_OWNER_WITH_OBJECTS,
        ROOMIE_INHERIT_OBJECTS_ONLY
    }
}
