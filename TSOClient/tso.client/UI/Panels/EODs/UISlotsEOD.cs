using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.SimAntics.NetPlay.EODs;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.EODs
{
    public class UISlotsEOD : UIEOD
    {
        public UIScript Script;

        // EOD State
        public VMEODSlotsStates State;

        private byte MachineOdds { get; set; }
        private int eachBet;
        private int currentBet;
        private int displayedBet;
        private short machineBalance;
        private int currentPayout = 0;
        private bool targetsSet = false;
        private int onOffState;
        private int wheelSpinTickCounter = 0;
        private short machineMinimumBalance;
        private short machineMaximumBalance;
        private WheelStopsList wheelListOne;
        private WheelStopsList wheelListTwo;
        private WheelStopsList wheelListThree;
        private UILabel activePayoutTable;
        private Texture2D activeWheelTexture;

        // Owner UI Images
        public UIImage ButtonSeat { get; set; }
        public UIImage TextBack { get; set; }

        // Owner UI Buttons
        public UISlider OddsSlider { get; set; }
        public UIButton OnOffButton { get; set; }
        public UIButton CashOutButton { get; set; }

        // Owner UI Textfields
        public UITextEdit CurrentOdds { get; set; }
        public UITextEdit CashText { get; set; }
        public UILabel Odds { get; set; } // starts at opaque=0
        public UILabel House { get; set; } // starts at opaque=0
        public UILabel Player { get; set; } // starts at opaque=0
        public UILabel OnOff { get; set; } // starts at opaque=0

        // Player UI Images
        public UIImage Wheel1;
        public UIImage Wheel2;
        public UIImage Wheel3;
        public UIImage WinningLine { get; set; }

        public UIImage LightsFrame1 { get; set; }
        public UIImage LightsFrame2 { get; set; }
        public UIImage BetIndents { get; set; }
        public UIImage Wheelsback { get; set; }
        public UIImage Chips { get; set; }

        public UIImage PayoutTableColumn1Row1 { get; set; }
        public UIImage PayoutTableColumn1Row2 { get; set; }
        public UIImage PayoutTableColumn1Row3 { get; set; }
        public UIImage PayoutTableColumn1Row4 { get; set; }
        public UIImage PayoutTableColumn2Row1 { get; set; }
        public UIImage PayoutTableColumn2Row2 { get; set; }
        public UIImage PayoutTableColumn2Row3 { get; set; }
        public UIImage PayoutTableColumn2Row4 { get; set; }

        // Player UI Buttons
        public UIButton ArmButton { get; set; }
        public UIButton SpinButton { get; set; }
        public UIButton SpinnerIncreaseBet { get; set; }
        public UIButton SpinnerDecreaseBet { get; set; }

        // Player UI Textfields
        public UILabel PayoutTable1 { get; set; }
        public UILabel PayoutTable2 { get; set; }
        public UILabel PayoutTable3 { get; set; }
        public UITextEdit BetText { get; set; }
        public UITextEdit PayoutText1 { get; set; }
        public UITextEdit PayoutText2 { get; set; }
        public UITextEdit PayoutText3 { get; set; }
        public UITextEdit PayoutText4 { get; set; }
        public UITextEdit PayoutText5 { get; set; }
        public UITextEdit PayoutText6 { get; set; }
        public UITextEdit PayoutText7 { get; set; }
        public UITextEdit PayoutText8 { get; set; }
        public UILabel Loading { get; set; } // announces wins and losses

        // Textures
        public Texture2D Wheel1Image { get; set; }
        public Texture2D Wheel2Image { get; set; }
        public Texture2D Wheel3Image { get; set; }
        public Texture2D LightsFrame1Image { get; set; }
        public Texture2D LightsFrame2Image { get; set; }
        public Texture2D Wheel1LegendImage { get; set; }
        public Texture2D Wheel2LegendImage { get; set; }
        public Texture2D Wheel3LegendImage { get; set; }

        // texutre constants including positions of each slot stop in the texture file
        public const int WHEEL_TEXTURE_WIDTH_AND_HEIGHT = 58;
        public const int WHEEL_TEXTURE_HALF_DRAW_HEIGHT = 29;


        public UISlotsEOD(UIEODController controller) : base(controller)
        {
            Script = this.RenderScript("slotseod.uis");
            PlaintextHandlers["slots_animate_lights"] = LightsHandlers;
            PlaintextHandlers["slots_new_game"] = NewGameHandler;
            PlaintextHandlers["slots_animate_wheels"] = AnimateWheelsHandler;
            PlaintextHandlers["slots_display_win"] = DisplayWinHandler;
            PlaintextHandlers["slots_display_loss"] = DisplayLossHandler;
            PlaintextHandlers["slots_toggle_offline_message"] = OfflineMessageHandler;
            PlaintextHandlers["slots_resume_manage"] = ResumeManageHandler;
            PlaintextHandlers["slots_deposit_fail"] = DepositFailHandler;
            BinaryHandlers["slots_owner_init"] = OwnerInitHandler;
            BinaryHandlers["slots_player_init"] = PlayerInitHandler;
            BinaryHandlers["slots_off_init"] = PlayerInitHandler;
            BinaryHandlers["slots_spin"] = SlotsSpinHandler;

            // Add message text
            Loading.Alignment = TextAlignment.Left;
            Loading.Caption = GameFacade.Strings["UIText", "259", "18"];
        }
        public override void OnExpand()
        {
            activePayoutTable.Visible = true;
            PayoutText1.Visible = true;
            PayoutText2.Visible = true;
            PayoutText3.Visible = true;
            PayoutText4.Visible = true;
            PayoutText5.Visible = true;
            PayoutText6.Visible = true;
            PayoutText7.Visible = true;
            PayoutText8.Visible = true;
            PayoutTableColumn1Row1.Visible = true;
            PayoutTableColumn1Row2.Visible = true;
            PayoutTableColumn1Row3.Visible = true;
            PayoutTableColumn1Row4.Visible = true;
            PayoutTableColumn2Row1.Visible = true;
            PayoutTableColumn2Row2.Visible = true;
            PayoutTableColumn2Row3.Visible = true;
            PayoutTableColumn2Row4.Visible = true;
            Loading.Y -= 135;
            base.OnExpand();
        }
        public override void OnContract()
        {
            activePayoutTable.Visible = false;
            PayoutText1.Visible = false;
            PayoutText2.Visible = false;
            PayoutText3.Visible = false;
            PayoutText4.Visible = false;
            PayoutText5.Visible = false;
            PayoutText6.Visible = false;
            PayoutText7.Visible = false;
            PayoutText8.Visible = false;
            PayoutTableColumn1Row1.Visible = false;
            PayoutTableColumn1Row2.Visible = false;
            PayoutTableColumn1Row3.Visible = false;
            PayoutTableColumn1Row4.Visible = false;
            PayoutTableColumn2Row1.Visible = false;
            PayoutTableColumn2Row2.Visible = false;
            PayoutTableColumn2Row3.Visible = false;
            PayoutTableColumn2Row4.Visible = false;
            Loading.Y += 135;
            base.OnContract();
        }
        public override void OnClose()
        {
            Send("slots_close_UI", "");
            base.OnClose();
        }
        private void PlayerInitHandler(string evt, byte [] args)
        {
            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Height = EODHeight.TallTall,
                Length = EODLength.Full,
                Tips = EODTextTips.Short,
                Timer = EODTimer.None,
                Expandable = true,
                Expanded = true
            });
            // hide owner UI elements
            OddsSlider.Visible = false;
            OnOffButton.Visible = false;
            CashOutButton.Visible = false;
            Odds.Visible = false;
            House.Visible = false;
            Player.Visible = false;
            OnOff.Visible = false;
            CashText.Visible = false;

            // move loading
            Loading.X -= 128;
            Loading.Y -= 20;

            // create player UI iamges
            Wheelsback = Script.Create<UIImage>("WheelsBack");
            AddAt(0, Wheelsback);
            LightsFrame1 = Script.Create<UIImage>("LightsFrame1");
            AddAt(1,LightsFrame1);
            LightsFrame2 = Script.Create<UIImage>("LightsFrame2");
            AddAt(2,LightsFrame2);
            BetIndents = Script.Create<UIImage>("BetIndents");
            AddAt(3,BetIndents);
            Chips = Script.Create<UIImage>("Chips");
            Add(Chips);

            // Customise and place payout table
            PayoutTableColumn1Row1 = Script.Create<UIImage>("PayoutTableColumn1").TripleTextureDraw(0, 0, 15, 21, 0, 0, 15, 21, 0, 0, 15, 21, true, false);
            Add(PayoutTableColumn1Row1);
            PayoutTableColumn1Row2 = Script.Create<UIImage>("PayoutTableColumn1").TripleTextureDraw(15, 0, 15, 21, 15, 0, 15, 21, 15, 0, 15, 21, true, false);
            PayoutTableColumn1Row2.Y += 21;
            Add(PayoutTableColumn1Row2);
            PayoutTableColumn1Row3 = Script.Create<UIImage>("PayoutTableColumn1").TripleTextureDraw(30, 0, 15, 21, 30, 0, 15, 21, 30, 0, 15, 21, true, false);
            PayoutTableColumn1Row3.Y += 42;
            Add(PayoutTableColumn1Row3);
            PayoutTableColumn1Row4 = Script.Create<UIImage>("PayoutTableColumn1").TripleTextureDraw(45, 0, 15, 21, 45, 0, 15, 21, 45, 0, 15, 21, true, false);
            PayoutTableColumn1Row4.Y += 63;
            Add(PayoutTableColumn1Row4);
            PayoutTableColumn2Row1 = Script.Create<UIImage>("PayoutTableColumn2").TripleTextureDraw(60, 0, 15, 21, 60, 0, 15, 21, 60, 0, 15, 21, true, false);
            Add(PayoutTableColumn2Row1);
            PayoutTableColumn2Row2 = Script.Create<UIImage>("PayoutTableColumn2").TripleTextureDraw(45, 0, 15, 21, 60, 0, 15, 21, 75, 0, 15, 21, true, false);
            PayoutTableColumn2Row2.Y += 21;
            Add(PayoutTableColumn2Row2);
            PayoutTableColumn2Row3 = Script.Create<UIImage>("PayoutTableColumn2").DoubleTextureDraw(75, 0, 15, 21, 75, 0, 15, 21, true, false);
            PayoutTableColumn2Row3.Y += 42;
            Add(PayoutTableColumn2Row3);
            PayoutTableColumn2Row4 = Script.Create<UIImage>("PayoutTableColumn2");
            PayoutTableColumn2Row4.SetBounds(75, 0, 15, 21);
            PayoutTableColumn2Row4.Y += 63;
            Add(PayoutTableColumn2Row4);

            // initialize payout textfields, which are currently ubiquitous across all slot machines
            PayoutText1.Y = PayoutTableColumn1Row1.Y - 1;
            PayoutText1.X = PayoutTableColumn1Row1.X + 55;
            PayoutText1.CurrentText = GameFacade.Strings["UIText","259", "36"];
            PayoutText1.CurrentText = PayoutText1.CurrentText.Replace("%i", "" + VMEODSlotsPlugin.SIX_SIX_SIX_PAYOUT_MULTIPLIER);
            PayoutText1.Mode = UITextEditMode.ReadOnly;
            Add(PayoutText1);

            PayoutText2.Y = PayoutTableColumn1Row2.Y - 1;
            PayoutText2.X = PayoutTableColumn1Row2.X + 55;
            PayoutText2.CurrentText = GameFacade.Strings["UIText", "259", "36"];
            PayoutText2.CurrentText = PayoutText2.CurrentText.Replace("%i", "" + VMEODSlotsPlugin.FIVE_FIVE_FIVE_PAYOUT_MULTIPLIER);
            PayoutText2.Mode = UITextEditMode.ReadOnly;
            Add(PayoutText2);

            PayoutText3.Y = PayoutTableColumn1Row3.Y - 1;
            PayoutText3.X = PayoutTableColumn1Row3.X + 55;
            PayoutText3.CurrentText = GameFacade.Strings["UIText", "259", "36"];
            PayoutText3.CurrentText = PayoutText3.CurrentText.Replace("%i", "" + VMEODSlotsPlugin.FOUR_FOUR_FOUR_PAYOUT_MULTIPLIER);
            PayoutText3.Mode = UITextEditMode.ReadOnly;
            Add(PayoutText3);

            PayoutText4.Y = PayoutTableColumn1Row4.Y - 1;
            PayoutText4.X = PayoutTableColumn1Row4.X + 55;
            PayoutText4.CurrentText = GameFacade.Strings["UIText", "259", "36"];
            PayoutText4.CurrentText = PayoutText4.CurrentText.Replace("%i", "" + VMEODSlotsPlugin.THREE_THREE_THREE_PAYOUT_MULTIPLIER);
            PayoutText4.Mode = UITextEditMode.ReadOnly;
            Add(PayoutText4);

            PayoutText5.Y = PayoutTableColumn2Row1.Y - 1;
            PayoutText5.X = PayoutTableColumn2Row1.X + 55;
            PayoutText5.CurrentText = GameFacade.Strings["UIText", "259", "36"];
            PayoutText5.CurrentText = PayoutText5.CurrentText.Replace("%i", "" + VMEODSlotsPlugin.TWO_TWO_TWO_PAYOUT_MULTIPLIER);
            PayoutText5.Mode = UITextEditMode.ReadOnly;
            Add(PayoutText5);

            PayoutText6.Y = PayoutTableColumn2Row2.Y - 1;
            PayoutText6.X = PayoutTableColumn2Row2.X + 55;
            PayoutText6.CurrentText = GameFacade.Strings["UIText", "259", "36"];
            PayoutText6.CurrentText = PayoutText6.CurrentText.Replace("%i", "" + VMEODSlotsPlugin.THREE_TWO_ONE_PAYOUT_MULTIPLIER);
            PayoutText6.Mode = UITextEditMode.ReadOnly;
            Add(PayoutText6);

            PayoutText7.Y = PayoutTableColumn2Row3.Y - 1;
            PayoutText7.X = PayoutTableColumn2Row3.X + 55;
            PayoutText7.CurrentText = GameFacade.Strings["UIText", "259", "36"];
            PayoutText7.CurrentText = PayoutText7.CurrentText.Replace("%i", "" + VMEODSlotsPlugin.ONE_ONE_ANY_PAYOUT_MULTIPLIER);
            PayoutText7.Mode = UITextEditMode.ReadOnly;
            Add(PayoutText7);

            PayoutText8.Y = PayoutTableColumn2Row4.Y - 1;
            PayoutText8.X = PayoutTableColumn2Row4.X + 55;
            PayoutText8.CurrentText = GameFacade.Strings["UIText", "259", "36"];
            PayoutText8.CurrentText = PayoutText8.CurrentText.Replace("%i", "" + VMEODSlotsPlugin.ONE_ANY_ANY_PAYOUT_MULTIPLIER);
            PayoutText8.Mode = UITextEditMode.ReadOnly;
            Add(PayoutText8);

            // create the wheel lists for the spinning
            wheelListOne = new WheelStopsList();
            wheelListTwo = new WheelStopsList();
            wheelListThree = new WheelStopsList();

            // the wheel textures are customized at a later time, but draw the initial pre-gameplay stops (sixth sixth sixth)
            Wheel1 = Script.Create<UIImage>("Wheel1").DoubleTextureDraw(0, wheelListOne.current.myStartingY, WHEEL_TEXTURE_WIDTH_AND_HEIGHT,
                WHEEL_TEXTURE_HALF_DRAW_HEIGHT, 0, wheelListOne.current.myStartingY + WHEEL_TEXTURE_HALF_DRAW_HEIGHT,
                WHEEL_TEXTURE_WIDTH_AND_HEIGHT, WHEEL_TEXTURE_HALF_DRAW_HEIGHT, false, true);
            Wheel2 = Script.Create<UIImage>("Wheel2").DoubleTextureDraw(0, wheelListTwo.current.myStartingY, WHEEL_TEXTURE_WIDTH_AND_HEIGHT,
                WHEEL_TEXTURE_HALF_DRAW_HEIGHT, 0, wheelListTwo.current.myStartingY + WHEEL_TEXTURE_HALF_DRAW_HEIGHT,
                WHEEL_TEXTURE_WIDTH_AND_HEIGHT, WHEEL_TEXTURE_HALF_DRAW_HEIGHT, false, true);
            Wheel3 = Script.Create<UIImage>("Wheel3").DoubleTextureDraw(0, wheelListThree.current.myStartingY, WHEEL_TEXTURE_WIDTH_AND_HEIGHT,
                WHEEL_TEXTURE_HALF_DRAW_HEIGHT, 0, wheelListThree.current.myStartingY + WHEEL_TEXTURE_HALF_DRAW_HEIGHT,
                WHEEL_TEXTURE_WIDTH_AND_HEIGHT, WHEEL_TEXTURE_HALF_DRAW_HEIGHT, false, true);

            Add(Wheel1);
            Add(Wheel2);
            Add(Wheel3);
            WinningLine = Script.Create<UIImage>("WinningLine");
            Add(WinningLine);

            MachineTypeInit(args[1]);

            if (args[2] == 1)
            {
                // add button listeners if the machine is on, args[2] == 0 if it's off
                AddPlayerListeners();
            }
        }

        private void OwnerInitHandler(string evt, byte[] args)
        {
            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Height = EODHeight.Normal,
                Length = EODLength.Full,
                Tips = EODTextTips.Short,
                Timer = EODTimer.None,
                Expandable = false
            });

            // create owner UIImages
            ButtonSeat = Script.Create<UIImage>("ButtonSeat");
            TextBack = Script.Create<UIImage>("TextBack");
            AddAt(0, ButtonSeat);
            AddAt(0, TextBack);

            // hide player UI elements
            ArmButton.Visible = false;
            SpinButton.Visible = false;
            SpinnerIncreaseBet.Visible = false;
            SpinnerDecreaseBet.Visible = false;
            PayoutTable1.Visible = false;
            PayoutTable2.Visible = false;
            PayoutTable3.Visible = false;
            BetText.Visible = false;
            PayoutText1.Visible = false;
            PayoutText2.Visible = false;
            PayoutText3.Visible = false;
            PayoutText4.Visible = false;
            PayoutText5.Visible = false;
            PayoutText6.Visible = false;
            PayoutText7.Visible = false;
            PayoutText8.Visible = false;

            // Move the messages, tweaks
            Loading.X = 72;
            Loading.Y = 6;
            Loading.Caption = GameFacade.Strings["UIText", "259", "24"];
            OnOff.Y = Odds.Y;
            OnOff.X += 15;
            OnOffButton.X += 15;
            Odds.X += 18;
            Player.X += 10;
            House.X += 10;
            OddsSlider.X += 25;

            // Set the Odds slider
            MachineOdds = args[0];
            OddsSlider.Tooltip = MachineOdds + "%";
            OddsSlider.MinValue = 80;
            OddsSlider.MaxValue = 110;
            OddsSlider.Value = MachineOdds = args[0];
            CurrentOdds = new UITextEdit();
            CurrentOdds.Size = new Microsoft.Xna.Framework.Vector2(45, 20);
            CurrentOdds.Y = House.Y;
            CurrentOdds.X = Odds.X + 46;
            CurrentOdds.Mode = UITextEditMode.ReadOnly;
            CurrentOdds.CurrentText = MachineOdds + "%";
            Add(CurrentOdds);
            Player.Y = House.Y = CashText.Y;
            OddsSlider.Y += 18;

            // initiate OnOffButton
            OnOffButton.ForceState = onOffState = args[3];
            OnOffButton.Tooltip = GameFacade.Strings["UIText", "259", "14"];
            if (args[3] == 0)
            {
                OnOffButton.Tooltip = GameFacade.Strings["UIText", "259", "13"];
            }
            else
            {
                OnOffButton.Tooltip = GameFacade.Strings["UIText", "259", "14"];
            }

            // calculate the money in the machine from the two shorts and populate textField
            machineBalance = Convert.ToInt16((255 * args[2]) + args[1]);
            CashText.Alignment = TextAlignment.Center;
            CashText.Mode = UITextEditMode.ReadOnly;
            CashText.CurrentText = "$" + machineBalance;

            // add click listeners
            OnOffButton.OnButtonClick += OnOffHandler;
            OddsSlider.OnChange += OddsChangeHandler;
            CashOutButton.OnButtonClick += CashoutButtonHandler;

            // get the minimum and maximum balances based on the machine type
            switch (args[4])
            {
                case 0:
                    machineMinimumBalance = (short)VMEODSlotMachineMinimumBalances.Viva_PGT;
                    machineMaximumBalance = (short)VMEODSlotMachineMaximumBalances.Viva_PGT;
                    break;
                case 1:
                    machineMinimumBalance = (short)VMEODSlotMachineMinimumBalances.Gypsy_Queen;
                    machineMaximumBalance = (short)VMEODSlotMachineMaximumBalances.Gypsy_Queen;
                    break;
                default:
                    machineMinimumBalance = (short)VMEODSlotMachineMinimumBalances.Jack_of_Hearts;
                    machineMaximumBalance = (short)VMEODSlotMachineMaximumBalances.Jack_of_Hearts;
                    break;
            }
        }
        private void CashoutButtonHandler(UIElement targetButton)
        {
            CashOutButton.OnButtonClick -= CashoutButtonHandler;

            // show an alert that asks the user if they want to make a desposit or a withdrawal
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = "Owner Transactions",
                Message = "What would you like to do?",
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = new UIAlertButton[] {
                    new UIAlertButton (UIAlertButtonType.OK, ((btn1) =>
                {
                    DepositPrompt();
                    UIScreen.RemoveDialog(alert);
                }), "Deposit"),
                    new UIAlertButton (UIAlertButtonType.Cancel, ((btn2) =>
                {
                    WithdrawPrompt();
                    UIScreen.RemoveDialog(alert);
                }), "Withdraw")
                }
            }, true);
        }
        private void DepositPrompt()
        {
            // show an alert that asks the user how much to deposit into the machine
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = "Deposit Simoleons",
                Message = "This machine is currently stocked with: $" + machineBalance + System.Environment.NewLine +
                System.Environment.NewLine + "For players to use this machine you must maintain a minimum balance of: $" + machineMinimumBalance +
                System.Environment.NewLine + System.Environment.NewLine + "How much would you like to deposit?" +
                System.Environment.NewLine + System.Environment.NewLine + "(This machine cannot hold more than: $" + machineMaximumBalance + ")",
                Alignment = TextAlignment.Left,
                TextEntry = true,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    InputHandler("d", alert.ResponseText.Trim());
                    UIScreen.RemoveDialog(alert);
                }),

            }, true);
        }
        private void WithdrawPrompt()
        {
            // show an alert that asks the user how much to withdraw from the machine
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = "Withdraw Simoleons",
                Message = "This machine is currently stocked with: $" + machineBalance + System.Environment.NewLine +
                System.Environment.NewLine + "For players to use this machine you must maintain a minimum balance of: $" + machineMinimumBalance +
                System.Environment.NewLine + System.Environment.NewLine + "How much would you like to withdraw?",
                Alignment = TextAlignment.Left,
                TextEntry = true,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    InputHandler("w", alert.ResponseText.Trim());
                    UIScreen.RemoveDialog(alert);
                }),

            }, true);
        }
        private void InputHandler(string type, string userInput)
        {
            short amount;
            userInput.Replace("-", ""); // in case any jokesters try to input a negative number
            string eventName = "";
            string eventMessage = "";
            // try to parse the user's input
            try
            {
                amount = Int16.Parse(userInput);
                // input is valid, now check it against machineBalance
                if (amount == 0)
                {
                    eventName = null;
                    eventMessage = "null";
                }
                else if (type.Equals("w")) { // withdrawing
                    if (amount > machineBalance)
                    {
                        eventName = null;
                        eventMessage = "overflow";
                    }
                    else
                    {
                        eventName = "slots_withdraw";
                        eventMessage = "" + amount;
                    }
                }
                else // depositing
                {
                    if ((amount + machineBalance) > machineMaximumBalance)
                    {
                        eventName = null;
                        eventMessage = "overflow";
                    }
                    else
                    {
                        eventName = "slots_deposit";
                        eventMessage = "" + amount;
                    }
                }
            }
            catch (ArgumentNullException nullException)
            {
                eventName = null;
                eventMessage = "null";
            }
            catch (FormatException formatException)
            {
                eventName = null;
                if (userInput.Length == 0)
                    eventMessage = "null";
                else
                    eventMessage = "invalid";
            }
            catch (OverflowException overFlowException)
            {
                eventName = null;
                eventMessage = "overflow";
            }
            finally
            {
                if (eventName != null)
                    Send(eventName, eventMessage);
                else
                    InputFailHandler(type, eventMessage);
            }
        }
        private void InputFailHandler(string transactionType, string failureReason)
        {
            string message = "";
            if (failureReason.Equals("null"))
            {
                ResumeManageHandler("slots_resume_manage", "0");
                return;
            }
            else if (failureReason.Equals("invalid"))
                message = "That is not a valid number!";
            else if (failureReason.Equals("overflow"))
            {
                if (transactionType.Equals("w"))
                    message = "You cannot withdraw more than the balance of the machine!";
                else
                    message = "You cannot deposit that many simoleons because the machine can only hold: $" + machineMaximumBalance;
            }
            else
                message = "An unknown error occured.";
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = "Transaction Error",
                Message = message,
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    ResumeManageHandler("slots_resume_manage", "0");
                    UIScreen.RemoveDialog(alert);
                }),

            }, true);
        }
        private void DepositFailHandler(string evt, string amountString)
        {
            // show an alert that informs the user that they don't have the money to make the deposit
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = "Transaction Error",
                Message = "You don't have enough simoleons to deposit: $" + amountString,
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    ResumeManageHandler("slots_resume_manage", "0");
                    UIScreen.RemoveDialog(alert);
                }),

            }, true);
        }
        private void ResumeManageHandler(string evt, string balance)
        {
            short debitedAmount = Int16.Parse(balance);
            machineBalance -= debitedAmount;
            CashText.CurrentText = "$" + machineBalance;
            CashOutButton.OnButtonClick += CashoutButtonHandler;
        }
        private void OddsChangeHandler(UIElement targetSlider)
        {
            OddsSlider.OnChange -= OddsChangeHandler;
            MachineOdds = Convert.ToByte(OddsSlider.Value);
            Send("slots_new_odds", new Byte[] { MachineOdds });
            OddsSlider.Tooltip = MachineOdds + "%";
            CurrentOdds.CurrentText = MachineOdds + "%";
            OddsSlider.OnChange += OddsChangeHandler;
        }
        private void OnOffHandler(UIElement targetButton)
        {
            OnOffButton.Disabled = true;
            if (onOffState == 0)
            {
                OnOffButton.ForceState = onOffState = 1;
                OnOffButton.Tooltip = GameFacade.Strings["UIText", "259", "14"];
            }
            else
            {
                OnOffButton.ForceState = onOffState = 0;
                OnOffButton.Tooltip = GameFacade.Strings["UIText", "259", "13"];
            }
            Send("slots_toggle_onOff", "" + onOffState);
            OnOffButton.Disabled = false;
        }
        private void NewGameHandler(String evt, String message)
        {
            currentPayout = 0;
            wheelSpinTickCounter = 0;
            AddPlayerListeners();
            wheelListOne.Reset();
            wheelListTwo.Reset();
            wheelListThree.Reset();
        }
        private void BetIncreaseHandler(UIElement targetButton)
        {
            RemovePlayerListeners();
            if (currentBet == 5)
            {
                // do nothing, cannot bet more than 5 coins
            }
            else
            {
                currentBet++;
                displayedBet = currentBet * eachBet;
                UpdateBetText();
            }
            AddPlayerListeners();
        }
        private void BetDecreaseHandler(UIElement targetButton)
        {
            RemovePlayerListeners();
            if (currentBet == 1)
            {
                // do nothing, cannot bet less than 1 coin
            }
            else
            {
                currentBet--;
                displayedBet = currentBet * eachBet;
                UpdateBetText();
            }
            AddPlayerListeners();
        }
        private void SpinHandler(UIElement targetButton)
        {
            RemovePlayerListeners();
            Send("slots_execute_bet", "" + displayedBet);
        }
        private void SlotsSpinHandler(string evt, Byte [] WinningsandTargetStops)
        {
            // get currentPayout despite Byte constraints
            currentPayout = Convert.ToInt32((WinningsandTargetStops[0] * 255) + WinningsandTargetStops[1]);

            // update text field
            Loading.Caption = GameFacade.Strings["UIText", "259", "21"];
            Loading.Caption = Loading.Caption.Replace("%i", "" + displayedBet);

            // get the three target stops, recast them as type EMOEODSlotsStops
            /*wheelListOne.targetStop = (EMOEDSlotsStops)Enum.Parse(typeof(EMOEDSlotsStops), (Enum.GetName(typeof(EMOEDSlotsStops),
                WinningsandTargetStops[2]))); depricated approach */
            wheelListOne.targetStop = (VMEODSlotsStops)Enum.ToObject(typeof(VMEODSlotsStops), WinningsandTargetStops[2]);
            wheelListTwo.targetStop = (VMEODSlotsStops)Enum.ToObject(typeof(VMEODSlotsStops), WinningsandTargetStops[3]);
            wheelListThree.targetStop = (VMEODSlotsStops)Enum.ToObject(typeof(VMEODSlotsStops), WinningsandTargetStops[4]);

            targetsSet = true;
        }
        private void DisplayLossHandler(string evt, string stringNumber)
        {
            Loading.Caption = GameFacade.Strings["UIText", "259", stringNumber];
        }
        private void DisplayWinHandler(string evt, string stringNumber)
        {
            var data = stringNumber.Split('%');
            Loading.Caption = GameFacade.Strings["UIText", "259", data[0]];
            Loading.Caption = Loading.Caption.Replace("%i", data[1]);
        }
        private void AnimateWheelsHandler(string evt, string message)
        {
            if (targetsSet == false)
                return;

            wheelSpinTickCounter++;

            if (wheelSpinTickCounter >= 150)
            {
                if (wheelSpinTickCounter == 150)
                {
                    // start spinning wheel1
                    wheelListOne.IncrementOffsetY(7);
                }
                else if (wheelSpinTickCounter == 151)
                {
                    wheelListOne.IncrementOffsetY(7);
                }
                else if (wheelSpinTickCounter == 152)
                {
                    // start spinning wheel2
                    wheelListTwo.IncrementOffsetY(7);
                    wheelListOne.IncrementOffsetY(12);
                }
                else if (wheelSpinTickCounter == 153)
                {
                    wheelListTwo.IncrementOffsetY(7);
                    wheelListOne.IncrementOffsetY(12);
                }
                else if (wheelSpinTickCounter == 154)
                {
                    // start spinning wheel3
                    wheelListThree.IncrementOffsetY(7);
                    wheelListTwo.IncrementOffsetY(12);
                    wheelListOne.IncrementOffsetY(17);
                }
                else if (wheelSpinTickCounter == 155)
                {
                    wheelListThree.IncrementOffsetY(7);
                    wheelListTwo.IncrementOffsetY(12);
                    wheelListOne.IncrementOffsetY(17);
                }
                else if (wheelSpinTickCounter == 156)
                {
                    wheelListThree.IncrementOffsetY(12);
                    wheelListTwo.IncrementOffsetY(17);
                    wheelListOne.IncrementOffsetY(22);
                }
                else if (wheelSpinTickCounter == 157)
                {
                    wheelListThree.IncrementOffsetY(12);
                    wheelListTwo.IncrementOffsetY(17);
                    wheelListOne.IncrementOffsetY(22);
                }
                else if (wheelSpinTickCounter == 158)
                {
                    wheelListThree.IncrementOffsetY(17);
                    wheelListTwo.IncrementOffsetY(22);
                    wheelListOne.IncrementOffsetY(29);
                }
                else if (wheelSpinTickCounter == 159)
                {
                    wheelListThree.IncrementOffsetY(17);
                    wheelListTwo.IncrementOffsetY(22);
                    wheelListOne.IncrementOffsetY(29);
                }
                else if (wheelSpinTickCounter == 160)
                {
                    wheelListThree.IncrementOffsetY(22);
                    wheelListTwo.IncrementOffsetY(29);
                    wheelListOne.IncrementOffsetY(29);
                }
                else if (wheelSpinTickCounter == 161)
                {
                    wheelListThree.IncrementOffsetY(22);
                    wheelListTwo.IncrementOffsetY(29);
                    wheelListOne.IncrementOffsetY(29);
                }
                else if (wheelSpinTickCounter == 186)
                {
                    // start slowing wheel1
                    wheelListOne.SlowDown();
                    wheelListThree.IncrementOffsetY(29);
                    wheelListTwo.IncrementOffsetY(29);
                    wheelListOne.IncrementOffsetY(29);
                }
                else if (wheelSpinTickCounter == 211)
                {
                    // start slowing wheel2
                    wheelListTwo.SlowDown();
                    wheelListThree.IncrementOffsetY(29);
                    wheelListTwo.IncrementOffsetY(29);
                    wheelListOne.IncrementOffsetY(29);
                }
                else if (wheelSpinTickCounter == 236)
                {
                    // start slowing wheel3
                    wheelListThree.SlowDown();
                    wheelListThree.IncrementOffsetY(29);
                    wheelListTwo.IncrementOffsetY(29);
                    wheelListOne.IncrementOffsetY(29);
                }
                // 30 ticks times 9 seconds = 270
                else if (wheelSpinTickCounter == 261)
                {
                    // tell the plugin that the wheels have stopped spinning
                    Send("slots_wheels_stopped", "" + currentPayout);
                }
                else // 150 < wheelSpinTickCounter > 260
                {
                    wheelListThree.IncrementOffsetY(29);
                    wheelListTwo.IncrementOffsetY(29);
                    wheelListOne.IncrementOffsetY(29);
                }
                DrawWheelStops();
            }
        }
        /*
         * The EOD calls this on tick or a timer based on ticks to simulate the flashing of the lights
         */
        private void LightsHandlers(string evt, string str)
        {
            if (LightsFrame2 == null) { return; }
            else if (LightsFrame2.Visible == true)
                LightsFrame2.Visible = false;
            else
                LightsFrame2.Visible = true;
        }
        private void OfflineMessageHandler(string evt, string str)
        {
            BetText.Visible = false;
            if (Loading.Caption.Equals(GameFacade.Strings["UIText", "259", "22"]))
            {
                Loading.Caption = GameFacade.Strings["UIText", "259", "23"];
            }
            else
            {
                Loading.Caption = GameFacade.Strings["UIText", "259", "22"];
            }
        }
        private void DrawWheelStops()
        {
            var oldWheel1 = Wheel1;
            var oldWheel2 = Wheel2;
            var oldWheel3 = Wheel3;

            // remove old wheels from behind new ones to avoid the 'flickering' effect
            Remove(oldWheel1);
            Remove(oldWheel2);
            Remove(oldWheel3);

            if (wheelListOne.OffsetY >= 0)
            {
                Wheel1 = Script.Create<UIImage>("Wheel1").DoubleTextureDraw(0, wheelListOne.next.myStartingY +
                    (WHEEL_TEXTURE_WIDTH_AND_HEIGHT - wheelListOne.OffsetY), WHEEL_TEXTURE_WIDTH_AND_HEIGHT, wheelListOne.OffsetY,
                    0, wheelListOne.current.myStartingY, WHEEL_TEXTURE_WIDTH_AND_HEIGHT, (WHEEL_TEXTURE_WIDTH_AND_HEIGHT - wheelListOne.OffsetY),
                    false, true);
            }
            else // wheel is spinning backwards as it stops
            {
                Wheel1 = Script.Create<UIImage>("Wheel1").DoubleTextureDraw(0, wheelListOne.current.myStartingY + (-1 * wheelListOne.OffsetY),
                    WHEEL_TEXTURE_WIDTH_AND_HEIGHT, (WHEEL_TEXTURE_WIDTH_AND_HEIGHT - (-1 * wheelListOne.OffsetY)),
                    0, wheelListOne.previous.myStartingY, WHEEL_TEXTURE_WIDTH_AND_HEIGHT, (-1 * wheelListOne.OffsetY), false, true);
            }
            Wheel1.Texture = activeWheelTexture;
            AddBefore(Wheel1, WinningLine);
            if (wheelListTwo.OffsetY >= 0)
            {
                Wheel2 = Script.Create<UIImage>("Wheel2").DoubleTextureDraw(0, wheelListTwo.next.myStartingY +
                (WHEEL_TEXTURE_WIDTH_AND_HEIGHT - wheelListTwo.OffsetY), WHEEL_TEXTURE_WIDTH_AND_HEIGHT, wheelListTwo.OffsetY,
                0, wheelListTwo.current.myStartingY, WHEEL_TEXTURE_WIDTH_AND_HEIGHT, (WHEEL_TEXTURE_WIDTH_AND_HEIGHT - wheelListTwo.OffsetY),
                false, true);
            }
            else // wheel is spinning backwards as it stops
            {
                Wheel2 = Script.Create<UIImage>("Wheel1").DoubleTextureDraw(0, wheelListTwo.current.myStartingY + (-1 * wheelListTwo.OffsetY),
                    WHEEL_TEXTURE_WIDTH_AND_HEIGHT, (WHEEL_TEXTURE_WIDTH_AND_HEIGHT - (-1 * wheelListTwo.OffsetY)),
                    0, wheelListTwo.previous.myStartingY, WHEEL_TEXTURE_WIDTH_AND_HEIGHT, (-1 * wheelListTwo.OffsetY), false, true);
            }
            Wheel2.Texture = activeWheelTexture;
            AddBefore(Wheel2, WinningLine);
            if (wheelListThree.OffsetY >= 0)
            {
                Wheel3 = Script.Create<UIImage>("Wheel3").DoubleTextureDraw(0, wheelListThree.next.myStartingY +
                 (WHEEL_TEXTURE_WIDTH_AND_HEIGHT - wheelListThree.OffsetY), WHEEL_TEXTURE_WIDTH_AND_HEIGHT, wheelListThree.OffsetY,
                 0, wheelListThree.current.myStartingY, WHEEL_TEXTURE_WIDTH_AND_HEIGHT, (WHEEL_TEXTURE_WIDTH_AND_HEIGHT - wheelListThree.OffsetY),
                 false, true);
            }
            else // wheel is spinning backwards as it stops
            {
                Wheel3 = Script.Create<UIImage>("Wheel1").DoubleTextureDraw(0, wheelListThree.current.myStartingY + (-1 * wheelListThree.OffsetY),
                    WHEEL_TEXTURE_WIDTH_AND_HEIGHT, (WHEEL_TEXTURE_WIDTH_AND_HEIGHT - (-1 * wheelListThree.OffsetY)),
                    0, wheelListThree.previous.myStartingY, WHEEL_TEXTURE_WIDTH_AND_HEIGHT, (-1 * wheelListThree.OffsetY), false, true);
            }
            Wheel3.Texture = activeWheelTexture;
            AddBefore(Wheel3, WinningLine);
        }
        /*
         * @param machineGrade: '0' is $1 slot machine, '1' is $5, '2' is $10
         */
        private void MachineTypeInit(byte machineGrade)
        {
            // customise wheel textures by machine type
            switch (machineGrade)
            {
                case 0:
                    Wheel1.Texture = Wheel1Image;
                    Wheel2.Texture = Wheel1Image;
                    Wheel3.Texture = Wheel1Image;
                    activeWheelTexture = Wheel1Image;
                    PayoutTableColumn1Row1.Texture = Wheel1LegendImage;
                    PayoutTableColumn1Row2.Texture = Wheel1LegendImage;
                    PayoutTableColumn1Row3.Texture = Wheel1LegendImage;
                    PayoutTableColumn1Row4.Texture = Wheel1LegendImage;
                    PayoutTableColumn2Row1.Texture = Wheel1LegendImage;
                    PayoutTableColumn2Row2.Texture = Wheel1LegendImage;
                    PayoutTableColumn2Row3.Texture = Wheel1LegendImage;
                    PayoutTableColumn2Row4.Texture = Wheel1LegendImage;
                    PayoutTable1.X -= 72;
                    PayoutTable1.Y -= 5;
                    PayoutTable2.Visible = false;
                    PayoutTable3.Visible = false;
                    activePayoutTable = PayoutTable1;
                    eachBet = 1;
                    break;
                case 1:
                    Wheel1.Texture = Wheel2Image;
                    Wheel2.Texture = Wheel2Image;
                    Wheel3.Texture = Wheel2Image;
                    activeWheelTexture = Wheel2Image;
                    PayoutTableColumn1Row1.Texture = Wheel2LegendImage;
                    PayoutTableColumn1Row2.Texture = Wheel2LegendImage;
                    PayoutTableColumn1Row3.Texture = Wheel2LegendImage;
                    PayoutTableColumn1Row4.Texture = Wheel2LegendImage;
                    PayoutTableColumn2Row1.Texture = Wheel2LegendImage;
                    PayoutTableColumn2Row2.Texture = Wheel2LegendImage;
                    PayoutTableColumn2Row3.Texture = Wheel2LegendImage;
                    PayoutTableColumn2Row4.Texture = Wheel2LegendImage;
                    PayoutTable2.X -= 30;
                    PayoutTable2.Y -= 5;
                    PayoutTable1.Visible = false;
                    PayoutTable3.Visible = false;
                    activePayoutTable = PayoutTable2;
                    eachBet = 5;
                    break;
                case 2:
                    Wheel1.Texture = Wheel3Image;
                    Wheel2.Texture = Wheel3Image;
                    Wheel3.Texture = Wheel3Image;
                    activeWheelTexture = Wheel3Image;
                    PayoutTableColumn1Row1.Texture = Wheel3LegendImage;
                    PayoutTableColumn1Row2.Texture = Wheel3LegendImage;
                    PayoutTableColumn1Row3.Texture = Wheel3LegendImage;
                    PayoutTableColumn1Row4.Texture = Wheel3LegendImage;
                    PayoutTableColumn2Row1.Texture = Wheel3LegendImage;
                    PayoutTableColumn2Row2.Texture = Wheel3LegendImage;
                    PayoutTableColumn2Row3.Texture = Wheel3LegendImage;
                    PayoutTableColumn2Row4.Texture = Wheel3LegendImage;
                    PayoutTable3.X -= 32;
                    PayoutTable3.Y -= 5;
                    PayoutTable1.Visible = false;
                    PayoutTable2.Visible = false;
                    activePayoutTable = PayoutTable3;
                    eachBet = 10;
                    break;
                default:
                    break;
            }
            // customise the chips texture
            Chips.SetBounds(machineGrade * 27, 0, 27, 20);
            // customise bet text
            BetText.Alignment = TextAlignment.Center;
            BetText.Mode = UITextEditMode.ReadOnly;
            Add(BetText);
            currentBet = 1;
            displayedBet = currentBet * eachBet;
            UpdateBetText();
        }
        private void UpdateBetText()
        {
            BetText.CurrentText = "$" + displayedBet;
        }
        private void RemovePlayerListeners()
        {
            try
            {
                ArmButton.OnButtonClick -= SpinHandler;
                SpinButton.OnButtonClick -= SpinHandler;
                SpinnerIncreaseBet.OnButtonClick -= BetIncreaseHandler;
                SpinnerDecreaseBet.OnButtonClick -= BetDecreaseHandler;
            }
            catch (Exception error)
            {
                Console.WriteLine("UISLotsEOD.RemoveAllListeners: There was an error: " + error);
                ArmButton.Disabled = true;
                SpinButton.Disabled = true;
                SpinnerIncreaseBet.Disabled = true;
                SpinnerDecreaseBet.Disabled = true;
            }
        }
        private void AddPlayerListeners() {
            ArmButton.OnButtonClick += SpinHandler;
            SpinButton.OnButtonClick += SpinHandler;
            SpinnerIncreaseBet.OnButtonClick += BetIncreaseHandler;
            SpinnerDecreaseBet.OnButtonClick += BetDecreaseHandler;
            ArmButton.Disabled = false;
            SpinButton.Disabled = false;
            SpinnerIncreaseBet.Disabled = false;
            SpinnerDecreaseBet.Disabled = false;
        }
    }
    class WheelStopsList
    {
        public WheelStopNode current;
        public WheelStopNode next;
        public WheelStopNode previous;
        private int myOffsetY = 0;
        private bool isSlowing;
        private int ticksToStop = 25;
        private int targetStopsAway = 0;
        private int distanceToTarget = -1;
        private Random randomLeftover = new Random();
        private int leftoverY = 0;
        private bool isStopping;
        public bool isStopped;
        public VMEODSlotsStops targetStop;

        public WheelStopsList()
        {
            // make a node for each possible stop in the correct order
            foreach (VMEODSlotsStops stop in Enum.GetValues(typeof(VMEODSlotsStops)))
            {
                if (previous != null)
                {
                    current = new WheelStopNode(stop);
                    previous.Next = current;
                }
                else
                {
                    next = current = new WheelStopNode(stop);
                }
                previous = current;
            }
            // close the loop by settings top-most's (the jackspot/sixth stop) next to bottom-most, the first one made (blankOne)
            current.Next = next; 

            // previous is not used during gameplay, so setting it to be a BLANK stop will serve a purpose when the wheel stops
            previous = next;
        }
        public int OffsetY
        {
            get { return myOffsetY; }
        }
        public void IncrementOffsetY(int value)
        {
            if (isSlowing)
            {
                int modifiedValue = 0;
                if (distanceToTarget == -1)
                {
                    distanceToTarget = ((targetStopsAway * UISlotsEOD.WHEEL_TEXTURE_WIDTH_AND_HEIGHT) -
                        myOffsetY);
                }
                switch (distanceToTarget)
                {
                    case 116:
                        {
                            modifiedValue = 22;
                            break;
                        }
                    case 94:
                        {
                            modifiedValue = 22;
                            break;
                        }
                    case 87: // special case
                        {
                            modifiedValue = 16;
                            break;
                        }
                    case 72:
                        {
                            modifiedValue = 17;
                            break;
                        }
                    case 71: // special case
                        {
                            modifiedValue = 16;
                            break;
                        }
                    case 55:
                        {
                            modifiedValue = 17;
                            break;
                        }
                    case 38:
                        {
                            modifiedValue = 12;
                            break;
                        }
                    case 29: // special case
                        {
                            modifiedValue = 15;
                            break;
                        }
                    case 26:
                        {
                            modifiedValue = 12;
                            break;
                        }
                    case 14:
                        {
                            modifiedValue = 7;
                            break;
                        }
                    case 7:
                        {
                            modifiedValue = 7;
                            break;
                        }
                    case 0:
                        {
                            modifiedValue = 0;
                            ticksToStop = 5;
                            isSlowing = false;
                            isStopping = true;
                            myOffsetY = leftoverY = randomLeftover.Next(0, 3);
                            break;
                        }
                    default:
                        {
                            modifiedValue = value; // 29, always
                            break;
                        }
                }
                distanceToTarget -= modifiedValue;
                myOffsetY += modifiedValue;
                ticksToStop--;
            }
            else if (isStopping)
            {
                switch (ticksToStop)
                {
                    case 4:
                        {
                            if (leftoverY == 2)
                            {
                                myOffsetY = leftoverY = 1;
                            }
                            else if(leftoverY == 1)
                            {
                                myOffsetY = leftoverY = 1;
                            }
                            else
                            {
                                myOffsetY = leftoverY = 0;
                            }
                            break;
                        }
                    case 2:
                        {
                            if (leftoverY == 1)
                            {
                                myOffsetY = leftoverY = 0;
                            }
                            else
                            {
                                myOffsetY = leftoverY = 0;
                            }
                            break;
                        }
                    case 0:
                        {
                            myOffsetY = leftoverY = 0;
                            isStopping = false;
                            isStopped = true;
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
                ticksToStop--;
                return;
            }
            else if (isStopped)
            {
                return;
            }
            else {
                myOffsetY += value;
            }
            if (myOffsetY >= UISlotsEOD.WHEEL_TEXTURE_WIDTH_AND_HEIGHT)
            {
                myOffsetY -= UISlotsEOD.WHEEL_TEXTURE_WIDTH_AND_HEIGHT;
                previous = current;
                current = next;
                next = next.Next;
            }
        }
        public void SlowDown()
        {
            targetStopsAway = 0;
            isSlowing = true;
            var tempNode = new WheelStopNode(current.myStop, current);
            while (!tempNode.Next.myStop.Equals(targetStop))
            {
                targetStopsAway++;
                tempNode = tempNode.Next;
            }
            if (targetStopsAway == 0) // targetStop is the very next stop
            {
                targetStopsAway = 12;
            }
        }
        public void Reset()
        {
            leftoverY = 0;
            ticksToStop = 25;
            distanceToTarget = -1;
            isSlowing = false;
            isStopping = false;
            isStopped = false;
        }
    }
    class WheelStopNode
    {
        public WheelStopNode Next { get; set; }
        public VMEODSlotsStops myStop;
        public int myStartingY;

        public WheelStopNode(VMEODSlotsStops stop)
        {
            myStop = stop;
            CalculateStartingY();
        }
        public WheelStopNode(VMEODSlotsStops stop, WheelStopNode next)
        {
            myStop = stop;
            Next = next;
            CalculateStartingY();
        }
        public void CalculateStartingY()
        {
            if ((byte)myStop % 2 == 0) // a blank stop
            {
                myStartingY = 6 * UISlotsEOD.WHEEL_TEXTURE_WIDTH_AND_HEIGHT;
            }
            else
            {
                switch ((byte)myStop)
                {
                    case 1:
                        {
                            myStartingY = 5 * UISlotsEOD.WHEEL_TEXTURE_WIDTH_AND_HEIGHT;
                            break;
                        }
                    case 3:
                        {
                            myStartingY = 4 * UISlotsEOD.WHEEL_TEXTURE_WIDTH_AND_HEIGHT;
                            break;
                        }
                    case 5:
                        {
                            myStartingY = 3 * UISlotsEOD.WHEEL_TEXTURE_WIDTH_AND_HEIGHT;
                            break;
                        }
                    case 7:
                        {
                            myStartingY = 2 * UISlotsEOD.WHEEL_TEXTURE_WIDTH_AND_HEIGHT;
                            break;
                        }
                    case 9:
                        {
                            myStartingY = 1 * UISlotsEOD.WHEEL_TEXTURE_WIDTH_AND_HEIGHT;
                            break;
                        }
                    case 11:
                        {
                            myStartingY = 0 * UISlotsEOD.WHEEL_TEXTURE_WIDTH_AND_HEIGHT;
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
        }
    }
}
