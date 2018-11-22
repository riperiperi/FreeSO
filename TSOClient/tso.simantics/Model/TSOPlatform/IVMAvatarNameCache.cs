using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Model.TSOPlatform
{
    /// <summary>
    /// An interface that allows components outwith SimAntics to provide names for avatars not present within the lot.
    /// </summary>
    public interface IVMAvatarNameCache
    {
        string GetNameForID(VM vm, uint persistID);

        /// <summary>
        /// Called to cache an avatar in. This is asynchronous - so it should be called before the user has any chance to do any action that requires the name.
        /// Ideal call times: When we join the lot (cache all roommates), when a roommate changes (cache the new roommate).
        /// </summary>
        /// <param name="persistID">The Persist ID for the avatar whose name we want to cache.</param>
        bool Precache(VM vm, uint persistID);
    }

    public class VMBasicAvatarNameCache : IVMAvatarNameCache
    {
        protected Dictionary<uint, string> AvatarNames = new Dictionary<uint, string>();

        public virtual string GetNameForID(VM vm, uint persistID)
        {
            if (persistID == 0) return "";
            string name;
            if (AvatarNames.TryGetValue(persistID, out name))
                return name;
            if (Precache(vm, persistID))
            {
                if (AvatarNames.TryGetValue(persistID, out name))
                    return name;
            }
            return "(offline user)";
        }

        public virtual bool Precache(VM vm, uint persistID)
        {
            //very simple implementation. if the sim is in the lot, cache their name
            var ava = vm.GetAvatarByPersist(persistID);
            if (ava != null)
            {
                AvatarNames[persistID] = ava.Name;
                return true;
            }
            return false;
        }
    }
}
