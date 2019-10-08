using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Files.Formats.tsodata;
using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.DbEvents;
using FSO.Server.Database.DA.Tasks;
using FSO.Server.Domain;
using FSO.Server.Protocol.Gluon.Packets;

namespace FSO.Server.Servers.Tasks.Domain
{
    public class BirthdayGiftTask : ITask
    {
        private IDAFactory DAFactory;
        private TaskTuning Tuning;
        private IGluonHostPool HostPool;

        public BirthdayGiftTask(IDAFactory DAFactory, TaskTuning tuning, IGluonHostPool hostPool)
        {
            this.DAFactory = DAFactory;
            this.Tuning = tuning;
            this.HostPool = hostPool;
        }

        public void Abort()
        {
        }

        public DbTaskType GetTaskType()
        {
            return DbTaskType.birthday_gift;
        }

        public void Run(TaskContext context)
        {
            //ensure the task is configured
            var tuning = Tuning.BirthdayGift;
            if (tuning == null) return;

            using (var da = DAFactory.Get())
            {
                const float dayLength = (24 * 60 * 60);
                //get a list of all avatars and their ages
                var avatars = da.Avatars.All().ToList();
                var now = Epoch.Now;
                var mailItems = new List<MessageItem>();

                //iterate all the gift items
                foreach (var item in tuning.items)
                {
                    var targAge = item.age;
                    
                    var eventName = "bday_" + item.age;
                    var alreadyAwardedUsers = new HashSet<uint>(da.Events.GetGenericParticipatingAvatars(eventName));
                    var toBeAwarded = avatars.Where(x => {
                        var daysOld = (now - x.date) / dayLength;
                        return daysOld > targAge && !alreadyAwardedUsers.Contains(x.avatar_id);
                        });

                    //attempt to give all 'to be awarded' avatars their awards
                    foreach (var avatar in toBeAwarded)
                    {
                        if (da.Events.GenericAvaTryParticipate(new DbGenericAvatarParticipation() { participation_name = eventName, participation_avatar = avatar.avatar_id}))
                        {
                            //award the object
                            da.Objects.Create(new Database.DA.Objects.DbObject()
                            {
                                type = item.guid,
                                shard_id = avatar.shard_id,
                                owner_id = avatar.avatar_id,
                                lot_id = null, //to inventory
                                dyn_obj_name = ""
                            });

                            //send email
                            if (item.mail_message != null)
                            {
                                mailItems.Add(new MessageItem()
                                {
                                    Subject = item.mail_subject ?? "Age Gift",
                                    Body = item.mail_message,
                                    SenderID = uint.MaxValue,
                                    SenderName = item.mail_sender_name ?? "The Sims Online",
                                    TargetID = avatar.avatar_id,
                                    Type = 4,
                                    Subtype = 0
                                });
                            }
                        }
                    }
                }

                if (mailItems.Count > 0)
                {
                    //TODO: MULTI-CITY - select mail target by shard id
                    var cityServers = HostPool.GetByRole(Database.DA.Hosts.DbHostRole.city);

                    foreach (var city in cityServers)
                    {
                        city.Write(new SendCityMail(mailItems));
                    }
                }
            }
        }
    }

    public class BirthdayGiftTaskTuning
    {
        public List<BirthdayGiftItem> items { get; set; } = new List<BirthdayGiftItem>();
    }

    public class BirthdayGiftItem
    {
        public float age { get; set; }
        public uint guid { get; set; }
        public string mail_subject { get; set; }
        public string mail_message { get; set; }
        public string mail_sender_name { get; set; }
    }
}
