using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODGameshowBuzzerPlugin : VMEODHandler
    {
        internal VMEODGameshowBuzzerPluginType EODType = VMEODGameshowBuzzerPluginType.Unknown;
        internal bool BuzzerEnabled { get; set; }
        internal double ClientListRefreshedStamp { get; set; }
        internal double SearchStamp { get; set; }
        internal double SessionStamp { get; set; }
        internal delegate void ClientConnectionChange();
        internal static event ClientConnectionChange ClientDisconnect;
        internal static event ClientConnectionChange ClientConnect;
        private VMEODGameshowBuzzerPlugin[] ConnectedContestants = new VMEODGameshowBuzzerPlugin[4];
        internal short MyScore { get; private set; }

        internal VMEODClient MyPlayerClient
        {
            get
            {
                return Server.Clients.FirstOrDefault((client) => (client.Avatar != null));
            }
        }
        
        private List<VMEODGameshowBuzzerPlugin> PotentialContestants
        {
            get
            {
                return Server.vm.EODHost.GetHandlers<VMEODGameshowBuzzerPlugin>().Where((handler) => handler.EODType.Equals(VMEODGameshowBuzzerPluginType.Player)).ToList();
            }
        }
        private readonly object PlayerConfigLock = new object();

        public VMEODGameshowBuzzerPlugin(VMEODServer server) : base(server)
        {

            BinaryHandlers["Buzzer_Host_FindNewPlayer"] = FindNewPlayerHandler;

        }

        public override void OnConnection(VMEODClient client)
        {
            if (client.Avatar != null) { // ignore the Controller
                if (Server.Object.Object.GUID > 0 /*1925646868*/) // host connected to host object
                {
                    EODType = VMEODGameshowBuzzerPluginType.Host;
                    SessionStamp = ClientListRefreshedStamp = System.DateTime.Now.ToOADate();

                    // subscribe to player connection events
                    ClientConnect += NewClientConnectedHandler;
                    ClientDisconnect += ValidateConnectedContestants;

                    // find players already connected
                    FillContestantSpots();
                }
                else // player connected to player object
                {
                    EODType = VMEODGameshowBuzzerPluginType.Player;
                    BuzzerEnabled = false;
                    // get the score from the object's via tempregisters
                    MyScore = client.Invoker.Thread.TempRegisters[0];

                    // new player has connected event
                    ClientConnect.Invoke();
                }
                client.Send("Buzzer_UIEOD_Init", new byte[] { (byte)EODType });
                base.OnConnection(client);
            }
        }
        public override void OnDisconnection(VMEODClient client)
        {
            if (EODType.Equals(VMEODGameshowBuzzerPluginType.Player)) // player disconnected
            {
                EODType = VMEODGameshowBuzzerPluginType.Unknown;
                ClientDisconnect.Invoke();
            }
            else // host disconnected
            {
                ClientConnect -= NewClientConnectedHandler;
                ClientDisconnect -= ValidateConnectedContestants;
            }
            base.OnDisconnection(client);
        }

        #region events

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newScore"></param>
        internal void ExecuteNewScoreEvent(short newScore)
        {
            MyScore = newScore;
            // execute Simantics event to display new score on this object

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="playerIndex"></param>
        /// <param name="host"></param>
        private void FindNewPlayerHandler(string evt, byte[] playerIndex, VMEODClient host)
        {
            var index = (int)playerIndex[0];
            if (index > -1 && index < 4)
                FillContestantSpot(index, true);
        }
        /// <summary>
        /// 
        /// </summary>
        private void NewClientConnectedHandler()
        {
            lock (PlayerConfigLock)
            {
                ClientListRefreshedStamp = System.DateTime.Now.ToOADate();
                FillContestantSpots();
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        private void FillContestantSpots()
        {
            ValidateConnectedContestants();
            lock (PlayerConfigLock)
            {
                for (int index = 0; index < ConnectedContestants.Length; index++)
                {
                    if (ConnectedContestants[index] == null)
                        FillContestantSpot(index, false);
                }
            }
            SendContestantRoster();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="viaSearchbtn"></param>
        private void FillContestantSpot(int index, bool viaSearchbtn)
        {
            lock (ConnectedContestants)
            {
                var spot = ConnectedContestants[index];
                if (spot != null)
                    spot.SearchStamp = ClientListRefreshedStamp;
                var players = new List<VMEODGameshowBuzzerPlugin>(PotentialContestants.Where((player) => (player.SessionStamp != SessionStamp)).ToList());
                if (players.Count == 0)
                {
                    // no new players were found
                    if (viaSearchbtn)
                        this.MyPlayerClient.Send("Buzzer_Host_NoNewPlayerError", new byte[0]);
                }
                else // 1 or more players found
                {
                    int newPlayersIndex = 0;
                    bool exhaustedSearchList = true;
                    if (players.Count > 1)
                    {
                        // check the SearchStamp to give priority to unvisited players
                        for (int i = 0; i < players.Count; i++)
                        {
                            if (players[i].SearchStamp < ClientListRefreshedStamp)
                            {
                                newPlayersIndex = i;
                                exhaustedSearchList = false;
                                break;
                            }
                        }
                        if (exhaustedSearchList) // all unconnected contestants have already been iterated through, reset their search stamp
                            foreach (var eod in players)
                                eod.SearchStamp = 0;
                    }
                    // 1 or more valid players found
                    if (spot != null)
                        spot.SessionStamp = 0;
                    ConnectedContestants[index] = spot = players[newPlayersIndex];
                    spot.SessionStamp = SessionStamp;
                }
                if (viaSearchbtn)
                    SendContestantRoster();
            }
        }

        private void SendContestantRoster()
        {
            lock (ConnectedContestants)
            {

            }
        }
        /// <summary>
        /// 
        /// </summary>
        private void ValidateConnectedContestants()
        {
            lock (ConnectedContestants)
            {
                for (int index = 0; index < ConnectedContestants.Length; index++)
                    ValidateConnectedContestant(index);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool ValidateConnectedContestant(int index)
        {
            bool valid = true;
            if (ConnectedContestants[index]?.MyPlayerClient == null)
            {
                ConnectedContestants[index] = null;
                valid = false;
            }
            return valid;
        }
    }

    public enum VMEODGameshowBuzzerPluginType : byte
    {
        Unknown = 0,
        Host = 1,
        Player = 2
    }
}
