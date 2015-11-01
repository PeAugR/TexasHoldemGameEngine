﻿namespace TexasHoldem.Logic.Helpers
{
    using System.Collections.Generic;
    using System.Linq;

    using TexasHoldem.Logic.Cards;

    // TODO: Consider replacing LINQ with something more efficient (profile the code)
    // For performance considerations this class is not implemented using Chain of Responsibility
    public class HandEvaluator : IHandEvaluator
    {
        private const int ComparableCards = 5;

        public BestHand GetBestHand(ICollection<Card> cards)
        {
            var cardTypeCounts = new int[15]; // Ace = 14
            foreach (var card in cards)
            {
                cardTypeCounts[(int)card.Type]++;
            }

            var straightFlushCards = this.GetStraightFlushCards(cards);
            if (straightFlushCards != null)
            {
                return new BestHand(HandRankType.StraightFlush, straightFlushCards);
            }

            if (this.HasFourOfAKind(cardTypeCounts))
            {
                var fourOfAKindType =
                    cards.GroupBy(x => x.Type)
                        .Where(x => x.Count() == 4)
                        .Select(x => x.Key)
                        .OrderByDescending(x => x)
                        .FirstOrDefault();
                var bestCards = new List<CardType>
                                    {
                                        fourOfAKindType,
                                        fourOfAKindType,
                                        fourOfAKindType,
                                        fourOfAKindType,
                                        cards.Where(x => x.Type != fourOfAKindType).Max(x => x.Type)
                                    };

                return new BestHand(HandRankType.FourOfAKind, bestCards);
            }

            var pairTypes = this.GetPairTypes(cardTypeCounts);
            var threeOfAKindTypes = this.GetThreeOfAKinds(cards);
            if ((pairTypes.Count > 0 && threeOfAKindTypes.Count > 0) || threeOfAKindTypes.Count == 2)
            {
                var bestCards = new List<CardType>();
                if (pairTypes.Count > 0)
                {
                    bestCards.AddRange(Enumerable.Repeat(threeOfAKindTypes[0], 3));
                    bestCards.AddRange(Enumerable.Repeat(pairTypes[0], 2));
                }
                else if (threeOfAKindTypes.Count == 2)
                {
                    bestCards.AddRange(Enumerable.Repeat(threeOfAKindTypes[0], 3));
                    bestCards.AddRange(Enumerable.Repeat(threeOfAKindTypes[1], 2));
                }

                return new BestHand(HandRankType.FullHouse, bestCards);
            }

            var flushCards = this.GetFlushCards(cards);
            if (flushCards != null)
            {
                return new BestHand(HandRankType.Flush, flushCards);
            }

            var straightCards = this.GetStraightCards(cards);
            if (straightCards != null)
            {
                return new BestHand(HandRankType.Straight, straightCards);
            }

            if (threeOfAKindTypes.Count > 0)
            {
                var bestThreeOfAKindType = threeOfAKindTypes[0];
                var bestCards =
                    cards.Where(x => x.Type != bestThreeOfAKindType)
                        .OrderByDescending(x => x.Type)
                        .Select(x => x.Type)
                        .Take(ComparableCards - 3).ToList();
                bestCards.AddRange(Enumerable.Repeat(bestThreeOfAKindType, 3));

                return new BestHand(HandRankType.ThreeOfAKind, bestCards);
            }

            if (pairTypes.Count >= 2)
            {
                var bestCards = new List<CardType>
                                    {
                                        pairTypes[0],
                                        pairTypes[0],
                                        pairTypes[1],
                                        pairTypes[1],
                                        cards.Where(x => x.Type != pairTypes[0] && x.Type != pairTypes[1])
                                            .Max(x => x.Type)
                                    };
                return new BestHand(HandRankType.TwoPairs, bestCards);
            }

            if (pairTypes.Count == 1)
            {
                var bestCards =
                    cards.Where(x => x.Type != pairTypes[0])
                        .OrderByDescending(x => x.Type)
                        .Select(x => x.Type)
                        .Take(3).ToList();
                bestCards.Add(pairTypes[0]);
                bestCards.Add(pairTypes[0]);
                return new BestHand(HandRankType.Pair, bestCards);
            }
            else
            {
                var bestCards = cards.OrderByDescending(x => x.Type).Select(x => x.Type).Take(ComparableCards).ToList();
                return new BestHand(HandRankType.HighCard, bestCards);
            }
        }

        private IList<CardType> GetPairTypes(int[] cardTypeCounts)
        {
            var pairs = new List<CardType>();
            for (var i = cardTypeCounts.Length - 1; i >= 0; i--)
            {
                if (cardTypeCounts[i] == 2)
                {
                    pairs.Add((CardType)i);
                }
            }

            return pairs;
        }

        private IList<CardType> GetThreeOfAKinds(ICollection<Card> cards)
        {
            return cards.GroupBy(x => x.Type).Where(x => x.Count() == 3).Select(x => x.Key).OrderByDescending(x => x).ToList();
        }

        private bool HasFourOfAKind(int[] cardTypeCounts)
        {
            return cardTypeCounts.Any(x => x == 4);
        }

        private ICollection<CardType> GetStraightFlushCards(ICollection<Card> cards)
        {
            var flushes = cards.GroupBy(x => x.Suit).Where(x => x.Count() >= ComparableCards).Select(x => x.ToList());
            return flushes.Select(this.GetStraightCards).FirstOrDefault(straightCards => straightCards != null);
        }

        private ICollection<CardType> GetStraightCards(ICollection<Card> cards)
        {
            var straightCards = cards.Select(x => (int)x.Type).Distinct().ToList();
            if (straightCards.Contains((int)CardType.Ace))
            {
                straightCards.Add(1);
            }

            straightCards.Sort();
            var lastCard = int.MaxValue;
            var straightLenth = 0;
            for (var i = straightCards.Count - 1; i >= 0; i--)
            {
                if (straightCards[i] == lastCard - 1)
                {
                    straightLenth++;
                    if (straightLenth == ComparableCards)
                    {
                        var bestStraight = new List<CardType>();
                        for (int j = straightCards[i]; j <= straightCards[i] + ComparableCards - 1; j++)
                        {
                            if (j == 1)
                            {
                                bestStraight.Add(CardType.Ace);
                            }
                            else
                            {
                                bestStraight.Add((CardType)j);
                            }
                        }

                        return bestStraight;
                    }
                }
                else
                {
                    straightLenth = 1;
                }

                lastCard = straightCards[i];
            }

            return null;
        }

        private ICollection<CardType> GetFlushCards(ICollection<Card> cards)
        {
            var flushCardTypes = cards
                .GroupBy(x => x.Suit)
                .FirstOrDefault(x => x.Count() >= ComparableCards)
                ?.Select(x => x.Type)
                .OrderByDescending(x => x)
                .Take(ComparableCards)
                .ToList();

            return flushCardTypes;
        }
    }
}
