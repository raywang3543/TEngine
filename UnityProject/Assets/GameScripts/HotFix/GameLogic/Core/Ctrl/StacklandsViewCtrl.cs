using System;
using System.Linq;
using GameLogic.Core.Model;

namespace GameLogic.Core.Ctrl
{
    /// <summary>
    /// 将 Model 转换为不可变表现快照，并统一经 CoreSystem 投递给 View/UI。
    /// </summary>
    internal sealed class StacklandsViewCtrl
    {
        private StacklandsGameModel Model => CoreSystem.Model;

        internal void PublishAll()
        {
            PublishBoard();
            PublishHud();
        }

        internal void PublishBoard()
        {
            if (Model.Run == null)
            {
                CoreSystem.PublishBoard(new BoardSnapshot
                    { Cards = Array.Empty<CardSnapshot>(), Boosters = Array.Empty<BoosterSnapshot>() });
                return;
            }
            CoreSystem.PublishBoard(new BoardSnapshot
            {
                Revision = Model.Run.Revision,
                SelectedInstanceId = Model.SelectedId,
                Cards = Model.Run.Cards.Select(ToSnapshot).ToList().AsReadOnly(),
                Boosters = Model.Run.Boosters.Select(pack => new BoosterSnapshot
                {
                    InstanceId = pack.InstanceId, BoosterId = pack.BoosterId,
                    NameZh = Model.Content.Boosters.Get(pack.BoosterId).NameZh, X = pack.X, Y = pack.Y,
                    Remaining = pack.Results.Count - pack.Revealed,
                }).ToList().AsReadOnly(),
            });
        }

        internal void PublishHud()
        {
            if (Model.Run == null) return;
            CoreSystem.PublishHud(new HudSnapshot
            {
                Moon = Model.Run.Moon, MoonRemaining = Math.Max(0f, Model.Run.MoonRemaining),
                MoonDuration = Model.Run.MoonDuration, Speed = Model.Run.Speed,
                Coins = Model.CountCard("coin"), Food = Model.CurrentFood(),
                CardCount = Model.CurrentCardCount(), CardCap = Model.CurrentCardCap(),
                CompletedQuestCount = Model.Profile.CompletedQuests.Count, Peaceful = Model.Run.Peaceful,
                Quests = Model.Content.Quests.All.OrderBy(item => item.Series).ThenBy(item => item.Order)
                    .Select(quest => new QuestSnapshot
                    {
                        Id = quest.Id, NameZh = quest.NameZh, DescriptionZh = quest.DescriptionZh,
                        Completed = Model.Profile.CompletedQuests.Contains(quest.Id), IsMain = quest.IsMain,
                    }).ToList().AsReadOnly(),
                Boosters = Model.Content.Boosters.All.OrderBy(item => item.UnlockQuestCount).Select(pack =>
                    new BoosterShopSnapshot
                    {
                        Id = pack.Id, NameZh = pack.NameZh, Price = pack.PriceAmount,
                        Unlocked = Model.Profile.CompletedQuests.Count >= pack.UnlockQuestCount,
                        LockText = Model.Profile.CompletedQuests.Count >= pack.UnlockQuestCount
                            ? string.Empty : $"完成 {pack.UnlockQuestCount} 项任务",
                    }).ToList().AsReadOnly(),
                Cardopedia = Model.Content.Cards.All.OrderBy(item => item.Category).ThenBy(item => item.NameZh)
                    .Select(card => new CardopediaEntrySnapshot
                    {
                        CardId = card.Id, NameZh = card.NameZh, Category = card.Category,
                        Discovered = Model.Profile.DiscoveredCards.Contains(card.Id),
                    }).ToList().AsReadOnly(),
            });
        }

        private CardSnapshot ToSnapshot(CardRunData card)
        {
            CardDefinition definition = Model.Content.Cards.Get(card.CardId);
            WorkRunData work = Model.Run.Works.FirstOrDefault(item => item.CardIds.Contains(card.InstanceId));
            int maxHp = Model.Content.Units.Contains(card.CardId)
                ? Model.Content.Units.Get(card.CardId).MaxHp.GetValueOrDefault() : 0;
            string displayId = card.CardId;
            if (!string.IsNullOrEmpty(card.EquipmentCardId) && Model.Content.Equipment.Contains(card.EquipmentCardId))
                displayId = Model.Content.Equipment.Get(card.EquipmentCardId).ProfessionCardId;
            CardDefinition display = Model.Content.Cards.Contains(displayId)
                ? Model.Content.Cards.Get(displayId) : definition;
            return new CardSnapshot
            {
                InstanceId = card.InstanceId, CardId = card.CardId, NameZh = display.NameZh,
                DescriptionZh = definition.DescriptionZh, Color = display.Color, Category = display.Category,
                StackId = card.StackId, StackOrder = card.StackOrder, X = card.X, Y = card.Y,
                SellPrice = definition.SellPrice.GetValueOrDefault(),
                FoodValue = definition.FoodValue.GetValueOrDefault(), Hp = card.Hp, MaxHp = maxHp,
                IsFoil = card.IsFoil, IsLocked = work != null,
                Progress = work == null ? 0f : 1f - Math.Max(0f, work.Remaining) / work.Duration,
                Status = work == null ? string.Empty : work.IsRecipe ? "制作中" : "工作中",
            };
        }
    }
}
