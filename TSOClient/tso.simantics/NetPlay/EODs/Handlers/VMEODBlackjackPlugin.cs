using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.SimAntics.NetPlay.EODs.Utils;
using FSO.SimAntics.NetPlay.EODs.Handlers.Data;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Primitives;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODBlackjackPlugin : VMEODHandler
    {
        private int ActivePlayerIndex;
        private int BettingTimer;
        private AbstractPlayingCardsDeck CardDeck;
        private int DecisionTimer;
        private int IdleTimer;
        private EODLobby<BlackjackPlayer> Lobby;
        private int MaxBet;
        private int MinBet;
        private int TableBalance;
        private int Tock;
        private VMEODBlackjackStates GameState;
        private VMEODBlackjackStates NextState = VMEODBlackjackStates.Invalid;
        private bool DealerIntermissionComplete;
        private List<VMEODEvent> DealerEventsQueue;

        private VMEODClient Controller;
        private VMEODClient Dealer;
        private BlackjackPlayer DealerPlayer;
        private VMEODClient Owner;

        #region static

        public static readonly int TABLE_MAX_BALANCE = 999999;

        public static Dictionary<string, byte> PlayingCardBlackjackValues = new Dictionary<string, byte>()
            {
                { "Ace", 1 },
                { "Two", 2 },
                { "Three", 3 },
                { "Four", 4 },
                { "Five", 5 },
                { "Six", 6 },
                { "Seven", 7 },
                { "Eight", 8 },
                { "Nine", 9 },
                { "Ten", 10 },
                { "Jack", 10 },
                { "Queen", 10 },
                { "King", 10 },
            };

        #endregion

        public VMEODBlackjackPlugin(VMEODServer server) : base(server)
        {
            Lobby = new EODLobby<BlackjackPlayer>(server, 4)
                    .BroadcastPlayersOnChange("blackjack_players_update")
                    .OnFailedToJoinDisconnect();
            ActivePlayerIndex = -1;
            DealerPlayer = new BlackjackPlayer();
            CardDeck = new AbstractPlayingCardsDeck(6, false);
            CardDeck.Shuffle(2);

            BinaryHandlers["blackjack_hit_request"] = HitRequestHandler;
            BinaryHandlers["blackjack_stand_request"] = StandRequestHandler;
            BinaryHandlers["blackjack_double_request"] = DoubleRequestHandler;
            BinaryHandlers["blackjack_split_request"] = SplitRequestHandler;
            BinaryHandlers["blackjack_bet_request"] = BetChangeRequestHandler;
            BinaryHandlers["blackjack_insurance_request"] = InsuranceRequestHandler;
            PlaintextHandlers["blackjack_close"] = UIClosedHandler;
            SimanticsHandlers[(short)VMEODBlackjackEvents.Split_Sequence_Complete] = SplitCompleteHandler;
            SimanticsHandlers[(short)VMEODBlackjackEvents.Animation_Sequence_Complete] = AnimationCompleteHandler;
            SimanticsHandlers[(short)VMEODBlackjackEvents.Dealer_Callback] = DealerCallbackHandler;
            SimanticsHandlers[(short)VMEODBlackjackEvents.Dealer_Check_Callback] = DealerCheckCallbackHandler;
            // owner
            PlaintextHandlers["blackjack_deposit"] = DepositHandler;
            PlaintextHandlers["blackjack_withdraw"] = WithdrawHandler;
            PlaintextHandlers["blackjack_new_minimum"] = NewMinimumBetHandler;
            PlaintextHandlers["blackjack_new_maximum"] = NewMaximumBetHandler;

            DealerIntermissionComplete = true;
            DealerEventsQueue = new List<VMEODEvent>();
        }

        #region public

        public override void Tick()
        {
            if (Controller == null)
                return;

            // handle next state
            if (NextState != VMEODBlackjackStates.Invalid)
            {
                var state = NextState;
                NextState = VMEODBlackjackStates.Invalid;
                GotoState(state);
            }

            switch (GameState)
            {
                case VMEODBlackjackStates.Waiting_For_Player:
                    {
                        if (!Lobby.IsEmpty())
                            EnqueueGotoState(VMEODBlackjackStates.Betting_Round);
                        break;
                    }
                case VMEODBlackjackStates.Betting_Round:
                    {
                        // check to see if all connected playesrs have submitted a bet - this check is for player disconnections
                        if (AllPlayersHaveSubmitted())
                        {
                            EnqueueGotoState(VMEODBlackjackStates.Pending_Initial_Transactions);
                        }
                        // there are still connected players who have not bet
                        else
                        {
                            if (BettingTimer > 0)
                            {
                                if (++Tock >= 30)
                                {
                                    BettingTimer--;
                                    Lobby.Broadcast("blackjack_timer", BitConverter.GetBytes(BettingTimer));
                                    Tock = 0;
                                }
                            }
                            // ran out of time
                            else
                                ForceEndBettingRound();
                        }
                        break;
                    }
                case VMEODBlackjackStates.Pending_Initial_Transactions:
                    {
                        if (IdleTimer > 0)
                        {
                            if (++Tock >= 30)
                            {
                                IdleTimer--;
                                Tock = 0;
                            }
                        }
                        else
                        {
                            // check to see if the pending transactions are finished
                            if (AllPendingTransactionFinal())
                            {
                                if (OneOrMoreBetsAccepted())
                                    DealCards();
                                else
                                {
                                    if (Lobby.IsEmpty())
                                        EnqueueGotoState(VMEODBlackjackStates.Waiting_For_Player);
                                    else
                                        EnqueueGotoState(VMEODBlackjackStates.Betting_Round);
                                }
                            }
                            // if they're not final, did someone just join? quick reace condition check
                            else
                            {
                                // oops someone joined. round must be changed by force
                                if (!AllPlayersHaveSubmitted())
                                {
                                    ForceEndBettingRound();
                                    EnqueueGotoState(VMEODBlackjackStates.Entre_Act);
                                }
                            }
                        }
                        break;
                    }
                case VMEODBlackjackStates.Player_Decision:
                    {
                        if (DecisionTimer > 0)
                        {
                            if (++Tock >= 30)
                            {
                                DecisionTimer--;
                                Lobby.Broadcast("blackjack_timer", BitConverter.GetBytes(DecisionTimer));
                                Tock = 0;
                            }
                        }
                        else
                        {
                            ForceStand(false);
                            EnqueueGotoState(VMEODBlackjackStates.Entre_Act);
                        }
                        break;
                    }
                case VMEODBlackjackStates.Insurance_Prompts:
                    {
                        if (DecisionTimer > 0)
                        {
                            if (++Tock >= 30)
                            {
                                DecisionTimer--;
                                Lobby.Broadcast("blackjack_timer", BitConverter.GetBytes(DecisionTimer));
                                Tock = 0;
                            }
                        }
                        else
                        {
                            // if anyone hasn't opted for insurance, they are denied the chance now
                            ForceInsuranceDenial();
                        }
                        break;
                    }
                case VMEODBlackjackStates.Finale:
                    {
                        if (IdleTimer > 0)
                        {
                            if (++Tock >= 30)
                            {
                                Tock = 0;
                                IdleTimer--;
                            }
                        }
                        else
                            EnqueueGotoState(VMEODBlackjackStates.Intermission);
                        break;
                    }
            }
            base.Tick();
        }

        public override void OnConnection(VMEODClient client)
        {
            if (client.Avatar != null)
            {
                // args[0] is Max Bet, args[1] is Min Bet
                var args = client.Invoker.Thread.TempRegisters;
                MinBet = args[1];
                MaxBet = args[0];
                if (args[2] == 0) // is a player
                {
                    // using their position at the table, put them in the proper index/slot
                    short playerIndex = args[3]; // 0, 1, 2, or 3
                    playerIndex--;

                    if (playerIndex < 0 || playerIndex > 3)
                    {
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Force_End_Playing, client.Avatar.ObjectID));
                        return;
                    }

                    // add to lobby
                    if (Lobby.Join(client, playerIndex))
                    {
                        var slot = Lobby.GetSlotData(client);
                        if (slot != null)
                        {
                            // get the amount of money the player has by sending a testOnly transaction for $1 from the table
                            var VM = client.vm;

                            VM.GlobalLink.PerformTransaction(VM, true, Server.Object.PersistID, client.Avatar.PersistID, 1,
                                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                                {
                                    if (success)
                                    {
                                        // set the slot data
                                        slot.Client = client;
                                        slot.PlayerIndex = playerIndex;
                                        slot.SimoleonBalance = (int)budget2;
                                        slot.ResetHands();
                                        slot.OnPlayerBetChange += BroadcastSingleBet;

                                        // set the table data
                                        TableBalance = (int)budget1;

                                        if (IsTableWithinLimits())
                                        {

                                            string[] data = new string[] { "" + playerIndex, MinBet + "", MaxBet + "", "" + Dealer.Avatar.ObjectID };

                                            // event to show the UI to the player
                                            client.Send("blackjack_player_show", VMEODGameCompDrawACardData.SerializeStrings(data));

                                            if (Dealer != null)
                                            {
                                                if (GameState.Equals(VMEODBlackjackStates.Waiting_For_Player) || GameState.Equals(VMEODBlackjackStates.Closed))
                                                    EnqueueGotoState(VMEODBlackjackStates.Betting_Round);
                                                else
                                                {
                                                    SyncAllPlayers(client);
                                                    if (GameState.Equals(VMEODBlackjackStates.Betting_Round))
                                                    {
                                                        client.Send("blackjack_toggle_betting", new byte[] { 1 }); // "Place your bets..."
                                                    }
                                                    else
                                                    {
                                                        client.Send("blackjack_toggle_betting", new byte[] { 0 }); // disallow betting
                                                        if (ActivePlayerIndex > -1)
                                                            client.Send("blackjack_late_comer", new byte[] { (byte)ActivePlayerIndex }); // "So-and-so's turn."
                                                    }
                                                }
                                            }
                                        }
                                        else  // the table does not have enough money
                                        {
                                            Lobby.Broadcast("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Table_NSF });
                                            EnqueueGotoState(VMEODBlackjackStates.Closed);
                                        }
                                    }
                                    else  // the table does not have enough money
                                    {
                                        Lobby.Broadcast("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Table_NSF });
                                        EnqueueGotoState(VMEODBlackjackStates.Closed);
                                    }
                                });
                        } // slot was null, should never happen
                        else
                        {
                            if (client != null && client.Avatar != null)
                                Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Force_End_Playing, client.Avatar.ObjectID));
                        }
                    } // could not join lobby
                    else
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Force_End_Playing, client.Avatar.ObjectID));
                } // end if (isPlayer)
                else // dealer or owner
                {
                    // is this the owner?
                    bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == client.Avatar.PersistID);
                    if (isOwner)
                    {
                        Owner = client;

                        // get the amount of money in the object by sending a testOnly transaction for $1 from Maxis
                        var VM = Owner.vm;

                        VM.GlobalLink.PerformTransaction(VM, true, uint.MaxValue, Server.Object.PersistID, 1,
                            (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                            {
                                if (success)
                                {
                                    GameState = VMEODBlackjackStates.Managing;
                                    TableBalance = (int)budget2;
                                    client.Send("blackjack_owner_show", TableBalance + "%" + MinBet + "%" + MaxBet);
                                }
                                else
                                    Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Failsafe_Remove_Dealer, client.Avatar.ObjectID));
                            });
                    }
                    else // not the owner, just the dealer
                    {
                        // The dealer's client is only used for animations. They literally have no other function.
                        Dealer = client;
                        if (GameState.Equals(VMEODBlackjackStates.Closed))
                            EnqueueGotoState(VMEODBlackjackStates.Waiting_For_Player);
                    }
                } // end if owner or dealer
            } // end if client.avatar is not null
            else
                Controller = client;
        }

        public override void OnDisconnection(VMEODClient client)
        {
            var slot = Lobby.GetSlotData(client);
            int playerIndex = -1;
            // slot will be null if owner or npc disconnected
            if (slot != null)
            {
                playerIndex = slot.PlayerIndex;
                slot.OnPlayerBetChange -= BroadcastSingleBet;
                slot.WarnedForObservation = false;
                slot.ResetHands();
                Lobby.Leave(client);
                if (playerIndex == ActivePlayerIndex)
                    ForceStand(true);
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Failsafe_Delete_ID, (short)slot.PlayerIndex));
            }
            if (Lobby.IsEmpty()) // no players
            {
                if (Dealer != null && client.Avatar != null)
                {
                    // if the client disconnecting is NOT the dealer, go to waiting for player
                    if (Dealer.Avatar.ObjectID != client.Avatar.ObjectID)
                        EnqueueGotoState(VMEODBlackjackStates.Waiting_For_Player);
                    else
                        EnqueueGotoState(VMEODBlackjackStates.Closed);
                }
                else
                    GameState = VMEODBlackjackStates.Closed;
            }
            base.OnDisconnection(client);
        }

        #endregion

        #region player events
        // client closes UI
        private void UIClosedHandler(string evt, string msg, VMEODClient client)
        {
            if (client != null && client.Avatar != null)
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Force_End_Playing, client.Avatar.ObjectID));
        }
        // client requests an action
        private void HitRequestHandler(string evt, byte[] blank, VMEODClient client)
        {
            if (GameState.Equals(VMEODBlackjackStates.Player_Decision)) {
            // is this client a player
            var player = Lobby.GetPlayerSlot(client);
                if (player > -1)
                {
                    // is this the current active player?
                    if (player == ActivePlayerIndex)
                    {
                        var slot = Lobby.GetSlotData(player);
                        // can they actually hit their current active hand
                        if (IsHandPlayable(slot.CurrentHandType))
                        {
                            // add the new card server-side, execute simantics event to play the hit sequence, goto to entre'act to wait
                            slot.Hit(CardDeck.Draw());
                            Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Player_Hit_Sequence, slot.Client.Avatar.ObjectID));
                            // broadcast this player's action
                            Lobby.Broadcast("blackjack_hit_broadcast", new byte[] { (byte)slot.PlayerIndex });
                            EnqueueGotoState(VMEODBlackjackStates.Entre_Act);
                        }
                        else
                        {
                            // they are the proper player but they cannot hit on this hand
                            client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Illegal_Hit });
                        }
                    }
                    else
                    {
                        // it is not this player's turn
                        client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.False_Start });
                    }
                }
            }
        }
        private void StandRequestHandler(string evt, byte[] blank, VMEODClient client)
        {
            if (GameState.Equals(VMEODBlackjackStates.Player_Decision))
            {
                // is this client a player
                var player = Lobby.GetPlayerSlot(client);
                if (player > -1)
                {
                    // is this the current active player?
                    if (player == ActivePlayerIndex)
                    {
                        var slot = Lobby.GetSlotData(player);
                        // close the hand server-side, execute simantics event to play the stand sequence, goto to entre'act to wait
                        slot.Stand();
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Player_Stand_Sequence, slot.Client.Avatar.ObjectID));
                        EnqueueGotoState(VMEODBlackjackStates.Entre_Act);
                        // broadcast this player's action
                        Lobby.Broadcast("blackjack_stand_broadcast", new byte[] { (byte)slot.PlayerIndex });
                    }
                    else
                    {
                        // it is not this player's turn
                        client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.False_Start });
                    }
                }
            }
        }
        private void DoubleRequestHandler(string evt, byte[] blank, VMEODClient client)
        {
            if (GameState.Equals(VMEODBlackjackStates.Player_Decision))
            {
                // is this client a player
                var player = Lobby.GetPlayerSlot(client);
                if (player > -1)
                {
                    // is this the current active player?
                    if (player == ActivePlayerIndex)
                    {
                        var slot = Lobby.GetSlotData(player);
                        // can they actually hit their current active hand
                        if (slot.CurrentHandType.Equals(VMEODBlackjackHandTypes.Double_Worthy) ||
                            slot.CurrentHandType.Equals(VMEODBlackjackHandTypes.Split_Worthy))
                        {
                            // can they afford to double down?
                            if (slot.SimoleonBalance >= slot.BetAmount)
                            {
                                EnqueueSequence(client, VMEODBlackjackEvents.Player_Double_Sequence, slot.BetAmount);
                                // broadcast this player's action
                                Lobby.Broadcast("blackjack_double_broadcast", new byte[] { (byte)slot.PlayerIndex });
                            }
                            else
                                client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Double_NSF });
                        }
                        else
                        {
                            // they are the proper player but they cannot double on this hand
                            client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Illegal_Double });
                        }
                    }
                    else
                    {
                        // it is not this player's turn
                        client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.False_Start });
                    }
                }
            }
        }
        private void SplitRequestHandler(string evt, byte[] blank, VMEODClient client)
        {
            if (GameState.Equals(VMEODBlackjackStates.Player_Decision))
            {
                // is this client a player
                var player = Lobby.GetPlayerSlot(client);
                if (player > -1)
                {
                    // is this the current active player?
                    if (player == ActivePlayerIndex)
                    {
                        var slot = Lobby.GetSlotData(player);
                        // can they actually hit their current active hand
                        if (slot.CurrentHandType.Equals(VMEODBlackjackHandTypes.Split_Worthy) && slot.TotalHands < 4) // no more than 4 hands
                        {
                            // can they afford to split?
                            if (slot.SimoleonBalance >= slot.BetAmount)
                            {
                                EnqueueSequence(client, VMEODBlackjackEvents.Player_Split_Sequence, slot.BetAmount);
                                // broadcast this player's action
                                Lobby.Broadcast("blackjack_split_broadcast", new byte[] { (byte)slot.PlayerIndex });
                            }
                            else
                                client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Split_NSF });
                        }
                        else
                        {
                            // they are the proper player but they cannot split this hand
                            client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Illegal_Split });
                        }
                    }
                    else
                    {
                        // it is not this player's turn
                        client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.False_Start });
                    }
                }
            }
        }
        // client tries to submit bet
        private void BetChangeRequestHandler(string evt, byte[] newBet, VMEODClient client)
        {
            if (GameState.Equals(VMEODBlackjackStates.Betting_Round))
            {
                var slot = Lobby.GetSlotData(client);
                if (slot != null) {
                    int bet = BitConverter.ToInt32(newBet, 0);
                    if (bet < MinBet)
                    {
                        client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Bet_Too_Low });
                        slot.Client.Send("blackjack_toggle_betting", new byte[] { 1 });
                    }
                    else if (bet > MaxBet)
                    {
                        client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Bet_Too_High });
                        slot.Client.Send("blackjack_toggle_betting", new byte[] { 1 });
                    }
                    else if (bet > slot.SimoleonBalance)
                    {
                        client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Bet_NSF });
                        slot.Client.Send("blackjack_toggle_betting", new byte[] { 1 });
                    }
                    else
                    {
                        slot.BetSubmitted = true;
                        // enqueue the sequence to debit the player
                        slot.PendingTransaction = true;
                        EnqueueSequence(client, VMEODBlackjackEvents.Player_Hit_Sequence, bet);
                    }
                }
            }
        }
        // client wants or doesn't want insurance
        private void InsuranceRequestHandler(string evt, byte[] wantsInsurance, VMEODClient client)
        {
            if (client == null)
                return;

            var slot = Lobby.GetSlotData(client);
            slot.PromptedForInsurance = false;

            if (wantsInsurance[0] == 1 && slot.BetAccepted)
                EnqueueInsuranceTransaction(slot.BetAmount, client);
            // In the event the player did not make selection in the UIAlert for insurance, that alert is still open. the callback will force it closed
            else
            {
                client.Send("blackjack_insurance_callback", BitConverter.GetBytes(0));

                // if all players have responded to insurance, move on
                if (AllPlayersRespondedInsurance())
                    EndInsuranceRound();
            }
        }
        // this semantics event occurs after any split animation sequences finishes
        private void SplitCompleteHandler(short evt, VMEODClient client)
        {
            BlackjackPlayer slot = null;
            if (ActivePlayerIndex > -1 && ActivePlayerIndex < 4)
                slot = Lobby.GetSlotData(Lobby.Players[ActivePlayerIndex]);
            // some checks in case the player left during the animation
            if (slot != null && slot.Client != null & slot.CurrentHandType.Equals(VMEODBlackjackHandTypes.Split_Worthy))
            {
                // creates another deck but leaves current deck alone. so slot.currentdeck+1 also needs to be sync'd
                // returns splitting hand index and the 4 cards to be sent to the splitting client for sync
                string[] serializedSplitNumberAndCards = slot.Split(CardDeck.Draw(2));
                slot.Client.Send("blackjack_split",
                    VMEODGameCompDrawACardData.SerializeStrings(serializedSplitNumberAndCards));
            }
            EnqueueGotoState(VMEODBlackjackStates.Player_Decision);
        }
        // this semantics event occurs after any animation sequence during gamestate = entre'act
        private void AnimationCompleteHandler(short evt, VMEODClient controller)
        {
            if (OneOrMoreBetsAccepted() || (!AllPendingTransactionFinal() && AllPlayersHaveSubmitted()))
            {
                // no one has done anything yet, so need to check if dealer's first card is Ace
                if (ActivePlayerIndex == -1)
                {
                    // dealer's first card is Ace, prompt everyone for insurance
                    if (DealerPlayer.IsMyFirstCardAnAce())
                        EnqueueGotoState(VMEODBlackjackStates.Insurance_Prompts);
                    else
                        EnqueueGotoState(VMEODBlackjackStates.Player_Decision);
                }
                else if (ActivePlayerIndex < 4)
                    EnqueueGotoState(VMEODBlackjackStates.Player_Decision);
                else
                    EnqueueGotoState(VMEODBlackjackStates.Dealer_Decision);
            }
            else
                EnqueueGotoState(VMEODBlackjackStates.Waiting_For_Player);
        }
        /*
         * This handles the queue of dealer events post-game. They're either events to declare a winner then give chips, or to collect chips from a loser.
         * Each event has data sent with it that contains the objectID of the player who either won or lost.
         */
        private void DealerCallbackHandler(short evt, VMEODClient controller)
        {
            if (!GameState.Equals(VMEODBlackjackStates.Waiting_For_Player))
            {
                if (!DealerIntermissionComplete)
                {
                    // if there are no more events
                    if (DealerEventsQueue.Count == 0)
                    {
                        DealerIntermissionComplete = true;
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Dealer_Collect_Cards)); // still sends dealer_callback
                    }
                    // otherwise send next event
                    else
                    {
                        VMEODEvent nextEvent = DealerEventsQueue[0];
                        DealerEventsQueue.RemoveAt(0);
                        Controller.SendOBJEvent(nextEvent);
                    }
                }
                else // final dealer_callback
                {
                    // if the table doesn't have enough money for minimum, it needs to close
                    if (!IsTableWithinLimits())
                    {
                        Lobby.Broadcast("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Table_NSF });
                        EnqueueGotoState(VMEODBlackjackStates.Closed);
                    }
                    // if there are still players, go to next game
                    else if (!Lobby.IsEmpty())
                        EnqueueGotoState(VMEODBlackjackStates.Betting_Round);
                    else
                        EnqueueGotoState(VMEODBlackjackStates.Waiting_For_Player);
                }
            }
        }
        /*
         * The callback happens after the animations for the dealer checking her cards. Here the dealer's hand is actually checked for blackjack.
         * The current state is Entre'Act.
         */
        private void DealerCheckCallbackHandler(short evt, VMEODClient controller)
        {
            byte blackjack = 0;
            if (DealerPlayer.CurrentHandType.Equals(VMEODBlackjackHandTypes.Blackjack))
            {
                blackjack = 1;
                ActivePlayerIndex = 4;

                // all players react losing
                for (int index = 0; index < 4; index++)
                {
                    var client = Lobby.Players[index];
                    if (client != null)
                    {
                        var slot = Lobby.GetSlotData(index);
                        if (slot.BetAccepted)
                            Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Player_Lose_Animation,
                                new short[] { client.Avatar.ObjectID }));
                    }
                }
                // skip directly to dealer decision, to make her hand active, which then goes to intermission
                EnqueueGotoState(VMEODBlackjackStates.Dealer_Decision);
            }
            else
            {
                // dealer does not have blackjack, so goto first player
                EnqueueGotoState(VMEODBlackjackStates.Player_Decision);
            }
            Lobby.Broadcast("dealer_blackjack_result", new byte[] { blackjack });
        }
        #endregion

        #region owner events
        /*
         * 
         */
        private void NewMinimumBetHandler(string evt, string newMinString, VMEODClient client)
        {
            string failureReason = "";
            short newMinBet;
            var result = Int16.TryParse(newMinString.Trim(), out newMinBet);
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == client.Avatar.PersistID);
            if (!isOwner)
                failureReason = VMEODSlotsInputErrorTypes.InvalidOwner.ToString(); // not the owner
            else if (result)
            {
                // proposed minimum bet must be greater than $1
                if (newMinBet < 1)
                    failureReason = VMEODRouletteInputErrorTypes.BetTooLow.ToString();
                // proposed minimum bet must not be greater than the MaxBet
                else if (newMinBet > MaxBet)
                    failureReason = VMEODRouletteInputErrorTypes.BetTooHigh.ToString();
                else
                {
                    // does the object have enough money to cover this bet amount?
                    if (newMinBet > TableBalance / 6)
                        failureReason = VMEODRouletteInputErrorTypes.BetTooHighForBalance.ToString();
                    else
                    {
                        // valid new minimum bet
                        MinBet = newMinBet;
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.New_Minimum_Bet, newMinBet));
                        client.Send("blackjack_min_bet_success", "" + newMinBet);
                        return;
                    }
                }
            }
            else
            {
                if (newMinString.Trim().Length == 0)
                    failureReason = VMEODSlotsInputErrorTypes.Null.ToString(); // empty data
                else
                    failureReason = VMEODSlotsInputErrorTypes.Invalid.ToString(); // invalid data
            }
            // send the fail reason
            client.Send("blackjack_n_bet_fail", failureReason);
        }

        private void NewMaximumBetHandler(string evt, string newMaxString, VMEODClient client)
        {
            string failureReason = "";
            short newMaxBet;
            var result = Int16.TryParse(newMaxString.Trim(), out newMaxBet);
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == client.Avatar.PersistID);
            if (!isOwner)
                failureReason = VMEODSlotsInputErrorTypes.InvalidOwner.ToString(); // not the owner
            else if (result)
            {
                // proposed maximum bet must be greater than or equal to minimum bet
                if (newMaxBet < MinBet)
                    failureReason = VMEODRouletteInputErrorTypes.BetTooLow.ToString();
                // proposed maximum bet must not be greater than $1000
                else if (newMaxBet > 1000)
                    failureReason = VMEODRouletteInputErrorTypes.BetTooHigh.ToString();
                else
                {
                    // does the object have enough money to cover this bet amount?
                    if (newMaxBet > TableBalance / 6)
                        failureReason = VMEODRouletteInputErrorTypes.BetTooHighForBalance.ToString();
                    else
                    {
                        // valid new max bet
                        MaxBet = newMaxBet;
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.New_Maximum_Bet, newMaxBet));
                        client.Send("blackjack_max_bet_success", "" + newMaxBet);
                        return;
                    }
                }
            }
            else
            {
                if (newMaxString.Trim().Length == 0)
                    failureReason = VMEODSlotsInputErrorTypes.Null.ToString(); // empty data
                else
                    failureReason = VMEODSlotsInputErrorTypes.Invalid.ToString(); // invalid data
            }
            // send the fail reason
            client.Send("blackjack_x_bet_fail", failureReason);
        }

        private void WithdrawHandler(string evt, string amountString, VMEODClient client)
        {
            string failureReason = "";

            // try to parse the withdraw amount
            int withdrawAmount;
            var result = Int32.TryParse(amountString.Trim(), out withdrawAmount);
            if (result)
            {
                // check the successfully parsed amount to make sure it's non-negative and against the TableBalance to determine if valid amount
                if (withdrawAmount == 0)
                    failureReason = VMEODSlotsInputErrorTypes.Null.ToString();
                else if (withdrawAmount < 0)
                    failureReason = VMEODSlotsInputErrorTypes.Invalid.ToString();
                else
                {
                    if (withdrawAmount > TableBalance)
                        failureReason = VMEODSlotsInputErrorTypes.Overflow.ToString();
                }
            }
            else // invalid input was sent to the server from a modified client
            {
                failureReason = VMEODSlotsInputErrorTypes.Invalid.ToString();
            }
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == client.Avatar.PersistID);
            // if the client is the owner of the object AND there were no failures detected
            if ((isOwner) && (failureReason.Length == 0))
            {
                // attempt to credit the owner by debiting the object
                var VM = client.vm;
                VM.GlobalLink.PerformTransaction(VM, false, Server.Object.PersistID, client.Avatar.PersistID, withdrawAmount,

                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                {
                    if (success)
                    {
                        TableBalance = (int)(budget1);
                        client.Send("blackjack_resume_manage", TableBalance + "");
                    }
                    else
                        client.Send("blackjack_withdraw_fail", VMEODSlotsInputErrorTypes.Unknown.ToString());
                });
            }
            else // otherwise, send the failureReason
            {
                if (failureReason.Length == 0)
                    failureReason = VMEODSlotsInputErrorTypes.InvalidOwner.ToString();
                client.Send("blackjack_withdraw_fail", failureReason);
            }
        }
        private void DepositHandler(string evt, string amountString, VMEODClient client)
        {
            string failureReason = "";

            // try to parse the deposit amount
            int depositAmount;
            var result = Int32.TryParse(amountString.Trim(), out depositAmount);
            if (result)
            {
                // check the successfully parsed amount to make sure it's non-negative and against the MachineBalanceMax to determine if valid amount
                if (depositAmount == 0)
                    failureReason = VMEODSlotsInputErrorTypes.Null.ToString();
                else if (depositAmount < 0)
                    failureReason = VMEODSlotsInputErrorTypes.Invalid.ToString();
                else
                {
                    if ((depositAmount + TableBalance) > int.MaxValue)
                        failureReason = VMEODSlotsInputErrorTypes.Overflow.ToString();
                }
            }
            else // invalid input was sent to the server from a modified client
            {
                failureReason = VMEODSlotsInputErrorTypes.Invalid.ToString();
            }
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == client.Avatar.PersistID);
            // if the client is the owner of the object AND there were no failures detected
            if ((isOwner) && (failureReason.Length == 0))
            {
                // attempt to credit the object by debiting the owner
                var VM = client.vm;
                VM.GlobalLink.PerformTransaction(VM, false, client.Avatar.PersistID, Server.Object.PersistID, depositAmount,

                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                {
                    if (success)
                    {
                        TableBalance = (int)(budget2);
                        client.Send("blackjack_resume_manage", TableBalance + "");
                    }
                    else
                    {
                        // Owner does not have enough simoleons to make this deposit
                        client.Send("blackjack_deposit_NSF", "" + amountString);
                    }
                });
            }
            else // otherwise, send the failureReason
            {
                if (failureReason.Length == 0)
                    failureReason = VMEODSlotsInputErrorTypes.InvalidOwner.ToString();
                client.Send("blackjack_deposit_fail", failureReason);
            }
        }

        #endregion

        #region private

        private void EnqueueGotoState(VMEODBlackjackStates nextState)
        {
            NextState = nextState;
        }

        private void GotoState(VMEODBlackjackStates newState)
        {
            if (GameState.Equals(newState))
                return;

            Tock = 0;

            switch (newState)
            {
                case VMEODBlackjackStates.Closed:
                    {
                        if (!Lobby.IsEmpty())
                        {
                            var players = new List<VMEODClient>(Lobby.Players);
                            foreach (var player in players)
                            {
                                if (player != null && player.Avatar != null)
                                    Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Force_End_Playing, player.Avatar.ObjectID));
                            }
                        }
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Failsafe_Remove_Dealer));
                        GameState = newState;
                        break;
                    }
                case VMEODBlackjackStates.Betting_Round:
                    {
                        NewGame();
                        GameState = newState;
                        break;
                    }
                case VMEODBlackjackStates.Player_Decision:
                    {
                        DecisionTimer = 15;
                        GameState = newState;

                        // validate the previous hand, broadcast it to all players (to sync)
                        if (ActivePlayerIndex > -1)
                        {
                            List<string> cardsList;
                            var lastPlayer = Lobby.GetSlotData(ActivePlayerIndex);
                            if (lastPlayer != null && !lastPlayer.CurrentHandType.Equals(VMEODBlackjackHandTypes.Empty))
                            {
                                cardsList = lastPlayer.GetCurrentCards();

                                // broadcast the hand to all players, showing it as the new active hand for now, though that may change
                                var serializedPlayerNumberAndCards = new List<string>(cardsList);
                                serializedPlayerNumberAndCards.Insert(0, "" + lastPlayer.PlayerIndex);
                                Lobby.Broadcast("blackjack_sync_player",
                                    VMEODGameCompDrawACardData.SerializeStrings(serializedPlayerNumberAndCards.ToArray()));

                                // sync the previous player's main hand, as this might show a newly drawn card
                                var serializedDeckNumberAndCards = new List<string>(cardsList);
                                serializedDeckNumberAndCards.Insert(0, "" + (lastPlayer.CurrentHand));
                                lastPlayer.Client.Send("blackjack_active_hand",
                                    VMEODGameCompDrawACardData.SerializeStrings(serializedDeckNumberAndCards.ToArray()));
                                
                                // now check if the last hand can still be played (as long as it isn't blackjack, bust, or stand)
                                if (!IsHandPlayable(lastPlayer.CurrentHandType))
                                {
                                    // this hand is finished, send the stand or double event to the player
                                    if (lastPlayer.CurrentHandType.Equals(VMEODBlackjackHandTypes.Doubled_Down) ||
                                        lastPlayer.CurrentHandType.Equals(VMEODBlackjackHandTypes.Doubled_Bust))
                                        lastPlayer.Client.Send("blackjack_double", VMEODGameCompDrawACardData.SerializeStrings(cardsList.ToArray()));
                                    else
                                        lastPlayer.Client.Send("blackjack_stand", VMEODGameCompDrawACardData.SerializeStrings(cardsList.ToArray()));

                                    // if the player has another hand, see if it is playable
                                    while (lastPlayer.StandAndGotoNextHand())
                                    {
                                        // there is another hand. get the cards and sync them with the player's UI
                                        var newCardsList = lastPlayer.GetCurrentCards();
                                        serializedDeckNumberAndCards = new List<string>(newCardsList);
                                        serializedDeckNumberAndCards.Insert(0, "" + lastPlayer.CurrentHand);
                                        lastPlayer.Client.Send("blackjack_active_hand",
                                            VMEODGameCompDrawACardData.SerializeStrings(serializedDeckNumberAndCards.ToArray()));
                                        
                                        // broadcast the hand to all players, showing it as the new active hand
                                        serializedPlayerNumberAndCards = new List<string>(newCardsList);
                                        serializedPlayerNumberAndCards.Insert(0, "" + lastPlayer.PlayerIndex);
                                        Lobby.Broadcast("blackjack_sync_player",
                                            VMEODGameCompDrawACardData.SerializeStrings(serializedPlayerNumberAndCards.ToArray()));

                                        // is the hand playable?
                                        if (IsHandPlayable(lastPlayer.CurrentHandType))
                                        {
                                            // YES! enable this player's input
                                            SendEnableInput(lastPlayer);
                                            return;
                                        }
                                        // not playable as it is a blackjack, go to the next hand
                                        lastPlayer.Client.Send("blackjack_blackjack", VMEODGameCompDrawACardData.SerializeStrings(newCardsList.ToArray()));
                                    }
                                }
                                else // yes the last player's current hand can still be played, do not change the player or current hand
                                {
                                    // enable this player's input
                                    SendEnableInput(lastPlayer);

                                    // broadcast the hand to all players, showing it as the new active hand
                                    serializedPlayerNumberAndCards = new List<string>(cardsList);
                                    serializedPlayerNumberAndCards.Insert(0, "" + lastPlayer.PlayerIndex);
                                    Lobby.Broadcast("blackjack_sync_player",
                                        VMEODGameCompDrawACardData.SerializeStrings(serializedPlayerNumberAndCards.ToArray()));

                                    return;
                                }
                            }
                        }
                        /*
                         * There was no previous player OR there were no other hands that previous player could play. so move on to next player
                         */
                        if (ActivePlayerIndex < 3) // if the activeplayer index is 3, the final player was the last to act, so skip to dealer
                        {
                            while (ActivePlayerIndex < 3)
                            {
                                // is there another player?
                                ActivePlayerIndex++;
                                var nextClient = Lobby.Players[ActivePlayerIndex];
                                if (nextClient != null)
                                {
                                    // found another player!
                                    var nextPlayer = Lobby.GetSlotData(nextClient);

                                    // check all of their hands to find the first playable one
                                    while (nextPlayer.CurrentHand < nextPlayer.TotalHands)
                                    {
                                        // sync their main hand, making it active (does not enable input)
                                        var cardsList = new List<string>(nextPlayer.GetCurrentCards());
                                        var serializedDeckNumberAndCards = new List<string>(cardsList);
                                        serializedDeckNumberAndCards.Insert(0, nextPlayer.CurrentHand + "");
                                        nextPlayer.Client.Send("blackjack_active_hand",
                                            VMEODGameCompDrawACardData.SerializeStrings(serializedDeckNumberAndCards.ToArray()));

                                        if (IsHandPlayable(nextPlayer.CurrentHandType))
                                        {
                                            // this is a playable hand!
                                            var serializedPlayerNumberAndCards = new List<string>(cardsList);
                                            serializedPlayerNumberAndCards.Insert(0, "" + nextPlayer.PlayerIndex); // insert player number 0-3

                                            // broadcast the player's cards to everyone, making this player appear active
                                            Lobby.Broadcast("blackjack_sync_player",
                                                VMEODGameCompDrawACardData.SerializeStrings(serializedPlayerNumberAndCards.ToArray()));

                                            // enable input for this player's current hand
                                            SendEnableInput(nextPlayer);
                                            return;
                                        }
                                        else
                                        {
                                            // this hand is a blackjack, send the blackjack event to the player to update their UI
                                            nextPlayer.Client.Send("blackjack_blackjack",
                                                VMEODGameCompDrawACardData.SerializeStrings(cardsList.ToArray()));
                                            if (!nextPlayer.StandAndGotoNextHand())
                                                break; // no more hands left, break this while loop and try next player
                                        }
                                    }
                                }
                            }
                        }
                        // no active players with playable hands were found, only the dealer remains
                        EnqueueGotoState(VMEODBlackjackStates.Dealer_Decision);
                        break;
                    }
                case VMEODBlackjackStates.Dealer_Decision:
                    {
                        GameState = newState;
                        // broadcast that the dealer is next, sync her cards across all players
                        List<string> momiCards = DealerPlayer.GetCurrentCards();
                        Lobby.Broadcast("blackjack_sync_dealer", VMEODGameCompDrawACardData.SerializeStrings(momiCards.ToArray()));

                        // hit or stand algorithm
                        var momiHandtotal = DealerPlayer.CurrentHandTotal;
                        if (momiHandtotal < 17) // hit below 17
                        {
                            DealerPlayer.Hit(CardDeck.Draw());
                            Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Dealer_Hit_Self_Sequence, Dealer.Avatar.ObjectID));
                            EnqueueGotoState(VMEODBlackjackStates.Entre_Act);
                        }
                        else
                        {
                            if (momiHandtotal == 17)
                            {
                                // hit on soft 17
                                if (DealerPlayer.SoftTotal)
                                {
                                    DealerPlayer.Hit(CardDeck.Draw());
                                    Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Dealer_Hit_Self_Sequence, Dealer.Avatar.ObjectID));
                                    EnqueueGotoState(VMEODBlackjackStates.Entre_Act);
                                }
                                else
                                {
                                    DealerPlayer.Stand();
                                    EnqueueGotoState(VMEODBlackjackStates.Finale);
                                }
                            }
                            else
                            {
                                DealerPlayer.Stand();
                                EnqueueGotoState(VMEODBlackjackStates.Finale);
                            }
                        }
                        break;
                    }
                case VMEODBlackjackStates.Intermission:
                    {
                        GameState = newState;
                        // find the winners and pay them, send winning/losing animations
                        int dealerHandTotal = DealerPlayer.CurrentHandTotal;
                        short playerObjectID = 0;

                        for (int index = 0; index < 4; index++)
                        {
                            var playerClient = Lobby.Players[index];
                            if (playerClient != null)
                            {
                                playerObjectID = playerClient.Avatar.ObjectID;
                                var slot = Lobby.GetSlotData(index);
                                if (slot.BetAccepted)
                                {
                                    int winnings = slot.CalculateTotalPayout(dealerHandTotal, DealerPlayer.CurrentHandType);
                                    if (winnings > 0)
                                    {
                                        // pay them
                                        EnqueuePayout(winnings, playerClient);

                                        // queue event for dealer to give them chips
                                        DealerEventsQueue.Add(new VMEODEvent((short)VMEODBlackjackEvents.Dealer_Declare_winner, new short[] { playerObjectID }));

                                        // event for player wins
                                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Player_Win_Animation,
                                            new short[] { playerObjectID, (short)winnings }));
                                    }
                                    else
                                    {
                                        // they are already mad that the dealer has blackjack, don't play mad animation twice
                                        if (!DealerPlayer.CurrentHandType.Equals(VMEODBlackjackHandTypes.Blackjack))
                                            Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Player_Lose_Animation,
                                                new short[] { playerObjectID }));

                                        // queue event for dealer to collect chips
                                        DealerEventsQueue.Add(new VMEODEvent((short)VMEODBlackjackEvents.Dealer_Collect_Chips, new short[] { playerObjectID }));
                                    }
                                    playerClient.Send("blackjack_win_loss_message", BitConverter.GetBytes(winnings));
                                }
                            }
                        }
                        // if there are dealer events, execute the first one
                        if (DealerEventsQueue.Count > 0)
                        {
                            DealerIntermissionComplete = false;
                            var firstEvent = DealerEventsQueue[0];
                            DealerEventsQueue.RemoveAt(0);
                            Controller.SendOBJEvent(firstEvent);
                        }
                        // if not, see if there are still players. goto betting if so, waiting if not
                        else
                        {
                            if (!Lobby.IsEmpty())
                                EnqueueGotoState(VMEODBlackjackStates.Betting_Round);
                            else
                                EnqueueGotoState(VMEODBlackjackStates.Waiting_For_Player);
                        }
                        break;
                    }
                case VMEODBlackjackStates.Waiting_For_Player:
                    {
                        ActivePlayerIndex = -1;
                        CardDeck.Shuffle(2);
                        GameState = newState;
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Dealer_Collect_Cards));
                        break;
                    }
                case VMEODBlackjackStates.Pending_Initial_Transactions:
                    {
                        Lobby.Broadcast("blackjack_timer", BitConverter.GetBytes(0));
                        IdleTimer = 2;
                        Tock = 0;
                        GameState = newState;
                        break;
                    }
                case VMEODBlackjackStates.Insurance_Prompts:
                    {
                        DecisionTimer = 10;
                        Tock = 0;
                        GameState = newState;
                        for (int index = 0; index < 4; index++)
                        {
                            var player = Lobby.Players[index];
                            if (player != null)
                            {
                                var slot = Lobby.GetSlotData(index);
                                if (slot.BetAccepted)
                                {
                                    slot.PromptedForInsurance = true;
                                    player.Send("blackjack_insurance_prompt", BitConverter.GetBytes(slot.BetAmount / 2));
                                }
                            }
                        }
                        break;
                    }
                case VMEODBlackjackStates.Finale:
                    {
                        int dealerTotal = 0; // send 0 if blackjack
                        if (!DealerPlayer.CurrentHandType.Equals(VMEODBlackjackHandTypes.Blackjack))
                            dealerTotal = DealerPlayer.CurrentHandTotal;
                        Lobby.Broadcast("dealer_hand_total", BitConverter.GetBytes(dealerTotal));
                        Tock = 0;
                        IdleTimer = 2;
                        GameState = newState;
                        break;
                    }
                default:
                    {
                        Tock = 0;
                        GameState = newState;
                        break;
                    }
            }
        }
        // reset all players' hands, including dealer's as well as players' bet data
        private void NewGame()
        {
            for (int index = 0; index < 4; index++)
            {
                var player = Lobby.Players[index];
                if (player != null)
                {
                    var slot = Lobby.GetSlotData(index);
                    slot.ResetHands();
                    if (slot.SimoleonBalance < MinBet)
                    {
                        // come back when you have more money
                        if (slot.Client != null && slot.Client.Avatar != null)
                        {
                            slot.Client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Player_NSF });
                            Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Force_End_Playing, slot.Client.Avatar.ObjectID));
                        }
                        else
                        {
                            Lobby.Leave(slot.Client);
                            Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Failsafe_Delete_ID, (short)slot.PlayerIndex));
                        }
                    }
                }
            }
            DealerPlayer.ResetHands();
            ActivePlayerIndex = -1;
            BettingTimer = 30;
            // send the name of the dealer along with the new game event
            Lobby.Broadcast("blackjack_new_game",
                VMEODGameCompDrawACardData.SerializeStrings(Dealer.Avatar.Name));
        }
        // ends the insurance prompting
        private void EndInsuranceRound()
        {
            // wait in entre'act for the Dealer_Check_Callback event
            EnqueueGotoState(VMEODBlackjackStates.Entre_Act);
            short blackjack = 0;
            if (DealerPlayer.CurrentHandType.Equals(VMEODBlackjackHandTypes.Blackjack))
                blackjack = 1;
            // send event to check for blackjack, momi avatar will play yes or no animation after checking based on data sent in this event
            Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Dealer_Check_Blackjack, new short[] { blackjack }));
        }
        private bool IsHandPlayable(VMEODBlackjackHandTypes handType)
        {
            switch (handType)
            {
                case VMEODBlackjackHandTypes.Blackjack:
                case VMEODBlackjackHandTypes.Bust:
                case VMEODBlackjackHandTypes.Stand:
                case VMEODBlackjackHandTypes.Doubled_Down:
                case VMEODBlackjackHandTypes.Doubled_Bust:
                    return false;
            }
            return true;
        }
        // Note: Will not send if the gamestate is not player decision aka the player ran out of time
        private void SendEnableInput(BlackjackPlayer player)
        {
            if (player != null && GameState.Equals(VMEODBlackjackStates.Player_Decision))
            {
                var serializedDeckNumberAndCards = player.GetCurrentCards();
                serializedDeckNumberAndCards.Insert(0, player.CurrentHand + "");
                switch (player.CurrentHandType)
                {
                    case VMEODBlackjackHandTypes.Split_Worthy:
                        {
                            if (player.TotalHands < 4 && player.BetAmount <= player.SimoleonBalance)
                            {
                                player.Client.Send("blackjack_resume_split",
                                    VMEODGameCompDrawACardData.SerializeStrings(serializedDeckNumberAndCards.ToArray()));
                            }
                            else
                            {
                                if (player.BetAmount <= player.SimoleonBalance)
                                {
                                    // you may not split again, you already have 4 active decks or not enough money. however you can double
                                    player.Client.Send("blackjack_resume_double",
                                        VMEODGameCompDrawACardData.SerializeStrings(serializedDeckNumberAndCards.ToArray()));
                                }
                                else
                                    player.Client.Send("blackjack_resume_hand",
                                        VMEODGameCompDrawACardData.SerializeStrings(serializedDeckNumberAndCards.ToArray()));
                            }
                            break;
                        }
                    case VMEODBlackjackHandTypes.Double_Worthy:
                        {
                            if (player.BetAmount <= player.SimoleonBalance)
                            {
                                player.Client.Send("blackjack_resume_double",
                                VMEODGameCompDrawACardData.SerializeStrings(serializedDeckNumberAndCards.ToArray()));
                            }
                            else
                                player.Client.Send("blackjack_resume_hand",
                                    VMEODGameCompDrawACardData.SerializeStrings(serializedDeckNumberAndCards.ToArray()));
                            break;
                        }
                    default:
                        {
                            player.Client.Send("blackjack_resume_hand",
                                VMEODGameCompDrawACardData.SerializeStrings(serializedDeckNumberAndCards.ToArray()));
                            break;
                        }
                }
            }
        }
        // active player didn't make decision within time limit, they are now forced to stand on this hand.
        private void ForceStand(bool playerLeft)
        {
            EnqueueGotoState(VMEODBlackjackStates.Entre_Act);
            if (!playerLeft)
            {
                var player = Lobby.Players[ActivePlayerIndex];
                if (player != null)
                {
                    var slot = Lobby.GetSlotData(player);
                    // failsafe disallow betting event
                    slot.Client.Send("blackjack_disable_hand_buttons", new byte[] { 0 });
                    var cardsList = slot.GetCurrentCards();
                    slot.Stand();
                    // broadcast this player's action
                    Lobby.Broadcast("blackjack_stand_broadcast", new byte[] { (byte)slot.PlayerIndex });
                    Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Player_Stand_Sequence, slot.Client.Avatar.ObjectID));
                }
            }
            else
            {
                ActivePlayerIndex++;
                GotoState(VMEODBlackjackStates.Entre_Act);
                AnimationCompleteHandler((short)VMEODBlackjackEvents.Animation_Sequence_Complete, Controller);
            }
        }
        // one or more players did not place a bet before the timer expired
        private void ForceEndBettingRound()
        {
            // immediately disallow server-side and client-side betting
            EnqueueGotoState(VMEODBlackjackStates.Pending_Initial_Transactions);
            Lobby.Broadcast("blackjack_toggle_betting", new byte[] { 0 });
            
            for (int index = 0; index < 4; index++)
            {
                var player = Lobby.Players[index];
                if (player != null)
                {
                    var slot = Lobby.GetSlotData(player);
                    if (!slot.BetSubmitted)
                    {
                        if (slot.WarnedForObservation)
                        {
                            // they have to leave, they were already warned last round
                            slot.Client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Observe_Twice });
                            if (player.Avatar != null)
                                Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Force_End_Playing, player.Avatar.ObjectID));
                            if (Lobby.IsEmpty())
                            {
                                EnqueueGotoState(VMEODBlackjackStates.Waiting_For_Player);
                                return;
                            }
                        }
                        else
                        {
                            // they did not bet, so they get a warning about being able to observe for this round only
                            slot.WarnedForObservation = true;
                            slot.Client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Observe_Once });
                        }
                    }
                }
            }
        }
        // if anyone has not opted for insurance, they are now denied the chance
        private void ForceInsuranceDenial()
        {
            for (int index = 0; index < 4; index++)
            {
                var player = Lobby.Players[index];
                if (player != null)
                {
                    var slot = Lobby.GetSlotData(index);
                    if (slot.PromptedForInsurance)
                        InsuranceRequestHandler("", new byte[] { 0 }, player);
                }
            }
        }
        // true if every non-null player has submitted a bet
        private bool AllPlayersHaveSubmitted()
        {
            for (int index = 0; index < 4; index++)
            {
                var player = Lobby.Players[index];
                if (player != null)
                {
                    if (!Lobby.GetSlotData(player).BetSubmitted)
                        return false;
                }
            }
            return true;
        }
        // false if any transactions still pending
        private bool AllPendingTransactionFinal()
        {
            for (int index = 0; index < 4; index++)
            {
                var player = Lobby.Players[index];
                if (player != null)
                {
                    if (Lobby.GetSlotData(player).PendingTransaction)
                        return false;
                }
            }
            return true;
        }
        // true if any non-null player has been debited a bet
        private bool OneOrMoreBetsAccepted()
        {
            for (int index = 0; index < 4; index++)
            {
                var player = Lobby.Players[index];
                if (player != null)
                {
                    if (Lobby.GetSlotData(player).BetAccepted)
                        return true;
                }
            }
            return false;
        }
        // true if no valid players' slots have PromptedForInsurance == true
        private bool AllPlayersRespondedInsurance()
        {
            for (int index = 0; index < 4; index++)
            {
                var player = Lobby.Players[index];
                if (player != null)
                {
                    if (Lobby.GetSlotData(player).PromptedForInsurance)
                        return false;
                }
            }
            return true;
        }
        private List<string> GetAllActiveCardsInPlay(bool includeDealer)
        {
            List<string> allCardsInPlay = new List<string>();

            // if there is a player and their best was accepted, push a list of strings representing the cards in their current hand
            for (int index = 0; index < 4; index++)
            {
                if (Lobby.Players[index] != null)
                {
                    var slot = Lobby.GetSlotData(index);
                    if (slot.BetAccepted)
                    {
                        var myCards = slot.GetCurrentCards();
                        string numberOfCards = myCards.Count + "";
                        allCardsInPlay.AddRange(myCards);
                        allCardsInPlay.Insert(index, numberOfCards);
                    }
                    else
                        allCardsInPlay.Insert(index, 0 + "");
                }
                else
                    allCardsInPlay.Insert(index, 0 + "");
            }

            // add the dealer's cards, too, if applicable
            if (includeDealer)
            {
                var dealersCards = DealerPlayer.GetCurrentCards();
                string number = dealersCards.Count + "";
                allCardsInPlay.AddRange(dealersCards);
                allCardsInPlay.Insert(4, number); // 4 is always dealer
            }

            // final result will be number of cards in 0 1 2 3 corresponding to number of cards belonging to the player in that index, followed
            // by the full list of cards. If dealer is included, [4] will include number of dealer's cards.
            return allCardsInPlay;
        }
        private List<string> GetAllAcceptedBets()
        {
            List<string> allAcceptedBets = new List<string>();

            // if there is a player and their best was accepted push their bet amount
            for (int index = 0; index < 4; index++)
            {
                if (Lobby.Players[index] != null)
                {
                    var slot = Lobby.GetSlotData(index);
                    if (slot.BetAccepted)
                    {
                        allAcceptedBets.Add(slot.BetAmount + "");
                        continue;
                    }
                }
                // either no player present, or bet was not accepted
                allAcceptedBets.Add("0");
            }
            return allAcceptedBets;
        }
        
        private void DealCards()
        {
            short[] playersPlaying = new short[4];
            // first actually deal the cards server side. find any valid player with an accepted bet
            for (int index = 0; index < 4; index++)
            {
                if (Lobby.Players[index] != null)
                {
                    var slot = Lobby.GetSlotData(index);
                    if (slot.BetAccepted)
                    {
                        slot.DealFirstTwoCards(0, CardDeck.Draw(), CardDeck.Draw()); // deal two cards to each player
                        playersPlaying[index] = slot.Client.Avatar.ObjectID;
                    }
                }
            }
            // deal two cards to the dealer as well
            DealerPlayer.DealFirstTwoCards(0, CardDeck.Draw(), CardDeck.Draw());

            // get a list of all the cards and send them to each player for the dealing sequence
            List<string> allCardsInPlay = GetAllActiveCardsInPlay(true);
            
            // goto entre'act, send card dealing sequence, send all cards to all clients
            EnqueueGotoState(VMEODBlackjackStates.Entre_Act);
            // players NOT playing will have their corresponding shorts be 0, but players who need cards will have their avatar.objectIDs sent to plugin
            Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Deal_Initial_Cards_Sequence, playersPlaying));
            Lobby.Broadcast("blackjack_deal_sequence",
                VMEODGameCompDrawACardData.SerializeStrings(allCardsInPlay.ToArray()));

            // sync the bets again, in case one was missed due to desync
            List<string> acceptedBets = GetAllAcceptedBets();
            if (acceptedBets != null)
                Lobby.Broadcast("blackjack_sync_accepted_bets", VMEODGameCompDrawACardData.SerializeStrings(acceptedBets.ToArray()));
        }
        /*
         * This only occurs when a new player joins mid-game, so during any gamestate that is not waiting for player or betting round
         */ 
        private void SyncAllPlayers(VMEODClient client)
        {
            List<string> acceptedBets = GetAllAcceptedBets();
            List<string> allCardsInPlay = GetAllActiveCardsInPlay(true);

            // the very last is the dealer's second card--always.  need to make sure that card is hidden unless it's the dealer's turn or the round
            // is already over, otherwise late-joining players can see the dealer's cards before the active players can!
            if (!GameState.Equals(VMEODBlackjackStates.Dealer_Decision) && !GameState.Equals(VMEODBlackjackStates.Intermission))
            {
                if (allCardsInPlay != null && allCardsInPlay.Count > 2)
                    allCardsInPlay[allCardsInPlay.Count - 1] = "Back";
            }

            if (acceptedBets != null)
                client.Send("blackjack_sync_accepted_bets", VMEODGameCompDrawACardData.SerializeStrings(acceptedBets.ToArray()));
            if (allCardsInPlay != null)
                client.Send("blackjack_sync_all_hands", VMEODGameCompDrawACardData.SerializeStrings(allCardsInPlay.ToArray()));
        }

        private void EnqueueSequence(VMEODClient client, VMEODBlackjackEvents sequenceType, int amount)
        {
            if (client == null)
                return;
            
            var slot = Lobby.GetSlotData(client);

            if (slot != null && slot.Client != null && slot.Client.Avatar.ObjectID == client.Avatar.ObjectID)
            {
                var VM = client.vm;

                // pay from player to object in the amount of their accepted bet
                VM.GlobalLink.PerformTransaction(VM, false, client.Avatar.PersistID, Server.Object.PersistID, amount,

                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                {
                    if (success)
                    {
                        // still same player?
                        if (slot.Client != null && slot.Client.Avatar.ObjectID == client.Avatar.ObjectID)
                        {
                            slot.SimoleonBalance = (int)budget1;
                            switch (sequenceType)
                            {
                                case VMEODBlackjackEvents.Player_Split_Sequence:
                                    {
                                        // execute simantics event to play the split sequence, goto to entre'act to wait
                                        slot.CumulativeBetAmount += amount;
                                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Player_Split_Sequence,
                                            new short[] { slot.Client.Avatar.ObjectID, (short)amount }));
                                        EnqueueGotoState(VMEODBlackjackStates.Entre_Act);
                                        break;
                                    }
                                case VMEODBlackjackEvents.Player_Double_Sequence:
                                    {
                                        // add the new card server-side, execute simantics event to play the hit sequence, goto to entre'act to wait
                                        slot.CumulativeBetAmount += amount;
                                        slot.Double(CardDeck.Draw());
                                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Player_Double_Sequence,
                                            new short[] { slot.Client.Avatar.ObjectID, (short)amount }));
                                        EnqueueGotoState(VMEODBlackjackStates.Entre_Act);
                                        break;
                                    }
                                default:
                                    {
                                        // failsafe disallow betting event
                                        slot.Client.Send("blackjack_toggle_betting", new byte[] { 0 });
                                        // failsafe to send their accept bet amount
                                        slot.Client.Send("blackjack_change_bet", BitConverter.GetBytes(amount));
                                        slot.PendingTransaction = false;
                                        slot.BetSubmitted = true;
                                        slot.BetAccepted = true;
                                        slot.BetAmount = amount;
                                        slot.CumulativeBetAmount = amount;
                                        slot.WarnedForObservation = false;

                                        // animate player
                                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Player_Place_Bet,
                                            new short[] { slot.Client.Avatar.ObjectID, (short)slot.BetAmount }));

                                        // was this the last player who needed to submit their bet?
                                        if (AllPlayersHaveSubmitted())
                                            EnqueueGotoState(VMEODBlackjackStates.Pending_Initial_Transactions);
                                        break;
                                    }
                            }
                        }
                    }
                    else // the transaction failed
                    {
                        if (!sequenceType.Equals(VMEODBlackjackEvents.Player_Hit_Sequence))
                        {
                            // double/split transaction failed, tell them insufficient funds and allow input again
                            slot.Client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Double_NSF });
                            SendEnableInput(slot);
                        }
                        else
                        {
                            // initial bet transaction failed
                            slot.Client.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Bet_NSF });
                            slot.PendingTransaction = false;
                            slot.BetSubmitted = false;
                            slot.BetAccepted = false;
                            slot.BetAmount = 0;
                            // if we're still in the betting round, they can bet so allow betting input
                            if (GameState.Equals(VMEODBlackjackStates.Betting_Round))
                                slot.Client.Send("blackjack_toggle_betting", new byte[] { 1 });
                        }
                    }
                });
            }
        }
        // pays the player their winnings
        private void EnqueuePayout(int payoutAmount, VMEODClient targetClient)
        {
            if (targetClient != null)
            {
                var VM = targetClient.vm;
                var slot = Lobby.GetSlotData(targetClient);

                // pay from object to player
                VM.GlobalLink.PerformTransaction(VM, false, Server.Object.PersistID, targetClient.Avatar.PersistID, payoutAmount,

                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                {
                    if (success)
                    {
                        TableBalance = (int)budget1;
                        if (slot != null && slot.Client != null && slot.Client.Avatar.ObjectID == targetClient.Avatar.ObjectID)
                            slot.SimoleonBalance = (int)budget2;
                    }
                });
            }
        }
        // attempts to deduct payment for insurance, if successful the client is notified so that it may display that the player is insured
        private void EnqueueInsuranceTransaction(int betAmount, VMEODClient insuredParty)
        {
            if (insuredParty != null)
            {
                var halfBetAmount = betAmount / 2;
                var VM = insuredParty.vm;
                var slot = Lobby.GetSlotData(insuredParty);

                // pay from player to object
                VM.GlobalLink.PerformTransaction(VM, false, insuredParty.Avatar.PersistID, Server.Object.PersistID, halfBetAmount,

                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                {
                    if (success)
                    {
                        TableBalance = (int)budget2;
                        // if it's the same player that requested it
                        if (slot != null && slot.Client != null && slot.Client.Avatar.ObjectID == insuredParty.Avatar.ObjectID)
                        {
                            slot.SimoleonBalance = (int)budget1;
                            slot.IsInsured = true;
                            slot.PromptedForInsurance = false;
                            insuredParty.Send("blackjack_insurance_callback", BitConverter.GetBytes(halfBetAmount));
                            // send money over head event
                            Controller.SendOBJEvent(new VMEODEvent((short)VMEODBlackjackEvents.Money_Over_Head,
                                new short[] { insuredParty.Avatar.ObjectID, (short)halfBetAmount }));

                            // if all players have responded to insurance, move on
                            if (AllPlayersRespondedInsurance())
                                EndInsuranceRound();
                        }
                    }
                    else // "You don't have enough money to make that bet."
                        insuredParty.Send("blackjack_alert", new byte[] { (byte)VMEODBlackjackAlerts.Bet_NSF });
                });
            }
        }

        private bool IsTableWithinLimits()
        {
            if (TableBalance >= MaxBet * 6 && TableBalance <= VMEODBlackjackPlugin.TABLE_MAX_BALANCE)
                return true;
            return false;
        }
        /*
         * During betting phase, when a player changes their bet, all other players should be notified in their client.
         */
        private void BroadcastSingleBet(BlackjackPlayer playerWithNewBet)
        {
            if (GameState.Equals(VMEODBlackjackStates.Betting_Round) || GameState.Equals(VMEODBlackjackStates.Player_Decision))
            {
                if (playerWithNewBet != null && playerWithNewBet.Client != null && playerWithNewBet.CumulativeBetAmount > -1)
                {
                    Lobby.Broadcast("blackjack_bet_update_player" + playerWithNewBet.PlayerIndex, playerWithNewBet.CumulativeBetAmount + "");
                }
            }
        }

        #endregion
    }
    public class BlackjackPlayer
    {
        private bool _BetAccepted;
        private bool _BetSubmitted;
        private bool _IsInsured;
        private bool _PendingTransaction;
        private bool _PromptedForInsurance;
        private bool _WarnedForObservation;

        private VMEODClient _Client;
        private List<PlayingCard>[] Hands;
        private VMEODBlackjackHandTypes[] HandTypes;
        private int _BetAmount;
        private int _CumulativeBetAmount;
        private int _CurrentHand;
        private int _PlayerIndex;
        private int _SimoleonBalance;
        private int _TotalHands;

        internal delegate void BetChange(BlackjackPlayer player);

        public BlackjackPlayer()
        {
            ResetHands();
        }
        public VMEODClient Client
        {
            get { return _Client; }
            set { _Client = value; }
        }
        public bool BetAccepted
        {
            get { return _BetAccepted; }
            set { _BetAccepted = value; }
        }
        public bool BetSubmitted
        {
            get { return _BetSubmitted; }
            set { _BetSubmitted = value; }
        }
        public bool PendingTransaction
        {
            get { return _PendingTransaction; }
            set { _PendingTransaction = value; }
        }
        // only used by dealer hand
        public bool SoftTotal
        {
            get
            {
                bool isSoftTotal = false;
                var hand = Hands[_CurrentHand];
                int handTotal = 0;
                if (hand != null)
                {
                    if (hand.Count > 1)
                    {
                        foreach (var card in hand)
                        {
                            string value = card.Value.ToString();
                            if (value.Equals(PlayingCardValues.Ace.ToString()))
                            {
                                if (!isSoftTotal && handTotal + 10 < 22)
                                {
                                    isSoftTotal = true;
                                    handTotal += 10;
                                }
                            }
                            handTotal += VMEODBlackjackPlugin.PlayingCardBlackjackValues[value];
                        }
                        // if it's over 21 from additional 10 given by an Ace, remove that 10 and the Ace is now a value of 1
                        if (handTotal > 21 && isSoftTotal)
                        {
                            isSoftTotal = false;
                            handTotal -= 10;
                        }
                    }
                }
                return isSoftTotal;
            }
        }
        public bool WarnedForObservation
        {
            get { return _WarnedForObservation; }
            set { _WarnedForObservation = value; }
        }
        public bool IsInsured
        {
            get { return _IsInsured; }
            set { _IsInsured = value; }
        }
        public bool PromptedForInsurance
        {
            get { return _PromptedForInsurance; }
            set { _PromptedForInsurance = value; }
        }
        public int PlayerIndex
        {
            get { return _PlayerIndex; }
            set { _PlayerIndex = value; }
        }
        public int SimoleonBalance
        {
            get { return _SimoleonBalance; }
            set { _SimoleonBalance = value; }
        }
        public int TotalHands
        {
            get { return _TotalHands; }
        }
        public int CurrentHand
        {
            get { return _CurrentHand; }
        }
        public int BetAmount
        {
            get { return _BetAmount; }
            set { _BetAmount = value; }
        }
        public int CumulativeBetAmount
        {
            get { return _CumulativeBetAmount; }
            set
            {
                _CumulativeBetAmount = value;
                if (value > -1)
                    OnPlayerBetChange.Invoke(this);
            }
        }
        public VMEODBlackjackHandTypes CurrentHandType
        {
            get
            {
                var hand = Hands[_CurrentHand];
                if (hand != null && HandTypes != null)
                    return HandTypes[_CurrentHand];
                return VMEODBlackjackHandTypes.Empty;
            }
        }
        public int CurrentHandTotal
        {
            get {
                var hand = Hands[_CurrentHand];
                int handTotal = 0;
                if (hand != null)
                {
                    if (hand.Count > 1)
                    {
                        bool isSoftTotal = false;
                        foreach (var card in hand)
                        {
                            string value = card.Value.ToString();
                            if (value.Equals(PlayingCardValues.Ace.ToString()))
                            {
                                if (!isSoftTotal && handTotal + 10 < 22)
                                {
                                    isSoftTotal = true;
                                    handTotal += 10;
                                }
                            }
                            handTotal += VMEODBlackjackPlugin.PlayingCardBlackjackValues[value];
                        }
                        // if it's over 21 from additional 10 given by an Ace, remove that 10 and the Ace is now a value of 1
                        if (handTotal > 21 && isSoftTotal)
                        {
                            isSoftTotal = false;
                            handTotal -= 10;
                        }
                    }
                }
                return handTotal;
            }
        }
        public int TotalBetsAccrossAllHands
        {
            get
            {
                int totalBets = 0;
                for (int index = 0; index < _TotalHands; index++)
                {
                    if (HandTypes[index].Equals(VMEODBlackjackHandTypes.Doubled_Down))
                        totalBets += _BetAmount * 2;
                    else
                        totalBets += BetAmount;
                }
                return totalBets;
            }
        }

        internal event BetChange OnPlayerBetChange;

        // used exclusively by the dealer
        public bool IsMyFirstCardAnAce()
        {
            var hand = Hands[CurrentHand];
            if (hand.Count > 1)
            {
                if (hand[0].Value.Equals(PlayingCardValues.Ace))
                    return true;
            }
            return false;
        }
        public List<String> GetCurrentCards()
        {
            List<String> cards = new List<string>();
            var hand = Hands[_CurrentHand];
            if (hand != null)
            {
                foreach (var card in hand)
                {
                    cards.Add(card.Value.ToString() + "_" + card.Suit.ToString()); // e.g. "Five_Clubs"
                }
            }
            return cards;
        }

        public void ResetHands()
        {
            Hands = new List<PlayingCard>[4];
            for (int index = 0; index < 4; index++)
            {
                Hands[index] = new List<PlayingCard>();
            }
            _BetAmount = 0;
            _BetAccepted = false;
            _BetSubmitted = false;
            _PendingTransaction = false;
            _CurrentHand = 0;
            _TotalHands = 0;
            _IsInsured = false;
            _PromptedForInsurance = false;
            HandTypes = new VMEODBlackjackHandTypes[4]
                { VMEODBlackjackHandTypes.Empty, VMEODBlackjackHandTypes.Empty, VMEODBlackjackHandTypes.Empty, VMEODBlackjackHandTypes.Empty };
        }
        /*
         * If false is returned, it is the final hand of the player. True means the player has more hands.
         */
        public bool StandAndGotoNextHand()
        {
            Stand();
            if (_CurrentHand < TotalHands - 1)
            {
                _CurrentHand++;
                return true;
            }
            else
                return false;
        }
        public void Stand()
        {
            // leave blackjack, doubled_down, bust, and double_bust alone
            switch (CurrentHandType)
            {
                case VMEODBlackjackHandTypes.Standard:
                case VMEODBlackjackHandTypes.Double_Worthy:
                case VMEODBlackjackHandTypes.Split_Worthy:
                    HandTypes[_CurrentHand] = VMEODBlackjackHandTypes.Stand;
                    break;
            }
        }
        public void Hit(PlayingCard newCard)
        {
            Hands[_CurrentHand].Add(newCard);
            var total = CurrentHandTotal;
            HandTypes[_CurrentHand] = (total < 22) ? VMEODBlackjackHandTypes.Standard : VMEODBlackjackHandTypes.Bust;
        }
        public string[] Split(PlayingCard[] newCards)
        {
            /*
             * First: deal with the actual "physical" hands on the server
             */

            var oldHand = Hands[CurrentHand];

            // if the next hand already contains cards, it needs to be moved
            int index = 4;
            while (--index > 0)
            {
                if (!HandTypes[index - 1].Equals(VMEODBlackjackHandTypes.Empty))
                {
                    // copy the hand over
                    var sourceHand = Hands[index - 1];
                    var card0 = sourceHand[0];
                    var card1 = sourceHand[1];
                    DealFirstTwoCards(index, card0, card1);
                }
            }
            DealFirstTwoCards(CurrentHand + 1, new PlayingCard(oldHand[0]), newCards[1]); // new hand
            DealFirstTwoCards(CurrentHand, new PlayingCard(oldHand[1]), newCards[0]); // current hand updated

            /*
             * Second: deal with returning the cards list in order to update the player's UI via server messsage
             */
            List<string> oldAndNewHand = new List<string>(GetCurrentCards()); // first two cards
            var newHand = Hands[CurrentHand + 1];
            foreach (var card in newHand)
                oldAndNewHand.Add(card.Value.ToString() + "_" + card.Suit.ToString());
            // instert the splitting index
            oldAndNewHand.Insert(0, "" + CurrentHand);
            return oldAndNewHand.ToArray();
        }
        public void Double(PlayingCard newCard)
        {
            Hands[_CurrentHand].Add(newCard);
            var total = CurrentHandTotal;
            HandTypes[_CurrentHand] = (total < 22) ? VMEODBlackjackHandTypes.Doubled_Down : VMEODBlackjackHandTypes.Doubled_Bust;
        }
        public void DealFirstTwoCards(int targetHandIndex, PlayingCard first, PlayingCard second)
        {
            // redealing this hand due to a split, thus not adding a hand
            if (Hands[targetHandIndex].Count != 0)
                Hands[targetHandIndex] = new List<PlayingCard>();
            else
                _TotalHands++;
            Hands[targetHandIndex].Add(first);
            Hands[targetHandIndex].Add(second);
            VMEODBlackjackHandTypes type = VMEODBlackjackHandTypes.Double_Worthy; // default is double
            /*
             * determine hand type
             */
            if (first.Value.Equals(PlayingCardValues.Ace)) // first card is ace
            {
                // blackjack- second card values at 10: ten, jack, queen, or king
                if ((int)second.Value >= 10)
                    type = VMEODBlackjackHandTypes.Blackjack;
                // split, second card is also ace
                if (second.Value.Equals(PlayingCardValues.Ace))
                    type = VMEODBlackjackHandTypes.Split_Worthy;
            }
            else if ((int)first.Value >= 10) // first card is ten, jack, queen, or king
            {
                // blackjack- second card is ace
                if (second.Value.Equals(PlayingCardValues.Ace))
                    type = VMEODBlackjackHandTypes.Blackjack;
                // split- second card also values at 10: ten, jack, queen, or king
                else if ((int)second.Value >= 10)
                    type = VMEODBlackjackHandTypes.Split_Worthy;
            }
            else if (first.Value.Equals(second.Value)) // cards are exact same number, can split
                type = VMEODBlackjackHandTypes.Split_Worthy;
            HandTypes[targetHandIndex] = type; // default is double: 2 not split-able non-blackjack cards can be doubled
        }
        public int CalculateTotalPayout(int dealersHandTotal, VMEODBlackjackHandTypes dealerHandType)
        {
            int totalPayout = 0;

            // dealer has blackjack
            if (dealerHandType.Equals(VMEODBlackjackHandTypes.Blackjack))
            {
                // check every hand against the dealer's total. Hand types affect payout if they're blackjack only, which would be a push
                if (_TotalHands > 0)
                {
                    _CurrentHand = 0;
                    while (_CurrentHand < _TotalHands)
                    {
                        // if they also have a blackjack, they get their bet back
                        if (CurrentHandType.Equals(VMEODBlackjackHandTypes.Blackjack))
                            totalPayout += BetAmount;
                        _CurrentHand++;
                    }
                    // now check for insurance, which cost half their bet. Since dealer did get blackjack, if they're insured they get full bet amount back
                    if (_IsInsured)
                        totalPayout += BetAmount;
                }
            }
            // dealer does not have blackjack
            else
            {
                // check every hand against the dealer's total. Hand types affect payout ratio if they're blackjack or doubled down.
                if (_TotalHands > 0)
                {
                    _CurrentHand = 0;
                    while (_CurrentHand < _TotalHands)
                    {
                        var handTotal = CurrentHandTotal;
                        if (handTotal < 22)
                        {
                            // win!
                            if (dealersHandTotal > 21 || dealersHandTotal < handTotal)
                            {
                                switch (CurrentHandType)
                                {
                                    case VMEODBlackjackHandTypes.Blackjack:
                                        {
                                            totalPayout += BetAmount + (int)(1.5 * BetAmount); // 3:2 or 1.5 times bet + original bet back
                                            break;
                                        }
                                    case VMEODBlackjackHandTypes.Doubled_Down:
                                        {
                                            totalPayout += 2* (BetAmount * 2); // double your bet, which was already doubled, plus original (doubled) bet back
                                            break;
                                        }
                                    case VMEODBlackjackHandTypes.Stand:
                                        {
                                            totalPayout += BetAmount * 2; // double your bet, i.e. your bet amount plus original bet back
                                            break;
                                        }
                                }
                            }
                            // push
                            else if (dealersHandTotal == handTotal)
                            {
                                // if player has blackjack, it still beats dealer's 21, which we know cannot be here
                                if (CurrentHandType.Equals(VMEODBlackjackHandTypes.Blackjack))
                                    totalPayout += BetAmount + (int)(1.5 * BetAmount); // 3:2 or 1.5 times bet + original bet back
                                else
                                    totalPayout += BetAmount; // original bet back for push
                            }
                        }
                        _CurrentHand++;
                    }
                }
            }
            return totalPayout;
        }
    }
    public enum VMEODBlackjackStates
    {
        Managing = -2,
        Invalid = -1,
        Closed = 0,
        Waiting_For_Player = 1,
        Betting_Round = 2,
        Pending_Initial_Transactions = 3,
        Entre_Act = 4, // used for waiting for cards to be dealt at round start or for animations of hit (& double) and stand
        Player_Decision = 5,
        Dealer_Decision = 6,
        Intermission = 7, // dealer collect chips animations, player win/lose animations, pay winners here
        Insurance_Prompts = 8,
        Finale = 9
    }
    public enum VMEODBlackjackHandTypes: byte
    {
        Empty = 0,
        Standard = 1,
        Double_Worthy = 2,
        Stand = 3,
        Doubled_Down = 4,
        Doubled_Bust = 5,
        Split_Worthy = 20,
        Blackjack = 21,
        Bust = 22,
    }
    public enum VMEODBlackjackEvents: short
    {
        Deal_Initial_Cards_Sequence = 1, // upon completion, invokes: Animation_Sequence_Complete
        Dealer_Collect_Chips = 2, // declare loser - upon completion, invokes: Dealer_Callback
        Dealer_Declare_winner = 3, // upon completion, invokes: Dealer_Callback
        Player_Place_Bet = 4,
        Dealer_Check_Blackjack = 5, // upon completion, invokes: Dealer_Check_Callback
        Player_Win_Animation = 6,
        Player_Lose_Animation = 7,
        Player_Stand_Sequence = 8, // upon completion, invokes: Animation_Sequence_Complete
        Player_Hit_Sequence = 9, // upon completion, invokes: Animation_Sequence_Complete
        Player_Double_Sequence = 10, // upon completion, invokes: Animation_Sequence_Complete
        Player_Split_Sequence = 11, // upon completion, invokes: Split_Sequence_Complete
        Dealer_Hit_Self_Sequence = 12, // upon completion, invokes: Animation_Sequence_Complete
        Dealer_Collect_Cards = 13, // upon completion, invokes: Dealer_Callback
        New_Minimum_Bet = 14,
        New_Maximum_Bet = 15,
        Money_Over_Head = 16, // playerid sent in temp0, amount sent in temp1
        //Player_Push_Animation = 17,
        Failsafe_Delete_ID = 18, // set the playerid for this player to 0
        Failsafe_Remove_Dealer = 19, // set the attribute dealerid to 0
        Force_End_Playing = 20,
        // plugin only
        Animation_Sequence_Complete = 100, // call back during entre'act
        Dealer_Check_Callback = 101, // during entre'act after insurance prompts
        Split_Sequence_Complete = 102,
        Dealer_Callback = 103 // call back during intermission
    }
    public enum VMEODBlackjackAlerts: byte
    {
        State_Race = 1, // race condition with states, very bad
        False_Start = 2,
        Illegal_Hit = 3,
        Illegal_Double = 4,
        Illegal_Split = 5,
        Bet_Too_Low = 6,
        Bet_Too_High = 7,
        Bet_NSF = 8,
        Double_NSF = 9,
        Split_NSF = 10,
        Observe_Once = 11,
        Observe_Twice = 12,
        Table_NSF = 13,
        Player_NSF = 14
    }
}