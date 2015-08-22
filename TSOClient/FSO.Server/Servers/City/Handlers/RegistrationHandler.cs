using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Shards;
using FSO.Server.Framework.Aries;
using FSO.Server.Protocol.Voltron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Content;
using FSO.Vitaboy;
using System.Text.RegularExpressions;

namespace FSO.Server.Servers.City.Handlers
{
    public class RegistrationHandler
    {
        /// <summary>
        /// Must not start with whitespace
        /// May not contain numbers or special characters
        /// At least 3 characters
        /// No more than 24 characters
        /// </summary>
        private static Regex NAME_VALIDATION = new Regex("^([a-zA-Z]){1}([a-zA-Z ]){2,23}$");

        /// <summary>
        /// Only printable ascii characters
        /// Minimum 0 characters
        /// Maximum 499 characters
        /// </summary>
        private static Regex DESC_VALIDATION = new Regex("^([a-zA-Z0-9\\s\\x20-\\x7F]){0,499}$");

        private Shard Shard;
        private IDAFactory DAFactory;
        private Content.Content Content;

        private Collection FemaleHeads;
        private Collection FemaleOutfits;
        private Collection MaleHeads;
        private Collection MaleOutfits;

        public RegistrationHandler(Shard shard, IDAFactory daFactory, Content.Content content)
        {
            this.Shard = shard;
            this.DAFactory = daFactory;
            this.Content = content;
            
            FemaleHeads = content.AvatarCollections.Get("ea_female_heads.col");
            FemaleOutfits = content.AvatarCollections.Get("ea_female.col");
            MaleHeads = content.AvatarCollections.Get("ea_male_heads.col");
            MaleOutfits = content.AvatarCollections.Get("ea_male.col");
        }

        /// <summary>
        /// Register a new avatar
        /// </summary>
        /// <param name="session"></param>
        /// <param name="packet"></param>
        public void Handle(IAriesSession session, RSGZWrapperPDU packet)
        {
            CollectionItem head = null;
            CollectionItem body = null;

            switch (packet.Gender)
            {
                case Protocol.Voltron.Model.Gender.FEMALE:
                    head = FemaleHeads.FirstOrDefault(x => x.FileID == packet.HeadOutfitId);
                    body = FemaleOutfits.FirstOrDefault(x => x.FileID == packet.BodyOutfitId);
                    break;
                case Protocol.Voltron.Model.Gender.MALE:
                    head = MaleHeads.FirstOrDefault(x => x.FileID == packet.HeadOutfitId);
                    body = MaleOutfits.FirstOrDefault(x => x.FileID == packet.BodyOutfitId);
                    break;
            }

            if(head == null || body == null)
            {
                throw new Exception("Invalid head or outfit provided for new avatar");
            }

            if (!NAME_VALIDATION.IsMatch(packet.Name))
            {
                throw new Exception("Invalid name");
            }

            if (!DESC_VALIDATION.IsMatch(packet.Description))
            {
                throw new Exception("Invalid description");
            }

            session.Write(new TransmitCreateAvatarNotificationPDU { });
        }
    }
}
