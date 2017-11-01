using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Client.UI.Model;
using FSO.Client.UI.Panels.EODs.Utils;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using System.Timers;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels.EODs
{
    class UIRouletteEOD : UIEOD
    {
        /*
         * Chips and Betting
         */
        private readonly int[] BlackNumbersArray = { 2, 4, 6, 8, 10, 11, 13, 15, 17, 20, 22, 24, 26, 28, 29, 31, 33, 35 };
        private readonly int[] RedNumbersArray = { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };
        private List<ChipStack> MyChipsInPlay = new List<ChipStack>();
        private List<ChipStack> NeighborChipsInPlay = new List<ChipStack>(); // other players' chips
        private UIRouletteEODStates GameState = UIRouletteEODStates.Initializing;
        private int MinBet;
        private int MaxBet;
        private int PlayerBalance;
        private int RoundTimeRemaining;
        private int CurrentRoundTotalBets;
        // boxes for shading
        private UIHighlightSprite DoubleZeroBox;
        private UIHighlightSprite ZeroBox;
        private ShadowBox[,] ShadowBoxArray;
        // images
        private PlayChip CurrentDraggingChip;
        private UISlotsImage FakeChipBtn1;
        private UISlotsImage FakeChipBtn2;
        private UISlotsImage FakeChipBtn3;
        private UISlotsImage FakeChipBtn4;
        private UISlotsImage FakeChipBtn5;
        // textures
        public Texture2D imageChip_001 { get; set; } // draggable chip sprite
        public Texture2D imageChip_005 { get; set; }
        public Texture2D imageChip_010 { get; set; }
        public Texture2D imageChip_025 { get; set; }
        public Texture2D imageChip_100 { get; set; }
        public Texture2D imageChip_001_Btn { get; set; }
        public Texture2D imageChip_005_Btn { get; set; }
        public Texture2D imageChip_010_Btn { get; set; }
        public Texture2D imageChip_025_Btn { get; set; }
        public Texture2D imageChip_100_Btn { get; set; }
        // buttons
        public UIButton btnChip1 { get; set; } // §1 *thanks for keeping it consistent, Maxis asset designer! /s
        public UIButton btnChip2 { get; set; } // §5
        public UIButton btnChip3 { get; set; } // §10
        public UIButton btnChip4 { get; set; } // §25
        public UIButton btnChip5 { get; set; } // §100
        private UIButton[] btnChipsArray;
        // constants for easy access/tweaking/customization or future updates
        public const int CHIP_TGA_HEIGHT = 25;
        public const int CHIP_TGA_WIDTH = 26;
        public const int NUMBER_SPACE_HEIGHT = 20;
        public const int NUMBER_SPACE_WIDTH = 21;
        public const int LOW_RETURN_SPACE_HEIGHT = 17;
        public const int DRAG_CHIP_RADIUS = 10;
        public const int DRAG_CHIP_CENTER_X = 10;
        public const int DRAG_CHIP_CENTER_Y = 10;
        /*
         * General EOD
         */
        private UIScript Script;
        private UIManageEODObjectPanel OwnerPanel;
        private UIMouseEventRef RouletteGraphMouseHandler;
        private Timer WheelSpinTimer = new Timer(333);
        private int WheelSpinElapsedCounter = 0;
        // Text fields
        public UILabel labelNumber { get; set; }
        public UILabel labelTotalBets { get; set; }
        private string ResultsString = "";
        // images
        private UIImage RouletteTable;
        private UIImage ScriptedRouletteWheel;
        private UISlotsImage RouletteWheel;
        private UIImage RouletteBall;
        private RouletteWheelStateList WheelStateList;
        private RouletteWheelStateNode[] UltimateStopsArray = new RouletteWheelStateNode[3];
        private byte UltimateStopColor;
        // buttons
        public UIButton btnShowAllBets { get; set; }

        public UIRouletteEOD(UIEODController controller) : base(controller)
        {
            InitUI();
            // player
            BinaryHandlers["roulette_new_game"] = NewGameHandler;
            BinaryHandlers["roulette_player"] = ShowPlayerUIHandler;
            BinaryHandlers["roulette_stack_overflow"] = ChipStackOverFlowHandler;
            BinaryHandlers["roulette_gameoverNSF"] = GameOverNSFHandler;
            BinaryHandlers["roulette_over_max"] = OverMaxBetHandler;
            PlaintextHandlers["roulette_bet_failed"] = NSFBetFailedHandler;
            PlaintextHandlers["roulette_sound_play"] = SoundEffectHandler;
            PlaintextHandlers["roulette_spin"] = SpinHandler;
            PlaintextHandlers["roulette_sync_mine"] = MyBetsSyncHandler;
            PlaintextHandlers["roulette_sync_neighbor"] = NeighborBetsSyncHandler;
            PlaintextHandlers["roulette_round_time"] = TimeHandler;
            PlaintextHandlers["roulette_under_min"] = UnderMinBetHandler;
            PlaintextHandlers["roulette_unknown_error"] = UnknownErrorHandler;
            // owner
            PlaintextHandlers["roulette_deposit_NSF"] = DepositFailHandler;
            PlaintextHandlers["roulette_deposit_fail"] = InputFailHandler;
            PlaintextHandlers["roulette_withdraw_fail"] = InputFailHandler;
            PlaintextHandlers["roulette_x_bet_fail"] = InputFailHandler; // max bet fail
            PlaintextHandlers["roulette_n_bet_fail"] = InputFailHandler; // min bet fail
            PlaintextHandlers["roulette_max_bet_success"] = ResumeFromBetAmountHandler;
            PlaintextHandlers["roulette_min_bet_success"] = ResumeFromBetAmountHandler;
            PlaintextHandlers["roulette_resume_manage"] = ResumeManageHandler;
            PlaintextHandlers["roulette_manage"] = ShowManageUIHandler;
        }
        /*
         * The "magic" of the dragging chip happens here. If in dragging state, put the currently dragging chip at the position of the mouse. THEN check
         * where the mouse is currently, determining if it is hovering over a position representing a valid bet on the table. If so, make all numbers NOT
         * represented in this valid bet to have a slight shade by making visible a mostly transparent black box image on their position. This makes the
         * numbers that ARE represented in this valid bet to stand out and be 'highlighted' by virtue of not being shaded. Finally, set the EOD Tip to
         * display the bet type and the payout ratio. Note: The user must be dragging the chip within the boundaries of the listener rectangle.
         */
        public override void Update(UpdateState state)
        {
            if (GameState.Equals(UIRouletteEODStates.Dragging))
            {
                Parent.Invalidate();
                if (CurrentDraggingChip != null)
                {
                    // move the dragged chip to mouse location
                    var mousePosition = GetMousePosition(state.MouseState);
                    // add half the image width and height to the position to make it centered on the mouse position    
                    var perceivedCoords = mousePosition - new Vector2(9, 9);
                    CurrentDraggingChip.ChipImage.Position = perceivedCoords;

                    // using the number list, set the tool tip
                    var array = VectorToNumbersList(mousePosition);
                    if (array != null)
                    {
                        var numberOfNumbers = array.Length;
                        if (numberOfNumbers == 0)
                            SetTip(GameFacade.Strings["UIText", "258", "22"].Replace("%d", MinBet + "")
                                + " " + GameFacade.Strings["UIText", "258", "21"].Replace("%d", MaxBet + ""));
                        else if (numberOfNumbers == 1)
                            SetTip(GameFacade.Strings["UIText", "258", "9"]);
                        else if (numberOfNumbers == 2)
                            SetTip(GameFacade.Strings["UIText", "258", "10"]);
                        else if (numberOfNumbers == 3)
                            SetTip(GameFacade.Strings["UIText", "258", "11"]);
                        else if (numberOfNumbers == 4)
                            SetTip(GameFacade.Strings["UIText", "258", "12"]);
                        else if (numberOfNumbers == 5)
                            SetTip(GameFacade.Strings["UIText", "258", "13"]);
                        else if (numberOfNumbers == 6)
                            SetTip(GameFacade.Strings["UIText", "258", "32"]);
                        else if (numberOfNumbers == 12)
                            SetTip(GameFacade.Strings["UIText", "258", "14"]);
                        else if (numberOfNumbers == 18)
                            SetTip(GameFacade.Strings["UIText", "258", "15"]);
                    }
                    else
                        SetTip(GameFacade.Strings["UIText", "258", "22"].Replace("%d", MinBet + "")
                                + " " + GameFacade.Strings["UIText", "258", "21"].Replace("%d", MaxBet + ""));

                    // using mouse location, determine which shadows to display or not
                    ThrowSomeShade(array);
                }
            }
            base.Update(state);
        }
        // player has closed the UI
        public override void OnClose()
        {
            SetTip("");
            Send("roulette_UI_close", "");
            base.OnClose();
        }
        // Server sends EOD time remaining as a string. Ignore this if in Managing State.
        private void TimeHandler(string evt, string timeString)
        {
            if (!GameState.Equals(UIRouletteEODStates.Managing))
            {
                RoundTimeRemaining = 0;
                int.TryParse(timeString, out RoundTimeRemaining);
                SetTime(RoundTimeRemaining);
                // goto spinning if not already there
                if (RoundTimeRemaining == 0)
                {
                    if (!GameState.Equals(UIRouletteEODStates.Spinning) && !GameState.Equals(UIRouletteEODStates.Results))
                        GotoState(UIRouletteEODStates.Spinning);
                }
                // goto idle (betting) if not already there
                else
                {
                    if (GameState.Equals(UIRouletteEODStates.Initializing) || GameState.Equals(UIRouletteEODStates.Results))
                        GotoState(UIRouletteEODStates.Idle);
                }
            }
        }
        /* After 333 milliseconds elapses 9 times, try to stop the roulette wheel from spinning by checking if the next spin is the correct stop that
         * matches the winning number's color sent from the server and saved in this.UltimateStopColor:byte - 0 for black, 1 for red, 2 for green
         */
        private void WheelSpinTimerHandler(object source, ElapsedEventArgs args)
        {
            var newX = 0;
            // ~10 seconds for now, as a guess
            if (++WheelSpinElapsedCounter >= 30)
            {
                var targetX = UltimateStopsArray[UltimateStopColor].X;
                newX = WheelStateList.Advance(targetX);
                if (newX == targetX)
                {
                    WheelSpinTimer.Stop();
                    GotoState(UIRouletteEODStates.Results);
                }
            }
            else
                newX = WheelStateList.Advance();
            RouletteWheel.SetBounds(newX, 0, 94, 40);
        }
        /*
         * This UI is just for players. Since a game might already be running, SetTip will say "Wait to bet" and the player won't be able to
         * bet until the server event is handled by TimeHandler, wherein the state will change to Idle aka betting.
         */
        private void ShowPlayerUIHandler(string evt, byte[] minMaxBetPlayerBalance)
        {
            if (NewGame(minMaxBetPlayerBalance))
            {
                RecoverButtonRefs();
                CreateImages();
                Add(btnChip1);
                Add(btnChip2);
                Add(btnChip3);
                Add(btnChip4);
                Add(btnChip5);
                Add(btnShowAllBets);
                UpdateTotalBets();
                AddListeners();
                Controller.ShowEODMode(new EODLiveModeOpt
                {
                    Buttons = 1,
                    Height = EODHeight.Tall,
                    Length = EODLength.Full,
                    Tips = EODTextTips.Short,
                    Timer = EODTimer.Normal,
                    Expandable = false
                });
                DisableChipButtons();
                SetTime(0);
                SetTip(GameFacade.Strings["UIText", "258", "17"]); // "Wait to bet";
            }
            else
            {
                GameOverNSF();
            }
        }
        /*
         * The UI for the owner
         */
        private void ShowManageUIHandler(string evt, string balanceMinMaxBet)
        {
            if (balanceMinMaxBet == null)
                return;
            int tempBalance;
            int tempMinBet;
            int tempMaxBet;
            var split = balanceMinMaxBet.Split('%');
            if (split.Length > 2)
            {
                if (Int32.TryParse(split[0], out tempBalance) & Int32.TryParse(split[1], out tempMinBet) & Int32.TryParse(split[2], out tempMaxBet))
                {
                    OwnerPanel = new UIManageEODObjectPanel(
                        ManageEODObjectTypes.Roulette, tempBalance, tempMaxBet * 140, 999999, tempMinBet, tempMaxBet);
                    Add(OwnerPanel);
                    // subscribe in order to send events based on type
                    OwnerPanel.OnNewByteMessage += SendByteMessage;
                    OwnerPanel.OnNewStringMessage += SendStringMessage;

                    Controller.ShowEODMode(new EODLiveModeOpt
                    {
                        Buttons = 0,
                        Height = EODHeight.Normal,
                        Length = EODLength.Full,
                        Tips = EODTextTips.Short,
                        Timer = EODTimer.None,
                        Expandable = false
                    });

                    SetTip(GameFacade.Strings["UIText", "259", "22"]); // "Closed for Maintenance"
                }
            }
        }
        private void SendByteMessage(EODMessageNode node)
        {
            Send("roulette_" + node.EventName, node.EventByteData);
        }
        private void SendStringMessage(EODMessageNode node)
        {
            Send("roulette_" + node.EventName, node.EventStringData);
        }
        private void ResumeManageHandler(string evt, string message)
        {
            if (OwnerPanel != null)
            {
                OwnerPanel.ResumeFromMachineBalance(evt, message);
            }
        }
        private void ResumeFromBetAmountHandler(string evt, string minOrMaxBetString)
        {
            if (OwnerPanel != null)
            {
                OwnerPanel.ResumeFromBetAmount(evt.Remove(0, 9), minOrMaxBetString); // truncate "roulette_"
            }
        }
        private void DepositFailHandler(string evt, string message)
        {
            if (OwnerPanel != null)
            {
                OwnerPanel.DepositFailHandler(evt, message);
            }
        }
        private void InputFailHandler(string evt, string message)
        {
            if (OwnerPanel != null)
            {
                OwnerPanel.InputFailHandler(evt.Remove(0, 9), message); // truncate "roulette_"
            }
        }
        private void ChipStackOverFlowHandler(string evt, byte[] attemptedBet)
        {
            // show an alert that tells the user that they can't have that many chips on the stack
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = "Betting Error",
                Message = "You can't place another chip onto this stack.",
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    UIScreen.RemoveDialog(alert);
                }),
            }, true);
        }
        private void GameOverNSFHandler(string evt, byte[] minBet)
        {
            GameOverNSF();
            int? sentMinBet;
            if (minBet != null && minBet.Length > 1)
                sentMinBet = minBet[0] * 255 + minBet[1];
            else
                sentMinBet = null;

            // show an alert that tells the user that they can't afford the minimum bet
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = "Game Over",
                Message = "You don't have enough money to meet the minimum bet of $" + ((sentMinBet != null) ? "" + sentMinBet : "" + MinBet) + ".",
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    UIScreen.RemoveDialog(alert);
                }),
            }, true);
        }
        private void OverMaxBetHandler(string evt, byte[] sentAttemptedBet)
        {
            int? attemptedBet;
            if (sentAttemptedBet != null && sentAttemptedBet.Length > 0)
                attemptedBet = sentAttemptedBet[0];
            else
                attemptedBet = null;

            // show an alert that tells the user that they can't afford the minimum bet
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = "Betting Error",
                Message = GameFacade.Strings["UIText", "258", "25"].Replace("$%d",
                        ((attemptedBet != null) ? "$" + attemptedBet + " of" : "That/Those")) +".",
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    UIScreen.RemoveDialog(alert);
                }),
            }, true);
        }
        private void NSFBetFailedHandler(string evt, string playerBalance)
        {
            // show an alert that tells the user that they can't afford their most recent bet
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = "Betting Error",
                Message = "You don't have enough money to place that bet.",
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    UIScreen.RemoveDialog(alert);
                }),
            }, true);

            // update the player balance so the UI doesn't let this happen again
            int balance;
            if (playerBalance != null && Int32.TryParse(playerBalance, out balance))
                PlayerBalance = balance;
        }
        private void UnknownErrorHandler(string evt, string uselessString)
        {
            // show an alert that tells the user that an unknown error prevented them from making their last bet
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = "Betting Error",
                Message = "An unknown error occured. Your last bet was not accepted.",
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    UIScreen.RemoveDialog(alert);
                }),
            }, true);
        }
        private void UnderMinBetHandler(string evt, string lowBet)
        {
            // show an alert that tells the user that they did not meet the table's minimum bet
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = "Betting Error",
                Message = "Your bet of $" + lowBet + " did not meet the table's minimum bet of $" + MinBet + ".",
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    UIScreen.RemoveDialog(alert);
                }),
            }, true);
        }
        private void SoundEffectHandler(string evt, string soundString)
        {
            HIT.HITVM.Get().PlaySoundEvent(soundString);
            HIT.HITVM.Get().PlaySoundEvent(soundString);
        }
        /*
         * Checking to make sure that the user has enough money to keep playing makes the UI look clean by disallowing them to interact with the bet
         * buttons. Obviously the server actually decides if they have enough money to keep playing. It also makes sure they can cover any bets.
         */
        private void NewGameHandler(string evt, byte[] minMaxBetPlayerBalance)
        {
            if (!NewGame(minMaxBetPlayerBalance))
            {
                GameOverNSF();
            }
            else
            {
                DisposeOfChips(true, true);
                RoundTimeRemaining = 30;
                GotoState(UIRouletteEODStates.Idle);
            }
        }
        /*
         * The server sends the winning number and this player's winnings in a string separated by a '%' due to byte constraints possibly limiting winnings
         */
        private void SpinHandler(string evt, string winningNumberPercentWinnings)
        {
            var split = winningNumberPercentWinnings.Split('%');
            int winningNumber = -1;
            int winnings = 0;
            if (split.Length > 1 && Int32.TryParse(split[0], out winningNumber) && Int32.TryParse(split[1], out winnings))
            {
                if (winningNumber == 0 || winningNumber == 100) // green number
                {
                    UltimateStopColor = 2;
                    RouletteBall.X = 74;
                }
                else if (BlackNumbersArray.Contains(winningNumber)) // black number
                {
                    UltimateStopColor = 0;
                    RouletteBall.X = 77; // grahpical curiosity
                }

                else // red number
                {
                    UltimateStopColor = 1;
                    RouletteBall.X = 74;
                }

                // update the label with the winning number
                labelNumber.Visible = false;
                labelNumber.Caption = ((winningNumber == 100) ? "00" : "" + winningNumber);

                /*
                 * Prepare the string for SetTip once the wheel stops spinning. NOTE: If the player wins ANYTHING, the win is displayed, regardless if the
                 * winnings is less than the total amount bet in the round. The loss message is only displayed if the user did not win anything from any bet.
                 */
                if (winnings > 0)
                    ResultsString = GameFacade.Strings["UIText", "258", "6"].Replace("%d", "" + winnings); // "You win $%d!"
                else
                {
                    if (CurrentRoundTotalBets > 0)
                        ResultsString = GameFacade.Strings["UIText", "258", "24"].Replace("%d", "" + CurrentRoundTotalBets); // "You lost $%d."
                    else
                        ResultsString = GameFacade.Strings["UIText", "258", "17"]; // "Wait to bet"
                }

                // start the timer for the spinning animation
                WheelSpinTimer.Start();
                // failsafe
                if (!GameState.Equals(UIRouletteEODStates.Spinning))
                    GotoState(UIRouletteEODStates.Spinning);
            }
        }
        /*
         * Each of the next 5 methods are for each of the 5 buttons, allowing the user to pick up a chip texture matching the button texture
         */
        private void ButtonChip1ClickedHandler(UIElement btn)
        {
            if (GameState.Equals(UIRouletteEODStates.Idle))
            {
                PickUpChip(1, btnChip1, imageChip_001);
                GotoState(UIRouletteEODStates.Dragging);
            }
            else
            {
                DropChip();
                ResumeBetting();
            }
        }
        private void ButtonChip2ClickedHandler(UIElement btn)
        {
            if (GameState.Equals(UIRouletteEODStates.Idle))
            {
                PickUpChip(5, btnChip2, imageChip_005);
                GotoState(UIRouletteEODStates.Dragging);
            }
            else
            {
                DropChip();
                ResumeBetting();
            }
        }
        private void ButtonChip3ClickedHandler(UIElement btn)
        {
            if (GameState.Equals(UIRouletteEODStates.Idle))
            {
                PickUpChip(10, btnChip3, imageChip_010);
                GotoState(UIRouletteEODStates.Dragging);
            }
            else
            {
                DropChip();
                ResumeBetting();
            }
        }
        private void ButtonChip4ClickedHandler(UIElement btn)
        {
            if (GameState.Equals(UIRouletteEODStates.Idle))
            {
                PickUpChip(25, btnChip4, imageChip_025);
                GotoState(UIRouletteEODStates.Dragging);
            }
            else
            {
                DropChip();
                ResumeBetting();
            }
        }
        private void ButtonChip5ClickedHandler(UIElement btn)
        {
            if (GameState.Equals(UIRouletteEODStates.Idle))
            {
                PickUpChip(100, btnChip5, imageChip_100);
                GotoState(UIRouletteEODStates.Dragging);
            }
            else
            {
                DropChip();
                ResumeBetting();
            }
        }
        /*
         * This event listens for the user's mouse to leave the table area, or for a click somewhere on it. The Vector2 of the click is sent to determine the
         * bet type that the user most likely is attempting to make.
         */
        private void OnTableMouseEvent(UIMouseEventType type, UpdateState update)
        {
            if (GameState.Equals(UIRouletteEODStates.Dragging))
            {
                switch (type)
                {
                    // out of bounds, return the chip
                    case UIMouseEventType.MouseOut:
                        {
                            DropChip();
                            ResumeBetting();
                            break;
                        }
                    // user clicks, evaluate where this should place the chip
                    case UIMouseEventType.MouseUp:
                        {
                            PlaceBet(GetMousePosition(update.MouseState));
                            break;
                        }
                }
            }
            else if (GameState.Equals(UIRouletteEODStates.Idle))
            {
                if (type.Equals(UIMouseEventType.MouseUp)) // user clicks somewhere during the betting period while NOT dragging a chip
                {
                    Vector2 allegedStackLocation = new Vector2();
                    int[] numbersList = new int[0];
                    try
                    {
                        numbersList = VectorToNumbersList(GetMousePosition(update.MouseState));
                        allegedStackLocation = NumbersListToActualVector(numbersList);
                    }
                    catch (Exception e)
                    {
                        // I got an occasional random null, but couldn't pinpoint where and why.
                    }

                    // try to find a stack at the location
                    foreach (var stack in MyChipsInPlay)
                    {
                        if (stack != null && stack.Position == allegedStackLocation)
                        {
                            // take it off the stack
                            var chip = stack.Pop();
                            // remove from UI
                            if (chip != null)
                            {
                                try
                                {
                                    Remove(chip.ChipImage);
                                    chip.Dispose();
                                }
                                catch (Exception e)
                                {
                                    chip.ChipImage.Visible = false;
                                }
                                // send the bet to the server to be removed
                                string typeString = "";
                                if (VMEODRoulettePlugin.RouletteBetTypes.TryGetValue(stack.BetType, out typeString) && numbersList.Length > 0)
                                {
                                    string numbersString = "";
                                    foreach (var number in numbersList)
                                        numbersString += "%" + number.ToString();
                                    Send("roulette_remove_bet", chip.Value + "%" + typeString + numbersString);
                                }

                                // pick up a new chip at the mouse location, making it appear that the user picked up the chip they clicked
                                PickUpChip(chip.Value, GetMousePosition(update.MouseState), ChipValueToTexture(chip.Value));
                                UpdateTotalBets();
                                GotoState(UIRouletteEODStates.Dragging);
                                return;
                            }
                        }
                    }
                }
            }
        }
        /*
         * Just before the spinning commences, the server sends all valid bets back to the player, so they can see what was accepted and validated before the 
         * betting round time expired. The server sends a string with the following information, each separated with a '%'
         * Total Number of Unique Bets, Bet1 Amount, Bet1 Type, 1-18 numbers involved in Bet1 each separated by %, Bet2 Amount, Bet2 Type, Bet2 Numbers, etc.
         * Given the complexity of handling data of drastically varying length (determined by Bet Type), a string event seemed to be the simplest approach.
         */
        private void MyBetsSyncHandler(string evt, string myBets)
        {
            if (myBets != null)
            {
                int numberOfBets = 0;
                var data = myBets.Split('%');
                if (data != null && Int32.TryParse(data[0], out numberOfBets))
                {
                    // remove visible chips
                    DisposeOfChips(true, false);
                    if (numberOfBets > 0 && data.Length > 3)
                    {
                        int dataIndex = 1;
                        for (int betCount = 0; betCount < numberOfBets; betCount++)
                        {
                            List<int> numbersList = new List<int>();
                            int numberOfNumbers = 0;
                            if (dataIndex + 2 < data.Length)
                            {
                                // get the bet amount
                                int betAmount = 0;
                                if (Int32.TryParse(data[dataIndex++], out betAmount))
                                {
                                    // get the bet type using the 3-character string
                                    string typeString = data[dataIndex++];
                                    if (!VMEODRoulettePlugin.RouletteBetTypes.ContainsValue(typeString))
                                        return;
                                    var type = VMEODRoulettePlugin.RouletteBetTypes.FirstOrDefault(x => x.Value == typeString).Key;
                                    numberOfNumbers = VMEODRoulettePlugin.BetTypeToNumberOfNumbersInPlay(type);
                                    if (numberOfNumbers == 0) return;
                                    int currentNumber = 0;
                                    while (currentNumber < numberOfNumbers)
                                    {
                                        int number = 0;
                                        if (dataIndex < data.Length && Int32.TryParse(data[dataIndex++], out number))
                                        {
                                            numbersList.Add(number);
                                            currentNumber++;
                                        }
                                        else return;
                                    }
                                    if (numbersList.Count > 0)
                                    {
                                        var numbersArray = numbersList.ToArray();
                                        // finally add this bet to the table
                                        PlaceBet(betAmount, type, NumbersListToActualVector(numbersArray), numbersArray, true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            UpdateTotalBets();
        }
        /* Same function as MyBetsSyncHandler() method above, but creates the stacks belonging to neighbor players.
         * Two things to note: 1) This is called EVERY TIME ANY player adds/removes ANY bet &
         * 2) If multiple players have bets on the same place (Vector 2) on the table, the first one is accepted and the rest are ignored. This is because
         * of the foolish design to make the casino chips way larger than the betting table space, making it impossible to have multiple stacks there.
         */
        private void NeighborBetsSyncHandler(string evt, string neighborBets)
        {
            if (neighborBets != null)
            {
                int numberOfBets = 0;
                var data = neighborBets.Split('%');
                if (data != null && Int32.TryParse(data[0], out numberOfBets))
                {
                    // remove visible chips
                    DisposeOfChips(false, true);
                    if (numberOfBets > 0 && data.Length > 3)
                    {
                        int dataIndex = 1;
                        for (int betCount = 0; betCount < numberOfBets; betCount++)
                        {
                            List<int> numbersList = new List<int>();
                            int numberOfNumbers = 0;
                            if (dataIndex + 2 < data.Length)
                            {
                                // get the bet amount
                                int betAmount = 0;
                                if (Int32.TryParse(data[dataIndex++], out betAmount))
                                {
                                    // get the bet type using the 3-character string
                                    string typeString = data[dataIndex++];
                                    if (!VMEODRoulettePlugin.RouletteBetTypes.ContainsValue(typeString))
                                        return;
                                    var type = VMEODRoulettePlugin.RouletteBetTypes.FirstOrDefault(x => x.Value == typeString).Key;
                                    numberOfNumbers = VMEODRoulettePlugin.BetTypeToNumberOfNumbersInPlay(type);
                                    if (numberOfNumbers == 0) return;
                                    int currentNumber = 0;
                                    while (currentNumber < numberOfNumbers)
                                    {
                                        int number = 0;
                                        if (dataIndex < data.Length && Int32.TryParse(data[dataIndex++], out number))
                                        {
                                            numbersList.Add(number);
                                            currentNumber++;
                                        }
                                        else return;
                                    }
                                    if (numbersList.Count > 0)
                                    {
                                        var numbersArray = numbersList.ToArray();
                                        // finally add this bet to the table
                                        PlaceNeighborBet(betAmount, type, NumbersListToActualVector(numbersArray), numbersArray);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        /*
         * This important method is called when 1) the client disconnects, 2) a new game begins, 3) as the server is synchronizing the bets before the spin
         * It not only removes all of the chip images from the UI, it also marks the custom made ones as disposible so the textures themselves are disposed.
         * The custom made textures use a mask to hide the alpha shadows inherent in the assets, allowing them to stack onto other chip textures and look
         * as if they belong on a stack, as opposed to being the bottom chip casting a shadow on the table. Thus they must be disposed of when not in use.
         */
        private void DisposeOfChips(bool disposeMychips, bool disposeNeighborChips)
        {
            // dispose of any chips leftover from previous round
            var imagesToRemove = new List<UISlotsImage>();
            if (disposeMychips)
            {
                foreach (var stack in MyChipsInPlay)
                {
                    var disposeMe = stack.Dispose();
                    if (disposeMe != null)
                        imagesToRemove.AddRange(disposeMe);
                }
                // make new list for my stacks
                MyChipsInPlay = new List<ChipStack>();
            }
            if (disposeNeighborChips)
            {
                foreach (var stack in NeighborChipsInPlay)
                {
                    var disposeMe = stack.Dispose();
                    if (disposeMe != null)
                        imagesToRemove.AddRange(disposeMe);
                }
                // make new list for neighbor stacks
                NeighborChipsInPlay = new List<ChipStack>();
            }
            // remove the images from the UI
            foreach (var image in imagesToRemove)
            {
                try
                {
                    Remove(image);
                }
                catch (Exception e)
                {
                    // something something log file?
                    if (image != null)
                    {
                        image.Visible = false;
                    }
                }
            }
            UpdateTotalBets();
        }
        private void InitUI()
        {
            // Identify and parse script
            Script = RenderScript("rouletteeod.uis");

            Remove(btnChip1);
            Remove(btnChip2);
            Remove(btnChip3);
            Remove(btnChip4);
            Remove(btnChip5);
            Remove(btnShowAllBets);

            // put the buttons in an array for ubiquitious access—an idiosyncratic necessity for multiple user EODs
            btnChipsArray = new UIButton[] { btnChip1, btnChip2, btnChip3, btnChip4, btnChip5, btnShowAllBets };
        }
        private void AddListeners()
        {
            btnChip1.OnButtonClick += ButtonChip1ClickedHandler;
            btnChip2.OnButtonClick += ButtonChip2ClickedHandler;
            btnChip3.OnButtonClick += ButtonChip3ClickedHandler;
            btnChip4.OnButtonClick += ButtonChip4ClickedHandler;
            btnChip5.OnButtonClick += ButtonChip5ClickedHandler;
            btnShowAllBets.OnButtonDown += (btn) => { ShowOnlyNeighborBets(); };
            btnShowAllBets.OnButtonClick += (btn) => { ShowOnlyMyBets(); };
            WheelSpinTimer.Elapsed += new ElapsedEventHandler(WheelSpinTimerHandler);
            // listen to the betting area for a mouse off, to see if chip was dragged out of playable area
            // listen to betting area for a mouse up (click), to see if a chip was placed
            // listen to betting area for a mouse over, to 'animate' the shadows
            RouletteGraphMouseHandler = this.ListenForMouse(new Rectangle(20, 20, (int)(RouletteTable.Width + RouletteTable.X - 10),
                (int)(RouletteTable.Height + RouletteTable.Y - 10)), OnTableMouseEvent);
        }
        // hide the stacks belonging to this player and display the bets from other player (if there are any), synchronized by the server
        private void ShowOnlyNeighborBets()
        {
            // hide my bets
            foreach (var stack in MyChipsInPlay)
                stack.Hide();
            // show any neighbor bets
            foreach (var stack in NeighborChipsInPlay)
                stack.Show();
        }
        // only the stacks belonging to this player
        private void ShowOnlyMyBets()
        {
            // show my bets
            foreach (var stack in MyChipsInPlay)
                stack.Show();
            // hide any neighbor bets
            foreach (var stack in NeighborChipsInPlay)
                stack.Hide();
        }
        /*
         * Circumventing Byte constraints, the arguments are as follows:
         * [0][1] Table's Minimum Bet, [2][3] Table's Maximum Bet, [4][5] Player's Simoleon Balance (up to $65279 which is well over maximum allowable)
         * @Returns false if the player doesn't have enough money to cover the Minimum Bet or if the data is invalid
         */
        private bool NewGame(byte[] minMaxBetPlayerBalance)
        {
            if (minMaxBetPlayerBalance != null && minMaxBetPlayerBalance.Length > 5)
            {
                MinBet = minMaxBetPlayerBalance[0] * 255 + minMaxBetPlayerBalance[1];
                MaxBet = minMaxBetPlayerBalance[2] * 255 + minMaxBetPlayerBalance[3];
                if (MaxBet < MinBet || MinBet == 0)
                    return false;
                PlayerBalance = (minMaxBetPlayerBalance[4] * 255) + minMaxBetPlayerBalance[5]; // as long as they have $1000 or more, since MaxBet < $1001

                // you don't have enough money to play on this table
                if (PlayerBalance < MinBet)
                    return false;

                return true;
            }
            // invalid data
            else
                return false;
        }
        // Spinning disallows betting and animates the wheel, Idle is the betting phase, Dragging allows a chip to follow the user's mouse pointer
        private void GotoState(UIRouletteEODStates state)
        {
            Parent.Invalidate();
            // each state updates the SetTip field for the EOD
            switch (state)
            {
                case UIRouletteEODStates.Idle:
                    {
                        SetTip(GameFacade.Strings["UIText", "258", "1"]);
                        DropChip();
                        RemoveAllShade();
                        SetTime(RoundTimeRemaining);
                        GameState = UIRouletteEODStates.Idle;
                        EnableChipButtons();
                        break;
                    }
                case UIRouletteEODStates.Dragging:
                    {
                        SetTip(GameFacade.Strings["UIText", "258", "22"] + " " + GameFacade.Strings["UIText", "258", "21"]);
                        DisableChipButtons();
                        AddAllShade();
                        GameState = UIRouletteEODStates.Dragging;
                        break;
                    }
                case UIRouletteEODStates.Spinning: // Spinning State
                    {
                        SetTip(GameFacade.Strings["UIText", "258", "2"]);
                        DropChip();
                        DisableChipButtons();
                        RemoveAllShade();
                        RoundTimeRemaining = 0;
                        SetTime(RoundTimeRemaining);
                        GameState = UIRouletteEODStates.Spinning;
                        labelNumber.Visible = false;
                        RouletteBall.Visible = false;
                        break;
                    }
                case UIRouletteEODStates.Results:
                    {
                        SetTip(ResultsString);
                        GameState = UIRouletteEODStates.Results;
                        WheelSpinElapsedCounter = 0;
                        labelNumber.Visible = true;
                        RouletteBall.Visible = true;
                        DisableChipButtons();
                        break;
                    }
            }
        }
        /*
         *  This is idiosyncratic behavior: When any client connects to the server, they don't have references to items created by the Script
         *  unless they were the very first client to join. Even moving the RenderScript to OnConnection doesn't seem to fix this. So the references
         *  need to be recovered.
         */
        private void RecoverButtonRefs()
        {
            btnChip1 = btnChipsArray[Array.LastIndexOf(btnChipsArray, btnChip1)];
            if (btnChip1 == null)
                btnChip1 = Script.Create<UIButton>("btnChip1");
            btnChip2 = btnChipsArray[Array.LastIndexOf(btnChipsArray, btnChip2)];
            if (btnChip2 == null)
                btnChip2 = Script.Create<UIButton>("btnChip2");
            btnChip3 = btnChipsArray[Array.LastIndexOf(btnChipsArray, btnChip3)];
            if (btnChip3 == null)
                btnChip3 = Script.Create<UIButton>("btnChip3");
            btnChip4 = btnChipsArray[Array.LastIndexOf(btnChipsArray, btnChip4)];
            if (btnChip4 == null)
                btnChip4 = Script.Create<UIButton>("btnChip4");
            btnChip5 = btnChipsArray[Array.LastIndexOf(btnChipsArray, btnChip5)];
            if (btnChip5 == null)
                btnChip5 = Script.Create<UIButton>("btnChip5");
            btnShowAllBets = btnChipsArray[Array.LastIndexOf(btnChipsArray, btnShowAllBets)];
            if (btnShowAllBets == null)
                btnShowAllBets = Script.Create<UIButton>("btnShowAllBets");
            btnChip1.Tooltip = GameFacade.Strings["UIText", "258", "8"].Replace("%d","1");
            btnChip2.Tooltip = GameFacade.Strings["UIText", "258", "8"].Replace("%d", "5");
            btnChip3.Tooltip = GameFacade.Strings["UIText", "258", "8"].Replace("%d", "10");
            btnChip4.Tooltip = GameFacade.Strings["UIText", "258", "8"].Replace("%d", "25");
            btnChip5.Tooltip = GameFacade.Strings["UIText", "258", "8"].Replace("%d", "100");
        }
        /* 
         * place non-button graphical elements
         */
        private void CreateImages()
        {
            RouletteTable = Script.Create<UIImage>("rouletteTable");
            Add(RouletteTable);
            /*
             * Create the roulette wheel list for the "spinning" animation
             */
            // the simulated "spinning" texture bounds
            var postGreen = new RouletteWheelStateNode(752, null);
            var greenSpin = new RouletteWheelStateNode(564, postGreen);
            var preGreen = new RouletteWheelStateNode(376, greenSpin);
            var redSpin = new RouletteWheelStateNode(658, preGreen);
            var blackSpin = new RouletteWheelStateNode(470, redSpin);
            postGreen.Next = blackSpin;
            // the three "ultimate" stops texture bounds need to be saved for later to ensure that the wheel graphic appears to "land" on the correct color
            var blackStop = new RouletteWheelStateNode(0, redSpin);
            postGreen.Ultimate = blackStop;
            UltimateStopsArray[0] = blackStop;
            var redStop = new RouletteWheelStateNode(94, preGreen);
            blackSpin.Ultimate = redStop;
            UltimateStopsArray[1] = redStop;
            var greenStop = new RouletteWheelStateNode(188, postGreen);
            preGreen.Ultimate = greenStop;
            UltimateStopsArray[2] = greenStop;

            // when the UI first opens, default stop will be the red stop
            WheelStateList = new RouletteWheelStateList(redStop);

            // create the actual graphic using Script => UIImage, then copy its properites when creating the proper UISlotsImage
            ScriptedRouletteWheel = Script.Create<UIImage>("rouletteWheel");
            RouletteWheel = new UISlotsImage(ScriptedRouletteWheel.Texture);
            RouletteWheel.Position = ScriptedRouletteWheel.Position;
            RouletteWheel.SetBounds(0, 0, redStop.X, 40);
            Add(RouletteWheel);
            RouletteBall = Script.Create<UIImage>("rouletteBall");
            RouletteBall.Y = 140;
            Add(RouletteBall);
            RouletteBall.Visible = false;

            // Make fake button images, put them undernearth real buttons. Because when dragged chips collide with visible buttons, bad things happen.
            FakeChipBtn1 = new UISlotsImage(imageChip_001_Btn);
            FakeChipBtn1.SetBounds(96, 0, 32, 26);
            FakeChipBtn1.Position = btnChip1.Position;
            Add(FakeChipBtn1);
            FakeChipBtn2 = new UISlotsImage(imageChip_005_Btn);
            FakeChipBtn2.SetBounds(96, 0, 32, 23);
            FakeChipBtn2.Position = btnChip2.Position;
            Add(FakeChipBtn2);
            FakeChipBtn3 = new UISlotsImage(imageChip_010_Btn);
            FakeChipBtn3.SetBounds(96, 0, 32, 22);
            FakeChipBtn3.Position = btnChip3.Position;
            Add(FakeChipBtn3);
            FakeChipBtn4 = new UISlotsImage(imageChip_025_Btn);
            FakeChipBtn4.SetBounds(96, 0, 32, 24);
            FakeChipBtn4.Position = btnChip4.Position;
            Add(FakeChipBtn4);
            FakeChipBtn5 = new UISlotsImage(imageChip_100_Btn);
            FakeChipBtn5.SetBounds(92, 0, 31, 24);
            FakeChipBtn5.Position = btnChip5.Position;
            FakeChipBtn5.X -= 1;
            Add(FakeChipBtn5);
            /*
             * Create all shadows
             */
            DoubleZeroBox = new UIHighlightSprite(20, 29)
            {
                X = RouletteTable.X + 1,
                Y = RouletteTable.Y + 2,
                Visible = true
            };
            Add(DoubleZeroBox);
            ZeroBox = new UIHighlightSprite(20, 29)
            {
                X = RouletteTable.X + 1,
                Y = RouletteTable.Y + 32,
                Visible = true
            };
            Add(ZeroBox);
            // an array will hold the shadows for each number so their number and color isRed can be accessed
            ShadowBoxArray = new ShadowBox[3, 12];
            int number = 0;
            bool isRed;
            UIHighlightSprite sprite;
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 12; y++)
                {
                    // the number on the table
                    number = (3 * (y + 1) - x);

                    // is it a red number found in the array of red numbers?
                    isRed = (Array.IndexOf(RedNumbersArray, number) != -1);
                    
                    // sprite
                    sprite = new UIHighlightSprite(20, 19)
                    {
                        X = (RouletteTable.X + 22) + (y * 21),
                        Y = (RouletteTable.Y + 2) + (x * 20),
                        Visible = false
                    };
                    Add(sprite);

                    // all together
                    ShadowBoxArray[x, y] = new ShadowBox(number, isRed, sprite);
                }
            }
        }
        // This method is a liason between a brand new player and their first chance to bet. It also is called after each placed bet. 
        private void ResumeBetting()
        {
            // check the game timer to determine what state to set the EOD
            if (RoundTimeRemaining == 0)
            {
                if (!GameState.Equals(UIRouletteEODStates.Spinning) && !GameState.Equals(UIRouletteEODStates.Results))
                    GotoState(UIRouletteEODStates.Spinning);
            }
            else
            {
                GotoState(UIRouletteEODStates.Idle);
            }
        }
        /*
         * The next two methods create images to continually update onto the player's mouse pointer. The first is called from the UIButtons exclusively.
         */
        private void PickUpChip(int chipValue, UIButton button, Texture2D chipImage)
        {
            CurrentDraggingChip = new PlayChip(chipValue, chipImage, button.Position, 0);
            Add(CurrentDraggingChip.ChipImage);
        }
        private void PickUpChip(int chipValue, Vector2 position, Texture2D chipImage)
        {
            CurrentDraggingChip = new PlayChip(chipValue, chipImage, position, 0);
            Add(CurrentDraggingChip.ChipImage);
        }
        // Drops the chip by removing it. The texture does not need to be disposed of because it may be used again.
        private void DropChip()
        {
            if (CurrentDraggingChip != null)
            {
                Remove(CurrentDraggingChip.ChipImage);
                CurrentDraggingChip = null;
            }
        }
        // Returns the texture matching the value of the chip stored in PlayChip
        private Texture2D ChipValueToTexture(int value)
        {
            if (value == 100)
                return imageChip_100;
            else if (value == 25)
                return imageChip_025;
            else if (value == 10)
                return imageChip_010;
            else if (value == 5)
                return imageChip_005;
            else
                return imageChip_001;
        }
        /*
         * The only option for this is to hardcode some ranges of x and y values in order to best assess which number(s) the user is hovering over. This
         * method checks every possible x and y combination and returns a list of all numbers represented by the bet type determined by the parameter.
         */
        private int[] VectorToNumbersList(Vector2 coords)
        {
            int[] betNumbers = new int[0];
            int x = (int)coords.X;
            int y = (int)coords.Y;
            x -= (int)RouletteTable.X;
            y -= (int)RouletteTable.Y;
            // edges of the table
            if (x < 0 || x > 290 || y < 0 || y > 98) // not on the table
                return null;
            // vector is on a "column bet space" = 12 numbers based on y 
            else if (x > 273)
            {
                if (y > 65) // nothing, due to jagged edge
                    return null;
                else if (y > 41) // bottom column starting with 1 ending with 34
                {
                    betNumbers = new int[] { 1, 4, 7, 10, 13, 16, 19, 22, 25, 28, 31, 34 };
                }
                else if (y > 21) // middle column starting with 2 ending with 35
                {
                    betNumbers = new int[] { 2, 5, 8, 11, 14, 17, 20, 23, 26, 29, 32, 35 };
                }
                else // top column starting with 3 ending with 36
                {
                    betNumbers = new int[] { 3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36 };
                }
            }
            // vector might be on 0 or 00
            else if (x < 19)
            {
                if (y > 63) // nothing, due to jagged edge
                    return null;
                else if (y < 29) // 00
                    betNumbers = new int[] { 100 };
                else if (y > 35) // 0
                    betNumbers = new int[] { 0 };
                else // split of 0 and 00
                    betNumbers = new int[] { 0, 100 };
            }
            else // 18 < x < 274 & 0 < y < 98
            {
                if (y > 79) // out of bounds OR "low" "high" "odd" "even" "black" "red"
                {
                    if (x < 22 || x > 275) // nothing, due to jagged edge
                        return null;
                    else if (x < 64) // "low"
                    {
                        betNumbers = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
                    }
                    else if (x < 106) // "even"
                    {
                        betNumbers = new int[] { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 36 };
                    }
                    else if (x < 148) // "red"
                    {
                        betNumbers = RedNumbersArray;
                    }
                    else if (x < 190) // "black"
                    {
                        betNumbers = BlackNumbersArray;
                    }
                    else if (x < 232) // "odd"
                    {
                        betNumbers = new int[] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33, 35 };
                    }
                    else // "high"
                    {
                        betNumbers = new int[] { 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36 };
                    }
                }
                else if (y > 65) // "dozen" based on x
                {
                    if (x < 22 || x > 275) // nothing, due to jagged edge
                        return null;
                    else if (x < 106) // first "dozen"
                    {
                        betNumbers = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
                    }
                    else if (x < 190) // second "dozen"
                    {
                        betNumbers = new int[] { 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
                    }
                    else // third "dozen"
                    {
                        betNumbers = new int[] { 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36 };
                    }
                }
                else if (y > 58) // non-zero "street" , "line" , "sucker", or the 0 & 1 "split"
                {
                    // reminder: we know 19 <= x < 274, and 58 > y < 65
                    if (x > 256) // "street"
                    {
                        betNumbers = new int[] { 34, 35, 36 };
                    }
                    else if (x > 249) // "line"
                    {
                        betNumbers = new int[] { 31, 32, 33, 34, 35, 36 };
                    }
                    else if (x > 235) // "street"
                    {
                        betNumbers = new int[] { 31, 32, 33 };
                    }
                    else if (x > 228) // "line"
                    {
                        betNumbers = new int[] { 28, 29, 30, 31, 32, 33 };
                    }
                    else if (x > 214) // "street"
                    {
                        betNumbers = new int[] { 28, 29, 30 };
                    }
                    else if (x > 207) // "line"
                    {
                        betNumbers = new int[] { 25, 26, 27, 28, 29, 30 };
                    }
                    else if (x > 193) // "street"
                    {
                        betNumbers = new int[] { 25, 26, 27 };
                    }
                    else if (x > 186) // "line"
                    {
                        betNumbers = new int[] { 22, 23, 24, 25, 26, 27 };
                    }
                    else if (x > 172) // "street"
                    {
                        betNumbers = new int[] { 22, 23, 24 };
                    }
                    else if (x > 165) // "line"
                    {
                        betNumbers = new int[] { 19, 20, 21, 22, 23, 24 };
                    }
                    else if (x > 151) // "street"
                    {
                        betNumbers = new int[] { 19, 20, 21 };
                    }
                    else if (x > 144) // "line"
                    {
                        betNumbers = new int[] { 16, 17, 18, 19, 20, 21 };
                    }
                    else if (x > 130) // "street"
                    {
                        betNumbers = new int[] { 16, 17, 18 };
                    }
                    else if (x > 123) // "line"
                    {
                        betNumbers = new int[] { 13, 14, 15, 16, 17, 18 };
                    }
                    else if (x > 109) // "street"
                    {
                        betNumbers = new int[] { 13, 14, 15 };
                    }
                    else if (x > 102) // "line"
                    {
                        betNumbers = new int[] { 10, 11, 12, 13, 14, 15 };
                    }
                    else if (x > 88) // "street"
                    {
                        betNumbers = new int[] { 10, 11, 12 };
                    }
                    else if (x > 81) // "line"
                    {
                        betNumbers = new int[] { 7, 8, 9, 10, 11, 12 };
                    }
                    else if (x > 67) // "street"
                    {
                        betNumbers = new int[] { 7, 8, 9 };
                    }
                    else if (x > 60) // "line"
                    {
                        betNumbers = new int[] { 4, 5, 6, 7, 8, 9 };
                    }
                    else if (x > 46) // "Street"
                    {
                        betNumbers = new int[] { 4, 5, 6 };
                    }
                    else if (x > 39) // "line"
                    {
                        betNumbers = new int[] { 1, 2, 3, 4, 5, 6 };
                    }
                    else if (x > 25) // "street"
                    {
                        betNumbers = new int[] { 1, 2, 3 };
                    }
                    else // "sucker" or "five"
                    {
                        betNumbers = new int[] { 0, 100, 1, 2, 3 };
                    }
                }
                else // we know 19 <= x < 274 & 0 >= y <= 58 
                {
                    // first find the exact number that was clicked
                    List<int> numbers = new List<int>();
                    if (x <= 21) // zero or double zero (100)
                    {
                        if (y < 32) // double zero
                            numbers.Add(100);
                        else // zero
                            numbers.Add(0);
                    }
                    else
                        numbers.Add((x - 1) / 21 * 3 - (y - 2) / 20);

                    if (numbers[0] == 0) // needs a special case because it's a pain
                    {
                        // we already know y < 66 and x >= 19
                        if (y > 45) // "split" 0 1
                            numbers.Add(1);
                        else if (y >= 39) // "street" 0 1 2
                        {
                            numbers.Add(1);
                            numbers.Add(2);
                        }
                        else if (y <= 35) // "street" 0 00 2
                        {
                            numbers.Add(100);
                            numbers.Add(2);
                        }
                        betNumbers = numbers.ToArray();
                        return betNumbers;
                    }
                    else if (numbers[0] == 100) // needs a special case because it's a pain
                    {
                        // we already know x >= 19 and 0 =< y < 32
                        if (y >= 29) // "street" 0 00 2
                        {
                            numbers.Insert(0, 0);
                            numbers.Add(2);
                        }
                        else if (y <= 25 && y >= 19) // "street" 00 2 3
                        {
                            numbers.Add(2);
                            numbers.Add(3);
                        }
                        else if (y < 25) // "split" 00 3
                            numbers.Add(3);
                        betNumbers = numbers.ToArray();
                        return betNumbers;
                    }
                    else if (numbers[0] == 2) // definitely needs a special case because it's a pain
                    {
                        if (x >= 40)
                            numbers.Add(5);
                        else if (x <= 25)
                        {
                            if (y <= 25) // "street" 00 2 3
                            {
                                numbers.Add(3);
                                numbers.Insert(0, 100);
                            }
                            else if (y <= 35 && y > 28) // "street" 0 00 2
                            {
                                numbers.Insert(0, 100);
                                numbers.Insert(0, 0);
                            }
                            else if (y >= 39) // "street" 0 1 2
                            {
                                numbers.Insert(0, 1);
                                numbers.Insert(0, 0);
                            }
                        }
                        if (y <= 25 && x > 25)
                        {
                            if (numbers.Count == 2) // "corner" 2 3 5 6
                            {
                                numbers.Insert(1, 3);
                                numbers.Add(6);
                            }
                            else // "split" 2 3
                                numbers.Add(3);
                        }
                        else if (y >= 39 && x > 25)
                        {
                            if (numbers.Count == 2) // "corner" 1 2 4 5
                            {
                                numbers.Insert(1, 4);
                                numbers.Insert(0, 1);
                            }
                            else // "split" 1 2
                                numbers.Insert(0, 1);
                        }
                        betNumbers = numbers.ToArray();
                        return betNumbers;
                    }

                    // check x for split/corner/street with number to the left
                    else if ((x - 1) % 21 == 0 || (x - 1) % 21 == 1 || (x - 1) % 21 == 2)
                    {
                        if (numbers[0] == 3)
                        {
                            // add the double zero (100) BEFORE the number
                            numbers.Insert(0, 100);
                        }
                        else if (numbers[0] == 1)
                        {
                            // add the zero BEFORE the number
                            numbers.Insert(0, 0);
                        }
                        else // the number will not be a zero, double zero (100), or a 1 2 3
                        {
                            // subtract 3 from the number and insert it at the beginning
                            numbers.Insert(0, numbers[0] - 3);
                        }
                    }
                    // "split/corner" with number to the right
                    else if ((x - 1) % 21 == 18 || (x - 1) % 21 == 19 || (x - 1) % 21 == 20)
                    {
                        if (numbers[0] < 34) // ignore 34 35 36 as they're on the right edge & this won't be a zero or double zero (100)
                        {
                            // add 3 to the number and append
                            numbers.Add(numbers[0] + 3);
                        }
                    }
                    /*
                     * do the same for Y
                     */
                    // check y for "split/corner/street" with number below
                    if ((y - 2) % 20 == 0 || (y - 2) % 20 == 1 || (y - 2) % 20 == 2)
                    {
                        if (numbers[0] == 0 && numbers.Count == 2) // the only case here is when 0 was added before the 1, so this has to be "street" 0 1 2
                        {
                            numbers.Add(2);
                        }
                        // split of 00 3 and/or top row
                        else if (numbers[0] == 100 || numbers[0] % 3 == 0) // only can be double zero (100) if it was added above before the 3, or top row
                        {
                            // do nothing, top row 
                        }
                        else
                        {
                            if (numbers.Count == 2) // "corner" so add 2 more numbers
                            {
                                // add one to the first number and insert it before the second
                                numbers.Insert(1, numbers[0] + 1);
                                // add one to second number and append
                                numbers.Add(numbers[2] + 1);
                            }
                            else // "split"
                            {
                                // add one to the number and append
                                numbers.Add(numbers[0] + 1);
                            }
                        }
                    }
                    // split/corner/street with number above
                    else if ((y - 2) % 20 == 17 || (y - 2) % 20 == 18 || (y - 2) % 20 == 19)
                    {
                        if (numbers[0] == 100 && numbers.Count == 2) // only case is double zero (100) added above before 3, so "street" of 00 2 3
                        {
                            numbers.Insert(1, 2);
                        }
                        else if (numbers[0] == 0 || (numbers[0] - 1) % 3 == 0) // only can be zero if it was added above before the 1, or bottom row
                        {
                            // do nothing, bottom row
                        }
                        else
                        {
                            if (numbers.Count == 2) // "corner" so add 2 more numbers
                            {
                                // subtract one from the second number and insert it before the second number
                                numbers.Insert(1, numbers[1] - 1);
                                // subtract one from the first number and insert it at the beginning
                                numbers.Insert(0, numbers[0] - 1);
                            }
                            else // "split"
                            {
                                // subtract one from the number and insert it at the beginning
                                numbers.Insert(0, numbers[0] - 1);
                            }
                        }
                    }
                    betNumbers = numbers.ToArray();
                }
            }
            return betNumbers;
        }
        /*
         * This likely overcomplicated method finds the exact location that a chip image should be placed onto the UI in order to properly reflect
         * the bet type represented by the numbers in the parameter numbersList. An exact Vector2 is crucial for cleanliness as well as in order to
         * properly see if another bet already exists in that location.
         */
        private Vector2 NumbersListToActualVector(int[] numbersList)
        {
            Vector2 result = new Vector2();
            int numberOfNumbers = numbersList.Length;

            // INSIDE BETS
            /*
             * "straight-up bet"
             */
            if (numberOfNumbers == 1)
            {
                // case for 0
                if (numbersList[0] == 0)
                {
                    result = new Vector2(1, 35);
                }
                // case for 00
                else if (numbersList[0] == 100)
                {
                    result = new Vector2(1, 5);
                }
                else
                {
                    result = new Vector2(((((numbersList[0] - 1) / 3) + 1) * NUMBER_SPACE_WIDTH) + 1,
                        (numbersList[0] % 3 == 0) ? 2 : ((3 - (numbersList[0] % 3)) * NUMBER_SPACE_HEIGHT) + 2);
                }
            }
            /*
             * "split bet"
             */
            else if (numberOfNumbers == 2)
            {
                // get the x and y of both numbers
                Vector2 numberPos1 = new Vector2();
                Vector2 numberPos2 = new Vector2();

                // "split" involving 0 or 100
                if (numbersList[0] == 0)
                {
                    // case for 0 and 00 split
                    if (numbersList[1] == 100) // "split" 0 00
                    {
                        numberPos1 = new Vector2(1, 35); // 0
                        numberPos2 = new Vector2(1, 5); // 00
                    }
                    else if (numbersList[1] == 1) // "split" 0 1
                    {
                        // position of 1
                        numberPos2 = new Vector2(((((numbersList[1] - 1) / 3) + 1) * NUMBER_SPACE_WIDTH) + 1,
                                (numbersList[1] % 3 == 0) ? 2 : ((3 - (numbersList[1] % 3)) * NUMBER_SPACE_HEIGHT) + 2);
                        // modified position of 0
                        numberPos1 = new Vector2(1, numberPos2.Y); // 0
                    }
                }
                else if (numbersList[0] == 100) // can only be "split" 00 3
                {
                    // position of 3
                    numberPos2 = new Vector2(((((numbersList[1] - 1) / 3) + 1) * NUMBER_SPACE_WIDTH) + 1,
                            (numbersList[1] % 3 == 0) ? 2 : ((3 - (numbersList[1] % 3)) * NUMBER_SPACE_HEIGHT) + 2);
                    // modified position of 00
                    numberPos1 = new Vector2(1, numberPos2.Y); // 0
                }
                else
                {
                    numberPos1 = new Vector2(((((numbersList[0] - 1) / 3) + 1) * NUMBER_SPACE_WIDTH) + 1,
                            (numbersList[0] % 3 == 0) ? 2 : ((3 - (numbersList[0] % 3)) * NUMBER_SPACE_HEIGHT) + 2);
                    numberPos2 = new Vector2(((((numbersList[1] - 1) / 3) + 1) * NUMBER_SPACE_WIDTH) + 1,
                            (numbersList[1] % 3 == 0) ? 2 : ((3 - (numbersList[1] % 3)) * NUMBER_SPACE_HEIGHT) + 2);
                }

                // get the midpoint
                result = (numberPos1 + numberPos2) / 2;
            }
            /*
             * "street bet"
             */
            else if (numberOfNumbers == 3)
            {
                float tempX1 = 0;
                float tempX2 = 0;
                float tempY1 = 0;
                float tempY2 = 0;
                if (numbersList[0] == 0)
                {
                    // 0, 00, 2
                    if (numbersList[1] == 100)
                    {
                        tempX1 = 2; // the X of 0 amd 00
                        tempX2 = ((((numbersList[2] - 1) / 3) + 1) * NUMBER_SPACE_WIDTH) + 1; // x of number:2
                        tempY1 = (numbersList[2] % 3 == 0) ? 2 : ((3 - (numbersList[2] % 3)) * NUMBER_SPACE_HEIGHT) + 2; // y of the number:2
                        tempY2 = (numbersList[2] % 3 == 0) ? 2 : ((3 - (numbersList[2] % 3)) * NUMBER_SPACE_HEIGHT) + 2; // y of the number:2 again
                        result = new Vector2((tempX1 + tempX2) / 2, (tempY1 + tempY2) / 2);  // mid point
                    }
                    // 0, 1, 2
                    else
                    {
                        tempX1 = 2; // the X of 0
                        tempX2 = ((((numbersList[1] - 1) / 3) + 1) * NUMBER_SPACE_WIDTH) + 1; // x of number:1
                        tempY1 = (numbersList[1] % 3 == 0) ? 2 : ((3 - (numbersList[1] % 3)) * NUMBER_SPACE_HEIGHT) + 2; // y of number:1
                        tempY2 = (numbersList[2] % 3 == 0) ? 2 : ((3 - (numbersList[2] % 3)) * NUMBER_SPACE_HEIGHT) + 2; // y of number:2
                        result = new Vector2((tempX1 + tempX2) / 2, (tempY1 + tempY2) / 2);  // mid point
                    }
                }
                // 00, 2, 3
                else if (numbersList[0] == 100)
                {
                    tempX1 = 2; // the X of 00
                    tempX2 = ((((numbersList[1] - 1) / 3) + 1) * NUMBER_SPACE_WIDTH) + 1; // x of number:2
                    tempY1 = (numbersList[1] % 3 == 0) ? 2 : ((3 - (numbersList[1] % 3)) * NUMBER_SPACE_HEIGHT) + 2; // y of number:2
                    tempY2 = (numbersList[2] % 3 == 0) ? 2 : ((3 - (numbersList[2] % 3)) * NUMBER_SPACE_HEIGHT) + 2; // y of number:3
                    result = new Vector2((tempX1 + tempX2) / 2, (tempY1 + tempY2) / 2);  // mid point
                }
                // simply the X of any of the non-zero numbers in the list, y is constant
                else
                {
                    result = new Vector2(((((numbersList[0] - 1) / 3) + 1) * NUMBER_SPACE_WIDTH) + 1, (2.5f * NUMBER_SPACE_HEIGHT) + 2);
                }
            }
            /*
             * "corner bet"
             */
            else if (numberOfNumbers == 4)
            {
                // simply find the midpoint of 4 numbers
                var firstNumberOrigin = new Vector2(((((numbersList[0] - 1) / 3) + 1) * NUMBER_SPACE_WIDTH) + 1,
                            (numbersList[0] % 3 == 0) ? 2 : ((3 - (numbersList[0] % 3)) * NUMBER_SPACE_HEIGHT) + 2);
                var secondNumberOrigin = new Vector2(((((numbersList[1] - 1) / 3) + 1) * NUMBER_SPACE_WIDTH) + 1,
                            (numbersList[1] % 3 == 0) ? 2 : ((3 - (numbersList[1] % 3)) * NUMBER_SPACE_HEIGHT) + 2);
                var thirdNumberOrigin = new Vector2(((((numbersList[2] - 1) / 3) + 1) * NUMBER_SPACE_WIDTH) + 1,
                            (numbersList[2] % 3 == 0) ? 2 : ((3 - (numbersList[2] % 3)) * NUMBER_SPACE_HEIGHT) + 2);
                var fourthNumberOrigin = new Vector2(((((numbersList[3] - 1) / 3) + 1) * NUMBER_SPACE_WIDTH) + 1,
                            (numbersList[3] % 3 == 0) ? 2 : ((3 - (numbersList[3] % 3)) * NUMBER_SPACE_HEIGHT) + 2);
                result = (firstNumberOrigin + secondNumberOrigin + thirdNumberOrigin + fourthNumberOrigin) / 4;
            }
            /*
             * "five bet" or "sucker bet" because in American roulette, the house edge is higher on this bet than any other bet
             */
            else if (numberOfNumbers == 5)
            {
                /* The chip can only be placed on two places on the board for this bet: this one and one called the "courtesy bet" line.
                 * The latter is omitted here for ease of use and since the spirit of it was overcrowded tables, which doesn't apply here.
                 * The position is the same constant y as the "street bet" and the x is the midpoint of the origins of 0/00 and 1/2/3
                 */
                result = new Vector2((2 + 2 + NUMBER_SPACE_WIDTH) / 2, (2.5f * NUMBER_SPACE_HEIGHT) + 2);
            }
            /*
             * "line bet"
             */
            else if (numberOfNumbers == 6)
            {
                // simply find the X midpoint of the first and 4th numbers ([0] and [3]), the Y is the same constant as "street" and "sucker" bet
                float tempX1 = ((((numbersList[0] - 1) / 3) + 1) * NUMBER_SPACE_WIDTH) + 1;
                float tempX2 = ((((numbersList[4] - 1) / 3) + 1) * NUMBER_SPACE_WIDTH) + 1;
                result = new Vector2((tempX1 + tempX2) / 2, (2.5f * NUMBER_SPACE_HEIGHT) + 2);
            }
            // OUTSIDE BETS
            else if (numberOfNumbers == 12)
            {
                /*
                 * "column bet" - even though it's a row from the UI perspective...also the chips will hang off the right side of the table, thanks Maxis
                 */
                if (numbersList[0] + 1 != numbersList[1]) // if the numbers are not sequential, it must be a column bet
                {
                    // x is constant, y varies by column which is determined by the first number in the list: 1, 2, or 3
                    result = new Vector2(13 * NUMBER_SPACE_WIDTH + 1, (numbersList[0] % 3 == 0) ? 2 : ((3 - (numbersList[0] % 3)) * NUMBER_SPACE_HEIGHT)+ 2);
                }
                /*
                 * "dozen bet" - 1 to 12, 13 to 24, or 25 to 36
                 */
                else // if the numbers are sequential, it must be a dozen bet
                {
                    // y is constant, x varies according to the dozen
                    result = new Vector2(numbersList[0] / 12 * 4f * NUMBER_SPACE_WIDTH + 2.5f * NUMBER_SPACE_WIDTH + 1, 3 * NUMBER_SPACE_HEIGHT + 2);
                }
            }
            else if (numberOfNumbers == 18)
            {
                if (numbersList[0] + 1 != numbersList[1]) // numbers are not sequential
                {
                    if (numbersList[0] == 1) // first number is 1
                    {
                        /*
                         * "red bet" - red numbers
                         */
                        if (numbersList[17] == 36) // last number is 36
                        {
                            // constant vector
                            result = new Vector2(5.5f * NUMBER_SPACE_WIDTH + 1, 3 * NUMBER_SPACE_HEIGHT + LOW_RETURN_SPACE_HEIGHT + 2);
                        }
                        /*
                         * "odd bet" - odd numbers
                         */
                        else // last number is 35
                        {
                            // constant vector
                            result = new Vector2(9.5f * NUMBER_SPACE_WIDTH + 1, 3 * NUMBER_SPACE_HEIGHT + LOW_RETURN_SPACE_HEIGHT + 2);
                        }
                    }
                    else // first number is 2
                    {
                        /*
                         * "even bet" - even numbers
                         */
                        if (numbersList[17] == 36) // last number is 36
                        {
                            // constant vector
                            result = new Vector2(3.5f * NUMBER_SPACE_WIDTH + 1, 3 * NUMBER_SPACE_HEIGHT + LOW_RETURN_SPACE_HEIGHT + 2);
                        }
                        /*
                         * "black bet" - black numbers
                         */
                        else // last number is 35
                        {
                            // constant vector
                            result = new Vector2(7.5f * NUMBER_SPACE_WIDTH + 1, 3 * NUMBER_SPACE_HEIGHT + LOW_RETURN_SPACE_HEIGHT + 2);
                        }
                    }
                }
                else // numbers are sequential
                {
                    /*
                     * "low bet" - 1 to 18
                     */
                    if (numbersList[0] == 1) // first number is 1
                    {
                        // constant vector
                        result = new Vector2(1.5f * NUMBER_SPACE_WIDTH + 1, 3 * NUMBER_SPACE_HEIGHT + LOW_RETURN_SPACE_HEIGHT + 2);
                    }
                    /*
                     * "high bet" - 19 to 36
                     */
                    else // first number is 19
                    {
                        // constant vector
                        result = new Vector2(11.5f * NUMBER_SPACE_WIDTH + 1, 3 * NUMBER_SPACE_HEIGHT + LOW_RETURN_SPACE_HEIGHT + 2);
                    }
                }
            }
            else
            {
                // massive error
                result = new Vector2(100, 100);
            }
            return result + new Vector2(RouletteTable.X, RouletteTable.Y); // add the offset for the roulette table
        }
        // Returns the type of bet based on the length of the numbers list and in some cases its contents. The bet type must be sent to the server.
        private VMEODRouletteBetTypes NumbersListToBetType(int[] numbersList)
        {
            var length = numbersList.Length;
            switch (length)
            {
                case 2:
                    return VMEODRouletteBetTypes.Split;
                case 3:
                    return VMEODRouletteBetTypes.Street;
                case 4:
                    return VMEODRouletteBetTypes.Corner;
                case 5:
                    return VMEODRouletteBetTypes.Sucker;
                case 6:
                    return VMEODRouletteBetTypes.Line;
                case 12:
                    {
                        if (numbersList[0] + 1 != numbersList[1]) // if the numbers are not sequential, it must be a column bet
                            return VMEODRouletteBetTypes.Column;
                        else // if it's not a column, it's a dozen (numbers are indeed sequential)
                            return VMEODRouletteBetTypes.Dozen;
                    }
                case 18:
                    {
                        if (numbersList[0] + 1 != numbersList[1]) // numbers are NOT sequential
                        {
                            if (numbersList[0] == 1) // first number is 1
                            {
                                // "red bet" - red numbers
                                if (numbersList[17] == 36) // last number is 36
                                    return VMEODRouletteBetTypes.Red;
                                // "odd bet" - odd numbers
                                else // last number is 35
                                    return VMEODRouletteBetTypes.Odd;
                            }
                            else // first number is NOT 1 (is 2)
                            {
                                // "even bet" - even numbers
                                if (numbersList[17] == 36) // last number is 36
                                    return VMEODRouletteBetTypes.Even;
                                // "black bet" - black numbers
                                else // last number is 35
                                    return VMEODRouletteBetTypes.Black;
                            }
                        }
                        else // numbers ARE sequential
                        {
                            // "low bet" - 1 to 18
                            if (numbersList[0] == 1) // first number is 1
                                return VMEODRouletteBetTypes.Low;
                            // "high bet" - 19 to 36
                            else // first number is NOT 1 (is 19)
                                return VMEODRouletteBetTypes.High;
                        }
                    }
                default: // length is 1
                    return VMEODRouletteBetTypes.StraightUp;
            }
        }
        /*
         * This method is overloaded in order to allow it to be called from both the other method, as well as the synchronizing event from the server,
         * which after the bets are synchronized, the very same bets don't need to be resent to the server for validation. It places the bet for the player
         * by checking if a bet already exists and imcrementing the stack or creating a new stack.
         */
        private void PlaceBet(int chipValue, VMEODRouletteBetTypes type, Vector2 actualCoordinates, int[] numbersList, bool skipEvent)
        {
            // will this bet put me over the max bet?
            if (CurrentRoundTotalBets + chipValue > MaxBet)
                OverMaxBetHandler("", new byte[] { (byte)chipValue });

            bool betIsValid = false;
            if (MyChipsInPlay.Count == 0)
            {
                var newChipStack = new ChipStack(actualCoordinates, type);
                newChipStack.Push(chipValue, ChipValueToTexture(chipValue));
                MyChipsInPlay.Add(newChipStack);
                betIsValid = true;
                var image = newChipStack.Peek().ChipImage;
                Add(image);
            }
            else
            {
                ChipStack existingChipStack = null;
                foreach (var stack in MyChipsInPlay)
                {
                    if (stack.Position.Equals(actualCoordinates))
                    {
                        existingChipStack = stack;
                        break;
                    }
                }
                // chip stack already exists at that location, so push a new chip on top of it
                if (existingChipStack != null)
                {
                    // if more chips can fit on the stack
                    if (existingChipStack.Push(chipValue, ChipValueToTexture(chipValue)))
                    {
                        var image = existingChipStack.Peek().ChipImage;
                        betIsValid = true;
                        Add(image);
                    }
                }
                // chip stack does not already exist, so start a new stack
                else
                {
                    var newChipStack = new ChipStack(actualCoordinates, type);
                    newChipStack.Push(chipValue, ChipValueToTexture(chipValue));
                    MyChipsInPlay.Add(newChipStack);
                    betIsValid = true;
                    var image = newChipStack.Peek().ChipImage;
                    Add(image);
                }
            }
            if (betIsValid && !skipEvent) {
                // send bet to server to be validated
                string typeString = "";
                if (VMEODRoulettePlugin.RouletteBetTypes.TryGetValue(type, out typeString))
                {
                    string numbersString = "";
                    foreach (var number in numbersList)
                        numbersString += "%" + number.ToString();
                    Send("roulette_new_bet", chipValue + "%" + typeString + numbersString);
                }
            }
            UpdateTotalBets();
        }
        /*
         * Functions identically to Placebet() except it affects the neighbor bets chip stacks, unseen until the player holds down the button to show
         * all neighbor bets.
         */
        private void PlaceNeighborBet(int chipValue, VMEODRouletteBetTypes type, Vector2 actualCoordinates, int[] numbersList)
        {
            ChipStack existingChipStack = null;
            if (NeighborChipsInPlay.Count > 0)
            {
                foreach (var stack in NeighborChipsInPlay)
                {
                    if (stack.Position.Equals(actualCoordinates))
                    {
                        existingChipStack = stack;
                        break;
                    }
                }
            }
            // chip stack already exists at that location, so push a new chip on top of it
            if (existingChipStack != null)
            {
                // if more chips can fit on the stack
                if (existingChipStack.Push(chipValue, ChipValueToTexture(chipValue)))
                {
                    var image = existingChipStack.Peek().ChipImage;
                    Add(image);
                    image.Visible = false;
                }
            }
            else
            {
                // chip stack does not already exist, so start a new stack
                var newChipStack = new ChipStack(actualCoordinates, type);
                newChipStack.Push(chipValue, ChipValueToTexture(chipValue));
                NeighborChipsInPlay.Add(newChipStack);
                var image = newChipStack.Peek().ChipImage;
                Add(image);
                image.Visible = false;
            }
        }
        /*
         * This overloaded method is the version that is always called from a mouse button click event. It checks where the user clicked the mouse
         * and chooses the most likely bet scheme that the user wants based on the position of the click. It sends this number list of the bet,
         * the exact location of where the chip would sit for the bet, as well as the bet amount based on the current dragged chip to the other
         * method of the same name.
         */
        private void PlaceBet(Vector2 proposedCoordinates)
        {
            var numbersList = VectorToNumbersList(proposedCoordinates);
            // actualCoordinates will be null if user clicked in bounds of the listening Rectangle, but not on the graph UIImage
            // CurrentDraggingChip *should* never be null here
            if (CurrentDraggingChip != null && numbersList != null)
            {
                VMEODRouletteBetTypes type = NumbersListToBetType(numbersList);
                Vector2 actualCoordinates = NumbersListToActualVector(numbersList);
                var chipValue = CurrentDraggingChip.Value;
                PlaceBet(chipValue, type, actualCoordinates, numbersList, false);
            }
            DropChip();
            ResumeBetting();
        }
        // A very appropriately named method to only set a select number of shadow box sprites to visible, by first making all visible, then some invisible
        private void ThrowSomeShade(params int[] doNotShadeList)
        {
            AddAllShade();
            if (doNotShadeList == null)
                return;
            if (doNotShadeList.Contains(0))
                ZeroBox.Visible = false;
            if (doNotShadeList.Contains(100))
                DoubleZeroBox.Visible = false;
            foreach (int number in doNotShadeList)
            {
                foreach (ShadowBox box in ShadowBoxArray)
                {
                    // remove shade for matching number(s)
                    if (box.Number == number)
                        box.UIHighlightSprite.Visible = false;
                }
            }
        }
        // update the textfield "Total Bets: $###" by recursively the value of each stack of chips
        private void UpdateTotalBets()
        {
            int totalBets = 0;
            foreach(var stack in MyChipsInPlay)
            {
                totalBets += stack.TotalStackValue;
            }
            CurrentRoundTotalBets = totalBets;
            labelTotalBets.Caption = GameFacade.Strings["UIText", "258", "19"].Replace("%d", "" + CurrentRoundTotalBets);
            EnableChipButtons();
        }
        // set all shadow boxes textures to visible
        private void AddAllShade()
        {
            ZeroBox.Visible = true;
            DoubleZeroBox.Visible = true;
            foreach (ShadowBox box in ShadowBoxArray)
            {
                box.UIHighlightSprite.Visible = true;
            }
        }
        // set all shadow box textures to invisible
        private void RemoveAllShade()
        {
            ZeroBox.Visible = false;
            DoubleZeroBox.Visible = false;
            foreach (ShadowBox box in ShadowBoxArray)
            {
                box.UIHighlightSprite.Visible = false;
            }
        }
        private void DisableChipButtons()
        {
            btnChip1.Visible = false;
            btnChip2.Visible = false;
            btnChip3.Visible = false;
            btnChip4.Visible = false;
            btnChip5.Visible = false;
        }
        // Don't allow the player to click a bet button if they can't afford the bet amount, or doing so would be over the table Max limit
        private void EnableChipButtons()
        {
            if (GameState.Equals(UIRouletteEODStates.Idle)) // only in betting phase
            {
                btnChip1.Visible = true;
                btnChip2.Visible = true;
                btnChip3.Visible = true;
                btnChip4.Visible = true;
                btnChip5.Visible = true;
                // validate buttons against player balance
                if (PlayerBalance - CurrentRoundTotalBets < 100)
                {
                    btnChip5.Visible = false;
                    if (PlayerBalance - CurrentRoundTotalBets < 25)
                    {
                        btnChip4.Visible = false;
                        if (PlayerBalance - CurrentRoundTotalBets < 10)
                        {
                            btnChip3.Visible = false;
                            if (PlayerBalance - CurrentRoundTotalBets < 5)
                            {
                                btnChip2.Visible = false;
                                if (PlayerBalance - CurrentRoundTotalBets < 1)
                                    btnChip1.Visible = false;
                            }
                        }
                    }
                }
                // validate buttons against max bet
                int remainingBetAllownace = MaxBet - CurrentRoundTotalBets;
                if (remainingBetAllownace < 100)
                {
                    btnChip5.Visible = false;
                    if (remainingBetAllownace < 25)
                    {
                        btnChip4.Visible = false;
                        if (remainingBetAllownace < 10)
                        {
                            btnChip3.Visible = false;
                            if (remainingBetAllownace < 5)
                            {
                                btnChip2.Visible = false;
                                if (remainingBetAllownace < 1)
                                    btnChip1.Visible = false;
                            }
                        }
                    }
                }
            }
        }
        // Gameover due to non-sufficient funds.
        private void GameOverNSF()
        {
            // while waiting for the server to send the alert that they're short on funds (non sufficient funds), disallow chip button interactions
            DisableChipButtons();

            // setting the state to managing will avoid the updates the EOD timer or unwanted state changes
            GameState = UIRouletteEODStates.Managing;
        }
    }
    /*
     * A simple container for a black rectangle drawn at each number's location at the table, big enough to cover one number, and fairly translucent.
     * These are made visible and invisble on demand in order to (inversely) highlight the numbers for which the user is considering a bet, based on the
     * position of their mouse point. Upon clicking a chip button and starting to drag a chip texture, all boxes are visible, darkening each number,
     * and the boxes become invisible as the user hovers over the different legal betting zones on the WheelTable.
     * @Cf. Class UIHighlightSprite in FSO.Client.UI.Controls.UISlotsImage.cs
     */
    public class ShadowBox
    {
        private int _Number;
        private bool _IsRed;
        private UIHighlightSprite _UIHighlightSprite;

        public ShadowBox(int number, bool isRed, UIHighlightSprite highlightsprite)
        {
            _Number = number;
            _IsRed = isRed;
            _UIHighlightSprite = highlightsprite;
            _UIHighlightSprite.InvalidateOpacity();
        }

        public int Number {
            get { return _Number; }
        }

        public bool IsRed
        {
            get { return _IsRed; }
        }

        public UIHighlightSprite UIHighlightSprite
        {
            get { return _UIHighlightSprite; }
        }
    }
    /*
     * This container holds all PlayChips and behaves like a "stack" data type. It keeps track of its location, total bet value and type, and no. of chips
     */ 
    public class ChipStack
    {
        private Vector2 _Position;
        private VMEODRouletteBetTypes _BetType;
        private int _ChipCount;
        private int _TotalStackValue;
        private List<PlayChip> _Chips = new List<PlayChip>();

        public ChipStack(Vector2 coords, VMEODRouletteBetTypes type)
        {
            _Position = coords;
            _BetType = type;
        }
        // the exact location of the stack of chips
        public Vector2 Position
        {
            get { return _Position; }
        }
        // have to set a chip count limit per stack
        public int ChipCount
        {
            get { return _ChipCount; }
        }
        // helpful for updating the total bets text field/label
        public int TotalStackValue
        {
            get { return _TotalStackValue; }
        }
        // cf. enum below, the bet type dictates the payout and the number of numbers to check for the winning roll
        public VMEODRouletteBetTypes BetType
        {
            get { return _BetType; }
        }
        // add a new chip to the stack using the texture and value provided
        public bool Push(int newChipValue, Texture2D texture)
        {
            if (_ChipCount < VMEODRoulettePlugin.GLOBAL_MAXIMUM_CHIPS_PER_STACK)
            {
                _Chips.Add(new PlayChip(newChipValue, texture, _Position, _ChipCount));
                _ChipCount++;
                _TotalStackValue += newChipValue;
                return true;
            }
            return false;
        }
        // take the chip off the stack, returning its image for removal and disposal
        public PlayChip Pop()
        {
            if (_Chips.Count > 0)
            {
                var chip = _Chips[_Chips.Count - 1];
                _Chips.RemoveAt(_Chips.Count - 1);
                _ChipCount--;
                _TotalStackValue -= chip.Value;
                return chip;
            }
            return null;
        }
        // need to access the image of the last chip pushed onto the stack for add/remove/visibility
        public PlayChip Peek()
        {
            if (_Chips.Count > 0)
                return _Chips[_Chips.Count - 1];
            else
                return null;
        }
        // check if the chip is a custom run-time created texture (with a mask), if so, mark it to be disposed when EOD closes or on a new game
        public UISlotsImage[] Dispose()
        {
            var images = new List<UISlotsImage>();
            foreach (var chip in _Chips)
            {
                images.Add(chip.ChipImage);
                if (chip.TextureNeedsDisposal == true)
                    chip.ChipImage.TextureNeedsDisposal = true;
            }
            if (images.Count > 0)
            {
                _ChipCount = 0;
                _TotalStackValue = 0;
                return images.ToArray();
            }
            return null;
        }
        // This will be useful for quickly and easily hiding chip stacks belonging to neighbor players on the table
         public void Hide()
        {
            foreach (var chip in _Chips)
            {
                chip.ChipImage.Visible = false;
            }
        }
        // This will be useful for quickly and easily showing chip stacks belonging to neighbor players on the table
        public void Show()
        {
            foreach (var chip in _Chips)
            {
                chip.ChipImage.Visible = true;
            }
        }
    }
    /*
     * This container holds the value of the bet with its corresponding texture all in one place. It also allows disposal of the texture.
     */
    public class PlayChip
    {
        private int _ChipValue;
        private UISlotsImage _ChipImage;
        private bool _TextureNeedsDisposal = false;

        public PlayChip(int value, Texture2D texture, Vector2 origin, int stackPosition)
        {
            _ChipValue = value;
            _ChipImage = new UISlotsImage(texture)
            {
                Position = origin + new Vector2(-1 * stackPosition * 0, -1 * stackPosition * 4) // left 1 and up 3 for each chip added to stack
            };
            _ChipImage.SetBounds(0,0,texture.Width, texture.Height);
            if (stackPosition > 0)
            {
                _TextureNeedsDisposal = true;
                _ChipImage.Texture = _ChipImage.ApplyCircleMask(texture, UIRouletteEOD.DRAG_CHIP_RADIUS,
                    new Vector2(UIRouletteEOD.DRAG_CHIP_CENTER_X, UIRouletteEOD.DRAG_CHIP_CENTER_Y));
            }
        }
        // the image needs to be accessible for adding, removing, hiding, and disposing of the texture
        public UISlotsImage ChipImage
        {
            get { return _ChipImage; }
        }
        // the value of the chip is either 1, 5, 10, 25, or 100
        public int Value
        {
            get { return _ChipValue; }
        }
        // if the texture was created and masked in order to block the alpha shadow in the asset file, it must be disposed when not in use
        public bool TextureNeedsDisposal
        {
            get { return _TextureNeedsDisposal; }
        }
        // flagging it here will cause it to be disposed in UISlotsImage when the EOD is closed
        public void Dispose()
        {
            if (_TextureNeedsDisposal == true)
                _ChipImage.TextureNeedsDisposal = true;
        }
    }
    /*
     * A very simple linked list for "spinning" the WheelImage by changing its X value
     */
    internal class RouletteWheelStateList
    {
        private RouletteWheelStateNode _Current;

        public RouletteWheelStateList(RouletteWheelStateNode current)
        {
            _Current = current;
        }
        public int Advance()
        {
            _Current = _Current.Next;
            return _Current.X;
        }
        public int Advance(int ultimateX)
        {
            if (_Current.Ultimate != null && _Current.Ultimate.X == ultimateX)
                _Current = _Current.Ultimate;
            else
                _Current = _Current.Next;
            return _Current.X;
        }
    }
    /*
     * A container for the x values for where to draw the wheel graphic, to make it appear to spin. It knows what x value should be next
     */
    internal class RouletteWheelStateNode
    {
        private RouletteWheelStateNode _Next; // each X value points to a simulated spinning wheel graphic
        private RouletteWheelStateNode _Ultimate; // one of the three X value options to show a non-spinning simulated graphic, for displaying the result
        private int _X; 

        public RouletteWheelStateNode(int x, RouletteWheelStateNode next)
        {
            _X = x;
            _Next = next;
        }
        public int X
        {
            get { return _X; }
        }
        public RouletteWheelStateNode Next
        {
            get { return _Next; }
            set { _Next = value; }
        }
        public RouletteWheelStateNode Ultimate
        {
            get { return _Ultimate; }
            set { _Ultimate = value; }
        }
    }
    // Spinning disallows betting and animates the wheel, Idle is the betting phase, Dragging allows a chip to follow the user's mouse pointer during betting
    public enum UIRouletteEODStates : byte
    {
            Spinning = 0,
            Idle = 1,
            Dragging = 2,
            Results = 3,
            Initializing = 4,
            Managing = 5
    }
}
