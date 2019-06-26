using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Model
{
    public class VMLoadError
    {
        public VMLoadErrorCode Code;
        public ushort SubjectID; //object id (not guid)
        public string SubjectName; //object GUID, wall filename, floor filename


        public VMLoadError(VMLoadErrorCode code, string subject)
        {
            Code = code;
            SubjectName = subject;
        }

        public VMLoadError(VMLoadErrorCode code, string subject, ushort subjectID) : this(code, subject)
        {
            SubjectID = subjectID;
        }

        public override string ToString()
        {
            if (SubjectID != 0) return Code.ToString() + ": " + SubjectName + " on item with ID " + SubjectID;
            else return Code.ToString() + ": " + SubjectName;
        }
    }

    public enum VMLoadErrorCode
    {
        MISSING_OBJECT,
        MISSING_WALL,
        MISSING_FLOOR,
        MISSING_ROOF,
        MISSING_ANIM,
        MISSING_MESH,
        MISSING_TEXTURE,
        INVALID_SCRIPT_STATE,
        UPGRADE,
        FATAL,

        UNKNOWN_ERROR
    }
}
