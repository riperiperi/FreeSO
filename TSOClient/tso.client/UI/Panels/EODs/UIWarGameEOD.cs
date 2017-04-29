using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Client.UI.Controls;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay.EODs.Handlers;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIWarGameEOD : UIEOD
    {
        // parser
        public UIScript Script;

        // buttons
        private UIButton ButtonToDisable;
        private UIButton ArtilleryButton { get; set; }
        private UIButton CavalryButton { get; set; }
        private UIButton CommandButton { get; set; }
        private UIButton IntelButton { get; set; }
        private UIButton InfantryButton { get; set; }

        // images
        private UIImage DefeatImageToShow;
        public UIImage MoveSelectionBkgnd;
        public UIImage ResultsBkgnd;
        public UIImage ResultVictoryImg;
        public UIImage ResultDefeatImg;
        public UIImage BluePlayerPos;
        public UIImage RedPlayerPos;
        public UIImage BlueMoveBorder;
        public UIImage RedMoveBorder;
        public UIImage DefeatArtilleryPos;
        public UIImage DefeatCavalryPos;
        public UIImage DefeatCommandPos;
        public UIImage DefeatInfantryPos;
        public UIImage DefeatIntelPos;
        public UIImage ProgPosition1;
        public UIImage ProgPosition2;
        public UIImage ProgPosition3;
        public UIImage ProgPosition4;
        public UIImage ProgPosition5;
        public UIImage ProgPosition6;

        // special iamges with special bounds
        public UISlotsImage MoveIconBlue;
        public UISlotsImage MoveIconRed;
        public UISlotsImage ProgressBarBlue;
        public UISlotsImage ProgressBarRed;

        // Textures
        public Texture2D MoveIconImage { get; set; }
        public Texture2D BlueProgressImage { get; set; }
        public Texture2D RedProgressImage { get; set; }

        private bool PlayerIsBlue;
        private int ProgressBeginningX;
        private int ProgressY;
        private byte RemainingBluePieces;
        private byte RemainingRedPieces;

        public const string TIE_ROUND_MESSAGE = "Phew! This round is a draw...";
        public const string TIE_ROUND_TITLE = "Draw";
        public const string TIE_GAME_MESSAGE = "Oh no! The game is a stalemate!";
        public const string TIE_GAME_TITLE = "Stalemate";

        // player pictures
        public UIVMPersonButton[] Players = new UIVMPersonButton[2];

        // constants
        public const int MOVE_ICON_WIDTH_HEIGHT = 24;

        public UIWarGameEOD(UIEODController controller) : base(controller)
        {
            BuildUI();
            PlaintextHandlers["WarGame_Init"] = InitHandler;
            PlaintextHandlers["WarGame_Reset"] = ResetHandler;
            PlaintextHandlers["WarGame_Draw_Opponent"] = DrawOpponentHandler;
            BinaryHandlers["WarGame_Resume"] = ResumeHandler;
            BinaryHandlers["WarGame_Victory"] = VictoryHandler;
            BinaryHandlers["WarGame_Defeat"] = DefeatHandler;
            BinaryHandlers["WarGame_Tie"] = TieHandler;
            BinaryHandlers["WarGame_Stalemate"] = StalemateHandler;
        }
        public override void OnClose()
        {
            if (PlayerIsBlue)
            {
                try
                {
                    Remove(Players[0]);
                }
                catch (Exception) { }
                Players[0] = null;
            }
            else
            {
                try
                {
                    Remove(Players[1]);
                }
                catch (Exception) { }
                Players[1] = null;
            }
            Send("WarGame_Close_UI", "");
            base.OnClose();
        }
        private void InitHandler(string evt, string data)
        {
            var split = data.Split('%');
            short avatarID;
            // place player Avatar into Players
            if (Int16.TryParse(split[0], out avatarID))
            {
                var avatar = (VMAvatar)Controller.Lot.vm.GetObjectById(avatarID);
                if (split[1].Equals("blue"))
                {
                    Players[0] = new UIVMPersonButton((VMAvatar)avatar, Controller.Lot.vm, false);
                    Players[0].Position = BluePlayerPos.Position;
                    PlayerIsBlue = true;
                    Add(Players[0]);
                }
                else
                {
                    Players[1] = new UIVMPersonButton((VMAvatar)avatar, Controller.Lot.vm, false);
                    Players[1].Position = RedPlayerPos.Position;
                    Add(Players[1]);
                }
            }
            // draw progressbar
            RemainingBluePieces = 5;
            RemainingRedPieces = 5;
            DrawProgressBar(RemainingBluePieces, RemainingRedPieces);

            // add the buttons as children to this, creating a new one if need be
            ArtilleryButton = Script.Create<UIButton>("ArtilleryButton");
            if (Children.Contains(ArtilleryButton))
                Remove(ArtilleryButton);
            Add(ArtilleryButton);
            CavalryButton = Script.Create<UIButton>("CavalryButton");
            if (Children.Contains(CavalryButton))
                Remove(CavalryButton);
            Add(CavalryButton);
            CommandButton = Script.Create<UIButton>("CommandButton");
            if (Children.Contains(CommandButton))
                Remove(CommandButton);
            Add(CommandButton);
            InfantryButton = Script.Create<UIButton>("InfantryButton");
            if (Children.Contains(InfantryButton))
                Remove(InfantryButton);
            Add(InfantryButton);
            IntelButton = Script.Create<UIButton>("IntelButton");
            if (Children.Contains(IntelButton))
                Remove(IntelButton);
            Add(IntelButton);

            // add defeated images beneath each button
            DefeatArtilleryPos = Script.Create<UIImage>("DefeatArtilleryPos");
            AddBefore(DefeatArtilleryPos, ArtilleryButton);
            DefeatArtilleryPos.Visible = false;
            DefeatCavalryPos = Script.Create<UIImage>("DefeatCavalryPos");
            AddBefore(DefeatCavalryPos, CavalryButton);
            DefeatCavalryPos.Visible = false;
            DefeatCommandPos = Script.Create<UIImage>("DefeatCommandPos");
            AddBefore(DefeatCommandPos, CommandButton);
            DefeatCommandPos.Visible = false;
            DefeatInfantryPos = Script.Create<UIImage>("DefeatInfantryPos");
            AddBefore(DefeatInfantryPos, InfantryButton);
            DefeatInfantryPos.Visible = false;
            DefeatIntelPos = Script.Create<UIImage>("DefeatIntelPos");
            AddBefore(DefeatIntelPos, IntelButton);
            DefeatIntelPos.Visible = false;

            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Tips = EODTextTips.None,
                Timer = EODTimer.None,
            });
        }
        private void DrawOpponentHandler(string evt, string opponentID)
        {
            // remove old opponent picture
            if (PlayerIsBlue)
            {
                if (Players[1] != null)
                {
                    Remove(Players[1]);
                    Players[1] = null;
                }
            }
            else
            {
                if (Players[0] != null)
                {
                    Remove(Players[0]);
                    Players[0] = null;
                }
            }
            short avatarID;
            if (Int16.TryParse(opponentID, out avatarID))
            {
                var avatar = (VMAvatar)Controller.Lot.vm.GetObjectById(avatarID);
                if (PlayerIsBlue)
                {
                    Players[1] = new UIVMPersonButton(avatar, Controller.Lot.vm, false);
                    Players[1].Position = RedPlayerPos.Position;
                    Add(Players[1]);
                }
                else
                {
                    Players[0] = new UIVMPersonButton(avatar, Controller.Lot.vm, false);
                    Players[0].Position = BluePlayerPos.Position;
                    Add(Players[0]);
                }
            }
        }
        private void ResetHandler(string evt, string msg)
        {
            // set progress image
            RemainingBluePieces = 5;
            RemainingRedPieces = 5;
            DrawProgressBar(RemainingBluePieces, RemainingRedPieces);

            RemoveListeners();

            // show and enable all buttons
            ArtilleryButton.Visible = true;
            ArtilleryButton.Disabled = false;
            CavalryButton.Visible = true;
            CavalryButton.Disabled = false;
            CommandButton.Visible = true;
            CommandButton.Disabled = false;
            IntelButton.Visible = true;
            IntelButton.Disabled = false;
            InfantryButton.Visible = true;
            InfantryButton.Disabled = false;

            // make defeat images invisible
            DefeatArtilleryPos.Visible = false;
            DefeatCavalryPos.Visible = false;
            DefeatCommandPos.Visible = false;
            DefeatInfantryPos.Visible = false;
            DefeatIntelPos.Visible = false;

            // set move choice icons to Unknown
            SetPlayerIcon((byte)UIWarGameEODMoveChoices.Unknown);
            SetOpponentIcon((byte)UIWarGameEODMoveChoices.Unknown);
        }
        private void ResumeHandler(string evt, Byte[] remainingPieces)
        {
            // sync remaining pieces
            RemainingBluePieces = remainingPieces[0];
            RemainingRedPieces = remainingPieces[1];
            DrawProgressBar(RemainingBluePieces, RemainingRedPieces);

            // hide prior results
            ResultDefeatImg.Visible = false;
            ResultVictoryImg.Visible = false;

            // set move choice icons to Unknown
            SetPlayerIcon((byte)UIWarGameEODMoveChoices.Unknown);
            SetOpponentIcon((byte)UIWarGameEODMoveChoices.Unknown);
            AddListeners();
        }
        private void ArtilleryButtonClickedHandler(UIElement target)
        {
            RemoveListeners();
            Send("WarGame_Piece_Selection", new Byte[] { (byte) VMEODWarGamePieceTypes.Artillery });
            SetPlayerIcon(GetIconByteFromPieceTypeByte((byte)VMEODWarGamePieceTypes.Artillery));
        }
        private void CavalryButtonClickedHandler(UIElement target)
        {
            RemoveListeners();
            Send("WarGame_Piece_Selection", new Byte[] { (byte)VMEODWarGamePieceTypes.Cavalry });
            SetPlayerIcon(GetIconByteFromPieceTypeByte((byte)VMEODWarGamePieceTypes.Cavalry));
        }
        private void CommandButtonClickedHandler(UIElement target)
        {
            RemoveListeners();
            Send("WarGame_Piece_Selection", new Byte[] { (byte)VMEODWarGamePieceTypes.Command });
            SetPlayerIcon(GetIconByteFromPieceTypeByte((byte)VMEODWarGamePieceTypes.Command));
        }
        private void IntelButtonClickedHandler(UIElement target)
        {
            RemoveListeners();
            Send("WarGame_Piece_Selection", new Byte[] { (byte)VMEODWarGamePieceTypes.Intelligence });
            SetPlayerIcon(GetIconByteFromPieceTypeByte((byte)VMEODWarGamePieceTypes.Intelligence));
        }
        private void InfantryButtonClickedHandler(UIElement target)
        {
            RemoveListeners();
            Send("WarGame_Piece_Selection", new Byte[] { (byte)VMEODWarGamePieceTypes.Infantry });
            SetPlayerIcon(GetIconByteFromPieceTypeByte((byte)VMEODWarGamePieceTypes.Infantry));
        }
        private void TieHandler(string evt, byte[] piecesPlayed)
        {
            SetOpponentIcon(piecesPlayed);
            // Alert: The round is a tie
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = UIWarGameEOD.TIE_ROUND_TITLE,
                Message = UIWarGameEOD.TIE_ROUND_MESSAGE,
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    UIScreen.RemoveDialog(alert);
                }),

            }, true);
        }
        private void StalemateHandler(string evt, byte[] remainingPieces)
        {
            SetOpponentIcon(remainingPieces[0]);
            // Alert: The game is a stalemate
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = UIWarGameEOD.TIE_GAME_TITLE,
                Message = UIWarGameEOD.TIE_GAME_MESSAGE,
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    UIScreen.RemoveDialog(alert);
                }),

            }, true);
        }
        private void DefeatHandler(string evt, byte[] piecesPlayed)
        {
            // show defeat image
            ResultDefeatImg.Visible = true;

            // find the defeated piece
            byte myPiecePlayed = 0;
            if (PlayerIsBlue)
            {
                myPiecePlayed = piecesPlayed[0];
                RemainingBluePieces--;
            }
            else
            {
                myPiecePlayed = piecesPlayed[1];
                RemainingRedPieces--;
            }

            // draw progress bar and opponent's chosen piece
            DrawProgressBar(RemainingBluePieces, RemainingRedPieces);
            SetOpponentIcon(piecesPlayed);

            // mark the button matching the played piece to be removed
            if (myPiecePlayed == (byte)VMEODWarGamePieceTypes.Artillery)
            {
                ButtonToDisable = ArtilleryButton;
                DefeatImageToShow = DefeatArtilleryPos;
            }
            else if (myPiecePlayed == (byte)VMEODWarGamePieceTypes.Cavalry)
            {
                ButtonToDisable = CavalryButton;
                DefeatImageToShow = DefeatCavalryPos;
            }
            else if (myPiecePlayed == (byte)VMEODWarGamePieceTypes.Command)
            {
                ButtonToDisable = CommandButton;
                DefeatImageToShow = DefeatCommandPos;
            }
            else if (myPiecePlayed == (byte)VMEODWarGamePieceTypes.Infantry)
            {
                ButtonToDisable = InfantryButton;
                DefeatImageToShow = DefeatInfantryPos;
            }
            else // if (myPiecePlayed == (byte)VMEODWarGamePieceTypes.Intelligence)
            {
                ButtonToDisable = IntelButton;
                DefeatImageToShow = DefeatIntelPos;
            }

            // remove a button if this player lost last round
            if (ButtonToDisable != null)
            {
                ButtonToDisable.Disabled = true;
                ButtonToDisable.Visible = false;
                DefeatImageToShow.Visible = true;
                DefeatImageToShow = null;
                ButtonToDisable = null;
            }
        }
        private void VictoryHandler(string evt, byte[] piecesPlayed)
        {
            if (PlayerIsBlue)
            {
                RemainingRedPieces--;
            }
            else
            {
                RemainingBluePieces--;
            }

            // draw progress bar and opponent's chosen piece
            DrawProgressBar(RemainingBluePieces, RemainingRedPieces);
            SetOpponentIcon(piecesPlayed);
            
            // show victory image
            ResultVictoryImg.Visible = true;
        }
        private void SetPlayerIcon(byte iconNumber)
        {
            if (PlayerIsBlue)
                MoveIconBlue.SetBounds(iconNumber * MOVE_ICON_WIDTH_HEIGHT,
                    0, MOVE_ICON_WIDTH_HEIGHT, MOVE_ICON_WIDTH_HEIGHT);
            else
                MoveIconRed.SetBounds(iconNumber * MOVE_ICON_WIDTH_HEIGHT,
                    0, MOVE_ICON_WIDTH_HEIGHT, MOVE_ICON_WIDTH_HEIGHT);
        }
        private void SetOpponentIcon(byte iconNumber)
        {
            if (!PlayerIsBlue)
                MoveIconBlue.SetBounds(iconNumber * MOVE_ICON_WIDTH_HEIGHT,
                    0, MOVE_ICON_WIDTH_HEIGHT, MOVE_ICON_WIDTH_HEIGHT);
            else
                MoveIconRed.SetBounds(iconNumber * MOVE_ICON_WIDTH_HEIGHT,
                    0, MOVE_ICON_WIDTH_HEIGHT, MOVE_ICON_WIDTH_HEIGHT);
        }
        private void SetOpponentIcon(byte[] piecesPlayed)
        {
            byte moveIcon = 0;
            if (PlayerIsBlue)
            {
                moveIcon = GetIconByteFromPieceTypeByte(piecesPlayed[1]);
                MoveIconRed.SetBounds(moveIcon * MOVE_ICON_WIDTH_HEIGHT, 0,
                    MOVE_ICON_WIDTH_HEIGHT, MOVE_ICON_WIDTH_HEIGHT);
            }
            else
            {
                moveIcon = GetIconByteFromPieceTypeByte(piecesPlayed[0]);
                MoveIconBlue.SetBounds(moveIcon * MOVE_ICON_WIDTH_HEIGHT, 0,
                    MOVE_ICON_WIDTH_HEIGHT, MOVE_ICON_WIDTH_HEIGHT);
            }
        }
        private void BuildUI()
        {
            var script = this.RenderScript("wargameeod.uis");
            Script = script;

            // add backgrounds
            MoveSelectionBkgnd = script.Create<UIImage>("MoveSelectionBkgnd");
            AddAt(0, MoveSelectionBkgnd);
            ResultsBkgnd = script.Create<UIImage>("ResultsBkgnd");
            AddAt(1, ResultsBkgnd);

            // add and make invisible the victory and defeat images
            ResultVictoryImg = script.Create<UIImage>("ResultVictoryImg");
            AddAt(2, ResultVictoryImg);
            ResultVictoryImg.Visible = false;
            ResultDefeatImg = script.Create<UIImage>("ResultDefeatImg");
            AddAt(3, ResultDefeatImg);
            ResultDefeatImg.Visible = false;

            // set player choice image
            MoveIconBlue = new UISlotsImage(MoveIconImage);
            MoveIconBlue.X = 215;
            MoveIconBlue.SetBounds(((byte)UIWarGameEODMoveChoices.Unknown * MOVE_ICON_WIDTH_HEIGHT), 0,
                MOVE_ICON_WIDTH_HEIGHT, MOVE_ICON_WIDTH_HEIGHT);
            MoveIconRed = new UISlotsImage(MoveIconImage);
            MoveIconRed.X = 285;
            MoveIconRed.SetBounds(((byte)UIWarGameEODMoveChoices.Unknown * MOVE_ICON_WIDTH_HEIGHT), 0,
                MOVE_ICON_WIDTH_HEIGHT, MOVE_ICON_WIDTH_HEIGHT);
            MoveIconBlue.Y = MoveIconRed.Y = 124;
            Add(MoveIconBlue);
            Add(MoveIconRed);

            // remove the initial buttons, they needed to be added in InitHandler
            try
            {
                Remove(ArtilleryButton);
                Remove(CavalryButton);
                Remove(CommandButton);
                Remove(InfantryButton);
                Remove(IntelButton);
            }
            catch (Exception) { }

            // add borders for player images
            BlueMoveBorder = script.Create<UIImage>("BlueMoveBorder");
            Add(BlueMoveBorder);
            RedMoveBorder = script.Create<UIImage>("RedMoveBorder");
            Add(RedMoveBorder);

            // add player images
            BluePlayerPos = script.Create<UIImage>("BluePlayerPos");
            RedPlayerPos = script.Create<UIImage>("RedPlayerPos");

            // get beginning progressbar.X and its Y
            ProgPosition1 = script.Create<UIImage>("ProgPosition1");
            ProgressBeginningX = (int)ProgPosition1.X;
            ProgressY = (int)ProgPosition1.Y;
        }
        private byte GetIconByteFromPieceTypeByte(byte pieceType)
        {
            byte moveIcon = 6;
            if (pieceType == (byte)VMEODWarGamePieceTypes.Artillery)
            {
                moveIcon = (byte)UIWarGameEODMoveChoices.Artillery;
            }
            else if (pieceType == (byte)VMEODWarGamePieceTypes.Cavalry)
            {
                moveIcon = (byte)UIWarGameEODMoveChoices.Cavalry;
            }
            else if (pieceType == (byte)VMEODWarGamePieceTypes.Command)
            {
                moveIcon = (byte)UIWarGameEODMoveChoices.Command;
            }
            else if (pieceType == (byte)VMEODWarGamePieceTypes.Infantry)
            {
                moveIcon = (byte)UIWarGameEODMoveChoices.Infantry;
            }
            else //if (pieceType == (byte)VMEODWarGamePieceTypes.Intelligence)
            {
                moveIcon = (byte)UIWarGameEODMoveChoices.Intelligence;
            }
            return moveIcon;
        }
        private void AddListeners()
        {
            if (ArtilleryButton.Visible == true)
            {
                ArtilleryButton.OnButtonClick += ArtilleryButtonClickedHandler;
            }
            if (CavalryButton.Visible == true)
            {
                CavalryButton.OnButtonClick += CavalryButtonClickedHandler;
            }
            if (CommandButton.Visible == true)
            {
                CommandButton.OnButtonClick += CommandButtonClickedHandler;
            }
            if (IntelButton.Visible == true)
            {
                IntelButton.OnButtonClick += IntelButtonClickedHandler;
            }
            if (InfantryButton.Visible == true)
            {
                InfantryButton.OnButtonClick += InfantryButtonClickedHandler;
            }
        }
        private void RemoveListeners()
        {
            if (ArtilleryButton.Visible == true)
                ArtilleryButton.OnButtonClick -= ArtilleryButtonClickedHandler;
            if (CavalryButton.Visible == true)
                CavalryButton.OnButtonClick -= CavalryButtonClickedHandler;
            if (CommandButton.Visible == true)
                CommandButton.OnButtonClick -= CommandButtonClickedHandler;
            if (IntelButton.Visible == true)
                IntelButton.OnButtonClick -= IntelButtonClickedHandler;
            if (InfantryButton.Visible == true)
                InfantryButton.OnButtonClick -= InfantryButtonClickedHandler;
        }
        private void DrawProgressBar(byte remainingBluePieces, byte remainingRedPieces)
        {
            // remove old image
            if (ProgressBarBlue != null)
                Remove(ProgressBarBlue);
            if (ProgressBarRed != null)
                Remove(ProgressBarRed);

            // calculate new blue draw width based on ratio of number of remaining blue pieces to the total nubmer of remaining pieces
            var totalPieces = remainingBluePieces + remainingRedPieces;
            float blueRatio = (0F + remainingBluePieces) / totalPieces;
            int thisRoundBlueProgress = (int)(blueRatio * BlueProgressImage.Width);

            // draw blue bar
            ProgressBarBlue = new UISlotsImage(BlueProgressImage);
            ProgressBarBlue.SetBounds(0, 0, thisRoundBlueProgress, BlueProgressImage.Height);
            ProgressBarBlue.X = ProgressBeginningX;

            // draw red bar based on remaining draw space
            ProgressBarRed = new UISlotsImage(RedProgressImage);
            ProgressBarRed.SetBounds(thisRoundBlueProgress, 0, (RedProgressImage.Width - thisRoundBlueProgress), RedProgressImage.Height);
            ProgressBarRed.X = ProgressBeginningX + thisRoundBlueProgress;

            ProgressBarBlue.Y = ProgressBarRed.Y = ProgressY;
            Add(ProgressBarBlue);
            Add(ProgressBarRed);
        }
    }
    public enum UIWarGameEODMoveChoices : byte
    {
        Unknown = 0,
        Artillery = 1,
        Cavalry = 2,
        Command = 3,
        Intelligence = 4,
        Infantry = 5
    }
}