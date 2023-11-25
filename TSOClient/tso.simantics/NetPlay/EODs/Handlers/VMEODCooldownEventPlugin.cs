using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.Engine.TSOTransaction;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODCooldownEventPlugin : VMEODHandler
    {
        private bool AlternateCommunityLotRules;
        private bool ConsiderLotCategory;
        private bool LimitByUserAccount;
        private bool UsePluginPersistOnly;

        private TimeSpan CooldownLength;
        private VMEODCooldownEventPluginCommunityData Data;
        private uint OBJGUID;
        private VMEODClient User;

        private readonly Object CommunityLock = new Object();
        private static readonly Object CooldownDBQueryLock = new Object();
        private static readonly Object GetAccountIDLock = new Object();

        public VMEODCooldownEventPlugin(VMEODServer server) : base(server)
        {
            OBJGUID = Server.Object.Object.OBJ.GUID;
        }

        public override void OnConnection(VMEODClient client)
        {
            if (client.Avatar != null)
            {
                User = client;
                var currentTime = Server.vm.Context.Clock.UTCNow;

                // arguments - [0] is type of cooldown: VMEODCooldownEventPluginModes; [1] is cooldown HOURS; [2] is cooldown MINUTES, [3] is cooldown SECONDS
                var args = client.Invoker.Thread.TempRegisters;
                CooldownLength = new TimeSpan(args[1], args[2], args[3]);

                // check cooldown type
                switch (args[0])
                {
                    case (int)VMEODCooldownEventPluginModes.AvatarThisLotOnly:
                        {
                            UsePluginPersistOnly = true;
                            break;
                        }
                    case (int)VMEODCooldownEventPluginModes.AccountThisLotOnly:
                        {
                            UsePluginPersistOnly = true;
                            LimitByUserAccount = true;
                            break;
                        }
                    case (int)VMEODCooldownEventPluginModes.AccountGlobal:
                        {
                            LimitByUserAccount = true;
                            break;
                        }
                    case (int)VMEODCooldownEventPluginModes.AvatarByCategory:
                        {
                            ConsiderLotCategory = true;
                            break;
                        }
                    case (int)VMEODCooldownEventPluginModes.AvatarByCategorySansCommunity:
                        {
                            ConsiderLotCategory = true;
                            AlternateCommunityLotRules = true;
                            break;
                        }
                    case (int)VMEODCooldownEventPluginModes.AccountByCategory:
                        {
                            ConsiderLotCategory = true;
                            LimitByUserAccount = true;
                            break;
                        }
                    case (int)VMEODCooldownEventPluginModes.AccountByCategorySansCommunity:
                        {
                            ConsiderLotCategory = true;
                            LimitByUserAccount = true;
                            AlternateCommunityLotRules = true;
                            break;
                        }
                }

                VMAsyncAccountUserIDFromAvatarCallback callback = null;
                if (UsePluginPersistOnly || AlternateCommunityLotRules && Server.vm.TSOState.CommunityLot)
                {
                    // this lot only, use LoadPluginPersist
                    callback = (uint userID) =>
                    {
                        Server.vm.GlobalLink.LoadPluginPersist(Server.vm, Server.Object.PersistID, Server.PluginID, (byte[] data) =>
                        {
                            lock (CommunityLock)
                            {
                                if (data == null)
                                    Data = new VMEODCooldownEventPluginCommunityData();
                                else
                                {
                                    Data = new VMEODCooldownEventPluginCommunityData(data);

                                    // data found, look for my avatar id or my user id
                                    Tuple<uint, uint, long> tuple = null;

                                    if (LimitByUserAccount)
                                        tuple = Data.AvatarIDUserIDTimeStamps.FirstOrDefault(entry => entry.Item2 == userID);
                                    else
                                        tuple = Data.AvatarIDUserIDTimeStamps.FirstOrDefault(entry => entry.Item1 == client.Avatar.PersistID);

                                    if (tuple != null)
                                    {
                                        var timeStamp = new DateTime(tuple.Item3);
                                        // entry found, check if cooldown has passed since timestamp
                                        if (currentTime < timeStamp)
                                        {
                                            // the specified cooldown has not passed yet
                                            SimanticsResponse((LimitByUserAccount) ? VMEODCooldownEventPluginEvents.LocalFailureForAccount
                                                : VMEODCooldownEventPluginEvents.LocalFailureForAvatar, GetRemainingTime(timeStamp - currentTime));
                                            return;
                                        }
                                        // the cooldown has passed - go to success below
                                        Data.AvatarIDUserIDTimeStamps.Remove(tuple);
                                    }
                                    // my persist ID was not found - go to success below
                                }
                                // my persist ID wasn't found or there was no data loaded from the DB or the cooldown passed
                                Data.AvatarIDUserIDTimeStamps.Add(new Tuple<uint, uint, long>(client.Avatar.PersistID, userID, currentTime.Ticks + CooldownLength.Ticks));
                                Server.vm.GlobalLink.SavePluginPersist(Server.vm, Server.Object.PersistID, Server.PluginID, Data.Save());
                                SimanticsResponse((LimitByUserAccount) ? VMEODCooldownEventPluginEvents.LocalSuccessForAccount
                                                : VMEODCooldownEventPluginEvents.LocalSuccessForAvatar, GetRemainingTime(CooldownLength));
                            }
                        });
                    };
                }
                else // global or categorical
                {
                    callback = (uint userID) =>
                    {
                        lock (CooldownDBQueryLock)
                        {
                            // try to get the data from the database
                            Server.vm.GlobalLink.GetObjectGlobalCooldown(Server.vm, OBJGUID, client.Avatar.PersistID, userID, CooldownLength, LimitByUserAccount, ConsiderLotCategory,
                                (bool? cooldownHasPassed, DateTime cooldownExpiry) =>
                                {
                                    if (cooldownHasPassed == null)
                                        SimanticsResponse(VMEODCooldownEventPluginEvents.DatabaseError, 0);
                                    else if ((bool)cooldownHasPassed)
                                    {
                                        if (LimitByUserAccount)
                                            SimanticsResponse((ConsiderLotCategory) ? VMEODCooldownEventPluginEvents.CategorySuccessForAccount
                                                : VMEODCooldownEventPluginEvents.GlobalSuccessForAccount, GetRemainingTime(CooldownLength));
                                        else
                                            SimanticsResponse((ConsiderLotCategory) ? VMEODCooldownEventPluginEvents.CategorySuccessForAvatar
                                                : VMEODCooldownEventPluginEvents.GlobalSuccessForAvatar, GetRemainingTime(CooldownLength));
                                    }
                                    else
                                    {
                                        if (LimitByUserAccount)
                                            SimanticsResponse((ConsiderLotCategory) ? VMEODCooldownEventPluginEvents.CategoryFailureForAccount
                                                : VMEODCooldownEventPluginEvents.GlobalFailureForAccount, GetRemainingTime(cooldownExpiry - currentTime));
                                        else
                                            SimanticsResponse((ConsiderLotCategory) ? VMEODCooldownEventPluginEvents.CategoryFailureForAvatar
                                                : VMEODCooldownEventPluginEvents.GlobalFailureForAvatar, GetRemainingTime(cooldownExpiry - currentTime));
                                    }
                                });
                        }
                    };
                }
                // finally execute
                lock (GetAccountIDLock)
                {
                    if (!LimitByUserAccount)
                        callback.Invoke(0);
                    else
                        Server.vm.GlobalLink.GetAccountIDFromAvatar(client.Avatar.PersistID, callback);
                }
            }
            base.OnConnection(client);
        }
        private void SimanticsResponse(VMEODCooldownEventPluginEvents type, params short[] args)
        {
            User.SendOBJEvent(new Model.VMEODEvent((short)type, args));
        }
        private short[] GetRemainingTime(TimeSpan remaining)
        {
            var days = remaining.Days;
            var hours = remaining.Hours;
            var minutes = (short)remaining.Minutes;
            var seconds = (short)remaining.Seconds;
            return new short[] { (short)(days * 24 + hours), minutes, seconds };
        }
    }
    internal class VMEODCooldownEventPluginCommunityData : VMSerializable
    {
        internal List<Tuple<uint, uint, long>> AvatarIDUserIDTimeStamps = new List<Tuple<uint, uint, long>>();

        internal VMEODCooldownEventPluginCommunityData() { }

        internal VMEODCooldownEventPluginCommunityData(byte[] data)
        {
            using (var reader = new BinaryReader(new MemoryStream(data)))
            {
                Deserialize(reader);
            }
        }

        public byte[] Save()
        {
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                SerializeInto(writer);
                return stream.ToArray();
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            try
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var persistID = reader.ReadUInt32();
                    var accountID = reader.ReadUInt32();
                    var tickStamp = reader.ReadInt64();
                    AvatarIDUserIDTimeStamps.Add(new Tuple<uint, uint, long>(persistID, accountID, tickStamp));
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                reader.Close();
            }
        }

        public void SerializeInto(BinaryWriter writer)
        {
            foreach (var tuple in AvatarIDUserIDTimeStamps)
            {
                writer.Write(tuple.Item1);
                writer.Write(tuple.Item2);
                writer.Write(tuple.Item3);
            }
        }
    }
    public enum VMEODCooldownEventPluginModes: short
    {
        // Cooldown for object applies only to the lot of the VM; the database won't be used at all in favor of LoadPluginPersist - by avatar persistID
        AvatarThisLotOnly = 0,
        // Cooldown for object applies only to the lot of the VM; the database provides the user (user_id), but persisted in SavePluginPersist/LoadPluginPersist
        AccountThisLotOnly = 1,
        // Cooldown for object is checked against avatar persistID (avatar_id) across all lots in shard
        AvatarGlobal = 2,
        // Cooldown for object is checked against account (user_id) instead of avatar persistID (avatar_id) across all lots in shard
        AccountGlobal = 3,
        // Cooldown for object is checked against avatar persistID (avatar_id) and FSO.Common.Enum.LotCategory value of VM
        AvatarByCategory = 4,
        // Cooldown for object is checked against avatar persistID (avatar_id) and FSO.Common.Enum.LotCategory value of VM; Community lots use LoadPluginPersist
        AvatarByCategorySansCommunity = 5,
        // Cooldown for object is checked against account (user_id) and FSO.Common.Enum.LotCategory value of VM
        AccountByCategory = 6,
        // Cooldown for object is checked against account (user_id) and FSO.Common.Enum.LotCategory value of VM; Community lots use LoadPluginPersist
        AccountByCategorySansCommunity = 7
    }
    public enum VMEODCooldownEventPluginEvents: short
    {
        Idle = 0,
        // Data errors
        DatabaseError = 7,
        PluginPersistDataError = 8,
        // failures, Global meaning for this object type on any and all lots
        GlobalFailureForAvatar = 5,
        GlobalFailureForAccount = 6,
        // failures, Category meaning this lot category
        CategoryFailureForAvatar = 3,
        CategoryFailureForAccount = 4,
        // failures, Local meaning this lot only
        LocalFailureForAvatar = 1,
        LocalFailureForAccount = 2,
        // successes, Local meaning this lot only
        LocalSuccessForAvatar = 101,
        LocalSuccessForAccount = 102,
        // successes, Category meaning this lot category
        CategorySuccessForAvatar = 103,
        CategorySuccessForAccount = 104,
        // successes, Global meaning for this object type on any and all lots
        GlobalSuccessForAvatar = 105,
        GlobalSuccessForAccount = 106
    }
}