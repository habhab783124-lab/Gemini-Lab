#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Core.Time;
using GeminiLab.Modules.Apple;
using UnityEngine;
using Random = UnityEngine.Random;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// <see cref="ITarotService"/> 默认实现。移除每日限制，支持 session 驱动的选牌流程。
    /// </summary>
    public sealed class TarotService : ITarotService
    {
        public const int DefaultSessionCost = 8;

        private readonly TarotDeckSO _deck;
        private readonly IAppleService _apple;
        private readonly IGameClock _clock;
        private readonly EventBus? _eventBus;
        private readonly ITarotReadingBackend _readingBackend;

        public TarotService(
            TarotDeckSO deck,
            IAppleService apple,
            IGameClock clock,
            EventBus? eventBus,
            ITarotReadingBackend readingBackend)
        {
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _apple = apple ?? throw new ArgumentNullException(nameof(apple));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _eventBus = eventBus;
            _readingBackend = readingBackend ?? throw new ArgumentNullException(nameof(readingBackend));
        }

        public TarotDeckSO Deck => _deck;
        public int SessionCost => DefaultSessionCost;
        public bool CanCreateSession => _apple.Balance >= SessionCost;

        public TarotSession CreateSession(string? question)
        {
            if (!_apple.TrySpend(SessionCost))
            {
                Debug.LogWarning("[Tarot] 苹果不足，无法开始抽牌");
                return new TarotSession
                {
                    Question = question ?? string.Empty,
                    SessionDateIso = _clock.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }

            var session = new TarotSession
            {
                Question = question ?? string.Empty,
                SessionDateIso = _clock.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CandidateCards = PickRandomCards(11)
            };
            return session;
        }

        public TarotSession ShuffleCards(TarotSession session)
        {
            session.CandidateCards = PickRandomCards(11);
            session.PastCard = null;
            session.PresentCard = null;
            session.FutureCard = null;
            session.PickedCount = 0;
            session.Readings.Clear();
            session.RevealedSlotIndex = 0;
            return session;
        }

        public TarotSession PickCard(TarotSession session, TarotCardSO card)
        {
            if (session.PickedCount >= 3) return session;

            var slot = (TarotSlotPosition)session.PickedCount;
            var orientation = Random.value < 0.5f ? TarotOrientation.Upright : TarotOrientation.Reversed;
            var draw = new TarotDrawResult(card, orientation, session.SessionDateIso);
            session.SetCardAtSlot(slot, draw);
            session.PickedCount++;

            _eventBus?.Publish(new TarotDrawnEvent(draw));
            _eventBus?.Publish(new CardDrawnEvent(draw.Card.Id, draw.Orientation));
            return session;
        }

        public TarotSession ConfirmSelection(TarotSession session)
        {
            return session; // placeholder; UI transitions to Reveal stage
        }

        public async Task<TarotReading> RequestReadingAsync(TarotDrawResult draw, PetId petId,
            TarotOrientation orientation, CancellationToken cancellationToken = default)
        {
            TarotReading reading;
            try
            {
                reading = await _readingBackend.RequestAsync(draw, petId, orientation, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Tarot] Reading backend failed, falling back. {ex.Message}");
                reading = LocalFallback.Build(draw, petId, orientation);
            }

            _eventBus?.Publish(new TarotReadingReceivedEvent(draw, reading));
            return reading;
        }

        public async Task<TarotSummaryResult> RequestSummaryAsync(
            TarotDrawResult past, TarotDrawResult present, TarotDrawResult future,
            string? question, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _readingBackend.RequestSummaryAsync(past, present, future, question, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Tarot] Summary backend failed, falling back. {ex.Message}");
                return TarotSummaryResult.Default();
            }
        }

        private List<TarotCardSO> PickRandomCards(int count)
        {
            var deckCards = new List<TarotCardSO>(_deck.Cards);
            int n = deckCards.Count;
            // Fisher-Yates shuffle, take first count
            for (int i = n - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (deckCards[i], deckCards[j]) = (deckCards[j], deckCards[i]);
            }
            int take = Mathf.Min(count, n);
            return deckCards.GetRange(0, take);
        }
    }
}
