using System;
using System.Collections.Generic;
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.EODs.Handlers.Data;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    /*
     * Due to the complex nature of this class, comments will be very verbose.
     */
    public class VMEODGameCompDrawACardPlugin : VMEODHandler
    {
        private VMEODGameCompDrawACardData Data;
        private VMEODClient UserClient;
        private CarducopiaDrawACardGame Game;
        private bool DeckHasChanged;
        private Random DrawCard = new Random();
        private bool ResponseFromServer;
        private bool UIInitialized;
        private VMEODGameCompDrawACardModes Mode;

        // constants
        public const string DEFAULT_GAME_TITLE = "Tsomania Carducopia Card Game";
        public const int MAXIMUM_UNIQUE_CARDS = 300;

        public VMEODGameCompDrawACardPlugin(VMEODServer server) : base(server)
        {
            // create a default deck until data is retrieved from the server
            Game = new CarducopiaDrawACardGame();

            // Initialize Mode to the unused mode of the plugin
            Mode = VMEODGameCompDrawACardModes.CopyDeck;

            // event listeners & handlers
            BinaryHandlers["DrawCard_Delete_Card"] = DeleteCurrentCardHandler;
            BinaryHandlers["DrawCard_Edit_Frequency"] = SetCardFrequencyHandler;
            BinaryHandlers["DrawCard_Goto_Card"] = GotoCardHandler;
            BinaryHandlers["DrawCard_Edit_Card"] = EditCurrentCardHandler;
            BinaryHandlers["DrawCard_Add_Card"] = PushNewCardHandler;
            BinaryHandlers["DrawCard_Edit_Game"] = EditGameHandler;
            PlaintextHandlers["DrawCard_Close"] = CloseHandler;

            // try to get the data from the server
            server.vm.GlobalLink.LoadPluginPersist(server.vm, server.Object.PersistID, server.PluginID, (byte[] data) =>
            {
                lock (this)
                {
                    if (data == null)
                    {
                        Data = new VMEODGameCompDrawACardData();
                        ResponseFromServer = true;
                    }
                    else
                    {
                        Data = new VMEODGameCompDrawACardData(data);
                        // make a new game based on the received data
                        Game = new CarducopiaDrawACardGame(Data);
                        // update the UI
                        ResponseFromServer = true;
                    }
                }
                if (!UIInitialized)
                {
                    switch (Mode)
                    {
                        case VMEODGameCompDrawACardModes.Manage:
                            {
                                UIInitialized = true;
                                UserClient.Send("DrawCard_Update_Deck", GetDeckListBoxData());
                                UserClient.Send("DrawCard_Update_Deck_Numbers", GetCardNumberData());
                                UserClient.Send("DrawCard_Manage", GetGameInfoMessage());
                                break;
                            }
                        case VMEODGameCompDrawACardModes.ViewCurrent:
                            {
                                UIInitialized = true;
                                // send the card matching the last index
                                UserClient.Send("DrawCard_Drawn", GetCurrentCardData());
                                break;
                            }
                        case VMEODGameCompDrawACardModes.ViewDeck:
                            {
                                UIInitialized = true;
                                UserClient.Send("DrawCard_Update_Deck_Numbers", GetCardNumberData());
                                UserClient.Send("DrawCard_Info", GetGameInfoMessage());
                                break;
                            }
                        case VMEODGameCompDrawACardModes.Draw:
                            {
                                UIInitialized = true;
                                // randomly draw a card
                                int index = DrawCard.Next(0, Game.UniqueCardCount);
                                Game.GotoCard(index);
                                DeckHasChanged = true;

                                // send new card text to UI
                                UserClient.Send("DrawCard_Drawn", GetCurrentCardData());
                                break;
                            }
                    }
                }
            });
        }
        /*
         * When the client connects, the value of temp0 found in TempRegisters[0] determines which interaction is being executed on the object
         * Only the item's owner should be able to "Manage Deck"; where TempRegisters[0] == 0
         * @param:client — the VMEODClient sending the connect event
         */
        public override void OnConnection(VMEODClient client)
        {
            base.OnConnection(client);
            UserClient = client;
            
            // find what the user wants to do based on the value of temp0
            var args = UserClient.Invoker.Thread.TempRegisters;

            /* "Manage Deck" */
            if (args[0] == (short)VMEODGameCompDrawACardModes.Manage)
            {
                Mode = VMEODGameCompDrawACardModes.Manage;
                if (ResponseFromServer)
                {
                    UIInitialized = true;
                    UserClient.Send("DrawCard_Update_Deck", GetDeckListBoxData());
                    UserClient.Send("DrawCard_Update_Deck_Numbers", GetCardNumberData());
                    UserClient.Send("DrawCard_Manage", GetGameInfoMessage());
                }
                else
                    UserClient.Send("DrawCard_Please_Wait", new byte[] { (byte)Mode } );
            }

            /* "View Current Card" */
            else if (args[0] == (short)VMEODGameCompDrawACardModes.ViewCurrent)
            {
                Mode = VMEODGameCompDrawACardModes.ViewCurrent;
                if (ResponseFromServer)
                {
                    UIInitialized = true;
                    // send the card matching the last index
                    UserClient.Send("DrawCard_Drawn", GetCurrentCardData());
                }
                else
                    UserClient.Send("DrawCard_Please_Wait", new byte[] { (byte)Mode });
            }

            /* "View Deck Info" */
            else if (args[0] == (short)VMEODGameCompDrawACardModes.ViewDeck)
            {
                Mode = VMEODGameCompDrawACardModes.ViewDeck;
                if (ResponseFromServer)
                {
                    UIInitialized = true;
                    UserClient.Send("DrawCard_Update_Deck_Numbers", GetCardNumberData());
                    UserClient.Send("DrawCard_Info", GetGameInfoMessage());
                }
                else
                    UserClient.Send("DrawCard_Please_Wait", new byte[] { (byte)Mode });
            }

            /* "Draw a Card" */
            else //if (args[0] == (short)VMEODGameCompDrawACardModes.Draw)
            {
                Mode = VMEODGameCompDrawACardModes.Draw;
                if (ResponseFromServer)
                {
                    UIInitialized = true;
                    // randomly draw a card
                    int index = DrawCard.Next(0, Game.UniqueCardCount);
                    Game.GotoCard(index);
                    DeckHasChanged = true;

                    // send new card text to UI
                    UserClient.Send("DrawCard_Drawn", GetCurrentCardData());
                }
                else
                    UserClient.Send("DrawCard_Please_Wait", new byte[] { (byte)Mode });
            }
        }
        /*
         * Upon a disconnect event, check to see if the deck has changed by way of the appopriate boolean. If so all of the Game variables are copied into the
         * variables of the Data. It's important to note steps have been taken to ensure that no empty strings are copied.
         * @param:client — the VMEODClient sending the disconnect event
         */
        public override void OnDisconnection(VMEODClient client)
        {
            if (DeckHasChanged)
            {
                bool errorThrown = false;
                // get the game info and put it into the data
                Data.GameTitle = Game.GameTitle;
                Data.GameDescription = Game.GameDescription;
                Data.LastIndex = (byte)Game.CurrentCardIndex;
                Data.CardText = new List<string>(Game.Deck.Count);
                Data.EachCardsCount = new List<byte>(Game.Deck.Count);
                foreach (var card in Game.Deck)
                {
                    Data.CardText.Add(card.Text);
                    Data.EachCardsCount.Add(card.Frequency);
                }
                try
                {
                    lock (this) {
                        Server.vm.GlobalLink.SavePluginPersist(Server.vm, Server.Object.PersistID, Server.PluginID, Data.Save());
                    }
                }
                catch (Exception)
                {
                    errorThrown = true;
                    // don't crash the server
                }
                // Send special event to clear the "pluginCardDrawn" flag in the object so the "View Current Card" interaction disappears until next "Draw Card"
                // but only do so if a new card deck was indeed successfully saved to the server. This was created via object .pif file.
                if (!errorThrown)
                    client.SendOBJEvent(new VMEODEvent(1));
            }
            base.OnDisconnection(client);
        }
        /*
         * Reponds to event called by client upon their clicking the UIButton:DeleteBtn while this evaluation is true in the UI:
         * (UIGameCompDrawACardPluginEODStates:State == UIGameCompDrawACardPluginEODStates.EditSingleCard)
         * @param evt:String containing the name of the event
         * @param args:Byte[] with a length of 1
         * @callbackEvent:"DrawCard_Update_Deck" updates the client UI's variable of UIListBox:SelectCardList - background
         * @callbackEvent:"DrawCard_Update_Card" updates the client UI's variable of String:CurrentCardText - re-enables user interactions with the UI
         * @callbackEvent:"DrawCard_Update_Deck_Numbers" updates the client UI's variables of int:TotalUniqueCards and int:GrandTotalCards - background
         * @callbackEvent:"DrawCard_Delete_Fail" alerts the client to the failure to delete the card - re-enables user interactions with the UI
         * @note: args[0] contains the index of the card to be deleted but it is not needed as the game will delete the card based on Game.CurrentCardIndex,
         * which had to be updated when the user navigated to the card that they see and wish to delete. If args[0] != Game.CurrentCardIndex, then this
         * command was sent from a modified client. I left this check in place in case there is action to be taken against modified clients in the future.
         */
        private void DeleteCurrentCardHandler(string evt, byte[] args, VMEODClient client)
        {
            // client has to be the object owner
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == UserClient.Avatar.PersistID);
            if (args[0] != Game.CurrentCardIndex)
            {
                // event sent from a modified client, instantly ban them and delete all of their money...or just Server.Disconnect(them)
            }
            if (isOwner)
            {
                if (Game.UniqueCardCount > 0)
                {
                    DeckHasChanged = true;
                    Game.DeleteCurrentCard();
                    // In this special case, if there are no cards in the deck the callback Event:"DrawCard_Update_Deck" will restore the user's ability
                    // to interact with the UI, but with some changes based on the fact that no cards remain in the deck-skipping Event:"DrawCard_Update_Card"
                    UserClient.Send("DrawCard_Update_Deck", GetDeckListBoxData());
                    if (Game.UniqueCardCount != 0)
                        UserClient.Send("DrawCard_Update_Card", GetCurrentCardData());
                    UserClient.Send("DrawCard_Update_Deck_Numbers", GetCardNumberData());
                }
                else
                {
                    // alert event: you can't delete this card
                    UserClient.Send("DrawCard_Delete_Fail", "");
                }
            }
            else
                // alert event: you can't delete this card
                UserClient.Send("DrawCard_Delete_Fail", "");
        }
        /*
         * Reponds to event called by client when they elect to save changes made to a new card, or and existing card whose frequency was changed in the
         * UITextEdit:NumDrawChancesTextEdit. The callback Event:"DrawCard_Update_Deck_Numbers" is sent in the background and does not affect the user's
         * continued interactions after electing to save.
         * @param evt:String containing the name of the event
         * @param newFrequency:Byte[] with a length of 1
         * @callbackEvent:"DrawCard_Update_Deck_Numbers" updates the client UI's variables of int:TotalUniqueCards and int:GrandTotalCards - background
         * @note: newFrequency[0] contains a byte that represents the new frequency that the card at Game.CurrentIndex should be set to in order to affect
         * its chances of being drawn. It must be greater than 0 and less than 100. This plugin does not support the preserving of cards with 0 frequency, and
         * due to the 2 character limit of the UITextEdit:NumDrawChancesTextEdit from which the value is taken, newFrequency[0] cannot be greater than 100.
         */
        private void SetCardFrequencyHandler(string evt, byte[] newFrequency, VMEODClient client)
        {
            // client has to be the object owner
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == UserClient.Avatar.PersistID);
            bool isValid = false;
            if ((newFrequency != null) && (newFrequency.Length > 0) && (newFrequency[0] > 0) && (newFrequency[0] < 100))
                isValid = true;
            else
            {
                // event sent from a modified client, instantly ban them and delete all of their money...or just Server.Disconnect(them)
            }
            if ((isOwner) && (isValid))
            {
                DeckHasChanged = true;
                Game.SetCurrentCardFrequency(newFrequency[0]);
            }
            UserClient.Send("DrawCard_Update_Deck_Numbers", GetCardNumberData());
        }
        /*
         * Responds to event called by client when they click UIButton:EditCardPrevBtn or UIButton:EditCardNextBtn while this evaluation is true in the UI:
         * (UIGameCompDrawACardPluginEODStates:State == UIGameCompDrawACardPluginEODStates.EditSingleCard) OR when the client double-clicks an element in
         * UIListBox:SelectCardList in the UI.
         * @param evt:String containing the name of the event
         * @param cardIndex:Byte Array with a length of 1
         * @callbackEvent:"DrawCard_Update_Card" updates the client UI's variable of String:CurrentCardText - re-enables user interactions with the UI
         * @note: arg[0] contains the byte to which to change Game.CurrentCardIndex by way of Game.GoToCard(), which also updates Game.CurrentCardText to be
         * used in the callback Event "DrawCard_Update_Card"
         */
        private void GotoCardHandler(string evt, byte[] cardIndex, VMEODClient client)
        {
            // client has to be the object owner
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == UserClient.Avatar.PersistID);
            bool isValid = false;
            if ((cardIndex != null) && (cardIndex[0] < Game.UniqueCardCount))
                isValid = true;
            if ((isOwner) && (isValid))
            {
                DeckHasChanged = true;
                Game.GotoCard(cardIndex[0]);
            }
            UserClient.Send("DrawCard_Update_Card", GetCurrentCardData());
        }
        /*
         * Responds to event called by client when they click UIButton:SaveBtn OR answering in the affirmitive on any UIAlert while this evaluation is true:
         * (UIGameCompDrawACardPluginEODStates:State == UIGameCompDrawACardPluginEODStates.EditSingleCard)
         * @param evt:String containing the name of the event
         * @param cardText:String containing the desired updated text of the card at Game.CurrentCardIndex
         * @note: Due to the MaxChars limit of 256 of the UITextEdit:EditCardTextEdit, cardText:String must not have a length greater than 256.
         * @note: No callback function is needed, which means the client's ability to interact with the UI is unaffected and not dependent on this handler.
         */
        private void EditCurrentCardHandler(string evt, byte[] cardTextArray, VMEODClient client)
        {
            // client has to be the object owner
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == UserClient.Avatar.PersistID);

            // validate the data
            bool isValid = false;
            string cardText = null;
            var strings = VMEODGameCompDrawACardData.DeserializeStrings(cardTextArray);
            if (strings != null)
                cardText = strings[0];
            if ((cardText != null) && (cardText.Length < 257))
                isValid = true;

            // If the user would like this card to be blank, replace the blank string with the name of the enum entry for custom.
            if (cardText.Length == 0)
                cardText = VMEODGameCompDrawACardTypes.VMEODGameCompDrawACardCustom.ToString();
            if ((isOwner) && (isValid))
            {
                if (!Game.CurrentCardText.Equals(cardText))
                {
                    DeckHasChanged = true;
                    Game.EditCurrentCard(cardText);
                    UserClient.Send("DrawCard_Update_Deck", GetDeckListBoxData());
                }
            }
        }
        /*
         * Responds to event called by client when they click UIButton:SaveBtn OR answering in the affirmitive on any UIAlert while this evaluation is true:
         * (UIGameCompDrawACardPluginEODStates:State == UIGameCompDrawACardPluginEODStates.EditSingleCard)
         * @param evt:String containing the name of the event
         * @param newCardTextAndFrequency:String containing the desired text and frequency of the card to be added to Game
         * @callbackEvent:"DrawCard_Update_Deck" updates the client UI's variable of UIListBox:SelectCardList - background
         * @callbackEvent:"DrawCard_Update_Card" updates the client UI's variable of String:CurrentCardText - re-enables user interactions with the UI
         * @callbackEvent:"DrawCard_Update_Deck_Numbers" updates the client UI's variables of int:TotalUniqueCards and int:GrandTotalCards - background
         * @note: Due to the MaxChars limit of 256 of the UITextEdit:NewCardTextEdit, split[0] must not have a length greater than 256.
         * @note: split[1] contains a string that represents the new frequency that the card at Game.CurrentIndex should be set to in order to affect
         * its chances of being drawn. It must be greater than 0 and less than 100. This plugin does not support the preserving of cards with 0 frequency, and
         * due to the 2 character limit of the UITextEdit:NumDrawChancesTextEdit from which the value is taken, (int)split[1] cannot be greater than 100.
         */
        private void PushNewCardHandler(string evt, byte[] newCardTextAndFrequencyArray, VMEODClient client)
        {
            if (newCardTextAndFrequencyArray == null)
                return;

            // client has to be the object owner
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == UserClient.Avatar.PersistID);

            // validate the data
            bool isValid = false;
            var split = VMEODGameCompDrawACardData.DeserializeStrings(newCardTextAndFrequencyArray);
            byte frequency = 100;
            if ((split.Length > 1) && (split[0].Length < 257))
            {
                if (Byte.TryParse(split[1], out frequency))
                    isValid = true;
                else
                {
                    frequency = 1;
                    isValid = true;
                }
                // If the user would like this card to be blank, replace the blank string with the name of the enum entry for custom.
                if (split[0].Length == 0)
                    split[0] = VMEODGameCompDrawACardTypes.VMEODGameCompDrawACardCustom.ToString();
            }
            if ((isOwner) && (isValid) && (Game.UniqueCardCount < MAXIMUM_UNIQUE_CARDS))
            {
                DeckHasChanged = true;
                Game.PushNewCard(split[0], frequency);
                UserClient.Send("DrawCard_Update_Deck", GetDeckListBoxData());
                UserClient.Send("DrawCard_Update_Deck_Numbers", GetCardNumberData());
            }
            UserClient.Send("DrawCard_Update_Card", GetCurrentCardData());
        }
        /*
         * Called on Event:"eod_close" ?
         * @param evt:String containing the name of the event
         * @param newCardTextAndFrequency:String containing the desired new name and description to be set as Game.GameTitle and Game.Description
         */
        private void EditGameHandler(string evt, byte[] gameTitleAndDescriptionByteArray, VMEODClient client)
        {
            if (gameTitleAndDescriptionByteArray == null)
                return;

            // client has to be the object owner
            bool isOwner = (((VMTSOObjectState)Server.Object.TSOState).OwnerID == UserClient.Avatar.PersistID);

            // validate the data
            bool isValid = false;
            string[] gameInfo = VMEODGameCompDrawACardData.DeserializeStrings(gameTitleAndDescriptionByteArray);
            if (gameInfo.Length > 1)
            {
                if ((gameInfo[0].Length < 53) && (gameInfo[1].Length < 257))
                {
                    isValid = true;
                    // If the user would like the game title to be blank, replace the blank string with the name of the enum entry for custom.
                    if (gameInfo[0].Length == 0)
                        gameInfo[0] = VMEODGameCompDrawACardTypes.VMEODGameCompDrawACardCustom.ToString();
                    // If the user would like the game description to be blank, replace the blank string with the name of the enum entry for custom.
                    if (gameInfo[1].Length == 0)
                        gameInfo[1] = VMEODGameCompDrawACardTypes.VMEODGameCompDrawACardCustom.ToString();
                }
            }
            if ((isOwner) && (isValid))
            {
                if (!Game.GameTitle.Equals(gameInfo[0]))
                {
                    DeckHasChanged = true;
                    Game.GameTitle = gameInfo[0];
                }
                if (!Game.GameDescription.Equals(gameInfo[1]))
                {
                    DeckHasChanged = true;
                    Game.GameDescription = gameInfo[1];
                }
            }
        }
        private void CloseHandler(string evt, string msg, VMEODClient client)
        {
            Server.Disconnect(client);
        }
        private byte[] GetCardNumberData()
        {
            return VMEODGameCompDrawACardData.SerializeStrings(Game.UniqueCardCount + "", Game.GrandTotalCardsCount + "");
        }
        private byte[] GetDeckListBoxData()
        {
            if (Game.UniqueCardCount == 0)
                return new byte[] { 0 };
            List<String> deckListBoxList = new List<string>();
            foreach (var card in Game.Deck)
            {
                // truncate each string to fit in the UIListBox but retain any possible Default strings in the enum below
                if (card.Text.Length > 40)
                    deckListBoxList.Add(card.Text.Substring(0, 40));
                else
                    deckListBoxList.Add(card.Text);
            }
            return VMEODGameCompDrawACardData.SerializeStrings(deckListBoxList.ToArray());
        }
        private byte[] GetCurrentCardData()
        {
            if ((Game.CurrentCardText == null) || (Game.CurrentCardText == ""))
                return null;
            else
                return VMEODGameCompDrawACardData.SerializeStrings(Game.CurrentCardText, Game.CurrentCardFrequency + "");
        }
        private byte[] GetGameInfoMessage()
        {
            return VMEODGameCompDrawACardData.SerializeStrings(Game.GameTitle, Game.GameDescription);
        }
    }
    /*
     * Not a LinkedList and not really a Tree. More of a Root Node Class containing Nodes leaves or children
     * Important Variables:
     * m_GameTitle:String - the user-defined name of the card game
     * m_GameDescription:String - the user-defined description of the card game
     * CurrentCardText:String - easily accessible string containing the current card's text, Deck[CurrentCardIndex].Text
     * m_UniqueCardCount:int - the number of possible cards that can be drawn in the game
     * m_GrandTotalCardsCount:int - the sum of all the frequencies of each unique card
     * CurrentCardFrequency:int - easily accessible int containing the current card's frequency, Deck[CurrentCardIndex].Frequency
     * CurrentCardIndex:int - int for navigating the Deck list
     * Deck - the List element containing the card Nodes, with each Node containing the data relevant to its card
     */
    class CarducopiaDrawACardGame
    {
        private string m_GameTitle;
        private string m_GameDescription;
        public string CurrentCardText;
        private int m_UniqueCardCount;
        private int m_GrandTotalCardsCount;
        public int CurrentCardFrequency;
        public int CurrentCardIndex;
        public List<CarducopiaCard> Deck;

        public CarducopiaDrawACardGame()
        {
            m_GameTitle = VMEODGameCompDrawACardPlugin.DEFAULT_GAME_TITLE;
            m_GameDescription = VMEODGameCompDrawACardPlugin.DEFAULT_GAME_TITLE;
            CurrentCardIndex = 0;
            Deck = new List<CarducopiaCard>(19);
            VMEODGameCompDrawACardTypes type;
            for (var index = 1; index <= Deck.Capacity; index++ )
            {
                type = (VMEODGameCompDrawACardTypes)Enum.ToObject(typeof(VMEODGameCompDrawACardTypes), (byte)index);
                PushNewCard(type.ToString());
            }
            UpdateCurrentCard();
        }

        public CarducopiaDrawACardGame(VMEODGameCompDrawACardData data)
        {
            m_GameTitle = data.GameTitle;
            m_GameDescription = data.GameDescription;
            Deck = new List<CarducopiaCard>();

            // data.CardText.Count SHOULD == data.EachCardsCount.Count, but just in case:
            int dataLimit = Math.Min(data.CardText.Count, data.EachCardsCount.Count);
            for (var index = 0; index < dataLimit; index++)
            {
                Deck.Add(new CarducopiaCard(data.CardText[index], data.EachCardsCount[index]));
            }
            m_UniqueCardCount = Deck.Count;
            CalculateGrandTotal();
            CurrentCardIndex = data.LastIndex;
            if (CurrentCardIndex > m_UniqueCardCount - 1)
                CurrentCardIndex = 0;
            UpdateCurrentCard();
        }
        public string GameTitle
        {
            get { return m_GameTitle; }
            set { m_GameTitle = value; }
        }
        public string GameDescription
        {
            get { return m_GameDescription; }
            set { m_GameDescription = value; }
        }
        public int UniqueCardCount
        {
            get { return m_UniqueCardCount; }
        }
        public int GrandTotalCardsCount
        {
            get { return m_GrandTotalCardsCount; }
        }
        public void PushNewCard(string cardText)
        {
            PushNewCard(cardText, 1);
        }
        public void PushNewCard(string cardText, byte frequency)
        {
            Deck.Add(new CarducopiaCard(cardText, frequency));
            m_UniqueCardCount = Deck.Count;
            CalculateGrandTotal();
            UpdateCurrentCard();
        }
        public void EditCurrentCard(string cardText)
        {
            if (m_UniqueCardCount == 0)
                return;
            Deck[CurrentCardIndex].Text = cardText;
            UpdateCurrentCard();
        }
        public void DeleteCurrentCard()
        {
            if (m_UniqueCardCount > 0)
            {
                Deck.RemoveAt(CurrentCardIndex);
                m_UniqueCardCount = Deck.Count;
                if (CurrentCardIndex > m_UniqueCardCount - 1)
                    GotoLastCard();
                else
                    UpdateCurrentCard();
                CalculateGrandTotal();
            }
        }
        public void SetCurrentCardFrequency(byte frequency)
        {
            if (m_UniqueCardCount == 0)
                return;
            CurrentCardFrequency = Deck[CurrentCardIndex].Frequency = frequency;
            CalculateGrandTotal();
        }
        public void GotoCard(int index)
        {
            if (index < m_UniqueCardCount)
            {
                CurrentCardIndex = index;
                UpdateCurrentCard();
            }
        }
        public void GotoLastCard()
        {
            if (m_UniqueCardCount == 0)
            {
                CurrentCardText = "";
                CurrentCardFrequency = 0;
            }
            else
            {
                CurrentCardIndex = m_UniqueCardCount - 1;
                UpdateCurrentCard();
            }
        }
        public void GotoFirstCard()
        {
            if (m_UniqueCardCount == 0)
            {
                CurrentCardText = "";
                CurrentCardFrequency = 0;
            }
            else
            {
                CurrentCardIndex = 0;
                UpdateCurrentCard();
            }
        }
        private void UpdateCurrentCard()
        {
            if (m_UniqueCardCount == 0)
            {
                CurrentCardText = "";
                CurrentCardFrequency = 0;
            }
            else
            {
                CurrentCardText = Deck[CurrentCardIndex].Text;
                CurrentCardFrequency = Deck[CurrentCardIndex].Frequency;
            }
        }
        private void CalculateGrandTotal()
        {
            m_GrandTotalCardsCount = 0;
            if (Deck.Count == 0) return;
            foreach (var card in Deck)
            {
                m_GrandTotalCardsCount += card.Frequency;
            }
        }
    }
    /*
     * Standard Node Class
     * Important Variables: m_Text:String = text of the card, m_Frequency:byte = frequency or number of card appearances in the "deck"
     */
    class CarducopiaCard
    {
        private string m_Text;
        private byte m_Frequency;

        public CarducopiaCard(string text, byte frequency)
        {
            m_Text = text;
            m_Frequency = frequency;
        }
        public string Text
        {
            get { return m_Text; }
            set { m_Text = value; }
        }
        public byte Frequency
        {
            get { return m_Frequency; }
            set { m_Frequency = value; }
        }
    }
    [Flags]
    public enum VMEODGameCompDrawACardTypes : byte
    {
        VMEODGameCompDrawACardCustom = 0,
        VMEODGameCompDrawACardDefaultOne = 1,
        VMEODGameCompDrawACardDefaultTwo = 2,
        VMEODGameCompDrawACardDefaultThree = 3,
        VMEODGameCompDrawACardDefaultFour = 4,
        VMEODGameCompDrawACardDefaultFive = 5,
        VMEODGameCompDrawACardDefaultSix = 6,
        VMEODGameCompDrawACardDefaultSeven = 7,
        VMEODGameCompDrawACardDefaultEight = 8,
        VMEODGameCompDrawACardDefaultNine = 9,
        VMEODGameCompDrawACardDefaultTen = 10,
        VMEODGameCompDrawACardDefaultEleven = 11,
        VMEODGameCompDrawACardDefaultTwelve = 12,
        VMEODGameCompDrawACardDefaultThirteen = 13,
        VMEODGameCompDrawACardDefaultFourteen = 14,
        VMEODGameCompDrawACardDefaultFifteen = 15,
        VMEODGameCompDrawACardDefaultSixteen = 16,
        VMEODGameCompDrawACardDefaultSeventeen = 17,
        VMEODGameCompDrawACardDefaultEighteen = 18,
        VMEODGameCompDrawACardDefaultNineteen = 19,
    }

    public enum VMEODGameCompDrawACardModes : short
    {
        Manage = 0,
        ViewCurrent = 1,
        ViewDeck = 2,
        Draw = 3,
        CopyDeck = 4 // no idea what this does
    }
}
