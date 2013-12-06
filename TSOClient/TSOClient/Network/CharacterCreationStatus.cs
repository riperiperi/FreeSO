using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Network
{
    public enum CharacterCreationStatus
    {
        NameAlreadyExisted = 0x01,
        ExceededCharacterLimit = 0x02,
        Success = 0x03
    }
}
