using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Client;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Primitives;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODRoulettePlugin : VMEODHandler
    {
        public static readonly List<byte> BlackNumbersList = new List<byte>{ 2, 4, 6, 8, 10, 11, 13, 15, 17, 20, 22, 24, 26, 28, 29, 31, 33, 35 };
        public static readonly List<byte> RedNumbersList = new List<byte> { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };
        public static readonly List<byte> LowNumbersList = new List<byte> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
        public static readonly List<byte> HighNumbersList = new List<byte> { 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36 };
        private VMEODRouletteGameStates GameState;
        private VMEODRouletteGameStates NextState;
        private int MinBet;
        private int MaxBet;
        private int TableBalance;
        private int Roundtimer;
        private int Tock;
        private VMEODClient Controller;
        private VMEODClient Croupier;
        private VMEODClient Owner;
        private List<RoulettePlayer> Players;
        private Random NextBall = new Random();

        public const int GLOBAL_MAXIMUM_CHIPS_PER_STACK = 20;
        public const int GLOBAL_MAXIMUM_ROULETTE_ROUND_BET = 1000;

        public static Dictionary<VMEODRouletteBetTypes, string> RouletteBetTypes = new Dictionary<VMEODRouletteBetTypes, string>()
        {
            { VMEODRouletteBetTypes.StraightUp, "ST8" },
            { VMEODRouletteBetTypes.Split, "SPL" },
            { VMEODRouletteBetTypes.Street, "STR" },
            { VMEODRouletteBetTypes.Corner, "COR" },
            { VMEODRouletteBetTypes.Sucker, "SUC" },
            { VMEODRouletteBetTypes.Line, "LIN" },
            { VMEODRouletteBetTypes.Dozen, "DOZ" },
            { VMEODRouletteBetTypes.Column, "COL" },
            { VMEODRouletteBetTypes.Odd, "ODD" },
            { VMEODRouletteBetTypes.Even, "EVE" },
            { VMEODRouletteBetTypes.Low, "LOW" },
            { VMEODRouletteBetTypes.High, "HIG" },
            { VMEODRouletteBetTypes.Red, "RED" },
            { VMEODRouletteBetTypes.Black, "BLA" }
        };

        public static int BetTypeToPayoutRatio(VMEODRouletteBetTypes type)
        {
            switch (type)
            {
                case VMEODRouletteBetTypes.StraightUp:
                    return 35;
                case VMEODRouletteBetTypes.Split:
                    return 17;
                case VMEODRouletteBetTypes.Street:
                    return 11;
                case VMEODRouletteBetTypes.Corner:
                    return 8;
                case VMEODRouletteBetTypes.Sucker:
                    return 6;
                case VMEODRouletteBetTypes.Line:
                    return 5;
                case VMEODRouletteBetTypes.Dozen:
                case VMEODRouletteBetTypes.Column:
                    return 2;
                case VMEODRouletteBetTypes.Even:
                case VMEODRouletteBetTypes.Odd:
                case VMEODRouletteBetTypes.Red:
                case VMEODRouletteBetTypes.Black:
                case VMEODRouletteBetTypes.High:
                case VMEODRouletteBetTypes.Low:
                    return 1;
            }
            return 0;
        }

        public static int BetTypeToNumberOfNumbersInPlay(VMEODRouletteBetTypes type)
        {
            switch (type)
            {
                case VMEODRouletteBetTypes.StraightUp:
                    return 1;
                case VMEODRouletteBetTypes.Split:
                    return 2;
                case VMEODRouletteBetTypes.Street:
                    return 3;
                case VMEODRouletteBetTypes.Corner:
                    return 4;
                case VMEODRouletteBetTypes.Sucker:
                    return 5;
                case VMEODRouletteBetTypes.Line:
                    return 6;
                case VMEODRouletteBetTypes.Dozen:
                case VMEODRouletteBetTypes.Column:
                    return 12;
                case VMEODRouletteBetTypes.Even:
                case VMEODRouletteBetTypes.Odd:
                case VMEODRouletteBetTypes.Red:
                case VMEODRouletteBetTypes.Black:
                case VMEODRouletteBetTypes.High:
                case VMEODRouletteBetTypes.Low:
                    return 18;
            }
            return 0;
        }

        public static bool CompareNumbersInPlay(List<byte> listA, List<byte> listB)
        {
            if (listA.Count == listB.Count)
            {
                for (int index = 0; index < listA.Count; index++)
                {
                    if (listA[index] != listB[index])
                        return false;
                }
                return true;
            }
            return false;
        }

        public static bool NumbersListIsValidForBetType(VMEODRouletteBetTypes type, List<byte> numbersList)
        {
            /*
             * **IMPORTANT** ASSUMPTIONS:
             * This assumes the numbersList contents are in proper ascending order, from smallest number to largest.
             */
            var numbersInPlay = BetTypeToNumberOfNumbersInPlay(type);
            if (numbersInPlay == numbersList.Count)
            {
                if (numbersInPlay == 1 && type.Equals(VMEODRouletteBetTypes.StraightUp)) // straight up
                {
                    if (numbersList[0] < 37 || numbersList[0] == 100) // 0 to 36 OR 100 being double zero
                        return true;
                }
                else if (numbersInPlay == 2 && type.Equals(VMEODRouletteBetTypes.Split)) // split
                {
                    if (numbersList[0] == 0)
                    {
                        if (numbersList[1] == 100 || numbersList[1] == 1)
                            return true;
                    }
                    else if (numbersList[0] == 100)
                    {
                        if (numbersList[1] == 3)
                            return true;
                    }
                    else
                    {
                        if (numbersList[0] + 1 == numbersList[1] || numbersList[0] + 3 == numbersList[1]) // split with number above (+1) or adjacent (+3)
                            return true;
                    }
                }
                else if (numbersInPlay == 3 && type.Equals(VMEODRouletteBetTypes.Street)) // street
                {
                    if (numbersList[0] == 0)
                    {
                        if (numbersList[1] == 1 && numbersList[2] == 2) // 0 1 2
                            return true;
                        else if (numbersList[1] == 100 && numbersList[2] == 2) // 0 00 2
                            return true;
                    }
                    else if (numbersList[0] == 100)
                    {
                        if (numbersList[1] == 2 && numbersList[2] == 3) // 00 2 3
                            return true;
                    }
                    else
                    {
                        if (numbersList[0] + 1 == numbersList[1] && numbersList[1] + 1 == numbersList[2]) // each number increases by one in a street
                            return true;
                    }
                }
                else if (numbersInPlay == 4 && type.Equals(VMEODRouletteBetTypes.Corner)) // corner
                {
                    // x, x+1, x+3, x+4
                    if (numbersList[0] + 1 == numbersList[1] && numbersList[2] + 1 == numbersList[3] && numbersList[0] + 3 == numbersList[2])
                        return true;
                }
                else if (numbersInPlay == 5 && type.Equals(VMEODRouletteBetTypes.Sucker)) // sucker: 0 00 1 2 3
                {
                    if (numbersList[0] == 0 && numbersList[1] == 100 && numbersList[2] == 1 && numbersList[3] == 2 && numbersList[4] == 3)
                        return true;
                }
                else if (numbersInPlay == 6 && type.Equals(VMEODRouletteBetTypes.Line)) // line
                {
                    // x, x+1, x+2, x+3, x+4, x+5
                    if (numbersList[0] + 1 == numbersList[1] && numbersList[1] + 1 == numbersList[2] && numbersList[3] + 1 == numbersList[4] &&
                        numbersList[4] + 1 == numbersList[5] && numbersList[0] + 3 == numbersList[3])
                        return true;
                }
                else if (numbersInPlay == 12)
                {
                    if (type.Equals(VMEODRouletteBetTypes.Column))
                    {
                        if (numbersList[0] == 1 || numbersList[0] == 2 || numbersList[0] == 3)
                        {
                            for (int index = 0; index < numbersInPlay - 2; index++)
                            {
                                if (numbersList[index] + 3 != numbersList[index + 1]) // each number is 3 plus the prior number in column bets
                                    return false;
                            }
                            return true;
                        }
                    }
                    else if (type.Equals(VMEODRouletteBetTypes.Dozen))
                    {
                        if (numbersList[0] == 1 || numbersList[0] == 13 || numbersList[0] == 25)
                        {
                            for (int index = 0; index < numbersInPlay - 2; index++)
                            {
                                if (numbersList[index] + 1 != numbersList[index + 1]) // numbers are sequential in dozen bets
                                    return false;
                            }
                            return true;
                        }
                    }
                }
                else if (numbersInPlay == 18)
                {
                    if (type.Equals(VMEODRouletteBetTypes.Red))
                    {
                        if (CompareNumbersInPlay(numbersList, RedNumbersList))
                            return true;
                    }
                    else if (type.Equals(VMEODRouletteBetTypes.Black))
                    {
                        if (CompareNumbersInPlay(numbersList, BlackNumbersList))
                            return true;
                    }
                    else if (type.Equals(VMEODRouletteBetTypes.High))
                    {
                        if (CompareNumbersInPlay(numbersList, HighNumbersList))
                            return true;
                    }
                    else if (type.Equals(VMEODRouletteBetTypes.Low))
                    {
                        if (CompareNumbersInPlay(numbersList, LowNumbersList))
                            return true;
                    }
                    else if (type.Equals(VMEODRouletteBetTypes.Odd))
                    {
                        if (numbersList[0] == 1)
                        {
                            foreach (var number in numbersList)
                            {
                                if (number % 2 == 0)
                                    return false;
                            }
                            return true;
                        }
                    }
                    else if (type.Equals(VMEODRouletteBetTypes.Even))
                    {
                        if (numbersList[0] == 2)
                        {
                            foreach (var number in numbersList)
                            {
                                if (number % 2 == 1)
                                    return false;
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public VMEODRoulettePlugin(VMEODServer server) : base(server)
        {
            Players = new List<RoulettePlayer>();
            GameState = VMEODRouletteGameStates.Closed;
            NextState = VMEODRouletteGameStates.Invalid;
            PlaintextHandlers["roulette_UI_close"] = UIClosedHandler;
            PlaintextHandlers["roulette_new_bet"] = PlaceBetHandler;
            PlaintextHandlers["roulette_remove_bet"] = RemoveBetHandler;
            PlaintextHandlers["roulette_deposit"] = DepositHandler;
            PlaintextHandlers["roulette_withdraw"] = WithdrawHandler;
            PlaintextHandlers["roulette_new_minimum"] = NewMinimumBetHandler;
            PlaintextHandlers["roulette_new_maximum"] = NewMaximumBetHandler;
        }

        public override void OnConnection(VMEODClient client)
        {
            if (client.Avatar != null)
            {
                // args[0] is Max Bet, args[1] is Min Bet
                var args = client.Invoker.Thread.TempRegisters;
                MinBet = args[1];
                MaxBet = args[0];
                if (args[2] == 0) // a player
                {
                    var player = new RoulettePlayer(client);
                    player.OnPlayerBetChange += BroadcastBets;
                    Players.Add(player);

                    // get the amount of money the player has by sending a testOnly transaction for $1 from the table
                    var VM = player.Client.vm;

                    VM.GlobalLink.PerformTransaction(VM, true, Server.Object.PersistID, player.Client.Avatar.PersistID, 1,

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
                            TableBalance = (int)budget1;
                            player.SimoleonBalance = (int)budget2;
                            client.Send("roulette_player", new byte[] { (byte)(MinBet / 255), (byte)(MinBet % 255), (byte)(MaxBet / 255),
                                (byte)(MaxBet % 255), (byte)(player.SimoleonBalance / 255), (byte)(player.SimoleonBalance % 255) });
                            if (Croupier != null && GameState.Equals(VMEODRouletteGameStates.WaitingForPlayer))
                                EnqueueGotoState(VMEODRouletteGameStates.BettingRound);
                        }
                    });
                }
                else // croupier or owner
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
                                TableBalance = (int)budget2;
                                client.Send("roulette_manage", TableBalance + "%" + MinBet + "%" + MaxBet);
                            }
                        });
                    }
                    else // not the owner, just the croupier
                    {
                        // The cropuier's client is only used for animations. They literally have no other function.
                        Croupier = client;
                        if (Players.Count == 0)
                            EnqueueGotoState(VMEODRouletteGameStates.WaitingForPlayer);
                        else
                            EnqueueGotoState(VMEODRouletteGameStates.BettingRound);
                    }
                }
            }
            else
            {
                Controller = client;
            }
        }

        public override void OnDisconnection(VMEODClient client)
        {
            if (Croupier != null && client.Equals(Croupier))
            {
                CloseTable();
            }
            else
            {
                var playersToRemove = new List<RoulettePlayer>(Players);
                foreach (var player in playersToRemove)
                {
                    if (player.Client.Equals(client))
                    {
                        // if wheel was spinning when player disconnected, rush any payout due to them
                        if (GameState.Equals(VMEODRouletteGameStates.Spinning))
                            ProcessPayout(player);
                        Players.Remove(player);
                        break;
                    }
                }
                if (Players.Count == 0)
                    EnqueueGotoState(VMEODRouletteGameStates.WaitingForPlayer);
                else
                {
                    // if player left during betting phase, update neighbor bets of existing players
                    if (GameState.Equals(VMEODRouletteGameStates.BettingRound))
                        BroadcastBets(null);
                }
            }
            base.OnDisconnection(client);
        }

        public override void Tick()
        {
            if (Controller == null)
                return;
            if (NextState != VMEODRouletteGameStates.Invalid)
            {
                GotoState(NextState);
                NextState = VMEODRouletteGameStates.Invalid;
            }
            switch (GameState)
            {
                case VMEODRouletteGameStates.BettingRound:
                    {
                        if (Roundtimer > 0)
                        {
                            if (++Tock >= 30)
                            {
                                Roundtimer--;
                                BroadcastTime(Roundtimer);
                                Tock = 0;
                            }
                        }
                        else
                            EnqueueGotoState(VMEODRouletteGameStates.Spinning);
                        break;
                    }
                case VMEODRouletteGameStates.Spinning:
                    {
                        if (++Tock >= 360) // let's try 12 seconds
                            EnqueueGotoState(VMEODRouletteGameStates.Intermission);
                        break;
                    }
                case VMEODRouletteGameStates.Intermission:
                    {
                        if (++Tock >= 180) // let's try 6 seconds
                            EnqueueGotoState(VMEODRouletteGameStates.BettingRound);
                        break;
                    }
            }
            base.Tick();
        }

        private void RemoveBetHandler(string evt, string valueAndTypeAndNumbers, VMEODClient client)
        {
            // identify if the player
            foreach (var player in Players)
            {
                if (player.Client != null && player.Client.Equals(client))
                {
                    var betToRemove = ParseAndValidateBet(valueAndTypeAndNumbers, player);
                    if (betToRemove == null) // if the bet was not legal
                    {
                        player.BroadcastUnknownError();
                    }
                    else
                    {
                        // simantics event for removing a bet, only play if removing bet is successful
                        if (player.RemoveBet(betToRemove))
                            Controller.SendOBJEvent(new VMEODEvent((short)VMEODRouletteEvents.PushPlayerBetRemoveAnimation,
                                new short[] { player.Client.Avatar.ObjectID }));
                    }
                    return;
                }
            }
        }

        private void PlaceBetHandler(string evt, string valueAndTypeAndNumbers, VMEODClient client)
        {
            // identy if the player
            foreach (var player in Players)
            {
                if (player.Client != null && player.Client.Equals(client))
                {
                    var betToPlace = ParseAndValidateBet(valueAndTypeAndNumbers, player);
                    if (betToPlace == null) // if the bet was not legal
                    {
                        player.BroadcastUnknownError();
                    }
                    else
                    {
                        // would this bet put the player over the maximum bet for the table round?
                        if (player.CalculateTotalBets() + betToPlace.BetAmount <= MaxBet)
                        {
                            // simantics event for placing a bet, only play if placing bet is successful
                            if (player.PlaceBet(betToPlace))
                                Controller.SendOBJEvent(new VMEODEvent((short)VMEODRouletteEvents.PushPlayerBetPlaceAnimation,
                                    new short[] { player.Client.Avatar.ObjectID }));
                        }
                        else
                            player.BroadcastOverMaxBet((byte)betToPlace.BetAmount);
                    }
                    return;
                }
            }
        }

        private RouletteBet ParseAndValidateBet(string valueAndTypeAndNumbers, RoulettePlayer player)
        {
            if (valueAndTypeAndNumbers != null && player != null)
            {
                int amount = 0;
                VMEODRouletteBetTypes type;
                List<byte> numbers = new List<byte>();
                var split = valueAndTypeAndNumbers.Split('%');
                if (split.Length > 2) // has to be at LEAST: one number, one bet type, and one number (e.g. $25 bet on ST8 #3) => 25, "ST8", 3
                {
                    if (Int32.TryParse(split[0], out amount))
                    {
                        // is this a valid bet?
                        if (amount == 1 || amount == 5 || amount == 10 || amount == 25 || amount == 100)
                        {
                            // valid type?
                            string typeString = split[1];
                            if (RouletteBetTypes.ContainsValue(typeString))
                            {
                                type = RouletteBetTypes.FirstOrDefault(x => x.Value == typeString).Key;
                                byte numberToPush = 255;
                                for (int index = 2; index < split.Length; index++)
                                {
                                    if (Byte.TryParse(split[index], out numberToPush))
                                    {
                                        // valid number?
                                        if (numberToPush < 37 || numberToPush == 100) // 0-36 or 100 is double zero
                                        {
                                            numbers.Add(numberToPush);
                                        }
                                        else return null; // invalid number
                                    }
                                    else return null; // invalid data
                                }

                                // finally push the bet
                                if (numbers.Count > 0)
                                {
                                    // the final hurdle is whether or not the numbers supplied actually match the bet type within the game rules
                                    if (NumbersListIsValidForBetType(type, numbers))
                                    {
                                        return new RouletteBet(amount, type, numbers.ToArray());
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

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
                    // does the machine have enough money to cover this bet amount?
                    if (newMinBet > TableBalance * 140)
                        failureReason = VMEODRouletteInputErrorTypes.BetTooHighForBalance.ToString();
                    else
                    {
                        // valid new minimum bet
                        MinBet = newMinBet;
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODRouletteEvents.NewMinimumBet, newMinBet));
                        client.Send("roulette_min_bet_success", "" + newMinBet);
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
            client.Send("roulette_n_bet_fail", failureReason);
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
                else if (newMaxBet > GLOBAL_MAXIMUM_ROULETTE_ROUND_BET)
                    failureReason = VMEODRouletteInputErrorTypes.BetTooHigh.ToString();
                else
                {
                    // does the machine have enough money to cover this bet amount?
                    if (newMaxBet > TableBalance / 140)
                        failureReason = VMEODRouletteInputErrorTypes.BetTooHighForBalance.ToString();
                    else
                    {
                        // valid new max bet
                        MaxBet = newMaxBet;
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODRouletteEvents.NewMaximumBet, newMaxBet));
                        client.Send("roulette_max_bet_success", "" + newMaxBet);
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
            client.Send("roulette_x_bet_fail", failureReason);
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
                // attempt to credit the owner by debiting the machine
                var VM = client.vm;
                VM.GlobalLink.PerformTransaction(VM, false, Server.Object.PersistID, client.Avatar.PersistID, withdrawAmount,

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
                        TableBalance = (int)(budget1);
                        client.Send("roulette_resume_manage", TableBalance + "");
                    }
                    else
                        client.Send("roulette_withdraw_fail", VMEODSlotsInputErrorTypes.Unknown.ToString());
                });
            }
            else // otherwise, send the failureReason
            {
                if (failureReason.Length == 0)
                    failureReason = VMEODSlotsInputErrorTypes.InvalidOwner.ToString();
                client.Send("roulette_withdraw_fail", failureReason);
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
                // attempt to credit the machine by debiting the owner
                var VM = client.vm;
                VM.GlobalLink.PerformTransaction(VM, false, client.Avatar.PersistID, Server.Object.PersistID, depositAmount,

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
                        TableBalance = (int)(budget2);
                        client.Send("roulette_resume_manage", TableBalance + "");
                    }
                    else
                    {
                        // Owner does not have enough simoleons to make this deposit
                        client.Send("roulette_deposit_NSF", "" + amountString);
                    }
                });
            }
            else // otherwise, send the failureReason
            {
                if (failureReason.Length == 0)
                    failureReason = VMEODSlotsInputErrorTypes.InvalidOwner.ToString();
                client.Send("roulette_deposit_fail", failureReason);
            }
        }

        private void GotoState(VMEODRouletteGameStates state)
        {
            if (GameState.Equals(state))
                return;
            switch (state)
            {
                case VMEODRouletteGameStates.BettingRound:
                    {
                        NewGame();
                        Roundtimer = 30;
                        Tock = 0;
                        GameState = state;
                        break;
                    }
                case VMEODRouletteGameStates.Closed:
                    {
                        Tock = 0;
                        GameState = state;
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODRouletteEvents.CroupierLost));
                        var playersToRemove = new List<RoulettePlayer>(Players);
                        foreach (var player in playersToRemove)
                        {
                            if (player.Client != null)
                            {
                                Server.Disconnect(player.Client);
                                Players.Remove(player);
                            }
                        }
                        Players = new List<RoulettePlayer>();
                        break;
                    }
                case VMEODRouletteGameStates.Intermission:
                    {
                        Tock = 0;
                        GameState = state;
                        // send event for croupier to collect chips
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODRouletteEvents.CroupierCollectChips, Croupier.Avatar.ObjectID));
                        // pay the payouts
                        SettleAccounts(false);
                        break;
                    }
                case VMEODRouletteGameStates.Spinning:
                    {
                        Tock = 0;
                        GameState = state;

                        // send event for croupier to spin the wheel
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODRouletteEvents.CroupierSpinWheel, Croupier.Avatar.ObjectID));

                        int winningNumber = NextBall.Next(0, 38);
                        if (winningNumber == 37)
                            winningNumber = 100; // double zero
                        // accept the bets of each player, check if they are a winner, send them spinning event
                        foreach (var player in Players)
                        {
                            int playerBetAmount = player.CalculateTotalBets();
                            if (playerBetAmount >= MinBet)
                            {
                                playerBetAmount = player.AcceptBets();
                                DeductBetAmount(playerBetAmount, player);
                                player.IsWinner(winningNumber);
                                // send event to display bet amount taken over head
                                Controller.SendOBJEvent(new VMEODEvent((short)VMEODRouletteEvents.PlayerShowBetAmount,
                                    new short[] { player.Client.Avatar.ObjectID, (short)playerBetAmount }));
                                player.Client.Send("roulette_sound_play", "ui_object_place");
                            }
                            else
                            {
                                if (playerBetAmount > 0)
                                    player.BroadCastUnderMinBet();
                            }
                            player.Client.Send("roulette_spin", winningNumber + "%" + player.PayoutDue);
                        }
                        break;
                    }
                default:
                    {
                        Tock = 0;
                        GameState = state;
                        Roundtimer = 0;
                        break;
                    }
            }
        }
        private void UIClosedHandler(string evt, string empty, VMEODClient client)
        {
            if (client != null)
                Server.Disconnect(client);
            var playersToRemove = new List<RoulettePlayer>(Players);
            foreach (var player in playersToRemove) {
                if (player.Client != null && player.Client.Equals(client))
                    Players.Remove(player);
                break;
            }
        }
        private void NewGame()
        {
            if (TableBalance < MaxBet * 140)
                CloseTable();
            else
            {
                var playersToRemove = new List<RoulettePlayer>(Players);
                foreach (var player in playersToRemove)
                {
                    if (player.Client != null)
                    {
                        if (player.SimoleonBalance >= MinBet)
                            player.NewGame(new byte[] { (byte)(MinBet / 255), (byte)(MinBet % 255), (byte)(MaxBet / 255), (byte)(MaxBet % 255),
                        (byte)(player.SimoleonBalance / 255), (byte)(player.SimoleonBalance % 255) });
                        else
                        {
                            player.BroadcastGameoverNSF(MinBet);
                            // don't come back until you have more money
                            Server.Disconnect(player.Client);
                            Players.Remove(player);
                        }
                    }
                    else // removal of null client
                        Players.Remove(player);
                }
                if (Croupier == null || Croupier.Avatar == null || Croupier.Avatar.Dead)
                    CloseTable();
                else if (Players.Count == 0)
                    EnqueueGotoState(VMEODRouletteGameStates.WaitingForPlayer);
            }
        }

        private void BroadcastTime(int time)
        {
            foreach (var player in Players)
            {
                if (player.Client != null)
                    player.Client.Send("roulette_round_time", "" + time );
            }
        }

        private void BroadcastBets(RoulettePlayer playerExcepted)
        {
            List<RouletteBet> allNeighborBets;
            foreach (var player in Players)
            {
                allNeighborBets = new List<RouletteBet>();
                // skip the player who changed their own bets
                if (playerExcepted == null || !player.Equals(playerExcepted))
                {
                    foreach (var playerToBroadcast in Players)
                    {
                        // don't copy your own bets
                        if (!player.Equals(playerToBroadcast))
                        {
                            // enlist the bets of every other player
                            allNeighborBets.AddRange(playerToBroadcast.AllBets);
                        }
                    }
                    if (allNeighborBets.Count > 0)
                        player.BroadcastNeighborBets(allNeighborBets);
                }
            }
        }
        private void EnqueueGotoState(VMEODRouletteGameStates nextState)
        {
            NextState = nextState;
        }

        private void CloseTable()
        {
            // something went wrong, need to pay winners immediately
            if (GameState.Equals(VMEODRouletteGameStates.Spinning) && Players.Count > 0)
                SettleAccounts(true);
            EnqueueGotoState(VMEODRouletteGameStates.Closed);
        }

        private void SettleAccounts(bool skipAnimations)
        {
            foreach (var player in Players)
            {
                if (!skipAnimations && Controller != null && player.Client != null)
                {
                    if (player.PayoutDue > 0)
                    {
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODRouletteEvents.PushPlayerWinAnimation,
                            new short[] { player.Client.Avatar.ObjectID, (short)player.PayoutDue }));
                        player.Client.Send("roulette_sound_play", "ui_moneyback");
                    }
                    else if (player.PaidForBets)
                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODRouletteEvents.PushPlayerLoseAnimation,
                            new short[] { player.Client.Avatar.ObjectID }));
                }
                ProcessPayout(player);
            }
        }

        private void ProcessPayout(RoulettePlayer player)
        {
            int payout = player.PayoutDue;
            bool playerPaidForBets = player.PaidForBets;
            player.Paid(); // player.PayoutDue is now 0, no duplicate paying
            if (payout > 0 && player.Client != null && playerPaidForBets)
            {
                player.SimoleonBalance += payout; // expected balance can be used for next game in case the server delays in paying out
                TableBalance -= payout; // expected balance can be used for next game in case the server delays in paying out
                ExecutePayout(payout, player);
            }
        }

        private void DeductBetAmount(int deductionAmount, RoulettePlayer player)
        {
            if (player != null && player.Client != null)
            {
                var VM = player.Client.vm;
                // player pays object the amount of their bets
                VM.GlobalLink.PerformTransaction(VM, false, player.Client.Avatar.PersistID, Server.Object.PersistID,  deductionAmount,

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
                        TableBalance = (int)budget2;
                        player.SimoleonBalance = (int)budget1;
                        player.PaidForBets = true;
                    }
                });
            }
        }

        private void ExecutePayout(int payout, RoulettePlayer player)
        {
            if (player != null && player.Client != null)
            {
                var VM = player.Client.vm;

                VM.GlobalLink.PerformTransaction(VM, false, Server.Object.PersistID, player.Client.Avatar.PersistID, payout,

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
                        TableBalance = (int)budget1;
                        player.SimoleonBalance = (int)budget2;
                    }
                });
            }
        }
    }

    internal class RoulettePlayer
    {
        private VMEODClient _Client;
        private List<RouletteBet> _AllBets;
        private int _SimoleonBalance;
        private int _PayoutDue;
        private bool _PaidForBets;

        internal delegate void BetChange(RoulettePlayer player);

        internal RoulettePlayer(VMEODClient client)
        {
            _Client = client;
            _AllBets = new List<RouletteBet>();
        }

        internal VMEODClient Client
        {
            get { return _Client; }
        }

        internal List<RouletteBet> AllBets
        {
            get { return _AllBets; }
        }

        internal int SimoleonBalance
        {
            get { return _SimoleonBalance; }
            set { _SimoleonBalance = value; }
        }

        internal int PayoutDue
        {
            get { return _PayoutDue; }
        }

        internal bool PaidForBets
        {
            get { return _PaidForBets; }
            set { _PaidForBets = value; }
        }

        internal event BetChange OnPlayerBetChange;

        internal bool PlaceBet(RouletteBet newBet)
        {
            // can I afford this bet?
            if (CalculateTotalBets() + newBet.BetAmount <= _SimoleonBalance)
            {
                // make sure a bet doesn't already exit of this type
                foreach (var bet in _AllBets)
                {
                    // if a bet of this type already exists, are the numbers the same?
                    if (bet.Type.Equals(newBet.Type))
                    {
                        // if the numbers are the same, attempt to increment the bet
                        if (VMEODRoulettePlugin.CompareNumbersInPlay(bet.NumbersInPlay, newBet.NumbersInPlay))
                        {
                            // if the chip type count for this bet is not already max
                            if (bet.Increment(newBet.BetAmount))
                            {
                                OnPlayerBetChange(this);
                                return true;
                            }
                            else // can't put another chip on this stack
                            {
                                BroadcastChipOverflow((byte)newBet.BetAmount);
                                return false;
                            }
                        }
                    }
                }
                // exact bet wasn't found, so make a new bet
                _AllBets.Add(newBet);
                OnPlayerBetChange(this);
                return true;
            }
            // no I cannot afford this bet
            else
                BroadcastNSF();
            return false;
        }

        internal bool RemoveBet(RouletteBet allegedExistingBet)
        {
            if (_AllBets.Count == 0) return false;
            foreach (var bet in _AllBets)
            {
                // if a bet of this type already exists, are the numbers the same?
                if (bet.Type.Equals(allegedExistingBet.Type))
                {
                    // if the numbers are the same, attempt to decrement the bet
                    if (VMEODRoulettePlugin.CompareNumbersInPlay(bet.NumbersInPlay, allegedExistingBet.NumbersInPlay))
                    {
                        // could not decrement the bet any lower, so the type must be removed entirely
                        if (!bet.Decrement(allegedExistingBet.BetAmount))
                        {
                            if (bet.BetAmount == allegedExistingBet.BetAmount)
                                _AllBets.Remove(bet);
                            else // mismatch on the amount of bet, so the bet stays. this should never happen
                                return false;
                        }
                        OnPlayerBetChange(this);
                        return true;
                    }
                }
            }
            // bet of that type was not found
            return false;
        }

        internal int AcceptBets()
        {
            int totalBetsCost = CalculateTotalBets();
            if (totalBetsCost <= _SimoleonBalance)
            {
                _SimoleonBalance -= totalBetsCost; // estimation only: but will be updated after actual transaction
                return totalBetsCost;
            }
            else // cannot afford the bets placed
            {
                return -1;
            }
        }

        internal int CalculateTotalBets()
        {
            int totalBetsCost = 0;
            foreach (var bet in _AllBets)
            {
                totalBetsCost += bet.BetAmount;
            }
            return totalBetsCost;
        }

        internal bool IsWinner(int winningNumber)
        {
            int payoutAmount = 0;
            foreach (var bet in _AllBets)
            {
                foreach (var numberInPlay in bet.NumbersInPlay)
                {
                    if (numberInPlay == winningNumber)
                    {
                        // payout ratio times bet amount plus the original bet amount back
                        payoutAmount += bet.BetAmount * VMEODRoulettePlugin.BetTypeToPayoutRatio(bet.Type) + bet.BetAmount;
                        break; // done with this bet, but multiple bets could mean multiple wins
                    }
                }
            }
            _PayoutDue = payoutAmount;
            if (_PayoutDue > 0)
                return true;
            return false;
        }

        internal void Paid()
        {
            _PayoutDue = 0;
            _PaidForBets = false;
        }

        internal void NewGame(byte[] msgData)
        {
            Paid();
            _AllBets = new List<RouletteBet>();
            if (Client != null)
                Client.Send("roulette_new_game", msgData);
        }
        internal void SyncMyBets()
        {
            if (Client != null)
            {
                // number of bets % bet amount % bet Type string % bet % number0 % number1... % bet amount % bet Type string % bet % number0 % number1... etc.
                int totalUniqueBets = 0;
                int grandTotalOfBets = 0;
                string betDataString;
                string allBetsDataString = "";
                string betTypeString = "";
                List<int> betDenoms = new List<int>();
                for (totalUniqueBets = 0; totalUniqueBets < _AllBets.Count; totalUniqueBets++)
                {
                    betDataString = "";
                    betTypeString = VMEODRoulettePlugin.RouletteBetTypes[_AllBets[totalUniqueBets].Type];
                    betDenoms = _AllBets[totalUniqueBets].BetDenominations;
                    foreach (int chip in betDenoms)
                    {
                        betDataString += "%" + chip + "%" + betTypeString;
                        foreach (var number in _AllBets[totalUniqueBets].NumbersInPlay)
                            betDataString += "%" + number;
                        grandTotalOfBets++;
                    }
                    allBetsDataString += betDataString;
                }
                if (allBetsDataString.Length == 0)
                    allBetsDataString = "%0";
                // remove the very first % when sending
                Client.Send("roulette_sync_mine", grandTotalOfBets + "%" + allBetsDataString.Remove(0, 1));
            }
        }
        internal void BroadcastNeighborBets(List<RouletteBet> neighborBets)
        {
            if (Client != null)
            {
                // number of bets % bet amount % bet Type string % bet % number0 % number1... % bet amount % bet Type string % bet % number0 % number1... etc.
                int totalUniqueBets = 0;
                int grandTotalOfBets = 0;
                string betDataString;
                string allBetsDataString = "";
                string betTypeString = "";
                List<int> betDenoms = new List<int>();
                for (totalUniqueBets = 0; totalUniqueBets < neighborBets.Count; totalUniqueBets++)
                {
                    betDataString = "";
                    betTypeString = VMEODRoulettePlugin.RouletteBetTypes[neighborBets[totalUniqueBets].Type];
                    betDenoms = neighborBets[totalUniqueBets].BetDenominations;
                    foreach (int chip in betDenoms)
                    {
                        betDataString += "%" + chip + "%" + betTypeString;
                        foreach (var number in neighborBets[totalUniqueBets].NumbersInPlay)
                            betDataString += "%" + number;
                        grandTotalOfBets++;
                    }
                    allBetsDataString += betDataString;
                }
                // remove the very first % when sending
                Client.Send("roulette_sync_neighbor", grandTotalOfBets + "%" + allBetsDataString.Remove(0, 1));
            }
        }
        internal void BroadcastOverMaxBet(byte sentBet)
        {
            if (Client != null)
                Client.Send("roulette_over_max", new byte[] { sentBet });
            SyncMyBets();
        }
        internal void BroadcastChipOverflow(byte failedBet)
        {
            if (Client != null)
                Client.Send("roulette_stack_overflow", new byte[] { failedBet });
            SyncMyBets();
        }
        internal void BroadcastNSF()
        {
            if (Client != null)
                Client.Send("roulette_bet_failed", "" + _SimoleonBalance);
            SyncMyBets();
        }
        internal void BroadcastGameoverNSF(int minBet)
        {
            Paid();
            _AllBets = new List<RouletteBet>();
            if (Client != null)
                Client.Send("roulette_gameoverNSF", new byte[] { (byte)(minBet / 255), (byte)(minBet % 255) });
        }
        internal void BroadcastUnknownError()
        {
            if (Client != null)
                Client.Send("roulette_unknown_error", "");
            SyncMyBets();
        }
        internal void BroadCastUnderMinBet()
        {
            if (Client != null)
                Client.Send("roulette_under_min", "" + CalculateTotalBets());
            _AllBets = new List<RouletteBet>();
            SyncMyBets();
        }
    }

    internal class RouletteBet
    {
        private VMEODRouletteBetTypes _Type;
        private int[] BetChips;
        private int BetChipCount;
        internal List<byte> NumbersInPlay;

        internal RouletteBet(int amount, VMEODRouletteBetTypes type, params byte[] numbers)
        {
            BetChips = new int[5];
            if (numbers != null && numbers.Length != 0)
            {
                NumbersInPlay = new List<byte>(numbers);
                _Type = type;
                if (amount == 100)
                    BetChips[4]++;
                else if (amount == 25)
                    BetChips[3]++;
                else if (amount == 10)
                    BetChips[2]++;
                else if (amount == 5)
                    BetChips[1]++;
                else
                    BetChips[0]++;
                BetChipCount++;
            }
        }

        internal List<int> BetDenominations
        {
            get
            {
                List<int> BetDenomList = new List<int>();
                for (int frequency = 0; frequency < BetChips[4]; frequency++)
                    BetDenomList.Add(100);
                for (int frequency = 0; frequency < BetChips[3]; frequency++)
                    BetDenomList.Add(25);
                for (int frequency = 0; frequency < BetChips[2]; frequency++)
                    BetDenomList.Add(10);
                for (int frequency = 0; frequency < BetChips[1]; frequency++)
                    BetDenomList.Add(5);
                for (int frequency = 0; frequency < BetChips[0]; frequency++)
                    BetDenomList.Add(1);
                return BetDenomList;
            }
        }

        internal int BetAmount
        {
            get
            {
                int total = 0;
                total += 100 * BetChips[4];
                total += 25 * BetChips[3];
                total += 10 * BetChips[2];
                total += 5 * BetChips[1];
                total += 1 * BetChips[0];
                return total;
            }
        }

        internal VMEODRouletteBetTypes Type
        {
            get { return _Type; }
        }

        internal bool Increment(int amount)
        {
            if (BetChipCount < VMEODRoulettePlugin.GLOBAL_MAXIMUM_CHIPS_PER_STACK)
            {
                if (amount == 100)
                    BetChips[4]++;
                else if (amount == 25)
                    BetChips[3]++;
                else if (amount == 10)
                    BetChips[2]++;
                else if (amount == 5)
                    BetChips[1]++;
                else if (amount == 1)
                    BetChips[0]++;
                else
                    return false; // invalid amount (should never happen)
                BetChipCount++;
                return true;
            }
            // can't put more chips on the stack
            return false;
        }

        internal bool Decrement(int amount)
        {
            if (BetChipCount > 1)
            {
                if (amount == 100)
                {
                    if (BetChips[4] > 0)
                        BetChips[4]--;
                    else // can't remove: amount mismatch (should never happen)
                        return false;
                }
                else if (amount == 25)
                {
                    if (BetChips[3] > 0)
                        BetChips[3]--;
                    else // can't remove: amount mismatch (should never happen)
                        return false;
                }
                else if (amount == 10)
                {
                    if (BetChips[2] > 0)
                        BetChips[2]--;
                    else // can't remove: amount mismatch (should never happen)
                        return false;
                }
                else if (amount == 5)
                {
                    if (BetChips[1] > 0)
                        BetChips[1]--;
                    else // can't remove: amount mismatch (should never happen)
                        return false;
                }
                else if (amount == 1)
                {
                    if (BetChips[0] > 0)
                        BetChips[0]--;
                    else // can't remove: amount mismatch (should never happen)
                        return false;
                }
                else
                    return false; // invalid amount (should never happen)
                BetChipCount--;
                return true;
            }
            // can't remove last chip
            return false;
        }
    }

    public enum VMEODRouletteEvents : short
    {
        VMEODDisconnect = -2,
        VMEODConnect = -1,
        CroupierSpinWheel = 1,
        CroupierCollectChips = 2,
        CroupierLost = 3,
        PushPlayerBetPlaceAnimation = 4, // matching player's Client.Avatar.PersistID sent as temp0
        PushPlayerBetRemoveAnimation = 5, // matching player's Client.Avatar.PersistID sent as temp0
        PushPlayerWinAnimation = 6, // matching player's Client.Avatar.PersistID sent as temp0
        PushPlayerLoseAnimation = 7, // matching player's Client.Avatar.PersistID sent as temp0
        NewMinimumBet = 8, // sent as temp0
        NewMaximumBet = 9, // sent as temp0
        PlayerShowBetAmount = 10, // player as temp0, betAmount as temp1
    }

    public enum VMEODRouletteGameStates : byte
    {
        Closed = 0,
        WaitingForPlayer = 1,
        BettingRound = 2,
        Spinning = 3,
        Intermission = 4,
        Invalid = 255
    }

    public enum VMEODRouletteBetTypes
    {
        StraightUp,
        Split,
        Street,
        Corner,
        Sucker,
        Line,
        Dozen,
        Column,
        Odd,
        Even,
        Low,
        High,
        Red,
        Black
    }
    [Flags]
    public enum VMEODRouletteInputErrorTypes : byte
    {
        BetTooLow = 0,
        BetTooHigh = 1,
        BetTooHighForBalance = 2,
        ObjectMustBeClosed = 3
    }
}
