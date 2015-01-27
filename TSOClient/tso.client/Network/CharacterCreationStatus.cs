using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Network
{
    public enum CharacterCreationStatus
    {
        NameAlreadyExisted,
        ExceededCharacterLimit,
        Success,
        GeneralError
    }
}
