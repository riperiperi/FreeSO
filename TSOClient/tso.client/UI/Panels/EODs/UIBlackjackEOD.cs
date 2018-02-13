using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Text;
using System.Threading.Tasks;
using FSO.Content.Model;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Client.UI.Panels.EODs.Utils;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.SimAntics.NetPlay.EODs.Handlers.Data;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIBlackjackEOD : UIEOD
    {
        // general UI
        private UIEODLobby Lobby;
        public UIScript Script;
        private UIManageEODObjectPanel OwnerPanel;
        public int TextX = 50;
        private int MainPlayerActiveHand;
        private int MyPlayerNumber;
        private int MyCurrentBetAmount;
        private int MinBet;
        private int MaxBet;
        private string Player1BetCaption;
        private string Player2BetCaption;
        private string Player3BetCaption;
        private string Player4BetCaption;
        private bool IsBettingAllowed;
        private List<string[]> CardsToDeal;
        private int DealingIndex;
        private Timer DealTimer;
        private Timer InvalidateTimer;
        private Random Random = new Random();
        private UIAlert InsuranceAlert;
        private short DealersID;
        private UIVMPersonButton DealerPersonButton;

        // text
        public UILabel labelTotalBet { get; set; }
        public UITextEdit TotalBetEntry { get; set; }
        private UILabel Player1BetAmount;
        private UILabel Player2BetAmount;
        private UILabel Player3BetAmount;
        private UILabel Player4BetAmount;
        private UILabel MyBetAmount;
        private UITextEdit DealerBetAmount;
        private UILabel DealerCardTotal;
        private UILabel Player1CardTotal;
        private UILabel Player2CardTotal;
        private UILabel Player3CardTotal;
        private UILabel Player4CardTotal;
        private UILabel MyPlayerCardTotal;
        private UILabel Player1SplitLetter;
        private UILabel Player2SplitLetter;
        private UILabel Player3SplitLetter;
        private UILabel Player4SplitLetter;
        private List<UITextEdit> MainPlayerCardTotals;
        private UILabel InsuredLabel;
        private string DealersName;

        // images
        public UIImage betEntryBox { get; set; }
        private UIImage DealerPos;
        private UIImage DealerHead;
        private UIImage Player1Head;
        private UIImage Player2Head;
        private UIImage Player3Head;
        private UIImage Player4Head;
        private UIImage Player1BetBack;
        private UIImage Player2BetBack;
        private UIImage Player3BetBack;
        private UIImage Player4BetBack;
        private UIImage DealerBetBack;
        private UIImage Player1TotalBack;
        private UIImage Player2TotalBack;
        private UIImage Player3TotalBack;
        private UIImage Player4TotalBack;
        private UIImage DealerTotalBack;
        private UIImage EODTallBack;
        private UIImage EODTallBackEnd;
        private UIImage BtnDoubleBack;
        private UIImage BtnSplitBack;
        private UIImage BtnBetBack;
        private UISlotsImage MainCardBack;
        private UIImage MainPlayerCardTotalBack;

        // containers
        private CardHand DealerCardContainer;
        private CardHand Player1CardContainer;
        private CardHand Player2CardContainer;
        private CardHand Player3CardContainer;
        private CardHand Player4CardContainer;
        private CardHand MyPlayerContainer;
        private List<CardHand> MainPlayerCardContainers;

        // textures
        private Texture2D PlayerPicturePlaceHolder = GetTexture(0x000002B300000001); // EOD_PizzaHeadPlaceholder1.bmp
        private Texture2D PlayerBetBack = GetTexture(0x95600000001); // eod_buzzer_playertimerback.bmp
        private Texture2D Blackjack_ScoreBox = GetTexture(0xCB300000001); // eod_blackjackscorebox.bmp
        public Texture2D imagePlayerBack { get; set; } // not sure
        public Texture2D imagePlayerBox { get; set; } // other player box
        public Texture2D imagePlayerBoxEdit { get; set; } // your player box
        public Texture2D imageBtnSeat { get; set; } // behind buttons
        public Texture2D image_BetAmount { get; set; } // behind bet amount

        // coordinates: images lacking textures
        public UIImage playerPos1 { get; set; }
        public UIImage playerPos2 { get; set; }
        public UIImage playerPos3 { get; set; }
        public UIImage playerPos4 { get; set; }
        public UIImage playerOffset_Head { get; set; }
        public UIImage playerOffset_Bet { get; set; }
        public UIImage playerOffset_Cards { get; set; }
        public UIImage offset_HandTotal { get; set; }
        public UIImage offset_ButtonSeat { get; set; }
        public UIImage offset_PlayerSeat { get; set; }
        public UIImage dealerPos_Head { get; set; }
        public UIImage dealerPos_Cards { get; set; }
        private Vector2 MainContainerCenterStage_Offset = new Vector2(82, 0);

        // buttons
        public UIButton btnHit { get; set; }
        public UIButton btnStand { get; set; }
        public UIButton btnDouble { get; set; }
        public UIButton btnSplit { get; set; }
        public UIButton btnBet { get; set; }
        public UIButton btnBetDown { get; set; } // not used, removed immediately- reason: stupid
        public UIButton btnBetUp { get; set; } // not used, removed immediately- reason: stupid
        public UIButton btnChip1 { get; set; }
        public UIButton btnChip2 { get; set; }
        public UIButton btnChip3 { get; set; }
        public UIButton btnChip4 { get; set; }
        public UIButton btnChip5 { get; set; }
        private UIButton[] AllButtons;

        // constants
        private readonly float ONE_HAND_SCALE = 1.80f;
        private readonly float TWO_HANDS_SCALE = 1.60f;
        private readonly float THREE_HANDS_SCALE = 1.40f;
        private readonly float FOUR_HANDS_SCALE = 1.20f;
        private readonly float COLLAPSED_HANDS_MARGIN = 10f;
        private readonly int MAIN_CARD_BACK_WIDTH = 246;

        // literal strings to be translated later
        string Blackjack = "Blackjack";
        string DealerBusts = "%s busts!";
        string DealerHasBlackjack = "%s has blackjack!";
        string DealerHasTotal = "%s stands on %d.";
        string DealerDoesNotHaveBlackjack = "%s does not have blackjack.";
        string Insurance = "Insurance";
        string Insured = "Insured!";
        string Error = "Error";
        string InsuranceQuestion = "Do you want insurance?";
        string InsuranceDesc1 = "Insurance costs half of your bet amount, which is $%d.";
        string InsuranceDesc2 = "But if the dealer has blackjack, you will get your whole bet amount back.";
        string InsuranceConfirmation = "You bought insurance for $%d.";


        // temporary solution until I learn how to put these strings into an external file to be translated
        public Dictionary<byte, string> AlertStrings = new Dictionary<byte, string>()
        {
            { (byte)VMEODBlackjackAlerts.State_Race, "This error message should never appear. Please report this and what you did to get it!" },
            { (byte)VMEODBlackjackAlerts.False_Start, "It's not your turn." },
            { (byte)VMEODBlackjackAlerts.Illegal_Hit, "You cannot hit on this hand." },
            { (byte)VMEODBlackjackAlerts.Illegal_Double, "You cannot double down on this hand." },
            { (byte)VMEODBlackjackAlerts.Illegal_Split, "You cannot split this hand." },
            { (byte)VMEODBlackjackAlerts.Bet_Too_Low, "Your bet must be at least $m." },
            { (byte)VMEODBlackjackAlerts.Bet_Too_High, "Your bet cannot be greater than $M." },
            { (byte)VMEODBlackjackAlerts.Bet_NSF, "You don't have enough money to make that bet." },
            { (byte)VMEODBlackjackAlerts.Double_NSF, "You don't have enough money to double down." },
            { (byte)VMEODBlackjackAlerts.Split_NSF, "You don't have enough money to split." },
            { (byte)VMEODBlackjackAlerts.Observe_Once, "You can observe this round, but you must bet and play on the next one." },
            { (byte)VMEODBlackjackAlerts.Observe_Twice, "You were removed from the table due to inactivity." },
            { (byte)VMEODBlackjackAlerts.Table_NSF, "The table had to close due to insufficient balance. Perhaps you should let the owner know." }
        };

        public UIBlackjackEOD(UIEODController controller) : base(controller)
        {
            // init UI
            Script = RenderScript("blackjackeod.uis");
            AllButtons = new UIButton[] { btnChip1, btnChip2, btnChip3, btnChip4, btnChip5, btnHit, btnStand, btnDouble, btnSplit, btnBet,
                btnBetDown, btnBetUp };

            PlayerLowerUIIinit();
            PlayerUpperUIInit();

            Remove(btnChip1);
            Remove(btnChip2);
            Remove(btnChip3);
            Remove(btnChip4);
            Remove(btnChip5);
            Remove(btnHit);
            Remove(btnStand);
            Remove(btnDouble);
            Remove(btnSplit);
            Remove(btnBet);
            Remove(btnBetDown);
            Remove(btnBetUp);

            // make the lobby
            Lobby = new UIEODLobby(this, 4)
                .WithPlayerUI(new UIEODLobbyPlayer(0, Player1Head, Player1BetAmount))
                .WithPlayerUI(new UIEODLobbyPlayer(1, Player2Head, Player2BetAmount))
                .WithPlayerUI(new UIEODLobbyPlayer(2, Player3Head, Player3BetAmount))
                .WithPlayerUI(new UIEODLobbyPlayer(3, Player4Head, Player4BetAmount))
                .WithCaptionProvider((player, avatar) =>
                {
                    string captionToReturn = "";
                    if (avatar == null)
                        captionToReturn = "";
                    else if (player.Slot == 0)
                        captionToReturn = Player1BetCaption;
                    else if (player.Slot == 1)
                        captionToReturn = Player2BetCaption;
                    else if (player.Slot == 2)
                        captionToReturn = Player3BetCaption;
                    else if (player.Slot == 3)
                        captionToReturn = Player4BetCaption;
                    else
                        captionToReturn = "";

                    return captionToReturn;
                });
            Add(Lobby);

            // player listeners
            PlaintextHandlers["blackjack_bet_update_player0"] = UpdateSingleBetHandler;
            PlaintextHandlers["blackjack_bet_update_player1"] = UpdateSingleBetHandler;
            PlaintextHandlers["blackjack_bet_update_player2"] = UpdateSingleBetHandler;
            PlaintextHandlers["blackjack_bet_update_player3"] = UpdateSingleBetHandler;
            PlaintextHandlers["blackjack_players_update"] = Lobby.UpdatePlayers;
            PlaintextHandlers["eod_leave"] = (evt, str) => { OnClose(); };
            BinaryHandlers["blackjack_active_hand"] = UpdateMainPlayerDeckHandler;
            BinaryHandlers["blackjack_alert"] = UIAlertHandler;
            BinaryHandlers["blackjack_blackjack"] = BlackjackHandler;
            BinaryHandlers["dealer_blackjack_result"] = DealerBlackjackHandler;
            BinaryHandlers["dealer_hand_total"] = DealerHandTotalHandler;
            BinaryHandlers["blackjack_change_bet"] = ChangeMyBetHandler;
            BinaryHandlers["blackjack_double"] = DoubleHandler;
            BinaryHandlers["blackjack_double_broadcast"] = PlayerChoiceBroadcastHandler;
            BinaryHandlers["blackjack_deal_sequence"] = StartDealingSequenceHandler;
            BinaryHandlers["blackjack_disable_hand_buttons"] = DisableHandButtonsHandler;
            BinaryHandlers["blackjack_hit_broadcast"] = PlayerChoiceBroadcastHandler;
            BinaryHandlers["blackjack_insurance_callback"] = InsuranceCallbackHandler;
            BinaryHandlers["blackjack_insurance_prompt"] = InsurancePromptHandler;
            BinaryHandlers["blackjack_late_comer"] = PlayerChoiceBroadcastHandler;
            BinaryHandlers["blackjack_new_game"] = NewGameHandler;
            BinaryHandlers["blackjack_player_show"] = PlayerShowUIHandler;
            BinaryHandlers["blackjack_resume_double"] = AllowDoubleInputHandler;
            BinaryHandlers["blackjack_resume_hand"] = AllowInputHandler;
            BinaryHandlers["blackjack_resume_split"] = AllowSplitInputHandler;
            BinaryHandlers["blackjack_split"] = SplitHandler;
            BinaryHandlers["blackjack_split_broadcast"] = PlayerChoiceBroadcastHandler;
            BinaryHandlers["blackjack_stand"] = StandHandler;
            BinaryHandlers["blackjack_stand_broadcast"] = PlayerChoiceBroadcastHandler;
            BinaryHandlers["blackjack_sync_accepted_bets"] = UpdateAllBetsHandler;
            BinaryHandlers["blackjack_sync_all_hands"] = SyncAllActiveHandsHandler;
            BinaryHandlers["blackjack_sync_dealer"] = DealerHandHandler;
            BinaryHandlers["blackjack_sync_player"] = UpdatePlayerCardContainerHandler;
            BinaryHandlers["blackjack_timer"] = UpdateEODTimerHandler;
            BinaryHandlers["blackjack_toggle_betting"] = ToggleBettingHandler;
            BinaryHandlers["blackjack_win_loss_message"] = WinLossHandler;
            // owner listeners
            PlaintextHandlers["blackjack_deposit_NSF"] = DepositFailHandler;
            PlaintextHandlers["blackjack_owner_show"] = OwnerShowUIHandler;
            PlaintextHandlers["blackjack_deposit_fail"] = InputFailHandler;
            PlaintextHandlers["blackjack_withdraw_fail"] = InputFailHandler;
            PlaintextHandlers["blackjack_x_bet_fail"] = InputFailHandler; // max bet fail
            PlaintextHandlers["blackjack_n_bet_fail"] = InputFailHandler; // min bet fail
            PlaintextHandlers["blackjack_max_bet_success"] = ResumeFromBetAmountHandler;
            PlaintextHandlers["blackjack_min_bet_success"] = ResumeFromBetAmountHandler;
            PlaintextHandlers["blackjack_resume_manage"] = ResumeManageHandler;

            // other
            DealTimer = new Timer(1000);
            DealTimer.Elapsed += new ElapsedEventHandler(DealTimerHandler);
            DealersName = "M.O.M.I.";
            InvalidateTimer = new Timer(1000);
            /*
             * NOTE: If you haven't noticed how bad the EOD invalidateion problem is, just disable this timer and see how impossible it is to keep up
             * with the flow of the game due to message (tips) not showing on time or at all.
             */
            InvalidateTimer.Elapsed += new ElapsedEventHandler((obj, args) => { Parent.Invalidate(); });
            InvalidateTimer.Start();
        }
        public override void OnClose()
        {
            CloseInteraction();
            Send("blackjack_close", "");
            base.OnClose();
        }
        #region events
        /*
         * Show the player UI, when this player joins. Delineate which hand (above) belongs to player's main hand (below), customise graphics.
         */
        private void PlayerShowUIHandler(string evt, byte[] playerSlotMinBetMaxBet)
        {
            if (playerSlotMinBetMaxBet == null) return;
            
            MainPlayerCardContainers = new List<CardHand>();
            MainPlayerCardTotals = new List<UITextEdit>();
            
            string[] data = VMEODGameCompDrawACardData.DeserializeStrings(playerSlotMinBetMaxBet);
            byte playerSlot = 5;
            int minBet = -1;
            int maxBet = -1;

            if (data.Length == 4 && Byte.TryParse(data[0], out playerSlot) && Int32.TryParse(data[1], out minBet) &&
                Int32.TryParse(data[2], out maxBet) && Int16.TryParse(data[3], out DealersID))
            {
                RefreshButtons();
                MinBet = minBet;
                MaxBet = maxBet;

                if (playerSlot == 0)
                {
                    MyPlayerContainer = Player1CardContainer;
                    MyPlayerCardTotal = Player1CardTotal;
                    MyBetAmount = Player1BetAmount;
                    MyPlayerNumber = 0;
                    playerPos1.Texture = imagePlayerBoxEdit;
                    Player1TotalBack.Texture = Blackjack_ScoreBox;
                }
                else if (playerSlot == 1)
                {
                    MyPlayerContainer = Player2CardContainer;
                    MyPlayerCardTotal = Player2CardTotal;
                    MyBetAmount = Player2BetAmount;
                    MyPlayerNumber = 1;
                    playerPos2.Texture = imagePlayerBoxEdit;
                    Player2TotalBack.Texture = Blackjack_ScoreBox;
                }
                else if (playerSlot == 2)
                {
                    MyPlayerContainer = Player3CardContainer;
                    MyPlayerCardTotal = Player3CardTotal;
                    MyBetAmount = Player3BetAmount;
                    MyPlayerNumber = 2;
                    playerPos3.Texture = imagePlayerBoxEdit;
                    Player3TotalBack.Texture = Blackjack_ScoreBox;
                }
                else if (playerSlot == 3)
                {
                    MyPlayerContainer = Player4CardContainer;
                    MyPlayerCardTotal = Player4CardTotal;
                    MyBetAmount = Player4BetAmount;
                    MyPlayerNumber = 3;
                    playerPos4.Texture = imagePlayerBoxEdit;
                    Player4TotalBack.Texture = Blackjack_ScoreBox;
                }
                else return;

                // make and add dealer VMPersonButton
                DealerPersonButton = Lobby.GetAvatarButton(DealersID, true);
                if (DealerPersonButton != null)
                {
                    DealerPersonButton.Position = DealerHead.Position + new Vector2(2, 2);
                    Add(DealerPersonButton);
                    DealersName = DealerPersonButton.Avatar.Name;
                }

                // you may not bet/player right away
                DisableBettingButtons();
                DisableHandButtons();

                // show EOD
                Controller.ShowEODMode(new EODLiveModeOpt
                {
                    Buttons = 2,
                    Height = EODHeight.TallTall,
                    Length = EODLength.Full,
                    Tips = EODTextTips.Short,
                    Timer = EODTimer.Normal,
                    Expandable = false,
                    Expanded = true
                });
            }
        }
        /*
         * The UI for the owner
         */
        private void OwnerShowUIHandler(string evt, string balanceMinMaxBet)
        {
            if (balanceMinMaxBet == null)
                return;

            // hide a lot
            TotalBetEntry.Visible = false;
            labelTotalBet.Visible = false;
            EODTallBack.Visible = false;
            EODTallBackEnd.Visible = false;
            
            playerPos1.Visible = false;
            Player1Head.Visible = false;
            Player1CardContainer.Visible = false;
            Player1TotalBack.Visible = false;
            Player1CardTotal.Visible = false;

            playerPos2.Visible = false;
            Player2Head.Visible = false;
            Player2CardContainer.Visible = false;
            Player2TotalBack.Visible = false;
            Player2CardTotal.Visible = false;

            playerPos3.Visible = false;
            Player3Head.Visible = false;
            Player3CardContainer.Visible = false;
            Player3TotalBack.Visible = false;
            Player3CardTotal.Visible = false;
            
            playerPos4.Visible = false;
            Player4Head.Visible = false;
            Player4CardContainer.Visible = false;
            Player4TotalBack.Visible = false;
            Player4CardTotal.Visible = false;
            
            DealerPos.Visible = false;
            DealerHead.Visible = false;
            DealerCardContainer.Visible = false;
            DealerTotalBack.Visible = false;
            DealerCardTotal.Visible = false;

            Player1BetBack.Visible = false;
            Player1BetAmount.Visible = false;
            Player2BetBack.Visible = false;
            Player2BetAmount.Visible = false;
            Player3BetBack.Visible = false;
            Player3BetAmount.Visible = false;
            Player4BetBack.Visible = false;
            Player4BetAmount.Visible = false;
            DealerBetBack.Visible = false;
            DealerBetAmount.Visible = false;
            
            int tempBalance;
            int tempMinBet;
            int tempMaxBet;
            var split = balanceMinMaxBet.Split('%');
            if (split.Length > 2)
            {
                if (Int32.TryParse(split[0], out tempBalance) & Int32.TryParse(split[1], out tempMinBet) & Int32.TryParse(split[2], out tempMaxBet))
                {
                    OwnerPanel = new UIManageEODObjectPanel(
                        ManageEODObjectTypes.Blackjack, tempBalance, tempMaxBet * 6, 999999, tempMinBet, tempMaxBet);
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
        /*
         * Players supplied sound string
         */
        private void SoundHandler(string evt, string soundString)
        {
            // play the sound
            HIT.HITVM.Get().PlaySoundEvent(soundString);
        }
        /*
         * Updates EODTimer
         */
        private void UpdateEODTimerHandler(string evt, byte[] newTime)
        {
            SetNewTime(BitConverter.ToInt32(newTime, 0));
        }
        /*
         * Just displays in the tip whether or not the dealer has blackjack
         */
        private void DealerBlackjackHandler(string evt, byte[] hasBlackjack)
        {
            string message = "";
            if (hasBlackjack[0] == 0)
                message = DealerDoesNotHaveBlackjack.Replace("%s", DealersName);
            else
                message = DealerHasBlackjack.Replace("%s", DealersName);
            ShowUIAlert(Blackjack, message, null);
        }
        private void DealerHandTotalHandler(string evt, byte[] dealerHandTotal)
        {
            int total = BitConverter.ToInt32(dealerHandTotal, 0);
            string newTip = "";
            if (total == 0) // blackjack
                newTip = DealerHasBlackjack.Replace("%s", DealersName);
            else if (total > 21) // bust
            {
                newTip = DealerBusts.Replace("%s", DealersName);
            }
            else // Dealer stands on ##.
            {
                newTip = DealerHasTotal.Replace("%s", DealersName);
                newTip = newTip.Replace("%d", "" + total);
            }
            SetNewTip(newTip);
        }
        /*
         * This merely sends a message, with the title, for a UIAlert. Further action must follow in another event.
         */
        private void UIAlertHandler(string evt, byte[] msg)
        {
            string alert = AlertStrings[msg[0]];
            ShowUIAlert(Error, alert, null);
        }
        /*
         * Show the prompt for insurance.
         */
        private void InsurancePromptHandler(string evt, byte[] msg)
        {
            string insuranceDesc1 = InsuranceDesc1.Replace("%d", MyCurrentBetAmount + "");
            InsuranceAlert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = Insurance,
                Message = InsuranceQuestion + System.Environment.NewLine + insuranceDesc1 + System.Environment.NewLine + InsuranceDesc2,
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = UIAlertButton.YesNo(
                    yes => {
                        Send("blackjack_insurance_request", new byte[] { 1 });
                        UIScreen.RemoveDialog(InsuranceAlert);
                        InsuranceAlert = null;
                    },
                    no => {
                        Send("blackjack_insurance_request", new byte[] { 0 });
                        UIScreen.RemoveDialog(InsuranceAlert);
                        InsuranceAlert = null;
                    }
                    ),
            }, true);
        }
        /*
         * This callback is used by the server to ensure that the dialog disappears. It also is used to show a confirmation of insurance dialog.
         */
        private void InsuranceCallbackHandler(string evt, byte[] insuranceIfNotZero)
        {
            int spentForInsurance = BitConverter.ToInt32(insuranceIfNotZero, 0);
            // if insurance was bought, create a new alert confirming it was bought
            if (spentForInsurance > 0)
            {
                // play the ka-ching sound
                HIT.HITVM.Get().PlaySoundEvent("ui_object_place");
                InsuranceAlert = ShowUIAlert(Insurance, InsuranceConfirmation.Replace("%d", spentForInsurance + ""), null);
                // show the insured label
                InsuredLabel.Visible = true;
            }
            else
            {
                if (InsuranceAlert != null)
                {
                    UIScreen.RemoveDialog(InsuranceAlert);
                    InsuranceAlert = null;
                }
            }
        }
        /*
         * Serialized strings are sent: [0] integer index of the hand being split, [3] & [4] cards to go into the new hand, [1] & [2] return to split hand.
         * On the very first hand of the game, this event will be called where [3] & [4] are null - 1 card container created for [1] and [0] will be 0.
         */
        private void SplitHandler(string evt, byte[] serializedSplitNumberAndCards)
        {
            if (serializedSplitNumberAndCards == null)
                return;
            var cardsArray = VMEODGameCompDrawACardData.DeserializeStrings(serializedSplitNumberAndCards);
            int splittingHandPoision = -1;
            var cardsList = new List<string>(cardsArray);
            cardsList.RemoveAt(0);
            if (cardsArray.Length == 5 && Int32.TryParse(cardsArray[0], out splittingHandPoision))
                AddMainCardContainer(splittingHandPoision, cardsList.ToArray());
        }
        /*
         * Changes my bet amount below and above, either to sync or update from split/double. Does not enable input.
         */
        private void ChangeMyBetHandler(string evt, byte[] newBetAmount)
        {
            // update bet amount
            MyCurrentBetAmount = BitConverter.ToInt32(newBetAmount, 0);
            // update displayed bet text below and above
            UpdateMyBetAmount("" + MyCurrentBetAmount);
            // play the ka-ching sound
            HIT.HITVM.Get().PlaySoundEvent("ui_object_place");
        }
        /*
         * This event occurs when a stand event occurs from the server, or automatically if the server detects a bust. Does not enable input.
         */
        private void StandHandler(string evt, byte[] serializedCardArray)
        {
            bool collapse = false;
            if (MainPlayerCardContainers.Count > 1 && MainPlayerActiveHand < MainPlayerCardContainers.Count - 1)
                collapse = true;
            string[] cards = VMEODGameCompDrawACardData.DeserializeStrings(serializedCardArray);
            CloseActiveHand(collapse, "", cards);
        }
        /*
         * This event occurs when a blackjack event occurs from the server, or automatically if the server detects bust or blackjack. Does not enable input.
         */
        private void BlackjackHandler(string evt, byte[] serializedCardArray)
        {
            bool collapse = false;
            if (MainPlayerCardContainers.Count > 1 && MainPlayerActiveHand < MainPlayerCardContainers.Count - 1)
                collapse = true;
            string[] cards = VMEODGameCompDrawACardData.DeserializeStrings(serializedCardArray);
            CloseActiveHand(collapse, Blackjack + "!", cards);
        }
        /*
         * This event occurs when the user successfully doubles or double busts.
         */
        private void DoubleHandler(string evt, byte[] serializedCardArray)
        {
            bool collapse = false;
            if (MainPlayerCardContainers.Count > 1 && MainPlayerActiveHand < MainPlayerCardContainers.Count - 1)
                collapse = true;
            string[] cards = VMEODGameCompDrawACardData.DeserializeStrings(serializedCardArray);
            CloseActiveHand(collapse, "($$)", cards);
        }
        /*
         * Occurs whenever a new card is drawn. Allows input from user.
         */
        private void AllowInputHandler(string evt, byte[] serializedDeckNumberAndCardsArray)
        {
            if (InsuranceAlert != null)
            {
                UIScreen.RemoveDialog(InsuranceAlert);
                InsuranceAlert = null;
            }
            UpdateMainPlayerDeckHandler(evt, serializedDeckNumberAndCardsArray);
            EnableHandButtons(false, false);
        }
        /*
         * Occurs when switching to a new active hand before any action is taken. Allows input from user- including double.
         */
        private void AllowDoubleInputHandler(string evt, byte[] serializedDeckNumberAndCardsArray)
        {
            if (InsuranceAlert != null)
            {
                UIScreen.RemoveDialog(InsuranceAlert);
                InsuranceAlert = null;
            }
            UpdateMainPlayerDeckHandler(evt, serializedDeckNumberAndCardsArray);
            EnableHandButtons(false, true);
        }
        /*
         * Occurs when switching to a new active hand before any action is taken. Allows input from user- including split or double.
         */
        private void AllowSplitInputHandler(string evt, byte[] serializedDeckNumberAndCardsArray)
        {
            if (InsuranceAlert != null)
            {
                UIScreen.RemoveDialog(InsuranceAlert);
                InsuranceAlert = null;
            }
            UpdateMainPlayerDeckHandler(evt, serializedDeckNumberAndCardsArray);
            EnableHandButtons(true, true);
        }
        /*
         * This event occurs whenever a new card is drawn OR when switching to a new active hand before any action is taken. Does not enable input.
         */
        private void UpdateMainPlayerDeckHandler(string evt, byte[] serializedDeckNumberAndCardsArray)
        {
            if (serializedDeckNumberAndCardsArray == null)
                return;
            var data = VMEODGameCompDrawACardData.DeserializeStrings(serializedDeckNumberAndCardsArray);
            if (data != null && data.Length > 1)
            {
                int newActiveHand = -1;
                var cardsList = new List<string>(data);
                cardsList.RemoveAt(0);
                if (Int32.TryParse(data[0], out newActiveHand))
                {
                    if (newActiveHand < MainPlayerCardContainers.Count)
                    {
                        SetNewActiveHand(newActiveHand, cardsList.ToArray());
                    }
                }
            }
        }
        /*
         * Updates a player's bet based on the event name's final character 0-3
         */
        private void UpdateSingleBetHandler(string evt, string newBet)
        {
            int playerIndex = (int)Char.GetNumericValue(evt, evt.Length - 1);
            int dummy = 0;
            if (Int32.TryParse(newBet, out dummy))
                UpdatePlayerBetAmount(playerIndex, dummy + "");
        }
        /*
         * Enables or disables betting based on the data sent.
         */
        private void ToggleBettingHandler(string evt, byte[] bettingState)
        {
            if (bettingState[0] == 0)
            {
                IsBettingAllowed = false;
                DisableBettingButtons();
            }
            else
            {
                SetNewTip(GameFacade.Strings["UIText", "263", "1"]); // "Place your Bets..."
                IsBettingAllowed = true;
                EnableBettingButtons();
            }
        }
        /*
         * Disables hand buttons.
         */
        private void DisableHandButtonsHandler(string evt, byte[] btnState)
        {
            DisableHandButtons();
        }
        /*
         * Every time the betting round begins, this event is broadcast to all players in the lobby. 
         */
        private void NewGameHandler(string evt, byte[] nothing)
        {
            // all hands and totals reset
            ResetAllHands();

            // no one's hand is active
            SetActiveOtherPlayerHand(null);

            // allow betting
            IsBettingAllowed = true;
            EnableBettingButtons();

            // hide insured and split labels
            InsuredLabel.Visible = false;
            Player1SplitLetter.Visible = false;
            Player2SplitLetter.Visible = false;
            Player3SplitLetter.Visible = false;
            Player4SplitLetter.Visible = false;

            SetNewTip(GameFacade.Strings["UIText", "263", "1"]); // "Place your Bets..."
        }
        /*
         * Stop the betting round and begin the dealing sequence
         */
        private void StartDealingSequenceHandler(string evt, byte[] serializedCards)
        {
            IsBettingAllowed = false;
            SetNewTip(GameFacade.Strings["UIText", "263", "2"]); // "Dealing."
            DisableBettingButtons();
            List<string> handSizesAndCards = new List<string>(VMEODGameCompDrawACardData.DeserializeStrings(serializedCards));
            SyncAllHands(handSizesAndCards, true);
        }
        /*
         * This event is for latecomers who have joined a game already in play, where other players have a disparate collection of cards on the table.
         */
        private void SyncAllActiveHandsHandler(string evt, byte[] serializedCards)
        {
            List<string> handSizesAndCards = new List<string>(VMEODGameCompDrawACardData.DeserializeStrings(serializedCards));
            SyncAllHands(handSizesAndCards, false);
        }
        /*
         * Update (synchronize) everyone's bet amounts in the UI.
         */
        private void UpdateAllBetsHandler(string evt, byte[] data)
        {
            string[] bets = VMEODGameCompDrawACardData.DeserializeStrings(data);
            int dummy = 0;
            Int32.TryParse(bets[0], out dummy);
            UpdatePlayerBetAmount(0, dummy + "");
            dummy = 0;
            Int32.TryParse(bets[1], out dummy);
            UpdatePlayerBetAmount(1, dummy + "");
            dummy = 0;
            Int32.TryParse(bets[2], out dummy);
            UpdatePlayerBetAmount(2, dummy + "");
            dummy = 0;
            Int32.TryParse(bets[3], out dummy);
            UpdatePlayerBetAmount(3, dummy + "");
        }
        /*
         * Set dealer hand active and sync/update the cards in the container.
         */
        private void DealerHandHandler(string evt, byte[] serializedCards)
        {
            SetTime(0);
            if (serializedCards == null) return;
            string[] cards = VMEODGameCompDrawACardData.DeserializeStrings(serializedCards);
            if (cards.Length > 0)
                UpdateOtherPlayerHand(4, true, cards);
            // set tip, so-and-so's turn
            if (DealersName[DealersName.Length - 1].Equals("s"))
                SetNewTip(DealersName + GameFacade.Strings["UIText", "263", "5"]); // "' turn."
            else
                SetNewTip(DealersName + GameFacade.Strings["UIText", "263", "4"]); // "'s turn."
        }
        /*
         * Sets a tip for winning and losing
         */
        private void WinLossHandler(string evt, byte[] winAmount)
        {
            var winnings = BitConverter.ToInt32(winAmount, 0);
            if (winnings > 0) // you win
            {
                int winningIndex = Random.Next(25, 30);
                var winningString = (GameFacade.Strings["UIText", "259", winningIndex + ""]); // slots strings 25-30 about winning
                winningString = winningString.Replace("%i", winnings + "");
                SetNewTip(winningString);
                // play the ka-ching sound
                HIT.HITVM.Get().PlaySoundEvent("ui_object_place");
            }
            else // you lose
            {
                int losingIndex = Random.Next(31, 34);
                var losingString = (GameFacade.Strings["UIText", "259", losingIndex + ""]); // slots strings 31-33 about losing
                SetNewTip(losingString);
            }
        }
        #endregion
        #region button_handlers
        /*
         * When buttons are pressed...
         */
        private void ChipButtonClickedHandler(int buttonNumber)
        {
            // get the current bet text
            string currentText = TotalBetEntry.CurrentText;
            int currentBet = -1;

            // if it's a valid number, add the value of the button press to it
            if (Int32.TryParse(currentText, out currentBet))
            {
                // very funny, you put a negative number
                currentBet = Math.Abs(currentBet);

                // increment by the value of the chip button
                if (buttonNumber == 1)
                    currentBet += 1;
                else if (buttonNumber == 2)
                    currentBet += 5;
                else if (buttonNumber == 3)
                    currentBet += 10;
                else if (buttonNumber == 4)
                    currentBet += 25;
                else //if (buttonNumber == 5)
                    currentBet += 100;

                // make sure it's not over the character limit
                if (currentBet >= 10000)
                    currentBet = 9999;

                // update in the display
                TotalBetEntry.CurrentText = currentBet + "";
            }
            else // not a valid number, but now it is
                TotalBetEntry.CurrentText = "0";
        }
        private void HitButtonClickedHandler(UIElement btn)
        {
            DisableHandButtons();
            Send("blackjack_hit_request", new byte[0]);
        }
        private void StandButtonClickedHandler(UIElement btn)
        {
            DisableHandButtons();
            Send("blackjack_stand_request", new byte[0]);
        }
        private void DoubleButtonClickedHandler(UIElement btn)
        {
            DisableHandButtons();
            Send("blackjack_double_request", new byte[0]);
        }
        private void SplitButtonClickedHandler(UIElement btn)
        {
            DisableHandButtons();
            Send("blackjack_split_request", new byte[0]);
        }
        private void BetButtonClickedHandler(UIElement btn)
        {
            DisableBettingButtons();
            // attempt to submit the bet amount by parsing the text in the textedit

            // get the current bet text
            string currentText = TotalBetEntry.CurrentText;
            string errorMessageTitle = "Betting Error";
            string errorMessage = "";
            int currentBet = -1;

            // if it's a valid number, add the value of the button press to it
            if (Int32.TryParse(currentText, out currentBet))
            {
                if (currentBet < MinBet)
                    errorMessage = AlertStrings[(byte)VMEODBlackjackAlerts.Bet_Too_Low].Replace("$m", "$" + MinBet);
                else if (currentBet > MaxBet)
                    errorMessage = AlertStrings[(byte)VMEODBlackjackAlerts.Bet_Too_High].Replace("$M", "$" + MaxBet);
            }
            else
            {
                // not a valid number
                errorMessage = "That is not a valid number!";
            }
            // enables betting again if an error is thrown
            if (errorMessage.Length > 0)
            {
                ShowUIAlert(errorMessageTitle, errorMessage, EnableBettingButtons);
            }
            else
                Send("blackjack_bet_request", BitConverter.GetBytes(currentBet));
        }
        #endregion
        #region timers
        /*
         * The interval at which to deal cards during the initial deal phase.
         */
        private void DealTimerHandler(object source, ElapsedEventArgs args)
        {
            if (++DealingIndex < CardsToDeal.Count)
            {
                var currentList = CardsToDeal[DealingIndex].ToList();
                var playerNumber = Int32.Parse(currentList[0]);
                currentList.RemoveAt(0); // what remains are the cards

                // update the player's hand in the upper UI
                UpdateOtherPlayerHand(playerNumber, true, currentList.ToArray());

                // I am this player, update my main hand, too (in the lower UI)
                if (playerNumber == MyPlayerNumber)
                {
                    if (MainPlayerCardContainers.Count == 0)
                        AddMainCardContainer(0, currentList.ToArray());
                    else
                        SetNewActiveHand(0, currentList.ToArray());
                }
                // play a sound
                HIT.HITVM.Get().PlaySoundEvent("blackjack_cards_hit");
            }
            else
                DealTimer.Stop();
        }
        #endregion
        #region private
        /*
         * Shows a UI alert and even allows an action argument for when the window is closed.
         */
        private UIAlert ShowUIAlert(string title, string message, Action action)
        {
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = title,
                Message = message,
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    UIScreen.RemoveDialog(alert);
                    action?.Invoke();
                }),
            }, true);
            return alert;
        }
        /*
         * Lights up the active player 0 - 3, corresponding to players 1 - 4. Updates their cards.
         */
        private void UpdatePlayerCardContainerHandler(string evt, byte[] serializedPlayerNumberAndCards)
        {
            if (serializedPlayerNumberAndCards == null)
                return;
            var data = VMEODGameCompDrawACardData.DeserializeStrings(serializedPlayerNumberAndCards);
            if (data != null && data.Length > 1)
            {
                int player = -1;
                var cardsList = new List<string>(data); // card names value_suit e.g. "Five_Clubs"
                cardsList.RemoveAt(0);
                if (Int32.TryParse(data[0], out player))
                {
                    UpdateOtherPlayerHand(player, true, cardsList.ToArray()); // set active
                    // set tip, so-and-so's turn
                    if (player == MyPlayerNumber)
                        SetNewTip(GameFacade.Strings["UIText", "263", "3"]); // "Your turn."
                    else
                    {
                        var button = Lobby.GetPlayerButton(player);
                        if (button != null && button.Avatar != null)
                        {
                            var name = button.Avatar.Name;
                            if (name != null)
                            {
                                if (name[name.Length - 1].Equals("s") || name[name.Length - 1].Equals("S"))
                                {
                                    SetNewTip(name + GameFacade.Strings["UIText", "263", "5"]); // "' turn."
                                    return;
                                }
                                else
                                {
                                    SetNewTip(name + GameFacade.Strings["UIText", "263", "4"]); // "'s turn."
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }
        // shows to all players the action just selected by the current player's turn
        private void PlayerChoiceBroadcastHandler(string evt, byte[] player)
        {
            string appendix = ".";
            if (evt[11].Equals('i')) // "blackjack_hit_broadcast"
                appendix = ": " + GameFacade.Strings["UIText", "263", "6"]; // "Hit"
            else if (evt[11].Equals('t')) // "blackjack_stand_broadcast"
                appendix = ": " + GameFacade.Strings["UIText", "263", "7"]; // "Stand"
            else if (evt[11].Equals('p')) // "blackjack_split_broadcast"
            {
                appendix = ": " + GameFacade.Strings["UIText", "263", "8"]; // "Split"
                MakeSplitLabelVisible(player[0]);
                // play the ka-ching sound
                HIT.HITVM.Get().PlaySoundEvent("ui_object_place");
            }
            else if (evt[11].Equals('o')) // "blackjack_double_broadcast"
            {
                appendix = ": " + GameFacade.Strings["UIText", "263", "9"]; // "Double down"
                // play the ka-ching sound
                HIT.HITVM.Get().PlaySoundEvent("ui_object_place");
            }
                // other option is 'a' for "blackjack_late_comer"

                // set the correct active hand
                SetActiveOtherPlayerHand(player[0]);

            if (player[0] == MyPlayerNumber)
                SetNewTip(GameFacade.Strings["UIText", "263", "3"].Replace(".", appendix)); // "Your turn."
            else
            {
                var button = Lobby.GetPlayerButton(player[0]);
                if (button != null && button.Avatar != null)
                {
                    var name = button.Avatar.Name;
                    if (name != null)
                    {
                        if (name[name.Length - 1].Equals("s") || name[name.Length - 1].Equals("S"))
                        {
                            SetNewTip(name + GameFacade.Strings["UIText", "263", "5"].Replace(".", appendix)); // "' turn."
                            return;
                        }
                        else
                        {
                            SetNewTip(name + GameFacade.Strings["UIText", "263", "4"].Replace(".", appendix)); // "'s turn."
                            return;
                        }
                    }
                }
            }
        }
        private void SendByteMessage(EODMessageNode node)
        {
            Send("blackjack_" + node.EventName, node.EventByteData);
        }
        private void SendStringMessage(EODMessageNode node)
        {
            Send("blackjack_" + node.EventName, node.EventStringData);
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
                OwnerPanel.ResumeFromBetAmount(evt.Remove(0, 10), minOrMaxBetString); // truncate "blackjack_"
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
                OwnerPanel.InputFailHandler(evt.Remove(0, 10), message); // truncate "blackjack_"
            }
        }
        /*
         * todo: If the LiveMode & EOD invalidation issue is ever solved on the EOD-wide scale, this middle-man method will be unnecessary.
         */
        private void SetNewTip(string newTip)
        {
            SetTip(newTip);
            Parent.Invalidate();
            Parent.Invalidate();
            Parent.Invalidate();
            Parent.Invalidate();
            Parent.Invalidate();
        }
        /*
         * todo: If the LiveMode & EOD invalidation issue is ever solved on the EOD-wide scale, this middle-man method will be unnecessary.
         */
        private void SetNewTime(int newTime)
        {
            SetTime(newTime);
            Parent.Invalidate();
        }
        /*
         * Pedantry at its finest - move the UI Label along the X axis slightly to make it appear more center
         * todo: If font rendering changes, this might need to be tweaked or removed entirely
         */
        private void UpdateCardTotalCaption(UILabel label, string newCaption)
        {
            string oldCaption = label.Caption;
            if (newCaption.Length > 1) // now a double digit number
            {
                if (oldCaption.Length < 2) // was a single digit number or blank
                    label.X -= 4; // move it back
            }
            else // now a single digit number or blank
            {
                if (oldCaption.Length > 1) // was a double digit number
                    label.X += 4; // move it forward
            }
            label.Caption = newCaption;
        }
        /*
         * Idiosyncrasy with EODs. Must occur when a new user joins in order that they have their own buttons.
         */
        private void RefreshButtons()
        {
            btnChip1 = AllButtons[0];
            if (btnChip1 == null)
                btnChip1 = Script.Create<UIButton>("btnChip1");
            btnChip2 = AllButtons[1];
            if (btnChip2 == null)
                btnChip2 = Script.Create<UIButton>("btnChip2");
            btnChip3 = AllButtons[2];
            if (btnChip3 == null)
                btnChip3 = Script.Create<UIButton>("btnChip3");
            btnChip4 = AllButtons[3];
            if (btnChip4 == null)
                btnChip4 = Script.Create<UIButton>("btnChip4");
            btnChip5 = AllButtons[4];
            if (btnChip5 == null)
                btnChip5 = Script.Create<UIButton>("btnChip5");
            btnHit = AllButtons[5];
            if (btnHit == null)
                btnHit = Script.Create<UIButton>("btnHit");
            btnStand = AllButtons[6];
            if (btnStand == null)
                btnStand = Script.Create<UIButton>("btnStand");
            btnDouble = AllButtons[7];
            if (btnDouble == null)
                btnDouble = Script.Create<UIButton>("btnDouble");
            btnSplit = AllButtons[8];
            if (btnSplit == null)
                btnSplit = Script.Create<UIButton>("btnSplit");
            btnBet = AllButtons[9];
            if (btnBet == null)
                btnBet = Script.Create<UIButton>("btnBet");
            btnBetDown = AllButtons[10];
            if (btnBet != null)
                Remove(btnBet);
            btnBetUp = AllButtons[11];
            if (btnBetUp != null)
                Remove(btnBetUp);

            /* Move all buttons down for TallTall*/
            btnChip1.Y += 188;
            btnChip2.Y += 188;
            btnChip3.Y += 188;
            btnChip4.Y += 188;
            btnChip5.Y += 188;
            btnHit.Y += 188;
            btnStand.Y += 188;
            btnDouble.Y += 188;
            btnSplit.Y += 188;
            btnBet.X -= 10;
            btnBet.Y += 188;

            // tooltips
            btnChip1.Tooltip = GameFacade.Strings["UIText", "263", "15"].Replace("%d", "1"); // Add $1 to bet
            btnChip2.Tooltip = GameFacade.Strings["UIText", "263", "15"].Replace("%d", "5"); // Add $5 to bet
            btnChip3.Tooltip = GameFacade.Strings["UIText", "263", "15"].Replace("%d", "10"); // Add $10 to bet
            btnChip4.Tooltip = GameFacade.Strings["UIText", "263", "15"].Replace("%d", "25"); // Add $25 to bet
            btnChip5.Tooltip = GameFacade.Strings["UIText", "263", "15"].Replace("%d", "100"); // Add $100 to bet
            btnBet.Tooltip = GameFacade.Strings["UIText", "263", "14"]; // Place bet
            btnHit.Tooltip = GameFacade.Strings["UIText", "263", "6"]; // "Hit"
            btnStand.Tooltip = GameFacade.Strings["UIText", "263", "7"]; // "Stand"
            btnSplit.Tooltip = GameFacade.Strings["UIText", "263", "8"]; // "Split"
            btnDouble.Tooltip = GameFacade.Strings["UIText", "263", "9"]; // "Double down"

            // Add as children
            Add(btnChip1);
            Add(btnChip2);
            Add(btnChip3);
            Add(btnChip4);
            Add(btnChip5);
            Add(btnHit);
            Add(btnStand);
            Add(btnDouble);
            Add(btnSplit);
            Add(btnBet);

            // subscribe
            btnChip1.OnButtonClick += (btn) => { ChipButtonClickedHandler(1); };
            btnChip2.OnButtonClick += (btn) => { ChipButtonClickedHandler(2); };
            btnChip3.OnButtonClick += (btn) => { ChipButtonClickedHandler(3); };
            btnChip4.OnButtonClick += (btn) => { ChipButtonClickedHandler(4); };
            btnChip5.OnButtonClick += (btn) => { ChipButtonClickedHandler(5); };
            btnHit.OnButtonClick += HitButtonClickedHandler;
            btnStand.OnButtonClick += StandButtonClickedHandler;
            btnDouble.OnButtonClick += DoubleButtonClickedHandler;
            btnSplit.OnButtonClick += SplitButtonClickedHandler;
            btnBet.OnButtonClick += BetButtonClickedHandler;
        }
        /*
         * The lower UI holds up to 4 hands and their totals, so the player can see better, especially if they end up splitting.
         */
        private void PlayerLowerUIIinit()
        {
            // text
            labelTotalBet.X -= 19;
            labelTotalBet.Y += 183;
            TotalBetEntry.Y += 188;
            TotalBetEntry.CurrentText = "0";

            // images
            var buttonSeatOffset = new Vector2(3, 3);
            var tallTallOffset = new Vector2(0, 188);
            betEntryBox = Script.Create<UIImage>("betEntryBox");
            AddAt(1, betEntryBox);
            betEntryBox.Y += 188;
            BtnDoubleBack = new UIImage(imageBtnSeat)
            {
                Position = btnDouble.Position - buttonSeatOffset + tallTallOffset
            };
            BtnSplitBack = new UIImage(imageBtnSeat)
            {
                Position = btnSplit.Position - buttonSeatOffset + tallTallOffset
            };
            BtnBetBack = new UIImage(imageBtnSeat)
            {
                Position = btnBet.Position - buttonSeatOffset + tallTallOffset
            };
            BtnBetBack.X -= 10;
            AddAt(2, BtnDoubleBack);
            AddAt(2, BtnSplitBack);
            AddAt(2, BtnBetBack);

            // eod_dc_selectcardback.bmp
            MainCardBack = new UISlotsImage(GetTexture(0x000002D600000001)).DoubleTextureDraw(0, 0, MAIN_CARD_BACK_WIDTH / 2, 105,
                238, 0, MAIN_CARD_BACK_WIDTH / 2, 105, true, false);
            MainCardBack.X = 154;
            MainCardBack.Y = btnChip2.Y + 191;
            AddAt(6, MainCardBack);

            InsuredLabel = new UILabel()
            {
                Caption = Insured,
                X = labelTotalBet.X + 42,
                Y = TotalBetEntry.Y + 25,
                Alignment = TextAlignment.Center,
                Visible = false
            };
            Add(InsuredLabel);
        }
        /*
         * The Upper UI holds representations of all players' hands, hand totals, including the dealer's. It also has the 4 players' bets and UIVMPersonButtons.
         */
        private void PlayerUpperUIInit()
        {
            // ./uigraphics/ucp/livepanel/lpanel_eodsubfulltall.bmp
            EODTallBack = new UIImage(GetTexture(0x000004E900000001))
            {
                Position = new Vector2(10, 88) // .X - 20 .Y + 58
            };
            AddAt(0, EODTallBack);

            // ./uigraphics/ucp/livepanel/lpanel_eodlayoutnonetall.bmp
            EODTallBackEnd = new UIImage(GetTexture(0x000004E300000001))
            {
                Position = new Vector2(420, 87) // .X - 20 .Y + 58
            };
            AddAt(1, EODTallBackEnd);

            var captionStyle = labelTotalBet.CaptionStyle.Clone();
            captionStyle.Size += 2;
            labelTotalBet.CaptionStyle = captionStyle;

            // positions only
            playerOffset_Head = Script.Create<UIImage>("playerOffset_Head");
            playerOffset_Head.X += 10;
            playerOffset_Head.Y -= 8;
            playerOffset_Bet = Script.Create<UIImage>("playerOffset_Bet");
            playerOffset_Bet.Position = new Vector2(34, 45);
            playerOffset_Cards = Script.Create<UIImage>("playerOffset_Cards");
            playerOffset_Cards.Y = 5;
            offset_HandTotal = Script.Create<UIImage>("offset_HandTotal");
            offset_ButtonSeat = Script.Create<UIImage>("offset_ButtonSeat");
            offset_PlayerSeat = Script.Create<UIImage>("offset_PlayerSeat");
            dealerPos_Head = Script.Create<UIImage>("dealerPos_Head");
            dealerPos_Cards = Script.Create<UIImage>("dealerPos_Cards");
            var cardTotalOffset = new Vector2(4, -3);

            // background images for "other" players and dealer
            playerPos1 = Script.Create<UIImage>("playerPos1");
            playerPos1.X += 5;
            playerPos1.Y += 58;
            playerPos1.Texture = imagePlayerBox;
            Add(playerPos1);
            Player1Head = new UIImage(PlayerPicturePlaceHolder)
            {
                X = playerPos1.X - playerOffset_Head.X,
                Y = playerPos1.Y + playerOffset_Head.Y
            };
            Player1Head.X -= Player1Head.Width;
            Add(Player1Head);
            Player1BetBack = new UIImage(PlayerBetBack);
            Player1BetBack.X = playerPos1.X - (Player1BetBack.Width * 0.75f);
            Player1BetBack.ScaleX = 0.75f;
            Player1BetBack.Y = (playerPos1.Y + playerPos1.Height) - (Player1BetBack.Height);
            Player1BetAmount = new UILabel()
            {
                X = Player1BetBack.X,
                Y = Player1BetBack.Y - 1,
                Alignment = TextAlignment.Center
            };
            Player1CardContainer = new CardHand()
            {
                Position = playerPos1.Position + playerOffset_Cards.Position
            };
            Add(Player1CardContainer);
            Player1TotalBack = new UIImage(PlayerPicturePlaceHolder)
            {
                X = playerPos1.X + playerPos1.Width,
                Y = playerPos1.Y
            };
            Player1TotalBack.X -= Player1TotalBack.Width * (2f / 3f);
            Player1TotalBack.ScaleX = Player1TotalBack.ScaleY = 0.66f;
            Add(Player1TotalBack);
            Player1CardTotal = new UILabel()
            {
                X = Player1TotalBack.X + cardTotalOffset.X,
                Y = Player1TotalBack.Y + cardTotalOffset.Y + 1,
                Alignment = TextAlignment.Center,
            };
            Add(Player1CardTotal);
            Player1SplitLetter = new UILabel()
            {
                X = Player1CardTotal.X - 1,
                Y = Player1CardTotal.Y + 15,
                Alignment = TextAlignment.Center,
                Caption = "S",
                Visible = false
            };
            Add(Player1SplitLetter);

            playerPos2 = Script.Create<UIImage>("playerPos2");
            playerPos2.X += 45;
            playerPos2.Y += 58;
            playerPos2.Texture = imagePlayerBox;
            Add(playerPos2);
            Player2Head = new UIImage(PlayerPicturePlaceHolder)
            {
                X = playerPos2.X - playerOffset_Head.X,
                Y = playerPos2.Y + playerOffset_Head.Y,
            };
            Player2Head.X -= Player2Head.Width;
            Add(Player2Head);
            Player2BetBack = new UIImage(PlayerBetBack);
            Player2BetBack.X = playerPos2.X - (Player2BetBack.Width * 0.75f);
            Player2BetBack.ScaleX = 0.75f;
            Player2BetBack.Y = (playerPos2.Y + playerPos2.Height) - (Player2BetBack.Height);
            Player2BetAmount = new UILabel()
            {
                X = Player2BetBack.X,
                Y = Player2BetBack.Y - 1,
                Alignment = TextAlignment.Center
            };
            Add(Player2BetAmount);
            Player2CardContainer = new CardHand()
            {
                Position = playerPos2.Position + playerOffset_Cards.Position
            };
            Add(Player2CardContainer);
            Player2TotalBack = new UIImage(PlayerPicturePlaceHolder)
            {
                X = playerPos2.X + playerPos2.Width,
                Y = playerPos2.Y
            };
            Player2TotalBack.X -= Player2TotalBack.Width * (2f / 3f);
            Player2TotalBack.ScaleX = Player2TotalBack.ScaleY = 0.66f;
            Add(Player2TotalBack);
            Player2CardTotal = new UILabel()
            {
                X = Player2TotalBack.X + cardTotalOffset.X,
                Y = Player2TotalBack.Y + cardTotalOffset.Y,
                Alignment = TextAlignment.Center
            };
            Add(Player2CardTotal);
            Player2SplitLetter = new UILabel()
            {
                X = Player2CardTotal.X - 1,
                Y = Player2CardTotal.Y + 15,
                Alignment = TextAlignment.Center,
                Caption = "S",
                Visible = false
            };
            Add(Player2SplitLetter);

            playerPos3 = Script.Create<UIImage>("playerPos3");
            playerPos3.X += 5;
            playerPos3.Y += 53;
            playerPos3.Texture = imagePlayerBox;
            Add(playerPos3);
            Player3Head = new UIImage(PlayerPicturePlaceHolder)
            {
                X = playerPos3.X - playerOffset_Head.X,
                Y = playerPos3.Y + playerOffset_Head.Y
            };
            Player3Head.X -= Player3Head.Width;
            Add(Player3Head);
            Player3BetBack = new UIImage(PlayerBetBack);
            Player3BetBack.X = playerPos3.X - (Player3BetBack.Width * 0.75f);
            Player3BetBack.ScaleX = 0.75f;
            Player3BetBack.Y = (playerPos3.Y + playerPos3.Height) - (Player3BetBack.Height);
            Player3BetAmount = new UILabel()
            {
                X = Player3BetBack.X,
                Y = Player3BetBack.Y - 1,
                Alignment = TextAlignment.Center
            };
            Add(Player3BetAmount);
            Player3CardContainer = new CardHand()
            {
                Position = playerPos3.Position + playerOffset_Cards.Position
            };
            Add(Player3CardContainer);
            Player3TotalBack = new UIImage(PlayerPicturePlaceHolder)
            {
                X = playerPos3.X + playerPos3.Width,
                Y = playerPos3.Y
            };
            Player3TotalBack.X -= Player3TotalBack.Width * (2f / 3f);
            Player3TotalBack.ScaleX = Player3TotalBack.ScaleY = 0.66f;
            Add(Player3TotalBack);
            Player3CardTotal = new UILabel()
            {
                X = Player3TotalBack.X + cardTotalOffset.X,
                Y = Player3TotalBack.Y + cardTotalOffset.Y,
                Alignment = TextAlignment.Center
            };
            Add(Player3CardTotal);
            Player3SplitLetter = new UILabel()
            {
                X = Player3CardTotal.X - 1,
                Y = Player3CardTotal.Y + 15,
                Alignment = TextAlignment.Center,
                Caption = "S",
                Visible = false
            };
            Add(Player3SplitLetter);

            playerPos4 = Script.Create<UIImage>("playerPos4");
            playerPos4.X += 45;
            playerPos4.Y += 53;
            playerPos4.Texture = imagePlayerBox;
            Add(playerPos4);
            Player4Head = new UIImage(PlayerPicturePlaceHolder)
            {
                X = playerPos4.X - playerOffset_Head.X,
                Y = playerPos4.Y + playerOffset_Head.Y
            };
            Player4Head.X -= Player4Head.Width;
            Add(Player4Head);
            Player4BetBack = new UIImage(PlayerBetBack);
            Player4BetBack.X = playerPos4.X - (Player4BetBack.Width * 0.75f);
            Player4BetBack.ScaleX = 0.75f;
            Player4BetBack.Y = (playerPos4.Y + playerPos4.Height) - (Player4BetBack.Height);
            Player4BetAmount = new UILabel()
            {
                X = Player4BetBack.X,
                Y = Player4BetBack.Y - 1,
                Alignment = TextAlignment.Center
            };
            Add(Player4BetAmount);
            Player4CardContainer = new CardHand()
            {
                Position = playerPos4.Position + playerOffset_Cards.Position
            };
            Add(Player4CardContainer);
            Player4TotalBack = new UIImage(PlayerPicturePlaceHolder)
            {
                X = playerPos4.X + playerPos4.Width,
                Y = playerPos4.Y
            };
            Player4TotalBack.X -= Player4TotalBack.Width * (2f / 3f);
            Player4TotalBack.ScaleX = Player4TotalBack.ScaleY = 0.66f;
            Add(Player4TotalBack);
            Player4CardTotal = new UILabel()
            {
                X = Player4TotalBack.X + cardTotalOffset.X,
                Y = Player4TotalBack.Y + cardTotalOffset.Y,
                Alignment = TextAlignment.Center
            };
            Add(Player4CardTotal);
            Player4SplitLetter = new UILabel()
            {
                X = Player4CardTotal.X - 1,
                Y = Player4CardTotal.Y + 15,
                Alignment = TextAlignment.Center,
                Caption = "S",
                Visible = false
            };
            Add(Player4SplitLetter);

            DealerPos = new UIImage(imagePlayerBox);
            DealerPos.Position = new Vector2(playerPos1.X - (playerPos2.X - playerPos1.X), (playerPos1.Y + playerPos3.Y) / 2);
            Add(DealerPos);
            DealerHead = new UIImage(PlayerPicturePlaceHolder)
            {
                X = DealerPos.X,
                Y = Player1Head.Y,
            };
            DealerHead.X += (DealerPos.Width - DealerHead.Width) / 2;
            Add(DealerHead);
            DealerBetBack = new UIImage(image_BetAmount);
            DealerBetBack.X = DealerPos.X + ((DealerPos.Width - DealerBetBack.Width) / 2);
            DealerBetBack.Y = DealerPos.Y + DealerPos.Height;
            DealerBetAmount = new UITextEdit()
            {
                X = DealerBetBack.X,
                Y = DealerBetBack.Y + 4,
                Size = DealerBetBack.Size.ToVector2(),
                CurrentText = "Dealer",
                Alignment = TextAlignment.Center,
                TextStyle = captionStyle,
                Mode = UITextEditMode.ReadOnly
            };
            DealerPos.Y -= 1;
            DealerCardContainer = new CardHand()
            {
                Position = DealerPos.Position + playerOffset_Cards.Position
            };
            Add(DealerCardContainer);
            DealerTotalBack = new UIImage(PlayerPicturePlaceHolder)
            {
                X = DealerPos.X + DealerPos.Width,
                Y = DealerPos.Y
            };
            DealerTotalBack.ScaleX = DealerTotalBack.ScaleY = 0.66f;
            DealerTotalBack.X -= DealerTotalBack.Width * (2f / 3f);
            Add(DealerTotalBack);
            DealerCardTotal = new UILabel()
            {
                X = DealerTotalBack.X + cardTotalOffset.X,
                Y = DealerTotalBack.Y + cardTotalOffset.Y,
                Alignment = TextAlignment.Center
            };
            Add(DealerCardTotal);

            // add labels
            Add(Player1BetBack);
            Add(Player1BetAmount);
            Add(Player2BetBack);
            Add(Player2BetAmount);
            Add(Player3BetBack);
            Add(Player3BetAmount);
            Add(Player4BetBack);
            Add(Player4BetAmount);
            Add(DealerBetBack);
            Add(DealerBetAmount);
        }
        /*
         * Removes every card from view, from above and below. Updates all captions to be empty (not even displaying zero). Empties lists of containers/labels.
         */
        private void ResetAllHands()
        {
            // reset hands from below
            if (MainPlayerCardContainers != null)
            {
                foreach (var container in MainPlayerCardContainers)
                {
                    if (container != null)
                        container.Reset();
                }
            }
            if (MainPlayerCardTotals != null)
            {
                foreach (var textEdit in MainPlayerCardTotals)
                {
                    if (textEdit != null)
                        textEdit.CurrentText = "";
                }
            }
            // reset hands from above
            if (Player1CardContainer != null)
            {
                Player1CardContainer.Reset();
                UpdateCardTotalCaption(Player1CardTotal, "");
            }
            if (Player2CardContainer != null)
            {
                Player2CardContainer.Reset();
                UpdateCardTotalCaption(Player2CardTotal, "");
            }
            if (Player3CardContainer != null)
            {
                Player3CardContainer.Reset();
                UpdateCardTotalCaption(Player3CardTotal, "");
            }
            if (Player4CardContainer != null)
            {
                Player4CardContainer.Reset();
                UpdateCardTotalCaption(Player4CardTotal, "");
            }
            if (DealerCardContainer != null)
            {
                DealerCardContainer.Reset();
                UpdateCardTotalCaption(DealerCardTotal, "");
            }
            if (Player1BetAmount != null)
                Player1BetAmount.Caption = "";
            if (Player2BetAmount != null)
                Player2BetAmount.Caption = "";
            if (Player3BetAmount != null)
                Player3BetAmount.Caption = "";
            if (Player4BetAmount != null)
                Player4BetAmount.Caption = "";

            MainPlayerCardContainers = new List<CardHand>();
            MainPlayerCardTotals = new List<UITextEdit>();

            // no one is playing yet
            DealingIndex = -1;
        }
        /*
         * This method happens at two specific times. If @param: useQueue:bool is true, the first pair of cards is being dealt to all active players and the
         * dealer. If it is false, this is happening when a new player has joined the table while a game is already in progress. It updates everyone's hands.
         */
        private void SyncAllHands(List<string> handSizesAndCards, bool useQueue)
        {
            if (handSizesAndCards.Count < 6)
                return;

            int player1NumCardsInHand = 0;
            int player2NumCardsInHand = 0;
            int player3NumCardsInHand = 0;
            int player4NumCardsInHand = 0;
            int dealerNumCardsInHand = 0;

            if (Int32.TryParse(handSizesAndCards[0], out player1NumCardsInHand) &&
                Int32.TryParse(handSizesAndCards[1], out player2NumCardsInHand) &&
                Int32.TryParse(handSizesAndCards[2], out player3NumCardsInHand) &&
                Int32.TryParse(handSizesAndCards[3], out player4NumCardsInHand) &&
                Int32.TryParse(handSizesAndCards[4], out dealerNumCardsInHand))
                {

                handSizesAndCards.RemoveAt(4);
                handSizesAndCards.RemoveAt(3);
                handSizesAndCards.RemoveAt(2);
                handSizesAndCards.RemoveAt(1);
                handSizesAndCards.RemoveAt(0);

                // what remains of handSizesAndCards is simply the list of cards

                if (useQueue)
                {
                    // cards should always come in pairs, 2 for each player, up to 10 cards total with dealer
                    CardsToDeal = new List<string[]>();
                    int cardIndex = 0;

                    // take the odd cards will be everyone's first cards, where even will be everyone's second cards
                    List<string> firstCards = handSizesAndCards.Where((value, index) => index % 2 == 0).ToList();
                    List<string> secondCards = handSizesAndCards.Where((value, index) => index % 2 == 1).ToList();

                    int dealindex = 0;

                    if (player1NumCardsInHand > 0)
                    {
                        // add second deal
                        CardsToDeal.Add(new string[] { "0", firstCards[cardIndex], secondCards[cardIndex] });
                        // insert first deal
                        CardsToDeal.Insert(dealindex++, new string[] { "0", firstCards[cardIndex] });
                        cardIndex++;
                    }
                    if (player2NumCardsInHand > 0)
                    {
                        // add second deal
                        CardsToDeal.Add(new string[] { "1", firstCards[cardIndex], secondCards[cardIndex] });
                        // insert first deal
                        CardsToDeal.Insert(dealindex++, new string[] { "1", firstCards[cardIndex] });
                        cardIndex++;
                    }
                    if (player3NumCardsInHand > 0)
                    {
                        // add second deal
                        CardsToDeal.Add(new string[] { "2", firstCards[cardIndex], secondCards[cardIndex] });
                        // insert first deal
                        CardsToDeal.Insert(dealindex++, new string[] { "2", firstCards[cardIndex] });
                        cardIndex++;
                    }
                    if (player4NumCardsInHand > 0)
                    {
                        // add second deal
                        CardsToDeal.Add(new string[] { "3", firstCards[cardIndex], secondCards[cardIndex] });
                        // insert first deal
                        CardsToDeal.Insert(dealindex++, new string[] { "3", firstCards[cardIndex] });
                        cardIndex++;
                    }
                    // add dealer's cards
                    // add second deal
                    CardsToDeal.Add(new string[] { "4", firstCards[cardIndex], "back" }); // dealer's second card is hidden
                    // insert first deal
                    CardsToDeal.Insert(dealindex++, new string[] { "4", firstCards[cardIndex] });


                    DealingIndex = -1;

                    // set the timer for dealing and start it
                    DealTimer.Start();
                }
                else
                {
                    int cardIndex = 0;
                    List<string> cards = null;

                    if (player1NumCardsInHand > 0)
                    {
                        cards = new List<string>();

                        for (int i = 0; i < player1NumCardsInHand; i++)
                            cards.Add(handSizesAndCards[cardIndex++]);

                        UpdateOtherPlayerHand(0, false, cards.ToArray());
                    }
                    if (player2NumCardsInHand > 0)
                    {
                        cards = new List<string>();

                        for (int i = 0; i < player2NumCardsInHand; i++)
                            cards.Add(handSizesAndCards[cardIndex++]);

                        UpdateOtherPlayerHand(1, false, cards.ToArray());
                    }
                    if (player3NumCardsInHand > 0)
                    {
                        cards = new List<string>();

                        for (int i = 0; i < player3NumCardsInHand; i++)
                            cards.Add(handSizesAndCards[cardIndex++]);

                        UpdateOtherPlayerHand(2, false, cards.ToArray());
                    }
                    if (player4NumCardsInHand > 0)
                    {
                        cards = new List<string>();

                        for (int i = 0; i < player4NumCardsInHand; i++)
                            cards.Add(handSizesAndCards[cardIndex++]);

                        UpdateOtherPlayerHand(3, false, cards.ToArray());
                    }
                    if (dealerNumCardsInHand > 0)
                    {
                        cards = new List<string>();

                        for (int i = 0; i < dealerNumCardsInHand; i++)
                            cards.Add(handSizesAndCards[cardIndex++]);

                        UpdateOtherPlayerHand(4, false, cards.ToArray());
                    }
                }
            }
        }
        /*
         * @param cards: string names of cards 2 and 3 go into new card container, 0 and 1 go into container that was just split
         * @param splittingHandPosition: the index of the card container thats was successfully split
         */
        private void AddMainCardContainer(int splittingHandPosition, params string[] cards)
        {
            int numberOfDecks = MainPlayerCardContainers.Count;
            if (cards == null || cards.Length == 0 || splittingHandPosition > numberOfDecks)
                return;

            // make the new container to the correct scale and add to UI
            var mainPlayerCardContainer = new CardHand();
            Add(mainPlayerCardContainer);

            // add the cards into the new container and set inactive/collapse
            if (cards.Length > 3)
            {
                mainPlayerCardContainer.AddCard(cards[2]);
                mainPlayerCardContainer.AddCard(cards[3]);
            }
            else
            {
                mainPlayerCardContainer.AddCard(cards[0]);
            }

            // add the new UITextEdit for the new hand's total value
            var mainPlayerCardTotal = new UITextEdit
            {
                TextStyle = TotalBetEntry.TextStyle,
                Alignment = TextAlignment.Center,
                Size = new Vector2(43, 27),
                Mode = UITextEditMode.ReadOnly,
                CurrentText = "" + mainPlayerCardContainer.TotalValueOfCards,
                MaxLines = 1,
                MaxChars = 4
            };
            Add(mainPlayerCardTotal);

            //new container is always after the splitting hand
            int insertIndex = splittingHandPosition + 1;

            if (MainPlayerCardContainers.Count > 0)
            {
                MainPlayerCardContainers.Insert(insertIndex, mainPlayerCardContainer);
                mainPlayerCardContainer.SetInactive();
                // insert the new UITextEdit into list
                MainPlayerCardTotals.Insert(insertIndex, mainPlayerCardTotal);

                // update hand that was split
                ResetTargetHand(MainPlayerCardContainers[splittingHandPosition], MainPlayerCardTotals[splittingHandPosition], new string[] { cards[0], cards[1] });
                //SetNewActiveHand(MainPlayerActiveHand, cards[0], cards[1]);
            }
            else // first hand this game
            {
                MainPlayerCardContainers.Add(mainPlayerCardContainer);
                MainPlayerCardTotals.Add(mainPlayerCardTotal);
                mainPlayerCardContainer.SetActive();
                UpdateMyOtherPlayerHand();
                UpdateMainLayout();
            }
        }
        /*
         * Reset the layout of the large main card containers to fit nicely in the small bit of real estate on the bottom of the EOD
         */
        private void UpdateMainLayout()
        {
            /*
             * First, set the scale and offset based on the number of already existing decks
             */
            int numberOfDecks = MainPlayerCardContainers.Count;
            int numberOfCollapsedDecks = MainPlayerActiveHand;
            float scale;
            Vector2 centerStageActiveHandPosition = MainContainerCenterStage_Offset; // 0 existing hands, so the first 1 uses this
            Vector2 offsetX = new Vector2(COLLAPSED_HANDS_MARGIN, 0);
            Vector2 offsetY = new Vector2(0, 10); // <4 hands use this
            if (numberOfDecks == 4)
            {
                scale = FOUR_HANDS_SCALE;
                offsetY = new Vector2(0, 20);
            }
            else if (numberOfDecks == 3)
            {
                scale = THREE_HANDS_SCALE;
            }
            else if (numberOfDecks == 2)
            {
                scale = TWO_HANDS_SCALE;
            }
            else
            {
                scale = ONE_HAND_SCALE;
            }
            Vector2 collapsedDecksOffset = new Vector2(COLLAPSED_HANDS_MARGIN + UIPlayingCard.FULL_CARD_WIDTH * scale, 0);
            centerStageActiveHandPosition = new Vector2(MAIN_CARD_BACK_WIDTH / 3 - (ONE_HAND_SCALE - scale) * MAIN_CARD_BACK_WIDTH / 3, 0);

            // offset & scale all existing containers properly based on their order in the list in relation to the MainPlayerActiveHand
            int rollingCardCount = 0;
            for (int index = MainPlayerCardContainers.Count - 1; index > -1; index--)
            {
                MainPlayerCardContainers[index].SetScale(scale);
                if (index > MainPlayerActiveHand)
                {
                    MainPlayerCardContainers[index].Position = MainCardBack.Position + offsetX + offsetY + collapsedDecksOffset * rollingCardCount;
                }
                else if (index < MainPlayerActiveHand)
                {
                    MainPlayerCardContainers[index].Position = MainCardBack.Position + offsetY + new Vector2(MAIN_CARD_BACK_WIDTH, 0) - offsetX -
                        collapsedDecksOffset * (index + 1);
                }
                else
                {
                    MainPlayerCardContainers[index].Position = MainCardBack.Position + offsetX + offsetY + centerStageActiveHandPosition
                        + collapsedDecksOffset * rollingCardCount;
                }
                rollingCardCount++;
            }
            // Update all UITextEdit positions & current text
            for (int index = 0; index < MainPlayerCardTotals.Count; index++)
            {
                var cardTotal_Offset = new Vector2(0, UIPlayingCard.CARD_HEIGHT * scale + 2);
                if (MainPlayerCardContainers[index].IsCollapsed)
                {
                    var numCards = MainPlayerCardContainers[index].TotalNumberOfCards;
                    if (numCards == 2)
                        cardTotal_Offset.X = -12;
                    else
                    {
                        cardTotal_Offset.X = -7.5f + ((numCards - 3) * 1.75f);
                    }
                }
                else
                    cardTotal_Offset.X = 2; // from 5
                MainPlayerCardTotals[index].Position = MainPlayerCardContainers[index].Position + cardTotal_Offset;
                //MainPlayerCardTotals[index].CurrentText = MainPlayerCardContainers[index].TotalValueOfCards.ToString();
            }
        }
        /*
         * Sets a new active MAIN hand, then synchronises the cards from the server into that hand. Updates layout.
         */
        private void SetNewActiveHand(int newActiveHand, params string[] cards)
        {
            if (newActiveHand > -1 && newActiveHand < MainPlayerCardContainers.Count)
            {
                // set new active hand
                MainPlayerActiveHand = newActiveHand;
                var activeContainer = MainPlayerCardContainers[MainPlayerActiveHand];
                var activeLabel = MainPlayerCardTotals[MainPlayerActiveHand];
                activeContainer.SetActive();

                // reset the container with with cards sent
                ResetTargetHand(activeContainer, activeLabel, cards);

                // make my cards at the top match my new active hand
                UpdateMyOtherPlayerHand();

                // update the layout
                UpdateMainLayout();
            }
        }
        /*
         * Updates the card graphics in the hand, the label showing the total value.
         */
        private void ResetTargetHand(CardHand container, UITextEdit label, params string[] cards)
        {
            if (container == null)
                return;
            container.Reset();
            if (cards != null)
            {
                foreach (var card in cards)
                    container.AddCard(card);
            }
            if (label != null)
            {
                label.CurrentText = "" + container.TotalValueOfCards;
                if (container.TotalValueOfCards == 0)
                    label.Visible = false;
                else
                    label.Visible = true;

                // deprecated
                /*if (container.Label.Length > 0)
                {
                    if (container.Label[0].Equals('('))
                    {
                        label.CurrentText = container.TotalValueOfCards + container.Label;
                        label.Size = new Vector2(56, 27);
                    }
                    else // blackjack
                    {
                        label.CurrentText = container.Label;
                        label.Size = new Vector2(76, 27);
                    }
                }*/
            }
        }
        // makes the "S" available for the specified player so other players know they're splitting
        private void MakeSplitLabelVisible(int player)
        {
            if (player == 0)
                Player1SplitLetter.Visible = true;
            else if (player == 1)
                Player2SplitLetter.Visible = true;
            else if (player == 2)
                Player3SplitLetter.Visible = true;
            else if (player == 3)
                Player4SplitLetter.Visible = true;
        }
        /*
         * Update the player's hand above that mirrors the active hand from the main player below. This happens when player changes active hands.
         */
        private void UpdateMyOtherPlayerHand()
        {
            var activeContainer = MainPlayerCardContainers[MainPlayerActiveHand];
            // dump cards in current player container (above)
            MyPlayerContainer.Reset();
            // add cards that exist in MainPlayerActiveHand
            foreach (var card in activeContainer.GetChildren())
                MyPlayerContainer.AddCard(card.Tooltip);
            UpdateCardTotalCaption(MyPlayerCardTotal, MyPlayerContainer.TotalValueOfCards.ToString());
        }
        /*
         * Finalise the active hand by synchronizing its contents from the server, updating the total text, and collapsing & making inactive
         * @param: cards - the cards sent from server for sync purposes
         */
        private void CloseActiveHand(bool collapse, string message, params string[] cards)
        {
            if (MainPlayerActiveHand < MainPlayerCardContainers.Count)
            {
                var activeContainer = MainPlayerCardContainers[MainPlayerActiveHand];
                var activeLabel = MainPlayerCardTotals[MainPlayerActiveHand];
                activeContainer.Label = message;

                // reset the container with with cards sent
                ResetTargetHand(activeContainer, activeLabel, cards);

                // make my cards at the top match my new active hand
                UpdateMyOtherPlayerHand();

                // collapse to the side, also makes hand inactive by changing opacity
                if (collapse)
                    activeContainer.Collapse();
                else // only change opacity, do not collapse
                    activeContainer.SetInactive();

                // update the layout
                UpdateMainLayout();
            }
        }
        /*
         * @param: player - 0 through 3 for which player
         * @param: cards[] - the cards in the hand
         */
        private void UpdateOtherPlayerHand(int player, bool setActive, params string[] cards)
        {
            CardHand playerHand = null;
            UILabel playertotal = null;
            if (player == 0) {
                playerHand = Player1CardContainer;
                playertotal = Player1CardTotal;
            }
            else if (player == 1) {
                playerHand = Player2CardContainer;
                playertotal = Player2CardTotal;
            }
            else if (player == 2) {
                playerHand = Player3CardContainer;
                playertotal = Player3CardTotal;
            }
            else if (player == 3) {
                playerHand = Player4CardContainer;
                playertotal = Player4CardTotal;
            }
            else if (player == 4)
            {
                playerHand = DealerCardContainer;
                playertotal = DealerCardTotal;
            }
            else
                return;

            // empty and and refill with cards
            playerHand.Reset();
            foreach (var card in cards)
                playerHand.AddCard(card);
            // update caption with the total value of cards
            UpdateCardTotalCaption(playertotal, playerHand.TotalValueOfCards + "");

            // set as the active hand, if applicable
            if (setActive)
                SetActiveOtherPlayerHand(playerHand);
        }
        /*
         * Sets all players & dealer hands inactive, but sets the @param active
         */
        private void SetActiveOtherPlayerHand(CardHand activeHand)
        {
            Player1CardContainer.SetInactive();
            Player2CardContainer.SetInactive();
            Player3CardContainer.SetInactive();
            Player4CardContainer.SetInactive();
            DealerCardContainer.SetInactive();
            if (activeHand != null)
                activeHand.SetActive();
        }
        private void SetActiveOtherPlayerHand(int player)
        {
            Player1CardContainer.SetInactive();
            Player2CardContainer.SetInactive();
            Player3CardContainer.SetInactive();
            Player4CardContainer.SetInactive();
            DealerCardContainer.SetInactive();
            if (player == 0)
                Player1CardContainer.SetActive();
            else if (player == 1)
                Player2CardContainer.SetActive();
            else if (player == 2)
                Player3CardContainer.SetActive();
            else if (player == 3)
                Player4CardContainer.SetActive();
            else
                DealerCardContainer.SetActive();
        }
        /*
         * @param: player - 0 through 3 for which player
         * @param: amountString - the new bet amount to display
         */
        private void UpdatePlayerBetAmount(int player, string amountString)
        {
            int offsetX = 0;
            if (amountString.Length == 1)
                offsetX = 15;
            else if (amountString.Length == 2)
                offsetX = 10;
            else if (amountString.Length == 3)
                offsetX = 6;
            else if (amountString.Length == 4)
                offsetX = 3;
            else
                return;
            if (player == 0)
            {
                Player1BetCaption = amountString; 
                Player1BetAmount.Caption = Player1BetCaption;
                Player1BetAmount.X = Player1BetBack.X + offsetX;
            }
            else if (player == 1)
            {
                Player2BetCaption = amountString;
                Player2BetAmount.Caption = Player2BetCaption;
                Player2BetAmount.X = Player2BetBack.X + offsetX;
            }
            else if (player == 2)
            {
                Player3BetCaption = amountString;
                Player3BetAmount.Caption = Player3BetCaption;
                Player3BetAmount.X = Player3BetBack.X + offsetX;
            }
            else if (player == 3)
            {
                Player4BetCaption = amountString;
                Player4BetAmount.Caption = Player4BetCaption;
                Player4BetAmount.X = Player4BetBack.X + offsetX;
            }
        }
        /*
         * Update the bet amount in the lower TextEdit and also the label of the corresponding hand of the player above.
         */
        private void UpdateMyBetAmount(string amountString)
        {
            TotalBetEntry.CurrentText = amountString;
            UpdatePlayerBetAmount(MyPlayerNumber, amountString);
        }
        /*
         * Enable and Disable buttons to control the MainPlayerActiveHand, optional split and double buttons.
         */
        private void EnableHandButtons(bool enableSplit, bool enableDouble)
        {
            DisableBettingButtons(); // failsafe
            btnHit.Disabled = false;
            btnStand.Disabled = false;
            if (enableSplit)
                btnSplit.Disabled = false;
            if (enableDouble)
                btnDouble.Disabled = false;
        }
        private void DisableHandButtons()
        {
            btnHit.Disabled = true;
            btnStand.Disabled = true;
            btnSplit.Disabled = true;
            btnDouble.Disabled = true;
        }
        /*
         * Enable and Disable betting buttons and changing the mode of the TextEdit TotalBetEntry.
         */
        private void EnableBettingButtons()
        {
            DisableHandButtons(); // failsafe
            // is it okay to enable betting?
            if (IsBettingAllowed)
            {
                // make textedit editable
                TotalBetEntry.Mode = UITextEditMode.Editor;
                TotalBetEntry.CurrentText = "0";
                // enable button
                btnBet.Disabled = false;
                btnChip1.Disabled = false;
                btnChip2.Disabled = false;
                btnChip3.Disabled = false;
                btnChip4.Disabled = false;
                btnChip5.Disabled = false;
            }
        }
        private void DisableBettingButtons()
        {
            // make textedit readonly
            TotalBetEntry.Mode = UITextEditMode.ReadOnly;
            // disable buttons
            btnBet.Disabled = true;
            btnChip1.Disabled = true;
            btnChip2.Disabled = true;
            btnChip3.Disabled = true;
            btnChip4.Disabled = true;
            btnChip5.Disabled = true;
        }
    }
#endregion
    internal class CardHand : UIContainer
    {
        private float CurrentOpacity = 1f;
        private float _CurrentScale = 1.0f;
        private UIImage PreviousCard;
        private int TotalCardsHidden;
        private int _TotalNumberOfCards;
        private int _TotalValueOfCards;
        private Boolean _IsCollapsed;
        private String _Label;

        public CardHand()
        {
            Label = "";
        }
        public CardHand(float scale)
        {
            _CurrentScale = scale;
        }
        public float CurrentScale
        {
            get { return _CurrentScale; }
        }
        public int TotalValueOfCards
        {
            get { return _TotalValueOfCards; }
        }
        public int TotalNumberOfCards
        {
            get { return _TotalNumberOfCards; }
        }
        public Boolean IsCollapsed
        {
            get { return _IsCollapsed; }
        }
        public void SetScale(float newScale)
        {
            _CurrentScale = newScale;
            UpdateChildren();
        }
        public string Label
        {
            get { return _Label; }
            set { _Label = value; }
        }
        internal void SetInactive()
        {
            CurrentOpacity = 0.4f;
            UpdateChildrenOpacity();
        }
        internal void SetActive()
        {
            if (_IsCollapsed)
                Expand();
            CurrentOpacity = 1f;
            UpdateChildrenOpacity();
        }
        private void UpdateChildren()
        {
            var cardList = new List<UIElement>(GetChildren());
            if (cardList != null && cardList.Count > 0)
            {
                Reset();
                foreach (var card in cardList)
                    AddCard(card.Tooltip);
            }
            if (_IsCollapsed)
                Collapse();
        }
        private void Expand()
        {
            if (_IsCollapsed)
            {
                var cardList = new List<UIElement>(GetChildren());
                if (cardList != null && cardList.Count > 0)
                {
                    foreach (var oldCard in cardList)
                    {
                        Remove(oldCard); // remove as child
                                         // make UIImage equivalent and place at oldCard's position
                        UIImage newCard = UIPlayingCard.GetPartialCardImage(oldCard.Tooltip);
                        newCard.Position = oldCard.Position;
                        newCard.ScaleX = newCard.ScaleY = _CurrentScale;
                        newCard.Opacity = CurrentOpacity;
                        newCard.InvalidateOpacity();
                        newCard.Tooltip = oldCard.Tooltip;
                        Add(newCard);
                    }

                    // swap top-most partial card texture with full card texture
                    UIImage card = (UIImage)cardList[cardList.Count - 1];
                    card.Texture = GetTexture(UIPlayingCard.GetFullCardAssetID(PreviousCard.Tooltip));
                    card.Reset();

                    float startingX = card.X; // start at top child, top-most visible card
                    for (int index = cardList.Count - 2; index > -1; index--)
                    {
                        startingX -= 8 * _CurrentScale;
                        cardList[index].X = startingX;
                    }
                }
                _IsCollapsed = false;
            }
        }
        private void UpdateChildrenOpacity()
        {
            var cardList = GetChildren();
            if (cardList != null)
            {
                foreach (var card in cardList)
                {
                    card.Opacity = CurrentOpacity;
                    card.InvalidateOpacity();
                }
            }
        }
        internal void Collapse()
        {
            var cardList = new List<UIElement>(GetChildren());
            if (cardList != null && cardList.Count > 0)
            {
                // swap top-most card full card texture with partial card
                UIImage card = (UIImage)cardList[cardList.Count - 1];
                card.Texture = GetTexture(UIPlayingCard.GetPartialCardAssetID(PreviousCard.Tooltip));
                card.Reset();

                // initial X values for translation
                float startingX = card.X; // start at top child, top-most visible card
                float constantX;

                if (cardList.Count > 2)
                {
                    constantX = 4f;
                    // if more than 2 cards, they need to be UISlotsImage(s)
                    foreach (var oldCard in cardList)
                    {
                        Remove(oldCard); // remove as child
                                         // make UISlotsImage equivalent and place at oldCard's position
                        UISlotsImage newCard = UIPlayingCard.GetPartialCardSlotsImage(oldCard.Tooltip);
                        newCard.Position = oldCard.Position;
                        newCard.ScaleX = newCard.ScaleY = _CurrentScale;
                        newCard.Opacity = CurrentOpacity;
                        newCard.SetBounds(0, 0, 4, UIPlayingCard.CARD_HEIGHT);
                        newCard.InvalidateOpacity();
                        newCard.Tooltip = oldCard.Tooltip;
                        Add(newCard);
                    }
                    cardList = new List<UIElement>(GetChildren()); // new list with updtaed UISlotsImage children
                }
                else
                    constantX = 8f;

                // translate all cards by a factor of the constant
                for (int index = cardList.Count - 2; index > -1; index--)
                {
                    startingX -= constantX * _CurrentScale;
                    cardList[index].X = startingX;
                }
            }
            _IsCollapsed = true;
            SetInactive();
        }
        internal void Reset()
        {
            var cardList = new List<UIElement>(GetChildren());
            foreach (var card in cardList)
                Remove(card);
            _TotalNumberOfCards = 0;
            _TotalValueOfCards = 0;
            TotalCardsHidden = 0;
            PreviousCard = null;
            //Label.Visible = false;
        }
        internal void AddCard(String cardName)
        {
            var card = UIPlayingCard.GetFullCardImage(cardName);
            if (card != null)
            {
                card.Tooltip = cardName;
                card.X += (_TotalNumberOfCards - TotalCardsHidden) * 8 * _CurrentScale;
                if (++_TotalNumberOfCards > 2)
                {
                    if (_TotalNumberOfCards > 8)
                    {
                        HideFirstCard();
                        card.X -= 8 * _CurrentScale;
                    }
                    ShiftCardsLeft();
                    card.X -= (4 * _CurrentScale) * (_TotalNumberOfCards - TotalCardsHidden - 2);
                }
                card.ScaleX = card.ScaleY = _CurrentScale;
                card.Opacity = CurrentOpacity;
                card.InvalidateOpacity();
                this.Add(card);
                if (PreviousCard != null)
                {
                    PreviousCard.Texture = GetTexture(UIPlayingCard.GetPartialCardAssetID(PreviousCard.Tooltip)); // swap for partial card image
                    PreviousCard.Reset();
                }
                PreviousCard = card;
            }
            CalculateTotalValue();
        }
        private void ShiftCardsLeft()
        {
            var cardList = GetChildren();
            foreach (var card in cardList)
                card.X -= 4* _CurrentScale;
        }
        private void HideFirstCard()
        {
            var cardList = new List<UIElement>(GetChildren());
            cardList[TotalCardsHidden++].Visible = false;
            ShiftCardsLeft();
        }
        private void CalculateTotalValue()
        {
            _TotalValueOfCards = 0;
            bool softAce = false;
            byte value;
            var cardList = GetChildren();
            foreach (var card in cardList)
            {
                if (card.Tooltip.Equals(FullPlayCardAssets.Back.ToString())) // don't count "back"
                    continue;
                var split = card.Tooltip.Split('_');
                // check for hard/soft ace
                if (split.Length > 1 && split[0].Equals("Ace"))
                {
                    if (softAce || _TotalValueOfCards > 10)
                        _TotalValueOfCards += 1;
                    else
                    {
                        _TotalValueOfCards += 11;
                        softAce = true;
                    }
                }
                else 
                    if (VMEODBlackjackPlugin.PlayingCardBlackjackValues.TryGetValue(split[0], out value))
                        _TotalValueOfCards += value;
            }
            if (_TotalValueOfCards > 21)
            {
                if (softAce)
                    _TotalValueOfCards -= 10;
            }
        }
    }
}
