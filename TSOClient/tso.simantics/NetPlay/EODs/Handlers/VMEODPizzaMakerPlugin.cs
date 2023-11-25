using FSO.SimAntics.NetPlay.EODs.Model;
using System;
using System.Collections.Generic;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODPizzaMakerPlugin : VMEODHandler
    {
        private VMEODClient ControllerClient;

        private int PhoneWaitSeconds = 5;
        private int ContributionTimeoutSeconds = 120;
        private int RestartDelaySeconds = 8;
        private int CardsPerSmallIngredient = 16;
        private int CardsPerMediumIngredient = 10;
        private int CardsPerLargeIngredient = 8;
        private int CardsPerBonusIngredientPerSize = 2;

        private VMEODClient[] Players = new VMEODClient[4]; //Body, Cooking1, Charisma, Cooking2
        private VMEODPizzaPlayer[] PizzaPlayers = new VMEODPizzaPlayer[4];
        private List<VMEODIngredientCard> Cards; //pool of ingredients
        private Random CardRandom = new Random();

        private VMEODPizzaState State;
        private int Timer;
        private int TimerFrames = 0;
        private int LastPizzaResult = 0;

        public VMEODPizzaMakerPlugin(VMEODServer server) : base(server)
        {
            PopulateCards();
            PlaintextHandlers["close"] = P_Close;
            PlaintextHandlers["ingredient"] = P_Ingredient;

            SimanticsHandlers[(short)VMEODPizzaObjEvent.RespondPhone] = S_RespondPhone;
            SimanticsHandlers[(short)VMEODPizzaObjEvent.AllContributed] = S_AllContributed;
            SimanticsHandlers[(short)VMEODPizzaObjEvent.RespondBake] = S_RespondBake;
        }

        public override void Tick()
        {
            if (ControllerClient != null)
            {
                if (Timer > 0)
                {
                    if (++TimerFrames >= 30)
                    {
                        TimerFrames = 0;
                        Timer--;
                        SendTime();
                    }
                }

                switch (State)
                {
                    case VMEODPizzaState.Lobby:
                        int count = 0;
                        foreach (var player in Players) if (player != null) count++;
                        if (count == 4) EnterState(VMEODPizzaState.PhoneCall);
                        break;
                    case VMEODPizzaState.PhoneCall:
                        if (Timer == 0)
                        {
                            ControllerClient.SendOBJEvent(new VMEODEvent((short)VMEODPizzaObjEvent.RingPhone, Players[1].Avatar.ObjectID));
                            Timer = -1;
                        }
                        break;
                    case VMEODPizzaState.Contribution:
                        if (Timer == 0)
                        {
                            Timer = -1;
                            //bake enter state will assign unassigned players.
                            if (State == VMEODPizzaState.Contribution) EnterState(VMEODPizzaState.Bake);
                        }
                        break;
                    case VMEODPizzaState.Bake:
                        break;
                    case VMEODPizzaState.Break:
                        if (Timer == 0)
                        {
                            Timer = -1;
                            EnterState(VMEODPizzaState.Lobby);
                        }
                        break;
                }
            }
        }

        public void SetTimer(int remaining)
        {
            Timer = remaining;
            TimerFrames = 0;
            SendTime();
        }

        public void SendTime()
        {
            foreach (var player in Players)
                if (player != null) player.Send("pizza_time", Timer.ToString());
        }

        public void EnterState(VMEODPizzaState state)
        {
            State = state;
            foreach (var player in Players)
                if (player != null) player.Send("pizza_state", ((byte)State).ToString());
            switch (state)
            {
                case VMEODPizzaState.Lobby:
                    foreach (var player in PizzaPlayers)
                    {
                        if (player != null) player.Contrib = null;
                    }

                    ControllerClient.SendOBJEvent(new VMEODEvent((short)VMEODPizzaObjEvent.Restart));
                    SetTimer(-1);
                    PlayerRosterUpdate();
                    break;
                case VMEODPizzaState.PhoneCall:
                    //give everyone cards they're missing
                    for (int i=0; i<4; i++)
                    {
                        var player = PizzaPlayers[i];
                        var client = Players[i];
                        for (int j = 0; j < 3; j++)
                        {
                            if (player.Hand[j] == null) player.Hand[j] = TakeCard();
                        }
                        PlayerHandUpdate(client, player);
                    }
                    SetTimer(PhoneWaitSeconds);
                    break;
                case VMEODPizzaState.Contribution:
                    SetTimer(ContributionTimeoutSeconds);
                    break;
                case VMEODPizzaState.Bake:
                    SetTimer(-1);
                    int pizzaHas = 0;
                    for (int i=0; i < 4; i++)
                    {
                        var player = PizzaPlayers[i];
                        if (player.Contrib == null)
                        {
                            /* non-freeso
                            var avaID = (short)(client.Avatar.ObjectID);
                            if (avaID > 255) avaID = 0; //cool object
                            */

                            var avaID = i;

                            var select = CardRandom.Next(3);
                            player.Contrib = player.Hand[select];
                            InsertCard(player.Contrib);
                            ControllerClient.SendOBJEvent(new VMEODEvent((short)VMEODPizzaObjEvent.Contribute,
                                (short)((short)player.Contrib.Type | (short)(avaID<<8)))); //lo: ingredient, hi: FREESO player id
                            player.Hand[select] = null;

                            PlayerHandUpdate(Players[i], player);
                        }

                        var size = (int)player.Contrib.Size;
                        var type = Math.Min(3, (int)player.Contrib.Type);

                        pizzaHas |= 1 << (type + size * 4);
                    }
                    PlayerContributionUpdate();

                    int pizzaResult = 1; //mama-mia! you burned it!
                    for (int i = 0; i < 3; i++) {
                        //for all sizes, check for bonus flags, then flags.
                        var flags = (pizzaHas >> (i * 4)) & 15;
                        if (flags == 15) pizzaResult = i + 5; //all flags set
                        else if (flags == 7) pizzaResult = i + 2; //everything but topping
                    }

                    LastPizzaResult = pizzaResult;
                    ControllerClient.SendOBJEvent(new VMEODEvent((short)VMEODPizzaObjEvent.Bake, (short)pizzaResult));
                    break;
                case VMEODPizzaState.Break:
                    foreach (var player in Players) player.Send("pizza_result", LastPizzaResult.ToString());
                    ControllerClient.SendOBJEvent(new VMEODEvent((short)VMEODPizzaObjEvent.PayoutResult, (short)LastPizzaResult));
                    SetTimer(RestartDelaySeconds);
                    break;
            }
        }

        public void P_Close(string evt, string text, VMEODClient client)
        {
            //Server.Disconnect(client);
        }

        public void P_Ingredient(string evt, string text, VMEODClient client)
        {
            int item;
            if (ControllerClient == null || !int.TryParse(text, out item)) return; //invalid
            var station = Array.IndexOf(Players, client);
            if (station == -1 || State != VMEODPizzaState.Contribution) return;
            var player = PizzaPlayers[station];
            if (player.Contrib != null || item < 0 || item > 2) return;

            /* non-freeso
            var avaID = (short)(client.Avatar.ObjectID);
            if (avaID > 255) avaID = 0; //cool object
            */

            var avaID = station; //freeso patched. should really signal this on init.

            player.Contrib = player.Hand[item];
            ControllerClient.SendOBJEvent(new VMEODEvent((short)VMEODPizzaObjEvent.Contribute, 
                (short)((short)player.Contrib.Type | (short)(avaID<<8)))); //lo: ingredient, hi: FREESO player id
            InsertCard(player.Hand[item]);
            player.Hand[item] = null;

            PlayerContributionUpdate();
        }

        public void S_RespondPhone(short evt, VMEODClient client)
        {
            if (State == VMEODPizzaState.PhoneCall) EnterState(VMEODPizzaState.Contribution);
        }

        public void S_AllContributed(short evt, VMEODClient client)
        {
            //bake enter state will handle us not having all assigned, if that ever happens.
            if (State == VMEODPizzaState.Contribution) EnterState(VMEODPizzaState.Bake);
        }

        public void S_RespondBake(short evt, VMEODClient client)
        {
            if (State == VMEODPizzaState.Bake) EnterState(VMEODPizzaState.Break);
        }

        public override void OnDisconnection(VMEODClient client)
        {
            var playerIndex = Array.IndexOf(Players, client);
            if (playerIndex != -1)
            {
                Players[playerIndex] = null;
                if (PizzaPlayers[playerIndex] != null)
                {
                    PizzaPlayers[playerIndex].Contrib = null;
                    //next player in this index will inherit the hand.
                    //but the contribution is reset.
                }
                PlayerRosterUpdate();
            }
        }

        public void PlayerRosterUpdate()
        {
            string msg = "";
            int count = 0;
            foreach (var player in Players)
            {
                msg += ((player == null) ? 0 : player.Avatar.ObjectID) + "\n";
                if (player != null) count++;
            }
            if (count < 4 && State != VMEODPizzaState.Lobby)
            {
                EnterState(VMEODPizzaState.Lobby);
                return;
            }
            foreach (var player in Players)
            {
                if (player != null) player.Send("pizza_players", msg);
            }
        }

        public void PlayerContributionUpdate()
        {
            string msg = "";
            foreach (var player in PizzaPlayers)
            {
                msg += ((player.Contrib == null) ? "--" : player.Contrib.ToString()) + "\n";
            }
            foreach (var player in Players)
            {
                if (player != null) player.Send("pizza_contrib", msg);
            }
        }

        public void PlayerHandUpdate(VMEODClient client, VMEODPizzaPlayer player)
        {
            string msg = "";
            foreach (var item in player.Hand)
            {
                msg += ((item == null) ? "--" : item.ToString()) + "\n";
            }
            client.Send("pizza_hand", msg);
        }

        public void PopulateCards()
        {
            Cards = new List<VMEODIngredientCard>();
            for (int i=0; i<3; i++)
            {
                //for each base ingredient
                for (int j = 0; j < CardsPerSmallIngredient; j++)
                    InsertCard(new VMEODIngredientCard((VMEODIngredientType)i, VMEODIngredientSize.Small));
                for (int j = 0; j < CardsPerMediumIngredient; j++)
                    InsertCard(new VMEODIngredientCard((VMEODIngredientType)i, VMEODIngredientSize.Medium));
                for (int j = 0; j < CardsPerLargeIngredient; j++)
                    InsertCard(new VMEODIngredientCard((VMEODIngredientType)i, VMEODIngredientSize.Large));

                for (int j = 0; j < CardsPerBonusIngredientPerSize; j++)
                {
                    InsertCard(new VMEODIngredientCard((VMEODIngredientType)(i + 3), VMEODIngredientSize.Small));
                    InsertCard(new VMEODIngredientCard((VMEODIngredientType)(i + 3), VMEODIngredientSize.Medium));
                    InsertCard(new VMEODIngredientCard((VMEODIngredientType)(i + 3), VMEODIngredientSize.Large));
                }
            }
        }

        public void InsertCard(VMEODIngredientCard card)
        {
            Cards.Insert(CardRandom.Next(Cards.Count+1), card);
        }

        public VMEODIngredientCard TakeCard()
        {
            var card = Cards[0];
            Cards.RemoveAt(0);
            return card;
        }

        public override void OnConnection(VMEODClient client)
        {
            var param = client.Invoker.Thread.TempRegisters;
            if (client.Avatar != null)
            {
                //take tuning settings from object
                PhoneWaitSeconds = param[1];
                ContributionTimeoutSeconds = param[2];
                RestartDelaySeconds = param[3];
                CardsPerSmallIngredient = param[4];
                CardsPerMediumIngredient = param[5];
                CardsPerLargeIngredient = param[6];
                CardsPerBonusIngredientPerSize = param[7];

                //what part are we trying to join?
                var station = param[0];
                if (Players[station] != null)
                {
                    //what? someone's already here...
                    Server.Disconnect(client);
                }
                else
                {
                    client.Send("pizza_show", "");
                    Players[station] = client;
                    //if not initialized, set up this player with an empty hand
                    //otherwise inherit the last hand.
                    if (PizzaPlayers[station] == null) PizzaPlayers[station] = new VMEODPizzaPlayer();
                    PlayerRosterUpdate();
                }
            }
            else
            {
                //we're the pizza controller!
                ControllerClient = client;
            }
        }
    }

    public class VMEODIngredientCard
    {
        public VMEODIngredientType Type;
        public VMEODIngredientSize Size;

        public override string ToString()
        {
            return ((byte)Type).ToString() + ((byte)Size).ToString();
        }

        public VMEODIngredientCard() { }

        public VMEODIngredientCard(VMEODIngredientType type, VMEODIngredientSize size)
        {
            Type = type; Size = size;
        }

        public VMEODIngredientCard(string code)
        {
            byte bT, bS;
            if (!byte.TryParse(code.Substring(0,1), out bT) || !byte.TryParse(code.Substring(1, 1), out bS)) return;
            Type = (VMEODIngredientType)bT;
            Size = (VMEODIngredientSize)bS;
        }
    }

    public enum VMEODIngredientType : byte
    {
        Dough = 0,
        Sauce = 1,
        Cheese = 2,
        Pepperoni = 3,
        Mushrooms = 4,
        Anchovies = 5
    }

    public enum VMEODIngredientSize : byte
    {
        Small = 0,
        Medium = 1,
        Large = 2
    }

    public enum VMEODPizzaState : byte
    {
        Lobby = 0, //end when have 4 players. return to when less.
        PhoneCall = 1, //delay then animation, ended by simantics callback
        Contribution = 2, //everyone submits food. ended by simantics callback
        Bake = 3, //animation, ended by simantics callback
        Break = 4 //wait x seconds, then return to lobby
    }

    public enum VMEODPizzaObjEvent : short
    {
        Idle = 0,
        RingPhone = 1,
        Contribute = 2,
        Bake = 3,
        PayoutResult = 4,
        Restart = 5,

        RespondPhone = 6,
        AllContributed = 7,
        RespondBake = 8
    }

    public class VMEODPizzaPlayer
    {
        public VMEODIngredientCard Contrib;
        public VMEODIngredientCard[] Hand = new VMEODIngredientCard[3];
    }
}
