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
        private bool InputAllowed;
        private int ObjectBalance;
        private int ObjectOdds;
        private int ObjectMinimumBalance;
        private int ObjectMaximumBalance;
        private int ObjectMinimumPlayerBet;
        private int ObjectMaximumPlayerBet;
        private int ObjectMaximumPlayerSideBet;
        private bool ObjectIsOn;
        private int ObjectPayoutRatio;
        private ManageEODObjectTypes Type;
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
        private UIButton EditMaximumSideBetButton;
        private UIImage EditMaximumBetButtonSeat;
        private UIImage EditMaximumSideBetButtonSeat;
        private UIImage MaximumBetTextBack;
        private UITextEdit MaximumBetText;
        private UIImage MaximumSideBetTextBack;
        private UITextEdit MaximumSideBetText;
        private UIImage MinimumBetTextBack;
        private UITextEdit MinimumBetText;
        private UILabel MachineBalanceLabel;
        private UILabel MinimumBetLabel;
        private UILabel MaximumBetLabel;
        private UILabel MaximumSideBetLabel;
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

        public const int MINIMUM_BET_LIMIT = 1;
        public const int MAXIMUM_BET_LIMIT = 1000;

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
            ObjectPayoutRatio = (type.Equals(ManageEODObjectTypes.Roulette)) ? 140 : 6; // worst case payout ratio is 140 for roulette, 6 for blackjack
            Type = type;
            ObjectBalance = currentBalance;
            ObjectMinimumBalance = minBalance;
            ObjectMaximumBalance = maxBalance;
            ObjectMinimumPlayerBet = minBet;
            ObjectMaximumPlayerBet = maxBet;
            InitUIAssets();
        }
        public UIManageEODObjectPanel(ManageEODObjectTypes type, int currentBalance, int minBalance, int maxBalance, int minBet, int maxBet, int maxSideBet)
        {
            ObjectPayoutRatio = 84; // holdem worst case payout ratio on ante bet is 84, side bet is 104
            Type = type;
            ObjectBalance = currentBalance;
            ObjectMinimumBalance = minBalance;
            ObjectMaximumBalance = maxBalance;
            ObjectMinimumPlayerBet = minBet;
            ObjectMaximumPlayerBet = maxBet;
            ObjectMaximumPlayerSideBet = maxSideBet;
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
                else if (transactionType.Equals("s"))
                    ResumeFromBetAmount("side_bet", "" + ObjectMaximumPlayerSideBet);
                return;
            }
            else if (failureReason.Equals(VMEODSlotsInputErrorTypes.Invalid.ToString()))
                message = GameFacade.Strings.GetString("f110", "24"); // "That is not a valid number!"
            else if (failureReason.Equals(VMEODSlotsInputErrorTypes.Overflow.ToString()))
            {
                if (transactionType.Equals("w"))
                    message = GameFacade.Strings.GetString("f110", "25"); // "You cannot withdraw more than the balance of the machine!"
                else if (transactionType.Equals("d"))
                    // "You cannot deposit that many simoleons because the machine can only hold: $%d"
                    message = GameFacade.Strings.GetString("f110", "27").Replace("%d", "" + ObjectMaximumBalance);
                else
                    message = GameFacade.Strings.GetString("f110", "34"); // "An unknown error occured."
            }
            else if (failureReason.Equals(VMEODRouletteInputErrorTypes.BetTooHighForBalance.ToString()))
            {
                switch (Type)
                {
                    case ManageEODObjectTypes.Roulette:
                        {
                            // "You do not have enough money in this object to cover that bet amount." \n \n
                            message = GameFacade.Strings.GetString("f110", "32") + System.Environment.NewLine + System.Environment.NewLine +
                                // "You must stock AT LEAST 140 times the maximum bet amount."
                                GameFacade.Strings.GetString("f110", "33");
                            break;
                        }
                    case ManageEODObjectTypes.Blackjack:
                        {
                            // "You do not have enough money in this object to cover that bet amount." \n \n
                            message = GameFacade.Strings.GetString("f110", "32") + System.Environment.NewLine + System.Environment.NewLine +
                                // "You must stock at least 6 times the maximum bet."
                                GameFacade.Strings.GetString("f110", "19");
                            break;
                        }
                    case ManageEODObjectTypes.HoldEmCasino:
                        {
                            // "You do not have enough money in this object to cover that bet amount." \n \n
                            message = GameFacade.Strings.GetString("f110", "32") + System.Environment.NewLine + System.Environment.NewLine +
                                // "You must stock at least 84 times the maximum ante bet and 104 times the maximum side bet."
                                GameFacade.Strings.GetString("f110", "41");
                            break;
                        }
                }
            }
            else if (failureReason.Equals(VMEODRouletteInputErrorTypes.BetTooLow.ToString()))
            {
                if (transactionType.Equals("n"))
                    message = GameFacade.Strings.GetString("f110", "28").Replace("%d", MINIMUM_BET_LIMIT + ""); // "The minimum bet cannot be lower than $%d."
                else if (transactionType.Equals("x"))
                    message = GameFacade.Strings.GetString("f110", "29"); // "The maximum bet cannot be lower than the minimum bet."
                else if (transactionType.Equals("s"))
                    message = GameFacade.Strings.GetString("f110", "45"); // "The side bet cannot be lower than $0."
                else
                    message = GameFacade.Strings.GetString("f110", "34"); // "An unknown error occured."
            }
            else if (failureReason.Equals(VMEODRouletteInputErrorTypes.BetTooHigh.ToString()))
            {
                if (transactionType.Equals("n"))
                    message = GameFacade.Strings.GetString("f110", "31"); // "The minimum bet cannot be higher than the maximum bet."
                else if (transactionType.Equals("x"))
                    message = GameFacade.Strings.GetString("f110", "30").Replace("%d", MAXIMUM_BET_LIMIT + ""); // "The maximum bet cannot be higher than $%d."
                else if (transactionType.Equals("s"))
                    message = GameFacade.Strings.GetString("f110", "44").Replace("%d", MAXIMUM_BET_LIMIT + ""); // "The side bet cannot be higher than $%d."
                else
                    message = GameFacade.Strings.GetString("f110", "34"); // "An unknown error occured."
            }
            else
                message = GameFacade.Strings.GetString("f110", "34"); // "An unknown error occured."

            // show the alert with the error message to the user
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = GameFacade.Strings.GetString("f110", "23"), // "Transaction Error"
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
                    else if (transactionType.Equals("s"))
                        ResumeFromBetAmount("side_bet", "" + ObjectMaximumPlayerSideBet);

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
                Title = GameFacade.Strings.GetString("f110", "23"), // "Transaction Error"
                Message = GameFacade.Strings.GetString("f110", "26").Replace("%d", "" + amountString), // "You don't have enough simoleons to deposit: $%d"
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
            InputAllowed = true;
        }
        public void ResumeFromBetAmount(string evt, string betAmountString)
        {
            int betAmount;
            var result = Int32.TryParse(betAmountString, out betAmount);
            if (evt != null && evt.Length > 3 && result)
            {
                if (evt[2].Equals('x')) // "max_bet"
                {
                    ObjectMaximumPlayerBet = betAmount;
                    MaximumBetText.CurrentText = "$" + ObjectMaximumPlayerBet;
                    ObjectMinimumBalance = ObjectPayoutRatio * ObjectMaximumPlayerBet;
                }
                else if (evt[2].Equals('n')) // "min_bet"
                {
                    ObjectMinimumPlayerBet = betAmount;
                    MinimumBetText.CurrentText = "$" + ObjectMinimumPlayerBet;
                }
                else // "side_bet"
                {
                    ObjectMaximumPlayerSideBet = betAmount;
                    MaximumSideBetText.CurrentText = "$" + ObjectMaximumPlayerSideBet;
                    ObjectMinimumBalance = ObjectMaximumPlayerBet * VMEODHoldEmCasinoPlugin.WORST_CASE_ANTE_PAYOUT_RATIO +
                         ObjectMaximumPlayerSideBet * VMEODHoldEmCasinoPlugin.WORST_CASE_SIDE_PAYOUT_RATIO;
                }
            }
            InputAllowed = true;
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
                Tooltip = GameFacade.Strings["UIText", "259", "10"] // "Click to cash out"
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
                Size = new Vector2(85, 20),
                X = 104,
                Y = 94,
                Alignment = TextAlignment.Center,
                Mode = UITextEditMode.ReadOnly,
                CurrentText = "$" + ObjectBalance,
                Tooltip = GameFacade.Strings.GetString("f110", "35") // "Cash in object"
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
                            Tooltip = GameFacade.Strings["UIText", "259", "12"], // "Current Odds"
                            X = 175,
                            Y = 60
                        };
                        Add(OnOffButton);
                        OnOffButton.OnButtonClick += OnOffButtonClick;

                        // initiate OnOffButton
                        if (ObjectIsOn)
                        {
                            OnOffButton.Tooltip = GameFacade.Strings["UIText", "259", "14"]; // "Turn Off"
                            OnOffButton.ForceState = 1;
                        }
                        else
                        {
                            OnOffButton.Tooltip = GameFacade.Strings["UIText", "259", "13"]; // "Turn On"
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
                            Caption = GameFacade.Strings["UIText", "259", "20"], // "Turn On/Off"
                            CaptionStyle = textStyle
                        };
                        Add(OnOff);

                        Odds = new UILabel()
                        {
                            Size = new Vector2(120, 21),
                            X = 278,
                            Y = 55,
                            _Alignment = 1,
                            Caption = GameFacade.Strings["UIText", "259", "7"], // "Set the Odds"
                            CaptionStyle = textStyle
                        };
                        Add(Odds);

                        Player = new UILabel()
                        {
                            Size = new Vector2(80, 21),
                            X = 380,
                            Y = 94,
                            _Alignment = 1,
                            Caption = GameFacade.Strings["UIText", "259", "9"], // "Player"
                            CaptionStyle = textStyle
                        };
                        Add(Player);

                        House = new UILabel()
                        {
                            Size = new Vector2(80, 21),
                            X = 220,
                            Y = 94,
                            _Alignment = 1,
                            Caption = GameFacade.Strings["UIText", "259", "8"], // "House"
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
                            Caption = GameFacade.Strings.GetString("f110", "35") + ":", // "Cash in object:"
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
                            Tooltip = GameFacade.Strings.GetString("f110", "13") // "Min bet"
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
                            Tooltip = GameFacade.Strings.GetString("f110", "14") // "Max bet"
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
                            Tooltip = GameFacade.Strings.GetString("f110", "15") + GameFacade.Strings.GetString("f110", "13"), // "Edit Min bet"
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
                            Tooltip = GameFacade.Strings.GetString("f110", "15") + GameFacade.Strings.GetString("f110", "14"), // "Edit Max bet"
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
                            Caption = GameFacade.Strings.GetString("f110", "13") + ":", // "Min bet:"
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
                            Caption = GameFacade.Strings.GetString("f110", "14") + ":", // "Max bet:"
                            CaptionStyle = textStyle
                        };
                        Add(MaximumBetLabel);

                        // liseners for editing bet buttons
                        EditMinimumBetButton.OnButtonClick += OnEditMinimumClick;
                        EditMaximumBetButton.OnButtonClick += OnEditMaximumClick;

                        // tweak for Hold'em Casino
                        if (Type.Equals(ManageEODObjectTypes.HoldEmCasino))
                        {
                            var offset = new Vector2(6, -18);
                            MinimumBetTextBack.Position += offset;
                            MinimumBetText.Position += offset;
                            MinimumBetLabel.Position += offset;
                            EditMinimumBetButtonSeat.Position += offset;
                            EditMinimumBetButton.Position += offset;

                            MaximumBetTextBack.Position += offset;
                            MaximumBetText.Position += offset;
                            MaximumBetLabel.Position += offset;
                            EditMaximumBetButtonSeat.Position += offset;
                            EditMaximumBetButton.Position += offset;

                            MachineBalanceTextBack.X -= 20;
                            MachineBalanceText.X -= 20;
                            CashOutButton.X -= 20;
                            CashOutButtonSeat.X -= 20;
                            MachineBalanceLabel.X -= 20;

                            MinimumBetLabel.Caption = GameFacade.Strings.GetString("f110", "36") + ":"; // "Min Ante Bet"
                            MaximumBetLabel.Caption = GameFacade.Strings.GetString("f110", "37") + ":"; // "Max Ante Bet"
                            EditMinimumBetButton.Tooltip = GameFacade.Strings.GetString("f110", "15") +
                                GameFacade.Strings.GetString("f110", "36") + ":"; // "Edit Min Ante Bet"
                            EditMaximumBetButton.Tooltip = GameFacade.Strings.GetString("f110", "15") +
                                GameFacade.Strings.GetString("f110", "37") + ":"; // "Edit Min Ante Bet"
                            
                            /* Add Max Side Bet stuff */
                            // label for maximum side bet
                            MaximumSideBetTextBack = new UIImage(TextBackTexture)
                            {
                                X = MaximumBetTextBack.X,
                                Y = MaximumBetTextBack.Y + 33
                            };
                            Add(MaximumSideBetTextBack);
                            MaximumSideBetText = new UITextEdit()
                            {
                                Size = new Vector2(65, 20),
                                X = MaximumSideBetTextBack.X + 14,
                                Y = MaximumSideBetTextBack.Y + 4,
                                Alignment = TextAlignment.Center,
                                Mode = UITextEditMode.ReadOnly,
                                CurrentText = "$" + ObjectMaximumPlayerSideBet,
                                TextStyle = textStyle,
                                Tooltip = GameFacade.Strings.GetString("f110", "38") // "Max Side Bet"
                            };
                            Add(MaximumSideBetText);
                            EditMaximumSideBetButtonSeat = new UIImage(ButtonSeatTexture)
                            {
                                X = MaximumSideBetTextBack.X - 32,
                                Y = MaximumSideBetTextBack.Y,
                            };
                            Add(EditMaximumSideBetButtonSeat);
                            EditMaximumSideBetButton = new UIButton(EditAmountTexture)
                            {
                                X = EditMaximumSideBetButtonSeat.X + 3,
                                Y = EditMaximumSideBetButtonSeat.Y + 3,
                                Tooltip = GameFacade.Strings.GetString("f110", "15") + GameFacade.Strings.GetString("f110", "38"), // "Edit Max Side Bet"
                            };
                            EditMaximumSideBetButtonSeat.ScaleX = EditMaximumSideBetButtonSeat.ScaleY = (EditMaximumSideBetButton.Size / CashOutButton.Size).X;
                            Add(EditMaximumSideBetButton);
                            MaximumSideBetLabel = new UILabel()
                            {
                                Size = new Vector2(60, 21),
                                X = EditMaximumSideBetButtonSeat.X - 70,
                                Y = MaximumSideBetText.Y,
                                Alignment = TextAlignment.Right,
                                Caption = GameFacade.Strings.GetString("f110", "38") + ":", // "Max Side Bet:"
                                CaptionStyle = textStyle
                            };
                            Add(MaximumSideBetLabel);

                            EditMaximumSideBetButton.OnButtonClick += OnEditSideClick;
                        }
                        break;
                    }
            }
            InputAllowed = true;
        }
        private void OnCashoutButtonClick(UIElement targetButton)
        {
            if (InputAllowed)
            {
                InputAllowed = false;

                // show an alert that asks the user if they want to make a desposit or a withdrawal
                UIAlert alert = null;
                alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                {
                    TextSize = 12,
                    Title = GameFacade.Strings.GetString("f110", "1"), // "Owner Transactions"
                    Message = GameFacade.Strings.GetString("f110", "2"), // "What would you like to do?"
                    Alignment = TextAlignment.Center,
                    TextEntry = false,
                    Buttons = new UIAlertButton[]
                    {
                        new UIAlertButton (UIAlertButtonType.OK, ((btn1) =>
                        {
                            DepositPrompt();
                            UIScreen.RemoveDialog(alert);
                        }), GameFacade.Strings.GetString("f110", "4")), // "Deposit"
                        new UIAlertButton (UIAlertButtonType.Cancel, ((btn2) =>
                        {
                            WithdrawPrompt();
                            UIScreen.RemoveDialog(alert);
                        }), GameFacade.Strings.GetString("f110", "3")) // "Withdraw"
                    }
                }, true);
            }
        }

        private void DepositPrompt()
        {
            // show an alert that asks the user how much to deposit into the machine
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = GameFacade.Strings.GetString("f110", "4") + " " + GameFacade.Strings.GetString("f110", "5"), // "Deposit Simoleons"
                // "This object is currently stocked with: $%d" \n \n
                Message = GameFacade.Strings.GetString("f110", "6").Replace("%d", "" + ObjectBalance) + System.Environment.NewLine +
                // "For players to use this object you must maintain a minimum balance of: $%d" \n \n
                System.Environment.NewLine + GameFacade.Strings.GetString("f110", "8").Replace("%d", "" + ObjectMinimumBalance) +
                // "How much would you like to deposit?"
                System.Environment.NewLine + System.Environment.NewLine + GameFacade.Strings.GetString("f110", "10") +
                // "(This object cannot hold more than: $%d)"
                System.Environment.NewLine + System.Environment.NewLine + "(" +
                GameFacade.Strings.GetString("f110", "7").Replace("%d", "" + ObjectMaximumBalance) + ")",
                Alignment = TextAlignment.Left,
                TextEntry = true,
                MaxChars = 6,
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
                Title = GameFacade.Strings.GetString("f110", "3") + " " + GameFacade.Strings.GetString("f110", "5"), // "Withdraw Simoleons" 
                // "This object is currently stocked with: $%d" \n \n
                Message = GameFacade.Strings.GetString("f110", "6").Replace("%d", "" + ObjectBalance) + System.Environment.NewLine +
                // "For players to use this object you must maintain a minimum balance of: $%d" \n \n
                System.Environment.NewLine + GameFacade.Strings.GetString("f110", "8").Replace("%d", "" + ObjectMinimumBalance) +
                // "How much would you like to withdraw?"
                System.Environment.NewLine + System.Environment.NewLine + GameFacade.Strings.GetString("f110", "9"),
                Alignment = TextAlignment.Left,
                TextEntry = true,
                MaxChars = 6,
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
            userInput.Replace("-", ""); // in case any jokesters try to input a negative number (validated on server, too)
            string eventName = null;
            string eventMessage = "";
            // try to parse the user's input
            try
            {
                amount = Int32.Parse(userInput);
                // input is valid, now check it against MachineBalance
                if (amount == 0 && !type.Equals("s")) // only side bets can be 0
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
                        if (amount * ObjectPayoutRatio > ObjectBalance) // roulette 140, blackjack 6, holdem 84
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
                    else if (amount > UIManageEODObjectPanel.MAXIMUM_BET_LIMIT)
                    {
                        eventMessage = VMEODRouletteInputErrorTypes.BetTooHigh.ToString();
                    }
                    else
                    {
                        // does the machine have enough money to cover this bet amount?
                        if (amount * ObjectPayoutRatio > ObjectBalance) // roulette 140, blackjack 6, holdem 84
                            eventMessage = VMEODRouletteInputErrorTypes.BetTooHighForBalance.ToString();
                        else
                        {
                            eventName = "new_maximum";
                            eventMessage = "" + amount;
                        }
                    }
                }
                else if (type.Equals("s")) // side bet
                {
                    // proposed maximum side bet must not be greater than maximum allowed for short data type constraints
                    if (amount < 0)
                        eventMessage = VMEODRouletteInputErrorTypes.BetTooLow.ToString();
                    else if (amount > UIManageEODObjectPanel.MAXIMUM_BET_LIMIT)
                    {
                        eventMessage = VMEODRouletteInputErrorTypes.BetTooHigh.ToString();
                    }
                    else
                    {
                        // does the machine have enough money to cover this bet amount?
                        if (amount * VMEODHoldEmCasinoPlugin.WORST_CASE_SIDE_PAYOUT_RATIO +
                            ObjectMaximumPlayerBet * VMEODHoldEmCasinoPlugin.WORST_CASE_ANTE_PAYOUT_RATIO > ObjectBalance)
                            eventMessage = VMEODRouletteInputErrorTypes.BetTooHighForBalance.ToString();
                        else
                        {
                            eventName = "new_side";
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
                OnOffButton.Tooltip = GameFacade.Strings["UIText", "259", "14"]; // "Turn Off"
                OnOffButton.ForceState = 1;
            }
            else
            {
                OnOffButton.Tooltip = GameFacade.Strings["UIText", "259", "13"]; // "Turn On"
                OnOffButton.ForceState = 0;
            }
            OnNewStringMessage(new EODMessageNode("toggle_onOff", "" + OnOffButton.ForceState));
            OnOffButton.OnButtonClick += OnOffButtonClick;
        }
        private void OnEditMinimumClick(UIElement target)
        {
            if (InputAllowed)
            {
                InputAllowed = false;
                SetBetPrompt(ManageEODBetTypes.MinBet);
            }
        }
        private void OnEditMaximumClick(UIElement target)
        {
            if (InputAllowed)
            {
                InputAllowed = false;
                SetBetPrompt(ManageEODBetTypes.MaxBet);
            }
        }
        private void OnEditSideClick(UIElement target)
        {
            if (InputAllowed)
            {
                InputAllowed = false;
                SetBetPrompt(ManageEODBetTypes.MaxSide);
            }
        }
        private void SetBetPrompt(ManageEODBetTypes betType)
        {
            // show an alert that asks the user how much to set the min/max bet
            UIAlert alert = null;
            string typeConditional = "";
            string setBet = "";
            string betTip = "";
            string shortCode = "";
            int tempMax = 0;
            if (Type.Equals(ManageEODObjectTypes.Roulette))
            {
                // "Roulette tables must be able to cover 35 times any bet for 4 simultaneous players, so at least 140 times the maximum bet." \n \n
                typeConditional = GameFacade.Strings.GetString("f110", "16") + System.Environment.NewLine + System.Environment.NewLine +
                    // "For example: if your maximum bet is $100, you must have AT LEAST $14000 in this object."
                    GameFacade.Strings.GetString("f110", "17");
            }
            else if (Type.Equals(ManageEODObjectTypes.Blackjack))
            {
                // "A Blackjack payout is 3:2 or one and a half times any bet. Tables must be able to cover up to 4 blackjacks per round." \n \n
                typeConditional = GameFacade.Strings.GetString("f110", "18") + System.Environment.NewLine + System.Environment.NewLine +
                    // "You must stock at least 6 times the maximum bet."
                    GameFacade.Strings.GetString("f110", "19");
                tempMax = MAXIMUM_BET_LIMIT;
            }
            else if (Type.Equals(ManageEODObjectTypes.HoldEmCasino))
            {
                // "Holdem Casino tables must cover the payout of up to four players, each of which have an ante and side bet." \n \n
                typeConditional = GameFacade.Strings.GetString("f110", "42") + System.Environment.NewLine + System.Environment.NewLine +
                    // "While the probability is very low, this could mean paying out for a Royal Flush, Straight Flush, 4 of a kind,
                    // and a Flush in the same hand, therefore:"  \n \n
                    GameFacade.Strings.GetString("f110", "43") + System.Environment.NewLine + System.Environment.NewLine +
                    // "You must stock at least 84 times the maximum ante bet and 104 times the maximum side bet."
                    GameFacade.Strings.GetString("f110", "41");
                tempMax = MAXIMUM_BET_LIMIT;
            }
            switch (betType)
            {
                case ManageEODBetTypes.MinBet:
                    {
                        setBet = GameFacade.Strings.GetString("f110", "13"); // "Min bet"
                        shortCode = setBet[2] + "";
                        // "(Note: Minimum bets can't be less than $%d)"
                        betTip = GameFacade.Strings.GetString("f110", "20").Replace("%d", "" + MINIMUM_BET_LIMIT);
                        break;
                    }
                case ManageEODBetTypes.MaxBet:
                    {
                        setBet = GameFacade.Strings.GetString("f110", "14"); // "Max bet"
                        shortCode = setBet[2] + "";
                        // "(Note: Maximum bets can't be greater than $%d)"
                        betTip = GameFacade.Strings.GetString("f110", "21").Replace("%d", "" + tempMax);
                        break;
                    }
                default:
                    //case ManageEODBetTypes.MaxSide:
                    {
                        setBet = GameFacade.Strings.GetString("f110", "39"); // "Side bet"
                        shortCode = "s";
                        // "(Note: Holdem casino bets can't be greater than $%d)"
                        betTip = GameFacade.Strings.GetString("f110", "40").Replace("%d", "" + tempMax);
                        break;
                    }
            }
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = GameFacade.Strings.GetString("f110", "15") + setBet, // "Edit Min/Max/Side bet"
                // "This object is currently stocked with: $%d" \n \n
                Message = GameFacade.Strings.GetString("f110", "6").Replace("%d", "" + ObjectBalance) + System.Environment.NewLine +
                System.Environment.NewLine + typeConditional + System.Environment.NewLine + System.Environment.NewLine +
                // "What would you like to set as your " + "Min/Max/Side bet?" \n \n Tip
                GameFacade.Strings.GetString("f110", "22") + setBet + "?" + System.Environment.NewLine + System.Environment.NewLine + betTip,
                Alignment = TextAlignment.Left,
                TextEntry = true,
                MaxChars = 4,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    UserInputHandler(shortCode, alert.ResponseText.Trim()); // "x" for maximum, "n" for minimum, "s" for side
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
        HoldEmCasino = 3,
        VideoPoker = 4
    }
    public enum ManageEODBetTypes: byte
    {
        MinBet = 0,
        MaxBet = 1,
        MaxSide = 2
    }
}
