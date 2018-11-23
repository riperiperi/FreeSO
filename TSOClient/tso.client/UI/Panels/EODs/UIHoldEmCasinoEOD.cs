using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Text;
using System.Threading.Tasks;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Client.UI.Panels.EODs.Utils;
using FSO.Client.UI.Panels.EODs.Archetypes;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.SimAntics.NetPlay.EODs.Handlers.Data;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIHoldEmCasinoEOD : UIEOD
    {
        // general UI
        private bool BettingIsAllowed;
        private List<string[]> CardsToDeal;
        private UIAlert CurrentAlert;
        private int DeadAirTime;
        private short DealersID;
        private string DealersName;
        private int DealingIndex;
        private UIVMPersonButton DealerPersonButton;
        private Timer DealTimer;
        private bool DecisionIsAllowed;
        private Timer InvalidateTimer;
        private UIEODLobby Lobby;
        private int MaxAnteBet;
        private int MaxSideBet;
        private int MinAnteBet;
        private int MyPlayerNumber;
        private UIManageEODObjectPanel OwnerPanel;
        
        // lower buttons
        private UIButton Ante1ChipButton;
        private UIButton Ante5ChipButton;
        private UIButton Ante10ChipButton;
        private UIButton Ante25ChipButton;
        private UIButton Ante100ChipButton;
        private UIButton CallButton;
        private UIButton CallAndFoldHelpButton;
        private UIButton ClearAnteBetButton;
        private UIButton ClearSideBetButton;
        private UIButton FoldButton;
        private UIButton HelpAnteBetButton;
        private UIButton HelpSideBetButton;
        private UIButton Side1ChipButton;
        private UIButton Side5ChipButton;
        private UIButton Side10ChipButton;
        private UIButton Side25ChipButton;
        private UIButton Side100ChipButton;
        private UIButton SubmitBetsButton;

        // lower labels
        private UILabel AnteBetLabel;
        private UITextEdit AnteBet;
        private UILabel SideBetLabel;
        private UITextEdit SideBet;

        // lower images
        private UIImage AnteBetBack;
        private UIImage SideBetBack;
        private UIImage CallButtonBack;
        private UIImage CallAndFoldHelpButtonBack;
        private UIImage ClearAnteBetButtonBack;
        private UIImage ClearSideBetButtonBack;
        private TwoCardHand MyCardHand;
        private FiveCardHand CommunityHand;
        private UIImage FoldButtonBack;
        private UIImage HelpAnteBetButtonBack;
        private UIImage HelpSideBetButtonBack;
        private UIImage SubmitBetsButtonBack;
        private Vector2 ButtonSeatOffset = new Vector2(3, 3);

        // lower textures
        private Texture2D BetAmountBGTexture = GetTexture(0x0000095500000001); // ./uigraphics/eods/gamecompbuzzer/eod_buzzer_playerscoreback.bmp
        private Texture2D ButtonSeat = GetTexture(0x000001A100000002); // ./uigraphics/eods/shared/eod_buttonseat_transparent.tga
        private Texture2D CallButtonTexture = GetTexture(0x00000C2100000001); // ./uigraphics/eods/blackjack/eod_blackjackdbldwnbtn.bmp
        private Texture2D Chip1ButtonTexture = GetTexture(0x00000C1800000001); // ./uigraphics/eods/casinoshared/eod_chipbtn_001.bmp
        private Texture2D Chip5ButtonTexture = GetTexture(0x00000C1900000001); // ./uigraphics/eods/casinoshared/eod_chipbtn_005.bmp
        private Texture2D Chip10ButtonTexture = GetTexture(0x00000C1A00000001); // ./uigraphics/eods/casinoshared/eod_chipbtn_010.bmp
        private Texture2D Chip25ButtonTexture = GetTexture(0x00000C1B00000001); // ./uigraphics/eods/casinoshared/eod_chipbtn_025.bmp
        private Texture2D Chip100ButtonTexture = GetTexture(0x00000C1C00000001); // ./uigraphics/eods/casinoshared/eod_chipbtn_100.bmp
        private Texture2D HelpButtonTexture = GetTexture(0x000004E100000001); // ./uigraphics/ucp/lpanel_eodhelpbtn.bmp
        private Texture2D FoldButtonTexture = GetTexture(0x0000093A00000001); // ./uigraphics/eods/blackjack/eod_blackjackstandbtn.bmp
        private Texture2D SubmitBetsButtonTexture = GetTexture(0x0000097A00000001); // ./uigraphics/eods/shared/eod_continuebtn.bmp
        private Texture2D ClearBetButtonTexture = GetTexture(0x0000095900000001); // ./uigraphics/eods/shared/eod_cancelbtn.bmp

        // upper labels
        private UILabel Player1AnteBetLabel;
        private UILabel Player1SideBetLabel;
        private UILabel Player2AnteBetLabel;
        private UILabel Player2SideBetLabel;
        private UILabel Player3AnteBetLabel;
        private UILabel Player3SideBetLabel;
        private UILabel Player4AnteBetLabel;
        private UILabel Player4SideBetLabel;
        private UITextEdit DealerBetAmount; // it just says "Dealer"

        // upper images
        private UIImage DealerBetBack;
        private UIImage EODTallBack;
        private UIImage EODTallBackEnd;
        private UIHighlightSprite LeftDivider;
        private UIHighlightSprite MiddleDivider;
        private UIHighlightSprite RightDivider;
        private UIImage Player1AnteBetBack;
        private UIImage Player2AnteBetBack;
        private UIImage Player3AnteBetBack;
        private UIImage Player4AnteBetBack;
        private UIImage Player1SideBetBack;
        private UIImage Player2SideBetBack;
        private UIImage Player3SideBetBack;
        private UIImage Player4SideBetBack;
        private TwoCardHand Player1CardsContainer;
        private TwoCardHand Player2CardsContainer;
        private TwoCardHand Player3CardsContainer;
        private TwoCardHand Player4CardsContainer;
        private TwoCardHand DealerCardsContainer;
        private UIImage Player1Head;
        private UIImage Player2Head;
        private UIImage Player3Head;
        private UIImage Player4Head;
        private UIImage DealerHead;

        // upper textures
        private Texture2D UpperPlayersVMPersonButtonBGTex = GetTexture(0x000002B300000001); // EOD_PizzaHeadPlaceholder1.bmp
        private Texture2D UpperPlayersBetBack = GetTexture(0x95600000001); // eod_buzzer_playertimerback.bmp

        // shared textures
        internal static Texture2D FourCardSlotsTexture = GetTexture(0x0000097F00000001); // ./uigraphics/eods/spades/eod_spades_4cardslots.bmp
        internal static Texture2D CommunityCardsBGTexture = GetTexture(0x00000CA000000001); // ./uigraphics/eods/shared/eod_videopoker_compositeback.bmp
        internal static readonly int CommunityCardsWidth = 195;
        internal static readonly int CommunityCardsHeight = 42;

        // literal strings found in _f111_casinoeodstrings.cst
        private string Error = GameFacade.Strings.GetString("f111", "10");
        private string Holdem = GameFacade.Strings.GetString("f111", "50");

        // alert strings found in _f111_casinoeodstrings.cst
        private Dictionary<byte, string> AlertStrings = new Dictionary<byte, string>()
        {
            { (byte)VMEODHoldEmCasinoAlerts.Ante_Bet_Help, GameFacade.Strings.GetString("f111", "38")
                + System.Environment.NewLine + System.Environment.NewLine + GameFacade.Strings.GetString( "f111", "71")},
            { (byte)VMEODHoldEmCasinoAlerts.Side_Bet_Help, GameFacade.Strings.GetString("f111", "43")
                + System.Environment.NewLine + System.Environment.NewLine + GameFacade.Strings.GetString( "f111", "71")},
            { (byte)VMEODHoldEmCasinoAlerts.Invalid_Ante, GameFacade.Strings.GetString("f111", "55") },
            { (byte)VMEODHoldEmCasinoAlerts.Invalid_Number, GameFacade.Strings.GetString("f111", "57") },
            { (byte)VMEODHoldEmCasinoAlerts.Invalid_Side_Valid_Ante, GameFacade.Strings.GetString("f111", "56") },
            { (byte)VMEODHoldEmCasinoAlerts.False_Start, GameFacade.Strings.GetString("f111", "17") },
            { (byte)VMEODHoldEmCasinoAlerts.Unknown_Betting_Error, GameFacade.Strings.GetString("f111", "35") },
            { (byte)VMEODHoldEmCasinoAlerts.Call_Fold_Help, GameFacade.Strings.GetString("f111", "76") },
            { (byte)VMEODHoldEmCasinoAlerts.State_Race, GameFacade.Strings.GetString("f111", "16") },
            { (byte)VMEODHoldEmCasinoAlerts.Bet_Too_Low, GameFacade.Strings.GetString("f111", "51") }, // antebet
            { (byte)VMEODHoldEmCasinoAlerts.Bet_Too_High, GameFacade.Strings.GetString("f111", "52") }, // antebet
            { (byte)VMEODHoldEmCasinoAlerts.Bet_NSF, GameFacade.Strings.GetString("f111", "23") },
            { (byte)VMEODHoldEmCasinoAlerts.Observe_Once, GameFacade.Strings.GetString("f111", "26") },
            { (byte)VMEODHoldEmCasinoAlerts.Observe_Twice, GameFacade.Strings.GetString("f111", "27") },
            { (byte)VMEODHoldEmCasinoAlerts.Table_NSF, GameFacade.Strings.GetString("f111", "28") },
            { (byte)VMEODHoldEmCasinoAlerts.Player_NSF, GameFacade.Strings.GetString("f111", "31") },
            { (byte)VMEODHoldEmCasinoAlerts.Call_NSF, GameFacade.Strings.GetString("f111", "54") },
            { (byte)VMEODHoldEmCasinoAlerts.Side_Bet_Too_High, GameFacade.Strings.GetString("f111", "53") }, // sidebet
            { (byte)VMEODHoldEmCasinoAlerts.Object_Broken, GameFacade.Strings.GetString("f111", "90") }
        };
        
        public UIHoldEmCasinoEOD(UIEODController controller) : base(controller)
        {
            InitLowerUI();
            InitUpperUI();

            // make the lobby
            Lobby = new UIEODLobby(this, 4)
                .WithPlayerUI(new UIEODLobbyPlayer(0, Player1Head, null))
                .WithPlayerUI(new UIEODLobbyPlayer(1, Player2Head, null))
                .WithPlayerUI(new UIEODLobbyPlayer(2, Player3Head, null))
                .WithPlayerUI(new UIEODLobbyPlayer(3, Player4Head, null));
            Add(Lobby);

            // listeners
            PlaintextHandlers["eod_leave"] = (evt, str) => { OnClose(); };
            // player listeners
            BinaryHandlers["holdemcasino_alert"] = UIAlertHandler;
            BinaryHandlers["holdemcasino_allow_input"] = AllowInputHandler;
            BinaryHandlers["holdemcasino_antebet_update_player1"] = UpdateSingleAnteBetHandler;
            BinaryHandlers["holdemcasino_antebet_update_player2"] = UpdateSingleAnteBetHandler;
            BinaryHandlers["holdemcasino_antebet_update_player3"] = UpdateSingleAnteBetHandler;
            BinaryHandlers["holdemcasino_antebet_update_player4"] = UpdateSingleAnteBetHandler;
            BinaryHandlers["holdemcasino_bet_callback"] = BetCallbackHandler;
            BinaryHandlers["holdemcasino_call_broadcast"] = PlayerChoiceBroadcastHandler;
            BinaryHandlers["holdemcasino_deal_sequence"] = StartDealingSequenceHandler;
            BinaryHandlers["holdemcasino_decision_callback"] = DecisionCallbackHandler;
            BinaryHandlers["holdemcasino_final_deal_sequence"] = DealFinalTwoCardsHandler;
            BinaryHandlers["holdemcasino_fold_broadcast"] = PlayerChoiceBroadcastHandler;
            BinaryHandlers["holdemcasino_late_comer"] = PlayerChoiceBroadcastHandler;
            BinaryHandlers["holdemcasino_light_up_hand"] = LightUpWinningHandHandler;
            BinaryHandlers["holdemcasino_new_game"] = NewGameHandler;
            BinaryHandlers["holdemcasino_player_show"] = ShowPlayerUIHandler;
            BinaryHandlers["holdemcasino_set_active_player"] = PlayerChoiceBroadcastHandler;
            BinaryHandlers["holdemcasino_sidebet_update_player1"] = UpdateSingleSideBetHandler;
            BinaryHandlers["holdemcasino_sidebet_update_player2"] = UpdateSingleSideBetHandler;
            BinaryHandlers["holdemcasino_sidebet_update_player3"] = UpdateSingleSideBetHandler;
            BinaryHandlers["holdemcasino_sidebet_update_player4"] = UpdateSingleSideBetHandler;
            BinaryHandlers["holdemcasino_sidebet_win_message"] = SideBetWinHandler;
            BinaryHandlers["holdemcasino_sync_accepted_bets"] = UpdateAllBetsHandler;
            BinaryHandlers["holdemcasino_sync_community"] = UpdateCommunityCardsHandler;
            BinaryHandlers["holdemcasino_sync_hand"] = SyncUpperPlayerHandHandler;
            BinaryHandlers["holdemcasino_sync_hands"] = SyncAllActiveHandsHandler;
            BinaryHandlers["holdemcasino_sync_hands_up"] = SyncAllActiveHandsUpHandler;
            BinaryHandlers["holdemcasino_timer"] = UpdateEODTimerHandler;
            BinaryHandlers["holdemcasino_toggle_betting"] = AllowBettingToggleHandler;
            BinaryHandlers["holdemcasino_win_loss_message"] = WinLossHandler;
            PlaintextHandlers["holdemcasino_players_update"] = Lobby.UpdatePlayers;
            // owner listeners
            PlaintextHandlers["holdemcasino_deposit_NSF"] = DepositFailHandler;
            PlaintextHandlers["holdemcasino_owner_show"] = ShowOwnerUIHandler;
            PlaintextHandlers["holdemcasino_deposit_fail"] = InputFailHandler;
            PlaintextHandlers["holdemcasino_withdraw_fail"] = InputFailHandler;
            PlaintextHandlers["holdemcasino_n_bet_fail"] = InputFailHandler; // min ante bet fail
            PlaintextHandlers["holdemcasino_s_bet_fail"] = InputFailHandler; // max side bet fail
            PlaintextHandlers["holdemcasino_x_bet_fail"] = InputFailHandler; // max ante bet fail
            PlaintextHandlers["holdemcasino_side_bet_success"] = ResumeFromBetAmountHandler;
            PlaintextHandlers["holdemcasino_max_bet_success"] = ResumeFromBetAmountHandler; // max ante bet
            PlaintextHandlers["holdemcasino_min_bet_success"] = ResumeFromBetAmountHandler; // min ante bet
            PlaintextHandlers["holdemcasino_resume_manage"] = ResumeManageHandler;

            // other
            DealTimer = new Timer(1400);
            DealTimer.Elapsed += new ElapsedEventHandler(DealTimerHandler);
            DealersName = "MOMI";
            InvalidateTimer = new Timer(1000);
            InvalidateTimer.Elapsed += new ElapsedEventHandler((obj, args) => { Parent.Invalidate(); });
            InvalidateTimer.Start();
        }

        public override void OnClose()
        {
            SetNewTip("");
            byte isOwner = (OwnerPanel == null) ? (byte)0 : (byte)1;
            Send("holdemcasino_close", new byte[] { isOwner });
            base.OnClose();
        }

        #region events and handlers

        private void ShowPlayerUIHandler(string evt, byte[] data)
        {
            if (data == null)
            {
                Send("holdemcasino_close", "");
                CloseInteraction();
            }

            // parse the data: MyPlayerNumber, Min & Max Ante Bet, Max Side Bet, and Dealers object ID
            var stringData = VMEODGameCompDrawACardData.DeserializeStrings(data);

            int playerNumber = -1;

            if (stringData != null && stringData.Length == 5 && Int32.TryParse(stringData[0], out playerNumber) &&
                Int32.TryParse(stringData[1], out MinAnteBet) && Int32.TryParse(stringData[2], out MaxAnteBet) &&
                Int32.TryParse(stringData[3], out MaxSideBet) && Int16.TryParse(stringData[4], out DealersID))
            {

                if (playerNumber > -1 && playerNumber < 4)
                    MyPlayerNumber = playerNumber + 1;
                else
                {
                    Send("holdemcasino_close", "");
                    CloseInteraction();
                    return;
                }

                // make and add dealer VMPersonButton
                DealerPersonButton = Lobby.GetAvatarButton(DealersID, true);
                if (DealerPersonButton != null)
                {
                    DealerPersonButton.Position = DealerHead.Position + new Vector2(2, 2);
                    Add(DealerPersonButton);
                    DealersName = DealerPersonButton.Avatar.Name;
                }

                // disable betting & decisions at first
                DisallowBetting();
                ToggleDecision(false);
                SetNewTip(GameFacade.Strings.GetString("f111", "47")); // "Wait until the next hand to bet."

                // show the UIEOD
                Controller.ShowEODMode(GetEODOptions(false));
            }
            else // invalid data sent
            {
                Send("holdemcasino_close", "");
                CloseInteraction();
            }
        }
        private void ShowOwnerUIHandler(string evt, string balanceMinAnteMaxAnteSide)
        {
            if (balanceMinAnteMaxAnteSide == null)
            {
                Send("holdemcasino_close", "");
                CloseInteraction();
            }
            else
            {
                // hide some stuff
                CallButton.Visible = false;
                CallButtonBack.Visible = false;
                FoldButton.Visible = false;
                FoldButtonBack.Visible = false;
                CallAndFoldHelpButton.Visible = false;
                CallAndFoldHelpButtonBack.Visible = false;
                Ante1ChipButton.Visible = false;
                Ante5ChipButton.Visible = false;
                Ante10ChipButton.Visible = false;
                Ante25ChipButton.Visible = false;
                Ante100ChipButton.Visible = false;
                Side1ChipButton.Visible = false;
                Side5ChipButton.Visible = false;
                Side10ChipButton.Visible = false;
                Side25ChipButton.Visible = false;
                Side100ChipButton.Visible = false;
                AnteBetBack.Visible = false;
                SideBetBack.Visible = false;
                AnteBetLabel.Visible = false;
                SideBetLabel.Visible = false;
                AnteBet.Visible = false;
                SideBet.Visible = false;
                SubmitBetsButton.Visible = false;
                SubmitBetsButtonBack.Visible = false;
                HelpAnteBetButton.Visible = false;
                HelpAnteBetButtonBack.Visible = false;
                HelpSideBetButton.Visible = false;
                HelpSideBetButtonBack.Visible = false;
                ClearAnteBetButton.Visible = false;
                ClearAnteBetButtonBack.Visible = false;
                ClearSideBetButton.Visible = false;
                ClearSideBetButtonBack.Visible = false;
                MyCardHand.Visible = false;

                EODTallBack.Visible = false;
                EODTallBackEnd.Visible = false;
                CommunityHand.Visible = false;
                Player1CardsContainer.Visible = false;
                Player2CardsContainer.Visible = false;
                Player3CardsContainer.Visible = false;
                Player4CardsContainer.Visible = false;
                DealerCardsContainer.Visible = false;
                LeftDivider.Visible = false;
                MiddleDivider.Visible = false;
                RightDivider.Visible = false;
                Player1Head.Visible = false;
                Player2Head.Visible = false;
                Player3Head.Visible = false;
                Player4Head.Visible = false;
                DealerHead.Visible = false;
                Player1AnteBetBack.Visible = false;
                Player2AnteBetBack.Visible = false;
                Player3AnteBetBack.Visible = false;
                Player4AnteBetBack.Visible = false;
                Player1AnteBetLabel.Visible = false;
                Player2AnteBetLabel.Visible = false;
                Player3AnteBetLabel.Visible = false;
                Player4AnteBetLabel.Visible = false;
                Player1SideBetBack.Visible = false;
                Player2SideBetBack.Visible = false;
                Player3SideBetBack.Visible = false;
                Player4SideBetBack.Visible = false;
                Player1SideBetLabel.Visible = false;
                Player2SideBetLabel.Visible = false;
                Player3SideBetLabel.Visible = false;
                Player4SideBetLabel.Visible = false;
                DealerBetBack.Visible = false;
                DealerBetAmount.Visible = false;
                
                // parse the data and add owner panel
                int tempBalance;
                int tempMinAnte;
                int tempMaxAnte;
                int tempMaxSide;
                var split = balanceMinAnteMaxAnteSide.Split('%');
                if (split.Length == 4 && Int32.TryParse(split[0], out tempBalance) && Int32.TryParse(split[1], out tempMinAnte)
                    && Int32.TryParse(split[2], out tempMaxAnte) && Int32.TryParse(split[3], out tempMaxSide))
                {
                    int minBalance = tempMaxAnte * VMEODHoldEmCasinoPlugin.WORST_CASE_ANTE_PAYOUT_RATIO +
                        tempMaxSide * VMEODHoldEmCasinoPlugin.WORST_CASE_SIDE_PAYOUT_RATIO;
                    OwnerPanel = new UIManageEODObjectPanel(ManageEODObjectTypes.HoldEmCasino, tempBalance, minBalance, VMEODBlackjackPlugin.TABLE_MAX_BALANCE,
                           tempMinAnte, tempMaxAnte, tempMaxSide);
                    Add(OwnerPanel);

                    // subscribe in order to send events based on type
                    OwnerPanel.OnNewByteMessage += SendByteMessage;
                    OwnerPanel.OnNewStringMessage += SendStringMessage;
                    SetNewTip(GameFacade.Strings["UIText", "259", "22"]); // "Closed for Maintenance"

                    // show the UIEOD
                    Controller.ShowEODMode(GetEODOptions(true));
                }
            }
        }
        /*
         * Every time the betting round begins, this event is broadcast to all players in the lobby. 
         */
        private void NewGameHandler(string evt, byte[] nothing)
        {
            // all hands and bets reset
            ResetAllHands();

            // no one's hand is active
            SetActiveOtherPlayerHand(-1);

            // allow betting, sets MY side and ante bets to 0, sets Tip
            AllowBetting();

            // cannot fold or call, disable buttons
            ToggleDecision(false);
        }
        /*
         * Updates a player's ante bet based on the event name's final character 1-4
         */
        private void UpdateSingleAnteBetHandler(string evt, byte[] newBetArray)
        {
            if (newBetArray != null)
            {
                var newBet = VMEODGameCompDrawACardData.DeserializeStrings(newBetArray);
                if (newBet != null && newBet.Length > 0)
                {
                    var anteBet = newBet[0];
                    if (anteBet.Equals("0") || anteBet.Equals(""))
                        anteBet = "Ante";
                    var playerIndex = (int)evt.Last() - '0';
                    SetPlayerBet(true, playerIndex, anteBet);
                }
            }
        }
        /*
         * Updates a player's side bet based on the event name's final character 1-4
         */
        private void UpdateSingleSideBetHandler(string evt, byte[] newBetArray)
        {
            if (newBetArray != null)
            {
                var newBet = VMEODGameCompDrawACardData.DeserializeStrings(newBetArray);
                if (newBet != null && newBet.Length > 0)
                {
                    var sideBet = newBet[0];
                    if (sideBet.Equals("0") || sideBet.Equals(""))
                        sideBet = "Side";
                    var playerIndex = (int)evt.Last() - '0';
                    SetPlayerBet(false, playerIndex, sideBet);
                }
            }
        }
        /*
         * Stop the betting round and begin the dealing sequence.
         */
        private void StartDealingSequenceHandler(string evt, byte[] serializedCards)
        {
            if (serializedCards != null)
            {
                var cardsArray = VMEODGameCompDrawACardData.DeserializeStrings(serializedCards);
                if (cardsArray != null && cardsArray.Length == 13)
                {
                    DisallowBetting();
                    SetNewTip(GameFacade.Strings["UIText", "263", "2"]); // "Dealing."
                    List<string> cardsList = new List<string>(cardsArray);
                    DealInitialCards(cardsList);
                }
            }
        }
        /*
         * This method occurs when one or more players "call" and two cards are dealt to the community cards.
         */
        private void DealFinalTwoCardsHandler(string evt, byte[] serializedCards)
        {
            if (serializedCards != null)
            {
                var cardsArray = VMEODGameCompDrawACardData.DeserializeStrings(serializedCards);
                if (cardsArray != null && cardsArray.Length == 2)
                {
                    SetNewTip(GameFacade.Strings["UIText", "263", "2"]); // "Dealing."

                    // queue the cards to be dealt one at a time
                    var firstCard = new List<string>() { "6" }; // 6 inserted first to denote community hand
                    var secondCard = new List<string>() { "6" };
                    firstCard.Add(cardsArray[0]);
                    secondCard.Add(cardsArray[1]);

                    CardsToDeal = new List<string[]>() { firstCard.ToArray(), secondCard.ToArray() };

                    // set the timer for dealing and start it
                    DealingIndex = -1;
                    DealTimer.Start();
                }
            }
        }
        /*
         * Updates EODTimer
         */
        private void UpdateEODTimerHandler(string evt, byte[] newTime)
        {
            SetNewTime(BitConverter.ToInt32(newTime, 0));
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
         * Occurs on the start of user's one and only turn
         */
        private void AllowInputHandler(string evt, byte[] serializedCards)
        {
            ToggleDecision(true);
        }
        /*
         * Any time the SubmitBetButton is clicked
         */
        private void SubmitBetsHandler()
        {
            if (BettingIsAllowed)
            {
                UpdateUserInput(false);
                string[] data = new string[2];
                var anteBetValue = GetBetValue(AnteBet);
                var sideBetValue = GetBetValue(SideBet);
                if (anteBetValue >= MinAnteBet && anteBetValue <= MaxAnteBet)
                {
                    data[0] = "" + anteBetValue;
                    data[1] = "0";
                    if (sideBetValue > -1 && sideBetValue <= MaxSideBet) // valid and less than the max
                    {
                        data[1] = "" + sideBetValue; // send both bets
                        Send("holdemcasino_submit_bets", VMEODGameCompDrawACardData.SerializeStrings(data));
                    }
                    else // if they want to proceed then send Ante Bet alone, if they don't then reenable input
                        ShowUIAlert(Holdem, AlertStrings[(byte)VMEODHoldEmCasinoAlerts.Invalid_Side_Valid_Ante],
                            () => { Send("holdemcasino_submit_bets", VMEODGameCompDrawACardData.SerializeStrings(data)); },
                            () => { UpdateUserInput(BettingIsAllowed); });
                }
                else
                    ShowUIAlert(GameFacade.Strings.GetString("f111", "15"), AlertStrings[(byte)VMEODHoldEmCasinoAlerts.Invalid_Ante],
                        () => { UpdateUserInput(BettingIsAllowed); });
            }
            else
                ShowUIAlert(GameFacade.Strings.GetString("f111", "15"), AlertStrings[(byte)VMEODHoldEmCasinoAlerts.Unknown_Betting_Error],
                    () => { UpdateUserInput(BettingIsAllowed); });
        }
        /*
         * When either the CallButton or the FoldButton is clicked
         */
        private void CallOrFoldHandler(byte decision)
        {
            if (DecisionIsAllowed)
            {
                DecisionIsAllowed = false;
                Send("holdemcasino_decision", new byte[] { decision }); // send 0 for fold and 1 for call
            }
            else // Alert: "It's not your turn." & Disable Call and Fold Buttons
                ShowUIAlert(Holdem, AlertStrings[(byte)VMEODHoldEmCasinoAlerts.False_Start], () => { ToggleDecision(DecisionIsAllowed); });
        }
        private void AllowBettingToggleHandler(string evt, byte[] data)
        {
            if (data != null)
            {
                if (data[0] == 0)
                {
                    DisallowBetting();
                }
                else
                    AllowBetting();
            }
        }
        /*
         * After a bet is accepted by the server or the betting round timer hit 0 before your bet was accepted
         */
        private void BetCallbackHandler(string evt, byte[] bets)
        {
            DisallowBetting();

            // remove any yes/no alerts
            if (CurrentAlert != null)
            {
                UIScreen.RemoveDialog(CurrentAlert);
                CurrentAlert = null;
            }

            if (bets != null)
            {
                // update AnteBet and the Bets for the (upper) fields pertaining to MyPlayerNumber
                var betStrings = VMEODGameCompDrawACardData.DeserializeStrings(bets);
                
                SetMyAnteBet(betStrings[0]);
                SetMySideBet(betStrings[1]);
            }
            else
            {
                SetMyAnteBet("Ante");
                SetMySideBet("Side");
            }
        }
        /*
         * After a "fold" or "call" is accepted by the server, or a "fold" forced by the server due to timeout
         */
        private void DecisionCallbackHandler(string evt, byte[] data)
        {
            ToggleDecision(false);
        }
        /*
         * Any time the string in the AnteBet TextEdit changes:
         */
        private void MyAnteBetHandler(UIElement textEdit)
        {
            if (BettingIsAllowed)
            {
                var anteBetValue = GetBetValue(AnteBet);
                if (anteBetValue == -1)
                {
                    SetMyAnteBet("0");
                    ShowUIAlert(Holdem, AlertStrings[(byte)VMEODHoldEmCasinoAlerts.Invalid_Number], null);
                }
                else
                    SubmitBetsButton.Disabled = !(anteBetValue >= MinAnteBet && anteBetValue <= MaxAnteBet);
            }
        }
        /*
         * Any time the string in the SideBet TextEdit changes:
         */
        private void MySideBetHandler(UIElement textEdit)
        {
            if (BettingIsAllowed)
            {
                var sideBetValue = GetBetValue(SideBet);
                if (sideBetValue == -1)
                {
                    SetMySideBet("0");
                    ShowUIAlert(Holdem, AlertStrings[(byte)VMEODHoldEmCasinoAlerts.Invalid_Number], null);
                }
            }
        }
        /*
         * Update an upper player's hand when they turn their cards over to reveal what they have
         */
        private void SyncUpperPlayerHandHandler(string evt, byte[] playerAndCards)
        {
            if (playerAndCards != null)
            {
                int playerNumber = 0;
                var cards = VMEODGameCompDrawACardData.DeserializeStrings(playerAndCards);
                if (cards != null && cards.Length == 3 && Int32.TryParse(cards[0], out playerNumber))
                {
                    UpdateUpperPlayerHand(playerNumber, null);
                    UpdateUpperPlayerHand(playerNumber, cards[1], cards[2]);
                }
            }
        }
        /*
         * This event is for latecomers who have joined a game already in play.
         */
        private void SyncAllActiveHandsHandler(string evt, byte[] playersPlaying)
        {
            if (playersPlaying != null & playersPlaying.Length == 4)
            {
                for (int index = 0; index < 4; index++) {
                    if (playersPlaying[index] == 1)
                    {
                        UpdateUpperPlayerHand(index + 1, "Back", "Back");
                    }
                }
                UpdateUpperPlayerHand(5, "Back", "Back"); // dealer's hand
            }
        }
        /*
         * This event is for latecomers who have joined a game after the last deal, where players are getting paid winnings.
         */
        private void SyncAllActiveHandsUpHandler(string evt, byte[] serializedCards)
        {
            if (serializedCards != null)
            {
                var cards = VMEODGameCompDrawACardData.DeserializeStrings(serializedCards);
                if (cards != null)
                {
                    int playerIndex = 1;
                    for (int index = 0; index < cards.Length; index++)
                    {
                        UpdateUpperPlayerHand(playerIndex, null);
                        UpdateUpperPlayerHand(playerIndex, cards[index++], cards[index]);
                        playerIndex++;
                    }
                }
            }
        }
        /*
         * Updates the community cards quickly for latecomers
         */
        private void UpdateCommunityCardsHandler(string evt, byte[] serializedCards)
        {
            if (serializedCards != null)
            {
                var cardStrings = VMEODGameCompDrawACardData.DeserializeStrings(serializedCards);
                if (cardStrings != null)
                {
                    CommunityHand.AddCard(null); // reset hand
                    foreach (string cardName in cardStrings)
                        CommunityHand.AddCard(cardName);
                }
            }
        }
        /*
         * Update (synchronize) everyone's bet amounts in the UI.
         */
        private void UpdateAllBetsHandler(string evt, byte[] data)
        {
            if (data != null)
            {
                string[] bets = VMEODGameCompDrawACardData.DeserializeStrings(data);
                if (bets != null & bets.Length > 0)
                {
                    for (int index = 0; index < 4; index++)
                    {
                        int dummy = 0;
                        Int32.TryParse(bets[index * 2], out dummy);
                        SetPlayerBet(true, index + 1, (dummy == 0) ? "Ante" : dummy + "");
                        dummy = 0;
                        Int32.TryParse(bets[index * 2 + 1], out dummy);
                        SetPlayerBet(false, index + 1, (dummy == 0) ? "Side" : dummy + "");
                    }
                }
            }
        }
        // shows to all players the action just selected by the current player's turn
        private void PlayerChoiceBroadcastHandler(string evt, byte[] player)
        {
            string appendix = ".";
            if (evt[13].Equals('c')) // "holdemcasino_call_broadcast"
                appendix = ": " + GameFacade.Strings.GetString("f111", "36"); // "Call"
            else if (evt[13].Equals('f')) // "holdemcasino_fold_broadcast"
                appendix = ": " + GameFacade.Strings.GetString("f111", "37"); // "Fold"
            else if (evt[13].Equals('s')) // "holdemcasino_set_active_player"
                appendix = ".";
            // other option is 'a' for "holdemcasino_late_comer"

            // set the correct active hand
            SetActiveOtherPlayerHand(player[0]);

            if (player[0] == MyPlayerNumber)
            {
                int myAnteBet = 0;
                if (appendix.Equals(".") && Int32.TryParse(AnteBet.CurrentText, out myAnteBet))
                {
                    myAnteBet *= 2;
                    // "Your turn. It is $%d to call. What will you do?"
                    SetNewTip(GameFacade.Strings.GetString("f111", "58").Replace("%d", "" + myAnteBet));
                }
                else
                {
                    // "Your turn: Call/Fold"
                    SetNewTip(GameFacade.Strings["UIText", "263", "3"].Replace(".", appendix));
                }
            }
            else
            {
                var button = Lobby.GetPlayerButton(player[0] - 1);
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
        /* <summary>
         * The data sent are a collection of strings:
         * [0] — [Optional Charater] + Winnings: 0 would be a loss, if optional character is p: the player pushed with deal; if q: the dealer didn't qualify
         * [1] — String representation of the HandType from HoldemHand.Hand.HandTypes enum
         * [2]-[4] — String names of Relevant Cards such as "Ace" or "Ten", but these are used as [Flags] enum entries to get strings from _f111_ .cst
         * </summary> */
        private void WinLossHandler(string evt, byte[] data)
        {
            if (data != null && data.Length > 0)
            {
                var stringData = VMEODGameCompDrawACardData.DeserializeStrings(data);
                if (stringData != null)
                {
                    string winningsString = stringData[0];
                    var handtype = (PokerHandTypeStringIndeces)Enum.Parse(typeof(PokerHandTypeStringIndeces), stringData[1]);
                    string handTypeString = GameFacade.Strings.GetString("f111", (int)handtype + "");
                    // if the first string properly evaluates into a number, the player won or lost and the dealer qualified
                    if (Int32.TryParse(winningsString, out int winnings))
                    {
                        String relevantCards = " (";
                        for (int index = 2; index < stringData.Length; index++)
                            relevantCards += " " + stringData[index] + " &";
                        relevantCards = relevantCards.Replace("( ", "(");
                        relevantCards += ")";
                        relevantCards = relevantCards.Replace(" &)", ")");
                        // if the number is 0, the player lost, so the handtype and cards represent the dealer's hand
                        if (winnings == 0)
                        {
                            // "Dealer wins with "
                            SetNewTip(GameFacade.Strings.GetString("f111", "74") + handTypeString + relevantCards);
                        }
                        else
                        {
                            // "You win %d with "
                            SetNewTip(GameFacade.Strings.GetString("f111", "75").Replace("%d", winningsString) + handTypeString + relevantCards);
                        }
                    }
                    else
                    {
                        // if the first char of the first string is 'q' then the dealer didn't qualify
                        if (winningsString[0] == 'q')
                        {
                            winningsString = winningsString.Remove(0, 1);
                            winningsString += ".";
                            // "Dealer did not quality. You win $%d"
                            SetNewTip(GameFacade.Strings.GetString("f111", "72", new string[] { winningsString }));
                        }
                        // if the first char of the first string is 'p' then the player pushed with the dealer, who qualified
                        else if (winningsString[0] == 'p')
                        {
                            // "Push! You split the pot: " + handType
                            SetNewTip(GameFacade.Strings.GetString("f111", "73") + " " + handTypeString + ".");
                        }
                    }
                }
            }
            else // "You folded."
                SetNewTip(GameFacade.Strings.GetString("f111", "89"));
        }
        /* <summary>
         * If the data sent are a byte array of size 0, the player did not win their side bet. Otherwise:
         * The data sent are a collection of strings:
         * [0] — Winnings
         * [1] — String representation of the HandType from HoldemHand.Hand.HandTypes enum
         * [2]-[4] — String names of Relevant Cards such as "Ace" or "Ten", but these are used as [Flags] enum entries to get strings from _f111_ .cst
         * </summary> */
        private void SideBetWinHandler(string evt, byte[] data)
        {
            if (data != null && data.Length > 0)
            {
                var stringData = VMEODGameCompDrawACardData.DeserializeStrings(data);
                if (stringData != null && stringData.Length > 0)
                {
                    string handType = "";
                    if (stringData.Length > 1)
                        handType = GameFacade.Strings.GetString("f111", (int)Enum.Parse(typeof(PokerHandTypeStringIndeces), stringData[1]) + "");
                    String relevantCards = " (";
                    for (int index = 2; index < stringData.Length; index++)
                        relevantCards += " " + stringData[index] + " &";
                    relevantCards = relevantCards.Replace("( ", "(");
                    relevantCards += ")";
                    relevantCards = relevantCards.Replace(" &)", ")");
                    // "Side Bet: $%d for " + handType + " (Suit)!"
                    SetNewTip(GameFacade.Strings.GetString("f111", "59").Replace("%d", stringData[0]) + handType + relevantCards);
                }
            }
            else
            {
                // "You did not win anything from your side bet."
                SetNewTip(GameFacade.Strings.GetString("f111", "88"));
            }
        }
        // makes visible certain cards, first 2 byte argument indices are for MyCardsContainer, latter 5 are for Community cards.
        private void LightUpWinningHandHandler(string evt, byte[] handsToLight)
        {
            if (handsToLight != null && handsToLight.Length == 7)
            {
                MyCardHand.UpdateChildrenOpacity((float)handsToLight[0], (float)handsToLight[1]);
                CommunityHand.UpdateChildrenOpacity((float)handsToLight[2], (float)handsToLight[3], (float)handsToLight[4], (float)handsToLight[5],
                    (float)handsToLight[6]);
            }
        }
        private void SendByteMessage(EODMessageNode node)
        {
            Send("holdemcasino_" + node.EventName, node.EventByteData);
        }
        private void SendStringMessage(EODMessageNode node)
        {
            Send("holdemcasino_" + node.EventName, node.EventStringData);
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
                OwnerPanel.ResumeFromBetAmount(evt.Remove(0, 13), minOrMaxBetString); // truncate "holdemcasino_"
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
                OwnerPanel.InputFailHandler(evt.Remove(0, 13), message); // truncate "holdemcasino_"
            }
        }
        #endregion

        #region timers
        /*
         * The interval at which to deal cards during the initial deal phase.
         */
        private void DealTimerHandler(object source, ElapsedEventArgs args)
        {
            if (DeadAirTime-- > 0)
                return;
            else if (++DealingIndex < CardsToDeal.Count)
            {
                var currentList = CardsToDeal[DealingIndex].ToList();
                var playerNumber = Int32.Parse(currentList[0]);
                currentList.RemoveAt(0); // what remains is the card

                // update the player's hand in the upper UI
                var cards = currentList.ToArray();
                UpdateUpperPlayerHand(playerNumber, cards);

                // I am this player, update my main hand, too (in the lower UI)
                if (playerNumber == MyPlayerNumber)
                {
                    foreach (var card in cards)
                        MyCardHand.AddCard(card);
                }

                // a moment before community cards appear
                if (CardsToDeal.Count - DealingIndex == 4)
                    DeadAirTime = 1;
            }
            else
                DealTimer.Stop();
        }
        #endregion
        #region private
        private EODLiveModeOpt GetEODOptions(bool isOwner)
        {
            if (isOwner)
                return new EODLiveModeOpt
                {
                    Buttons = 0,
                    Height = EODHeight.Normal,
                    Length = EODLength.Full,
                    Tips = EODTextTips.Short,
                    Timer = EODTimer.None,
                    Expandable = false
                };
            return new EODLiveModeOpt
            {
                Buttons = 2,
                Expandable = false,
                Expanded = true,
                Height = EODHeight.TallTall,
                Length = EODLength.Full,
                Timer = EODTimer.Normal,
                Tips = EODTextTips.Short
            };
        }
        /*
         * This method happens when the first pair of cards is being dealt to all active players and the dealer. It then deals 3 cards to the
         * community hand. The argmuent will always be of count 13, 10 cards for players & dealer, 3 for community hand.
         */
        private void DealInitialCards(List<string> allCardNames)
        {
            // cards should always come in pairs, 2 for each player, 10 cards total with dealer, then 3 more for the community hand
            CardsToDeal = new List<string[]>();

            // Remove the latter 3 cards that belong to the community hand
            var communityCards = new List<string>() { allCardNames[10], allCardNames[11], allCardNames[12] };
            allCardNames.RemoveRange(10, 3); // leave first 10 cards 0-9, remove last 3

            // Of the remaining 10 cards, the odd cards will be everyone's first cards and even cards will be everyone's second cards
            List<string> firstCards = allCardNames.Where((value, index) => index % 2 == 0).ToList();
            List<string> secondCards = allCardNames.Where((value, index) => index % 2 == 1).ToList();

            int dealindex = 0;

            // if the card name is actually a blank string, skip the player by incrementing the cardIndex as that player position is empty this round
            for (int cardIndex = 0; cardIndex < 5; cardIndex++)
            {
                if (firstCards[cardIndex].Length > 1) // not a blank string
                {
                    // add the second card after the player's number 1-5 with 5 being dealer
                    CardsToDeal.Add(new string[] { (cardIndex + 1) + "", secondCards[cardIndex] });

                    // insert the first card
                    CardsToDeal.Insert(dealindex++, new string[] { (cardIndex + 1) + "", firstCards[cardIndex] });
                }
            }

            // now add the community cards to player "6" at the end, to be dealt last
            foreach (var cardName in communityCards)
            {
                CardsToDeal.Add(new string[] { "6", cardName });
            }
            
            // set the index for dealing and start the timer
            DealingIndex = -1;

            // dead air time to delay
            DeadAirTime = 1;

            DealTimer.Start();
        }
        private void AnteChipButtonPressed(byte buttonValue)
        {
            var isLegalAction = BettingIsAllowed;
            if (isLegalAction)
            {
                // try to parse the current value of the string in the AnteBet TextEdit. A returned value of -1 means the input is invalid.
                int actualValue = GetBetValue(AnteBet);
                if (actualValue != -1)
                {
                    // add the value of the button pressed
                    actualValue += buttonValue;
                    // cap at MaxAnteBet
                    if (actualValue > MaxAnteBet)
                        actualValue = MaxAnteBet;
                }
                else
                    actualValue = 0;

                // put the new value into the TextEdit as a string
                SetMyAnteBet("" + actualValue);
            }
            else
                UpdateUserInput(isLegalAction);
        }
        private int GetBetValue(UITextEdit betTextEdit)
        {
            int actualValue = -1;
            if (betTextEdit != null && betTextEdit.CurrentText != null && Int32.TryParse(betTextEdit.CurrentText, out actualValue))
            {
                if (actualValue < 0 || actualValue > 9999)
                    actualValue = -1;
            }
            return actualValue;
        }
        private void SideChipButtonPressed(byte buttonValue)
        {
            var isLegalAction = BettingIsAllowed;
            if (isLegalAction)
            {
                // try to parse the current value of the string in the SideBet TextEdit. A returned value of -1 means the input is invalid.
                int actualValue = GetBetValue(SideBet);
                if (actualValue != -1)
                {
                    // add the value of the button pressed
                    actualValue += buttonValue;
                    // cap at MaxSideBet
                    if (actualValue > MaxSideBet)
                        actualValue = MaxSideBet;
                }
                else
                    actualValue = 0;

                // put the new value into the TextEdit as a string
                SetMySideBet("" + actualValue);
            }
            else
                UpdateUserInput(isLegalAction);
        }
        private void AllowBetting()
        {
            BettingIsAllowed = true;
            UpdateUserInput(BettingIsAllowed);
            // "Place you bets. Min Ante: $%d"
            SetNewTip(GameFacade.Strings.GetString("f111", "46").Replace("%d", MinAnteBet + ""));
            ClearAnteBet();
            ClearSideBet();
        }
        private void DisallowBetting()
        {
            BettingIsAllowed = false;
            UpdateUserInput(BettingIsAllowed);
        }
        private void ToggleDecision(bool isAllowed)
        {
            DecisionIsAllowed = isAllowed;
            CallButton.Disabled = !isAllowed;
            FoldButton.Disabled = !isAllowed;
        }
        private void UpdateUserInput(bool allowed) {
            Ante1ChipButton.Disabled = !allowed;
            Ante5ChipButton.Disabled = !allowed;
            Ante10ChipButton.Disabled = !allowed;
            Ante25ChipButton.Disabled = !allowed;
            Ante100ChipButton.Disabled = !allowed;
            Side1ChipButton.Disabled = !allowed;
            Side5ChipButton.Disabled = !allowed;
            Side10ChipButton.Disabled = !allowed;
            Side25ChipButton.Disabled = !allowed;
            Side100ChipButton.Disabled = !allowed;

            ClearAnteBetButton.Disabled = !allowed;
            ClearSideBetButton.Disabled = !allowed;
            SubmitBetsButton.Disabled = true;

            if (allowed)
            {
                AnteBet.Mode = UITextEditMode.Editor;
                SideBet.Mode = UITextEditMode.Editor;
                CallButton.Disabled = true;
                FoldButton.Disabled = true;
                ClearAnteBet();
                ClearSideBet();
            }
            else
            {
                AnteBet.Mode = UITextEditMode.ReadOnly;
                SideBet.Mode = UITextEditMode.ReadOnly;
            }
        }
        private void InitLowerUI()
        {
            // 2 buttons on the right and help button
            CallButton = new UIButton(CallButtonTexture)
            {
                Tooltip = GameFacade.Strings.GetString("f111", "36"), // "Call"
                X = 439f,
                Y = 254f,
                Disabled = true
            };
            Add(CallButton);
            CallButtonBack = new UIImage(ButtonSeat)
            {
                Position = CallButton.Position - ButtonSeatOffset,
            };
            AddAt(0, CallButtonBack);
            FoldButton = new UIButton(FoldButtonTexture)
            {
                Tooltip = GameFacade.Strings.GetString("f111", "37"), // "Fold"
                X = 439f,
                Y = 300f,
                Disabled = true
            };
            Add(FoldButton);
            FoldButtonBack = new UIImage(ButtonSeat)
            {
                Position = FoldButton.Position - ButtonSeatOffset
            };
            AddAt(1, FoldButtonBack);
            CallAndFoldHelpButtonBack = new UIImage(ButtonSeat)
            {
                X = 425f,
                Y = 281f
            };
            CallAndFoldHelpButtonBack.ScaleX = CallAndFoldHelpButtonBack.ScaleY = 0.6f;
            Add(CallAndFoldHelpButtonBack);
            CallAndFoldHelpButton = new UIButton(HelpButtonTexture)
            {
                Position = CallAndFoldHelpButtonBack.Position + (ButtonSeatOffset * 0.6f),
                Tooltip = GameFacade.Strings.GetString("f111", "85"), // "Call or Fold Help"
            };
            Add(CallAndFoldHelpButton);


            // buttons for the blind bet
            Ante1ChipButton = new UIButton(Chip1ButtonTexture)
            {
                Position = new Vector2(25, 253),
                Tooltip = GameFacade.Strings.GetString("f111", "40").Replace("%d", "1"), // "Add $1 to Ante bet"
                Disabled = true
            };
            Add(Ante1ChipButton);
            Ante5ChipButton = new UIButton(Chip5ButtonTexture)
            {
                Position = new Vector2(45, 233),
                Tooltip = GameFacade.Strings.GetString("f111", "40").Replace("%d", "5"), // "Add $5 to Ante bet"
                Disabled = true
            };
            Add(Ante5ChipButton);
            Ante10ChipButton = new UIButton(Chip10ButtonTexture)
            {
                Position = new Vector2(63, 255),
                Tooltip = GameFacade.Strings.GetString("f111", "40").Replace("%d", "10"), // "Add $10 to Ante bet"
                Disabled = true
            };
            Add(Ante10ChipButton);
            Ante25ChipButton = new UIButton(Chip25ButtonTexture)
            {
                Position = new Vector2(82, 232),
                Tooltip = GameFacade.Strings.GetString("f111", "40").Replace("%d", "25"), // "Add $25 to Ante bet"
                Disabled = true
            };
            Add(Ante25ChipButton);
            Ante100ChipButton = new UIButton(Chip100ButtonTexture)
            {
                Position = new Vector2(103, 254),
                Tooltip = GameFacade.Strings.GetString("f111", "40").Replace("%d", "100"), // "Add $100 to Ante bet"
                Disabled = true
            };
            Add(Ante100ChipButton);
            

            // buttons for side bets
            Side1ChipButton = new UIButton(Chip1ButtonTexture)
            {
                Position = new Vector2(25, 253),
                Tooltip = GameFacade.Strings.GetString("f111", "41").Replace("%d", "1"), // "Add $1 to Side bet"
                Disabled = true
            };
            Add(Side1ChipButton);
            Side1ChipButton.Y = Ante1ChipButton.Y + 47;
            Side5ChipButton = new UIButton(Chip5ButtonTexture)
            {
                Position = new Vector2(45, 233),
                Tooltip = GameFacade.Strings.GetString("f111", "41").Replace("%d", "5"), // "Add $5 to Side bet"
                Disabled = true
            };
            Add(Side5ChipButton);
            Side5ChipButton.Y += 91;
            Side10ChipButton = new UIButton(Chip10ButtonTexture)
            {
                Position = new Vector2(63, 255),
                Tooltip = GameFacade.Strings.GetString("f111", "41").Replace("%d", "10"), // "Add $10 to Side bet"
                Disabled = true
            };
            Add(Side10ChipButton);
            Side10ChipButton.Y = Ante10ChipButton.Y + 47;
            Side25ChipButton = new UIButton(Chip25ButtonTexture)
            {
                Position = new Vector2(82, 232),
                Tooltip = GameFacade.Strings.GetString("f111", "41").Replace("%d", "25"), // "Add $25 to Side bet"
                Disabled = true
            };
            Add(Side25ChipButton);
            Side25ChipButton.Y += 91;
            Side100ChipButton = new UIButton(Chip100ButtonTexture)
            {
                Position = new Vector2(103, 254),
                Tooltip = GameFacade.Strings.GetString("f111", "41").Replace("%d", "100"), // "Add $100 to Side bet"
                Disabled = true
            };
            Side100ChipButton.Y = Ante100ChipButton.Y + 47;
            Add(Side100ChipButton);
            

            // diving line
            var horizontalDivider = new UIHighlightSprite(107, 1, 0.25f);
            horizontalDivider.X = Ante1ChipButton.X;
            horizontalDivider.Y = (Side1ChipButton.Y + Ante1ChipButton.Y + Ante1ChipButton.Texture.Height) / 2;
            Add(horizontalDivider);


            // bet text fields and their labels
            AnteBetBack = new UIImage(BetAmountBGTexture)
            {
                X = Ante100ChipButton.X + 40,
                Y = Ante1ChipButton.Y + 4
            };
            Add(AnteBetBack);
            SideBetBack = new UIImage(BetAmountBGTexture)
            {
                X = AnteBetBack.X,
                Y = AnteBetBack.Y + AnteBetBack.Texture.Height + 4
            };
            Add(SideBetBack);
            AnteBetLabel = new UILabel()
            {
                X = AnteBetBack.X,
                Y = Ante5ChipButton.Y,
                Alignment = TextAlignment.Center,
                Caption = GameFacade.Strings.GetString("f111", "39"), // "Ante Bet"
            };
            Add(AnteBetLabel);
            SideBetLabel = new UILabel()
            {
                X = SideBetBack.X,
                Y = Side5ChipButton.Y,
                Alignment = TextAlignment.Center,
                Caption = GameFacade.Strings.GetString("f111", "42"), // "Side Bet"
            };
            Add(SideBetLabel);
            AnteBet = new UITextEdit()
            {
                X = AnteBetBack.X,
                Y = AnteBetBack.Y + 3,
                Size = AnteBetBack.Size.ToVector2(),
                Alignment = TextAlignment.Center,
                CurrentText = "Ante",
                Mode = UITextEditMode.ReadOnly,
            };
            var myBetTextStyle = AnteBet.TextStyle.Clone();
            myBetTextStyle.Size += 2;
            AnteBet.TextStyle = myBetTextStyle;
            Add(AnteBet);
            SideBet = new UITextEdit()
            {
                X = SideBetBack.X,
                Y = SideBetBack.Y + 3,
                Size = SideBetBack.Size.ToVector2(),
                Alignment = TextAlignment.Center,
                CurrentText = "Side",
                Mode = UITextEditMode.ReadOnly,
                TextStyle = myBetTextStyle
            };
            Add(SideBet);


            // submit and help buttons
            SubmitBetsButtonBack = new UIImage(ButtonSeat)
            {
                X = AnteBetBack.X + 80,
                Y = AnteBetBack.Y + 14
            };
            Add(SubmitBetsButtonBack);
            SubmitBetsButton = new UIButton(SubmitBetsButtonTexture)
            {
                Position = SubmitBetsButtonBack.Position + ButtonSeatOffset,
                Tooltip = GameFacade.Strings["UIText", "263", "14"], // "Place Bet"
                Disabled = true
            };
            Add(SubmitBetsButton);
            HelpAnteBetButton = new UIButton(HelpButtonTexture)
            {
                X = SubmitBetsButton.X - 25,
                Y = AnteBetLabel.Y + 1,
                Tooltip = GameFacade.Strings.GetString("f111", "83"), // "Ante Bet Help"
            };
            HelpAnteBetButtonBack = new UIImage(ButtonSeat)
            {
                Position = HelpAnteBetButton.Position - (ButtonSeatOffset * 0.6f)
            };
            HelpAnteBetButtonBack.ScaleX = HelpAnteBetButtonBack.ScaleY = 0.6f;
            Add(HelpAnteBetButtonBack);
            Add(HelpAnteBetButton);
            HelpSideBetButton = new UIButton(HelpButtonTexture)
            {
                X = SubmitBetsButton.X - 27,
                Y = SideBetLabel.Y + 1,
                Tooltip = GameFacade.Strings.GetString("f111", "84"), // "Side Bet Help"
            };
            HelpSideBetButtonBack = new UIImage(ButtonSeat)
            {
                Position = HelpSideBetButton.Position - (ButtonSeatOffset * 0.6f)
            };
            HelpSideBetButtonBack.ScaleX = HelpSideBetButtonBack.ScaleY = 0.6f;
            Add(HelpSideBetButtonBack);
            Add(HelpSideBetButton);


            // the clearing bet buttons
            ClearAnteBetButton = new UIButton(ClearBetButtonTexture)
            {
                X = SubmitBetsButton.X,
                Y = SubmitBetsButton.Y - 38,
                Tooltip = GameFacade.Strings.GetString("f111", "44"), // "Clear Ante Bet"
                Disabled = true
            };
            ClearAnteBetButtonBack = new UIImage(ButtonSeat)
            {
                Position = ClearAnteBetButton.Position - ButtonSeatOffset
            };
            Add(ClearAnteBetButtonBack);
            Add(ClearAnteBetButton);

            ClearSideBetButton = new UIButton(ClearBetButtonTexture)
            {
                X = SubmitBetsButton.X,
                Y = SubmitBetsButton.Y + 38,
                Tooltip = GameFacade.Strings.GetString("f111", "45"), // "Clear Side Bet"
                Disabled = true
            };
            ClearSideBetButtonBack = new UIImage(ButtonSeat)
            {
                Position = ClearSideBetButton.Position - ButtonSeatOffset
            };
            Add(ClearSideBetButtonBack);
            Add(ClearSideBetButton);


            // large cards container
            MyCardHand = new TwoCardHand(2.5f) // 2f
            {
                X = SubmitBetsButton.X + 40, // + 60
                Y = ClearAnteBetButton.Y // + 10
            };
            Add(MyCardHand);


            // add button listeners
            Ante1ChipButton.OnButtonClick += (btn) => { AnteChipButtonPressed(1); };
            Ante5ChipButton.OnButtonClick += (btn) => { AnteChipButtonPressed(5); };
            Ante10ChipButton.OnButtonClick += (btn) => { AnteChipButtonPressed(10); };
            Ante25ChipButton.OnButtonClick += (btn) => { AnteChipButtonPressed(25); };
            Ante100ChipButton.OnButtonClick += (btn) => { AnteChipButtonPressed(100); };
            Side1ChipButton.OnButtonClick += (btn) => { SideChipButtonPressed(1); };
            Side5ChipButton.OnButtonClick += (btn) => { SideChipButtonPressed(5); };
            Side10ChipButton.OnButtonClick += (btn) => { SideChipButtonPressed(10); };
            Side25ChipButton.OnButtonClick += (btn) => { SideChipButtonPressed(25); };
            Side100ChipButton.OnButtonClick += (btn) => { SideChipButtonPressed(100); };
            AnteBet.OnChange += MyAnteBetHandler;
            SideBet.OnChange += MySideBetHandler;
            SubmitBetsButton.OnButtonClick += (btn) => { SubmitBetsHandler(); };
            CallAndFoldHelpButton.OnButtonClick += (btn) => { ShowUIAlert(Holdem, AlertStrings[(byte)VMEODHoldEmCasinoAlerts.Call_Fold_Help], null); };
            HelpAnteBetButton.OnButtonClick += (btn) => { ShowUIAlert(Holdem,
                AlertStrings[(byte)VMEODHoldEmCasinoAlerts.Ante_Bet_Help].Replace("%n", "" + MinAnteBet).Replace("%x", "" + MaxAnteBet), null); };
            HelpSideBetButton.OnButtonClick += (btn) => { ShowUIAlert(Holdem,
                AlertStrings[(byte)VMEODHoldEmCasinoAlerts.Side_Bet_Help].Replace("Min: $%n ", "").Replace("%x", "" + MaxSideBet), null); };
            ClearAnteBetButton.OnButtonClick += (btn) => { ClearAnteBet(); };
            ClearSideBetButton.OnButtonClick += (btn) => { ClearSideBet(); };
            CallButton.OnButtonClick += (btn) => { CallOrFoldHandler(1); };
            FoldButton.OnButtonClick += (btn) => { CallOrFoldHandler(0); };


            // one final adjustment
            Ante1ChipButton.Y += 5;
            Ante5ChipButton.Y += 5;
            Ante10ChipButton.Y += 5;
            Ante25ChipButton.Y += 5;
            Ante100ChipButton.Y += 5;
            Side1ChipButton.Y -= 5;
            Side5ChipButton.Y -= 5;
            Side10ChipButton.Y -= 5;
            Side25ChipButton.Y -= 5;
            Side100ChipButton.Y -= 5;
        }
        private void InitUpperUI()
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

            // community and player cards
            CommunityHand = new FiveCardHand(1.25f)
            {
                Position = (new Vector2 (503, 321) - new Vector2(CommunityCardsWidth * 1.25f, 0)) / 2
            };
            CommunityHand.X += 64;
            Add(CommunityHand);
            Player1CardsContainer = new TwoCardHand(1f)
            {
                X = 71,
                Y = 97,
            };
            Add(Player1CardsContainer);
            Player1CardsContainer.SetInactive();
            Player2CardsContainer = new TwoCardHand(1f)
            {
                X = Player1CardsContainer.X + (102),
                Y = 97,
            };
            Add(Player2CardsContainer);
            Player2CardsContainer.SetInactive();
            Player3CardsContainer = new TwoCardHand(1f)
            {
                X = Player1CardsContainer.X + (102 * 2),
                Y = 97,
            };
            Add(Player3CardsContainer);
            Player3CardsContainer.SetInactive();
            Player4CardsContainer = new TwoCardHand(1f)
            {
                X = Player1CardsContainer.X + (102 * 3),
                Y = 97,
            };
            Add(Player4CardsContainer);
            Player4CardsContainer.SetInactive();
            DealerCardsContainer = new TwoCardHand(1f)
            {
                X = 110,
                Y = CommunityHand.Y + 5.5f
            };
            Add(DealerCardsContainer);
            DealerCardsContainer.SetInactive();

            // dividing lines
            LeftDivider = new UIHighlightSprite(1, 60, 0.25f);
            LeftDivider.X = Player1CardsContainer.X + 68;
            LeftDivider.Y = Player1CardsContainer.Y;
            Add(LeftDivider);
            MiddleDivider = new UIHighlightSprite(1, 60, 0.25f);
            MiddleDivider.X = Player2CardsContainer.X + 68;
            MiddleDivider.Y = Player1CardsContainer.Y;
            Add(MiddleDivider);
            RightDivider = new UIHighlightSprite(1, 60, 0.25f);
            RightDivider.X = Player3CardsContainer.X + 68;
            RightDivider.Y = Player1CardsContainer.Y;
            Add(RightDivider);

            // VMPersonButtons
            Player1Head = new UIImage(UpperPlayersVMPersonButtonBGTex)
            {
                X = Player1CardsContainer.X - 26,
                Y = Player1CardsContainer.Y + 7
            };
            Add(Player1Head);
            Player2Head = new UIImage(UpperPlayersVMPersonButtonBGTex)
            {
                X = Player2CardsContainer.X - 26,
                Y = Player2CardsContainer.Y + 7
            };
            Add(Player2Head);
            Player3Head = new UIImage(UpperPlayersVMPersonButtonBGTex)
            {
                X = Player3CardsContainer.X - 26,
                Y = Player3CardsContainer.Y + 7
            };
            Add(Player3Head);
            Player4Head = new UIImage(UpperPlayersVMPersonButtonBGTex)
            {
                X = Player4CardsContainer.X - 26,
                Y = Player4CardsContainer.Y + 7
            };
            Add(Player4Head);
            DealerHead = new UIImage(UpperPlayersVMPersonButtonBGTex)
            {
                X = DealerCardsContainer.X - 42,
                Y = DealerCardsContainer.Y + 7 - 5.5f
            };
            Add(DealerHead);

            // bet backs and labels
            Player1AnteBetBack = new UIImage(UpperPlayersBetBack)
            {
                X = Player1Head.X,
                Y = Player1CardsContainer.Y + 43,
                ScaleX = 0.75f
            };
            Add(Player1AnteBetBack);
            Player1AnteBetLabel = new UILabel()
            {
                X = Player1Head.X,
                Y = Player1CardsContainer.Y + 42,
                Alignment = TextAlignment.Center,
            };

            var style = Player1AnteBetLabel.CaptionStyle.Clone();
            style.Size = 10;
            Player1AnteBetLabel.CaptionStyle = style;

            Add(Player1AnteBetLabel);
            Player1SideBetBack = new UIImage(UpperPlayersBetBack)
            {
                X = Player1Head.X + 50,
                Y = Player1CardsContainer.Y + 43,
                ScaleX = 0.75f
            };
            Add(Player1SideBetBack);
            Player1SideBetLabel = new UILabel()
            {
                X = Player1Head.X + 52,
                Y = Player1CardsContainer.Y + 42,
                Alignment = TextAlignment.Center,
                CaptionStyle = style
            };
            Add(Player1SideBetLabel);

            Player2AnteBetBack = new UIImage(UpperPlayersBetBack)
            {
                X = Player2Head.X,
                Y = Player2CardsContainer.Y + 43,
                ScaleX = 0.75f
            };
            Add(Player2AnteBetBack);
            Player2AnteBetLabel = new UILabel()
            {
                X = Player2Head.X,
                Y = Player2CardsContainer.Y + 42,
                Alignment = TextAlignment.Center,
                CaptionStyle = style
            };
            Add(Player2AnteBetLabel);
            Player2SideBetBack = new UIImage(UpperPlayersBetBack)
            {
                X = Player2Head.X + 50,
                Y = Player2CardsContainer.Y + 43,
                ScaleX = 0.75f
            };
            Add(Player2SideBetBack);
            Player2SideBetLabel = new UILabel()
            {
                X = Player2Head.X + 52,
                Y = Player2CardsContainer.Y + 42,
                Alignment = TextAlignment.Center,
                CaptionStyle = style
            };
            Add(Player2SideBetLabel);

            Player3AnteBetBack = new UIImage(UpperPlayersBetBack)
            {
                X = Player3Head.X,
                Y = Player3CardsContainer.Y + 43,
                ScaleX = 0.75f
            };
            Add(Player3AnteBetBack);
            Player3AnteBetLabel = new UILabel()
            {
                X = Player3Head.X,
                Y = Player3CardsContainer.Y + 42,
                Alignment = TextAlignment.Center,
                CaptionStyle = style
            };
            Add(Player3AnteBetLabel);
            Player3SideBetBack = new UIImage(UpperPlayersBetBack)
            {
                X = Player3Head.X + 50,
                Y = Player3CardsContainer.Y + 43,
                ScaleX = 0.75f
            };
            Add(Player3SideBetBack);
            Player3SideBetLabel = new UILabel()
            {
                X = Player3Head.X + 52,
                Y = Player3CardsContainer.Y + 42,
                Alignment = TextAlignment.Center,
                CaptionStyle = style
            };
            Add(Player3SideBetLabel);

            Player4AnteBetBack = new UIImage(UpperPlayersBetBack)
            {
                X = Player4Head.X,
                Y = Player4CardsContainer.Y + 43,
                ScaleX = 0.75f
            };
            Add(Player4AnteBetBack);
            Player4AnteBetLabel = new UILabel()
            {
                X = Player4Head.X,
                Y = Player4CardsContainer.Y + 42,
                Alignment = TextAlignment.Center,
                CaptionStyle = style
            };
            Add(Player4AnteBetLabel);
            Player4SideBetBack = new UIImage(UpperPlayersBetBack)
            {
                X = Player4Head.X + 50,
                Y = Player4CardsContainer.Y + 43,
                ScaleX = 0.75f
            };
            Add(Player4SideBetBack);
            Player4SideBetLabel = new UILabel()
            {
                X = Player4Head.X + 52,
                Y = Player4CardsContainer.Y + 42,
                Alignment = TextAlignment.Center,
                CaptionStyle = style
            };
            Add(Player4SideBetLabel);

            DealerBetBack = new UIImage(BetAmountBGTexture)
            {
                X = DealerHead.X - 15,
                Y = CommunityHand.Y + 1.25f * 42f - 20f,
                ScaleX = 0.75f,
                ScaleY = 0.75f
            };
            DealerBetBack.Y -= 2;
            Add(DealerBetBack);
            DealerBetAmount = new UITextEdit()
            {
                X = DealerBetBack.X - 10,
                Y = DealerBetBack.Y + 1,
                Size = DealerBetBack.Size.ToVector2(),
                CurrentText = "Dealer",
                Alignment = TextAlignment.Center,
                //TextStyle = captionStyle,
                Mode = UITextEditMode.ReadOnly
            };
            Add(DealerBetAmount);

            // set the default captions and positioning for the bet labels
            SetPlayerBet(true, 1, "Ante");
            SetPlayerBet(true, 2, "Ante");
            SetPlayerBet(true, 3, "Ante");
            SetPlayerBet(true, 4, "Ante");
            SetPlayerBet(false, 1, "Side");
            SetPlayerBet(false, 2, "Side");
            SetPlayerBet(false, 3, "Side");
            SetPlayerBet(false, 4, "Side");
        }
        private void ClearAnteBet()
        {
            SetMyAnteBet("0");
        }
        private void ClearSideBet()
        {
            SetMySideBet("0");
        }
        private void ResetAllHands()
        {
            SetPlayerBet(true, 1, "Ante");
            SetPlayerBet(true, 2, "Ante");
            SetPlayerBet(true, 3, "Ante");
            SetPlayerBet(true, 4, "Ante");
            SetPlayerBet(false, 1, "Side");
            SetPlayerBet(false, 2, "Side");
            SetPlayerBet(false, 3, "Side");
            SetPlayerBet(false, 4, "Side");
            UpdateUpperPlayerHand(1, null);
            UpdateUpperPlayerHand(2, null);
            UpdateUpperPlayerHand(3, null);
            UpdateUpperPlayerHand(4, null);
            UpdateUpperPlayerHand(5, null);
            UpdateUpperPlayerHand(6, null);
            MyCardHand.AddCard(null);
        }
        /*
         * Update the cards in a player's hand (upper UI panel)
         */
        private void UpdateUpperPlayerHand(int playerNumber, params string[] cards)
        {
            TwoCardHand cardsContainer;
            switch (playerNumber)
            {
                case 1: cardsContainer = Player1CardsContainer; break;
                case 2: cardsContainer = Player2CardsContainer; break;
                case 3: cardsContainer = Player3CardsContainer; break;
                case 4: cardsContainer = Player4CardsContainer; break;
                case 5: cardsContainer = DealerCardsContainer; break;
                default:
                    {
                        if (cards != null)
                        {
                            foreach (var card in cards)
                                CommunityHand.AddCard(card);
                        }
                        else
                            CommunityHand.AddCard(null);
                        return;
                    }
            }
            if (cards != null)
            {
                foreach (var card in cards)
                    cardsContainer.AddCard(card);
            }
            else
                cardsContainer.AddCard(null);
        }
        /*
         * Shows a UI alert and even allows an action with zero arguments for when the window is closed using "OK" button.
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
         * Shows a UI alert and allows one action argument for when the window is closed with "Yes" and one for "No".
         */
        private UIAlert ShowUIAlert(string title, string message, Action yesAction, Action noAction)
        {
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = title,
                Message = message,
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = UIAlertButton.YesNo((yes) =>
                {
                    UIScreen.RemoveDialog(alert);
                    CurrentAlert = null;
                    yesAction?.Invoke();
                }, (no) =>
                {
                    UIScreen.RemoveDialog(alert);
                    CurrentAlert = null;
                    noAction?.Invoke();
                }),
            }, true);
            CurrentAlert = alert;
            return alert;
        }
        private void SetPlayerBet(bool isAnte, int player, string newBet)
        {
            float offsetX = 0f;
            if (newBet == null || newBet.Length < 1 || newBet.Length > 4 || player < 1 || player > 4)
                return;
            else if (newBet.Length == 4)
                offsetX = 3f;
            else if (newBet.Length == 3)
                offsetX = 7f;
            else if (newBet.Length == 2)
                offsetX = 11f;
            else
                offsetX = 16f;
            if (!isAnte)
                offsetX += 51f;

            UILabel label;
            float x = 0f;
            switch (player)
            {
                case 1:
                    {
                        x = Player1Head.X;
                        label = (isAnte) ? Player1AnteBetLabel : Player1SideBetLabel; break;
                    }
                case 2:
                    {
                        x = Player2Head.X;
                        label = (isAnte) ? Player2AnteBetLabel : Player2SideBetLabel; break;
                    }
                case 3:
                    {
                        x = Player3Head.X;
                        label = (isAnte) ? Player3AnteBetLabel : Player3SideBetLabel; break;
                    }
                default:
                    {
                        x = Player4Head.X;
                        label = (isAnte) ? Player4AnteBetLabel : Player4SideBetLabel; break;
                    }
            }
            label.X = x + offsetX;
            label.Caption = newBet;
        }
        private void SetMyAnteBet(string newBet)
        {
            AnteBet.CurrentText = newBet;
            MyAnteBetHandler(AnteBet);
        }
        private void SetMySideBet(string newBet)
        {
            SideBet.CurrentText = newBet;
            MySideBetHandler(SideBet);
        }
        private void SetActiveOtherPlayerHand(int playerIndex)
        {
            Player1CardsContainer.SetInactive();
            Player2CardsContainer.SetInactive();
            Player3CardsContainer.SetInactive();
            Player4CardsContainer.SetInactive();
            DealerCardsContainer.SetInactive();
            if (playerIndex == 1)
                Player1CardsContainer.SetActive();
            else if (playerIndex == 2)
                Player2CardsContainer.SetActive();
            else if (playerIndex == 3)
                Player3CardsContainer.SetActive();
            else if (playerIndex == 4)
                Player4CardsContainer.SetActive();
            else if (playerIndex == 5)
                DealerCardsContainer.SetActive();
        }
        private void SetNewTip(string newTip)
        {
            SetTip(newTip);
            Parent.Invalidate();
        }
        private void SetNewTime(int newTime)
        {
            SetTime(newTime);
            Parent.Invalidate();
        }

        #endregion
    }
    internal class TwoCardHand : CardHand
    {
        private UISlotsImage Background;
        private Vector2 FirstCardPosition = new Vector2(1f, 2f);
        private Vector2 SecondCardPosition = new Vector2(35f, 2f);
        private UIImage Card1 = new UIImage();
        private UIImage Card2 = new UIImage();
        public TwoCardHand(float targetScale)
        {
            // add the backbround
            Background = new UISlotsImage(UIHoldEmCasinoEOD.FourCardSlotsTexture);
            Background.SetBounds(0, 0, 63, 42);
            _CurrentScale = Background.ScaleX = Background.ScaleY = targetScale;
            Background.Reset();
            Add(Background);

            // add the cards
            Card1.ScaleX = Card1.ScaleY = _CurrentScale;
            Card1.Position = FirstCardPosition * _CurrentScale;
            Card1.Visible = false;
            Add(Card1);
            Card2.ScaleX = Card2.ScaleY = _CurrentScale;
            Card2.Position = SecondCardPosition * _CurrentScale;
            Card2.Visible = false;
            Add(Card2);
        }
        new public void AddCard(String cardName)
        {
            if (cardName == null)
                AddNewCard(null);
            else if (cardName == "")
                AddNewCard(null);
            else
                AddNewCard(UIPlayingCard.GetFullCardImage(cardName));
        }
        private void AddNewCard(UIImage card)
        {
            SetActive();
            if (card != null)
            {
                UIImage targetCard = null;
                if (Card1.Visible == false)
                    targetCard = Card1;
                else if (Card2.Visible == false)
                    targetCard = Card2;
                if (targetCard != null)
                {
                    targetCard.Texture = card.Texture;
                    targetCard.Visible = true;
                }
            }
            else // it's null, clear all cards
            {
                Card1.Visible = false;
                Card2.Visible = false;
            }
        }
    }
    internal class FiveCardHand : CardHand
    {
        private UISlotsImage Background;
        private Vector2 CardStartOffset = new Vector2(2f, 2f);
        // card widths with spacers = 44 (card width 26) spaces are 18. 44 times card number plus (3,2)
        private UIImage Card1 = new UIImage();
        private UIImage Card2 = new UIImage();
        private UIImage Card3 = new UIImage();
        private UIImage Card4 = new UIImage();
        private UIImage Card5 = new UIImage();
        
        public FiveCardHand(float targetScale)
        {
            // add the background
            Background = new UISlotsImage(UIHoldEmCasinoEOD.CommunityCardsBGTexture);
            Background.SetBounds(106, 25, UIHoldEmCasinoEOD.CommunityCardsWidth, UIHoldEmCasinoEOD.CommunityCardsHeight);
            _CurrentScale = Background.ScaleX = Background.ScaleY = targetScale;
            Background.Reset();
            Add(Background);
            
            // add the cards
            Card1.ScaleX = Card1.ScaleY = _CurrentScale;
            Card1.Position = _CurrentScale * CardStartOffset;
            Card1.Visible = false;
            Add(Card1);
            Card2.ScaleX = Card2.ScaleY = _CurrentScale;
            Card2.Position = _CurrentScale * (CardStartOffset + new Vector2(41, 0));
            Card2.Visible = false;
            Add(Card2);
            Card3.ScaleX = Card3.ScaleY = _CurrentScale;
            Card3.Position = _CurrentScale * (CardStartOffset + new Vector2(41 * 2, 0));
            Card3.Visible = false;
            Add(Card3);
            Card4.ScaleX = Card4.ScaleY = _CurrentScale;
            Card4.Position = _CurrentScale * (CardStartOffset + new Vector2(41 * 3, 0));
            Card4.Visible = false;
            Add(Card4);
            Card5.ScaleX = Card5.ScaleY = _CurrentScale;
            Card5.Position = _CurrentScale * (CardStartOffset + new Vector2(41 * 4, 0));
            Card5.Visible = false;
            Add(Card5);
        }
        new public void AddCard(String cardName)
        {
            if (cardName == null)
                AddNewCard(null);
            else if (cardName == "")
                AddNewCard(null);
            else
                AddNewCard(UIPlayingCard.GetFullCardImage(cardName));
        }
        private void AddNewCard(UIImage card)
        {
            SetActive();
            if (card != null)
            {
                UIImage targetCard = null;
                if (Card1.Visible == false)
                    targetCard = Card1;
                else if (Card2.Visible == false)
                    targetCard = Card2;
                else if (Card3.Visible == false)
                    targetCard = Card3;
                else if (Card4.Visible == false)
                    targetCard = Card4;
                else if (Card5.Visible == false)
                    targetCard = Card5;
                if (targetCard != null)
                {
                    targetCard.Texture = card.Texture;
                    targetCard.Visible = true;
                }
            }
            else // it's null, clear all cards
            {
                Card1.Visible = false;
                Card2.Visible = false;
                Card3.Visible = false;
                Card4.Visible = false;
                Card5.Visible = false;
            }
        }
    }
}
