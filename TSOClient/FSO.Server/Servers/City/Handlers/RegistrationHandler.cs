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
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Common;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;

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

        private CityServerContext Context;
        private IDAFactory DAFactory;
        private Content.Content Content;
        
        /// <summary>
        /// Used for validation
        /// </summary>
        private Dictionary<uint, PurchasableOutfit> ValidFemaleOutfits = new Dictionary<uint, PurchasableOutfit>();
        private Dictionary<uint, PurchasableOutfit> ValidMaleOutfits = new Dictionary<uint, PurchasableOutfit>();

        public RegistrationHandler(CityServerContext context, IDAFactory daFactory, Content.Content content)
        {
            this.Context = context;
            this.DAFactory = daFactory;
            this.Content = content;
            
            content.AvatarCollections.Get("ea_female_heads.col")
                .Select(x => content.AvatarPurchasables.Get(x.PurchasableOutfitId)).ToList()
                    .ForEach(x => ValidFemaleOutfits.Add((uint)(x.OutfitID >> 32), x));

            content.AvatarCollections.Get("ea_female.col")
                .Select(x => content.AvatarPurchasables.Get(x.PurchasableOutfitId)).ToList()
                    .ForEach(x => ValidFemaleOutfits.Add((uint)(x.OutfitID >> 32), x));

            content.AvatarCollections.Get("ea_male_heads.col")
                .Select(x => content.AvatarPurchasables.Get(x.PurchasableOutfitId)).ToList()
                    .ForEach(x => ValidMaleOutfits.Add((uint)(x.OutfitID >> 32), x));

            content.AvatarCollections.Get("ea_male.col")
                .Select(x => content.AvatarPurchasables.Get(x.PurchasableOutfitId)).ToList()
                    .ForEach(x => ValidMaleOutfits.Add((uint)(x.OutfitID >> 32), x));
        }

        /// <summary>
        /// Register a new avatar
        /// </summary>
        /// <param name="session"></param>
        /// <param name="packet"></param>
        public void Handle(IVoltronSession session, RSGZWrapperPDU packet)
        {
            PurchasableOutfit head = null;
            PurchasableOutfit body = null;

            switch (packet.Gender)
            {
                case Protocol.Voltron.Model.Gender.FEMALE:
                    head = ValidFemaleOutfits[packet.HeadOutfitId];
                    body = ValidFemaleOutfits[packet.BodyOutfitId];
                    break;
                case Protocol.Voltron.Model.Gender.MALE:
                    head = ValidMaleOutfits[packet.HeadOutfitId];
                    body = ValidMaleOutfits[packet.BodyOutfitId];
                    break;
            }

            if(head == null)
            {
                session.Write(new CreateASimResponse {
                    Status = CreateASimStatus.FAILED,
                    Reason = CreateASimFailureReason.HEAD_VALIDATION_ERROR
                });
                return;
            }

            if(body == null)
            {
                session.Write(new CreateASimResponse
                {
                    Status = CreateASimStatus.FAILED,
                    Reason = CreateASimFailureReason.BODY_VALIDATION_ERROR
                });
                return;
            }

            if (!NAME_VALIDATION.IsMatch(packet.Name))
            {
                session.Write(new CreateASimResponse
                {
                    Status = CreateASimStatus.FAILED,
                    Reason = CreateASimFailureReason.NAME_VALIDATION_ERROR
                });
                return;
            }

            if (!DESC_VALIDATION.IsMatch(packet.Description))
            {
                session.Write(new CreateASimResponse
                {
                    Status = CreateASimStatus.FAILED,
                    Reason = CreateASimFailureReason.DESC_VALIDATION_ERROR
                });
                return;
            }

            uint newId = 0;

            using (var db = DAFactory.Get())
            {
                //TODO: Handle unique name errors, enforce avatar limit, enforce per city limit?
                var newAvatar = new DbAvatar();
                newAvatar.shard_id = Context.ShardId;
                newAvatar.name = packet.Name;
                newAvatar.description = packet.Description;
                newAvatar.date = Epoch.Now;
                newAvatar.head = head.OutfitID;
                newAvatar.body = body.OutfitID;
                newAvatar.skin_tone = (byte)packet.SkinTone;
                newAvatar.gender = packet.Gender == Protocol.Voltron.Model.Gender.FEMALE ? DbAvatarGender.female : DbAvatarGender.male;
                newAvatar.user_id = session.UserId;

                newId = db.Avatars.Create(newAvatar);
            }

            ((VoltronSession)session).AvatarId = newId;

            session.Write(new CreateASimResponse {
                Status = CreateASimStatus.SUCCESS,
                NewAvatarId = newId
            });
            session.Write(new TransmitCreateAvatarNotificationPDU { });
        }
    }
}
