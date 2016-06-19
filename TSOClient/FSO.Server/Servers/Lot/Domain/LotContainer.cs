using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using FSO.SimAntics;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using Ninject;
using Ninject.Extensions.ChildKernel;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Lot.Domain
{
    /// <summary>
    /// 
    /// </summary>
    public class LotContainer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private IDAFactory DAFactory;
        private LotContext Context;
        private ILotHost Host;

        private VM Lot;
        private VMServerDriver VMDriver;
        
        public LotContainer(IDAFactory da, LotContext context, ILotHost host)
        {
            VM.UseWorld = false;
            DAFactory = da;
            Host = host;
            Context = context;

            ResetVM();
        }

        public void ResetVM()
        { 
            VMDriver = new VMServerDriver(new VMTSOGlobalLinkStub());
            VMDriver.OnTickBroadcast += TickBroadcast;
            VMDriver.OnDirectMessage += DirectMessage;

            var vm = new VM(new VMContext(null), VMDriver, new VMNullHeadlineProvider());
            Lot = vm;
            vm.Init();

            
            var path = Content.Content.Get().GetPath("housedata/blueprints/playtest_00.xml");
            string filename = Path.GetFileName(path);
            /*
            try
            {
                //try to load from FSOV first.
                LoadState(vm, "Content/LocalHouse/" + filename.Substring(0, filename.Length - 4) + ".fsov");
            }
            catch (Exception)
            {
                try
                {
                    Console.WriteLine("Failed FSOV load... Trying Backup");
                    LoadState(vm, "Content/LocalHouse/" + filename.Substring(0, filename.Length - 4) + "_backup.fsov");
                }
                catch (Exception)
                {
                    Console.WriteLine("CRITICAL::: Failed FSOV load... Trying Blueprint (first run or something went wrong)");
                    */
                    short jobLevel = -1;

                    //quick hack to find the job level from the chosen blueprint
                    //the final server will know this from the fact that it wants to create a job lot in the first place...

                    try
                    {
                        if (filename.StartsWith("nightclub") || filename.StartsWith("restaurant") || filename.StartsWith("robotfactory"))
                            jobLevel = Convert.ToInt16(filename.Substring(filename.Length - 9, 2));
                    }
                    catch (Exception) { }
                    vm.SendCommand(new VMBlueprintRestoreCmd
                    {
                        JobLevel = jobLevel,
                        XMLData = File.ReadAllBytes(path)
                    });

                    vm.Context.Clock.Hours = 10;
            /*
                }
            }
            */
            vm.MyUID = uint.MaxValue - 1;
        }

        public void Message(IVoltronSession session, FSOVMCommand cmd)
        {
            VMDriver.SubmitMessage(session.AvatarId, new VMNetMessage(VMNetMessageType.Command, cmd.Data));
        }

        private void DirectMessage(VMNetClient target, VMNetMessage msg)
        {
            object packet = (msg.Type == VMNetMessageType.Direct) ?
                (object)(new FSOVMDirectToClient() { Data = msg.Data })
                : (object)(new FSOVMTickBroadcast() { Data = msg.Data });
            Host.Send(target.PersistID, packet);
        }

        private void TickBroadcast(VMNetMessage msg, HashSet<VMNetClient> ignore)
        {
            HashSet<uint> ignoreIDs = new HashSet<uint>(ignore.Select(x => x.PersistID));
            Host.Broadcast(ignoreIDs, new FSOVMTickBroadcast() { Data = msg.Data });
        }

        /// <summary>
        /// Load and initialize everything to start up the lot
        /// </summary>
        public void Run()
        {
            LOG.Info("Starting to host lot with dbid = " + Context.DbId);
            Host.SetOnline(true);

            var timeKeeper = new Stopwatch(); //todo: smarter timing
            timeKeeper.Start();
            long lastMs = 0;
            while (true)
            {
                lastMs += 16;
                //TicksSinceSave++;
                try
                {
                    Lot.Update();
                }
                catch (Exception e)
                {
                    /*state.CloseNet(VMCloseNetReason.Unspecified);
                    Console.WriteLine(e.ToString());
                    SaveLot();
                    Thread.Sleep(500);

                    ResetVM();
                    //restart on exceptions... but print them to console
                    //just for people who like 24/7 servers.
                    */
                }

                /*
                if (TicksSinceSave > SaveTickFreq)
                {
                    //quick and dirty periodic save
                    SaveLot();
                    TicksSinceSave = 0;
                }*/

                Thread.Sleep((int)Math.Max(0, (lastMs + 16) - timeKeeper.ElapsedMilliseconds));
            }
        }

        //Run on the background thread
        public void AvatarJoin(IVoltronSession session)
        {
            using (var da = DAFactory.Get())
            {
                var avatar = da.Avatars.Get(session.AvatarId);
                LOG.Info("Avatar " + avatar.name + " has joined");

                //Load all the avatars data
                var state = new VMNetAvatarPersistState();
                state.Name = avatar.name;
                state.PersistID = session.AvatarId;
                state.DefaultSuits = new SimAntics.VMAvatarDefaultSuits(avatar.gender == DbAvatarGender.female);
                state.DefaultSuits.Daywear = avatar.body;
                state.BodyOutfit = avatar.body;
                state.HeadOutfit = avatar.head;
                state.Gender = (short)avatar.gender;

                state.SkinTone = avatar.skin_tone;

                state.Permissions = SimAntics.Model.TSOPlatform.VMTSOAvatarPermissions.Owner;

                var client = new VMNetClient();
                client.AvatarState = state;
                client.RemoteIP = session.IpAddress;
                client.PersistID = session.AvatarId;

                VMDriver.ConnectClient(client);
            }
        }

        //Run on the background thread
        public void AvatarLeave(IVoltronSession session)
        {
            //Exit lot, Persist the avatars data, remove avatar lock
            LOG.Info("Avatar left");
            VMDriver.DisconnectClient(session.AvatarId);
            Host.ReleaseAvatarClaim(session);
        }

    }
}
