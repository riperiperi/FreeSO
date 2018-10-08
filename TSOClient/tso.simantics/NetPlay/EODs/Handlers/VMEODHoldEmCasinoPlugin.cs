using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.SimAntics.NetPlay.EODs.Utils;
using FSO.SimAntics.NetPlay.EODs.Handlers.Data;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.EODs.Model;
using HoldemHand;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODHoldEmCasinoPlugin : VMEODHandler
    {
        private int ActivePlayerIndex;
        private int BettingTimer;
        private AbstractPlayingCardsDeck CardDeck;
        private int DecisionTimer;
        private int IdleTimer;
        private EODLobby<HoldEmCasinoPlayer> Lobby;
        private int MaxAnteBet;
        private int MaxSideBet;
        private int MinAnteBet;
        private int TableBalance;
        private int Tock;
        private VMEODHoldEmCasinoStates GameState;
        private VMEODHoldEmCasinoStates NextState = VMEODHoldEmCasinoStates.Invalid;
        private bool DealerIntermissionComplete;
        private List<VMEODEvent> DealerEventsQueue;
        private List<VMEODClient> SyncQueue;

        private HoldEmCasinoPlayer CommunityHand;
        private VMEODClient Controller;
        private VMEODClient Dealer;
        private HoldEmCasinoPlayer DealerPlayer;
        private VMEODClient Owner;

        public static readonly int MAXIMUM_BET = 1000;
        public static readonly int WORST_CASE_ANTE_PAYOUT_RATIO = 84;
        public static readonly int WORST_CASE_SIDE_PAYOUT_RATIO = 104;

        public VMEODHoldEmCasinoPlugin(VMEODServer server) : base(server)
        {
            Lobby = new EODLobby<HoldEmCasinoPlayer>(server, 4)
                    .BroadcastPlayersOnChange("holdemcasino_players_update")
                    .OnFailedToJoinDisconnect();
            ActivePlayerIndex = -1;
            DealerPlayer = new HoldEmCasinoPlayer();
            CommunityHand = new HoldEmCasinoPlayer();
            CardDeck = new AbstractPlayingCardsDeck(1, false);

            // listeners
            BinaryHandlers["holdemcasino_decision"] = DecisionRequestHandler;
            BinaryHandlers["holdemcasino_submit_bets"] = BetChangeRequestHandler;
            BinaryHandlers["holdemcasino_close"] = UIClosedHandler;
            SimanticsHandlers[(short)VMEODHoldEmCasinoEvents.Animation_Sequence_Complete] = AnimationCompleteHandler;
            SimanticsHandlers[(short)VMEODHoldEmCasinoEvents.Dealer_Callback] = DealerCallbackHandler;
            // owner
            PlaintextHandlers["holdemcasino_deposit"] = DepositHandler;
            PlaintextHandlers["holdemcasino_withdraw"] = WithdrawHandler;
            PlaintextHandlers["holdemcasino_new_minimum"] = NewMinimumAnteBetHandler;
            PlaintextHandlers["holdemcasino_new_maximum"] = NewMaximumAnteBetHandler;
            PlaintextHandlers["holdemcasino_new_side"] = NewMaximumSideBetHandler;

            DealerIntermissionComplete = true;
            DealerEventsQueue = new List<VMEODEvent>();
            SyncQueue = new List<VMEODClient>();
        }
        #region Public
        public override void Tick()
        {
            if (Controller == null)
                return;

            // sync any latecomers' UIEODs
            lock (SyncQueue)
            {
                if (SyncQueue.Count > 0)
                {
                    for (int index = 0; index < SyncQueue.Count; index++)
                        SyncAllPlayers(SyncQueue[index]);
                    SyncQueue.Clear();
                }
            }

            // handle next state
            if (NextState != VMEODHoldEmCasinoStates.Invalid)
            {
                var state = NextState;
                NextState = VMEODHoldEmCasinoStates.Invalid;
                GotoState(state);
            }

            switch (GameState)
            {
                case VMEODHoldEmCasinoStates.Waiting_For_Player:
                    {
                        if (!Lobby.IsEmpty())
                            EnqueueGotoState(VMEODHoldEmCasinoStates.Betting_Round);
                        break;
                    }
                case VMEODHoldEmCasinoStates.Betting_Round:
                    {
                        // check to see if all connected playesrs have submitted a bet - this check is for player disconnections
                        if (AllPlayersHaveSubmitted())
                        {
                            EnqueueGotoState(VMEODHoldEmCasinoStates.Pending_Initial_Transactions);
                        }
                        // there are still connected players who have not bet
                        else
                        {
                            if (BettingTimer > 0)
                            {
                                if (++Tock >= 30)
                                {
                                    BettingTimer--;
                                    Lobby.Broadcast("holdemcasino_timer", BitConverter.GetBytes(BettingTimer));
                                    Tock = 0;
                                }
                            }
                            // ran out of time
                            else
                                ForceEndBettingRound();
                        }
                        break;
                    }
                case VMEODHoldEmCasinoStates.Pending_Initial_Transactions:
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
                                    DealIntitialCards();
                                else
                                {
                                    if (Lobby.IsEmpty())
                                        EnqueueGotoState(VMEODHoldEmCasinoStates.Waiting_For_Player);
                                    else
                                        EnqueueGotoState(VMEODHoldEmCasinoStates.Betting_Round);
                                }
                            }
                            // if they're not final, did someone just join? quick race condition check
                            else
                            {
                                // oops someone joined. round must be changed by force
                                if (!AllPlayersHaveSubmitted())
                                {
                                    ForceEndBettingRound();
                                    EnqueueGotoState(VMEODHoldEmCasinoStates.Entre_Act);
                                }
                                else
                                {
                                    // todo: failsafe give people their money back and reset
                                    RefundAllPlayers();
                                    EnqueueGotoState(VMEODHoldEmCasinoStates.Closed);
                                }
                            }
                        }
                        break;
                    }
                case VMEODHoldEmCasinoStates.Player_Decision:
                    {
                        if (DecisionTimer > 0)
                        {
                            if (++Tock >= 30)
                            {
                                DecisionTimer--;
                                Lobby.Broadcast("holdemcasino_timer", BitConverter.GetBytes(DecisionTimer));
                                Tock = 0;
                            }
                        }
                        else
                        {
                            ForceFold(false);
                            EnqueueGotoState(VMEODHoldEmCasinoStates.Entre_Act);
                        }
                        break;
                    }
                case VMEODHoldEmCasinoStates.Awaiting_Evaluation:
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
                            // we are either checking to see if the side bets are evaluated, or the final hands
                            if (CommunityHand.GetCurrentCards().Count == 3) // 3 cards means side bet payouts
                            {
                                if (AllSideBetsEvaluated())
                                    EnqueueGotoState(VMEODHoldEmCasinoStates.Side_Bet_Payouts);
                                else
                                {
                                    // todo: failsafe give people their money back and reset
                                    RefundAllPlayers();
                                    EnqueueGotoState(VMEODHoldEmCasinoStates.Closed);
                                }
                            }
                            else // 5 cards, so checking final hands for ante/call payouts
                            {
                                if (AllHandsEvaluated())
                                    EnqueueGotoState(VMEODHoldEmCasinoStates.Finale);
                                else
                                {
                                    // todo: failsafe give people their money back and reset
                                    RefundAllPlayers();
                                    EnqueueGotoState(VMEODHoldEmCasinoStates.Closed);
                                }
                            }
                        }
                        break;
                    }
                case VMEODHoldEmCasinoStates.Hold:
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
                            EnqueueGotoState(VMEODHoldEmCasinoStates.Player_Decision);
                        break;
                    }
            }
            base.Tick();
        }
        public override void OnConnection(VMEODClient client)
        {
            if (client.Avatar != null)
            {
                // args[0] is Max Ante Bet, args[1] is Min Ante Bet, args[4] is Max Side Bet
                var args = client.Invoker.Thread.TempRegisters;
                MinAnteBet = args[1];
                MaxAnteBet = args[0];
                MaxSideBet = args[4];
                if (args[2] == 0) // is a player
                {
                    // get bets accepted from other players
                    List<string> acceptedBets = GetAllAcceptedBets();

                    // using their position at the table, put them in the proper index/slot
                    short playerIndex = args[3]; // args[3] is 1, 2, 3, or 4
                    playerIndex--;

                    if (playerIndex < 0 || playerIndex > 3) // invalid, get out
                    {
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Force_End_Playing, client.Avatar.ObjectID));
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
                                        slot.ResetHand();
                                        slot.OnPlayerAnteBetChange += BroadcastSingleAnteBet;
                                        slot.OnPlayerSideBetChange += BroadcastSingleSideBet;

                                        // set the table data
                                        var balance = (int)budget1;
                                        TableBalance = balance;

                                        if (balance >= (MaxAnteBet * WORST_CASE_ANTE_PAYOUT_RATIO + MaxSideBet * WORST_CASE_SIDE_PAYOUT_RATIO)
                                            && balance <= VMEODBlackjackPlugin.TABLE_MAX_BALANCE)
                                        {
                                            string[] data = new string[] { "" + playerIndex, MinAnteBet + "", MaxAnteBet + "", MaxSideBet + "",
                                                "" + Dealer.Avatar.ObjectID };

                                            // event to show the UI to the player
                                            client.Send("holdemcasino_player_show", VMEODGameCompDrawACardData.SerializeStrings(data));

                                            if (Dealer != null)
                                            {
                                                if (GameState.Equals(VMEODHoldEmCasinoStates.Waiting_For_Player) || GameState.Equals(VMEODHoldEmCasinoStates.Closed))
                                                    EnqueueGotoState(VMEODHoldEmCasinoStates.Betting_Round);
                                                else // player is joining mid-game or mid-betting round
                                                {
                                                    if (GameState.Equals(VMEODHoldEmCasinoStates.Betting_Round))
                                                    {
                                                        // send all ante and side bets
                                                        if (acceptedBets != null && acceptedBets.Count > 0)
                                                            client.Send("holdemcasino_sync_accepted_bets", VMEODGameCompDrawACardData.SerializeStrings(acceptedBets.ToArray()));
                                                        client.Send("holdemcasino_toggle_betting", new byte[] { 1 }); // "Place your bets..."
                                                    }
                                                    else
                                                    {
                                                        lock (SyncQueue)
                                                            SyncQueue.Add(client);
                                                        client.Send("holdemcasino_toggle_betting", new byte[] { 0 }); // disallow betting
                                                        if (ActivePlayerIndex > -1)
                                                            client.Send("holdemcasino_late_comer", new byte[] { (byte)ActivePlayerIndex }); // "So-and-so's turn."
                                                    }
                                                }
                                            }
                                        }
                                        else  // the table does not have enough money
                                        {
                                            client.Send("holdemcasino_alert", new byte[] { (byte)VMEODHoldEmCasinoAlerts.Table_NSF });
                                            Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Force_End_Playing, client.Avatar.ObjectID));
                                        }
                                    }
                                    else  // the table does not have enough money or transaction failed for some reason
                                    {
                                        client.Send("holdemcasino_alert", new byte[] { (byte)VMEODHoldEmCasinoAlerts.Table_NSF });
                                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Force_End_Playing, client.Avatar.ObjectID));
                                    }
                                });
                        } // slot was null, should never happen
                        else
                        {
                            if (client != null && client.Avatar != null)
                                Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Force_End_Playing, client.Avatar.ObjectID));
                        }
                    } // could not join lobby
                    else
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Force_End_Playing, client.Avatar.ObjectID));
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
                                    GameState = VMEODHoldEmCasinoStates.Managing;
                                    TableBalance = (int)budget2;
                                    client.Send("holdemcasino_owner_show", TableBalance + "%" + MinAnteBet + "%" + MaxAnteBet + "%" + MaxSideBet);
                                }
                                else
                                    Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Failsafe_Remove_Dealer, client.Avatar.ObjectID));
                            });
                    }
                    else // not the owner, just the dealer
                    {
                        // The dealer's client is only used for animations. They literally have no other function.
                        Dealer = client;
                        if (GameState.Equals(VMEODHoldEmCasinoStates.Closed))
                            EnqueueGotoState(VMEODHoldEmCasinoStates.Waiting_For_Player);
                    }
                } // end if owner or dealer
            } // end if client.avatar is not null
            else
                Controller = client;

            base.OnConnection(client);
        }
        public override void OnDisconnection(VMEODClient client)
        {
            var slot = Lobby.GetSlotData(client);
            int playerIndex = -1;
            // slot will be null if owner or npc disconnected
            if (slot != null)
            {
                playerIndex = slot.PlayerIndex;
                slot.OnPlayerAnteBetChange -= BroadcastSingleAnteBet;
                slot.OnPlayerSideBetChange -= BroadcastSingleSideBet;
                slot.WarnedForObservation = false;
                slot.ResetHand();
                Lobby.Leave(client);
                if (playerIndex == ActivePlayerIndex)
                    ForceFold(true);
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Failsafe_Delete_ID, (short)(slot.PlayerIndex + 1)));
                //slot.Client = null;
            }
            if (Lobby.IsEmpty()) // no players
            {
                if (Dealer != null && client.Avatar != null)
                {
                    // if the client disconnecting is NOT the dealer, go to waiting for player
                    if (Dealer.Avatar.ObjectID != client.Avatar.ObjectID)
                        EnqueueGotoState(VMEODHoldEmCasinoStates.Waiting_For_Player);
                    else
                        EnqueueGotoState(VMEODHoldEmCasinoStates.Closed);
                }
                else
                    GameState = VMEODHoldEmCasinoStates.Closed;
            }
            base.OnDisconnection(client);
        }
        #endregion
        #region owner events
        /*
         * Events for the owner to stock the table and set the min/max ante and max side bets
         */
        private void NewMinimumAnteBetHandler(string evt, string newMinString, VMEODClient client)
        {
            string failureReason = "";
            short newMinBet;
            var result = Int16.TryParse(newMinString.Trim(), out newMinBet);
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == client.Avatar.PersistID);
            if (!isOwner)
                failureReason = VMEODSlotsInputErrorTypes.InvalidOwner.ToString(); // not the owner
            else if (result)
            {
                // proposed minimum bet must be greater than $0
                if (newMinBet < 1)
                    failureReason = VMEODRouletteInputErrorTypes.BetTooLow.ToString();
                // proposed minimum bet must not be greater than the MaxAnteBet
                else if (newMinBet > MaxAnteBet)
                    failureReason = VMEODRouletteInputErrorTypes.BetTooHigh.ToString();
                else
                {
                    // does the object have enough money to cover this bet amount?
                    if (TableBalance < MaxSideBet * WORST_CASE_SIDE_PAYOUT_RATIO + newMinBet * WORST_CASE_ANTE_PAYOUT_RATIO)
                        failureReason = VMEODRouletteInputErrorTypes.BetTooHighForBalance.ToString();
                    else
                    {
                        // valid new minimum bet
                        MinAnteBet = newMinBet;
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.New_Minimum_Bet, newMinBet));
                        client.Send("holdemcasino_min_bet_success", "" + newMinBet);
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
            client.Send("holdemcasino_n_bet_fail", failureReason);
        }
        private void NewMaximumAnteBetHandler(string evt, string newMaxString, VMEODClient client)
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
                if (newMaxBet < MinAnteBet)
                    failureReason = VMEODRouletteInputErrorTypes.BetTooLow.ToString();
                // proposed maximum bet must not be greater than $1000 for short data type restrictions
                else if (newMaxBet > VMEODHoldEmCasinoPlugin.MAXIMUM_BET)
                    failureReason = VMEODRouletteInputErrorTypes.BetTooHigh.ToString();
                else
                {
                    // does the object have enough money to cover this bet amount?
                    if (TableBalance < MaxSideBet * WORST_CASE_SIDE_PAYOUT_RATIO + newMaxBet * WORST_CASE_ANTE_PAYOUT_RATIO)
                        failureReason = VMEODRouletteInputErrorTypes.BetTooHighForBalance.ToString();
                    else
                    {
                        // valid new max bet
                        MaxAnteBet = newMaxBet;
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.New_Maximum_Bet, newMaxBet));
                        client.Send("holdemcasino_max_bet_success", "" + newMaxBet);
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
            client.Send("holdemcasino_x_bet_fail", failureReason);
        }
        private void NewMaximumSideBetHandler(string evt, string newMaxString, VMEODClient client)
        {
            string failureReason = "";
            short newMaxBet;
            var result = Int16.TryParse(newMaxString.Trim(), out newMaxBet);
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == client.Avatar.PersistID);
            if (!isOwner)
                failureReason = VMEODSlotsInputErrorTypes.InvalidOwner.ToString(); // not the owner
            else if (result)
            {
                // proposed maximum side bet must be 0 or greater
                if (newMaxBet < 0)
                    failureReason = VMEODRouletteInputErrorTypes.BetTooLow.ToString();
                // proposed maximum side bet must not be greater than $1000 for short data type restrictions
                else if (newMaxBet > VMEODHoldEmCasinoPlugin.MAXIMUM_BET)
                    failureReason = VMEODRouletteInputErrorTypes.BetTooHigh.ToString();
                else
                {
                    // does the object have enough money to cover this bet amount?
                    if (TableBalance < newMaxBet * WORST_CASE_SIDE_PAYOUT_RATIO + MaxAnteBet * WORST_CASE_ANTE_PAYOUT_RATIO)
                        failureReason = VMEODRouletteInputErrorTypes.BetTooHighForBalance.ToString();
                    else
                    {
                        // valid new max side bet
                        MaxSideBet = newMaxBet;
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.New_Max_Side_Bet, newMaxBet));
                        client.Send("holdemcasino_side_bet_success", "" + newMaxBet);
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
            client.Send("holdemcasino_s_bet_fail", failureReason);
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
                        client.Send("holdemcasino_resume_manage", TableBalance + "");
                    }
                    else
                        client.Send("holdemcasino_withdraw_fail", VMEODSlotsInputErrorTypes.Unknown.ToString());
                });
            }
            else // otherwise, send the failureReason
            {
                if (failureReason.Length == 0)
                    failureReason = VMEODSlotsInputErrorTypes.InvalidOwner.ToString();
                client.Send("holdemcasino_withdraw_fail", failureReason);
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
                        client.Send("holdemcasino_resume_manage", TableBalance + "");
                    }
                    else
                    {
                        // Owner does not have enough simoleons to make this deposit
                        client.Send("holdemcasino_deposit_NSF", "" + amountString);
                    }
                });
            }
            else // otherwise, send the failureReason
            {
                if (failureReason.Length == 0)
                    failureReason = VMEODSlotsInputErrorTypes.InvalidOwner.ToString();
                client.Send("holdemcasino_deposit_fail", failureReason);
            }
        }
        #endregion // dealer events
        #region Events
        // client closes UI
        // if the data sent equals 0, the client is a player. if it's 1, the client is the owner and closed the managing window
        private void UIClosedHandler(string evt, byte[] data, VMEODClient client)
        {
            if (client != null && client.Avatar != null)
            {
                if (data[0] == 0)
                    Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Force_End_Playing, client.Avatar.ObjectID));
                else
                    Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Failsafe_Remove_Dealer, client.Avatar.ObjectID));
            }
        }
        private void DecisionRequestHandler(string evt, byte[] decisionType, VMEODClient client)
        {
            if (GameState.Equals(VMEODHoldEmCasinoStates.Player_Decision))
            {
                // is this client a player
                var player = Lobby.GetPlayerSlot(client);
                if (player > -1)
                {
                    // is this the current active player?
                    if (player == ActivePlayerIndex)
                    {
                        var slot = Lobby.GetSlotData(player);
                        // if player wants to "Call"
                        if (decisionType[0] == 1)
                        {
                            // can they afford to call?
                            if (slot.SimoleonBalance >= slot.AnteBetAmount * 2)
                            {
                                EnqueueSequence(client, VMEODHoldEmCasinoEvents.Player_Call_Sequence, slot.AnteBetAmount * 2);
                                // broadcast this player's action
                                Lobby.Broadcast("holdemcasino_call_broadcast", new byte[] { (byte)(slot.PlayerIndex + 1) });
                            }
                            else
                            {
                                client.Send("holdemcasino_alert", new byte[] { (byte)VMEODHoldEmCasinoAlerts.Call_NSF });
                                SendEnableInput(slot);
                            }
                        }
                        // if player wants to "Fold"
                        else
                        {
                            Lobby.Broadcast("holdemcasino_fold_broadcast", new byte[] { (byte)(slot.PlayerIndex + 1) });
                            slot.HasFolded = true;
                            // send fold animation
                            Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Player_Fold_Sequence, slot.Client.Avatar.ObjectID));
                            EnqueueGotoState(VMEODHoldEmCasinoStates.Entre_Act);
                        }
                    }
                    else
                    {
                        // it is not this player's turn
                        client.Send("holdemcasino_alert", new byte[] { (byte)VMEODHoldEmCasinoAlerts.False_Start });
                    }
                    client.Send("holdemcasino_decision_callback", decisionType); // disables their decision buttons
                }
            }
        }
        // client tries to submit bet
        private void BetChangeRequestHandler(string evt, byte[] newBetsSerialized, VMEODClient client)
        {
            if (GameState.Equals(VMEODHoldEmCasinoStates.Betting_Round))
            {
                var slot = Lobby.GetSlotData(client);
                if (slot != null & newBetsSerialized != null)
                {
                    string[] betStrings = VMEODGameCompDrawACardData.DeserializeStrings(newBetsSerialized);
                    int newAnteBet = 0;
                    int newSideBet = 0;
                    if (betStrings != null && betStrings.Length == 2 && Int32.TryParse(betStrings[0], out newAnteBet) &&
                        Int32.TryParse(betStrings[1], out newSideBet))
                    {
                        if (newAnteBet < MinAnteBet)
                        {
                            client.Send("holdemcasino_alert", new byte[] { (byte)VMEODHoldEmCasinoAlerts.Bet_Too_Low });
                            slot.Client.Send("holdemcasino_toggle_betting", new byte[] { 1 });
                        }
                        else if (newAnteBet > MaxAnteBet)
                        {
                            client.Send("holdemcasino_alert", new byte[] { (byte)VMEODHoldEmCasinoAlerts.Bet_Too_High });
                            slot.Client.Send("holdemcasino_toggle_betting", new byte[] { 1 });
                        }
                        else if (newAnteBet + newSideBet > slot.SimoleonBalance)
                        {
                            client.Send("holdemcasino_alert", new byte[] { (byte)VMEODHoldEmCasinoAlerts.Bet_NSF });
                            slot.Client.Send("holdemcasino_toggle_betting", new byte[] { 1 });
                        }
                        else
                        {
                            // make sure side bet isn't too high
                            if (newSideBet > MaxSideBet)
                            {
                                client.Send("holdemcasino_alert", new byte[] { (byte)VMEODHoldEmCasinoAlerts.Side_Bet_Too_High });
                                slot.Client.Send("holdemcasino_toggle_betting", new byte[] { 1 });
                            }
                            else
                            {
                                slot.BetSubmitted = true;
                                slot.PropsedAnteBetAmount = newAnteBet;
                                slot.PropsedSideBetAmount = newSideBet;
                                // enqueue the sequence to debit the player
                                slot.PendingTransaction = true;
                                EnqueueSequence(client, VMEODHoldEmCasinoEvents.Player_Place_Bet, newAnteBet + newSideBet);
                            }
                        }
                    }
                }
            }
        }
        // this simantics event occurs after any animation sequence during gamestate = entre'act
        private void AnimationCompleteHandler(short evt, VMEODClient controller)
        {
            if (OneOrMoreBetsAccepted() || (!AllPendingTransactionFinal() && AllPlayersHaveSubmitted()))
            {
                // no one has done anything yet, have side bets been paid out yet?
                if (ActivePlayerIndex == -1)
                {
                    if (GameState.Equals(VMEODHoldEmCasinoStates.Side_Bet_Payouts) || GameState.Equals(VMEODHoldEmCasinoStates.Hold))
                        EnqueueGotoState(VMEODHoldEmCasinoStates.Player_Decision);
                    else
                        EnqueueGotoState(VMEODHoldEmCasinoStates.Side_Bet_Payouts);
                }
                else if (ActivePlayerIndex < 3)
                    EnqueueGotoState(VMEODHoldEmCasinoStates.Player_Decision);
                else
                    EnqueueGotoState(VMEODHoldEmCasinoStates.Dealer_Decision);
            }
            else
                EnqueueGotoState(VMEODHoldEmCasinoStates.Waiting_For_Player);
        }
        /*
         * This handles the queue of dealer events. They're either events to declare a winner then give chips, or to collect chips from a loser.
         * Each event has data sent with it that contains the objectID of the player who either won or lost.
         */
        private void DealerCallbackHandler(short evt, VMEODClient controller)
        {
            if (!GameState.Equals(VMEODHoldEmCasinoStates.Waiting_For_Player))
            {
                if (!DealerIntermissionComplete)
                {
                    // if there are no more events
                    if (DealerEventsQueue.Count == 0)
                    {
                        DealerIntermissionComplete = true;
                        // just finished the animations to pay out side bets
                        if (GameState.Equals(VMEODHoldEmCasinoStates.Side_Bet_Payouts))
                            EnqueueGotoState(VMEODHoldEmCasinoStates.Player_Decision);
                        // otherwise gamestate is finale and just finished animations to pay out ante bets
                        else
                            Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Dealer_Collect_Cards)); // still sends dealer_callback
                    }
                    // otherwise send next event
                    else
                    {
                        VMEODEvent nextEvent = DealerEventsQueue[0];
                        DealerEventsQueue.RemoveAt(0);
                        Controller.SendOBJEvent(nextEvent);
                    }
                }
                else // final dealer_callback, game state is finale
                {
                    // if the table doesn't have enough money for minimum, it needs to close
                    if (!IsTableWithinLimits())
                    {
                        Lobby.Broadcast("holdemcasino_alert", new byte[] { (byte)VMEODHoldEmCasinoAlerts.Table_NSF });
                        EnqueueGotoState(VMEODHoldEmCasinoStates.Closed);
                    }
                    // if there are still players, go to next game
                    else if (!Lobby.IsEmpty())
                        EnqueueGotoState(VMEODHoldEmCasinoStates.Betting_Round);
                    else
                        EnqueueGotoState(VMEODHoldEmCasinoStates.Waiting_For_Player);
                }
            }
        }
        #endregion
        #region Private
        /*
         * Ideally this never happens, but in the event of an unrecoverable error with bets or hand evaluations, rather than having the table
         * be stuck forever, return all bets to players, invoked before table closes.
         */
        private void RefundAllPlayers()
        {
            int cumulativeRefund = 0;
            var players = new List<VMEODClient>(Lobby.Players);
            foreach (var player in players)
            {
                var slot = Lobby.GetSlotData(player);
                if (slot != null && slot.BetAccepted)
                {
                    cumulativeRefund += slot.AnteBetAmount;
                    cumulativeRefund += slot.SideBetAmount;
                    if (cumulativeRefund > 0)
                        EnqueuePayout(cumulativeRefund, player);
                }  
            }
        }
        /*
         * Returns a value of 1 in the index of the player if that player has an accepted bet and is therefore playing this hand.
         */
        private byte[] GetAllActivePlayers()
        {
            byte[] players = new byte[4];
            for (int index = 0; index < 4; index++)
            {
                if (Lobby.Players[index] != null)
                {
                    var slot = Lobby.GetSlotData(index);
                    if (slot.BetAccepted)
                    {
                        players[index] = 1;
                    }
                }
            }
            return players;
        }
        private bool AllHandsEvaluated()
        {
            bool result = DealerPlayer.HandEvaluated;
            if (result)
            {
                for (int index = 0; index < 4; index++)
                {
                    if (Lobby.Players[index] != null)
                    {
                        var slot = Lobby.GetSlotData(index);
                        if (slot.BetAccepted && !slot.HasFolded)
                        {
                            if (!slot.HandEvaluated)
                                return false;
                        }
                    }
                }
            }
            return result;
        }
        private bool AllSideBetsEvaluated()
        {
            for (int index = 0; index < 4; index++)
            {
                if (Lobby.Players[index] != null)
                {
                    var slot = Lobby.GetSlotData(index);
                    if (slot.BetAccepted)
                    {
                        if (!slot.SideBetEvaluated)
                            return false;
                    }
                }
            }
            return true;
        }
        // active player didn't make decision within time limit, they are now forced to fold this hand.
        private void ForceFold(bool playerLeft)
        {
            EnqueueGotoState(VMEODHoldEmCasinoStates.Entre_Act);
            if (!playerLeft)
            {
                var player = Lobby.Players[ActivePlayerIndex];
                if (player != null)
                {
                    var slot = Lobby.GetSlotData(player);
                    slot.HasFolded = true;
                    // failsafe disallow betting event
                    slot.Client.Send("holdemcasino_decision_callback", new byte[] { 0 });

                    // broadcast this player's action
                    Lobby.Broadcast("holdemcasino_fold_broadcast", new byte[] { (byte)(slot.PlayerIndex + 1) });
                    Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Player_Fold_Sequence, slot.Client.Avatar.ObjectID));
                }
            }
            else
            {
                GotoState(VMEODHoldEmCasinoStates.Entre_Act);
                AnimationCompleteHandler((short)VMEODHoldEmCasinoEvents.Animation_Sequence_Complete, Controller);
            }
        }
        /*
         * Deal two more community cards
         * @param: playersPlaying: each index contains the objectID of the avatars that called, 0 for folded or vacant
         */
        private void DealFinalCards(short[] playersPlaying)
        {
            // deal the two new community cards and send to broadcast them
            CommunityHand.DealCards(CardDeck.Draw(), CardDeck.Draw());
            var cards = CommunityHand.GetCurrentCards();
            if (cards != null && cards.Count == 5)
            {
                Lobby.Broadcast("holdemcasino_final_deal_sequence", VMEODGameCompDrawACardData.SerializeStrings(new string[] { cards[3], cards[4] }));
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Deal_Additional_Community, playersPlaying));
                EnqueueGotoState(VMEODHoldEmCasinoStates.Entre_Act);

                // start to evaluate all players' hands as this process isn't thread safe
                for (int index = 0; index < 4; index++)
                {
                    if (playersPlaying[index] != 0)
                    {
                        var slot = Lobby.GetSlotData(index);
                        slot.EvaluateHand(CommunityHand.GetCurrentCards());
                    }
                }
                DealerPlayer.EvaluateHand(CommunityHand.GetCurrentCards());
            }
        }
        #endregion
        #region Copied but Modified from VMEODBlackjackPlugin.cs

        private void EnqueueGotoState(VMEODHoldEmCasinoStates nextState)
        {
            NextState = nextState;
        }

        private void GotoState(VMEODHoldEmCasinoStates newState)
        {
            if (GameState.Equals(newState))
                return;

            Tock = 0;

            switch (newState)
            {
                case VMEODHoldEmCasinoStates.Closed:
                    {
                        if (!Lobby.IsEmpty())
                        {
                            var players = new List<VMEODClient>(Lobby.Players);
                            foreach (var player in players)
                            {
                                if (player != null && player.Avatar != null)
                                    Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Force_End_Playing, player.Avatar.ObjectID));
                            }
                        }
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Failsafe_Remove_Dealer));
                        GameState = newState;
                        break;
                    }
                case VMEODHoldEmCasinoStates.Betting_Round:
                    {
                        // did the object break?
                        var broken = ((VMTSOEntityState)Controller.Invoker.PlatformState as VMTSOObjectState).Broken;
                        if (broken)
                        {
                            Lobby.Broadcast("holdemcasino_alert", new byte[] { (byte)VMEODHoldEmCasinoAlerts.Object_Broken });
                            EnqueueGotoState(VMEODHoldEmCasinoStates.Closed);
                        }
                        else
                        {
                            NewGame();
                            GameState = newState;
                        }
                        break;
                    }
                case VMEODHoldEmCasinoStates.Player_Decision:
                    {
                        DecisionTimer = 15;
                        GameState = newState;

                        while (ActivePlayerIndex < 3) // if the activeplayer index is 3, the final player was the last to act, so skip to dealer
                        {
                            ActivePlayerIndex++;

                            // is there another player?
                            var nextClient = Lobby.Players[ActivePlayerIndex];
                            if (nextClient != null)
                            {
                                var nextPlayer = Lobby.GetSlotData(nextClient);
                                // found another player!
                                if (nextPlayer.BetAccepted)
                                {
                                    SendEnableInput(nextPlayer);
                                    Lobby.Broadcast("holdemcasino_set_active_player", BitConverter.GetBytes(nextPlayer.PlayerIndex + 1));
                                    return;
                                }
                            }
                        }
                        // there are no more potential players this round so draw 2 cards for the community
                        EnqueueGotoState(VMEODHoldEmCasinoStates.Dealer_Decision);
                        break;
                    }
                case VMEODHoldEmCasinoStates.Dealer_Decision: // if any players called, draw two more community cards
                    {
                        GameState = newState;
                        // only 3 cards have been dealt, so see if anyone called before dealing 2 more
                        if (CommunityHand.GetCurrentCards().Count == 3)
                        {
                            short[] playersPlaying = new short[4];
                            bool someoneCalled = false;
                            for (int index = 0; index < 4; index++)
                            {
                                if (Lobby.Players[index] != null)
                                {
                                    var slot = Lobby.GetSlotData(index);
                                    if (slot.BetAccepted && slot.Client != null && !slot.HasFolded)
                                    {
                                        playersPlaying[index] = slot.Client.Avatar.ObjectID;
                                        someoneCalled = true;
                                    }
                                }
                            }
                            if (someoneCalled)
                                DealFinalCards(playersPlaying);
                            else
                            {
                                DealerPlayer.HandEvaluated = true;
                                EnqueueGotoState(VMEODHoldEmCasinoStates.Finale);
                            }
                        }
                        else // 5 cards have already been dealt
                            EnqueueGotoState(VMEODHoldEmCasinoStates.Finale);
                        break;
                    }
                case VMEODHoldEmCasinoStates.Side_Bet_Payouts: // before allowing player decision, pay out any side bet winners
                    {
                        GameState = newState;
                        short winnings = 0;
                        if (AllSideBetsEvaluated())
                        {
                            for (int index = 0; index < 4; index++)
                            {
                                var playerClient = Lobby.Players[index];
                                if (playerClient != null)
                                {
                                    var slot = Lobby.GetSlotData(index);
                                    var playerObjectID = playerClient.Avatar.ObjectID;
                                    if (slot.BetAccepted)
                                        winnings = slot.CalculateSideBetPayout();

                                    if (winnings > 0)
                                    {
                                        // get the hand type, add winnings before
                                        var dataStringList = slot.GetSideHandType();
                                        dataStringList.Insert(0, winnings + "");

                                        // pay them
                                        EnqueuePayout(winnings, playerClient);

                                        // queue event for dealer to give them chips
                                        DealerEventsQueue.Add(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Dealer_Declare_winner, new short[] { playerObjectID }));

                                        // event for player wins
                                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Player_Win_Side_Bet,
                                            new short[] { playerObjectID, winnings }));

                                        playerClient.Send("holdemcasino_sidebet_win_message",
                                            VMEODGameCompDrawACardData.SerializeStrings(dataStringList.ToArray()));
                                    }
                                    else
                                    {
                                        if (slot.BetAccepted)
                                            playerClient.Send("holdemcasino_sidebet_win_message", new byte[0]);
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
                            // if not annouce that no one won side bets
                            else
                                EnqueueGotoState(VMEODHoldEmCasinoStates.Hold);
                        }
                        else
                            EnqueueGotoState(VMEODHoldEmCasinoStates.Awaiting_Evaluation);
                        break;
                    }
                case VMEODHoldEmCasinoStates.Finale: // pay ante and call bets to winners, collect chips from losers, player animations
                    {
                        GameState = newState;

                        // broadcast events to reveal the dealer's hand, if someone called
                        if (CommunityHand.GetCurrentCards().Count == 5) // everyone folded if there are only 3 community cards here
                        {
                            List<string> allCardsInPlay = GetAllActiveCardsInPlay(true);
                            if (allCardsInPlay != null)
                                Lobby.Broadcast("holdemcasino_sync_hands_up", VMEODGameCompDrawACardData.SerializeStrings(allCardsInPlay.ToArray()));

                        }
                        if (AllHandsEvaluated())
                        {
                            // find the winners and pay them, send winning/losing animations
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
                                        if (!slot.HasFolded)
                                        {
                                            short winnings = slot.CalculateFinalPayout(DealerPlayer);

                                            // get the hand type, add winnings before
                                            List<string> dataStringList = null;

                                            if (winnings > 0)
                                            {
                                                // get winner's hand type
                                                dataStringList = slot.GetHandType();

                                                // did the player push the dealer?
                                                if (slot.Pushed)
                                                {
                                                    // add winnings before winner's hand type with a 'p' for push
                                                    dataStringList.Insert(0, "p" + winnings);
                                                }
                                                // did the dealer qualify?
                                                else if (!DealerPlayer.HandQualified)
                                                {
                                                    // add winnings before winner's hand type with a 'q' for dealer didn't qualify
                                                    dataStringList.Insert(0, "q" + winnings);
                                                }
                                                // otherwise standard win
                                                else
                                                {
                                                    // add winnings before winner's hand type
                                                    dataStringList.Insert(0, "" + winnings);
                                                }

                                                // pay them
                                                EnqueuePayout(winnings, playerClient);

                                                // queue event for dealer to give them chips
                                                DealerEventsQueue.Add(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Dealer_Declare_winner, new short[] { playerObjectID }));

                                                // event for player wins
                                                Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Player_Win_Animation,
                                                    new short[] { playerObjectID, winnings }));
                                            }
                                            else // player lost to dealer
                                            {
                                                // get dealer's hand type, add $0 before
                                                dataStringList = DealerPlayer.GetHandType();
                                                dataStringList.Insert(0, "0");

                                                Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Player_Lose_Animation,
                                                    new short[] { playerObjectID }));

                                                // queue event for dealer to collect chips
                                                DealerEventsQueue.Add(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Dealer_Collect_Chips, new short[] { playerObjectID }));
                                            }
                                            playerClient.Send("holdemcasino_win_loss_message",
                                                VMEODGameCompDrawACardData.SerializeStrings(dataStringList.ToArray()));
                                        }
                                        else // you folded
                                        {
                                            // queue event for dealer to collect chips
                                            DealerEventsQueue.Add(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Dealer_Collect_Chips, new short[] { playerObjectID }));
                                            // resync their cards
                                            var myCards = slot.GetCurrentCards();
                                            playerClient.Send("holdemcasino_sync_hand",
                                                VMEODGameCompDrawACardData.SerializeStrings(new string[] { slot.PlayerIndex + 1 + "", myCards[0], myCards[1] }));
                                            // tell them they folded, winning nothing
                                            playerClient.Send("holdemcasino_win_loss_message", VMEODGameCompDrawACardData.SerializeStrings(new string[0]));
                                        }
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
                            // if not, see if there are still players. goto denoument if so, waiting if not
                            else
                            {
                                if (!Lobby.IsEmpty()) 
                                    EnqueueGotoState(VMEODHoldEmCasinoStates.Betting_Round);
                                else
                                    EnqueueGotoState(VMEODHoldEmCasinoStates.Waiting_For_Player);
                            }
                        }
                        else
                            EnqueueGotoState(VMEODHoldEmCasinoStates.Awaiting_Evaluation);
                        break;
                    }
                case VMEODHoldEmCasinoStates.Waiting_For_Player:
                    {
                        ActivePlayerIndex = -1;
                        CardDeck.Shuffle(2);
                        GameState = newState;
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Dealer_Collect_Cards));
                        break;
                    }
                case VMEODHoldEmCasinoStates.Awaiting_Evaluation:
                    {
                        Lobby.Broadcast("holdemcasino_timer", BitConverter.GetBytes(0));
                        IdleTimer = 2;
                        Tock = 0;
                        GameState = newState;
                        break;
                    }
                case VMEODHoldEmCasinoStates.Pending_Initial_Transactions:
                    {
                        Lobby.Broadcast("holdemcasino_timer", BitConverter.GetBytes(0));
                        IdleTimer = 2;
                        Tock = 0;
                        GameState = newState;
                        break;
                    }
                case VMEODHoldEmCasinoStates.Hold:
                    {
                        IdleTimer = 3;
                        Tock = 0;
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
        /*
         * [WORST CASE SCENARIO ANTE PAYOUT]
         * Ante Bet (Pay Table 2) => Community cards would be { Ten, Jack, Queen } suited and { 7 or below any suit, same number any suit }
         * Example community cards { 10c, Jc, Qc, 7s, 7h }, Dealer Qualifies (pair of fours or better) but loses against all players
         ***********************************************************************************************************************************
         * Player# - [Type of Hand] - Payout ratio - (desc. of hand) e.g. example
         ***********************************************************************************************************************************
         * Player1 - [Royal Flush] - 25:1 - (has King and Ace of same suit as three community) e.g. { Kc, Ac }
         * Player2 - [Straight Flush] - 25:1 - (same suit as community) e.g. { 8c, 9c } plus community { 10c, Jc, Qc }
         * Player3 - [4 of a Kind] (7 or below) - 12:1 - (two remaining community cards make 4 of a kind with pocket pair) e.g. { 7d, 7c }
         * Player4 - [Flush] (same suit as royal) - 2:1 - (hand has same suit as royal flush) e.g. { 2c, 3c }
         ***********************************************************************************************************************************
         * Ante Pot Total:
         * 64:1 + 5x Ante bet per player returned (because calling is 2x Ante, so 4 + 1 orig ante) = 84 times Max Ante ($1000 => $84000)
         ***********************************************************************************************************************************
         * [WORST CASE SCENARIO SIDE PAYOUT]
         * Side Bet (Table 1) => Community cards would be { Ten, Jack, Queen }
         * Example community cards { 10c, Jc, Qc }
         ***********************************************************************************************************************************
         * Player# - [Type of Hand] - Payout ratio - (desc. of hand) e.g. example
         ***********************************************************************************************************************************
         * Player1 - [Royal Flush] - 25:1 - (has King and Ace of same suit as three community) e.g. { Kc, Ac }
         * Player2 - [Straight Flush] - 25:1 - (same suit as community) e.g. { 8c, 9c } plus community { 10c, Jc, Qc }
         * Player3 - [Flush] - 25:1 - (Same suit as community) e.g. { 6c, 7c }
         * Player4 - [Flush] - 25:1 - (Same suit as community) e.g. { 4c, 5c }
         * Side Total: 100:1 + 1x Side bet returned per player = 104 times Max Side Bet ($1000 => $104000)
         ***********************************************************************************************************************************
         * Reference: https://wizardofodds.com/games/casino-hold-em/ (Ante Pay Table 2, AA+ Pay Table 1)
         ***********************************************************************************************************************************
         * [SHORT DATA TYPE CAP]
         * The most any single player can win at one time is 30x their Max Ante Bet (25x for Royal Flush, 5 for winning call bet and returning
         * original ante bet). 32767 / 30 = 1092.23333 so max bets will be set at $1000.
         ***********************************************************************************************************************************
         * Note: The reference to the table max balance constant in VMEODBlackjackPlugin was left here on purpose.
         */
        private bool IsTableWithinLimits()
        {
            if (TableBalance >= (MaxAnteBet * WORST_CASE_ANTE_PAYOUT_RATIO + MaxSideBet * WORST_CASE_SIDE_PAYOUT_RATIO)
                && TableBalance <= VMEODBlackjackPlugin.TABLE_MAX_BALANCE)
                return true;
            return false;
        }
        /*
         * During betting phase, when a player changes their ante bet all other players should be notified in their client.
         */
        private void BroadcastSingleAnteBet(HoldEmCasinoPlayer playerWithNewBet)
        {
            if (GameState.Equals(VMEODHoldEmCasinoStates.Betting_Round))
            {
                if (playerWithNewBet != null && playerWithNewBet.Client != null && playerWithNewBet.AnteBetAmount > -1)
                {
                    Lobby.Broadcast("holdemcasino_antebet_update_player" + (playerWithNewBet.PlayerIndex + 1),
                        VMEODGameCompDrawACardData.SerializeStrings(playerWithNewBet.AnteBetAmount + ""));
                }
            }
        }
        /*
         * During betting phase, when a player changes their side bet all other players should be notified in their client.
         */
        private void BroadcastSingleSideBet(HoldEmCasinoPlayer playerWithNewBet)
        {
            if (GameState.Equals(VMEODHoldEmCasinoStates.Betting_Round))
            {
                if (playerWithNewBet != null && playerWithNewBet.Client != null && playerWithNewBet.SideBetAmount > -1)
                {
                    Lobby.Broadcast("holdemcasino_sidebet_update_player" + (playerWithNewBet.PlayerIndex + 1),
                         VMEODGameCompDrawACardData.SerializeStrings(playerWithNewBet.SideBetAmount + ""));
                }
            }
        }
        /*
         * Deals two ("back" face down) cards to each player and dealer, then 3 face-up cards to the community pile
         */
        private void DealIntitialCards()
        {
            List<string> allCardsInPlay = new List<string>();
            short[] playersPlaying = new short[4];
            // first actually deal the cards server side. find any valid player with an accepted bet
            for (int index = 0; index < 4; index++)
            {
                if (Lobby.Players[index] != null)
                {
                    var slot = Lobby.GetSlotData(index);
                    if (slot.BetAccepted)
                    {
                        slot.DealCards(CardDeck.Draw(), CardDeck.Draw()); // deal two cards to each player
                        playersPlaying[index] = slot.Client.Avatar.ObjectID;
                        // add face up cards to be dealt to the player in the UI
                        var cards = slot.GetCurrentCards();
                        allCardsInPlay.Add(cards[0]);
                        allCardsInPlay.Add(cards[1]);
                    }
                    else // add two blank strings since the player isn't playing
                    { 
                        allCardsInPlay.Add("");
                        allCardsInPlay.Add("");
                    }
                }
                else // add two blank strings since the player isn't present
                {
                    allCardsInPlay.Add("");
                    allCardsInPlay.Add("");
                }
            }
            // deal two cards to the dealer as well
            DealerPlayer.DealCards(CardDeck.Draw(), CardDeck.Draw());
            allCardsInPlay.Add("Back");
            allCardsInPlay.Add("Back");

            // deal three cards to the community deck
            CommunityHand.DealCards(CardDeck.Draw(), CardDeck.Draw(), CardDeck.Draw());
            var communityCards = CommunityHand.GetCurrentCards();
            allCardsInPlay.Add(communityCards[0]);
            allCardsInPlay.Add(communityCards[1]);
            allCardsInPlay.Add(communityCards[2]);

            // players NOT playing will have their corresponding shorts be 0, but players who need cards will have their avatar.objectIDs sent to plugin
            Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Deal_Initial_Cards_Sequence, playersPlaying));

            // sync the bets again, in case one was missed due to desync
            List<string> acceptedBets = GetAllAcceptedBets();
            if (acceptedBets != null)
                Lobby.Broadcast("holdemcasino_sync_accepted_bets", VMEODGameCompDrawACardData.SerializeStrings(acceptedBets.ToArray()));
            
            // goto entre'act, send card dealing sequence, send all cards to all clients
            EnqueueGotoState(VMEODHoldEmCasinoStates.Entre_Act);

            for (int index = 0; index < 4; index++)
            {
                if (Lobby.Players[index] != null)
                {
                    var slot = Lobby.GetSlotData(index);
                    var alteredCards = new List<string>(allCardsInPlay);
                    // send the dealing events
                    slot.Client.Send("holdemcasino_deal_sequence", VMEODGameCompDrawACardData.SerializeStrings(alteredCards.ToArray()));

                    // start the evaluation of each players' hand for the side bet, as this process isn't thread safe
                    if (slot.BetAccepted)
                        slot.EvaluateSideBet(communityCards);
                }
            }
        }
        /*
         * This only occurs when a new player joins mid-game, so during any gamestate that is not waiting for player or betting round
         */
        private void SyncAllPlayers(VMEODClient client)
        {
            // sync all ante and side bets
            List<string> acceptedBets = GetAllAcceptedBets();
            if (acceptedBets != null)
                client.Send("holdemcasino_sync_accepted_bets", VMEODGameCompDrawACardData.SerializeStrings(acceptedBets.ToArray()));

            // player cards need to be sync'd
            List<string> allCardsInPlay = GetAllActiveCardsInPlay(false);
            if (allCardsInPlay != null)
                client.Send("holdemcasino_sync_hands_up", VMEODGameCompDrawACardData.SerializeStrings(allCardsInPlay.ToArray()));

            // sync the community cards: there will be 3 or 5
            List<string> communityCards = CommunityHand.GetCurrentCards();
            if (communityCards != null)
                client.Send("holdemcasino_sync_community", VMEODGameCompDrawACardData.SerializeStrings(communityCards.ToArray()));
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
                    allAcceptedBets.Add(slot.AnteBetAmount + ""); // will be 0 if bet isn't accepted
                    allAcceptedBets.Add(slot.SideBetAmount + ""); // will be 0 if bet isn't accepted
                }
                else // failsafe 
                {
                    allAcceptedBets.Add("0");
                    allAcceptedBets.Add("0");
                }
            }
            return allAcceptedBets;
        }
        /*
         * This method returns all cards in each player's hand. It does not return community cards.
         */
        private List<string> GetAllActiveCardsInPlay(bool showDealerCards)
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
                        if (slot.HasFolded) // they folded
                        {
                            allCardsInPlay.Add("Back");
                            allCardsInPlay.Add("Back");
                        }
                        else
                        {
                            var myCards = slot.GetCurrentCards();
                            allCardsInPlay.AddRange(myCards);
                        }
                    }
                    else // not playing
                    {
                        allCardsInPlay.Add("");
                        allCardsInPlay.Add("");
                    }
                }
                else // no player here
                {
                    allCardsInPlay.Add("");
                    allCardsInPlay.Add("");
                }
            }
            // add the dealer's cards
            List<string> dealersCards = new List<String>();
            if (showDealerCards)
                dealersCards = DealerPlayer.GetCurrentCards();
            else
                dealersCards = new List<String>() { "Back", "Back" };
            allCardsInPlay.AddRange(dealersCards);

            return allCardsInPlay;
        }
        /*
         * Note: Will not send if the gamestate is not player decision, such as if the player ran out of time to call or fold
         */
        private void SendEnableInput(HoldEmCasinoPlayer player)
        {
            if (player.Client != null && GameState.Equals(VMEODHoldEmCasinoStates.Player_Decision))
            {
                player.Client.Send("holdemcasino_allow_input", new byte[0]);
            }
        }
        #endregion
        #region Localized from VMEODBlackjackPlugin.cs

        /*
         * Reset all players' hands, including dealer's as well as players' bet data
         */
        private void NewGame()
        {
            for (int index = 0; index < 4; index++)
            {
                var player = Lobby.Players[index];
                var slot = Lobby.GetSlotData(index);
                slot.ResetHand();
                if (player != null)
                {
                    if (slot.SimoleonBalance < MinAnteBet)
                    {
                        // come back when you have more money
                        if (slot.Client != null && slot.Client.Avatar != null)
                        {
                            slot.Client.Send("holdemcasino_alert", new byte[] { (byte)VMEODHoldEmCasinoAlerts.Player_NSF });
                            Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Force_End_Playing, slot.Client.Avatar.ObjectID));
                        }
                    }
                }
            }
            // reset community cards and dealer's hand
            DealerPlayer.ResetHand();
            CommunityHand.ResetHand();
            ActivePlayerIndex = -1;
            BettingTimer = 30;
            CardDeck.Shuffle(2);

            Lobby.Broadcast("holdemcasino_new_game", new byte[0]);
        }
        /*
         * One or more players did not place a bet before the timer expired.
         */
        private void ForceEndBettingRound()
        {
            // immediately disallow server-side and client-side betting
            EnqueueGotoState(VMEODHoldEmCasinoStates.Pending_Initial_Transactions);
            Lobby.Broadcast("holdemcasino_toggle_betting", new byte[] { 0 });

            for (int index = 0; index < 4; index++)
            {
                var player = Lobby.Players[index];
                if (player != null)
                {
                    var slot = Lobby.GetSlotData(player);
                    if (!slot.BetSubmitted) // they didn't submit a bet
                    {
                        if (slot.WarnedForObservation)
                        {
                            // they have to leave, they were already warned last round
                            slot.Client.Send("holdemcasino_alert", new byte[] { (byte)VMEODHoldEmCasinoAlerts.Observe_Twice });
                            if (player.Avatar != null)
                                Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Force_End_Playing, player.Avatar.ObjectID));
                            if (Lobby.IsEmpty())
                            {
                                EnqueueGotoState(VMEODHoldEmCasinoStates.Waiting_For_Player);
                                return;
                            }
                        }
                        else
                        {
                            // they did not bet, so they get a warning about being able to observe for this round only
                            slot.WarnedForObservation = true;
                            slot.Client.Send("holdemcasino_alert", new byte[] { (byte)VMEODHoldEmCasinoAlerts.Observe_Once });
                        }
                    }
                }
            }
        }
        /*
         * Catch-all method for handling the required transaction of certain actions: initial betting (ante & side) and calling (2x the anti bet)
         */
        private void EnqueueSequence(VMEODClient client, VMEODHoldEmCasinoEvents sequenceType, int amount)
        {
            if (client == null)
                return;

            var slot = Lobby.GetSlotData(client);

            if (slot.Client != null && slot.Client.Avatar.ObjectID == client.Avatar.ObjectID)
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
                                case VMEODHoldEmCasinoEvents.Player_Call_Sequence:
                                    {
                                        // execute simantics event to play the call sequence, goto to entre'act to wait
                                        slot.CallBetAmount = amount;
                                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Player_Call_Sequence,
                                            new short[] { slot.Client.Avatar.ObjectID, (short)amount }));
                                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Money_Over_Head,
                                            new short[] { slot.Client.Avatar.ObjectID, (short)amount }));
                                        EnqueueGotoState(VMEODHoldEmCasinoStates.Entre_Act);
                                        break;
                                    }
                                default: // bet sequence
                                    {
                                        // failsafe disallow betting event
                                        slot.Client.Send("holdemcasino_toggle_betting", new byte[] { 0 });
                                        slot.PendingTransaction = false;
                                        slot.BetSubmitted = true;
                                        slot.BetAccepted = true;
                                        slot.AnteBetAmount = slot.PropsedAnteBetAmount;
                                        slot.PropsedAnteBetAmount = 0;
                                        slot.SideBetAmount = slot.PropsedSideBetAmount;
                                        slot.PropsedSideBetAmount = 0;
                                        slot.WarnedForObservation = false;
                                        // failsafe to send their accept bet amount
                                        slot.Client.Send("holdemcasino_bet_callback",
                                            VMEODGameCompDrawACardData.SerializeStrings(new string[2] { slot.AnteBetAmount + "", slot.SideBetAmount + "" }));

                                        // animate player
                                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODHoldEmCasinoEvents.Player_Place_Bet,
                                            new short[] { slot.Client.Avatar.ObjectID, (short)slot.CumulativeBetAmount }));

                                        // was this the last player who needed to submit their bet?
                                        if (AllPlayersHaveSubmitted())
                                            EnqueueGotoState(VMEODHoldEmCasinoStates.Pending_Initial_Transactions);
                                        break;
                                    }
                            }
                        }
                    }
                    else // the transaction failed
                    {
                        if (!sequenceType.Equals(VMEODHoldEmCasinoEvents.Player_Call_Sequence))
                        {
                            // call transaction failed, tell them insufficient funds and allow input again
                            slot.Client.Send("holdemcasino_alert", new byte[] { (byte)VMEODHoldEmCasinoAlerts.Call_NSF });
                            slot.CallBetAmount = 0;
                            SendEnableInput(slot);
                        }
                        else
                        {
                            // initial bet transaction failed
                            slot.Client.Send("holdemcasino_alert", new byte[] { (byte)VMEODHoldEmCasinoAlerts.Bet_NSF });
                            slot.PendingTransaction = false;
                            slot.BetSubmitted = false;
                            slot.BetAccepted = false;
                            slot.AnteBetAmount = 0;
                            slot.PropsedAnteBetAmount = 0;
                            slot.SideBetAmount = 0;
                            slot.PropsedSideBetAmount = 0;
                            // if we're still in the betting round, they can bet so allow betting input
                            if (GameState.Equals(VMEODHoldEmCasinoStates.Betting_Round))
                                slot.Client.Send("holdemcasino_toggle_betting", new byte[] { 1 });
                        }
                    }
                });
            }
            // client not found
            else
                slot.ResetHand();
        }

        #endregion
        #region Copied from VMEODBlackjackPlugin.cs

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
        #endregion
    }
    public class HoldEmCasinoPlayer
    {
        private bool _BetAccepted;
        private bool _BetSubmitted;
        private bool _HandIsEvaluated;
        private bool _HandQualified;
        private bool _HasFolded;
        private bool _PendingTransaction;
        private bool _Pushed;
        private bool _SideBetEvaluated;
        private bool _WarnedForObservation;

        private VMEODClient _Client;
        private List<PlayingCard> Hand;

        private Hand MyFiveCardSideBetHand;
        private Hand MySevenCardHoldemHand;

        private int _AnteBetAmount;
        private int _CallBetAmount;
        private int _PlayerIndex;
        private int _PropsedAnteBetAmount;
        private int _PropsedSideBetAmount;
        private int _SideBetAmount;
        private int _SimoleonBalance;

        internal delegate void BetChange(HoldEmCasinoPlayer player);

        public HoldEmCasinoPlayer()
        {

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
        public Hand DealerHoldemHand
        {
            get { return MySevenCardHoldemHand; }
        }
        public bool HandEvaluated
        {
            get
            {
                if (ReferenceEquals(MySevenCardHoldemHand, null))
                    return true;
                else
                    return (MySevenCardHoldemHand.HandValue != 0 | _HandIsEvaluated);
            }
            set { _HandIsEvaluated = value; }
        }
        public bool HandQualified
        {
            get { return _HandQualified; }
        }
        public bool HasFolded
        {
            get { return _HasFolded; }
            set { _HasFolded = value; }
        }
        public bool PendingTransaction
        {
            get { return _PendingTransaction; }
            set { _PendingTransaction = value; }
        }
        public bool Pushed
        {
            get { return _Pushed; }
        }
        public bool SideBetEvaluated
        {
            get
            {
                return (MyFiveCardSideBetHand.HandValue != 0);
            }
        }
        public bool WarnedForObservation
        {
            get { return _WarnedForObservation; }
            set { _WarnedForObservation = value; }
        }
        public int AnteBetAmount
        {
            get { return _AnteBetAmount; }
            set
            {
                _AnteBetAmount = value;
                if (_AnteBetAmount != 0)
                    OnPlayerAnteBetChange.Invoke(this);
            }
        }
        public int CallBetAmount
        {
            get { return _CallBetAmount; }
            set { _CallBetAmount = value; }
        }
        public int CumulativeBetAmount
        {
            get { return _AnteBetAmount + _SideBetAmount; }
        }
        public int PlayerIndex
        {
            get { return _PlayerIndex; }
            set { _PlayerIndex = value; }
        }
        public int SideBetAmount
        {
            get { return _SideBetAmount; }
            set
            {
                _SideBetAmount = value;
                if (_SideBetAmount != 0)
                    OnPlayerSideBetChange.Invoke(this);
            }
        }
        public int PropsedAnteBetAmount
        {
            get { return _PropsedAnteBetAmount; }
            set { _PropsedAnteBetAmount = value; }
        }
        public int PropsedSideBetAmount
        {
            get { return _PropsedSideBetAmount; }
            set { _PropsedSideBetAmount = value; }
        }
        public int SimoleonBalance
        {
            get { return _SimoleonBalance; }
            set { _SimoleonBalance = value; }
        }

        internal event BetChange OnPlayerAnteBetChange;
        internal event BetChange OnPlayerSideBetChange;

        public void ResetHand()
        {
            Hand = new List<PlayingCard>();
            MyFiveCardSideBetHand = null;
            MySevenCardHoldemHand = null;
            _AnteBetAmount = 0;
            _CallBetAmount = 0;
            _SideBetAmount = 0;
            _PropsedAnteBetAmount = 0;
            _PropsedSideBetAmount = 0;
            _BetAccepted = false;
            _BetSubmitted = false;
            _PendingTransaction = false;
            _HandIsEvaluated = false;
            _HandQualified = false;
            _HasFolded = false;
            _Pushed = false;
            _SideBetEvaluated = false;
        }
        public void DealCards(params PlayingCard[] cards)
        {
            if (cards != null)
            {
                foreach (var card in cards)
                    Hand.Add(card);
            }
        }
        public List<String> GetCurrentCards()
        {
            List<String> cards = new List<string>();
            if (Hand != null)
            {
                foreach (var card in Hand)
                {
                    cards.Add(card.Value.ToString() + "_" + card.Suit.ToString()); // e.g. "Five_Clubs"
                }
            }
            return cards;
        }
        public List<string> GetHandType()
        {
            List<string> type = new List<string>();
            if (!ReferenceEquals(MySevenCardHoldemHand, null))
            {
                var handType = MySevenCardHoldemHand.HandTypeValue;
                type.Add(handType.ToString());
                type.AddRange(GetRelevantCardNames(handType, MySevenCardHoldemHand.Description));
            }
            return type;
        }
        public List<string> GetSideHandType()
        {
            List<string> type = new List<string>();
            if (!ReferenceEquals(MyFiveCardSideBetHand, null))
            {
                var handType = MyFiveCardSideBetHand.HandTypeValue;
                type.Add(handType.ToString());
                type.AddRange(GetRelevantCardNames(handType, MyFiveCardSideBetHand.Description));
            }
            return type;
        }
        private List<string> GetRelevantCardNames(Hand.HandTypes handType, string description)
        {
            List<string> relevantCardNames = new List<string>();
            switch (handType)
            {
                // High card: CardValueString
                case HoldemHand.Hand.HandTypes.HighCard:
                case HoldemHand.Hand.HandTypes.Pair:
                case HoldemHand.Hand.HandTypes.Trips:
                case HoldemHand.Hand.HandTypes.Straight:
                case HoldemHand.Hand.HandTypes.FourOfAKind:
                    {
                        char splitter = ',';
                        if (description.Contains(':')) // only HighCard
                            splitter = ':';
                        var split = description.Split(splitter); // always 2 strings, [0] is useless
                        string cardString = split[1].Replace(" high", ""); // Straight only
                        cardString = cardString.Replace("'s", ""); // Trips and FourOfAKind
                        relevantCardNames.Add(cardString.Trim()); // e.g. " Ace" or " Ten" so remove leading whitespace
                        break;
                    }
                case HoldemHand.Hand.HandTypes.TwoPair:
                case HoldemHand.Hand.HandTypes.FullHouse:
                    {
                        // split length 2 for full house, 3 for twopair; split[0] is NOT useless
                        var split = description.Split(new string[] { "'s" }, StringSplitOptions.RemoveEmptyEntries);

                        // add first card
                        if (split != null && split.Length > 0)
                        {
                            var splitCardOne = split[0].Split(','); // splitCardOne[0] is useless
                            if (splitCardOne != null && splitCardOne.Length > 1)
                                relevantCardNames.Add(splitCardOne[1].Trim());
                        }
                        // add second card
                        if (split.Length > 1)
                            relevantCardNames.Add(split[1].Replace(" and ", "").Trim());

                        /* add third card? twopair only
                        if (split.Length > 2)
                            relevantCardNames.Add(split[2].Replace(" with a ", "").Replace(" for a kicker", "").Trim());*/

                        break;
                    }
                case HoldemHand.Hand.HandTypes.Flush:
                case HoldemHand.Hand.HandTypes.StraightFlush:
                    {
                        var split = description.Split(new string[] { "with" }, StringSplitOptions.None);

                        // add the suit — "Straight Flush (Suit) " to "Suit" e.g. "Diamonds"
                        relevantCardNames.Add(split[0].Substring(split[0].IndexOf('(') + 1).Trim().Replace(")",""));

                        /* add the high card
                        relevantCardNames.Add(split[1].Replace("high", "").Trim()); // " CardStringName high" to "CardStringName" */
                        break;
                    }
            }
            return relevantCardNames;
        }
        public void EvaluateHand(List<string> board)
        {
            // community cards
            String boardString = "";
            foreach (var card in board)
                boardString += AbstractPlayingCardsDeck.CardShortHand[card] + " ";

            // my pocket/hole cards
            var myRawCards = GetCurrentCards();
            String pocketCardsString = "";
            foreach (var card in myRawCards)
                pocketCardsString += AbstractPlayingCardsDeck.CardShortHand[card] + " ";

            // evaluate the 7 card hand
            MySevenCardHoldemHand = new HoldemHand.Hand(pocketCardsString, boardString);

            // did my hand qualify? Dealer only
            var handType = MySevenCardHoldemHand.HandTypeValue;

            if (handType.Equals(HoldemHand.Hand.HandTypes.Pair))
            {
                // "One pair, " + CardStringName e.g. "Ace" or "Nine"
                var description = MySevenCardHoldemHand.Description;
                var split = description.Split(',');
                var card = split[1].Trim();
                if (!card.Equals("Two") & !card.Equals("Three"))
                    _HandQualified = true; // is a pair of "Four" or better
            }
            else if (SidePayoutRatios.ContainsKey(handType)) // is better than a Pair, e.g. NOT HighCard
                _HandQualified = true;
        }
        public void EvaluateSideBet(List<string> board)
        {
            // community cards
            String boardString = "";
            foreach (var card in board)
                boardString += AbstractPlayingCardsDeck.CardShortHand[card] + " ";

            // my pocket/hole cards
            var myRawCards = GetCurrentCards();
            String pocketCardsString = "";
            foreach (var card in myRawCards)
                pocketCardsString += AbstractPlayingCardsDeck.CardShortHand[card] + " ";
            
            // evaluate the 5 card hand, save the type
            MyFiveCardSideBetHand = new HoldemHand.Hand(pocketCardsString, boardString);
        }
        public short CalculateSideBetPayout()
        {
            // check if it's a pair, if so, it must be Aces to get a payout
            var handType = MyFiveCardSideBetHand.HandTypeValue;
            int payoutRatio = 0;
            if (handType.Equals(HoldemHand.Hand.HandTypes.Pair))
            {
                // "One pair, " + Singular Card Name e.g. "Ace" or "Nine"
                var description = MyFiveCardSideBetHand.Description;
                var split = description.Split(',');
                if (split[1].Trim().Equals("Ace"))
                    payoutRatio = SidePayoutRatios[handType];
            }
            else if (SidePayoutRatios.ContainsKey(handType))
            {
                payoutRatio = SidePayoutRatios[handType];
            }
            if (payoutRatio > 0)
                return (short)(payoutRatio * _SideBetAmount + _SideBetAmount);
            return 0;
        }
        public short CalculateFinalPayout(HoldEmCasinoPlayer dealer)
        {
            int antePayoutRatio = 0;
            int CallBetPayoutRatio = 0;
            int callBet = AnteBetAmount * 2;
            // if dealer doesn't qualify, ante bet is payout chart but call bet is returned
            if (!dealer.HandQualified)
            {
                antePayoutRatio = AntePayoutRatios[MySevenCardHoldemHand.HandTypeValue];
            }
            // if dealer qualifies and it's a push, ante and bet calls are returned
            else if (dealer.DealerHoldemHand.HandValue == MySevenCardHoldemHand.HandValue)
            {
                _Pushed = true;
            }
            // if dealer qualifies and player wins, ante bet is chart and call bet is 1:1
            else if (dealer.DealerHoldemHand.HandValue < MySevenCardHoldemHand.HandValue)
            {
                antePayoutRatio = AntePayoutRatios[MySevenCardHoldemHand.HandTypeValue];
                CallBetPayoutRatio = 1;
            }
            else // player lost
                return 0;

            return (short)(antePayoutRatio * AnteBetAmount + CallBetPayoutRatio * CallBetAmount + AnteBetAmount + CallBetAmount);
        }
        private Dictionary<Hand.HandTypes, int> AntePayoutRatios = new Dictionary<HoldemHand.Hand.HandTypes, int>
        {
            { HoldemHand.Hand.HandTypes.StraightFlush, 25 },
            { HoldemHand.Hand.HandTypes.FourOfAKind, 12 },
            { HoldemHand.Hand.HandTypes.FullHouse, 3 },
            { HoldemHand.Hand.HandTypes.Flush, 2 },
            { HoldemHand.Hand.HandTypes.Straight, 1 },
            { HoldemHand.Hand.HandTypes.Trips, 1 },
            { HoldemHand.Hand.HandTypes.TwoPair, 1 },
            { HoldemHand.Hand.HandTypes.Pair, 1 },
            { HoldemHand.Hand.HandTypes.HighCard, 1 }
        };
        private Dictionary<Hand.HandTypes, int> SidePayoutRatios = new Dictionary<HoldemHand.Hand.HandTypes, int>
        {
            { HoldemHand.Hand.HandTypes.StraightFlush, 25 },
            { HoldemHand.Hand.HandTypes.FourOfAKind, 25 },
            { HoldemHand.Hand.HandTypes.FullHouse, 25 },
            { HoldemHand.Hand.HandTypes.Flush, 25 },
            { HoldemHand.Hand.HandTypes.Straight, 7 },
            { HoldemHand.Hand.HandTypes.Trips, 7 },
            { HoldemHand.Hand.HandTypes.TwoPair, 7 },
            { HoldemHand.Hand.HandTypes.Pair, 7 } // MUST be pair of Aces
        };
    }

    public enum VMEODHoldEmCasinoStates
    {
        Managing = -2, // for owner
        Invalid = -1,
        Closed = 0,
        Waiting_For_Player = 1,
        Betting_Round = 2,
        Pending_Initial_Transactions = 3,
        Entre_Act = 4, // used for waiting for cards to be dealt at round start or for animations of call / fold
        Player_Decision = 5,
        Dealer_Decision = 6, // if any players called, draw 2 more community cards
        Side_Bet_Payouts = 7, // after initial deal, pay players with winning side bets
        Finale = 8, // final payouts of ante and call bets
        Awaiting_Evaluation = 9,
        Hold = 10
    }
    public enum VMEODHoldEmCasinoEvents : short
    {
        Deal_Initial_Cards_Sequence = 1, // upon completion, invokes: Animation_Sequence_Complete
        Dealer_Collect_Chips = 2, // declare loser - upon completion, invokes: Dealer_Callback
        Dealer_Declare_winner = 3, // upon completion, invokes: Dealer_Callback
        Player_Place_Bet = 4, // on initial bet, requires Simoleon transaction
        // Reserved_For_Blackjack = 5
        Player_Win_Animation = 6,
        Player_Lose_Animation = 7,
        Player_Fold_Sequence = 8, // upon completion, invokes: Animation_Sequence_Complete
        // Reserved_For_Blackjack = 9
        // Reserved_For_Blackjack = 10
        // Reserved_For_Blackjack = 11
        // Reserved_For_Blackjack = 12
        Dealer_Collect_Cards = 13, // upon completion, invokes: Dealer_Callback
        New_Minimum_Bet = 14,
        New_Maximum_Bet = 15,
        Money_Over_Head = 16, // playerid sent in temp0, amount sent in temp1
        Deal_Additional_Community = 17, // Dealer animation to deal two more community cards - invokes: Animation_Sequence_Complete
        Failsafe_Delete_ID = 18, // set the playerid for this player to 0
        Failsafe_Remove_Dealer = 19, // set the attribute dealerid to 0
        Force_End_Playing = 20,
        /* New for HoldemCasino, in addition to enum = 17 */
        Player_Call_Sequence = 21, // requires Simoleon transaction, upon completion invokes: Animation_Sequence_Complete
        New_Max_Side_Bet = 22, // new bet sent in temp0
        Player_Win_Side_Bet = 23,

        // plugin only
        Animation_Sequence_Complete = 100, // call back during entre'act
        // Reserved_For_Blackjack = 101
        // Reserved_For_Blackjack = 102
        Dealer_Callback = 103 // call back during intermission

    }
    public enum VMEODHoldEmCasinoAlerts: byte
    {
        Ante_Bet_Help = 0,
        Side_Bet_Help = 1,
        Invalid_Ante = 2,
        Invalid_Number = 3,
        Invalid_Side_Valid_Ante = 4,
        False_Start = 5,
        Unknown_Betting_Error = 6,
        Call_Fold_Help = 7,
        State_Race = 8,
        Bet_Too_Low = 9,
        Bet_Too_High = 10,
        Bet_NSF = 11,
        Observe_Once = 12,
        Observe_Twice = 13,
        Table_NSF = 14,
        Player_NSF = 15,
        Call_NSF = 16,
        Side_Bet_Too_High = 17,
        Object_Broken = 18
    }
}
