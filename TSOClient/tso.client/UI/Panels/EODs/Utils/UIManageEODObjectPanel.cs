using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Controls;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels.EODs.Utils
{
    public class UIManageEODObjectPanel : UIContainer
    {
        private ManageEODObjectTypes Type;
        private int ObjectBalance;
        private int ObjectOdds;
        private int ObjectMinimumBalance;
        private int ObjectMaximumBalance;
        private int ObjectMinimumPlayerBet;
        private int ObjectMaximumPlayerBet;
        private bool ObjectIsOn;
        /*
         * Slot Machine Assets
         */
        private Texture2D RadioButtonTexture = GetTexture(0x0000049C00000001); // cf. slotseod.uis
        private UISlider OddsSlider;
        private UIButton OnOffButton;
        private UITextEdit CurrentOdds;
        private UILabel Odds;
        private UILabel House;
        private UILabel Player;
        private UILabel OnOff;
        /*
         * Group Casino Object Assets
         */
        private Texture2D EditAmountTexture = GetTexture(0x000007E200000001); // cf. budget.uis
        private UIButton EditMinimumBetButton;
        private UIImage EditMinimumBetButtonSeat;
        private UIButton EditMaximumBetButton;
        private UIImage EditMaximumBetButtonSeat;
        private UIImage MaximumBetTextBack;
        private UITextEdit MaximumBetText;
        private UIImage MinimumBetTextBack;
        private UITextEdit MinimumBetText;
        private UILabel MachineBalanceLabel;
        private UILabel MinimumBetLabel;
        private UILabel MaximumBetLabel;
        /*
         * Shared Object Assets
         */
        private Texture2D ButtonSeatTexture = GetTexture(0x0000019700000002); // cf. slotseod.uis
        private Texture2D CashOutTexture = GetTexture(0x00000C9F00000001); // cf. slotseod.uis
        private Texture2D TextBackTexture = GetTexture(0x0000088B00000001); // cf. slotseod.uis
        private UIButton CashOutButton;
        private UIImage CashOutButtonSeat;
        private UIImage MachineBalanceTextBack;
        private UITextEdit MachineBalanceText;

        public delegate void SendMessage(EODMessageNode Send);
        public event SendMessage OnNewStringMessage;
        public event SendMessage OnNewByteMessage;

        public UIManageEODObjectPanel(ManageEODObjectTypes type, int currentBalance, int minBalance, int maxBalance, int currentOdds, bool isOn)
        {
            Type = type;
            ObjectBalance = currentBalance;
            ObjectMinimumBalance = minBalance;
            ObjectMaximumBalance = maxBalance;
            ObjectOdds = currentOdds;
            ObjectIsOn = isOn;
            InitUIAssets();
        }

        public UIManageEODObjectPanel(ManageEODObjectTypes type, int currentBalance, int minBalance, int maxBalance, int minBet, int maxBet)
        {
            Type = type;
            ObjectBalance = currentBalance;
            ObjectMinimumBalance = minBalance;
            ObjectMaximumBalance = maxBalance;
            ObjectMinimumPlayerBet = minBet;
            ObjectMaximumPlayerBet = maxBet;
            InitUIAssets();
        }

        public void SetObjectOnOff(bool isNowOn)
        {
            ObjectIsOn = isNowOn;
        }

        public void InputFailHandler(string transactionType, string failureReason)
        {
            string message = "";
            if (transactionType == null || failureReason == null)
                return;
            else if (transactionType.Length > 1)
                transactionType = "" + transactionType[0]; // truncate it

            if (failureReason.Equals(VMEODSlotsInputErrorTypes.Null.ToString()))
            {
                if (transactionType.Equals("w") || transactionType.Equals("d"))
                    ResumeFromMachineBalance("", "" + ObjectBalance);
                else if (transactionType.Equals("x"))
                    ResumeFromBetAmount("max_bet", "" + ObjectMaximumPlayerBet);
                else if (transactionType.Equals("n"))
                    ResumeFromBetAmount("min_bet", "" + ObjectMinimumPlayerBet);
                return;
            }
            else if (failureReason.Equals(VMEODSlotsInputErrorTypes.Invalid.ToString()))
                message = "That is not a valid number!";
            else if (failureReason.Equals(VMEODSlotsInputErrorTypes.Overflow.ToString()))
            {
                if (transactionType.Equals("w"))
                    message = "You cannot withdraw more than the balance of the machine!";
                else if (transactionType.Equals("d"))
                    message = "You cannot deposit that many simoleons because the machine can only hold: $" + ObjectMaximumBalance;
                else
                    message = "An unknown error occured.";
            }
            else if (failureReason.Equals(VMEODRouletteInputErrorTypes.BetTooHighForBalance.ToString()))
                message = "You do not have enough money in this object to cover that bet amount." + System.Environment.NewLine
                    + System.Environment.NewLine + "You must stock AT LEAST 140 times the maximum bet amount.";
            else if (failureReason.Equals(VMEODRouletteInputErrorTypes.BetTooLow.ToString()))
            {
                if (transactionType.Equals("n"))
                    message = "The minimum bet cannot be lower than $1.";
                else if (transactionType.Equals("x"))
                    message = "The maximum bet cannot be lower than the minimum bet.";
                else
                    message = "An unknown error occured.";
            }
            else if (failureReason.Equals(VMEODRouletteInputErrorTypes.BetTooHigh.ToString()))
            {
                if (transactionType.Equals("n"))
                    message = "The minimum bet cannot be higher than the maximum bet.";
                else if (transactionType.Equals("x"))
                    message = "The maximum bet cannot be higher than $1000.";
                else
                    message = "An unknown error occured.";
            }/*
            else if (failureReason.Equals(VMEODRouletteInputErrorTypes.ObjectMustBeClosed))
                message = "You must first close the object before changing the betting rules or the object's balance.";*/
            else
                message = "An unknown error occured.";

            // show the alert with the error message to the user
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
                    if (transactionType.Equals("w") || transactionType.Equals("d"))
                        ResumeFromMachineBalance("", "" + ObjectBalance);
                    else if (transactionType.Equals("x"))
                        ResumeFromBetAmount("max_bet", "" + ObjectMaximumPlayerBet);
                    else if (transactionType.Equals("n"))
                        ResumeFromBetAmount("min_bet", "" + ObjectMinimumPlayerBet);
                    UIScreen.RemoveDialog(alert);
                }),
            }, true);
        }
        public void DepositFailHandler(string evt, string amountString)
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
                    ResumeFromMachineBalance("resume_manage", "" + ObjectBalance);
                    UIScreen.RemoveDialog(alert);
                }),
            }, true);
        }
        public void ResumeFromMachineBalance(string evt, string balance)
        {
            int newBalance;
            var result = Int32.TryParse(balance, out newBalance);
            if (result)
                ObjectBalance = newBalance;
            MachineBalanceText.CurrentText = "$" + ObjectBalance;
            CashOutButton.OnButtonClick += OnCashoutButtonClick;
            if (!Type.Equals(ManageEODObjectTypes.SlotMachine))
            {
                EditMinimumBetButton.OnButtonClick += OnEditMinimumClick;
                EditMaximumBetButton.OnButtonClick += OnEditMaximumClick;
            }
        }
        public void ResumeFromBetAmount(string evt, string minOrMaxBet)
        {
            int betAmount;
            var result = Int32.TryParse(minOrMaxBet, out betAmount);
            if (evt != null && evt.Length > 3 && result)
            {
                if (evt[2].Equals('x')) // "max_bet"
                {
                    ObjectMaximumPlayerBet = betAmount;
                    MaximumBetText.CurrentText = "$" + ObjectMaximumPlayerBet;
                }
                else // "min_bet"
                {
                    ObjectMinimumPlayerBet = betAmount;
                    MinimumBetText.CurrentText = "$" + ObjectMinimumPlayerBet;
                }
            }
            CashOutButton.OnButtonClick += OnCashoutButtonClick;
            EditMinimumBetButton.OnButtonClick += OnEditMinimumClick;
            EditMaximumBetButton.OnButtonClick += OnEditMaximumClick;
        }

        private void InitUIAssets()
        {
            // add the cash out button (and background) and machine balance information, ubiquitious to all objects
            CashOutButtonSeat = new UIImage(ButtonSeatTexture)
            {
                X = 60,
                Y = 90
            };
            AddAt(0, CashOutButtonSeat);
            CashOutButton = new UIButton(CashOutTexture)
            {
                X = 63,
                Y = 93,
                Tooltip = GameFacade.Strings["UIText", "259", "10"]
            };
            Add(CashOutButton);
            CashOutButton.OnButtonClick += OnCashoutButtonClick;

            MachineBalanceTextBack = new UIImage(TextBackTexture)
            {
                X = 100,
                Y = 90
            };
            AddAt(0, MachineBalanceTextBack);
            MachineBalanceText = new UITextEdit()
            {
                Size = new Vector2(65, 20),
                X = 114,
                Y = 94,
                Alignment = TextAlignment.Center,
                Mode = UITextEditMode.ReadOnly,
                CurrentText = "$" + ObjectBalance,
                Tooltip = GameFacade.Strings["UIText", "259", "11"]
            };
            var textStyle = MachineBalanceText.TextStyle.Clone();
            textStyle.Size = 12;
            MachineBalanceText.TextStyle = textStyle;
            Add(MachineBalanceText);

            // add more assets depending on machine type
            switch (Type)
            {
                case ManageEODObjectTypes.SlotMachine:
                    {
                        // add the on/off button, and the odds slider
                        OnOffButton = new UIButton(GetTexture(0x0000049C00000001))
                        {
                            Tooltip = GameFacade.Strings["UIText", "259", "12"],
                            X = 175,
                            Y = 60
                        };
                        Add(OnOffButton);
                        OnOffButton.OnButtonClick += OnOffButtonClick;

                        // initiate OnOffButton
                        if (ObjectIsOn)
                        {
                            OnOffButton.Tooltip = GameFacade.Strings["UIText", "259", "14"];
                            OnOffButton.ForceState = 1;
                        }
                        else
                        {
                            OnOffButton.Tooltip = GameFacade.Strings["UIText", "259", "13"];
                            OnOffButton.ForceState = 0;
                        }

                        // the slider for the odds
                        OddsSlider = new UISlider()
                        {
                            Texture = GetTexture(0x00000CA400000001),
                            Tooltip = ObjectOdds + "%",
                            Size = new Vector2(100, 1),
                            MinValue = 80,
                            MaxValue = 110,
                            Orientation = 0,
                            X = 286,
                            Y = 104
                        };
                        Add(OddsSlider);
                        OddsSlider.Value = ObjectOdds;
                        OddsSlider.OnChange += OddsChangeHandler;

                        // add slot machine labels
                        OnOff = new UILabel()
                        {
                            Size = new Vector2(100, 21),
                            X = 70,
                            Y = 55,
                            _Alignment = 1,
                            Caption = GameFacade.Strings["UIText", "259", "20"],
                            CaptionStyle = textStyle
                        };
                        Add(OnOff);

                        Odds = new UILabel()
                        {
                            Size = new Vector2(120, 21),
                            X = 278,
                            Y = 55,
                            _Alignment = 1,
                            Caption = GameFacade.Strings["UIText", "259", "7"],
                            CaptionStyle = textStyle
                        };
                        Add(Odds);

                        Player = new UILabel()
                        {
                            Size = new Vector2(80, 21),
                            X = 380,
                            Y = 94,
                            _Alignment = 1,
                            Caption = GameFacade.Strings["UIText", "259", "9"],
                            CaptionStyle = textStyle
                        };
                        Add(Player);

                        House = new UILabel()
                        {
                            Size = new Vector2(80, 21),
                            X = 220,
                            Y = 94,
                            _Alignment = 1,
                            Caption = GameFacade.Strings["UIText", "259", "8"],
                            CaptionStyle = textStyle
                        };
                        Add(House);

                        // FreeSO exclusive
                        CurrentOdds = new UITextEdit()
                        {
                            Size = new Vector2(45, 20),
                            X = Odds.X + 46,
                            Y = 79,
                            Mode = UITextEditMode.ReadOnly,
                            CurrentText = ObjectOdds + "%"
                        };
                        Add(CurrentOdds);

                        break;
                    }
                default:
                    {
                        // minor adjustments
                        MachineBalanceTextBack.Y -= 6;
                        MachineBalanceText.Y -= 6;
                        CashOutButton.Y -= 6;
                        CashOutButtonSeat.Y -= 6;

                        // label for machine balance
                        MachineBalanceLabel = new UILabel()
                        {
                            Size = new Vector2(100, 21),
                            X = 82,
                            Y = 57,
                            Caption = GameFacade.Strings["UIText", "259", "11"] + ":", // "Cash in machine"
                            CaptionStyle = textStyle
                        };
                        Add(MachineBalanceLabel);
                        
                        // text field and back for minimum bet
                        MinimumBetTextBack = new UIImage(TextBackTexture)
                        {
                            X = 338,
                            Y = 62
                        };
                        Add(MinimumBetTextBack);
                        MinimumBetText = new UITextEdit()
                        {
                            Size = new Vector2(65, 20),
                            X = MinimumBetTextBack.X + 14,
                            Y = MinimumBetTextBack.Y + 4,
                            Alignment = TextAlignment.Center,
                            Mode = UITextEditMode.ReadOnly,
                            CurrentText = "$" + ObjectMinimumPlayerBet,
                            TextStyle = textStyle,
                            Tooltip = "Min bet"
                        };
                        Add(MinimumBetText);

                        // text field and back for maximum bet
                        MaximumBetTextBack = new UIImage(TextBackTexture)
                        {
                            X = MinimumBetTextBack.X,
                            Y = MinimumBetTextBack.Y + 33
                        };
                        Add(MaximumBetTextBack);
                        MaximumBetText = new UITextEdit()
                        {
                            Size = new Vector2(65, 20),
                            X = MaximumBetTextBack.X + 14,
                            Y = MaximumBetTextBack.Y + 4,
                            Alignment = TextAlignment.Center,
                            Mode = UITextEditMode.ReadOnly,
                            CurrentText = "$" + ObjectMaximumPlayerBet,
                            TextStyle = textStyle,
                            Tooltip = "Max bet"
                        };
                        Add(MaximumBetText);

                        // button and back for minimum bet
                        EditMinimumBetButtonSeat = new UIImage(ButtonSeatTexture)
                        {
                            X = MinimumBetTextBack.X - 32,
                            Y = MinimumBetTextBack.Y,
                        };

                        Add(EditMinimumBetButtonSeat);
                        EditMinimumBetButton = new UIButton(EditAmountTexture)
                        {
                            X = EditMinimumBetButtonSeat.X + 3,
                            Y = EditMinimumBetButtonSeat.Y + 3,
                            Tooltip = "Edit Min bet",
                        };
                        EditMinimumBetButtonSeat.ScaleX = EditMinimumBetButtonSeat.ScaleY = (EditMinimumBetButton.Size / CashOutButton.Size).X;
                        Add(EditMinimumBetButton);

                        // button and back for maximum bet
                        EditMaximumBetButtonSeat = new UIImage(ButtonSeatTexture)
                        {
                            X = MaximumBetTextBack.X - 32,
                            Y = MaximumBetTextBack.Y
                        };
                        Add(EditMaximumBetButtonSeat);
                        EditMaximumBetButton = new UIButton(EditAmountTexture)
                        {
                            X = EditMaximumBetButtonSeat.X + 3,
                            Y = EditMaximumBetButtonSeat.Y + 3,
                            Tooltip = "Edit Max bet",
                        };
                        EditMaximumBetButtonSeat.ScaleX = EditMaximumBetButtonSeat.ScaleY = (EditMaximumBetButton.Size / CashOutButton.Size).X;
                        Add(EditMaximumBetButton);

                        // label for minimum bet
                        MinimumBetLabel = new UILabel()
                        {
                            Size = new Vector2(60, 21),
                            X = EditMinimumBetButtonSeat.X - 70,
                            Y = MinimumBetText.Y,
                            Alignment = TextAlignment.Right,
                            Caption = "Min Bet:",
                            CaptionStyle = textStyle
                        };
                        Add(MinimumBetLabel);

                        // label for maximum bet
                        MaximumBetLabel = new UILabel()
                        {
                            Size = new Vector2(60, 21),
                            X = EditMaximumBetButtonSeat.X - 70,
                            Y = MaximumBetText.Y,
                            Alignment = TextAlignment.Right,
                            Caption = "Max Bet:",
                            CaptionStyle = textStyle
                        };
                        Add(MaximumBetLabel);

                        // liseners for editing bet buttons
                        EditMinimumBetButton.OnButtonClick += OnEditMinimumClick;
                        EditMaximumBetButton.OnButtonClick += OnEditMaximumClick;
                        break;
                    }
            }

        } // InitUIAssets()

        private void OnCashoutButtonClick(UIElement targetButton)
        {
            CashOutButton.OnButtonClick -= OnCashoutButtonClick;
            if (!Type.Equals(ManageEODObjectTypes.SlotMachine))
            {
                EditMinimumBetButton.OnButtonClick -= OnEditMinimumClick;
                EditMaximumBetButton.OnButtonClick -= OnEditMaximumClick;
                /*if (ObjectIsOn)
                {
                    InputFailHandler("w", VMEODRouletteInputErrorTypes.ObjectMustBeClosed.ToString());
                    return;
                }*/
            }
            // show an alert that asks the user if they want to make a desposit or a withdrawal
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = "Owner Transactions",
                Message = "What would you like to do?",
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = new UIAlertButton[]
                {
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
                Message = "This object is currently stocked with: $" + ObjectBalance + System.Environment.NewLine +
                System.Environment.NewLine + "For players to use this object you must maintain a minimum balance of: $" + ObjectMinimumBalance +
                System.Environment.NewLine + System.Environment.NewLine + "How much would you like to deposit?" +
                System.Environment.NewLine + System.Environment.NewLine + "(This machine cannot hold more than: $" + ObjectMaximumBalance + ")",
                Alignment = TextAlignment.Left,
                TextEntry = true,
                MaxChars = 5,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    UserInputHandler("d", alert.ResponseText.Trim());
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
                Message = "This object is currently stocked with: $" + ObjectBalance + System.Environment.NewLine +
                System.Environment.NewLine + "For players to use this object you must maintain a minimum balance of: $" + ObjectMinimumBalance +
                System.Environment.NewLine + System.Environment.NewLine + "How much would you like to withdraw?",
                Alignment = TextAlignment.Left,
                TextEntry = true,
                MaxChars = 5,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    UserInputHandler("w", alert.ResponseText.Trim());
                    UIScreen.RemoveDialog(alert);
                }),
            }, true);
        }

        private void UserInputHandler(string type, string userInput)
        {
            int amount = 0;
            userInput.Replace("-", ""); // in case any jokesters try to input a negative number (validated on server)
            string eventName = null;
            string eventMessage = "";
            // try to parse the user's input
            try
            {
                amount = Int32.Parse(userInput);
                // input is valid, now check it against MachineBalance
                if (amount == 0)
                {
                    eventMessage = VMEODSlotsInputErrorTypes.Null.ToString();
                }
                else if (type.Equals("w"))
                { // withdrawing
                    if (amount > ObjectBalance)
                    {
                        eventMessage = VMEODSlotsInputErrorTypes.Overflow.ToString();
                    }
                    else
                    {
                        eventName = "withdraw";
                        eventMessage = "" + amount;
                    }
                }
                else if (type.Equals("d")) // depositing
                {
                    if ((amount + ObjectBalance) > ObjectMaximumBalance)
                    {
                        eventMessage = VMEODSlotsInputErrorTypes.Overflow.ToString();
                    }
                    else
                    {
                        eventName = "deposit";
                        eventMessage = "" + amount;
                    }
                }
                else if (type.Equals("n")) // minimum bet
                {
                    // proposed minimum bet must be greater than $0 and not greater than maximum bet
                    if (amount < 0)
                    {
                        eventMessage = VMEODRouletteInputErrorTypes.BetTooLow.ToString();
                    }
                    else if (amount > ObjectMaximumPlayerBet)
                    {
                        eventMessage = VMEODRouletteInputErrorTypes.BetTooHigh.ToString();
                    }
                    else
                    {
                        // does the machine have enough money to cover this bet amount?
                        if (amount > ObjectBalance * 140)
                            eventMessage = VMEODRouletteInputErrorTypes.BetTooHighForBalance.ToString();
                        else
                        {
                            eventName = "new_minimum";
                            eventMessage = "" + amount;
                        }
                    }
                }
                else if (type.Equals("x")) // maximum bet
                {
                    // proposed maximum bet must be greater than or equal to minimum bet
                    if (amount < ObjectMinimumPlayerBet)
                    {
                        eventMessage = VMEODRouletteInputErrorTypes.BetTooLow.ToString();
                    }
                    // proposed maximum bet must not be greater than $1000
                    else if (amount > VMEODRoulettePlugin.GLOBAL_MAXIMUM_ROULETTE_ROUND_BET)
                    {
                        eventMessage = VMEODRouletteInputErrorTypes.BetTooHigh.ToString();
                    }
                    else
                    {
                        // does the machine have enough money to cover this bet amount?
                        if (amount > ObjectBalance * 140)
                            eventMessage = VMEODRouletteInputErrorTypes.BetTooHighForBalance.ToString();
                        else
                        {
                            eventName = "new_maximum";
                            eventMessage = "" + amount;
                        }
                    }
                }
                else
                {
                    eventMessage = VMEODSlotsInputErrorTypes.Unknown.ToString();
                }
            }
            catch (ArgumentNullException nullException)
            {
                eventName = null;
                eventMessage = VMEODSlotsInputErrorTypes.Null.ToString();
            }
            catch (FormatException formatException)
            {
                eventName = null;
                if (userInput.Length == 0)
                    eventMessage = VMEODSlotsInputErrorTypes.Null.ToString();
                else
                    eventMessage = VMEODSlotsInputErrorTypes.Invalid.ToString();
            }
            catch (OverflowException overFlowException)
            {
                eventName = null;
                eventMessage = VMEODSlotsInputErrorTypes.Overflow.ToString();
            }
            finally
            {
                if (eventName != null)
                    OnNewStringMessage(new EODMessageNode(eventName, eventMessage));
                else
                    InputFailHandler(type, eventMessage);
            }
        }

        private void OddsChangeHandler(UIElement targetSlider)
        {
            OddsSlider.OnChange -= OddsChangeHandler;
            ObjectOdds = Convert.ToByte(OddsSlider.Value);
            CurrentOdds.CurrentText = ObjectOdds + "%";
            OnNewByteMessage(new EODMessageNode("new_odds", new byte[] { (byte)ObjectOdds }));
            OddsSlider.OnChange += OddsChangeHandler;
        }

        private void OnOffButtonClick(UIElement targetButton)
        {
            OnOffButton.OnButtonClick -= OnOffButtonClick;
            ObjectIsOn = !ObjectIsOn;
            if (ObjectIsOn)
            {
                OnOffButton.Tooltip = GameFacade.Strings["UIText", "259", "14"];
                OnOffButton.ForceState = 1;
            }
            else
            {
                OnOffButton.Tooltip = GameFacade.Strings["UIText", "259", "13"];
                OnOffButton.ForceState = 0;
            }
            OnNewStringMessage(new EODMessageNode("toggle_onOff", "" + OnOffButton.ForceState));
            OnOffButton.OnButtonClick += OnOffButtonClick;
        }

        private void OnEditMinimumClick(UIElement target)
        {
            CashOutButton.OnButtonClick -= OnCashoutButtonClick;
            EditMinimumBetButton.OnButtonClick -= OnEditMinimumClick;
            EditMaximumBetButton.OnButtonClick -= OnEditMaximumClick;
            /*if (ObjectIsOn)
                InputFailHandler("n", VMEODRouletteInputErrorTypes.ObjectMustBeClosed.ToString());
            else*/
                SetBetPrompt(true);
        }

        private void OnEditMaximumClick(UIElement target)
        {
            CashOutButton.OnButtonClick -= OnCashoutButtonClick;
            EditMinimumBetButton.OnButtonClick -= OnEditMinimumClick;
            EditMaximumBetButton.OnButtonClick -= OnEditMaximumClick;
            /*if (ObjectIsOn)
                InputFailHandler("x", VMEODRouletteInputErrorTypes.ObjectMustBeClosed.ToString());
            else*/
                SetBetPrompt(false);
        }

        private void SetBetPrompt(bool isMinBet)
        {
            // show an alert that asks the user how much to set the min/max bet
            UIAlert alert = null;
            string typeConditional = "";
            string setBet = "";
            string betTip = "";
            if (Type.Equals(ManageEODObjectTypes.Roulette))
            {
                typeConditional = "Roulette Tables must be able to cover 35 times any bet for 4 simultaneous players, so AT LEAST 140x the maximum bet."
                    + System.Environment.NewLine + "For example: if your maximum bet is $100, you must have AT LEAST $14000 in this object.";
                if (isMinBet)
                {
                    setBet = "Minimum";
                    betTip = "(Note: Minimum bets can't be less than $1)";
                }
                else
                {
                    setBet = "Maximum";
                    betTip = "(Note: Maximum bets can't be greater than $1000)";
                }
            }
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = "Set " + setBet + " Bet",
                Message = "This object is currently stocked with: $" + ObjectBalance + System.Environment.NewLine +
                System.Environment.NewLine + typeConditional + System.Environment.NewLine + System.Environment.NewLine +
                "What would you like to set as your " + setBet.Replace('M', 'm') + " bet?" + System.Environment.NewLine + System.Environment.NewLine
                + betTip,
                Alignment = TextAlignment.Left,
                TextEntry = true,
                MaxChars = 4,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    UserInputHandler(setBet[2] + "", alert.ResponseText.Trim()); // 'x' for maximum, 'n' for minimum
                    UIScreen.RemoveDialog(alert);
                }),
            }, true);
        }
    }

    public class EODMessageNode
    {
        public string EventName;
        public string EventStringData;
        public byte[] EventByteData;

        public EODMessageNode(string evt, string data)
        {
            EventName = evt;
            EventStringData = data;
        }

        public EODMessageNode(string evt, byte[] data)
        {
            EventName = evt;
            EventByteData = data;
        }
    }
    public enum ManageEODObjectTypes: byte
    {
        SlotMachine = 0,
        Roulette = 1,
        Blackjack = 2,
        VideoPoker = 4
    }
}
