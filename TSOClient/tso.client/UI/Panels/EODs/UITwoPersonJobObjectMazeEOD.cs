using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Content.Model;
using FSO.HIT;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.SimAntics.NetPlay.EODs.Utils;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels.EODs
{
    public class UITwoPersonJobObjectMazeEOD : UIEOD
    {
        private UIScript Script;
        private Random Rand;
        private UIAlert Alert;

        // script stuff
        public string NorthString { get; set; }
        public string SouthString { get; set; }
        public string EastString { get; set; }
        public string WestString { get; set; }
        public string WaitingMessage { get; set; }
        public string PayoutString { get; set; }
        public string WinMessageString { get; set; }
        public string LoseMessageString { get; set; }
        public string WinTitleString { get; set; }
        public string LoseTitleString { get; set; }
        public string WinMessageStringDP { get; set; }

        public UIButton North { get; set; }
        public UIButton East { get; set; }
        public UIButton South { get; set; }
        public UIButton West { get; set; }

        public UILabel PayoutCaption { get; set; }
        public UILabel PayoutField { get; set; }
        public UILabel Waiting { get; set; }

        public UIImage HWall { get; set; }
        public UIImage VWall { get; set; }

        // shared
        private UIImage UIPlayBackground;
        private UIImage UIWaitBackground;
        // logic player
        private UIContainer WallContainer;
        private UIContainer ColoredCellsContainer;
        private UIContainer SolutionPathContainer;
        private Texture2D MazeXTrackTexture;
        // charisma player
        private UIImage NorthBack;
        private UIImage SouthBack;
        private UIImage EastBack;
        private UIImage WestBack;
        private UIImage NorthWall;
        private UIImage SouthWall;
        private UIImage WestWall;
        private UIImage EastWall;
        private UIImage[] ColoredBoxes;

        // positions
        private Vector2 NWCellOriginOffset;
        private Vector2 NWWallOriginOffset;
        private float CellOffsetX;
        private float CellOffsetY;

        // charisma player captions for color blind players
        private string SquareColorCaption = GameFacade.Strings.GetString("f112", "11"); // "Cell color: "
        public static readonly byte BLUE_STRING_INDEX = 12; // "Blue"
        public static readonly byte GREEN_STRING_INDEX = 13; // "Green"
        public static readonly byte RED_STRING_INDEX = 14; // "Red"
        public static readonly byte YELLOW_STRING_INDEX = 15; // "Yellow"
        public static readonly byte BLANK_STRING_INDEX = 16; // "None"

        public UITwoPersonJobObjectMazeEOD(UIEODController controller) : base(controller)
        {
            Rand = new Random();

            // logic player
            BinaryHandlers["TSOMaze_Draw_Solution"] = DrawSolutionHandler;
            BinaryHandlers["TSOMaze_BlueIcon"] = MarkCellHandler;
            BinaryHandlers["TSOMaze_GreenIcon"] = MarkCellHandler;
            BinaryHandlers["TSOMaze_RedIcon"] = MarkCellHandler;
            BinaryHandlers["TSOMaze_YellowIcon"] = MarkCellHandler;
            BinaryHandlers["TSOMaze_ExitIcon"] = MarkCellHandler;
            BinaryHandlers["TSOMaze_Mark_Walls"] = MarkWallsHandler;
            BinaryHandlers["TSOMaze_Show_Logic"] = ShowLogicPlayerHandler;
            // charisma player
            BinaryHandlers["TSOMaze_Show_Charisma"] = ShowCharismaPlayerHandler;
            BinaryHandlers["TSOMaze_Init_Cell"] = UpdateCellHandler;
            BinaryHandlers["TSOMaze_Update_Cell"] = UpdateCellHandler;
            BinaryHandlers["TSOMaze_Final_Cell"] = UpdateCellHandler;
            // shared
            BinaryHandlers["TSOMaze_Show_Waiting"] = ShowWaitingMessageHandler;
            BinaryHandlers["TSOMaze_Update_Timer"] = UpdateTimerHandler;
            BinaryHandlers["TSOMaze_Show_Result"] = ShowResultAlertHandler;
        }
        public override void OnClose()
        {
            CloseInteraction();
            base.OnClose();
        }
        private void ShowCharismaPlayerHandler(string evt, byte[] data)
        {
            Script = RenderScript("twopersonjobobjectmazecharisma.uis");
            // background images
            UIPlayBackground = Script.Create<UIImage>("UIPlayBackground");
            AddAt(0, UIPlayBackground);

            // all walls
            NorthWall = Script.Create<UIImage>("HWall");
            NorthWall.Position = UIPlayBackground.Position;
            NorthWall.X += 13;
            NorthWall.Y += 11;
            Add(NorthWall);
            SouthWall = Script.Create<UIImage>("HWall");
            SouthWall.Position = UIPlayBackground.Position;
            SouthWall.X += 13;
            SouthWall.Y += 61;
            Add(SouthWall);
            WestWall = Script.Create<UIImage>("VWall");
            WestWall.Position = UIPlayBackground.Position;
            WestWall.X += 9;
            WestWall.Y += 14;
            Add(WestWall);
            EastWall = Script.Create<UIImage>("VWall");
            EastWall.Position = UIPlayBackground.Position;
            EastWall.X += 59;
            EastWall.Y += 14;
            Add(EastWall);

            // create boxes
            // uigraphics / eods / twopersonjobobjectmaze / EOD_2PJobObj_Blue.bmp, Green.bmp, Red.bmp, Yellow.bmp
            var boxOffset = new Vector2(16, 18);
            ColoredBoxes = new UIImage[] { new UIImage(GetTexture(0x000007FD00000001)), new UIImage(GetTexture(0x000007FE00000001)),
                new UIImage(GetTexture(0x0000080100000001)), new UIImage(GetTexture(0x0000080400000001)), new UIImage(GetTexture(0x0000080300000001)) };

            for (int index = 0; index < ColoredBoxes.Length; index++)
            {
                var box = ColoredBoxes[index];
                box.ScaleX = box.ScaleY = 5.0f;
                box.Position = UIPlayBackground.Position + boxOffset;
                box.Visible = false;
                box.Tooltip = GameFacade.Strings.GetString("f112", (index + BLUE_STRING_INDEX) + "");
                Add(box);
            }
            // the tooltip for the exit icon should be "Blue"
            ColoredBoxes[4].Tooltip = GameFacade.Strings.GetString("f112", "" + BLUE_STRING_INDEX);

            if (Waiting != null)
                Remove(Waiting);
            if (HWall != null)
                Remove(HWall);
            if (VWall != null)
                Remove(VWall);

            // labels: since they're useless in FreeSO's maze—as payouts are not happening—I've decided to make them help colorblind players
            if (PayoutCaption != null)
            {
                PayoutCaption.Alignment = TextAlignment.Left;
                PayoutCaption.Caption = SquareColorCaption;
            }
            if (PayoutField != null)
            {
                PayoutField.Alignment = TextAlignment.Left;

                // "Waiting for Logic Player"
                ShowWaitingMessageHandler(evt, data); // disables buttons, sets time to 0
            }

            // add listeners to buttons, sscale them
            var offsetFat = North.Size.X * 0.5f;
            var offsetSkinny = North.Size.Y * 0.5f;
            North.OnButtonClick += (btn) => { Send("TSOMaze_Button_Click", new byte[] { (byte)AbstractMazeCellCardinalDirections.North }); };
            North.ScaleX = North.ScaleY = 1.5f;
            North.X = North.X - offsetFat + 12;
            North.Y = North.Y - offsetSkinny + 2;
            West.OnButtonClick += (btn) => { Send("TSOMaze_Button_Click", new byte[] { (byte)AbstractMazeCellCardinalDirections.West }); };
            West.ScaleX = West.ScaleY = 1.5f;
            West.X -= offsetSkinny;
            West.Y = West.Y - offsetFat + 9;
            East.OnButtonClick += (btn) => { Send("TSOMaze_Button_Click", new byte[] { (byte)AbstractMazeCellCardinalDirections.East }); };
            East.ScaleX = East.ScaleY = 1.5f;
            East.X += 2;
            East.Y = East.Y - offsetFat + 9;
            South.OnButtonClick += (btn) => { Send("TSOMaze_Button_Click", new byte[] { (byte)AbstractMazeCellCardinalDirections.South }); };
            South.ScaleX = South.ScaleY = 1.5f;
            South.X = South.X - offsetFat + 12;
            South.Y = South.Y - offsetSkinny + 6;
            UIPlayBackground.Y += 2;

            // show EOD
            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Tips = EODTextTips.None,
                Timer = EODTimer.Normal,
                Expandable = false,
                Expanded = false
            });
        }
        private void ShowLogicPlayerHandler(string evt, byte[] data)
        {
            Script = RenderScript("twopersonjobobjectmazelogic.uis");
            UIWaitBackground = Script.Create<UIImage>("UIWaitBackground");
            Add(UIWaitBackground);
            UIPlayBackground = Script.Create<UIImage>("UIPlayBackground");
            Add(UIPlayBackground);

            // get some offsets
            var red = Script.Create<UIImage>("RedIcon");
            NWCellOriginOffset = red.Position + UIPlayBackground.Position;
            var wall = Script.Create<UIImage>("HWall");
            NWWallOriginOffset = wall.Position + UIPlayBackground.Position;
            var green = Script.Create<UIImage>("GreenIcon");
            CellOffsetX = green.Position.X;
            CellOffsetY = green.Position.Y;
            
            SetTime(0);

            VWall = Script.Create<UIImage>("VWall");
            HWall = Script.Create<UIImage>("HWall");
            
            var gd = UIWaitBackground.Texture.GraphicsDevice;
            // try to get textures for Slot Machine #4 Slot stops
            try
            {
                AbstractTextureRef MazeXTrackTextureRef = new FileTextureRef("Content/Textures/EOD/mazextrack.bmp");
                MazeXTrackTexture = MazeXTrackTextureRef.Get(gd);
            }
            catch (Exception e)
            {
                var blue = Script.Create<UIImage>("BlueIcon");
                MazeXTrackTexture = blue.Texture;
            }

            // show EOD
            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Tips = EODTextTips.None,
                Timer = EODTimer.Normal,
                Expandable = false,
                Expanded = false
            });
        }
        private void UpdateCellHandler(string evt, byte[] wallConfigAndColor)
        {
            evt = evt.Remove(0, 8); // remove "TSOMaze_"
            bool enableButtons = (evt[0] == 'U'); // if "Update_Cell"
            bool showExit = (evt[0] == 'F');
            if (wallConfigAndColor.Length == 2)
            {
                EnableAllButtons(); // enables directional buttons
                NorthWall.Visible = false;
                WestWall.Visible = false;
                EastWall.Visible = false;
                SouthWall.Visible = false;
                AddWalls(-1, -1, wallConfigAndColor[0]); // disables invalid directional buttons if flag is 1, all buttons if 0
                UpdateCellColorAndLabel(wallConfigAndColor[1]);
                if (!enableButtons)
                {
                    DisableAllButtons();
                    if (!showExit)
                        PayoutField.Caption = WaitingMessage;
                }
                ColoredBoxes[4].Visible = showExit;
            }
        }
        /*
         * Show "Waiting for Logic player" in the payout TextField, but only sets time to 0 for logic player
         */
        private void ShowWaitingMessageHandler(string evt, byte[] nothing)
        {
            SetTime(0);
            if (PayoutField != null) // charisma player
            {
                AddWalls(-1, -1, (byte)MazeWallConfigCodes.All);
                UpdateCellColorAndLabel(BLANK_STRING_INDEX);
                PayoutField.Caption = WaitingMessage;
            }
            Parent.Invalidate();
        }
        /*
         * Update the EODTimer and Tip
         */
        private void UpdateTimerHandler(string evt, byte[] time)
        {
            SetTime(BitConverter.ToInt32(time, 0));
            Parent.Invalidate();
        }
        /*
         * Displays a UIAlert if the players solved the maze (win) or ran out of time (loss)
         */
        private void ShowResultAlertHandler(string evt, byte[] result)
        {
            var randomMessageString = Rand.Next(0, 5);
            var successOrFailureTitle = 28; // "Success"
            if (result[0] == 0) // loss
            {
                randomMessageString += 17; // the loss strings start at 17 in f112
                successOrFailureTitle = 27; // "Failure"
            }
            else // win
                randomMessageString += 22; // the win strings start at 22
            if (Alert != null)
                UIScreen.RemoveDialog(Alert);
            Alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = GameFacade.Strings.GetString("f112", successOrFailureTitle + ""),
                Message = GameFacade.Strings.GetString("f112", randomMessageString + ""),
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = UIAlertButton.Ok(
                ok =>
                {
                    UIScreen.RemoveDialog(Alert);
                    Alert = null;
                }),
            }, true);
        }
        private void MarkCellHandler(string evt, byte[] coords)
        {
            UIContainer container = ColoredCellsContainer;
            evt = evt.Remove(0, 8); // remove "TSOMaze_"
            if (coords.Length > 0 && coords.Length % 2 == 0)
            {
                string index = "15"; // "Yellow"
                switch (evt[0])
                {
                    // blue is always the first color marked, so make a new container for it and make sure it's visible
                    case 'B':
                        {
                            index = "12"; // "Blue"
                            if (ColoredCellsContainer != null)
                                Remove(ColoredCellsContainer);
                            ColoredCellsContainer = new UIContainer()
                            {
                                Visible = true
                            };
                            Add(ColoredCellsContainer);
                            container = ColoredCellsContainer;
                            break;
                        }
                    case 'E':
                        {
                            index = "12";
                            if (SolutionPathContainer != null)
                                Remove(SolutionPathContainer);
                            SolutionPathContainer = new UIContainer()
                            {
                                Visible = true
                            };
                            container = SolutionPathContainer;
                            Add(SolutionPathContainer);
                            break; // "Blue" for "ExitIcon"
                        }
                    case 'G': index = "13"; break; // "Green"
                    case 'R': index = "14"; break; // "Red"
                }
                for (int x = 0; x < coords.Length - 1; x++)
                {
                    var color = Script.Create<UIImage>(evt);
                    color.Position = NWCellOriginOffset;
                    color.Y += coords[x++] * CellOffsetY;
                    color.X += coords[x] * CellOffsetX;
                    color.Tooltip = GameFacade.Strings.GetString("f112", index);
                    container.Add(color);
                }
            }
        }
        /*
         * Logic player only: based on the param, draw walls on cells based on the supplied wall config code
         * @param wallConfigCodes: An array of all of the wallconfig codes of every OTHER cell, like a checkerboard pattern:
         * For even rows (including 0), mark walls for even columns (including 0)
         * For odd rows, mark walls for odd columns
         * Special cases for outer maze walls which are north: row=0, south: row=MAX_ROWS, west: column=0, east: column=MAX_COLUMNS
         */
        private void MarkWallsHandler(string evt, byte[] wallConfigCodes)
        {
            // remove the old maze
            if (WallContainer != null)
            {
                Remove(WallContainer);
                if (SolutionPathContainer != null)
                    SolutionPathContainer.Visible = false;
            }
            WallContainer = new UIContainer();
            Add(WallContainer);
            int coordsIndex = 0;
            for (int row = 0; row < VMEODTwoPersonJobObjectMazePlugin.MAX_ROWS; row++)
            {
                for (int column = 0; column < VMEODTwoPersonJobObjectMazePlugin.MAX_COLUMNS; column++)
                {
                    if (coordsIndex < wallConfigCodes.Length)
                    {
                        // draw left and right borders
                        if (column == 0)
                            AddVWall(row, column, false);
                        else if (column == VMEODTwoPersonJobObjectMazePlugin.MAX_COLUMNS - 1)
                            AddVWall(row, column, true);

                        // draw north and south borders
                        if (row == 0)
                            AddHWall(row, column, false);
                        else if (row == VMEODTwoPersonJobObjectMazePlugin.MAX_ROWS - 1)
                            AddHWall(row, column, true);

                        if (row % 2 == 0) // start at column 0, skip odd columns
                        {
                            // draw the north walls of row 0 and south walls of row MAX_ROWS - 1 regardless, to close the maze border
                            if (column % 2 == 0)
                                AddWalls(row, column, wallConfigCodes[coordsIndex++]);
                        }
                        else // start at column 1, skip even columns
                        {
                            // draw the west wall of column 0 and east wall of MAX_COLUMNS - 1 regardless, to close the maze border
                            if (column % 2 == 1)
                                AddWalls(row, column, wallConfigCodes[coordsIndex++]);
                        }
                    }
                }
            }
        }
        /*
         * Logic player only: removes colored cells and draws the path from the origin to the goal
         */
        private void DrawSolutionHandler(string evt, byte[] solutionPathCoords)
        {
            ColoredCellsContainer.Visible = false;
            if (solutionPathCoords.Length > 2 && solutionPathCoords.Length % 2 == 1)
            {
                // the very first byte is the color of the origin
                var colorCode = solutionPathCoords[0];
                string index = "";
                string colorString = "";
                switch (colorCode)
                {
                    case 0:
                        {
                            index = "12"; // "Blue"
                            colorString = "BlueIcon";
                            break;
                        }
                    case 1:
                        {
                            index = "13"; // "Green"
                            colorString = "GreenIcon";
                            break;
                        }
                    case 2:
                        {
                            index = "14"; // "Red"
                            colorString = "RedIcon";
                            break;
                        }
                    default:
                        {
                            index = "15"; // "Yellow"
                            colorString = "YellowIcon";
                            break;
                        }
                }
                // the first two coordinates are the origin
                var origin = Script.Create<UIImage>(colorString);
                origin.Position = NWCellOriginOffset;
                origin.Y += solutionPathCoords[1] * CellOffsetY;
                origin.X += solutionPathCoords[2] * CellOffsetX;
                origin.Tooltip = GameFacade.Strings.GetString("f112", index);
                SolutionPathContainer.Add(origin);

                // now draw the path with remaining coordinates
                for (int x = 3; x < solutionPathCoords.Length - 1; x++)
                {
                    var track = new UIImage(MazeXTrackTexture);
                    track.Position = NWCellOriginOffset;
                    track.Y += solutionPathCoords[x++] * CellOffsetY;
                    track.X += solutionPathCoords[x] * CellOffsetX;
                    SolutionPathContainer.Add(track);
                }
            }
        }
        /*
         * Makes visible the colored box matching the wallColor param, or makes all invisible if blank. Updates caption to match the color/none
         */
        private void UpdateCellColorAndLabel(byte wallColor)
        {
            // 0, 1, 2, 3, or 4 = blue, green, red, yellow, or none
            // if wallColor = 4, all boxes will be invisible
            int boxIndex = wallColor;
            for (int index = 0; index < ColoredBoxes.Length; index++)
            {
                if (boxIndex == index)
                    ColoredBoxes[index].Visible = true;
                else
                    ColoredBoxes[index].Visible = false;
            }
            PayoutField.Caption = GameFacade.Strings.GetString("f112", (wallColor + BLUE_STRING_INDEX) + ""); // "Blue" | "Green" | "Red" | "Yellow" | "None"
        }
        /*
         * For Logic Player: Based on wallCode, builds walls for cell found at row and column
         * For Charisma Player: Based on wallCode, makes each of the four walls and disables buttons for invalid directions
         */
        private void AddWalls(int row, int column, byte wallCode)
        {
            // get the wall configuration, really just for cleaner, better understood code
            MazeWallConfigCodes wallConfig = MazeWallConfigCodes.All;
            if (Enum.IsDefined(typeof(MazeWallConfigCodes), wallCode))
            {
                var wallConfigName = Enum.GetName(typeof(MazeWallConfigCodes), wallCode);
                wallConfig = (MazeWallConfigCodes)Enum.Parse(typeof(MazeWallConfigCodes), wallConfigName);
            }
            else
                return;
            // add the walls based on the configuration
            switch (wallConfig)
            {
                case MazeWallConfigCodes.None:
                    {
                        return;
                    }
                case MazeWallConfigCodes.North_Only:
                    {
                        AddHWall(row, column, false);
                        break;
                    }
                case MazeWallConfigCodes.North_West:
                    {
                        AddHWall(row, column, false);
                        AddVWall(row, column, false);
                        break;
                    }
                case MazeWallConfigCodes.North_East:
                    {
                        AddHWall(row, column, false);
                        AddVWall(row, column, true);
                        break;
                    }
                case MazeWallConfigCodes.North_South:
                    {
                        AddHWall(row, column, false);
                        AddHWall(row, column, true);
                        break;
                    }
                case MazeWallConfigCodes.North_West_East:
                    {
                        AddHWall(row, column, false);
                        AddVWall(row, column, false);
                        AddVWall(row, column, true);
                        break;
                    }
                case MazeWallConfigCodes.North_West_South:
                    {
                        AddHWall(row, column, false);
                        AddVWall(row, column, false);
                        AddHWall(row, column, true);
                        break;
                    }
                case MazeWallConfigCodes.North_East_South:
                    {
                        AddHWall(row, column, false);
                        AddVWall(row, column, true);
                        AddHWall(row, column, true);
                        break;
                    }
                case MazeWallConfigCodes.West_Only:
                    {
                        AddVWall(row, column, false);
                        break;
                    }
                case MazeWallConfigCodes.West_East:
                    {
                        AddVWall(row, column, false);
                        AddVWall(row, column, true);
                        break;
                    }
                case MazeWallConfigCodes.West_South:
                    {
                        AddVWall(row, column, false);
                        AddHWall(row, column, true);
                        break;
                    }
                case MazeWallConfigCodes.West_East_South:
                    {
                        AddVWall(row, column, false);
                        AddVWall(row, column, true);
                        AddHWall(row, column, true);
                        break;
                    }
                case MazeWallConfigCodes.East_Only:
                    {
                        AddVWall(row, column, true);
                        break;
                    }
                case MazeWallConfigCodes.East_South:
                    {
                        AddVWall(row, column, true);
                        AddHWall(row, column, true);
                        break;
                    }
                case MazeWallConfigCodes.South_Only:
                    {
                        AddHWall(row, column, true);
                        break;
                    }
                default: // charisma player only, happens when logic players disconnects - (MazeWallConfigCodes.All)
                    {
                        AddHWall(row, column, false);
                        AddVWall(row, column, false);
                        AddVWall(row, column, true);
                        AddHWall(row, column, true);
                        break;
                    }
            }
        }
        /*
         * Horizontal Walls: north and south
         */
        private void AddHWall(int row, int column, bool isSouth)
        {
            if (row == -1) // charisma player
            {
                if (isSouth)
                {
                    SouthWall.Visible = true;
                    South.Disabled = true;
                }
                else
                {
                    NorthWall.Visible = true;
                    North.Disabled = true;
                }
            }
            else // logic player
            {
                if (isSouth)
                    row++;
                UIHighlightSprite hwall = new UIHighlightSprite((int)HWall.Width, (int)HWall.Height, 0.95f);
                hwall.Position = NWWallOriginOffset;
                hwall.Y += row * CellOffsetY;
                hwall.X += column * CellOffsetX;
                WallContainer.Add(hwall);
            }
        }
        /*
         * Vertical walls: west and east
         */
        private void AddVWall(int row, int column, bool isEast)
        {
            if (row == -1) // charisma player
            {
                if (isEast)
                {
                    EastWall.Visible = true;
                    East.Disabled = true;
                }
                else
                {
                    WestWall.Visible = true;
                    West.Disabled = true;
                }
            }
            else // logic player
            {
                if (isEast)
                    column++;
                UIHighlightSprite vwall = new UIHighlightSprite((int)VWall.Width, (int)VWall.Height, 0.95f);
                vwall.Position = NWWallOriginOffset;
                vwall.Y += row * CellOffsetY;
                vwall.X += column * CellOffsetX;
                WallContainer.Add(vwall);
            }
        }
        /*
         * Charisma Player only: enables all 4 directional buttons 
         */
        private void EnableAllButtons()
        {
            North.Disabled = false;
            North.CurrentFrame = 0;
            West.Disabled = false;
            West.CurrentFrame = 0;
            East.Disabled = false;
            East.CurrentFrame = 0;
            South.Disabled = false;
            South.CurrentFrame = 0;
        }
        /*
         * Charisma Player only: disables all buttons when at the final solution/exit cell
         */
        private void DisableAllButtons()
        {
            North.Disabled = true;
            West.Disabled = true;
            East.Disabled = true;
            South.Disabled = true;
        }
    }
}
