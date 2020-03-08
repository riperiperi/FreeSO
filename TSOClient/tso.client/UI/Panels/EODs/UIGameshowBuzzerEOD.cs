using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Model;
using FSO.Client.UI.Framework;
using FSO.Common.Utils;
using FSO.Content.Model;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIGameshowBuzzerEOD : UIEOD
    {
        //shared assets
        private Texture2D PlayerScoreBackTexture = GetTexture(0x95500000001); // eod_buzzer_playerscoreback
        private Texture2D PlayersVMPersonButtonBackTex = GetTexture(0x000002B300000001); // EOD_PizzaHeadPlaceholder1.bmp

        EODLiveModeOpt EODLiveModeOptions;

        /*
         * player assets
         */

        private UIImage PlayerScoreBack;
        private UIImage ScoreBack;
        private UITextEdit PlayerScore;

        /*
         * host  assets
         */
        private UIImage EODBuzzersBack;
        private Texture2D ButtonSeatTexture;
        private UIContainer Stage;

        // more options panel
        private UIContainer MoreOptionsPanel;
        private UIImage EODMoreOptionsBack;
        private UITextEdit MoreOptionsTitle;
        private Texture2D SetTimeBackTexture;
        private UIImage SetAnswerTimeBack;
        private UITextEdit SetAnswerTimeTextEdit;
        private UITextEdit SetAnswerTimeLabel;
        private UIImage SetBuzzerTimeBack;
        private UITextEdit SetBuzzerTimeTextEdit;
        private UITextEdit SetBuzzerTimeLabel;
        private UIButton AutoDeductWrongPointsbtn;
        private UITextEdit AutoDeductWrongPointsLabel;
        private UIButton AllowNegativeScoresbtn;
        private UITextEdit AllowNegativeScoresLabel;
        private UIButton AutoDisableOnWrongbtn;
        private UITextEdit AutoDisableOnWrongLabel;
        private UIButton AutoEnableAllOnRightbtn;
        private UITextEdit AutoEnableAllOnRightLabel;
        private UITextEdit GoBackLabel;
        private UIButton GoBackbtn;
        private UIImage GoBackbtnBack;

        // master buzzer
        private Texture2D BuzzerTogglebtnTexture;
        private UIInvisibleButton BuzzerTogglebtn;
        private UISlotsImage BuzzerToggledOnImage;
        private UISlotsImage BuzzerToggledOffImage;
        private UITextEdit BuzzerEnabledLabel;
        private UITextEdit BuzzerDisabledLabel;
        private UITextEdit BuzzerMasterLabel;

        // current-question-point-value and more-options assets
        private UITextEdit GlobalScore;
        private UITextEdit GlobalScoreLabel;
        private UIImage PointValueBack;
        private UIButton Optionsbtn;
        private UITextEdit OptionsButtonTextEdit;

        // player slot lights
        private ContestantLightsFrame[] PlayerLights;

        // enable player buttons
        private UILabel EnablePlayersLabel;
        private Texture2D CheckboxbtnTexture;
        private UIButton[] EnablePlayer;

        // players' faces
        private UIImage Player1PersonBtnBack;
        private UIVMPersonButton Player1PersonBtn;
        private UIImage Player2PersonBtnBack;
        private UIVMPersonButton Player2PersonBtn;
        private UIImage Player3PersonBtnBack;
        private UIVMPersonButton Player3PersonBtn;
        private UIImage Player4PersonBtnBack;
        private UIVMPersonButton Player4PersonBtn;

        // player move buttons
        private Texture2D MovePlayerRightbtnTexture;
        private UIButton[] MovePlayerRightbtn;

        private Texture2D MovePlayerLeftbtnTexture;
        private UIButton[] MovePlayerLeftbtn;

        // find new players buttons
        private Texture2D FindNewPlayerbtnTexture;
        private UIButton[] FindNewPlayerbtn;

        // player scores
        private UITextEdit[] PlayerScores;

        // player correct and incorrect buttons
        private Texture2D PlayerCorrectbtnTexture;
        private Texture2D PlayerIncorrectbtnTexture;
        private UIButton[] PlayerCorrectbtn;
        private UIButton[] PlayerIncorrectbtn;

        // help btns
        private Texture2D HelpbtnTexture;
        private UIImage LowerUIHelpbtnBack;
        private UIButton LowerUIHelpbtn;
        private UIImage UpperUIHelpbtnBack;
        private UIButton UpperUIHelpbtn;


        public UIGameshowBuzzerEOD(UIEODController controller) : base(controller)
        {
            BinaryHandlers["Buzzer_UIEOD_Init"] = InitHandler;
        }

        private void InitHandler(string evt, byte[] eodType)
        {
            if (eodType[0] == 1) // VMEODGameshowBuzzerPluginType.Host is 2
                ShowHostUI();
            else
                ShowPlayerUI();
        }

        private void FindNewPlayerbtnClicked(int player)
        {

        }

        private void PlayerMoveRightbtnClicked(int player)
        {
            HIT.HITVM.Get().PlaySoundEvent(UISounds.Click);
        }

        private void PlayerMoveLeftbtnClicked(int player)
        {
            HIT.HITVM.Get().PlaySoundEvent(UISounds.Click);
        }

        private void PlayerCorrectbtnClicked(int player)
        {
            HIT.HITVM.Get().PlaySoundEvent("jobobject_sequencec");
        }

        private void PlayerIncorrectbtnClicked(int player)
        {
            HIT.HITVM.Get().PlaySoundEvent("pizza_oven_buzzer");
        }

        private void EnablePlayerbtnClicked(int player)
        {
            HIT.HITVM.Get().PlaySoundEvent("tv_exp_switch");
        }

        private void OptionsbtnClicked(UIElement btn)
        {

            HIT.HITVM.Get().PlaySoundEvent(UISounds.Click);
            ShowOptionsPanel(30, 30, true, false, true, false);
        }

        private void GoBackbtnClicked(UIElement btn)
        {
            HIT.HITVM.Get().PlaySoundEvent(UISounds.Click);
            Remove(MoreOptionsPanel);
            Add(Stage);
        }

        private void MasterBuzzerbtnClicked(UIElement btn)
        {
            HIT.HITVM.Get().PlaySoundEvent("tv_exp_turnon");
            BuzzerToggledOnImage.Visible = !BuzzerToggledOnImage.Visible;
            BuzzerToggledOffImage.Visible = !BuzzerToggledOffImage.Visible;
        }



        /// <summary>
        /// 
        /// </summary>
        private void ShowHostUI()
        {
            InitHostUI();

            // init EOD options
            EODController.ShowEODMode(new EODLiveModeOpt
            {
                Height = EODHeight.ExtraTall,
                Length = EODLength.Full,
                Tips = EODTextTips.Short,
                Timer = EODTimer.None
            });
        }
        /// <summary>
        /// 
        /// 
        /// </summary>
        private void ShowPlayerUI()
        {

            // init EOD options
            EODController.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Tips = EODTextTips.Short,
                Timer = EODTimer.Normal
            });
        }

        private void ShowOptionsPanel(short buzzerTime, short answerTime, bool deductOnWrong, bool allowNegativePoints, bool disableOnWrong, bool enableOnRight)
        {
            Add(MoreOptionsPanel);
            SetBuzzerTimeTextEdit.CurrentText = buzzerTime + "";
            SetAnswerTimeTextEdit.CurrentText = answerTime + "";
            AutoDeductWrongPointsbtn.Selected = deductOnWrong;
            AllowNegativeScoresbtn.Selected = allowNegativePoints;
            AutoDisableOnWrongbtn.Selected = disableOnWrong;
            AutoEnableAllOnRightbtn.Selected = enableOnRight;
            Remove(Stage);
        }

        /// <summary>
        /// 
        /// 
        /// </summary>
        private void InitHostUI()
        {
            // textures
            SetTimeBackTexture = GetTexture(0x95600000001); // eod_buzzer_playertimerback
            PlayerCorrectbtnTexture = GetTexture(0x31700000001); // gizmo_top100listsbtn
            PlayerIncorrectbtnTexture = GetTexture(0x95900000001); // eod_cancelbtn
            ButtonSeatTexture = GetTexture(0x000001A100000002); // eod_buttonseat_transparent.tga
            MovePlayerRightbtnTexture = GetTexture(0x2D400000001); // eod_dc_editcardnextbtn
            MovePlayerLeftbtnTexture = GetTexture(0x2D500000001); // eod_dc_editcardpreviousbtn
            CheckboxbtnTexture = GetTexture(0x49300000001); // options_checkboxbtn
            HelpbtnTexture = GetTexture(0x4E100000001);  // lpanel_eodhelpbtn
            BuzzerTogglebtnTexture = GetTexture(0x95800000001); // eod_buzzer_toggleonoff
            FindNewPlayerbtnTexture = GetTexture(0x31300000001); // gizmo_searchbtn
            Texture2D optionsbtnTex = GetTexture(0x4C000000001); // ucp_optionsmodebtn
            Texture2D checkbtnTex = GetTexture(0x2D800000001); // eod_dc_sharedcheckbtn

            // custom textures
            var gd = GameFacade.GraphicsDevice;
            Texture2D extraTallBackTex = null;
            Texture2D lightsframe1hTex;
            Texture2D lightsframe2hTex;
            Texture2D lightsframebackhTex;
            Texture2D lightsframebluehTex;
            Texture2D lightsframeredhTex;

            // try to get the custom back for extratall
            AbstractTextureRef extraTallBackTexRef = new FileTextureRef("Content/Textures/EOD/lpanel_eodpanelextratallback.png");
            try
            {
                extraTallBackTex = extraTallBackTexRef.Get(gd);
                EODBuzzersBack = new UIImage(extraTallBackTex);
                EODMoreOptionsBack = new UIImage(extraTallBackTex);
            }
            catch (Exception e)
            {
                EODBuzzersBack = new UIImage();
                EODMoreOptionsBack = new UIImage();
            }
            EODBuzzersBack.Position = new Vector2(10, 96);
            AddAt(0, EODBuzzersBack);

            // try to get custom horizontal textures for lights
            try
            {
                AbstractTextureRef lightsframe1hRef = new FileTextureRef("Content/Textures/EOD/Buzzer/eod_lightsframe1h.png");
                lightsframe1hTex = lightsframe1hRef.Get(gd);
                AbstractTextureRef lightsframe2hRef = new FileTextureRef("Content/Textures/EOD/Buzzer/eod_lightsframe2h.png");
                lightsframe2hTex = lightsframe2hRef.Get(gd);
                AbstractTextureRef lightsframebackhRef = new FileTextureRef("Content/Textures/EOD/Buzzer/eod_lightsframebackh.png");
                lightsframebackhTex = lightsframebackhRef.Get(gd);
                AbstractTextureRef lightsframebluehRef = new FileTextureRef("Content/Textures/EOD/Buzzer/eod_lightsframeblueh.png");
                lightsframebluehTex = lightsframebluehRef.Get(gd);
                AbstractTextureRef lightsframeredhRef = new FileTextureRef("Content/Textures/EOD/Buzzer/eod_lightsframeredh.png");
                lightsframeredhTex = lightsframeredhRef.Get(gd);
            }
            catch (Exception e)  // all or nothing; if one isn't found, the class below will use their default counterparts and it will look ugly...but function
            {
                lightsframe1hTex = null;
                lightsframe2hTex = null;
                lightsframebackhTex = null;
                lightsframebluehTex = null;
                lightsframeredhTex = null;
            }

            Stage = new UIContainer();
            Add(Stage);

            /*
             * non-player-specific assets
             */
            GlobalScoreLabel = new UITextEdit()
            {
                X = 25,
                Y = 98,
                CurrentText = "Question Value",
                Alignment = TextAlignment.Center,
                Mode = UITextEditMode.ReadOnly,
                Size = new Vector2(120, 20)
            };
           Stage.Add(GlobalScoreLabel);
            
            var setGlobalScoreBack = new UIImage(PlayerScoreBackTexture)
            {
                X = 50,
                Y = 118
            };
           Stage.Add(setGlobalScoreBack);

            GlobalScore = new UITextEdit()
            {
                X = setGlobalScoreBack.X + 6,
                Y = setGlobalScoreBack.Y + 6,
                CurrentText = "100",
                MaxChars = 5,
                MaxLines = 1,
                Size = new Vector2(60, 20),
                Mode = UITextEditMode.Editor,
                Alignment = TextAlignment.Center,
                Tooltip = "Player Score"
            };
           Stage.Add(GlobalScore);

            // Options button
            var optionsbtnBack = new UIImage(ButtonSeatTexture)
            {
                X = 363,
                Y = 122,
                ScaleX = 0.65f,
                ScaleY = 0.65f
            };
           Stage.Add(optionsbtnBack);
            Optionsbtn = new UIButton(optionsbtnTex)
            {
                X = 365,
                Y = 124
            };
           Stage.Add(Optionsbtn);
            Optionsbtn.OnButtonClick += OptionsbtnClicked;
            OptionsButtonTextEdit = new UITextEdit()
            {
                X = Optionsbtn.X + 25,
                Y = Optionsbtn.Y,
                CurrentText = "Options",
                Mode = UITextEditMode.ReadOnly,
                Size = new Vector2(100, 20),
                Alignment = TextAlignment.Left
            };
           Stage.Add(OptionsButtonTextEdit);

            // master button and nearby labels
            BuzzerToggledOnImage = new UISlotsImage(BuzzerTogglebtnTexture)
            {
                X = 220,
                Y = 118,
                Visible = false
            };
            BuzzerToggledOnImage.SetBounds(37, 0, 37, 31);
           Stage.Add(BuzzerToggledOnImage);

            BuzzerToggledOffImage = new UISlotsImage(BuzzerTogglebtnTexture)
            {
                X = 220,
                Y = 118
            };
            BuzzerToggledOffImage.SetBounds(0, 0, 37, 31);
           Stage.Add(BuzzerToggledOffImage);

            BuzzerTogglebtn = new UIInvisibleButton(37, 31, TextureUtils.TextureFromColor(gd, new Color(new Vector4(0, 0, 0, 0)), 1, 1))
            {
                X = 220,
                Y = 118
            };
           Stage.Add(BuzzerTogglebtn);
            BuzzerTogglebtn.OnButtonClick += MasterBuzzerbtnClicked;

            BuzzerEnabledLabel = new UITextEdit()
            {
                X = BuzzerTogglebtn.X - 66,
                Y = BuzzerTogglebtn.Y + 6,
                CurrentText = "Disabled",
                Alignment = TextAlignment.Right,
                Mode = UITextEditMode.ReadOnly,
                Size = new Vector2(62, 20)
            };
           Stage.Add(BuzzerEnabledLabel);

            BuzzerDisabledLabel = new UITextEdit()
            {
                X = BuzzerTogglebtn.X + 40,
                Y = BuzzerTogglebtn.Y + 6,
                CurrentText = "Enabled",
                Mode = UITextEditMode.ReadOnly,
                Alignment = TextAlignment.Left,
                Size = new Vector2(60, 20)
            };
           Stage.Add(BuzzerDisabledLabel);

            BuzzerMasterLabel = new UITextEdit()
            {
                X = 175,
                Y = 98,
                CurrentText = "PLAYER BUZZERS",
                Alignment = TextAlignment.Center,
                Mode = UITextEditMode.ReadOnly,
                Size = new Vector2(120, 20)
            };
           Stage.Add(BuzzerMasterLabel);

            EnablePlayersLabel = new UILabel()
            {
                X = 218,
                Y = 155,
                Alignment = TextAlignment.Center,
                Caption = "Allow?"
            };
           Stage.Add(EnablePlayersLabel);

            // player 1

            Player1PersonBtnBack = new UIImage(PlayersVMPersonButtonBackTex)
            {
                X = 57,
                Y = 195
            };
           Stage.Add(Player1PersonBtnBack);

            // player 2

            Player2PersonBtnBack = new UIImage(PlayersVMPersonButtonBackTex)
            {
                X = 165,
                Y = 195
            };
           Stage.Add(Player2PersonBtnBack);

            // player 3

            Player3PersonBtnBack = new UIImage(PlayersVMPersonButtonBackTex)
            {
                X = 288,
                Y = 195
            };
           Stage.Add(Player3PersonBtnBack);

            // player 4

            Player4PersonBtnBack = new UIImage(PlayersVMPersonButtonBackTex)
            {
                X = 396,
                Y = 195
            };
           Stage.Add(Player4PersonBtnBack);

            // player light frames
            PlayerLights = new ContestantLightsFrame[4]
            {
             new ContestantLightsFrame(lightsframe1hTex, lightsframe2hTex, lightsframebackhTex, lightsframebluehTex, lightsframeredhTex){ X = Player1PersonBtnBack.X - 37f, Y = Player1PersonBtnBack.Y - 20f },
             new ContestantLightsFrame(lightsframe1hTex, lightsframe2hTex, lightsframebackhTex, lightsframebluehTex, lightsframeredhTex){ X = Player2PersonBtnBack.X - 37f, Y = Player2PersonBtnBack.Y - 20f },
             new ContestantLightsFrame(lightsframe1hTex, lightsframe2hTex, lightsframebackhTex, lightsframebluehTex, lightsframeredhTex){ X = Player3PersonBtnBack.X - 37f, Y = Player3PersonBtnBack.Y - 20f },
             new ContestantLightsFrame(lightsframe1hTex, lightsframe2hTex, lightsframebackhTex, lightsframebluehTex, lightsframeredhTex){ X = Player4PersonBtnBack.X - 37f, Y = Player4PersonBtnBack.Y - 20f }
            };
            foreach (var light in PlayerLights)Stage.Add(light);

            // enable player checkboxes
            EnablePlayer = new UIButton[4]
            {
                new UIButton(CheckboxbtnTexture){X = Player1PersonBtnBack.X + 3, Y = EnablePlayersLabel.Y},
                new UIButton(CheckboxbtnTexture){X = Player2PersonBtnBack.X + 3, Y = EnablePlayersLabel.Y},
                new UIButton(CheckboxbtnTexture){X = Player3PersonBtnBack.X + 3, Y = EnablePlayersLabel.Y},
                new UIButton(CheckboxbtnTexture){X = Player4PersonBtnBack.X + 3, Y = EnablePlayersLabel.Y}
            };
            // added and listeners added with move buttons below

            EnablePlayer[0].Selected = true;
            PlayerLights[0].Blue();
            EnablePlayer[1].Selected = true;
            PlayerLights[1].Flash();
            EnablePlayer[2].Selected = true;
            PlayerLights[2].Blue();

            /*
             * move players right and left buttons
             */
            MovePlayerLeftbtn = new UIButton[4]
            {
                new UIButton(MovePlayerLeftbtnTexture)
                {
                    X = Player1PersonBtnBack.X - 18,
                    Y = Player1PersonBtnBack.Y - 1,
                    Visible = false
                },
                new UIButton(MovePlayerLeftbtnTexture)
                {
                    X = Player2PersonBtnBack.X - 18,
                    Y = Player2PersonBtnBack.Y - 1
                },
                new UIButton(MovePlayerLeftbtnTexture)
                {
                    X = Player3PersonBtnBack.X - 18,
                    Y = Player3PersonBtnBack.Y - 1
                },
                new UIButton(MovePlayerLeftbtnTexture)
                {
                    X = Player4PersonBtnBack.X - 18,
                    Y = Player4PersonBtnBack.Y - 1
                },
            };
            MovePlayerRightbtn = new UIButton[4]
            {
                new UIButton(MovePlayerRightbtnTexture)
                {
                    X = Player1PersonBtnBack.X + 35,
                    Y = Player1PersonBtnBack.Y - 1
                },
                new UIButton(MovePlayerRightbtnTexture)
                {
                    X = Player2PersonBtnBack.X + 35,
                    Y = Player2PersonBtnBack.Y - 1
                },
                new UIButton(MovePlayerRightbtnTexture)
                {
                    X = Player3PersonBtnBack.X + 35,
                    Y = Player3PersonBtnBack.Y - 1
                },
                new UIButton(MovePlayerRightbtnTexture)
                {
                    X = Player4PersonBtnBack.X + 35,
                    Y = Player4PersonBtnBack.Y - 1,
                    Visible = false
                }
            };
            // listeners
            for (var i = 0; i < 4; i++)
            {
                var left = MovePlayerLeftbtn[i];
               Stage.Add(left);
                left.OnButtonClick += (element) => { PlayerMoveLeftbtnClicked(i); };

                var right = MovePlayerRightbtn[i];
               Stage.Add(right);
                right.OnButtonClick += (element) => { PlayerMoveRightbtnClicked(i); };

                var enable = EnablePlayer[i];
               Stage.Add(enable);
                enable.OnButtonClick += (element) => { EnablePlayerbtnClicked(i); };
            }

            /*
             * find new player buttons and backs
             */
            var findNewPlayerbtnBacks = new UIImage[4]
            {
                new UIImage(ButtonSeatTexture)
                {X = Player1PersonBtnBack.X - 1,
                Y = Player1PersonBtnBack.Y + 32,
                ScaleX = 0.72f,
                ScaleY = 0.72f },
                new UIImage(ButtonSeatTexture)
                {X = Player2PersonBtnBack.X - 1,
                Y = Player2PersonBtnBack.Y + 32,
                ScaleX = 0.72f,
                ScaleY = 0.72f },
                new UIImage(ButtonSeatTexture)
                {X = Player3PersonBtnBack.X - 1,
                Y = Player3PersonBtnBack.Y + 32,
                ScaleX = 0.72f,
                ScaleY = 0.72f },
                new UIImage(ButtonSeatTexture)
                {X = Player4PersonBtnBack.X - 1,
                Y = Player4PersonBtnBack.Y + 32,
                ScaleX = 0.72f,
                ScaleY = 0.72f }
            };
            FindNewPlayerbtn = new UIButton[4];
            for (var i = 0; i < findNewPlayerbtnBacks.Length; i++)
            {
                var back = findNewPlayerbtnBacks[i];
               Stage.Add(back);
                var btn = FindNewPlayerbtn[i] = new UIButton(FindNewPlayerbtnTexture)
                {
                    X = back.X + 2,
                    Y = back.Y + 2,
                    ScaleX = 0.80f,
                    ScaleY = 0.80f,
                    Tooltip = "Find new player"
                };
               Stage.Add(btn);
                btn.OnButtonClick += (element) => { FindNewPlayerbtnClicked(i); };
            }

            /*
             * Correct buttons and backs
             */
            var playerCorrectbtnBack = new UIImage[4]
            {
                new UIImage(ButtonSeatTexture)
                {
                    X = Player1PersonBtnBack.X - 20,
                    Y = Player1PersonBtnBack.Y + 60,
                    ScaleX = 0.88f,
                    ScaleY = 0.88f
                },
                new UIImage(ButtonSeatTexture)
                {
                    X = Player2PersonBtnBack.X - 20,
                    Y = Player2PersonBtnBack.Y + 60,
                    ScaleX = 0.88f,
                    ScaleY = 0.88f
                },
                new UIImage(ButtonSeatTexture)
                {
                    X = Player3PersonBtnBack.X - 20,
                    Y = Player3PersonBtnBack.Y + 60,
                    ScaleX = 0.88f,
                    ScaleY = 0.88f
                },
                new UIImage(ButtonSeatTexture)
                {
                    X = Player4PersonBtnBack.X - 20,
                    Y = Player4PersonBtnBack.Y + 60,
                    ScaleX = 0.88f,
                    ScaleY = 0.88f
                }
            };
            PlayerCorrectbtn = new UIButton[4];
            for (var i = 0; i < playerCorrectbtnBack.Length; i++)
            {
                var back = playerCorrectbtnBack[i];
               Stage.Add(back);
                var btn = PlayerCorrectbtn[i] = new UIButton(PlayerCorrectbtnTexture)
                {
                    X = back.X + 2,
                    Y = back.Y + 2,
                    Tooltip = "Correct"
                };
               Stage.Add(btn);
                btn.OnButtonClick += (element) => { PlayerCorrectbtnClicked(i); };
            }

            // Incorrect buttons and backs
            var playerIncorrectbtnBack = new UIImage[4]
            {
                new UIImage(ButtonSeatTexture)
                {
                    X = Player1PersonBtnBack.X + 15,
                    Y = Player1PersonBtnBack.Y + 60,
                    ScaleX = 0.88f,
                    ScaleY = 0.88f
                },
                new UIImage(ButtonSeatTexture)
                {
                    X = Player2PersonBtnBack.X + 15,
                    Y = Player2PersonBtnBack.Y + 60,
                    ScaleX = 0.88f,
                    ScaleY = 0.88f
                },
                new UIImage(ButtonSeatTexture)
                {
                    X = Player3PersonBtnBack.X + 15,
                    Y = Player3PersonBtnBack.Y + 60,
                    ScaleX = 0.88f,
                    ScaleY = 0.88f
                },
                new UIImage(ButtonSeatTexture)
                {
                    X = Player4PersonBtnBack.X + 15,
                    Y = Player4PersonBtnBack.Y + 60,
                    ScaleX = 0.88f,
                    ScaleY = 0.88f
                }
            };
            PlayerIncorrectbtn = new UIButton[4];
            for (var i = 0; i < playerIncorrectbtnBack.Length; i++)
            {
                var back = playerIncorrectbtnBack[i];
               Stage.Add(back);
                var btn = PlayerIncorrectbtn[i] = new UIButton(PlayerIncorrectbtnTexture)
                {
                    X = back.X + 2,
                    Y = back.Y + 2,
                    ScaleX = 0.90f,
                    ScaleY = 0.90f,
                    Tooltip = "Incorrect"
                };
               Stage.Add(btn);
                btn.OnButtonClick += (element) => { PlayerIncorrectbtnClicked(i); };
            }

            // score backs and score texts
            var playerScoreBack = new UIImage[]
            {
                new UIImage(PlayerScoreBackTexture)
                {
                    X = Player1PersonBtnBack.X - 25,
                    Y = Player1PersonBtnBack.Y + 100
                },
                new UIImage(PlayerScoreBackTexture)
                {
                    X = Player2PersonBtnBack.X - 25,
                    Y = Player2PersonBtnBack.Y + 100
                },
                new UIImage(PlayerScoreBackTexture)
                {
                    X = Player3PersonBtnBack.X - 25,
                    Y = Player3PersonBtnBack.Y + 100
                },
                new UIImage(PlayerScoreBackTexture)
                {
                    X = Player4PersonBtnBack.X - 25,
                    Y = Player4PersonBtnBack.Y + 100
                },
            };
            PlayerScores = new UITextEdit[4];
            for (var i = 0; i < playerScoreBack.Length; i++)
            {
                var back = playerScoreBack[i];
                Stage.Add(back);
                var score = PlayerScores[i] = new UITextEdit()
                {
                    X = back.X + 6,
                    Y = back.Y + 6,
                    CurrentText = "9999",
                    MaxChars = 5,
                    MaxLines = 1,
                    Size = new Vector2(60, 20),
                    Mode = UITextEditMode.Editor,
                    Alignment = TextAlignment.Center,
                    Tooltip = "Player Score"
                };
                Stage.Add(score);

                /*
                 * More options
                 */
                MoreOptionsPanel = new UIContainer();

                EODMoreOptionsBack.Position = new Vector2(10, 96);
                MoreOptionsPanel.Add(EODMoreOptionsBack);

                MoreOptionsTitle = new UITextEdit()
                {
                    X = 175,
                    Y = 98,
                    CurrentText = "MORE OPTIONS",
                    Alignment = TextAlignment.Center,
                    Mode = UITextEditMode.ReadOnly,
                    Size = new Vector2(120, 20)
                };
                MoreOptionsPanel.Add(MoreOptionsTitle);

                SetAnswerTimeLabel = new UITextEdit()
                {
                    X = 55,
                    Y = 164,
                    CurrentText = "Set Answer Time",
                    Alignment = TextAlignment.Right,
                    Mode = UITextEditMode.ReadOnly,
                    Size = new Vector2(200, 20)
                };
                MoreOptionsPanel.Add(SetAnswerTimeLabel);
                SetAnswerTimeBack = new UIImage(SetTimeBackTexture)
                {
                    X = 265,
                    Y = SetAnswerTimeLabel.Y + 1
                };
                MoreOptionsPanel.Add(SetAnswerTimeBack);
                SetAnswerTimeTextEdit = new UITextEdit()
                {
                    X = SetAnswerTimeBack.X - 3,
                    Y = SetAnswerTimeBack.Y,
                    MaxLines = 1,
                    MaxChars = 3,
                    Mode = UITextEditMode.Editor,
                    Size = new Vector2(60, 20),
                    Alignment = TextAlignment.Center,
                    Tooltip = "Seconds to answer",
                    CurrentText = "10"
                };
                MoreOptionsPanel.Add(SetAnswerTimeTextEdit);

                SetBuzzerTimeLabel = new UITextEdit()
                {
                    X = 55,
                    Y = 124,
                    CurrentText = "Set Buzzer Time",
                    Alignment = TextAlignment.Right,
                    Mode = UITextEditMode.ReadOnly,
                    Size = new Vector2(200, 20)
                };
                MoreOptionsPanel.Add(SetBuzzerTimeLabel);
                SetBuzzerTimeBack = new UIImage(SetTimeBackTexture)
                {
                    X = 265,
                    Y = SetBuzzerTimeLabel.Y + 1
                };
                MoreOptionsPanel.Add(SetBuzzerTimeBack);
                SetBuzzerTimeTextEdit = new UITextEdit()
                {
                    X = SetBuzzerTimeBack.X - 2,
                    Y = SetBuzzerTimeBack.Y,
                    MaxLines = 1,
                    MaxChars = 3,
                    Mode = UITextEditMode.Editor,
                    Size = new Vector2(60, 20),
                    Alignment = TextAlignment.Center,
                    Tooltip = "Seconds to buzz",
                    CurrentText = "12"
                };
                MoreOptionsPanel.Add(SetBuzzerTimeTextEdit);

                AutoDeductWrongPointsLabel = new UITextEdit()
                {
                    X = 57, // 80?
                    Y = 204,
                    CurrentText = "Deduct Points on Wrong Answer",
                    Alignment = TextAlignment.Right,
                    Mode = UITextEditMode.ReadOnly,
                    Size = new Vector2(200, 20)
                };
                MoreOptionsPanel.Add(AutoDeductWrongPointsLabel);
                AutoDeductWrongPointsbtn = new UIButton(CheckboxbtnTexture)
                {
                    X = SetBuzzerTimeTextEdit.X + 21,
                    Y = AutoDeductWrongPointsLabel.Y,
                    Selected = false
                };
                MoreOptionsPanel.Add(AutoDeductWrongPointsbtn);

                AllowNegativeScoresLabel = new UITextEdit()
                {
                    X = 53,
                    Y = 244,
                    CurrentText = "Allow Negative Points",
                    Alignment = TextAlignment.Right,
                    Mode = UITextEditMode.ReadOnly,
                    Size = new Vector2(200, 20)
                };
                MoreOptionsPanel.Add(AllowNegativeScoresLabel);
                AllowNegativeScoresbtn = new UIButton(CheckboxbtnTexture)
                {
                    X = SetBuzzerTimeTextEdit.X + 21,
                    Y = AllowNegativeScoresLabel.Y,
                    Selected = false
                };
                MoreOptionsPanel.Add(AllowNegativeScoresbtn);

                AutoDisableOnWrongLabel = new UITextEdit()
                {
                    X = 55,
                    Y = 284,
                    CurrentText = "Disable Buzzer on Wrong Answer",
                    Alignment = TextAlignment.Right,
                    Mode = UITextEditMode.ReadOnly,
                    Size = new Vector2(200, 20)
                };
                MoreOptionsPanel.Add(AutoDisableOnWrongLabel);
                AutoDisableOnWrongbtn = new UIButton(CheckboxbtnTexture)
                {
                    X = SetBuzzerTimeTextEdit.X + 21,
                    Y = AutoDisableOnWrongLabel.Y,
                    Selected = true
                };
                MoreOptionsPanel.Add(AutoDisableOnWrongbtn);

                AutoEnableAllOnRightLabel = new UITextEdit()
                {
                    X = 55,
                    Y = 324,
                    CurrentText = "Enable All Buzzers on Right Answer",
                    Alignment = TextAlignment.Right,
                    Mode = UITextEditMode.ReadOnly,
                    Size = new Vector2(200, 20)
                };
                MoreOptionsPanel.Add(AutoEnableAllOnRightLabel);
                AutoEnableAllOnRightbtn = new UIButton(CheckboxbtnTexture)
                {
                    X = SetBuzzerTimeTextEdit.X + 21,
                    Y = AutoEnableAllOnRightLabel.Y,
                    Selected = true
                };
                MoreOptionsPanel.Add(AutoEnableAllOnRightbtn);

                GoBackLabel = new UITextEdit()
                {
                    Position = OptionsButtonTextEdit.Position,
                    CurrentText = "Go back",
                    Mode = UITextEditMode.ReadOnly,
                    Size = new Vector2(100, 20),
                    Alignment = TextAlignment.Left
                };
                MoreOptionsPanel.Add(GoBackLabel);
                GoBackbtnBack = new UIImage(ButtonSeatTexture)
                {
                    X = 363,
                    Y = 122,
                    ScaleX = 0.65f,
                    ScaleY = 0.65f
                };
                MoreOptionsPanel.Add(optionsbtnBack);
                GoBackbtn = new UIButton(checkbtnTex)
                {
                    Position = Optionsbtn.Position,
                    ScaleX = 0.60f,
                    ScaleY = 0.60f
                };
                MoreOptionsPanel.Add(GoBackbtn);
                GoBackbtn.OnButtonClick += GoBackbtnClicked;
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    internal class ContestantLightsFrame : UIContainer
    {
        private bool TexturesValid;
        private UIImage Lights1 = new UIImage(); 
        private UIImage Lights2 = new UIImage();
        private UIImage LightsBack = new UIImage();
        private UIImage LightsBlue = new UIImage(); 
        private UIImage LightsRed = new UIImage();
        private System.Timers.Timer FlashTimer;

        internal ContestantLightsFrame(Texture2D light1, Texture2D light2, Texture2D back, Texture2D blue, Texture2D red)
        {
            FlashTimer = new System.Timers.Timer(66);
            if (light1 != null)
            {
                TexturesValid = true;
                LightsBack = new UIImage(back);
               Add(LightsBack);
                Lights1 = new UIImage(light1);
               Add(Lights1);
                Lights2 = new UIImage(light2);
               Add(Lights2);
                LightsBlue = new UIImage(blue);
               Add(LightsBlue);
                LightsRed = new UIImage(red);
               Add(LightsRed);
                FlashTimer.Elapsed += (source, args) => { Lights1.Visible = !Lights1.Visible; Lights2.Visible = !Lights2.Visible; };
                Red();
            }
        }
        internal void Flash()
        {
            LightsRed.Visible = false;
            LightsBlue.Visible = false;
            Lights1.Visible = true;
            Lights2.Visible = false;
            if (TexturesValid)
                FlashTimer.Start();
        }
        internal void Blue()
        {
            FlashTimer.Stop();
            LightsRed.Visible = false;
            LightsBlue.Visible = true;
            Lights1.Visible = false;
            Lights2.Visible = false;
        }
        internal void Red()
        {
            FlashTimer.Stop();
            LightsRed.Visible = true;
            LightsBlue.Visible = false;
            Lights1.Visible = false;
            Lights2.Visible = false;
        }
    }
}
