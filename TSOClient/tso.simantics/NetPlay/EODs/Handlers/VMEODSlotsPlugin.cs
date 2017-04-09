using FSO.SimAntics.Primitives;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.NetPlay.EODs.Model;
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
        private VMEODClient userClient;
        private short onOffState;
        private VMEODSlotsStates State = VMEODSlotsStates.Managing;
        private Random slotsRandom = new Random();
        private int tock = 0;
        // money left in machine object
        private short machineOdds;
        private short machineBalance = 0;
        private short machineBalanceMax = 0;
        private short machineBalanceMin = 0;
        // wheel stops possibilities
        private int totalStops = 0;
        private int initialBlankStops;
        private int totalBlankStops;
        private int totalFirstStops;
        private int totalSecondStops;
        private int totalThirdStops;
        private int totalFourthStops;
        private int totalFifthStops;
        private int totalSixthStops;
        private VMEODSlotsStops[] slotsStops;
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
            initialBlankStops = 75;
            totalFirstStops = 42;
            totalSecondStops = 49;
            totalThirdStops = 36;
            totalFourthStops = 24;
            totalFifthStops = 12;
            totalSixthStops = 6;

            BinaryHandlers["slots_new_odds"] = NewOddsHandler;
            PlaintextHandlers["slots_toggle_onOff"] = ToggleOnOffHandler;
            PlaintextHandlers["slots_execute_bet"] = BetHandler;
            PlaintextHandlers["slots_wheels_stopped"] = WheelsStoppedHandler;
            PlaintextHandlers["slots_close_UI"] = SlotsCloseHandler;
            PlaintextHandlers["slots_withdraw"] = WithdrawHandler;
            PlaintextHandlers["slots_deposit"] = DepositHandler;
            SimanticsHandlers[(short)VMOEDSlotsObjectEvents.GameOver] = GameOverHandler;
            SimanticsHandlers[(short)VMOEDSlotsObjectEvents.InsufficientFunds] = GameOverHandler;
            SimanticsHandlers[1] = UnkownEventHandler;
            SimanticsHandlers[2] = UnkownEventHandler;
            SimanticsHandlers[3] = UnkownEventHandler;
            SimanticsHandlers[7] = UnkownEventHandler;
        }
        private void UnkownEventHandler(short eventID, VMEODClient player)
        {
            Console.WriteLine("I received this event and don't know what to do: " + eventID);
        }
        /*
         * This override of Tick is to be used to animate the UI lights and simulate the spinning of the slot wheels.
         */
        public override void Tick()
        {
            if (userClient != null) {
                tock++;
                switch (State)
                {
                    case VMEODSlotsStates.Idle:
                        if (tock >= 20)  // half of a real world second
                        {
                            userClient.Send("slots_animate_lights", "");
                            tock = 0;
                        }
                        break;
                    case VMEODSlotsStates.DisplayingLoss:
                        if (tock >= 20)
                        {
                            userClient.Send("slots_animate_lights", "");
                            tock = 0;
                        }
                        break;
                    case VMEODSlotsStates.Running:
                        userClient.Send("slots_animate_wheels", "");
                        if (tock >= 15)
                        {
                            userClient.Send("slots_animate_lights", "");
                            tock = 0;
                        }
                        break;
                    case VMEODSlotsStates.DisplayingWin:
                        if (tock >= 3)
                        {
                            userClient.Send("slots_animate_lights", "");
                            tock = 0;
                        }
                        break;
                    case VMEODSlotsStates.Off:
                        if (tock >= 90)
                        {
                            userClient.Send("slots_toggle_offline_message", "");
                            tock = 0;
                        }
                        break;
                    default:
                        // userClient is managing, no lights or wheels are visible
                        break;
                }
            }
            else
                tock = 0;
        }
        public override void OnConnection(VMEODClient client)
        {
            userClient = client;
            var args = client.Invoker.Thread.TempRegisters;
            if (args[1] < 80) // user is player, because on owner this has to be greater than 79
            {
                // get the machine odds
                machineOdds = args[3];

                // check the type of machine to set the min and max balances
                switch (args[2])
                {
                    case 0:
                        {
                            machineBalanceMin = (short)VMEODSlotMachineMinimumBalances.Viva_PGT;
                            machineBalanceMax = (short)VMEODSlotMachineMaximumBalances.Viva_PGT;
                            break;
                        }
                    case 1:
                        {
                            machineBalanceMin = (short)VMEODSlotMachineMinimumBalances.Gypsy_Queen;
                            machineBalanceMax = (short)VMEODSlotMachineMaximumBalances.Gypsy_Queen;
                            break;
                        }
                    case 2:
                        {
                            machineBalanceMin = (short)VMEODSlotMachineMinimumBalances.Jack_of_Hearts;
                            machineBalanceMax = (short)VMEODSlotMachineMaximumBalances.Jack_of_Hearts; // 32767 is max value of a short data type
                            break;
                        }
                    default:
                        {
                            Console.WriteLine("VMEODSlotsPlugin.OnConnection: I have no idea what slot machine type this is...");
                            break;
                        }
                }

                // get the amount of money in the machine by sending a testOnly transaction for $1 from maxis to machine
                var VM = userClient.vm;

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
                        machineBalance = Convert.ToInt16(budget2);
                        if (IsObjectBalanceInBounds() == true)
                        {
                            CalculateWheelStops(machineOdds);
                            userClient.Send("slots_player_init", new byte[3] { Convert.ToByte(args[1]), Convert.ToByte(args[2]), 1 });
                            State = VMEODSlotsStates.Idle;
                        }
                        else
                        {
                            userClient.Send("slots_player_init", new byte[3] { Convert.ToByte(args[1]), Convert.ToByte(args[2]), 0 });
                            State = VMEODSlotsStates.Off;
                        }
                    }
                    else
                    {
                        Console.WriteLine("There was an error trying to get this slot machine's balance.");
                        machineBalance = -1;
                        userClient.Send("slots_player_init", new byte[3] { Convert.ToByte(args[1]), Convert.ToByte(args[2]), 0 });
                        State = VMEODSlotsStates.Off;
                    }
                });
            }
            else // user is owner
            {
                // I'm sure there's a more elegant way, but get the type of machine by the 2nd letter in its name
                var name = Server.Object.Name;
                byte machineType = 0;
                if ((name != null) && (name.Length > 1)) {
                    switch (name[1]) {
                        // "Jack of Hearts" Slot Machine
                        case 'J':
                            {
                                machineType = 2;
                                break;
                            }
                        // "Gypsy Queen" Slot Machine
                        case 'G':
                            {
                                machineType = 1;
                                break;
                            }
                        // Viva PGT Home Casino - note lack of quote marks
                        default:
                            {
                                machineType = 0;
                                break;
                            }
                    }
                }
                State = VMEODSlotsStates.Managing;
                int tempMachineBalance = (16384 * args[3]) + args[2];
                userClient.Send("slots_owner_init", new byte[5] { Convert.ToByte(args[1]), Convert.ToByte(tempMachineBalance % 255),
                    Convert.ToByte(tempMachineBalance / 255), Convert.ToByte(args[4]), machineType });
            }
        }
        public override void OnDisconnection(VMEODClient client)
        {
            base.OnDisconnection(client);
        }
        private void ToggleOnOffHandler(string evt, string OnOffState, VMEODClient client)
        {
            onOffState = Int16.Parse(OnOffState);
            client.SendOBJEvent(new VMEODEvent((short)VMOEDSlotsObjectEvents.ToggleOnOff));
        }
        private void NewOddsHandler(string evt, Byte [] args, VMEODClient client)
        {
            machineOdds = args[0];
            client.SendOBJEvent(new VMEODEvent((short)VMOEDSlotsObjectEvents.SetNewPayout, new short[1] { machineOdds }));
        }
        private void WithdrawHandler(string evt, string amountString, VMEODClient client)
        {
            // parse the withdraw amount
            short withdrawAmount = Int16.Parse(amountString);

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
                    client.Send("slots_resume_manage", amountString);
                }
                else
                {
                    Console.WriteLine("VMEODSlotsPlugin.WithdrawHandler: There was an error trying to withdraw money to the owner!");
                    client.Send("slots_resume_manage", "0");
                }
            });

            // client.SendOBJEvent(new VMEODEvent((short)VMOEDSlotsObjectEvents.WithdrawCash)); *Depricated*
        }
        private void DepositHandler(string evt, string amountString, VMEODClient client)
        {
            // parse the withdraw amount
            short depositAmount = Int16.Parse(amountString);

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
                    client.Send("slots_resume_manage", "-" + amountString);
                }
                else
                {
                    client.Send("slots_deposit_fail", "" + amountString);
                }
            });
        }
        private void GameOverHandler(short eventID, VMEODClient player)
        {
            // check if object has enough money or too much money, then start new game or disconnect the player
            if (IsObjectBalanceInBounds() == true)
            {
                player.Send("slots_new_game", "");
                State = VMEODSlotsStates.Idle;
            }
            else
            {
                State = VMEODSlotsStates.Off;
                userClient.Send("slots_toggle_offline_message", "");
            }
        }
        private void BetHandler(string evt, string betAmountString, VMEODClient client)
        {
            short betAmount = Int16.Parse(betAmountString);
            // attempt to debit player the bet amount
            var VM = client.vm;
            
            VM.GlobalLink.PerformTransaction(VM, false, client.Avatar.PersistID, Server.Object.PersistID, Convert.ToInt32(betAmount),

            (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
            {
                // add bet to the balance in object
                machineBalance += betAmount;
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
                    // roll the slots rng and get result, send to UI and send to SlotResultHandler() to determine win
                    SlotResultHandler(RollNewGame(betAmount));
                }
                else
                    client.SendOBJEvent(new VMEODEvent((short)VMOEDSlotsObjectEvents.InsufficientFunds));
            });
        }
        private void SlotResultHandler(int[] betAndSlotResults)
        {
            short betAmount = Convert.ToInt16(betAndSlotResults[0]);
            int winnings = 0;
            // determine if roll is a winning roll and adjust winnings accordingly
            switch ((byte)slotsStops[betAndSlotResults[1]])
            {
                case (byte)VMEODSlotsStops.Sixth:
                    {
                        if (slotsStops[betAndSlotResults[2]].Equals(VMEODSlotsStops.Sixth))
                        {
                            if (slotsStops[betAndSlotResults[3]].Equals(VMEODSlotsStops.Sixth))
                            {
                                winnings = betAmount * SIX_SIX_SIX_PAYOUT_MULTIPLIER;
                            }
                        }
                        break;
                    }
                case (byte)VMEODSlotsStops.Fifth:
                    {
                        if (slotsStops[betAndSlotResults[2]].Equals(VMEODSlotsStops.Fifth))
                        {
                            if (slotsStops[betAndSlotResults[3]].Equals(VMEODSlotsStops.Fifth))
                            {
                                winnings = betAmount * FIVE_FIVE_FIVE_PAYOUT_MULTIPLIER;
                            }
                        }
                        break;
                    }
                case (byte)VMEODSlotsStops.Fourth:
                    {
                        if (slotsStops[betAndSlotResults[2]].Equals(VMEODSlotsStops.Fourth))
                        {
                            if (slotsStops[betAndSlotResults[3]].Equals(VMEODSlotsStops.Fourth))
                            {
                                winnings = betAmount * FOUR_FOUR_FOUR_PAYOUT_MULTIPLIER;
                            }
                        }
                        break;
                    }
                case (byte)VMEODSlotsStops.Third:
                    {
                        if (slotsStops[betAndSlotResults[2]].Equals(VMEODSlotsStops.Third))
                        {
                            if (slotsStops[betAndSlotResults[3]].Equals(VMEODSlotsStops.Third))
                            {
                                winnings = betAmount * THREE_THREE_THREE_PAYOUT_MULTIPLIER;
                            }
                        }
                        break;
                    }
                case (byte)VMEODSlotsStops.Second:
                    {
                        if (slotsStops[betAndSlotResults[2]].Equals(VMEODSlotsStops.Second))
                        {
                            if (slotsStops[betAndSlotResults[3]].Equals(VMEODSlotsStops.Second))
                            {
                                winnings = betAmount * TWO_TWO_TWO_PAYOUT_MULTIPLIER;
                            }
                        }
                        break;
                    }
                case (byte)VMEODSlotsStops.First:
                    {
                        if (slotsStops[betAndSlotResults[2]].Equals(VMEODSlotsStops.First))
                            winnings = betAmount * ONE_ONE_ANY_PAYOUT_MULTIPLIER;
                        else
                            winnings = betAmount * ONE_ANY_ANY_PAYOUT_MULTIPLIER;
                        break;
                    }
                default: // losing roll, winnings remain 0
                    break;
            }
            if (winnings > 0)
            {
                // dispatch event to tell object the winning amount
                userClient.SendOBJEvent(new VMEODEvent((short)VMOEDSlotsObjectEvents.SetWinAmount, new short[1] { Convert.ToInt16(winnings) }));
                // dispatch event to tell the object that the user has won
                userClient.SendOBJEvent(new VMEODEvent((short)VMOEDSlotsObjectEvents.ExecuteWin, new short[1] { betAmount }));
            }
            else
            {
                // dispatch event to tell object that the user has lost
                userClient.SendOBJEvent(new VMEODEvent((short)VMOEDSlotsObjectEvents.ExecuteLoss, new short[1] { betAmount }));
            }

            // send winnings and spins results to UI
            userClient.Send("slots_spin", new Byte[] { Convert.ToByte(winnings / 255), Convert.ToByte(winnings % 255),
                (byte)slotsStops[betAndSlotResults[1]], (byte)slotsStops[betAndSlotResults[2]], (byte)slotsStops[betAndSlotResults[3]] });
            State = VMEODSlotsStates.Running;
        }
        private void SlotsCloseHandler(string evt, string arg, VMEODClient client)
        {
            Server.Disconnect(client);
        }
        private int[] RollNewGame(int betAmount)
        {
            int wheelOneStop = slotsRandom.Next(totalStops);
            int wheelTwoStop = slotsRandom.Next(totalStops);
            int wheelThreeStop = slotsRandom.Next(totalStops);
            return new int[4] { betAmount, wheelOneStop, wheelTwoStop, wheelThreeStop};
        }
        private void WheelsStoppedHandler(string evt, string payoutString, VMEODClient client)
        {
            if (payoutString[0] != '0')
            {
                State = VMEODSlotsStates.DisplayingWin;
                // send win event with random number for message displayed
                client.Send("slots_display_win", "" + slotsRandom.Next(25, 30) + "%" + payoutString);

                // pay the player
                short winnings = Int16.Parse(payoutString);
                
                var VM = client.vm;
                VM.GlobalLink.PerformTransaction(VM, false, Server.Object.PersistID, client.Avatar.PersistID, Convert.ToInt32(winnings),

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
                        machineBalance -= winnings;
                    }
                    else
                    {
                        Console.WriteLine("VMEODSlotsPlugin.WheelsStoppedHandler: There was an error while paying the player the winnings.");
                    }
                });
            }
            else
            {
                State = VMEODSlotsStates.DisplayingLoss;
                // send loss event with random number for message displayed
                client.Send("slots_display_loss", "" + slotsRandom.Next(30, 35));
            }
        }
        private bool IsObjectBalanceInBounds()
        {
            if ((machineBalance >= machineBalanceMin) && (machineBalance < machineBalanceMax))
                return true;
            else
                return false;
        }
        private void CalculateWheelStops(short paybackPercent)
        {
            // calculate to decimal and get inverse, in order to increase or decrease number of blank stops to affect winning odds
            float paybackDecimal = 1F - (paybackPercent / 100F);

            // increase or decrease each type of non-blank stop except for First by the rtp %
            totalSecondStops = totalSecondStops - Convert.ToInt32(Math.Round(totalSecondStops * paybackDecimal, 0));
            totalThirdStops = totalThirdStops - Convert.ToInt32(Math.Round(totalThirdStops * paybackDecimal, 0));
            totalFourthStops = totalFourthStops - Convert.ToInt32(Math.Round(totalFourthStops * paybackDecimal, 0));
            totalFifthStops = totalFifthStops - Convert.ToInt32(Math.Round(totalFifthStops * paybackDecimal, 0));
            totalSixthStops = totalSixthStops - Convert.ToInt32(Math.Round(totalSixthStops * paybackDecimal, 0));
            
            // First stops have special cases to tweak the RTP to be perfect, all math in a separate spreadsheet to prove accuracy
            if (paybackPercent < 100) // if less than 100%, remove firstStops
            {
                totalFirstStops = Convert.ToInt32(Math.Round(totalFirstStops - ((totalFirstStops * paybackDecimal) / 3), 0));
                if ((paybackPercent < 83) || (paybackPercent > 85 && paybackPercent < 97) || (paybackPercent == 98))
                    totalFirstStops += 1; // these special cases need 1 more firstStop
            }
            else // if 100% or above, add firstStops
                totalFirstStops = Convert.ToInt32(Math.Round(totalFirstStops + ((totalFirstStops * paybackDecimal) / 3), 0));

            // next calculate the total blank stops *note if paybackPercent is > 100, additionalBlankStops will be negative
            var additionalBlankStops = initialBlankStops * paybackDecimal;
            totalBlankStops = Convert.ToInt32(Math.Round(initialBlankStops + additionalBlankStops,0));

            // stupid floating point number fix
            if (paybackPercent == 110 && initialBlankStops == 75)
                totalBlankStops += 1;

            // divide the total blank stops evenly among the different types, adding the remainder to the last blank stop: sixthBlankStop
            int firstBlankStops;
            int secondBlankStops;
            int thirdBlankStops;
            int fourthBlankStops;
            int fifthBlankStops;
            int sixthBlankStops;
            firstBlankStops = secondBlankStops = thirdBlankStops = fourthBlankStops = fifthBlankStops = totalBlankStops / 6;
            sixthBlankStops = (totalBlankStops / 6) + (totalBlankStops % 6);

            // finally, add together all stops to get the number of totalStops
            totalStops = totalFirstStops + totalSecondStops + totalThirdStops + totalFourthStops + totalFifthStops + totalSixthStops +
                totalBlankStops;

            // Create new array of bytes with each index being a possible slot stop
            slotsStops = new VMEODSlotsStops[totalStops];
            int index = 0;
            int s;
            for (s = 0; s < firstBlankStops; s++)
            {
                slotsStops[index++] = VMEODSlotsStops.FirstBlank;
            }
            for (s = 0; s < totalFirstStops; s++)
            {
                slotsStops[index++] = VMEODSlotsStops.First;
            }
            for (s = 0; s < secondBlankStops; s++)
            {
                slotsStops[index++] = VMEODSlotsStops.SecondBlank;
            }
            for (s = 0; s < totalSecondStops; s++)
            {
                slotsStops[index++] = VMEODSlotsStops.Second;
            }
            for (s = 0; s < thirdBlankStops; s++)
            {
                slotsStops[index++] = VMEODSlotsStops.ThirdBlank;
            }
            for (s = 0; s < totalThirdStops; s++)
            {
                slotsStops[index++] = VMEODSlotsStops.Third;
            }
            for (s = 0; s < fourthBlankStops; s++)
            {
                slotsStops[index++] = VMEODSlotsStops.FourthBlank;
            }
            for (s = 0; s < totalFourthStops; s++)
            {
                slotsStops[index++] = VMEODSlotsStops.Fourth;
            }
            for (s = 0; s < fifthBlankStops; s++)
            {
                slotsStops[index++] = VMEODSlotsStops.FifthBlank;
            }
            for (s = 0; s < totalFifthStops; s++)
            {
                slotsStops[index++] = VMEODSlotsStops.Fifth;
            }
            for (s = 0; s < sixthBlankStops; s++)
            {
                slotsStops[index++] = VMEODSlotsStops.SixthBlank;
            }
            for (s = 0; s < totalSixthStops; s++)
            {
                slotsStops[index++] = VMEODSlotsStops.Sixth;
            }
            if (index != totalStops)
                Console.WriteLine("VMEODSlotsPlugin.CalculateWheelStops: There was an error in creating the stops Array. The math is wrong.");
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
        InsufficientFunds = 5,
        GameOver = 6, // dispatched by object after win, loss, or "ok" click dialogue box of insufficient funds
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
}
