using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.files.formats.iff;

namespace tso.simantics.engine.scopes
{
    public enum VMVariableScope {

        /** This is used to change the stack object **/
        MyObjectAttributes = 0,
        StackObjectAttributes = 1,
        MyObject = 3,
        StackObject = 4,
        Literal = 7,
        Temps = 8,
        Parameters = 9,
        StackObjectID = 10,
        MyPersonData = 18,
        StackObjectsDefinition = 21,
        Local = 25,
        StackObjectTuning = 26,
        DynSpriteFlagForTempOfStackObject = 27
    }
}
