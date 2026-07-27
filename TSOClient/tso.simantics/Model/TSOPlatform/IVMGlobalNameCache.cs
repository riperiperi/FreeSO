using System.Collections.Generic;

namespace FSO.SimAntics.Model.TSOPlatform
{
    /// <summary>
    /// An interface that allows components outwith SimAntics to provide names for entities not present within the lot.
    /// </summary>
    public interface IVMGlobalNameCache
    {
        string GetNameForID(VM vm, VMGlobalEntityType type, uint persistID);

        /// <summary>
        /// Called to cache an entity in. This is asynchronous - so it should be called before the user has any chance to do any action that requires the name.
        /// Ideal call times: When we join the lot (cache all roommates), when a roommate changes (cache the new roommate).
        /// </summary>
        /// <param name="persistID">The Persist ID for the avatar whose name we want to cache.</param>
        bool Precache(VM vm, VMGlobalEntityType type, uint persistID);
    }

    public enum VMGlobalEntityType
    {
        Avatar,
        Lot
    }

    public class VMBasicGlobalNameCache : IVMGlobalNameCache
    {
        protected Dictionary<VMGlobalEntityType, Dictionary<uint, string>> Caches = [];

        protected Dictionary<uint, string> GetTypeCache(VMGlobalEntityType type)
        {
            if (!Caches.TryGetValue(type, out var cache))
            {
                cache = new Dictionary<uint, string>();
                Caches[type] = cache;
            }

            return cache;
        }

        public virtual string GetNameForID(VM vm, VMGlobalEntityType type, uint persistID)
        {
            if (persistID == 0) return "";

            var cache = GetTypeCache(type);

            string name;
            if (cache.TryGetValue(persistID, out name))
                return name;
            if (Precache(vm, type, persistID))
            {
                if (cache.TryGetValue(persistID, out name))
                    return name;
            }

            switch (type)
            {
                case VMGlobalEntityType.Avatar:
                    return "(offline user)";
                case VMGlobalEntityType.Lot:
                    return $"({(short)(persistID >> 16)}, {(short)persistID})";
                default:
                    return "Retrieving...";
            }
        }

        public virtual bool Precache(VM vm, VMGlobalEntityType type, uint persistID)
        {
            //very simple implementation. if the sim is in the lot, cache their name
            var ava = vm.GetAvatarByPersist(persistID);
            if (ava != null)
            {
                var cache = GetTypeCache(type);
                cache[persistID] = ava.Name;
                return true;
            }
            return false;
        }
    }
}
