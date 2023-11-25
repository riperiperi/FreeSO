using System.Collections.Generic;
using System.Linq;
using FSO.SimAntics.NetPlay.EODs.Model;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODAbstractGameshowBuzzerPlugin : VMEODHandler
    {
        internal VMEODGameshowBuzzerPluginType EODType = VMEODGameshowBuzzerPluginType.Unknown;
        internal delegate void ClientConnectionChange();
        internal static event ClientConnectionChange ClientDisconnect;
        internal static event ClientConnectionChange ClientConnect;
        internal delegate void GameMechanicEvent(VMEODGameshowBuzzerPlayerPlugin invokingPlayer);
        internal static event GameMechanicEvent PlayerBuzzed;
        internal static event GameMechanicEvent SyncEvent;
        protected List<VMEODEvent> SimanticsQueue;

        protected VMEODClient Controller;

        public static readonly short MAX_ANSWER_TIME = 120;
        public static readonly short MIN_ANSWER_TIME = 2;
        public static readonly short MAX_BUZZER_TIME = 120;
        public static readonly short MIN_BUZZER_TIME = 2;
        public static readonly short MAX_SCORE = 9999;
        public static readonly int ACTIVE_BUZZER_WINDOW_TICKS = 30;

        protected List<VMEODGameshowBuzzerPlayerPlugin> PotentialContestants
        {
            get
            {
                return Server.vm.EODHost.GetHandlers<VMEODGameshowBuzzerPlayerPlugin>().Where((handler) => handler.EODType.Equals(VMEODGameshowBuzzerPluginType.Player)).ToList();
            }
        
        }
        public VMEODAbstractGameshowBuzzerPlugin(VMEODServer server) : base(server)
        {
            SimanticsQueue = new List<VMEODEvent>();

        }

        public override void OnConnection(VMEODClient client)
        {
            if (client.Avatar != null)
            {
                if (EODType.Equals(VMEODGameshowBuzzerPluginType.Player)) // player connected to player object
                {
                    // new player has connected event
                    ClientConnect?.Invoke();
                }
            }
            else
                Controller = client;
            SyncEvent += SyncExecutor;
            base.OnConnection(client);
        }
        public override void OnDisconnection(VMEODClient client)
        {
            if (EODType.Equals(VMEODGameshowBuzzerPluginType.Player)) // player disconnected
            {
                EODType = VMEODGameshowBuzzerPluginType.Unknown;
                ClientDisconnect?.Invoke();
            }
            SyncEvent -= SyncExecutor;
            base.OnDisconnection(client);
        }

        protected void EnqueueEvent(VMEODEvent evt)
        {
            lock (SimanticsQueue)
                SimanticsQueue.Add(evt);
        }

        protected void ExecuteQueuedEvents()
        {
            lock (SimanticsQueue)
            {
                var queue = new List<VMEODEvent>(SimanticsQueue);
                SimanticsQueue.Clear();
                foreach (var evt in queue)
                    Controller.SendOBJEvent(evt);
            }
        }

        #region events

        protected void PlayerBuzzedEventHandler(VMEODGameshowBuzzerPlayerPlugin buzzingPlayerPlugin)
        {
            PlayerBuzzed?.Invoke(buzzingPlayerPlugin);
        }
        protected void SyncHandler(VMEODGameshowBuzzerPlayerPlugin syncToPlayer)
        {
            SyncEvent?.Invoke(syncToPlayer);   
        }
        protected void SyncExecutor(VMEODGameshowBuzzerPlayerPlugin evoker)
        {
            ExecuteQueuedEvents();
        }

        #endregion
    }

    public enum VMEODGameshowBuzzerPluginType : byte
    {
        Unknown = 0,
        Host = 1,
        Player = 2
    }

    public enum VMEODGameshowBuzzerPluginErrors : byte
    {
        H_NoNewPlayerError = 31,
        H_NoChangesAllowed = 32,
        H_OptionUnderflow = 33,
        H_OptionOverflow = 34
    }

    public enum VMEODGameshowBuzzerPluginEODTips : byte
    {
        H_Player_Timeout = 38,
        P_Waiting = 24,
        P_Ready = 25
    }
}
