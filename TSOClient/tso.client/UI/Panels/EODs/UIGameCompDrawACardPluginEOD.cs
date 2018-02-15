using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.SimAntics.NetPlay.EODs.Handlers.Data;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Panels.EODs
{
    class UIGameCompDrawACardPluginEOD : UIEOD
    {
        /* Elements for Deck Info */
        public UIImage SelectCardBack { get; set; }
        public UILabel SelectCardText { get; set; }
        public UIListBox SelectCardList { get; set; }

        /* Elements for Draw / View Current Card */
        public UIImage ViewDrawInfoBack { get; set; }
        public UITextEdit ViewDrawInfoTextEdit { get; set; }

        /* Elements for Editing / Creating new CARDS */
        public UIImage EditCardBack { get; set; }
        public UIImage NewCardBack { get; set; }
        public UILabel DrawChancesText { get; set; }
        public UITextEdit NumDrawChancesTextEdit { get; set; }
        public UITextEdit EditCardTextEdit { get; set; }
        public UITextEdit NewCardTextEdit { get; set; }
        public UIButton SpinnerMinusBtn { get; set; }
        public UIButton SpinnerPlusBtn { get; set; }
        public UIButton DeleteBtn { get; set; }
        public UIButton SaveBtn { get; set; }
        public UIButton EditCardPrevBtn { get; set; }
        public UIButton EditCardNextBtn { get; set; }

        /* Elements for Editing / Manage GAME */
        public UIImage ManageGameBack { get; set; }
        public UILabel NumCardsText { get; set; }
        public UILabel UniqueCardsText { get; set; }
        public UITextEdit ManageGameDescripTextEdit { get; set; }
        public UITextEdit ManageGameTitleTextEdit { get; set; }
        public UITextEdit NumCardsTextEdit { get; set; }
        public UITextEdit UniqueCardsTextEdit { get; set; }

        // shared Elements
        public UIButton EditCardBtn { get; set; }
        public UIButton EditGameBtn { get; set; }
        public UIButton NewCardBtn { get; set; }
        private UITextEdit BetterLabel { get; set; }
        private string BetterLabelDeckString;
        private string ZeroCardMessage;
        private string LoadingString;
        private string ConfirmDelete;
        private string SaveChangesPrompt;
        private UIGameCompDrawACardPluginEODStates State;
        private int TotalUniqueCards;
        private int GrandTotalCards;
        private string[] CurrentDeckListBox;
        private string CurrentCardTextFromServer;
        private int CurrentCardFrequencyFromServer;
        private int CurrentCardIndex = -1;
        private string CurrentGameTitle;
        private string CurrentGameDescription;
        private bool CurrentCardInfoChanged;
        private bool NumCardsChanged;
        private bool GameInfoChanged;
        public UIScript Script;

        // misc texture
        public Texture2D ImagePanelDivider { get; set; }

        public UIGameCompDrawACardPluginEOD(UIEODController controller) : base(controller)
        {
            BuildUI();
            PlaintextHandlers["DrawCard_Delete_Fail"] = DeleteFailHander;
            BinaryHandlers["DrawCard_Please_Wait"] = PleaseWaitHandler;
            BinaryHandlers["DrawCard_Drawn"] = DrawViewCardUI;
            BinaryHandlers["DrawCard_Info"] = DeckInfoUI;
            BinaryHandlers["DrawCard_Manage"] = ManageDeckUI;
            BinaryHandlers["DrawCard_Update_Card"] = UpdateCurrentCardHandler;
            BinaryHandlers["DrawCard_Update_Deck"] = UpdateDeckHandler;
            BinaryHandlers["DrawCard_Update_Deck_Numbers"] = DeckNumbersHandler;
        }

        public override void OnClose()
        {
            base.OnClose();
            Send("DrawCard_Close", "");
            CloseInteraction();
        }

        private void BuildUI()
        {
            Script = RenderScript("gamecompdrawacardeod.uis");

            // a FreeSO exclusive change to the ugly UI labels
            BetterLabel = new UITextEdit();
            BetterLabel.X = 70;
            BetterLabel.Y = 9;
            BetterLabel.Size = new Microsoft.Xna.Framework.Vector2(300, 24);
            BetterLabel.TextStyle.Size = 10;
            BetterLabel.Mode = UITextEditMode.ReadOnly;
            ZeroCardMessage = GameFacade.Strings.GetString("f112", "1"); // "You have zero cards in this game deck."
            Add(BetterLabel);
            Remove(SelectCardText);
            LoadingString = GameFacade.Strings["UIText", "259", "6"]; // "Loading...";
            ConfirmDelete = GameFacade.Strings.GetString("f112", "2"); // "Delete card"
            SaveChangesPrompt = GameFacade.Strings.GetString("f112", "3"); // "Do you want to save your changes?"

            // tweaks to UI--Maxis, you're killing me
            NumDrawChancesTextEdit.Mode = UITextEditMode.Editor;
            NumCardsTextEdit.Mode = UITextEditMode.ReadOnly;
            UniqueCardsText.Y = NumCardsText.Y = UniqueCardsTextEdit.Y;
            UniqueCardsText.X -= 11;
            UniqueCardsTextEdit.X -= 10;
            NumCardsText.X -= 15;
            NumCardsTextEdit.X -= 4;
            EditCardBtn.X += 20;
            EditCardBtn.Y -= 4;
            EditGameBtn.X += 20;
            EditGameBtn.Y -= 4;
            NewCardBtn.X += 20;
            NewCardBtn.Y -= 4;
            ViewDrawInfoTextEdit.X += 5;
            ViewDrawInfoTextEdit.Y += 1;
            ManageGameTitleTextEdit.X += 2;
            ManageGameDescripTextEdit.X += 2;
            DeleteBtn.Tooltip = GameFacade.Strings.GetString("f112", "2"); // "Delete card"

            // add button listeners
            EditCardPrevBtn.OnButtonClick += EditCardPrevBtnClickedHandler;
            EditCardNextBtn.OnButtonClick += EditCardNextBtnHandler;
            SpinnerMinusBtn.OnButtonClick += SpinnerMinusBtnHandler;
            SpinnerPlusBtn.OnButtonClick += SpinnerPlusBtnHandler;
            DeleteBtn.OnButtonClick += DeleteBtnHandler;
            SaveBtn.OnButtonClick += SaveBtnHandler;
            EditCardBtn.OnButtonClick += EditCardBtnHandler;
            EditGameBtn.OnButtonClick += EditGameBtnHandler;
            NewCardBtn.OnButtonClick += NewCardBtnHandler;

            // add field listeners
            NumDrawChancesTextEdit.OnChange += (target) => { NumCardsChanged = true; SaveBtn.Disabled = false; };
            ManageGameDescripTextEdit.OnChange += (target) => { GameInfoChanged = true; SaveBtn.Disabled = false; };
            ManageGameTitleTextEdit.OnChange += (target) => { GameInfoChanged = true; SaveBtn.Disabled = false; };
            NewCardTextEdit.OnChange += (target) => { CurrentCardInfoChanged = true; SaveBtn.Disabled = false; };
            EditCardTextEdit.OnChange += (target) => { CurrentCardInfoChanged = true; SaveBtn.Disabled = false; };
            SelectCardList.OnDoubleClick += CardClickedHandler;
            
            DisableButtons();

            // to avoid a null issue
            State = UIGameCompDrawACardPluginEODStates.ViewSingleCard;
        }

        private void PleaseWaitHandler(string evt, byte[] mode)
        {
            DisableButtons();
            BetterLabel.CurrentText = LoadingString;
            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Tips = EODTextTips.Short,
                Timer = EODTimer.None,
            });
        }

        private void DeleteFailHander(string evt, string msg)
        {
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = GameFacade.Strings["UIText", "203", "9"], // "Delete card(s)"
                Message = GameFacade.Strings.GetString("f112", "6"), // "Server error. The card could not be deleted."
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = new UIAlertButton[] {
                            new UIAlertButton (UIAlertButtonType.OK, ((btn1) =>
                            {
                                UpdateCurrentCardHandler(evt,
                                    VMEODGameCompDrawACardData.SerializeStrings(CurrentCardTextFromServer, CurrentCardFrequencyFromServer + ""));
                                UIScreen.RemoveDialog(alert);
                            }))
                        }
            }, true);
        }

        private void EditCardPrevBtnClickedHandler(UIElement target)
        {
            DisableButtons();
            UIAlert alert = null;
            if ((CurrentCardInfoChanged) || (NumCardsChanged))
            {
                if (State.Equals(UIGameCompDrawACardPluginEODStates.MakeNewCard))
                {
                    alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                    {
                        TextSize = 12,
                        Title = GameFacade.Strings["UIText", "203", "10"], // "Save current panel"
                        Message = SaveChangesPrompt,
                        Alignment = TextAlignment.Center,
                        TextEntry = false,
                        Buttons = new UIAlertButton[] {
                            new UIAlertButton (UIAlertButtonType.Yes, ((btn1) =>
                            {
                                SaveNewCard();
                                GotoPreviousCard();
                                UIScreen.RemoveDialog(alert);
                            })),
                            new UIAlertButton (UIAlertButtonType.No, ((btn2) =>
                            {
                                CancelNewCard();
                                GotoPreviousCard();
                                UIScreen.RemoveDialog(alert);
                            }))
                        }
                    }, true);
                }
                else
                {
                    alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                    {
                        TextSize = 12,
                        Title = GameFacade.Strings["UIText", "203", "10"], // "Save current panel"
                        Message = SaveChangesPrompt,
                        Alignment = TextAlignment.Center,
                        TextEntry = false,
                        Buttons = new UIAlertButton[] {
                            new UIAlertButton (UIAlertButtonType.Yes, ((btn1) =>
                            {
                                SaveCardChanges();
                                GotoPreviousCard();
                                UIScreen.RemoveDialog(alert);
                            })),
                            new UIAlertButton (UIAlertButtonType.No, ((btn2) =>
                            {
                                GotoPreviousCard();
                                UIScreen.RemoveDialog(alert);
                            }))
                        }
                    }, true);
                }
            }
            else
                GotoPreviousCard();
        }

        private void EditCardNextBtnHandler(UIElement target)
        {
            DisableButtons();
            UIAlert alert = null;
            if ((CurrentCardInfoChanged) || (NumCardsChanged))
            {
                if (State.Equals(UIGameCompDrawACardPluginEODStates.MakeNewCard))
                {
                    alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                    {
                        TextSize = 12,
                        Title = GameFacade.Strings["UIText", "203", "10"], // "Save current panel"
                        Message = SaveChangesPrompt,
                        Alignment = TextAlignment.Center,
                        TextEntry = false,
                        Buttons = new UIAlertButton[] {
                            new UIAlertButton (UIAlertButtonType.Yes, ((btn1) =>
                            {
                                SaveNewCard();
                                GotoNextCard();
                                UIScreen.RemoveDialog(alert);
                            })),
                            new UIAlertButton (UIAlertButtonType.No, ((btn2) =>
                            {
                                CancelNewCard();
                                GotoNextCard();
                                UIScreen.RemoveDialog(alert);
                            }))
                        }
                    }, true);
                }
                else
                {
                    alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                    {
                        TextSize = 12,
                        Title = GameFacade.Strings["UIText", "203", "10"], // "Save current panel"
                        Message = SaveChangesPrompt,
                        Alignment = TextAlignment.Center,
                        TextEntry = false,
                        Buttons = new UIAlertButton[] {
                            new UIAlertButton (UIAlertButtonType.Yes, ((btn1) =>
                            {
                                SaveCardChanges();
                                GotoNextCard();
                                UIScreen.RemoveDialog(alert);
                            })),
                            new UIAlertButton (UIAlertButtonType.No, ((btn2) =>
                            {
                                GotoNextCard();
                                UIScreen.RemoveDialog(alert);
                            }))
                        }
                    }, true);
                }
            }
            else
                GotoNextCard();
        }

        private void SpinnerMinusBtnHandler(UIElement target)
        {
            DisableButtons();
            int newFrequency = 1;
            if (Int32.TryParse(NumDrawChancesTextEdit.CurrentText, out newFrequency))
            {
                if (newFrequency > 1)
                {
                    newFrequency--;
                    NumCardsChanged = true;
                    NumDrawChancesTextEdit.CurrentText = newFrequency + "";
                }
            }
            EnableButtons();
        }

        private void SpinnerPlusBtnHandler(UIElement target)
        {
            DisableButtons();
            int newFrequency = 100;
            if (Int32.TryParse(NumDrawChancesTextEdit.CurrentText, out newFrequency))
            {
                if (newFrequency < 99)
                {
                    newFrequency++;
                    NumCardsChanged = true;
                    NumDrawChancesTextEdit.CurrentText = newFrequency + "";
                }
            }
            EnableButtons();
        }

        private void DeleteBtnHandler(UIElement target)
        {
            DisableButtons();
            UIAlert alert = null;
            if (State.Equals(UIGameCompDrawACardPluginEODStates.EditSingleCard))
            {
                alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                {
                    TextSize = 12,
                    Title = ConfirmDelete, // "Delete card"
                    Message = ConfirmDelete + "?", // "Delete card?"
                    Alignment = TextAlignment.Center,
                    TextEntry = false,
                    Buttons = new UIAlertButton[] {
                            new UIAlertButton (UIAlertButtonType.Yes, ((btn1) =>
                            {
                                DeleteCardHandler();
                                UIScreen.RemoveDialog(alert);
                            })),
                            new UIAlertButton (UIAlertButtonType.No, ((btn2) =>
                            {
                                EnableButtons();
                                UIScreen.RemoveDialog(alert);
                            }))
                        }
                }, true);
            }
            else if (State.Equals(UIGameCompDrawACardPluginEODStates.MakeNewCard))
            {
                alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                {
                    TextSize = 12,
                    Title = ConfirmDelete, // "Delete card"
                    Message = ConfirmDelete + "?", // "Delete card?"
                    Alignment = TextAlignment.Center,
                    TextEntry = false,
                    Buttons = new UIAlertButton[] {
                            new UIAlertButton (UIAlertButtonType.Yes, ((btn1) =>
                            {
                                CancelNewCard();
                                SwitchToEditDeck();
                                EnableButtons();
                                UIScreen.RemoveDialog(alert);
                            })),
                            new UIAlertButton (UIAlertButtonType.No, ((btn2) =>
                            {
                                EnableButtons();
                                UIScreen.RemoveDialog(alert);
                            }))
                        }
                }, true);
            }
            else
                EnableButtons();
        }

        private void SaveBtnHandler(UIElement target)
        {
            DisableButtons();
            if (State.Equals(UIGameCompDrawACardPluginEODStates.EditSingleCard))
            {
                if ((CurrentCardInfoChanged) || (NumCardsChanged))
                {
                    SaveCardChanges();
                    EnableButtons();
                }
            }
            else if (State.Equals(UIGameCompDrawACardPluginEODStates.MakeNewCard))
            {
                if ((CurrentCardInfoChanged) || (NumCardsChanged))
                {
                    if (TotalUniqueCards == VMEODGameCompDrawACardPlugin.MAXIMUM_UNIQUE_CARDS - 1)
                    {
                        UIAlert alert = null;
                        alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                        {
                            TextSize = 12,
                            Title = GameFacade.Strings["UIText", "203", "10"], // "Save current panel"
                            // "Your new card has been saved.  You have saved the maximum number of individual cards."
                            Message = GameFacade.Strings["UIText", "203", "47"],
                            Alignment = TextAlignment.Center,
                            TextEntry = false,
                            Buttons = new UIAlertButton[] {
                            new UIAlertButton (UIAlertButtonType.OK, ((btn1) =>
                            {
                                UIScreen.RemoveDialog(alert);
                            }))
                        }
                        }, true);
                        SaveNewCard();
                        SwitchToEditDeck();
                    }
                    else if (TotalUniqueCards == VMEODGameCompDrawACardPlugin.MAXIMUM_UNIQUE_CARDS)
                    {
                        UIAlert alert = null;
                        alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                        {
                            TextSize = 12,
                            Title = GameFacade.Strings["UIText", "203", "10"], // "Save current panel"
                            Message = GameFacade.Strings.GetString("f112", "5"), // "Error: You have already saved the maximum number of individual cards."
                            Alignment = TextAlignment.Center,
                            TextEntry = false,
                            Buttons = new UIAlertButton[] {
                            new UIAlertButton (UIAlertButtonType.OK, ((btn1) =>
                            {
                                CancelNewCard();
                                SwitchToEditDeck();
                                EnableButtons();
                                UIScreen.RemoveDialog(alert);
                            }))
                        }
                        }, true);
                    }
                    else
                    {
                        SaveNewCard();
                        SwitchToEditDeck();
                    }
                }
            }
            else if (State.Equals(UIGameCompDrawACardPluginEODStates.EditGame))
            {
                if (GameInfoChanged)
                    SaveNewGameData();
                EnableButtons();
            }
        }

        private void EditCardBtnHandler(UIElement target)
        {
            DisableButtons();
            UIAlert alert = null;
            if ((CurrentCardInfoChanged) || (NumCardsChanged))
            {
                if (State.Equals(UIGameCompDrawACardPluginEODStates.MakeNewCard))
                {
                    alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                    {
                        TextSize = 12,
                        Title = GameFacade.Strings["UIText", "203", "10"], // "Save current panel"
                        Message = SaveChangesPrompt,
                        Alignment = TextAlignment.Center,
                        TextEntry = false,
                        Buttons = new UIAlertButton[] {
                            new UIAlertButton (UIAlertButtonType.Yes, ((btn1) =>
                            {
                                SaveNewCard();
                                SwitchToEditDeck();
                                UIScreen.RemoveDialog(alert);
                            })),
                            new UIAlertButton (UIAlertButtonType.No, ((btn2) =>
                            {
                                CancelNewCard();
                                SwitchToEditDeck();
                                EnableButtons();
                                UIScreen.RemoveDialog(alert);
                            }))
                        }
                    }, true);
                }
                else
                {
                    alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                    {
                        TextSize = 12,
                        Title = GameFacade.Strings["UIText", "203", "10"], // "Save current panel"
                        Message = SaveChangesPrompt,
                        Alignment = TextAlignment.Center,
                        TextEntry = false,
                        Buttons = new UIAlertButton[] {
                            new UIAlertButton (UIAlertButtonType.Yes, ((btn1) =>
                            {
                                SaveCardChanges();
                                SwitchToEditDeck();
                                EnableButtons();
                                UIScreen.RemoveDialog(alert);
                            })),
                            new UIAlertButton (UIAlertButtonType.No, ((btn2) =>
                            {
                                CancelEditCard();
                                SwitchToEditDeck();
                                EnableButtons();
                                UIScreen.RemoveDialog(alert);
                            }))
                        }
                    }, true);
                }
            }
            else
            {
                SwitchToEditDeck();
                EnableButtons();
            }
        }

        private void EditGameBtnHandler(UIElement target)
        {
            DisableButtons();
            UIAlert alert = null;
            if ((CurrentCardInfoChanged) || (NumCardsChanged))
            {
                if (State.Equals(UIGameCompDrawACardPluginEODStates.MakeNewCard))
                {
                    alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                    {
                        TextSize = 12,
                        Title = GameFacade.Strings["UIText", "203", "10"], // "Save current panel"
                        Message = SaveChangesPrompt,
                        Alignment = TextAlignment.Center,
                        TextEntry = false,
                        Buttons = new UIAlertButton[] {
                            new UIAlertButton (UIAlertButtonType.Yes, ((btn1) =>
                            {
                                SaveNewCard();
                                SwitchToEditGame();
                                UIScreen.RemoveDialog(alert);
                            })),
                            new UIAlertButton (UIAlertButtonType.No, ((btn2) =>
                            {
                                CancelNewCard();
                                SwitchToEditGame();
                                EnableButtons();
                                UIScreen.RemoveDialog(alert);
                            }))
                        }
                    }, true);
                }
                else
                {
                    alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                    {
                        TextSize = 12,
                        Title = GameFacade.Strings["UIText", "203", "10"], // "Save current panel"
                        Message = SaveChangesPrompt,
                        Alignment = TextAlignment.Center,
                        TextEntry = false,
                        Buttons = new UIAlertButton[] {
                            new UIAlertButton (UIAlertButtonType.Yes, ((btn1) =>
                            {
                                SaveCardChanges();
                                SwitchToEditGame();
                                EnableButtons();
                                UIScreen.RemoveDialog(alert);
                            })),
                            new UIAlertButton (UIAlertButtonType.No, ((btn2) =>
                            {
                                CancelEditCard();
                                SwitchToEditGame();
                                EnableButtons();
                                UIScreen.RemoveDialog(alert);
                            }))
                        }
                    }, true);
                }
            }
            else
            {
                SwitchToEditGame();
                EnableButtons();
            }
        }

        private void NewCardBtnHandler(UIElement target)
        {
            UIAlert alert = null;
            if (State.Equals(UIGameCompDrawACardPluginEODStates.MakeNewCard))
            {
                // do nothing
            }
            else if (State.Equals(UIGameCompDrawACardPluginEODStates.EditSingleCard))
            {
                DisableButtons();
                if (CurrentCardInfoChanged)
                {
                    alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                    {
                        TextSize = 12,
                        Title = GameFacade.Strings["UIText", "203", "10"], // "Save current panel"
                        Message = SaveChangesPrompt,
                        Alignment = TextAlignment.Center,
                        TextEntry = false,
                        Buttons = new UIAlertButton[] {
                            new UIAlertButton (UIAlertButtonType.Yes, ((btn1) =>
                            {
                                SaveCardChanges();
                                SwitchToMakeNewCard();
                                EnableButtons();
                                UIScreen.RemoveDialog(alert);
                            })),
                            new UIAlertButton (UIAlertButtonType.No, ((btn2) =>
                            {
                                CancelEditCard();
                                SwitchToMakeNewCard();
                                EnableButtons();
                                UIScreen.RemoveDialog(alert);
                            }))
                        }
                    }, true);
                }
                else
                {
                    SwitchToMakeNewCard();
                    EnableButtons();
                }
            }
            else
            {
                DisableButtons();
                SwitchToMakeNewCard();
                EnableButtons();
            }
        }

        private void CardClickedHandler(UIElement list)
        {
            DisableButtons();
            BetterLabel.CurrentText = LoadingString;
            CurrentCardIndex = SelectCardList.SelectedIndex;
            Send("DrawCard_Goto_Card", new byte[] { (byte)CurrentCardIndex });
            SwitchToEditCard();
        }

        private void UpdateDeckHandler(string evt, byte[] deckListBoxData)
        {
            if (deckListBoxData[0] == 0)
            {
                // no more cards
                SelectCardList.Items = new List<UIListBoxItem>(0);
                SelectCardList.Visible = false;
                Remove(SelectCardList);
                BetterLabelDeckString = ZeroCardMessage;
                if (ManageGameBack != null)
                    SwitchToEditDeck();
                BetterLabel.CurrentText = BetterLabelDeckString;
                EnableButtons();
                return;
            }
            // StringSplitOptions.RemoveEmptyEntries because the last one will always be empty, and no other string will ever be empty
            string[] replacedStringsArray = VMEODGameCompDrawACardData.DeserializeStrings(deckListBoxData);
            CurrentDeckListBox = DefaultCardTextReplacer(replacedStringsArray);
            string[] regexSplit;
            var list = new List<UIListBoxItem>(CurrentDeckListBox.Length);
            for (var index = 0; index < CurrentDeckListBox.Length; index++)
            {
                // new lines won't be helpful in this UIListBox
                regexSplit = CurrentDeckListBox[index].Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                // finally in order to pass the W-test (string full of widest character: capital W), 27 chars max
                if (regexSplit[0].Length > 27)
                    list.Add(new UIListBoxItem(null, regexSplit[0].Substring(0, 27)));
                else
                    list.Add(new UIListBoxItem(null, regexSplit[0]));
            }
            SelectCardList.Items = list;
            try
            {
                Remove(SelectCardList);
            }
            catch (Exception) { }
            Add(SelectCardList);
        }

        private void DeckNumbersHandler(string evt, byte[] numbersData)
        {
            if (numbersData == null) return;
            var split = VMEODGameCompDrawACardData.DeserializeStrings(numbersData);
            if (split.Length < 2) return;
            if (Int32.TryParse(split[0], out TotalUniqueCards))
                UniqueCardsTextEdit.CurrentText = "" + TotalUniqueCards;
            if (Int32.TryParse(split[1], out GrandTotalCards))
                NumCardsTextEdit.CurrentText = "" + GrandTotalCards;
        }

        private void UpdateCurrentCardHandler(string evt, byte[] cardAndFrequencyData)
        {
            if (cardAndFrequencyData == null)
            {
                CurrentCardTextFromServer = GameFacade.Strings["UIText", "203", "48"]; // "There are no cards to draw from this deck."
            }
            else {
                BetterLabelDeckString = SelectCardText.Caption;
                var split = VMEODGameCompDrawACardData.DeserializeStrings(cardAndFrequencyData);
                if (split.Length < 1) return;

                // check for default card enum type, and replace with the appropriate string table text
                CurrentCardTextFromServer = DefaultCardTextReplacer(split[0]);

                // get the card frequency
                int newFrequency;
                if (Int32.TryParse(split[1], out newFrequency))
                    CurrentCardFrequencyFromServer = newFrequency;
                else
                    CurrentCardFrequencyFromServer = 1;
            }

            // update the textfields right away depending on the state and enable buttons since this is the response from the server
            switch (State)
            {
                case UIGameCompDrawACardPluginEODStates.EditDeck:
                    {
                        BetterLabel.CurrentText = BetterLabelDeckString;
                        EnableButtons();
                        break;
                    }
                case UIGameCompDrawACardPluginEODStates.EditGame:
                    {
                        BetterLabel.CurrentText = GameFacade.Strings["UIText", "203", "2"]; // "Edit game info"
                        EnableButtons();
                        break;
                    }
                case UIGameCompDrawACardPluginEODStates.EditSingleCard:
                    {
                        BetterLabel.CurrentText = GameFacade.Strings.GetString("f112", "4"); // "Edit card"
                        EditCardTextEdit.CurrentText = CurrentCardTextFromServer;
                        NumDrawChancesTextEdit.CurrentText = "" + CurrentCardFrequencyFromServer;
                        CurrentCardInfoChanged = false;
                        NumCardsChanged = false;
                        EnableButtons();
                        break;
                    }
                case UIGameCompDrawACardPluginEODStates.MakeNewCard:
                    {
                        BetterLabel.CurrentText = GameFacade.Strings["UIText", "203", "4"]; // "New card"
                        EnableButtons();
                        break;
                    }
                case UIGameCompDrawACardPluginEODStates.ViewSingleCard:
                    {
                        ViewDrawInfoTextEdit.CurrentText = CurrentCardTextFromServer;
                        break;
                    }
            }
        }

        private void DeckInfoUI(string evt, byte[] gameData)
        {
            State = UIGameCompDrawACardPluginEODStates.ViewDeck;

            // add background
            ManageGameBack = Script.Create<UIImage>("ManageGameBack");
            AddAt(0, ManageGameBack);
            
            // hide deck info
            SelectCardList.Visible = false;
            BetterLabel.Visible = false;

            // hide edit card info
            EditCardTextEdit.Visible = false;
            SpinnerMinusBtn.Visible = false;
            SpinnerPlusBtn.Visible = false;
            DrawChancesText.Visible = false;
            NumDrawChancesTextEdit.Visible = false;

            // hide next and prev card buttons
            EditCardPrevBtn.Visible = false;
            EditCardNextBtn.Visible = false;

            // show game info
            NumCardsText.Visible = true;
            NumCardsTextEdit.Visible = true;
            UniqueCardsText.Visible = true;
            UniqueCardsTextEdit.Visible = true;
            ManageGameTitleTextEdit.Visible = true;
            ManageGameTitleTextEdit.Mode = UITextEditMode.ReadOnly;
            ManageGameDescripTextEdit.Visible = true;
            ManageGameDescripTextEdit.Mode = UITextEditMode.ReadOnly;
            ManageGameDescripTextEdit.InitDefaultSlider();

            // hide new card info
            NewCardTextEdit.Visible = false;

            // hide save and delete button
            DeleteBtn.Visible = false;
            SaveBtn.Visible = false;

            // hide game/card buttons
            EditCardBtn.Visible = false;
            EditGameBtn.Visible = false;
            NewCardBtn.Visible = false;

            // Perfectionist tweaks
            ManageGameBack.X -= 20;
            NumCardsText.X -= 20;
            NumCardsTextEdit.X -= 20;
            UniqueCardsText.X -= 20;
            UniqueCardsTextEdit.X -= 20;
            ManageGameTitleTextEdit.X -= 20;
            ManageGameDescripTextEdit.X -= 20;

            UpdateGameData(gameData);

            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Tips = EODTextTips.None,
                Timer = EODTimer.None,
            });
        }

        private void DrawViewCardUI(string evt, byte[] cardData)
        {
            State = UIGameCompDrawACardPluginEODStates.ViewSingleCard;

            // add backgrounds
            ViewDrawInfoBack = Script.Create<UIImage>("ViewDrawInfoBack");
            AddAt(0, ViewDrawInfoBack);
            ViewDrawInfoTextEdit.Visible = true;
            ViewDrawInfoTextEdit.InitDefaultSlider();

            // hide deck info
            SelectCardList.Visible = false;
            BetterLabel.Visible = false;

            // hide edit card info
            EditCardTextEdit.Visible = false;
            SpinnerMinusBtn.Visible = false;
            SpinnerPlusBtn.Visible = false;
            DrawChancesText.Visible = false;
            NumDrawChancesTextEdit.Visible = false;

            // hide next and prev card buttons
            EditCardPrevBtn.Visible = false;
            EditCardNextBtn.Visible = false;

            // show game info
            NumCardsText.Visible = false;
            NumCardsTextEdit.Visible = false;
            UniqueCardsText.Visible = false;
            UniqueCardsTextEdit.Visible = false;
            ManageGameTitleTextEdit.Visible = false;
            ManageGameDescripTextEdit.Visible = false;

            // hide new card info
            NewCardTextEdit.Visible = false;

            // hide save and delete button
            DeleteBtn.Visible = false;
            SaveBtn.Visible = false;

            // hide game/card buttons
            EditCardBtn.Visible = false;
            EditGameBtn.Visible = false;
            NewCardBtn.Visible = false;

            // update the card
            UpdateCurrentCardHandler("", cardData);

            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Tips = EODTextTips.None,
                Timer = EODTimer.None,
            });
        }

        private void ManageDeckUI(string evt, byte[] gameData)
        {
            // add backgrounds, even more tweaks
            SelectCardBack = Script.Create<UIImage>("SelectCardBack");
            SelectCardBack.X -= 8;
            SelectCardBack.Y -= 9;
            SelectCardList.X -= 8;
            SelectCardList.Y += 1;
            SelectCardList.InitDefaultSlider();
            SelectCardList.Slider.X = SelectCardBack.X + SelectCardBack.Width;
            SelectCardList.Slider.Y += 3;
            AddAt(0, SelectCardBack);

            if (!BetterLabel.CurrentText.Equals(ZeroCardMessage))
            {
                BetterLabel.CurrentText = BetterLabelDeckString;
                BetterLabelDeckString = SelectCardText.Caption;
            }

                ManageGameBack = Script.Create<UIImage>("ManageGameBack");
            AddAt(1, ManageGameBack);
            ManageGameBack.Visible = false;

            EditCardBack = Script.Create<UIImage>("EditCardBack");
            AddAt(2, EditCardBack);
            EditCardBack.Visible = false;

            NewCardBack = Script.Create<UIImage>("NewCardBack");
            AddAt(3, NewCardBack);
            NewCardBack.Visible = false;

            // hide elements from deck info
            ViewDrawInfoTextEdit.Visible = false;

            // sliders plus tweaks
            ManageGameDescripTextEdit.InitDefaultSlider();
            EditCardTextEdit.InitDefaultSlider();
            EditCardTextEdit.Y += 6;
            EditCardTextEdit.Slider.Y += 2;
            NewCardTextEdit.InitDefaultSlider();
            NewCardTextEdit.Y += 6;
            NewCardTextEdit.Slider.Y += 2;

            SwitchToEditDeck();
            UpdateGameData(gameData);
            EnableButtons();
        }

        private void SaveNewCard()
        {
            string newCardText = NewCardTextEdit.CurrentText;
            if (newCardText.Length > 257)
                newCardText = newCardText.Substring(0, 257);
            int newFrequency = 100;
            if (Int32.TryParse(NumDrawChancesTextEdit.CurrentText, out newFrequency))
            {
                if (newFrequency < 1)
                    newFrequency = 1;
                if (newFrequency > 99)
                    newFrequency = 99;
            }
            else
                newFrequency = 1;
            NewCardTextEdit.CurrentText = "";
            CurrentCardInfoChanged = false;
            NumCardsChanged = false;
            BetterLabel.CurrentText = LoadingString;
            Send("DrawCard_Add_Card", VMEODGameCompDrawACardData.SerializeStrings(newCardText, newFrequency + ""));
        }

        private void SaveCardChanges()
        {
            if (NumCardsChanged)
            {
                int newFrequency = 100;
                if (Int32.TryParse(NumDrawChancesTextEdit.CurrentText, out newFrequency))
                {
                    if (newFrequency != CurrentCardFrequencyFromServer)
                    {
                        if ((newFrequency > 0) && (newFrequency < 100))
                            Send("DrawCard_Edit_Frequency", new byte[] { (byte)newFrequency });
                    }
                }
                NumCardsChanged = false;
            }
            if (CurrentCardInfoChanged)
            {
                string newCardText;
                newCardText = EditCardTextEdit.CurrentText;
                if (!newCardText.Equals(CurrentCardTextFromServer))
                {
                    if (newCardText.Length > 257)
                        newCardText = newCardText.Substring(0, 257);
                    Send("DrawCard_Edit_Card", VMEODGameCompDrawACardData.SerializeStrings(newCardText));
                }
                CurrentCardInfoChanged = false;
            }
        }

        private void DeleteCardHandler()
        {
            // deleting last card
            if (TotalUniqueCards == 1)
            {
                SelectCardList.SelectedIndex = CurrentCardIndex = -1;
            }
            // make sure index is in bounds since capacity is decrementing
            else if (CurrentCardIndex == TotalUniqueCards - 1)
            {
                CurrentCardIndex--;
                SelectCardList.SelectedIndex = CurrentCardIndex;
            }
            BetterLabel.CurrentText = LoadingString;
            Send("DrawCard_Delete_Card", new byte[] { 0 });
        }

        private void GotoPreviousCard()
        {
            if (CurrentCardIndex == 0)
                CurrentCardIndex = TotalUniqueCards - 1;
            else
                CurrentCardIndex--;
            SelectCardList.SelectedIndex = CurrentCardIndex;
            BetterLabel.CurrentText = LoadingString;
            Send("DrawCard_Goto_Card", new byte[] { (byte)CurrentCardIndex });
        }

        private void GotoNextCard()
        {
            if (CurrentCardIndex == TotalUniqueCards - 1)
                CurrentCardIndex = 0;
            else
                CurrentCardIndex++;
            SelectCardList.SelectedIndex = CurrentCardIndex;
            BetterLabel.CurrentText = LoadingString;
            Send("DrawCard_Goto_Card", new byte[] { (byte)CurrentCardIndex });
        }

        private void CancelNewCard()
        {
            NewCardTextEdit.CurrentText = "";
            CurrentCardInfoChanged = false;
            NumCardsChanged = false;
        }

        private void CancelEditCard()
        {
            if (CurrentCardInfoChanged)
            {
                EditCardTextEdit.CurrentText = CurrentCardTextFromServer;
                CurrentCardInfoChanged = false;
            }
            if (NumCardsChanged)
            {
                NumDrawChancesTextEdit.CurrentText = CurrentCardFrequencyFromServer + "";
                NumCardsChanged = false;
            }
        }

        private void SwitchToEditGame()
        {
            State = UIGameCompDrawACardPluginEODStates.EditGame;

            // update text for BetterLabel
            if (!BetterLabel.CurrentText.Equals(LoadingString))
                BetterLabel.CurrentText = GameFacade.Strings["UIText", "203", "2"]; // "Edit game info"

            // hide deck info
            SelectCardBack.Visible = false;
            SelectCardList.Visible = false;
            SelectCardList.Slider.Visible = false;

            // hide edit card info
            EditCardBack.Visible = false;
            EditCardTextEdit.Visible = false;
            SpinnerMinusBtn.Visible = false;
            SpinnerPlusBtn.Visible = false;
            DrawChancesText.Visible = false;
            NumDrawChancesTextEdit.Visible = false;

            // hide next and prev card buttons
            EditCardPrevBtn.Visible = false;
            EditCardNextBtn.Visible = false;

            // show game info
            ManageGameBack.Visible = true;
            NumCardsText.Visible = true;
            NumCardsTextEdit.Visible = true;
            UniqueCardsText.Visible = true;
            UniqueCardsTextEdit.Visible = true;
            ManageGameTitleTextEdit.Visible = true;
            ManageGameDescripTextEdit.Visible = true;

            // hide new card info
            NewCardBack.Visible = false;
            NewCardTextEdit.Visible = false;

            // show save button
            SaveBtn.Visible = true;

            // hide delete button
            DeleteBtn.Visible = false;

            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 1,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Tips = EODTextTips.Short,
                Timer = EODTimer.None
            });
        }

        private void SwitchToEditDeck()
        {
            State = UIGameCompDrawACardPluginEODStates.EditDeck;

            // update text for BetterLabel
            if (!BetterLabel.CurrentText.Equals(LoadingString))
                BetterLabel.CurrentText = BetterLabelDeckString;

            // show deck info
            SelectCardBack.Visible = true;
            SelectCardList.Visible = true;
            SelectCardList.Slider.Visible = true;

            // hide edit card info
            EditCardBack.Visible = false;
            EditCardTextEdit.Visible = false;
            SpinnerMinusBtn.Visible = false;
            SpinnerPlusBtn.Visible = false;
            DrawChancesText.Visible = false;
            NumDrawChancesTextEdit.Visible = false;

            // hide next and prev card buttons
            EditCardPrevBtn.Visible = false;
            EditCardNextBtn.Visible = false;

            // hide game info
            ManageGameBack.Visible = false;
            NumCardsText.Visible = false;
            NumCardsTextEdit.Visible = false;
            UniqueCardsText.Visible = false;
            UniqueCardsTextEdit.Visible = false;
            ManageGameTitleTextEdit.Visible = false;
            ManageGameDescripTextEdit.Visible = false;

            // hide new card info
            NewCardBack.Visible = false;
            NewCardTextEdit.Visible = false;

            // hide save and delete button
            DeleteBtn.Visible = false;
            SaveBtn.Visible = false;

            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Tips = EODTextTips.Short,
                Timer = EODTimer.None
            });
        }

        private void SwitchToEditCard()
        {
            State = UIGameCompDrawACardPluginEODStates.EditSingleCard;

            // update text for BetterLabel
            if (!BetterLabel.CurrentText.Equals(LoadingString))
                BetterLabel.CurrentText = GameFacade.Strings.GetString("f112", "4"); // "Edit card"

            // hide deck info
            SelectCardBack.Visible = false;
            SelectCardList.Visible = false;
            SelectCardList.Slider.Visible = false;

            // show edit card info
            EditCardBack.Visible = true;
            EditCardTextEdit.Visible = true;
            SpinnerMinusBtn.Visible = true;
            SpinnerPlusBtn.Visible = true;
            DrawChancesText.Visible = true;
            NumDrawChancesTextEdit.Visible = true;

            // show next and prev card buttons
            EditCardPrevBtn.Visible = true;
            EditCardNextBtn.Visible = true;

            // hide game info
            ManageGameBack.Visible = false;
            NumCardsText.Visible = false;
            NumCardsTextEdit.Visible = false;
            UniqueCardsText.Visible = false;
            UniqueCardsTextEdit.Visible = false;
            ManageGameTitleTextEdit.Visible = false;
            ManageGameDescripTextEdit.Visible = false;

            // hide new card info
            NewCardBack.Visible = false;
            NewCardTextEdit.Visible = false;

            // show save and delete button
            DeleteBtn.Visible = true;
            SaveBtn.Visible = true;
            
            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 2,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Tips = EODTextTips.Short,
                Timer = EODTimer.None
            });
        }

        private void SwitchToMakeNewCard()
        {
            if (TotalUniqueCards == VMEODGameCompDrawACardPlugin.MAXIMUM_UNIQUE_CARDS)
            {
                UIAlert alert = null;
                alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                {
                    TextSize = 12,
                    Title = GameFacade.Strings["UIText", "203", "10"], // "Save current panel"
                    Message = GameFacade.Strings.GetString("f112", "5"), // "Error: You have already saved the maximum number of individual cards."
                    Alignment = TextAlignment.Center,
                    TextEntry = false,
                    Buttons = new UIAlertButton[] {
                            new UIAlertButton (UIAlertButtonType.OK, ((btn1) =>
                            {
                                SwitchToEditDeck();
                                UIScreen.RemoveDialog(alert);
                            }))
                        }
                }, true);
                return;
            }

            State = UIGameCompDrawACardPluginEODStates.MakeNewCard;

            // update text for BetterLabel
            if (!BetterLabel.CurrentText.Equals(LoadingString))
                BetterLabel.CurrentText = GameFacade.Strings["UIText", "203", "4"]; // "New card"

            // hide deck info
            SelectCardBack.Visible = false;
            SelectCardList.Visible = false;
            SelectCardList.Slider.Visible = false;

            // show edit card info
            EditCardBack.Visible = false;
            EditCardTextEdit.Visible = false;
            SpinnerMinusBtn.Visible = true;
            SpinnerPlusBtn.Visible = true;
            DrawChancesText.Visible = true;
            NumDrawChancesTextEdit.Visible = true;
            NumDrawChancesTextEdit.CurrentText = "" + 1;

            // hide next and prev card buttons
            EditCardPrevBtn.Visible = false;
            EditCardNextBtn.Visible = false;

            // hide game info
            ManageGameBack.Visible = false;
            NumCardsText.Visible = false;
            NumCardsTextEdit.Visible = false;
            UniqueCardsText.Visible = false;
            UniqueCardsTextEdit.Visible = false;
            ManageGameTitleTextEdit.Visible = false;
            ManageGameDescripTextEdit.Visible = false;

            // show new card info
            NewCardBack.Visible = true;
            NewCardTextEdit.Visible = true;

            // new card could have no text, so allow it to be saved
            CurrentCardInfoChanged = true;

            // show save and delete button
            DeleteBtn.Visible = true;
            SaveBtn.Visible = true;

            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 2,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Tips = EODTextTips.Short,
                Timer = EODTimer.None
            });
        }

        private string[] DefaultCardTextReplacer(string[] rawListBoxStrings)
        {
            string[] processedStrings = new string[rawListBoxStrings.Length];
            for (var index = 0; index < rawListBoxStrings.Length; index++)
            {
                processedStrings[index] = DefaultCardTextReplacer(rawListBoxStrings[index]);
                // truncate each string to fit in the UIListBox
                if (processedStrings[index].Length > 40)
                    processedStrings[index] = processedStrings[index].Substring(0, 40);
            }
            return processedStrings;
        }

        private string DefaultCardTextReplacer(string rawString)
        {
            string replacedString = rawString;
            int stringOffSetValue = 20;
            VMEODGameCompDrawACardTypes type;
            if ((rawString.Length > 0) && (rawString[0].Equals('V')) && (Enum.TryParse(rawString, out type)))
            {
                // In order to avoid trying to save blank strings to the server, this enum replaces all empty strings sent to the server
                if (type == VMEODGameCompDrawACardTypes.VMEODGameCompDrawACardCustom)
                    replacedString = "";
                // The text of the card matches a default enum, the value of which, when added to an offset value of 20, corresponds to a string in the table
                else
                {
                    stringOffSetValue += (byte)type;
                    replacedString = GameFacade.Strings["UIText", "203", stringOffSetValue + ""]; // any default card string 21-39
                }
            }
            return replacedString;
        }

        private void SaveNewGameData()
        {
            if ((!ManageGameTitleTextEdit.CurrentText.Equals(CurrentGameTitle)) || (!ManageGameDescripTextEdit.CurrentText.Equals(CurrentGameDescription)))
            {
                CurrentGameTitle = ManageGameTitleTextEdit.CurrentText;
                CurrentGameDescription = ManageGameDescripTextEdit.CurrentText;
                Send("DrawCard_Edit_Game", VMEODGameCompDrawACardData.SerializeStrings(CurrentGameTitle, CurrentGameDescription));
            }
            GameInfoChanged = false;
        }

        private void UpdateGameData(byte[] gameDataByteArray)
        {
            // 3 characters are required to make both the title and description valid, as they are separated by a % for split parsing
            if (gameDataByteArray == null)
            {
                ManageGameTitleTextEdit.CurrentText = CurrentGameTitle = VMEODGameCompDrawACardPlugin.DEFAULT_GAME_TITLE;
                // "This deck of cards is completely customizable.  You can edit this text to make your own game rules."
                ManageGameDescripTextEdit.CurrentText = CurrentGameDescription = GameFacade.Strings["UIText", "203", "20"];
                return;
            }
            var split = VMEODGameCompDrawACardData.DeserializeStrings(gameDataByteArray);
            if (split.Length < 2)
            {
                ManageGameTitleTextEdit.CurrentText = CurrentGameTitle = VMEODGameCompDrawACardPlugin.DEFAULT_GAME_TITLE;
                // "This deck of cards is completely customizable.  You can edit this text to make your own game rules."
                ManageGameDescripTextEdit.CurrentText = CurrentGameDescription = GameFacade.Strings["UIText", "203", "20"];
            }
            else
            {
                // check for the flag that the game title is blank
                split[0] = DefaultCardTextReplacer(split[0]);
                // check for the flag that the game description is blank
                split[1] = DefaultCardTextReplacer(split[1]);

                // update the UITextEdits
                ManageGameTitleTextEdit.CurrentText = CurrentGameTitle = split[0];

                // this means the user has not changed the description at all yet
                if (CurrentGameTitle.Equals(VMEODGameCompDrawACardPlugin.DEFAULT_GAME_TITLE))
                    // "This deck of cards is completely customizable.  You can edit this text to make your own game rules."
                    ManageGameDescripTextEdit.CurrentText = CurrentGameDescription = GameFacade.Strings["UIText", "203", "20"];
                else
                    ManageGameDescripTextEdit.CurrentText = CurrentGameDescription = split[1];
            }
            if (State.Equals(UIGameCompDrawACardPluginEODStates.ViewDeck))
                ManageGameDescripTextEdit.Slider.X -= 20;
        }
        private void DisableButtons()
        {
            EditCardPrevBtn.Disabled = true;
            EditCardNextBtn.Disabled = true;
            SpinnerMinusBtn.Disabled = true;
            SpinnerPlusBtn.Disabled = true;
            DeleteBtn.Disabled = true;
            SaveBtn.Disabled = true;
            EditCardBtn.Disabled = true;
            EditGameBtn.Disabled = true;
            NewCardBtn.Disabled = true;
            NumCardsTextEdit.Mode = UITextEditMode.ReadOnly;
            ManageGameDescripTextEdit.Mode = UITextEditMode.ReadOnly;
            ManageGameTitleTextEdit.Mode = UITextEditMode.ReadOnly;
            NewCardTextEdit.Mode = UITextEditMode.ReadOnly;
            EditCardTextEdit.Mode = UITextEditMode.ReadOnly;
            SelectCardList.OnDoubleClick -= CardClickedHandler;
        }
        private void EnableButtons()
        {
            EditCardPrevBtn.Disabled = false;
            EditCardNextBtn.Disabled = false;
            SpinnerMinusBtn.Disabled = false;
            SpinnerPlusBtn.Disabled = false;
            EditCardBtn.Disabled = false;
            EditGameBtn.Disabled = false;
            NewCardBtn.Disabled = false;
            NumCardsTextEdit.Mode = UITextEditMode.Editor;
            ManageGameDescripTextEdit.Mode = UITextEditMode.Editor;
            ManageGameTitleTextEdit.Mode = UITextEditMode.Editor;
            NewCardTextEdit.Mode = UITextEditMode.Editor;
            EditCardTextEdit.Mode = UITextEditMode.Editor;

            if (SelectCardList.Visible == true)
                SelectCardList.OnDoubleClick += CardClickedHandler;

            if ((State.Equals(UIGameCompDrawACardPluginEODStates.EditSingleCard)) || (State.Equals(UIGameCompDrawACardPluginEODStates.MakeNewCard)))
            {
                DeleteBtn.Disabled = false;
                if ((CurrentCardInfoChanged) || (NumCardsChanged))
                    SaveBtn.Disabled = false;
            }
            else if (State.Equals(UIGameCompDrawACardPluginEODStates.EditGame)) {
                if (GameInfoChanged)
                    SaveBtn.Disabled = false;
            }
        }
    }
    public enum UIGameCompDrawACardPluginEODStates : byte
    {
        ViewSingleCard = 0,
        ViewDeck = 1,
        EditGame = 2,
        EditDeck = 3,
        EditSingleCard = 4,
        MakeNewCard = 5
    }
}
