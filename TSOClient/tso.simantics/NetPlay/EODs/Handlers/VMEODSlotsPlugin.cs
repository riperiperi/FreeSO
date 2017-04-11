using FSO.SimAntics.Primitives;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODSlotsPlugin : VMEODHandler
    {
        private VMEODClient UserClient;
        private Random SlotsRandom = new Random();
        private byte MachineType;
        private byte MachineBetDenomination;
        private short MachinePaybackPercent;
        private short MachineBalance = 0;
        private short MachineBalanceMax = 0;
        private short MachineBalanceMin = 0;
        private int CurrentWinnings = 0;
        // wheel stops possibilities
        private int TotalStops = 0;
        private int InitialBlankStops;
        private int TotalBlankStops;
        private int TotalFirstStops;
        private int TotalSecondStops;
        private int TotalThirdStops;
        private int TotalFourthStops;
        private int TotalFifthStops;
        private int TotalSixthStops;
        private VMEODSlotsStops[] SlotsStops;
        // payout constants
        public const int SIX_SIX_SIX_PAYOUT_MULTIPLIER = 500;
        public const int FIVE_FIVE_FIVE_PAYOUT_MULTIPLIER = 150;
        public const int FOUR_FOUR_FOUR_PAYOUT_MULTIPLIER = 75;
        public const int THREE_THREE_THREE_PAYOUT_MULTIPLIER = 50;
        public const int TWO_TWO_TWO_PAYOUT_MULTIPLIER = 25;
        public const int THREE_TWO_ONE_PAYOUT_MULTIPLIER = 10;
        public const int ONE_ONE_ANY_PAYOUT_MULTIPLIER = 5;
        public const int ONE_ANY_ANY_PAYOUT_MULTIPLIER = 2;

        public VMEODSlotsPlugin(VMEODServer server) : base(server)
        {
            // Insitialise stop probability data for 100% RTP slot machine, these will change below if owner set payout is not 100%
            InitialBlankStops = 75;
            TotalFirstStops = 42;
            TotalSecondStops = 49;
            TotalThirdStops = 36;
            TotalFourthStops = 24;
            TotalFifthStops = 12;
            TotalSixthStops = 6;

            BinaryHandlers["slots_new_odds"] = NewOddsHandler;
            PlaintextHandlers["slots_toggle_onOff"] = ToggleOnOffHandler;
            PlaintextHandlers["slots_execute_bet"] = BetHandler;
            PlaintextHandlers["slots_wheels_stopped"] = WheelsStoppedHandler;
            PlaintextHandlers["slots_close_UI"] = SlotsCloseHandler;
            PlaintextHandlers["slots_withdraw"] = WithdrawHandler;
            PlaintextHandlers["slots_deposit"] = DepositHandler;
            SimanticsHandlers[(short)VMOEDSlotsObjectEvents.GameOver] = GameOverHandler;
            SimanticsHandlers[(short)VMOEDSlotsObjectEvents.InsufficientFunds] = GameOverHandler;
            SimanticsHandlers[1] = UnknownEventHandler;
            SimanticsHandlers[2] = UnknownEventHandler;
            SimanticsHandlers[3] = UnknownEventHandler;
            SimanticsHandlers[7] = UnknownEventHandler;
        }
        private void UnknownEventHandler(short eventID, VMEODClient player)
        {
            //Console.WriteLine("I received this event and don't know what to do: " + eventID);
        }
        /*
         * This override of Tick is to be used to animate the UI lights and simulate the spinning of the slot wheels.
         */
        public override void Tick()
        {

        }
        public override void OnConnection(VMEODClient client)
        {
            UserClient = client;

            var args = client.Invoker.Thread.TempRegisters;
            
            var name = Server.Object.Name;
            if ((name != null) && (name.Length > 1))
            {
                switch (name[1])
                {
                    // "Jack of Hearts" Slot Machine
                    case 'J':
                        {
                            MachineType = 2;
                            MachineBetDenomination = 10;
                            MachineBalanceMin = (short)VMEODSlotMachineMinimumBalances.Jack_of_Hearts;
                            MachineBalanceMax = (short)VMEODSlotMachineMaximumBalances.Jack_of_Hearts; // 32767 is max value of a short data type
                            break;
                        }
                    // "Gypsy Queen" Slot Machine
                    case 'G':
                        {
                            MachineType = 1;
                            MachineBetDenomination = 5;
                            MachineBalanceMin = (short)VMEODSlotMachineMinimumBalances.Gypsy_Queen;
                            MachineBalanceMax = (short)VMEODSlotMachineMaximumBalances.Gypsy_Queen;
                            break;
                        }
                    // Viva PGT Home Casino - note lack of quote marks
                    default:
                        {
                            MachineType = 0;
                            MachineBetDenomination = 1;
                            MachineBalanceMin = (short)VMEODSlotMachineMinimumBalances.Viva_PGT;
                            MachineBalanceMax = (short)VMEODSlotMachineMaximumBalances.Viva_PGT;
                            break;
                        }
                }
            }
            // is the cilent the owner of the object?
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == UserClient.Avatar.PersistID);

            // UserClient is a player
            if ((args != null) && (args.Length > 3) && (args[1] < 79)) // is player because arg[1] on player can only be 1 5 or 10
            { // args[3] = StackObject's Payback
                MachinePaybackPercent = args[3];
                CalculateWheelStops(MachinePaybackPercent);
                UserClient.Send("slots_player_init", new byte[] { MachineBetDenomination, MachineType });
            }
            // UserClient is owner
            else if ((isOwner == true) && (args != null) && (args.Length > 4) && (args[1] > 79)) // HAS to be 80 > args[1] < 100
            { // args[1] = StackObject's Payback, args[2] = StackObject's (Balance % 16384), args[3] = StackObject's (Balance / 16384), args[4] = 0 for "Off" or 1 for "On"
                var AlledgedMachineBalance = Convert.ToInt16((16384 * args[3]) + args[2]);
                UserClient.Send("slots_owner_init", new byte[5] { Convert.ToByte(args[1]), Convert.ToByte(AlledgedMachineBalance % 255),
                    Convert.ToByte(AlledgedMachineBalance / 255), Convert.ToByte(args[4]), MachineType });
            }
            // get the amount of money in the machine by sending a testOnly transaction for $1 from maxis to machine
            var VM = UserClient.vm;

            VM.GlobalLink.PerformTransaction(VM, true, uint.MaxValue, Server.Object.PersistID, 1,

            (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
            {
                //TODO: Make this part of global link
                VM.SendCommand(new VMNetAsyncResponseCmd(0, new VMTransferFundsState
                {
                    Responded = true,
                    Success = success,
                    TransferAmount = transferAmount,
                    UID1 = uid1,
                    Budget1 = budget1,
                    UID2 = uid2,
                    Budget2 = budget2
                }));
                if (success)
                {
                    // amount of money currently in the machine
                    MachineBalance = Convert.ToInt16(budget2);
                    if ((args != null) && (args.Length > 3) && (args[1] < 79))
                        UserClient.Send("slots_new_game", "");
                }
                else
                {
                    //Console.WriteLine("There was an error trying to get this slot machine's balance.");
                    MachineBalance = -1;
                }
            });
        }
        public override void OnDisconnection(VMEODClient client)
        {
            base.OnDisconnection(client);
        }
        private void GameOverHandler(short eventID, VMEODClient player)
        {
            // check if object has enough money or too much money, then start new game or send offline message to the player
            if (IsObjectBalanceInBounds() == true)
            {
                player.Send("slots_new_game", "");
            }
            else
            {
                UserClient.Send("slots_close_machine", "");
            }
        }
        private void ToggleOnOffHandler(string evt, string OnOffStateString, VMEODClient client)
        {
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == UserClient.Avatar.PersistID);
            if (isOwner)
                client.SendOBJEvent(new VMEODEvent((short)VMOEDSlotsObjectEvents.ToggleOnOff));
        }
        private void NewOddsHandler(string evt, Byte [] args, VMEODClient client)
        {
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == UserClient.Avatar.PersistID);
            if (isOwner)
            {
                if ((args != null) && (args.Length > 0))
                {
                    if (args[0] < 80)
                        MachinePaybackPercent = 80;
                    else if (args[0] > 110)
                        MachinePaybackPercent = 110;
                    else
                        MachinePaybackPercent = args[0];
                }
                client.SendOBJEvent(new VMEODEvent((short)VMOEDSlotsObjectEvents.SetNewPayout, new short[1] { MachinePaybackPercent }));
            }
        }
        private void WithdrawHandler(string evt, string amountString, VMEODClient client)
        {
            string failureReason = "";

            // try to parse the withdraw amount
            var result = Int16.TryParse(amountString.Trim(), out short withdrawAmount);
            if (result)
            {
                // check the successfully parsed amount to make sure it's non-negative and against the MachineBalance to determine if valid amount
                if (withdrawAmount == 0)
                    failureReason = VMEODSlotsInputErrorTypes.Null.ToString();
                else if (withdrawAmount < 0)
                    failureReason = VMEODSlotsInputErrorTypes.Invalid.ToString();
                else
                {
                    if (withdrawAmount > MachineBalance)
                        failureReason = VMEODSlotsInputErrorTypes.Overflow.ToString();
                }
            }
            else // invalid input was sent to the server from a modified client
            {
                failureReason = VMEODSlotsInputErrorTypes.Invalid.ToString();
            }
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == UserClient.Avatar.PersistID);
            // if the client is the owner of the object AND there were no failures detected
            if ((isOwner == true) && (failureReason.Length == 0))
            {

                // atempt to credit the owner by debiting the machine
                var VM = client.vm;
                VM.GlobalLink.PerformTransaction(VM, false, Server.Object.PersistID, client.Avatar.PersistID, Convert.ToInt32(withdrawAmount),

                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                {
                //TODO: Make this part of global link
                VM.SendCommand(new VMNetAsyncResponseCmd(0, new VMTransferFundsState
                    {
                        Responded = true,
                        Success = success,
                        TransferAmount = transferAmount,
                        UID1 = uid1,
                        Budget1 = budget1,
                        UID2 = uid2,
                        Budget2 = budget2
                    }));
                    if (success)
                    {
                        MachineBalance = Convert.ToInt16(budget1);
                        client.Send("slots_resume_manage", budget1 + "");
                    }
                    else
                    {
                        //Console.WriteLine("VMEODSlotsPlugin.WithdrawHandler: There was an error trying to withdraw money to the owner!");
                        client.Send("slots_withdraw_fail", VMEODSlotsInputErrorTypes.Unknown.ToString());
                    }
                });
            }
            else // otherwise, send the failureReason
            {
                if (failureReason.Length == 0)
                    failureReason = VMEODSlotsInputErrorTypes.InvalidOwner.ToString();
                client.Send("slots_withdraw_fail", failureReason);
            }
        }
        private void DepositHandler(string evt, string amountString, VMEODClient client)
        {
            string failureReason = "";

            // try to parse the deposit amount
            var result = Int16.TryParse(amountString.Trim(), out short depositAmount);
            if (result)
            {
                // check the successfully parsed amount to make sure it's non-negative and against the MachineBalanceMax to determine if valid amount
                if (depositAmount == 0)
                    failureReason = VMEODSlotsInputErrorTypes.Null.ToString();
                else if (depositAmount < 0)
                    failureReason = VMEODSlotsInputErrorTypes.Invalid.ToString();
                else
                {
                    if (depositAmount + MachineBalance > MachineBalanceMax)
                        failureReason = VMEODSlotsInputErrorTypes.Overflow.ToString();
                }
            }
            else // invalid input was sent to the server from a modified client
            {
                failureReason = VMEODSlotsInputErrorTypes.Invalid.ToString();
            }
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == UserClient.Avatar.PersistID);
            // if the client is the owner of the object AND there were no failures detected
            if ((isOwner == true) && (failureReason.Length == 0))
            {
                // atempt to credit the machine by debiting the owner
                var VM = client.vm;
                VM.GlobalLink.PerformTransaction(VM, false, client.Avatar.PersistID, Server.Object.PersistID, Convert.ToInt32(depositAmount),

                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                {
                //TODO: Make this part of global link
                VM.SendCommand(new VMNetAsyncResponseCmd(0, new VMTransferFundsState
                    {
                        Responded = true,
                        Success = success,
                        TransferAmount = transferAmount,
                        UID1 = uid1,
                        Budget1 = budget1,
                        UID2 = uid2,
                        Budget2 = budget2
                    }));
                    if (success)
                    {
                        MachineBalance = Convert.ToInt16(budget2);
                        client.Send("slots_resume_manage", budget2 + "");
                    }
                    else
                    {
                        // Owner does not have enough simoleons to make this deposit
                        client.Send("slots_deposit_NSF", "" + amountString);
                    }
                });
            }
            else // otherwise, send the failureReason
            {
                if (failureReason.Length == 0)
                    failureReason = VMEODSlotsInputErrorTypes.InvalidOwner.ToString();
                client.Send("slots_deposit_fail", failureReason);
            }
        }
        private void BetHandler(string evt, string betAmountString, VMEODClient client)
        {
            bool betIsValid = false;
            bool inputIsValid = Int16.TryParse(betAmountString, out short betAmount);

            // Check to make sure the valid input is actually a valid bet by testing MachineBetDenomination and betAmount
            if (inputIsValid)
            {
                if (betAmount % MachineBetDenomination == 0)
                {
                    if (betAmount >= MachineBetDenomination)
                    {
                        if (betAmount <= MachineBetDenomination * 5)
                            betIsValid = true;
                    }
                }
            }

            if (betIsValid == true)
            {
                // roll the slots rng and get result, send to UI and send to SlotResultHandler() to determine amount of CurrentWinnings, CurrentWinnings = 0 is a loss
                var thisGameBetAndResults = RollNewGame(betAmount);
                CurrentWinnings = SlotResultHandler(thisGameBetAndResults);

                // attempt to debit player the bet amount
                var VM = client.vm;

                VM.GlobalLink.PerformTransaction(VM, false, client.Avatar.PersistID, Server.Object.PersistID, Convert.ToInt32(betAmount),

                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                {
                //TODO: Make this part of global link
                VM.SendCommand(new VMNetAsyncResponseCmd(0, new VMTransferFundsState
                    {
                        Responded = true,
                        Success = success,
                        TransferAmount = transferAmount,
                        UID1 = uid1,
                        Budget1 = budget1,
                        UID2 = uid2,
                        Budget2 = budget2
                    }));
                    if (success)
                    {
                    // update the balance of the machine
                    MachineBalance = Convert.ToInt16(budget2);

                        if (CurrentWinnings > 0)
                        {
                        // dispatch event to tell object the winning amount
                        UserClient.SendOBJEvent(new VMEODEvent((short)VMOEDSlotsObjectEvents.SetWinAmount, new short[1] { Convert.ToInt16(CurrentWinnings) }));
                        // dispatch event to tell the object that the user has won
                        UserClient.SendOBJEvent(new VMEODEvent((short)VMOEDSlotsObjectEvents.ExecuteWin, new short[1] { betAmount }));
                        }
                        else
                        {
                        // dispatch event to tell object that the user has lost
                        UserClient.SendOBJEvent(new VMEODEvent((short)VMOEDSlotsObjectEvents.ExecuteLoss, new short[1] { betAmount }));
                        }
                        // send spins results to UI to simulate gameplay
                        UserClient.Send("slots_spin", new Byte[] { (byte)SlotsStops[thisGameBetAndResults[1]], (byte)SlotsStops[thisGameBetAndResults[2]],
                        (byte)SlotsStops[thisGameBetAndResults[3]] });
                    }
                    else
                    {
                        CurrentWinnings = 0; // forfeit any winnings
                        client.SendOBJEvent(new VMEODEvent((short)VMOEDSlotsObjectEvents.InsufficientFunds));
                    }
                });
            }
            else
            {
                // bet input was invalid due to a modified client or some major server communication error
                client.SendOBJEvent(new VMEODEvent((short)VMOEDSlotsObjectEvents.InsufficientFunds));
            }
        }
        private int SlotResultHandler(int[] betAndSlotResults)
        {
            // the bet argument here has already been checked for validity and authenticity
            short betAmount = Convert.ToInt16(betAndSlotResults[0]);

            int winnings = 0;
            
            // determine if roll is a winning roll and adjust winnings accordingly
            switch ((byte)SlotsStops[betAndSlotResults[1]])
            {
                case (byte)VMEODSlotsStops.Sixth:
                    {
                        if (SlotsStops[betAndSlotResults[2]].Equals(VMEODSlotsStops.Sixth))
                        {
                            if (SlotsStops[betAndSlotResults[3]].Equals(VMEODSlotsStops.Sixth))
                            {
                                winnings = betAmount * SIX_SIX_SIX_PAYOUT_MULTIPLIER;
                            }
                        }
                        break;
                    }
                case (byte)VMEODSlotsStops.Fifth:
                    {
                        if (SlotsStops[betAndSlotResults[2]].Equals(VMEODSlotsStops.Fifth))
                        {
                            if (SlotsStops[betAndSlotResults[3]].Equals(VMEODSlotsStops.Fifth))
                            {
                                winnings = betAmount * FIVE_FIVE_FIVE_PAYOUT_MULTIPLIER;
                            }
                        }
                        break;
                    }
                case (byte)VMEODSlotsStops.Fourth:
                    {
                        if (SlotsStops[betAndSlotResults[2]].Equals(VMEODSlotsStops.Fourth))
                        {
                            if (SlotsStops[betAndSlotResults[3]].Equals(VMEODSlotsStops.Fourth))
                            {
                                winnings = betAmount * FOUR_FOUR_FOUR_PAYOUT_MULTIPLIER;
                            }
                        }
                        break;
                    }
                case (byte)VMEODSlotsStops.Third:
                    {
                        if (SlotsStops[betAndSlotResults[2]].Equals(VMEODSlotsStops.Third))
                        {
                            if (SlotsStops[betAndSlotResults[3]].Equals(VMEODSlotsStops.Third))
                            {
                                winnings = betAmount * THREE_THREE_THREE_PAYOUT_MULTIPLIER;
                            }
                        }
                        else if (SlotsStops[betAndSlotResults[2]].Equals(VMEODSlotsStops.Second))
                        {
                            if (SlotsStops[betAndSlotResults[3]].Equals(VMEODSlotsStops.First))
                            {
                                winnings = betAmount * THREE_TWO_ONE_PAYOUT_MULTIPLIER;
                            }
                        }
                        break;
                    }
                case (byte)VMEODSlotsStops.Second:
                    {
                        if (SlotsStops[betAndSlotResults[2]].Equals(VMEODSlotsStops.Second))
                        {
                            if (SlotsStops[betAndSlotResults[3]].Equals(VMEODSlotsStops.Second))
                            {
                                winnings = betAmount * TWO_TWO_TWO_PAYOUT_MULTIPLIER;
                            }
                        }
                        break;
                    }
                case (byte)VMEODSlotsStops.First:
                    {
                        if (SlotsStops[betAndSlotResults[2]].Equals(VMEODSlotsStops.First))
                            winnings = betAmount * ONE_ONE_ANY_PAYOUT_MULTIPLIER;
                        else
                            winnings = betAmount * ONE_ANY_ANY_PAYOUT_MULTIPLIER;
                        break;
                    }
                default: // losing roll, winnings remain 0
                    winnings = 0;
                    break;
            }
            return winnings;
        }
        private void SlotsCloseHandler(string evt, string arg, VMEODClient client)
        {
            Server.Disconnect(client);
        }
        private int[] RollNewGame(int betAmount)
        {
            int wheelOneStop = SlotsRandom.Next(TotalStops);
            int wheelTwoStop = SlotsRandom.Next(TotalStops);
            int wheelThreeStop = SlotsRandom.Next(TotalStops);
            return new int[4] { betAmount, wheelOneStop, wheelTwoStop, wheelThreeStop};
        }
        private void WheelsStoppedHandler(string evt, string uselessString, VMEODClient client)
        {
            if (CurrentWinnings > 0)
            {
                // send win event with random number for message displayed
                client.Send("slots_display_win", "" + SlotsRandom.Next(25, 30) + "%" + CurrentWinnings);

                // pay the player
                var VM = client.vm;
                VM.GlobalLink.PerformTransaction(VM, false, Server.Object.PersistID, client.Avatar.PersistID, Convert.ToInt32(CurrentWinnings),

                // debit the balance of the machine
                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                {
                    //TODO: Make this part of global link
                    VM.SendCommand(new VMNetAsyncResponseCmd(0, new VMTransferFundsState
                    {
                        Responded = true,
                        Success = success,
                        TransferAmount = transferAmount,
                        UID1 = uid1,
                        Budget1 = budget1,
                        UID2 = uid2,
                        Budget2 = budget2
                    }));
                    if (success)
                    {
                        MachineBalance = Convert.ToInt16(budget1);
                        CurrentWinnings = 0; // winning payout cannot be duplicated
                    }
                    else
                    {
                        //Console.WriteLine("VMEODSlotsPlugin.WheelsStoppedHandler: There was an error while paying the player the winnings.");
                        CurrentWinnings = 0; // winning payout cannot be duplicated
                    }
                });
            }
            else
            {
                // send loss event with random number for message displayed
                client.Send("slots_display_loss", "" + SlotsRandom.Next(30, 35));
            }
        }
        private bool IsObjectBalanceInBounds()
        {
            if ((MachineBalance >= MachineBalanceMin) && (MachineBalance < MachineBalanceMax))
                return true;
            else
                return false;
        }
        private void CalculateWheelStops(short paybackPercent)
        {
            // calculate to decimal and get inverse, in order to increase or decrease number of blank stops to affect winning odds
            float paybackDecimal = 1F - (paybackPercent / 100F);

            // increase or decrease each type of non-blank stop except for First by the rtp %
            TotalSecondStops = TotalSecondStops - Convert.ToInt32(Math.Round(TotalSecondStops * paybackDecimal, 0));
            TotalThirdStops = TotalThirdStops - Convert.ToInt32(Math.Round(TotalThirdStops * paybackDecimal, 0));
            TotalFourthStops = TotalFourthStops - Convert.ToInt32(Math.Round(TotalFourthStops * paybackDecimal, 0));
            TotalFifthStops = TotalFifthStops - Convert.ToInt32(Math.Round(TotalFifthStops * paybackDecimal, 0));
            TotalSixthStops = TotalSixthStops - Convert.ToInt32(Math.Round(TotalSixthStops * paybackDecimal, 0));
            
            // First stops have special cases to tweak the RTP to be perfect, all math in a separate spreadsheet to prove accuracy
            if (paybackPercent < 100) // if less than 100%, remove firstStops
            {
                TotalFirstStops = Convert.ToInt32(Math.Round(TotalFirstStops - ((TotalFirstStops * paybackDecimal) / 3), 0));
                if ((paybackPercent < 83) || (paybackPercent > 85 && paybackPercent < 97) || (paybackPercent == 98))
                    TotalFirstStops += 1; // these special cases need 1 more firstStop
            }
            else // if 100% or above, add firstStops
                TotalFirstStops = Convert.ToInt32(Math.Round(TotalFirstStops + ((TotalFirstStops * paybackDecimal) / 3), 0));

            // next calculate the total blank stops *note if paybackPercent is > 100, additionalBlankStops will be negative
            var additionalBlankStops = InitialBlankStops * paybackDecimal;
            TotalBlankStops = Convert.ToInt32(Math.Round(InitialBlankStops + additionalBlankStops,0));

            // stupid floating point number fix
            if (paybackPercent == 110 && InitialBlankStops == 75)
                TotalBlankStops += 1;

            // divide the total blank stops evenly among the different types, adding the remainder to the last blank stop: sixthBlankStop
            int firstBlankStops;
            int secondBlankStops;
            int thirdBlankStops;
            int fourthBlankStops;
            int fifthBlankStops;
            int sixthBlankStops;
            firstBlankStops = secondBlankStops = thirdBlankStops = fourthBlankStops = fifthBlankStops = TotalBlankStops / 6;
            sixthBlankStops = (TotalBlankStops / 6) + (TotalBlankStops % 6);

            // finally, add together all stops to get the number of TotalStops
            TotalStops = TotalFirstStops + TotalSecondStops + TotalThirdStops + TotalFourthStops + TotalFifthStops + TotalSixthStops +
                TotalBlankStops;

            // Create new array of bytes with each index being a possible slot stop
            SlotsStops = new VMEODSlotsStops[TotalStops];
            int index = 0;
            int s;
            for (s = 0; s < firstBlankStops; s++)
            {
                SlotsStops[index++] = VMEODSlotsStops.FirstBlank;
            }
            for (s = 0; s < TotalFirstStops; s++)
            {
                SlotsStops[index++] = VMEODSlotsStops.First;
            }
            for (s = 0; s < secondBlankStops; s++)
            {
                SlotsStops[index++] = VMEODSlotsStops.SecondBlank;
            }
            for (s = 0; s < TotalSecondStops; s++)
            {
                SlotsStops[index++] = VMEODSlotsStops.Second;
            }
            for (s = 0; s < thirdBlankStops; s++)
            {
                SlotsStops[index++] = VMEODSlotsStops.ThirdBlank;
            }
            for (s = 0; s < TotalThirdStops; s++)
            {
                SlotsStops[index++] = VMEODSlotsStops.Third;
            }
            for (s = 0; s < fourthBlankStops; s++)
            {
                SlotsStops[index++] = VMEODSlotsStops.FourthBlank;
            }
            for (s = 0; s < TotalFourthStops; s++)
            {
                SlotsStops[index++] = VMEODSlotsStops.Fourth;
            }
            for (s = 0; s < fifthBlankStops; s++)
            {
                SlotsStops[index++] = VMEODSlotsStops.FifthBlank;
            }
            for (s = 0; s < TotalFifthStops; s++)
            {
                SlotsStops[index++] = VMEODSlotsStops.Fifth;
            }
            for (s = 0; s < sixthBlankStops; s++)
            {
                SlotsStops[index++] = VMEODSlotsStops.SixthBlank;
            }
            for (s = 0; s < TotalSixthStops; s++)
            {
                SlotsStops[index++] = VMEODSlotsStops.Sixth;
            }
            if (index != TotalStops)
            {
                //Console.WriteLine("VMEODSlotsPlugin.CalculateWheelStops: There was an error in creating the stops Array. The math is wrong.");
            }
        }
    }
    public enum VMEODSlotsStates : byte
    {
        Idle = 0, // after GameOver event
        Running = 1, // during initial ExecuteLoss/ExecuteWin event
        DisplayingWin = 2,  // during ExecuteWin after wheels stop
        DisplayingLoss = 3, // during ExecuteLoss after wheels stop
        Off = 4, // machine turned off, full, or insufficient funds
        Managing = 5 // owner is managing object
    }
    public enum VMOEDSlotsObjectEvents : short
    {
        InitPlay = -2,
        StopPlay = -1, // dispatched by object on Exit for Motives — onDisconnect may be better
        Idle = 0,
        // Unknown or Unimplemented = 1,
        // Unknown or Unimplemented = 2,
        // Unknown or Unimplemented = 3,
        ExecuteWin = 4, // @arg short betAmount
        InsufficientFunds = 5, // also dispatched by object after users clicks "ok" from insufficient funds dialogue alert
        GameOver = 6, // dispatched by object after win, or loss
        // Unknown or Unimplemented = 7,
        ExecuteLoss = 8, // @arg short betAmount
        SetWinAmount = 9, // @arg short winAmount
        WithdrawCash = 10, // dispatched by object intereaction "Get Cash"
        SetNewPayout = 11, // @arg short newPayout
        ToggleOnOff = 12
    }
    [Flags]
    public enum VMEODSlotsStops : byte
    {
        FirstBlank = 0,
        First = 1,
        SecondBlank = 2,
        Second = 3,
        ThirdBlank = 4,
        Third = 5,
        FourthBlank = 6,
        Fourth = 7,
        FifthBlank = 8,
        Fifth = 9,
        SixthBlank = 10,
        Sixth = 11
    }
    public enum VMEODSlotMachineMinimumBalances : short
    {
        Viva_PGT = 2500,
        Gypsy_Queen = 12500,
        Jack_of_Hearts = 25000
    }
    public enum VMEODSlotMachineMaximumBalances : short
    {
        Viva_PGT = 7500,
        Gypsy_Queen = 17500,
        Jack_of_Hearts = 32500
    }
    [Flags]
    public enum VMEODSlotsInputErrorTypes : byte
    {
        Null = 0,
        Invalid = 1,
        Overflow = 2,
        NonsufficientFunds = 3,
        InvalidOwner = 4,
        Unknown = 5
    }
}
