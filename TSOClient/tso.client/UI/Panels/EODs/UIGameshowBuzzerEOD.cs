using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Client.UI.Controls;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIGameshowBuzzerEOD : UIEOD
    {
        //shared assets
        private Texture2D PlayerScoreBackTexture = GetTexture(0x95500000001); // eod_buzzer_playerscoreback
        private Texture2D PlayersVMPersonButtonBackTex = GetTexture(0x000002B300000001); // EOD_PizzaHeadPlaceholder1.bmp

        private UIVMPersonButton FocusedPlayer;
        private UIImage FocusedPlayerScoreBack;

        EODLiveModeOpt EODLiveModeOptions;


        // host lower assets
        private UIImage Player1PersonBtnBack;
        private UIVMPersonButton Player1PersonBtn;

        private Texture2D ButtonSeatTexture;

        private Texture2D MovePlayerRightbtnTexture;
        private UIButton MovePlayer1Rightbtn;

        private Texture2D MovePlayerLeftbtnTexture;
        private UIButton MovePlayer1Leftbtn;

        private Texture2D EnablebtnTexture;
        private UIButton EnablePlayer1Btn;

        private Texture2D HelpbtnTexture;
        private UIImage EnablePlayerHelpbtnBack; // Scale to 0.6f
        private UIButton EnablePlayerHelpbtn;
        private UIImage MovePlayerHelpbtnBack; // Scale to 0.6f
        private UIButton MovePlayerHelpbtn;
        private UIImage SearchPlayerHelpbtnBack; // Scale to 0.6f
        private UIButton SearchPlayerHelpbtn;

        private Texture2D BuzzerTogglebtnTexture;
        private UIButton BuzzerTogglebtn;

        private UIImage Player1ScoreBack;
        private UITextEdit Player1ScoreTextEdit;

        private Texture2D SearchOtherPlayersbtnTexture;
        private UIImage SearchOtherPlayersbtnBack;
        private UIButton SearchOtherPlayersbtn;

        private UILabel BuzzerEnabledLabel;
        private UILabel BuzzerDisabledLabel;
        private UILabel PlayersEnabledLabel;
        private UILabel MovePlayersLabel;
        private UILabel PlayersScoresLabel;
        private UILabel SearchForOtherPlayersLabel;

        // host upper assets
        private UIImage EODTallBack;
        private UIImage EODTallBackEnd;

        private UIImage SetScoreBack;
        private UITextEdit SetScoreTextEdit;
        private Texture2D SetTimeBackTexture;
        private UIImage SetTimeBack;
        private UITextEdit SetTimeTextEdit;

        private UIImage FocusedPlayerCorrectButtonSeat;
        private UIImage FocusedPlayerIncorrectButtonSeat;
        private Texture2D FocusedPlayerCorrectbtnTexture;
        private UIButton FocusedPlayerCorrectbtn;
        private Texture2D FocusedPlayerIncorrectbtnTexture;
        private UIButton FocusedPlayerIncorrectbtn;


        public UIGameshowBuzzerEOD(UIEODController controller) : base(controller)
        {
            InitUI();
        }

        /// <summary>
        /// 
        /// </summary>
        private void InitHostUI()
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

            InitHostUpperUI();
            InitHostLowerUI();

            // init EOD options
            EODLiveModeOptions = new EODLiveModeOpt
            {
                Buttons = 0,
                Height = EODHeight.TallTall,
                Length = EODLength.Full,
                Tips = EODTextTips.Short,
                Timer = EODTimer.None,
                Expandable = false,
                Expanded = true
            };

        }
        /// <summary>
        /// 
        /// 
        /// </summary>
        private void InitPlayerUI()
        {
            // init EOD options
            EODLiveModeOptions = new EODLiveModeOpt
            {
                Buttons = 0,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Tips = EODTextTips.Short,
                Timer = EODTimer.Normal
            };

        }
        /// <summary>
        /// 
        /// 
        /// </summary>
        private void InitHostLowerUI()
        {
            // textures
            ButtonSeatTexture = GetTexture(0x000001A100000002); // eod_buttonseat_transparent.tga
            MovePlayerRightbtnTexture = GetTexture(0x28B00000001); // eod_costumetrunkscrollupbtn
            MovePlayerLeftbtnTexture = GetTexture(0x28A00000001); // eod_costumetrunkscrolldownbtn
            EnablebtnTexture = GetTexture(0x46000000001); // sounds_checkboxbtn
            HelpbtnTexture = GetTexture(0x4E100000001);  // lpanel_eodhelpbtn
            BuzzerTogglebtnTexture = GetTexture(0x95800000001); // eod_buzzer_toggleonoff
            SearchOtherPlayersbtnTexture = GetTexture(0x31300000001); // gizmo_searchbtn


        }

        private void InitHostUpperUI()
        {
            // textures
            SetTimeBackTexture = GetTexture(0x95600000001); // eod_buzzer_playertimerback
            FocusedPlayerCorrectbtnTexture = GetTexture(0x31700000001); // gizmo_top100listsbtn
            FocusedPlayerIncorrectbtnTexture = GetTexture(0x95900000001); // eod_cancelbtn


        }
    }
}
