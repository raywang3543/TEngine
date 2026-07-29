using GameLogic;
using TEngine;

namespace GameLogic.Core
{
    /// <summary>
    /// 马戏牌局的控制器与模型统一入口。
    /// </summary>
    public sealed class CarnivalSystem : Singleton<CarnivalSystem>
    {
        private CarnivalPokerGame _pokerCtrl;
        private CarnivalContentModel _contentModel;

        protected override void OnInit()
        {
            base.OnInit();
            _contentModel = new CarnivalContentModel();
            _pokerCtrl = new CarnivalPokerGame(_contentModel);
            _pokerCtrl.StartNewRun();
            RegisterEvents();
        }

        protected override void OnRelease()
        {
            UnregisterEvents();
            _pokerCtrl?.Release();
            _contentModel?.Release();
            _pokerCtrl = null;
            _contentModel = null;
            base.OnRelease();
        }

        private void RegisterEvents()
        {
            GameEvent.AddEventListener(EventDefine.CarnivalRequestState, PublishState);
            GameEvent.AddEventListener(EventDefine.CarnivalStartNewRun, OnStartNewRun);
            GameEvent.AddEventListener<int>(EventDefine.CarnivalToggleCard, OnToggleCard);
            GameEvent.AddEventListener(EventDefine.CarnivalPlaySelected, OnPlaySelected);
            GameEvent.AddEventListener(EventDefine.CarnivalDiscardSelected, OnDiscardSelected);
            GameEvent.AddEventListener<string>(EventDefine.CarnivalBuyPerformer, OnBuyPerformer);
            GameEvent.AddEventListener<string>(EventDefine.CarnivalBuyConsumable, OnBuyConsumable);
            GameEvent.AddEventListener<string>(EventDefine.CarnivalUseConsumable, OnUseConsumable);
            GameEvent.AddEventListener(EventDefine.CarnivalContinueFromShop, OnContinueFromShop);
            GameEvent.AddEventListener(EventDefine.CarnivalSkipBlind, OnSkipBlind);
            GameEvent.AddEventListener(EventDefine.CarnivalSortHandByRank, OnSortHandByRank);
            GameEvent.AddEventListener(EventDefine.CarnivalSortHandBySuit, OnSortHandBySuit);
        }

        private void UnregisterEvents()
        {
            GameEvent.RemoveEventListener(EventDefine.CarnivalRequestState, PublishState);
            GameEvent.RemoveEventListener(EventDefine.CarnivalStartNewRun, OnStartNewRun);
            GameEvent.RemoveEventListener<int>(EventDefine.CarnivalToggleCard, OnToggleCard);
            GameEvent.RemoveEventListener(EventDefine.CarnivalPlaySelected, OnPlaySelected);
            GameEvent.RemoveEventListener(EventDefine.CarnivalDiscardSelected, OnDiscardSelected);
            GameEvent.RemoveEventListener<string>(EventDefine.CarnivalBuyPerformer, OnBuyPerformer);
            GameEvent.RemoveEventListener<string>(EventDefine.CarnivalBuyConsumable, OnBuyConsumable);
            GameEvent.RemoveEventListener<string>(EventDefine.CarnivalUseConsumable, OnUseConsumable);
            GameEvent.RemoveEventListener(EventDefine.CarnivalContinueFromShop, OnContinueFromShop);
            GameEvent.RemoveEventListener(EventDefine.CarnivalSkipBlind, OnSkipBlind);
            GameEvent.RemoveEventListener(EventDefine.CarnivalSortHandByRank, OnSortHandByRank);
            GameEvent.RemoveEventListener(EventDefine.CarnivalSortHandBySuit, OnSortHandBySuit);
        }

        private void OnStartNewRun()
        {
            _pokerCtrl.StartNewRun();
            PublishState();
        }

        private void OnToggleCard(int cardId)
        {
            _pokerCtrl.ToggleCard(cardId);
            PublishState();
        }

        private void OnPlaySelected()
        {
            _pokerCtrl.PlaySelected();
            PublishState();
        }

        private void OnDiscardSelected()
        {
            _pokerCtrl.DiscardSelected();
            PublishState();
        }

        private void OnBuyPerformer(string performerId)
        {
            _pokerCtrl.BuyPerformer(performerId);
            PublishState();
        }

        private void OnBuyConsumable(string consumableId)
        {
            _pokerCtrl.BuyConsumable(consumableId);
            PublishState();
        }

        private void OnUseConsumable(string consumableId)
        {
            _pokerCtrl.UseConsumable(consumableId);
            PublishState();
        }

        private void OnContinueFromShop()
        {
            _pokerCtrl.ContinueFromShop();
            PublishState();
        }

        private void OnSkipBlind()
        {
            _pokerCtrl.SkipBlind();
            PublishState();
        }

        private void OnSortHandByRank()
        {
            _pokerCtrl.SortHandByRank();
            PublishState();
        }

        private void OnSortHandBySuit()
        {
            _pokerCtrl.SortHandBySuit();
            PublishState();
        }

        private void PublishState()
        {
            if (_pokerCtrl == null)
            {
                Log.Warning("Carnival PokerCtrl is null");
                return;
            }

            GameEvent.Send(EventDefine.CarnivalStateChanged, new CarnivalGameState(_pokerCtrl));
        }
    }
}
