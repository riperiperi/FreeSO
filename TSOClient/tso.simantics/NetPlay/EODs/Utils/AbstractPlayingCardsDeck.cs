using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Utils
{
    public class AbstractPlayingCardsDeck
    {
        private CardQueue DrawStack;
        private PlayingCard _LastDrawnCard;
        private CardQueue DiscardStack;
        private bool UseDiscardStack;
        /*
         * A discard stack may be used in order to keep track of the remaining unused cards, otherwise drawn cards return to the bottom of play stack.
         */ 
        public AbstractPlayingCardsDeck(int numberOfDecks, bool useDiscardStack)
        {
            DrawStack = new CardQueue(numberOfDecks);
            if (useDiscardStack)
            {
                DiscardStack = new CardQueue();
                UseDiscardStack = useDiscardStack;
            }
            Shuffle(1);
        }
        public string LastDrawnCard
        {
            get {
                if (_LastDrawnCard != null)
                    return _LastDrawnCard.Value.ToString() + "_" + _LastDrawnCard.Suit.ToString();
                else
                    return null;
            }
        }
        public PlayingCard Draw()
        {
            lock (this)
            {
                _LastDrawnCard = DrawStack.Dequeue(!UseDiscardStack);
                if (_LastDrawnCard != null)
                {
                    if (UseDiscardStack)
                        DiscardStack.Enqueue(_LastDrawnCard);
                    return _LastDrawnCard;
                }
                return null;
            }
        }
        public string DrawString()
        {
            _LastDrawnCard = DrawStack.Dequeue(!UseDiscardStack);
            if (_LastDrawnCard != null)
            {
                if (UseDiscardStack)
                    DiscardStack.Enqueue(_LastDrawnCard);
                return _LastDrawnCard.Value.ToString() + "_" + _LastDrawnCard.Suit.ToString();
            }
            return null;
        }
        public PlayingCard[] Draw(int consecutiveDraws)
        {
            List<PlayingCard> cardList = new List<PlayingCard>();
            for (int draw = 0; draw < consecutiveDraws; draw++)
            {
                _LastDrawnCard = DrawStack.Dequeue(!UseDiscardStack);
                if (_LastDrawnCard != null)
                {
                    if (UseDiscardStack)
                        DiscardStack.Enqueue(_LastDrawnCard);
                    cardList.Add(_LastDrawnCard);
                }
            }
            if (cardList.Count > 0)
                return cardList.ToArray();
            return null;
        }
        public string[] DrawStrings(int consecutiveDraws)
        {
            List<string> cardList = new List<string>();
            for (int draw = 0; draw < consecutiveDraws; draw++)
            {
                _LastDrawnCard = DrawStack.Dequeue(!UseDiscardStack);
                if (_LastDrawnCard != null)
                {
                    if (UseDiscardStack)
                        DiscardStack.Enqueue(_LastDrawnCard);
                    cardList.Add(_LastDrawnCard.Value.ToString() + "_" + _LastDrawnCard.Suit.ToString());
                }
            }
            if (cardList.Count > 0)
                return cardList.ToArray();
            return null;
        }
        public void Shuffle(int iterations)
        {
            _LastDrawnCard = null;
            if (UseDiscardStack)
                DrawStack.AddCards(false, DiscardStack.RemoveAllCards());
            DrawStack.Shuffle(iterations);
        }
        public void ResetDeck()
        {
            _LastDrawnCard = null;
            if (UseDiscardStack)
                DrawStack.AddCards(true, DiscardStack.RemoveAllCards());
            else
                DrawStack.AddCards(true);
        }
        public void GetNewDeck(int numberOfDecks, bool useDiscardStack)
        {
            _LastDrawnCard = null;
            DrawStack = new CardQueue(numberOfDecks);
            if (useDiscardStack)
            {
                DiscardStack = new CardQueue();
                UseDiscardStack = useDiscardStack;
            }
        }
    }
    internal class CardQueue
    {
        private PlayingCard TopCard; // first, head
        private PlayingCard BottomCard; // last, tail
        private int TotalCards;
        private Random Random = new Random();

        public CardQueue(int numberOfDecks)
        {
            var valuesArray = Enum.GetValues(typeof(PlayingCardValues));
            var suitsArray = Enum.GetValues(typeof(PlayingCardSuits));
            for (int iterations = 0; iterations < numberOfDecks; iterations++)
            {
                foreach (PlayingCardSuits suit in suitsArray)
                {
                    foreach (PlayingCardValues value in valuesArray)
                        Enqueue(value, suit);
                }
            }
            Shuffle();
        }
        public CardQueue(params PlayingCard[] specificCards)
        {
            if (specificCards == null || specificCards.Length == 0) 
                return;
            foreach (var card in specificCards)
                Enqueue(card);
            Shuffle();
        }
        public void AddCards(bool shuffle, params PlayingCard[] cardsToAdd)
        {
            if (cardsToAdd != null && cardsToAdd.Length > 0)
            {
                foreach (var card in cardsToAdd)
                    Enqueue(new PlayingCard(card));
            }
            else if (TotalCards == 0)
                return;
            if (shuffle)
                Shuffle();
        }
        public void Enqueue(string cardValue, string cardSuit)
        {
            PlayingCardValues value;
            PlayingCardSuits suit;
            if (Enum.TryParse<PlayingCardValues>(cardValue, true, out value) && Enum.TryParse<PlayingCardSuits>(cardSuit, true, out suit))
                Enqueue(new PlayingCard(value, suit));
        }
        public void Enqueue(PlayingCardValues cardValue, PlayingCardSuits cardSuit)
        {
            Enqueue(new PlayingCard(cardValue, cardSuit));
        }
        public void Enqueue(PlayingCard card)
        {
            if (BottomCard != null)
                BottomCard.Next = card;
            else
                TopCard = card;
            BottomCard = card;
            TotalCards++;
        }
        public PlayingCard Dequeue(bool enqueueImmediately)
        {
            if (TopCard != null)
            {
                PlayingCard card = new PlayingCard(TopCard);
                TopCard = TopCard.Next;
                TotalCards--;
                if (enqueueImmediately)
                    Enqueue(card);
                return card;
            }
            else
                BottomCard = null;
            return null;
        }
        public void Shuffle(int iterations)
        {
            for (var iteration = 0; iteration < iterations; iteration++)
            {
                Shuffle();
            }
        }
        /* Richard Durstenfeld version of Fisher–Yates shuffle
         * -- To shuffle an array a of n elements (indices 0..n-1):
            for i from n−1 downto 1 do
            j ← random integer such that 0 ≤ j ≤ i
            exchange a[j] and a[i]
         */
        private void Shuffle()
        {
            if (TotalCards > 1)
            {
                var cardsArray = RemoveAllCards();
                PlayingCard tempCard = null;
                for (int index = cardsArray.Length - 1; index > 0; index--) {
                    int random = Random.Next(0, index + 1);
                    if (index != random)
                    {
                        tempCard = cardsArray[index];
                        cardsArray[index] = cardsArray[random];
                        cardsArray[random] = tempCard;
                    }
                }
                foreach (var card in cardsArray)
                    Enqueue(card);
            }
        }
        public PlayingCard[] RemoveAllCards()
        {
            List<PlayingCard> cards = new List<PlayingCard>();
            var card = Dequeue(false);
            while (card != null)
            {
                cards.Add(card);
                card = Dequeue(false);
            }
            return cards.ToArray();
        }
    }
    public class PlayingCard
    {
        private PlayingCardValues _Value;
        private PlayingCardSuits _Suit;
        private PlayingCard _Next;

        public PlayingCard(PlayingCardValues value, PlayingCardSuits suit)
        {
            _Value = value;
            _Suit = suit;
        }
        public PlayingCard(PlayingCard copy)
        {
            _Value = copy.Value;
            _Suit = copy.Suit;
        }
        public PlayingCardValues Value
        {
            get { return _Value; }
            set { _Value = value; }
        }
        public PlayingCardSuits Suit
        {
            get { return _Suit; }
            set { _Suit = value; }
        }
        public PlayingCard Next
        {
            get { return _Next; }
            set { _Next = value; }
        }
    }
    [Flags]
    public enum PlayingCardValues : byte // values are not specific to any type of card game
    {
        //Joker = 0
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13
    }
    [Flags]
    public enum PlayingCardSuits : byte
    {
        Clubs = 0,
        Diamonds = 1,
        Hearts = 2,
        Spades = 3
    }
}
