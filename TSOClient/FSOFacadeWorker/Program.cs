using FSO.Client.UI.Panels.WorldUI;
using FSO.Client.Utils;
using FSO.Client.Utils.GameLocator;
using FSO.Common;
using FSO.Common.Rendering.Framework;
using FSO.Common.Utils;
using FSO.Content;
using FSO.Files.RC;
using FSO.LotView;
using FSO.LotView.Facade;
using FSO.Server.Clients;
using FSO.SimAntics;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Drivers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSOFacadeWorker
{
    class Program
    {
        private static _3DLayer Layer;
        private static GraphicsDevice GD;

        private static FacadeConfig Config;

        static void Main(string[] args)
        {
            FSO.Windows.Program.InitWindows();
            TimedReferenceController.SetMode(CacheType.PERMANENT);

            Console.WriteLine("Loading Config...");
            try
            {
                var configString = File.ReadAllText("facadeconfig.json");
                Config = Newtonsoft.Json.JsonConvert.DeserializeObject<FacadeConfig>(configString);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not find configuration file 'facadeconfig.json'. Please ensure it is valid and present in the same folder as this executable.");
                return;
            }

            Console.WriteLine("Locating The Sims Online...");
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(baseDir);
            //Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);

            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;

            ILocator gameLocator;
            bool linux = pid == PlatformID.MacOSX || pid == PlatformID.Unix;
            if (linux && Directory.Exists("/Users"))
                gameLocator = new MacOSLocator();
            else if (linux)
                gameLocator = new LinuxLocator();
            else
                gameLocator = new WindowsLocator();

            bool useDX = true;

            FSOEnvironment.Enable3D = true;
            GameThread.NoGame = true;
            GameThread.UpdateExecuting = true;

            var path = gameLocator.FindTheSimsOnline();

            if (path != null)
            {
                FSOEnvironment.ContentDir = "Content/";
                FSOEnvironment.GFXContentDir = "Content/" + (useDX ? "DX/" : "OGL/");
                FSOEnvironment.Linux = linux;
                FSOEnvironment.DirectX = useDX;
                FSOEnvironment.GameThread = Thread.CurrentThread;

                FSO.HIT.HITVM.Init();
                FSO.HIT.HITVM.Get().SetMasterVolume(FSO.HIT.Model.HITVolumeGroup.AMBIENCE, 0);
                FSO.HIT.HITVM.Get().SetMasterVolume(FSO.HIT.Model.HITVolumeGroup.FX, 0);
                FSO.HIT.HITVM.Get().SetMasterVolume(FSO.HIT.Model.HITVolumeGroup.MUSIC, 0);
                FSO.HIT.HITVM.Get().SetMasterVolume(FSO.HIT.Model.HITVolumeGroup.VOX, 0);
                FSO.Files.Formats.IFF.Chunks.STR.DefaultLangCode = FSO.Files.Formats.IFF.Chunks.STRLangCode.EnglishUS;
            }

            Console.WriteLine("Creating Graphics Device...");
            var gds = new GraphicsDeviceServiceMock();
            var gd = gds.GraphicsDevice;

            //set up some extra stuff like the content manager
            var services = new GameServiceContainer();
            var content = new ContentManager(services);
            content.RootDirectory = FSOEnvironment.GFXContentDir;
            services.AddService<IGraphicsDeviceService>(gds);

            var vitaboyEffect = content.Load<Effect>("Effects/Vitaboy");
            FSO.Vitaboy.Avatar.setVitaboyEffect(vitaboyEffect);

            WorldConfig.Current = new WorldConfig()
            {
                LightingMode = 3,
                SmoothZoom = true,
                SurroundingLots = 0
            };
            DGRP3DMesh.Sync = true;

            Console.WriteLine("Looks like that worked. Loading FSO Content!");
            VMContext.InitVMConfig(false);
            Content.Init(path, gd);
            WorldContent.Init(services, content.RootDirectory);
            VMAmbientSound.ForceDisable = true;
            Layer = new _3DLayer();
            Layer.Initialize(gd);
            GD = gd;

            Console.WriteLine("Starting Worker Loop!");
            WorkerLoop();

            Console.WriteLine("Exiting.");
            GameThread.Killed = true;
            GameThread.OnKilled.Set();
            gds.Release();
        }

        public static ApiClient api;
        public static int TotalLotNum = 0;
        public static List<uint> LotQueue = new List<uint>();
        private static bool LoginSent;
        private static int Done;

        public static void Login()
        {
            api = new ApiClient(Config.Api_Url);
            api.AdminLogin(Config.User, Config.Password, (result) =>
            {
                if (!result)
                {
                    Console.WriteLine("Login Failed! Trying again in a wee bit.");
                    LoginSent = false;
                }
                else
                {
                    api.GetLotList(1, (lots) =>
                    {
                        Console.WriteLine("Got a lot list for full thumbnail rebake.");
                        //LotQueue.AddRange(lots);
                        //TotalLotNum += lots.Length;
                        //for (int i = 0; i < 4000; i++)
                        //{
                        //    LotQueue.RemoveAt(0);
                        //}
                        RenderLot();
                        RenderLot();
                    });
                }

            });
        }

        

        private static void RenderLot()
        {

            Console.WriteLine("Requesting work...");

            api.GetWork((shard, location) =>
            {
                if (shard == -1)
                {
                    //no work
                    if (location == uint.MaxValue)
                    {
                        //error, try logging in again
                        LoginSent = false;
                        GameThread.OnWork.Set();
                        return;
                    } else
                    {
                        if (Config.Sleep_Time == 0)
                        {
                            //exit when no work remains
                            Environment.Exit(0);
                            return;
                        }

                        //no work, try again in 30 s
                        Thread.Sleep(Config.Sleep_Time);
                        RenderLot();
                        return;
                    }
                }

                api.GetFSOV((uint)shard, location, (bt) =>
                {
                    try
                    {
                        if (bt == null)
                        {
                            RenderLot();
                        }
                        else
                        {
                            Console.WriteLine("Rendering lot " + location + "...");
                            var fsof = RenderFSOF(bt, GD);
                            using (var mem = new MemoryStream())
                            {
                                fsof.Save(mem);
                                Console.WriteLine("Done! Uploading FSOF for lot " + location + ".");

                                //File.WriteAllBytes("C:/fsof/" + lot + ".fsof", mem.ToArray());
                                //RenderLot();
                                api.UploadFSOF(1, location, mem.ToArray(), (success) =>
                                {
                                    if (!success)
                                    {
                                        Console.WriteLine("Uploading fsof for " + location + " did not succeed.");
                                    }

                                    if (Done++ > Config.Limit)
                                    {
                                        Console.WriteLine("Restarting due to large number of thumbnails rendered.");
                                        Environment.Exit(0);
                                        return;
                                    }
                                    RenderLot();
                                });
                            }
                        }
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("===== Could not render lot " + location + "! =====");
                        Console.WriteLine(e.ToString());
                        RenderLot();
                    }
                });
            });
        }

        public static void WorkerLoop()
        {
            int loggedIn = 0;
            int loginAttempts = 0;
            while (true)
            {
                if (!LoginSent)
                {
                    LoginSent = true;
                    Console.WriteLine("Attempting Login... ("+(loginAttempts++)+")");
                    Login();
                }
                GameThread.OnWork.WaitOne(1000);
                GameThread.DigestUpdate(null);
            }
            
        }

        public static FSOF RenderFSOF(byte[] fsov, GraphicsDevice gd)
        {
            var marshal = new VMMarshal();
            using (var mem = new MemoryStream(fsov))
            {
                marshal.Deserialize(new BinaryReader(mem));
            }

            var world = new FSO.LotView.RC.WorldRC(gd);
            world.Opacity = 1;
            Layer.Add(world);

            var globalLink = new VMTSOGlobalLinkStub();
            var driver = new VMServerDriver(globalLink);

            var vm = new VM(new VMContext(world), driver, new VMNullHeadlineProvider());
            vm.Init();

            vm.Load(marshal);

            SetOutsideTime(gd, vm, world, 0.5f, false);
            world.State.PrepareLighting();
            var facade = new LotFacadeGenerator();
            facade.FLOOR_TILES = 64;
            facade.GROUND_SUBDIV = 5;
            facade.FLOOR_RES_PER_TILE = 2;

            SetAllLights(vm, world, 0.5f, 0);

            var result = facade.GetFSOF(gd, world, vm.Context.Blueprint, () => { SetAllLights(vm, world, 0.0f, 100); }, true);

            Layer.Remove(world);
            world.Dispose();
            vm.Context.Ambience.Kill();
            foreach (var ent in vm.Entities)
            { //stop object sounds
                var threads = ent.SoundThreads;
                for (int i = 0; i < threads.Count; i++)
                {
                    threads[i].Sound.RemoveOwner(ent.ObjectID);
                }
                threads.Clear();
            }

            return result;
        }

        private static void SetAllLights(VM vm, World world, float outsideTime, short contribution)
        {
            foreach (var light in vm.Entities.Where(x => x.Object.Resource.SemiGlobal?.Iff?.Filename == "lightglobals.iff"))
            {
                light.SetValue(FSO.SimAntics.Model.VMStackObjectVariable.LightingContribution, contribution);
            }
            vm.Context.Architecture.SignalAllDirty();
            vm.Context.Architecture.Tick();
            SetOutsideTime(GD, vm, world, outsideTime, false);
        }

        private static void SetOutsideTime(GraphicsDevice gd, VM vm, World world, float time, bool lightsOn)
        {
            vm.Context.Architecture.SetTimeOfDay(time);
            world.Force2DPredraw(gd);
            vm.Context.Architecture.SetTimeOfDay();
        }
    }
}
