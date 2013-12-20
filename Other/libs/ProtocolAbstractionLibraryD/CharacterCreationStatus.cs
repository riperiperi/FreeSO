using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProtocolAbstractionLibraryD
{
    public enum CharacterCreationStatus
    {
        NameAlreadyExisted,
        ExceededCharacterLimit,
        Success,
        GeneralError
    }
}
