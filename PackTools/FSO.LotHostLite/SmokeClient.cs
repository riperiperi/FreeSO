using FSO.Content;
using FSO.Common.Utils;
using FSO.LotHostLite.Sandbox;
using FSO.SimAntics;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.LotHostLite
{
    /// <summary>
    /// Headless lockstep client for LotHostLite: FSOSandboxClient + VMClientDriver
    /// + a full local VM, wired exactly like desktop SandboxGameScreen's external
    /// mode. Joins, waits for the state sync, prints entity hashes, and can send
    /// chat / run a pie-menu interaction so a second client can observe them.
    /// </summary>
    public static class SmokeClient
    {
        public static int Run(string[] args)
        {
            string Arg(string name, string def = null)
            {
                for (int i = 0; i < args.Length - 1; i++)
                    if (args[i] == name) return args[i + 1];
                return def;
            }

            var connect = Arg("--connect", "127.0.0.1:37564");
            var tsoDir = Arg("--tso-dir") ?? throw new ArgumentException("--tso-dir required");
            var name = Arg("--name", "smoke" + new Random().Next(1000));
            var chat = Arg("--chat");
            var interactGuid = Arg("--interact"); // pack object GUID, e.g. 0x6B4F0003
            var ticks = int.Parse(Arg("--ticks", "300"));
            if (!tsoDir.EndsWith(Path.DirectorySeparatorChar.ToString())) tsoDir += Path.DirectorySeparatorChar;

            Program.BootContent(tsoDir, Arg("--packs"), "fso-smoke-" + name, args.Contains("--bare-objects"));

            Console.WriteLine($"[{name}] booting client VM...");
            VM.UseWorld = false;
            VMContext.InitVMConfig(false);
            FSO.Content.Content.Init(tsoDir, ContentMode.SERVER);

            var driver = new VMClientDriver((state, progress) =>
                Console.WriteLine($"[{name}] net state {state} ({progress:F2})"));
            var cli = new FSOSandboxClient();
            driver.OnClientCommand += (msg) => cli.Write(new VMNetMessage(VMNetMessageType.Command, msg));
            driver.OnShutdown += (reason) => cli.Disconnect();
            cli.OnMessage += driver.ServerMessage;

            var persistID = (uint)new Random().Next(1000, int.MaxValue);
            var myState = new VMNetAvatarPersistState()
            {
                Name = name,
                DefaultSuits = new VMAvatarDefaultSuits(false),
                BodyOutfit = 0x24C0000000D,
                HeadOutfit = 0x000000000D,
                PersistID = persistID,
                SkinTone = 0,
                Gender = 1,
                Permissions = VMTSOAvatarPermissions.Admin,
                Budget = 1000000,
            };

            cli.OnConnectComplete += () =>
            {
                Console.WriteLine($"[{name}] connected, sending AvatarData persist={persistID}");
                var dat = new MemoryStream();
                var str = new BinaryWriter(dat);
                myState.SerializeInto(str);
                cli.Write(new VMNetMessage(VMNetMessageType.AvatarData, dat.ToArray()));
                dat.Close();
            };

            var vm = new VM(new VMContext(null), driver, new VMNullHeadlineProvider());
            vm.Init();
            vm.MyUID = persistID;
            var chatLog = new List<string>();
            vm.OnChatEvent += (evt) =>
            {
                var text = evt.Text is string[] arr ? string.Join(" | ", arr) : evt.Text?.ToString();
                var line = $"chat[{evt.SenderUID}] {text}";
                chatLog.Add(line);
                Console.WriteLine($"[{name}] {line}");
            };

            Console.WriteLine($"[{name}] connecting {connect}...");
            cli.Connect(connect);

            bool synced = false;
            bool chatSent = false, interactSent = false, interactSeenInQueue = false;
            long tick = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (tick < ticks)
            {
                GameThread.UpdateExecuting = true;
                GameThread.DigestUpdate(new FSO.Common.Rendering.Framework.Model.UpdateState());
                vm.Tick();
                GameThread.UpdateExecuting = false;
                tick++;

                if (!synced && vm.Context.Architecture != null && vm.Entities.Count > 0)
                {
                    synced = true;
                    Console.WriteLine($"[{name}] SYNCED at local tick {tick}: {vm.Entities.Count} entities, " +
                        $"arch {vm.Context.Architecture.Width}x{vm.Context.Architecture.Height}, hash={Program.EntityHash(vm)}");
                }

                // The avatar arrives via our SimJoin echo a few ticks after sync, and is
                // transiently Hidden while its walk-in plays — wait it out.
                if (synced && interactGuid != null && !interactSent)
                {
                    var myAva = FindMyAvatar(vm, persistID);
                    if (myAva != null &&
                        myAva.GetValue(FSO.SimAntics.Model.VMStackObjectVariable.Hidden) == 0)
                    {
                        Console.WriteLine($"[{name}] my avatar: objID={myAva.ObjectID} at {myAva.Position}");
                        var guid = Convert.ToUInt32(interactGuid.Replace("0x", ""), 16);
                        var target = vm.Entities.FirstOrDefault(e => e.Object?.OBJ?.GUID == guid);
                        interactSent = true; // one attempt either way
                        if (target == null) Console.WriteLine($"[{name}] INTERACT TARGET {interactGuid} NOT IN VM");
                        else
                        {
                            var pie = target.GetPieMenu(vm, myAva, false, true);
                            Console.WriteLine($"[{name}] pie menu on {interactGuid}: " +
                                (pie.Count == 0 ? "(empty)" : string.Join(", ", pie.Select(p => $"{p.ID}:{p.Name}"))));
                            if (pie.Count == 0)
                            {
                                var hidden = target.GetPieMenu(vm, myAva, true, false);
                                Console.WriteLine($"[{name}] piedbg TreeTable={(target.TreeTable == null ? "NULL" : target.TreeTable.Interactions.Length.ToString())} " +
                                    $"TTAs={(target.TreeTableStrings == null ? "NULL" : "ok")} withHidden={hidden.Count} " +
                                    $"targetPos={target.Position} callerPos={myAva.Position} " +
                                    $"callerHidden={myAva.GetValue(FSO.SimAntics.Model.VMStackObjectVariable.Hidden)} " +
                                    $"hideInteraction={myAva.GetValue(FSO.SimAntics.Model.VMStackObjectVariable.HideInteraction)}");
                                if (hidden.Count > 0) pie = hidden;
                            }
                            if (pie.Count > 0)
                            {
                                vm.SendCommand(new VMNetInteractionCmd
                                {
                                    Interaction = pie[0].ID,
                                    CalleeID = target.ObjectID,
                                    Global = false,
                                });
                                Console.WriteLine($"[{name}] sent interaction {pie[0].ID} ({pie[0].Name})");
                            }
                        }
                    }
                }

                if (synced && chat != null && !chatSent && tick % 30 == 0)
                {
                    chatSent = true;
                    vm.SendCommand(new VMNetChatCmd { Message = chat });
                    Console.WriteLine($"[{name}] sent chat: {chat}");
                }

                if (synced && tick % 100 == 0)
                    Console.WriteLine($"[{name}] tick={tick} entities={vm.Entities.Count} hash={Program.EntityHash(vm)}");

                if (interactSent && !interactSeenInQueue)
                {
                    var ava3 = FindMyAvatar(vm, persistID);
                    var act = ava3?.Thread.Queue.FirstOrDefault(q => q.Name != null && q.Name != "Idle");
                    if (act != null)
                    {
                        interactSeenInQueue = true;
                        Console.WriteLine($"[{name}] INTERACTION IN QUEUE at tick {tick}: {act.Name} " +
                            $"(mode {act.Mode}, priority {act.Priority})");
                    }
                }

                var target2 = tick * 33;
                var wait = target2 - sw.ElapsedMilliseconds;
                if (wait > 0) Thread.Sleep((int)wait);
            }

            var myAva2 = FindMyAvatar(vm, persistID);
            var interactState = "n/a";
            if (myAva2 is VMAvatar ava)
            {
                interactState = $"queue=[{string.Join("; ", ava.Thread.Queue.Select(q => q.Name ?? q.ActionRoutine?.ID.ToString()))}] " +
                    $"kill={ava.KillTimeout} hidden={ava.GetValue(FSO.SimAntics.Model.VMStackObjectVariable.Hidden)} " +
                    $"ghost={ava.GetPersonData(FSO.SimAntics.Model.VMPersonDataVariable.IsGhost)} pos={ava.Position}";
            }

            Console.WriteLine($"[{name}] FINAL synced={synced} entities={vm.Entities.Count} hash={Program.EntityHash(vm)} " +
                $"chatSeen={chatLog.Count} interact={interactState}");
            return synced ? 0 : 1;
        }

        static VMAvatar FindMyAvatar(VM vm, uint persistID)
        {
            return vm.Entities.OfType<VMAvatar>().FirstOrDefault(a => a.PersistID == persistID);
        }
    }
}
