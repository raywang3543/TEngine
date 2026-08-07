using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using GameLogic.Core;
using UnityEngine;
using UnityEngine.UIElements;
using YooAsset;

namespace GameLogic.Core.View
{
    /// <summary>
    /// Stacklands 牌桌的 Unity 2D 表现层，只消费快照并发送命令。
    /// </summary>
    public sealed class StacklandsBoardView : MonoBehaviour
    {
        private const float WholeStackHoldSeconds = 0.5f;
        private const float WholeStackHoldMovementTolerance = 0.2f;
        // 点击与拖拽的判定阈值（像素）：卡包点击开包、单位点击展开装备卡堆共用。
        private const float ClickDragThresholdPixels = 10f;
        private const float MinOrthoSize = 3.5f;
        private const float DefaultOrthoSize = 5f;
        private const float MaxOrthoSize = 7f;
        private const float ShopShelfY = MaxOrthoSize * 0.5f;
        private const float ShopSlotSpacing = 1.75f;
        // 卡槽组整体右移量：2 个卡槽宽度（卡槽图 135px @ PPU 100 = 1.35 世界单位）。
        private const float ShopSlotsXOffset = 2.7f;
        // 装备卡堆相对单位的横向间距与纵向偏移（装备卡与牌面同图，1.5 x 2 世界单位）。
        private const float EquipmentFanSpacing = 1.6f;
        private const float EquipmentFanYOffset = -1.85f;
        // 月末进食动画：食物卡飞行速度（世界单位/秒）与到达判定距离（小于牌面高度的重合阈值）。
        private const float FeedingFlySpeed = 16f;
        private const float FeedingArriveDistance = 0.4f;
        // 食物卡与单位重合后停留 0.35 秒再消失。
        private const float FeedingOverlapSeconds = 0.35f;
        /// <summary>月末结算中被消耗、正飞向进食单位的食物卡。</summary>
        private sealed class FlyingFood
        {
            internal CardView View;
            internal string UnitInstanceId;
            internal Vector3 FallbackTarget;
            /// <summary>与单位重合后的剩余停留秒数；&lt;0 表示尚未重合。</summary>
            internal float OverlapRemaining = -1f;
        }
        private readonly List<FlyingFood> _flyingFoods = new List<FlyingFood>();
        private readonly Dictionary<string, CardView> _cards = new Dictionary<string, CardView>();
        private readonly Dictionary<string, BoosterView> _boosters = new Dictionary<string, BoosterView>();
        private readonly Dictionary<string, ShopSlotView> _shopSlots = new Dictionary<string, ShopSlotView>();
        private readonly HashSet<CardView> _wholeStackFeedbackCards = new HashSet<CardView>();
        private readonly Dictionary<string, Sprite> _cardSprites = new Dictionary<string, Sprite>();
        private readonly List<SubAssetsHandle> _cardSpriteHandles = new List<SubAssetsHandle>();
        private Sprite _boosterSprite;
        private Sprite _slotSprite;
        private Sprite _slotBorderGreen;
        private Sprite _slotBorderRed;
        private Sprite _borderBlack;
        private Sprite _borderWhite;
        private Sprite _borderYellow;
        private Camera _camera;
        private Transform _boardFrame;
        private float _boardFrameAspect;
        private Sprite _whiteSprite;
        private Font _font;
        private SellSlotView _sellSlot;
        private CardView _draggedCard;
        private CardView _dropTargetCard;
        private readonly Dictionary<string, CardSnapshot> _cardData = new Dictionary<string, CardSnapshot>();
        private readonly List<EquippedCardView> _equipmentFanCards = new List<EquippedCardView>();
        private CardView _equipmentFanUnit;
        private EquippedCardView _pressedEquipmentCard;
        private BoosterView _draggedBooster;
        private readonly Dictionary<CardView, Vector3> _draggedOffsets = new Dictionary<CardView, Vector3>();
        private Vector3 _dragOffset;
        private Vector3 _boosterDragOffset;
        private Vector3 _boosterDragStartedPointer;
        private Vector3 _cardPressPointer;
        private Vector3 _dragStartedPosition;
        private Vector3 _lastPointer;
        private bool _panning;
        private bool _dragWholeStack;
        private bool _boosterMoved;
        private bool _wholeStackHoldEligible;
        private float _mouseDragStartedAt;
        private float _touchDragStartedAt;

        private void Awake()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                var cameraObject = new GameObject("Stacklands Board Camera");
                _camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }
            _camera.orthographic = true;
            _camera.orthographicSize = DefaultOrthoSize;
            // 相机默认停在地图中心（世界原点）。
            _camera.transform.position = new Vector3(0f, 0f, -10f);
            _camera.rect = new Rect(0f, 0f, 1f, 1f);
            foreach (Camera overlayCamera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (overlayCamera != _camera && overlayCamera.depth > _camera.depth &&
                    overlayCamera.cullingMask == 1 << LayerMask.NameToLayer("UI"))
                    overlayCamera.enabled = false;
            }
            _whiteSprite = CreateWhiteSprite();
            _font = CreateChineseFont();
            LoadCardBackgroundsAsync().Forget();
            CreateBoardFrame();
        }

        private void Update()
        {
            UpdateBoardFrame();
            UpdateFlyingFoods();
            HandleKeyboard();
            HandleMouse();
            HandleTouch();
        }

        public void Render(BoardSnapshot snapshot)
        {
            if (snapshot == null) return;
            EnsureAssets();
            // 所属单位被销毁（出售/战斗死亡等）时清理残留的装备卡堆。
            if (_equipmentFanUnit == null) CloseEquipmentFan();
            _cardData.Clear();
            foreach (CardSnapshot card in snapshot.Cards) _cardData[card.InstanceId] = card;
            var cardIds = new HashSet<string>(snapshot.Cards.Select(item => item.InstanceId));
            foreach (string id in _cards.Keys.Where(id => !cardIds.Contains(id)).ToArray())
            {
                DestroyView(_cards[id].gameObject);
                _cards.Remove(id);
            }

            foreach (CardSnapshot card in snapshot.Cards)
            {
                if (!_cards.TryGetValue(card.InstanceId, out CardView view))
                {
                    view = CreateCard(card.InstanceId);
                    _cards.Add(card.InstanceId, view);
                }
                view.Render(card, card.InstanceId == snapshot.SelectedInstanceId);
            }

            var boosterIds = new HashSet<string>(snapshot.Boosters.Select(item => item.InstanceId));
            foreach (string id in _boosters.Keys.Where(id => !boosterIds.Contains(id)).ToArray())
            {
                DestroyView(_boosters[id].gameObject);
                _boosters.Remove(id);
            }

            foreach (BoosterSnapshot booster in snapshot.Boosters)
            {
                if (!_boosters.TryGetValue(booster.InstanceId, out BoosterView view))
                {
                    view = CreateBooster(booster.InstanceId);
                    _boosters.Add(booster.InstanceId, view);
                }
                view.Render(booster);
            }
        }

        public void RenderCardProgress(CardProgressBatch snapshot)
        {
            if (snapshot?.Cards == null) return;
            foreach (CardProgressSnapshot card in snapshot.Cards)
                if (_cards.TryGetValue(card.InstanceId, out CardView view))
                    view.RenderProgress(card.Progress);
        }

        /// <summary>
        /// 月末结算进食动画：被消耗的食物卡已从 Model 移除，这里把对应视图移出快照管理，
        /// 让它飞向进食单位；两张卡重合后食物卡销毁。目标单位随帧取实时位置，单位消失时飞向最后已知位置。
        /// </summary>
        public void PlayFeeding(IReadOnlyList<FeedingSnapshot> pairs)
        {
            if (pairs == null) return;
            foreach (FeedingSnapshot pair in pairs)
            {
                if (pair == null || string.IsNullOrEmpty(pair.FoodInstanceId)) continue;
                if (!_cards.TryGetValue(pair.FoodInstanceId, out CardView food) || food == null) continue;
                _cards.Remove(pair.FoodInstanceId);
                // 结算瞬间玩家可能正拖着这张食物（单卡或整堆）：取消拖拽状态，让飞行动画接管。
                if (_draggedCard == food)
                {
                    ClearWholeStackFeedback();
                    HideSlotBorders();
                    _draggedCard = null;
                    _draggedOffsets.Clear();
                    _dragWholeStack = false;
                    _wholeStackHoldEligible = false;
                }
                else _draggedOffsets.Remove(food);
                food.SetDragSorting(true);
                food.SetColliderEnabled(false);
                Vector3 fallback = food.transform.position;
                if (!string.IsNullOrEmpty(pair.UnitInstanceId) &&
                    _cards.TryGetValue(pair.UnitInstanceId, out CardView unit) && unit != null)
                    fallback = unit.transform.position;
                else if (_cardData.TryGetValue(pair.UnitInstanceId, out CardSnapshot data))
                    fallback = new Vector3(data.X, data.Y, 0f);
                _flyingFoods.Add(new FlyingFood
                    { View = food, UnitInstanceId = pair.UnitInstanceId, FallbackTarget = fallback });
            }
        }

        /// <summary>同一时刻只飞最早入队的一张：到达销毁后下一张才开始，形成依次飞来的节奏。</summary>
        private void UpdateFlyingFoods()
        {
            if (_flyingFoods.Count == 0) return;
            FlyingFood item = _flyingFoods[0];
            if (item.View == null)
            {
                _flyingFoods.RemoveAt(0);
                return;
            }
            Vector3 target = item.FallbackTarget;
            if (!string.IsNullOrEmpty(item.UnitInstanceId) &&
                _cards.TryGetValue(item.UnitInstanceId, out CardView unit) && unit != null)
                target = unit.transform.position;
            if (item.OverlapRemaining >= 0f)
            {
                // 已与单位卡重合：停留 0.35 秒再销毁，下一张随后起飞。
                item.OverlapRemaining -= Time.unscaledDeltaTime;
                if (item.OverlapRemaining > 0f) return;
                DestroyView(item.View.gameObject);
                _flyingFoods.RemoveAt(0);
                return;
            }
            Vector3 next = Vector3.MoveTowards(item.View.transform.position, target,
                FeedingFlySpeed * Time.unscaledDeltaTime);
            // 与单位卡重合（距离小于阈值）时停在单位位置，进入停留计时。
            if (Vector2.Distance(next, target) <= FeedingArriveDistance)
            {
                item.View.transform.position = target;
                item.OverlapRemaining = FeedingOverlapSeconds;
                return;
            }
            item.View.transform.position = next;
        }

        public void RenderHud(HudSnapshot snapshot)
        {
            if (snapshot == null) return;
            EnsureAssets();
            // 出售槽与商店槽延迟到对局开始后的首个 HUD 快照创建，开始菜单阶段不显示。
            if (_sellSlot == null)
            {
                _sellSlot = CreateSellSlot();
                LayoutShopSlots();
            }
            _sellSlot.Render(snapshot.Coins);
            bool shopLayoutChanged = false;
            var slotIds = new HashSet<string>(snapshot.Boosters.Select(item => item.Id));
            foreach (string id in _shopSlots.Keys.Where(id => !slotIds.Contains(id)).ToArray())
            {
                DestroyView(_shopSlots[id].gameObject);
                _shopSlots.Remove(id);
                shopLayoutChanged = true;
            }

            for (int index = 0; index < snapshot.Boosters.Count; index++)
            {
                BoosterShopSnapshot booster = snapshot.Boosters[index];
                if (!_shopSlots.TryGetValue(booster.Id, out ShopSlotView slot))
                {
                    slot = CreateShopSlot(booster.Id);
                    _shopSlots.Add(booster.Id, slot);
                    shopLayoutChanged = true;
                }
                if (slot.Order != index) shopLayoutChanged = true;
                slot.Order = index;
                slot.Render(booster, snapshot.Coins);
            }
            if (shopLayoutChanged) LayoutShopSlots();
        }

        private void HandleKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Alpha1)) SendSpeed(0f);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SendSpeed(1f);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SendSpeed(5f);
            if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals)) Zoom(-0.7f);
            if (Input.GetKeyDown(KeyCode.Minus)) Zoom(0.7f);
        }

        private void HandleMouse()
        {
            Vector3 mouse = Input.mousePosition;
            if (Input.mouseScrollDelta.y != 0f && !PointerOverUi(mouse)) Zoom(-Input.mouseScrollDelta.y * 0.5f);

            if ((Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) && !PointerOverUi(mouse))
            {
                _panning = true;
                _lastPointer = mouse;
            }
            if (_panning && (Input.GetMouseButton(1) || Input.GetMouseButton(2)))
            {
                Vector3 before = ScreenToWorld(_lastPointer);
                Vector3 after = ScreenToWorld(mouse);
                _camera.transform.position += before - after;
                ClampCameraPosition();
                _lastPointer = mouse;
            }
            if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2)) _panning = false;

            if (Input.GetMouseButtonDown(0) && !PointerOverUi(mouse))
            {
                BeginPointer(mouse, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
                _mouseDragStartedAt = Time.unscaledTime;
            }
            if (Input.GetMouseButton(0) && _draggedCard != null)
            {
                Vector3 anchorPosition = ScreenToWorld(mouse) + _dragOffset;
                UpdateWholeStackHoldEligibility(anchorPosition);
                if (_wholeStackHoldEligible && Time.unscaledTime - _mouseDragStartedAt >= WholeStackHoldSeconds)
                    PromoteToWholeStackDrag();
                CloseEquipmentFanOnDrag(mouse);
                MoveDraggedCards(anchorPosition);
                UpdateStackTargetFeedback();
                UpdateSlotFeedback(ScreenToWorld(mouse));
            }
            else if (Input.GetMouseButton(0) && _draggedBooster != null)
                UpdateBoosterDrag(mouse);
            if (Input.GetMouseButtonUp(0))
                EndPointer(mouse, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        }

        private void HandleTouch()
        {
            if (Input.touchCount == 2)
            {
                Touch a = Input.GetTouch(0);
                Touch b = Input.GetTouch(1);
                float previous = ((a.position - a.deltaPosition) - (b.position - b.deltaPosition)).magnitude;
                Zoom((previous - (a.position - b.position).magnitude) * 0.012f);
                return;
            }
            if (Input.touchCount != 1) return;
            Touch touch = Input.GetTouch(0);
            if (PointerOverUi(touch.position) && _draggedCard == null && _draggedBooster == null) return;
            if (touch.phase == TouchPhase.Began)
            {
                BeginPointer(touch.position, false);
                _touchDragStartedAt = Time.unscaledTime;
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                if (_draggedCard != null)
                {
                    Vector3 anchorPosition = ScreenToWorld(touch.position) + _dragOffset;
                    UpdateWholeStackHoldEligibility(anchorPosition);
                    if (_wholeStackHoldEligible &&
                        Time.unscaledTime - _touchDragStartedAt >= WholeStackHoldSeconds)
                        PromoteToWholeStackDrag();
                    CloseEquipmentFanOnDrag(touch.position);
                    MoveDraggedCards(anchorPosition);
                    UpdateStackTargetFeedback();
                    UpdateSlotFeedback(ScreenToWorld(touch.position));
                }
                else if (_draggedBooster != null)
                    UpdateBoosterDrag(touch.position);
                else if (touch.phase == TouchPhase.Moved)
                {
                    _camera.transform.position -= (Vector3)touch.deltaPosition * (_camera.orthographicSize / 450f);
                    ClampCameraPosition();
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                EndPointer(touch.position, false);
        }

        private void BeginPointer(Vector3 pointer, bool wholeStack)
        {
            Vector2 world = ScreenToWorld(pointer);
            Collider2D hit = Physics2D.OverlapPointAll(world)
                .OrderBy(item => item.transform.position.z).FirstOrDefault();
            if (hit == null)
            {
                // 点击空白处收起装备卡堆。
                CloseEquipmentFan();
                return;
            }
            EquippedCardView equippedCard = hit.GetComponent<EquippedCardView>();
            if (equippedCard != null)
            {
                _pressedEquipmentCard = equippedCard;
                return;
            }
            BoosterView booster = hit.GetComponent<BoosterView>();
            if (booster != null)
            {
                _draggedBooster = booster;
                _boosterDragOffset = booster.transform.position - (Vector3)world;
                _boosterDragStartedPointer = pointer;
                _boosterMoved = false;
                return;
            }
            ShopSlotView shopSlot = hit.GetComponent<ShopSlotView>();
            if (shopSlot != null) return;
            ClearDropTargetFeedback();
            _draggedCard = hit.GetComponent<CardView>();
            if (_draggedCard == null)
            {
                CloseEquipmentFan();
                return;
            }
            // 敌对单位不可拖动，按下时只响应点选，不进入拖动状态。
            if (_cardData.TryGetValue(_draggedCard.InstanceId, out CardSnapshot pressedData) &&
                pressedData.IsHostile)
            {
                CoreSystem.SubmitCommand(new StacklandsCommandDto
                {
                    Kind = StacklandsCommandKind.SelectCard, InstanceId = _draggedCard.InstanceId,
                });
                _draggedCard = null;
                return;
            }
            // 按下其他卡牌时收起装备卡堆；按下所属单位时保留，松开时切换展开/收起。
            if (_draggedCard != _equipmentFanUnit) CloseEquipmentFan();
            ClearWholeStackFeedback();
            _dragOffset = _draggedCard.transform.position - (Vector3)world;
            _dragStartedPosition = _draggedCard.transform.position;
            _cardPressPointer = pointer;
            _dragWholeStack = wholeStack || IsCurrencyStack(_draggedCard.StackId);
            _wholeStackHoldEligible = !wholeStack &&
                                      _cards.Values.Count(card => card.StackId == _draggedCard.StackId) > 1;
            CacheDraggedCards();
            SetDraggedCardsSorting(true);
            if (_dragWholeStack) ShowWholeStackFeedback();
            CoreSystem.SubmitCommand(new StacklandsCommandDto
            {
                Kind = StacklandsCommandKind.SelectCard, InstanceId = _draggedCard.InstanceId,
            });
        }

        private void EndPointer(Vector3 pointer, bool wholeStack)
        {
            if (_pressedEquipmentCard != null)
            {
                // 松开时仍在同一装备卡上：卸下该槽位装备；否则视为点击其他位置，只收起卡堆。
                EquippedCardView pressed = _pressedEquipmentCard;
                _pressedEquipmentCard = null;
                if (pressed != null && _equipmentFanUnit != null && pressed.Contains(ScreenToWorld(pointer)))
                    CoreSystem.SubmitCommand(new StacklandsCommandDto
                    {
                        Kind = StacklandsCommandKind.Unequip,
                        InstanceId = _equipmentFanUnit.InstanceId,
                        EquipmentSlot = pressed.Slot,
                    });
                CloseEquipmentFan();
                return;
            }
            if (_draggedBooster != null)
            {
                UpdateBoosterDrag(pointer);
                string boosterId = _draggedBooster.InstanceId;
                Vector3 position = _draggedBooster.transform.position;
                bool moved = _boosterMoved;
                _draggedBooster.SetDragSorting(false);
                _draggedBooster = null;
                _boosterMoved = false;
                CoreSystem.SubmitCommand(new StacklandsCommandDto
                {
                    Kind = moved ? StacklandsCommandKind.MoveBooster : StacklandsCommandKind.OpenBooster,
                    InstanceId = boosterId,
                    X = position.x,
                    Y = position.y,
                });
                return;
            }
            if (_draggedCard == null) return;
            Vector2 world = ScreenToWorld(pointer);
            string draggedId = _draggedCard.InstanceId;
            string draggedStackId = _draggedCard.StackId;
            bool moveWholeStack = _dragWholeStack || wholeStack;
            var movingIds = new HashSet<string>(moveWholeStack
                ? _cards.Values.Where(card => card.StackId == draggedStackId).Select(card => card.InstanceId)
                : new[] { draggedId });
            bool overSellSlot = _sellSlot != null && _sellSlot.Contains(world);
            bool canSell = _cardData.TryGetValue(draggedId, out CardSnapshot data) && data.CanSell;
            ShopSlotView shopSlot = _shopSlots.Values.FirstOrDefault(slot => slot != null && slot.Contains(world));
            bool overShopSlot = shopSlot != null;
            if ((overSellSlot && !canSell) || overShopSlot)
            {
                // 卡槽操作不改变牌堆位置；Core 成功扣款后只会移除本次支付的金币。
                RestoreDraggedCards();
            }
            // 纯点击（位移不足阈值）可佩戴且已佩戴装备的单位：展开/收起装备卡堆，不发送移动命令。
            CardView pressedCard = _draggedCard;
            bool clickOnly = Vector2.Distance(_cardPressPointer, pointer) < ClickDragThresholdPixels;
            if (clickOnly && !moveWholeStack &&
                _cardData.TryGetValue(draggedId, out CardSnapshot clickData) &&
                clickData.CanEquip && clickData.EquippedItems is { Count: > 0 })
            {
                RestoreDraggedCards();
                ClearWholeStackFeedback();
                ClearDropTargetFeedback();
                SetDraggedCardsSorting(false);
                HideSlotBorders();
                _draggedCard = null;
                _draggedOffsets.Clear();
                _dragWholeStack = false;
                _wholeStackHoldEligible = false;
                ToggleEquipmentFan(pressedCard);
                return;
            }
            ClearWholeStackFeedback();
            ClearDropTargetFeedback();
            SetDraggedCardsSorting(false);
            HideSlotBorders();
            _draggedCard = null;
            _draggedOffsets.Clear();
            _dragWholeStack = false;
            _wholeStackHoldEligible = false;
            if (overSellSlot)
            {
                if (canSell)
                    CoreSystem.SubmitCommand(new StacklandsCommandDto
                    {
                        Kind = StacklandsCommandKind.SellCard, InstanceId = draggedId,
                    });
                return;
            }
            if (overShopSlot)
            {
                CoreSystem.SubmitCommand(new StacklandsCommandDto
                {
                    Kind = StacklandsCommandKind.BuyBooster,
                    InstanceId = draggedId,
                    ContentId = shopSlot.BoosterId,
                });
                return;
            }
            // 松手判定按被拖卡的牌面重叠（而非指针点）寻找堆叠目标，避免牌面已接触但指针未命中。
            CardView target = FindStackTarget(pressedCard, movingIds);
            CoreSystem.SubmitCommand(new StacklandsCommandDto
            {
                Kind = moveWholeStack ? StacklandsCommandKind.MoveStack : StacklandsCommandKind.MoveCard,
                InstanceId = draggedId,
                TargetInstanceId = target == null ? null : target.InstanceId,
                X = world.x,
                Y = world.y,
            });
        }

        private void CacheDraggedCards()
        {
            _draggedOffsets.Clear();
            if (_draggedCard == null) return;
            Vector3 anchorPosition = _draggedCard.transform.position;
            IEnumerable<CardView> cards = _dragWholeStack
                ? _cards.Values.Where(card => card.StackId == _draggedCard.StackId)
                : new[] { _draggedCard };
            foreach (CardView card in cards) _draggedOffsets[card] = card.transform.position - anchorPosition;
        }

        private void PromoteToWholeStackDrag()
        {
            if (_dragWholeStack || !_wholeStackHoldEligible || _draggedCard == null) return;
            _dragWholeStack = true;
            _wholeStackHoldEligible = false;
            CacheDraggedCards();
            SetDraggedCardsSorting(true);
            ShowWholeStackFeedback();
        }

        private void UpdateWholeStackHoldEligibility(Vector3 anchorPosition)
        {
            if (!_wholeStackHoldEligible || _dragWholeStack) return;
            if (Vector2.Distance(_dragStartedPosition, anchorPosition) <= WholeStackHoldMovementTolerance) return;
            _wholeStackHoldEligible = false;
        }

        private void ShowWholeStackFeedback()
        {
            ClearWholeStackFeedback();
            if (_draggedCard == null) return;
            foreach (CardView card in _cards.Values.Where(card => card.StackId == _draggedCard.StackId))
            {
                card.SetWholeStackDragFeedback(true, card == _draggedCard);
                _wholeStackFeedbackCards.Add(card);
            }
        }

        private void ClearWholeStackFeedback()
        {
            foreach (CardView card in _wholeStackFeedbackCards)
                if (card != null) card.SetWholeStackDragFeedback(false, false);
            _wholeStackFeedbackCards.Clear();
        }

        /// <summary>
        /// 拖动卡牌悬停在卡槽上时显示反馈边框：出售槽按能否出售、商店槽按来源金币堆能否支付分绿/红。
        /// </summary>
        private void UpdateSlotFeedback(Vector2 world)
        {
            if (_draggedCard == null)
            {
                HideSlotBorders();
                return;
            }
            _cardData.TryGetValue(_draggedCard.InstanceId, out CardSnapshot data);
            if (_sellSlot != null && _sellSlot.Contains(world))
                _sellSlot.ShowBorder(data is { CanSell: true });
            else _sellSlot?.HideBorder();

            foreach (ShopSlotView slot in _shopSlots.Values)
            {
                if (slot == null) continue;
                if (slot.Contains(world))
                    slot.ShowBorder(data != null && data.CardId == Model.StacklandsGameModel.CurrencyCardId &&
                                    slot.Unlocked && CountStackCurrency(data.StackId) >= slot.Price);
                else slot.HideBorder();
            }
        }

        private bool IsCurrencyStack(string stackId)
        {
            CardSnapshot[] cards = _cardData.Values.Where(card => card.StackId == stackId).ToArray();
            return cards.Length > 1 && cards.All(card => card.CardId == Model.StacklandsGameModel.CurrencyCardId);
        }

        private int CountStackCurrency(string stackId)
        {
            return _cardData.Values.Count(card => card.StackId == stackId &&
                                                  card.CardId == Model.StacklandsGameModel.CurrencyCardId);
        }

        private void RestoreDraggedCards()
        {
            foreach (KeyValuePair<CardView, Vector3> pair in _draggedOffsets)
                if (pair.Key != null) pair.Key.transform.position = _dragStartedPosition + pair.Value;
        }

        /// <summary>
        /// 点击可佩戴且已佩戴装备的单位：在单位下方展开装备卡堆；再次点击同一单位则收起。
        /// </summary>
        private void ToggleEquipmentFan(CardView unit)
        {
            if (_equipmentFanUnit == unit)
            {
                CloseEquipmentFan();
                return;
            }
            CloseEquipmentFan();
            if (!_cardData.TryGetValue(unit.InstanceId, out CardSnapshot data) ||
                data.EquippedItems == null || data.EquippedItems.Count == 0) return;
            _equipmentFanUnit = unit;
            Vector3 center = unit.transform.position;
            for (int i = 0; i < data.EquippedItems.Count; i++)
            {
                EquippedItemSnapshot item = data.EquippedItems[i];
                Sprite background = null;
                if (!string.IsNullOrEmpty(item.Color)) _cardSprites.TryGetValue(item.Color, out background);
                EquippedCardView view = EquippedCardView.Create(transform, item, _whiteSprite, _font, background);
                float offset = (i - (data.EquippedItems.Count - 1) * 0.5f) * EquipmentFanSpacing;
                view.transform.position =
                    new Vector3(center.x + offset, center.y + EquipmentFanYOffset, -0.5f);
                _equipmentFanCards.Add(view);
            }
        }

        private void CloseEquipmentFan()
        {
            _equipmentFanUnit = null;
            _pressedEquipmentCard = null;
            foreach (EquippedCardView view in _equipmentFanCards)
                if (view != null) DestroyView(view.gameObject);
            _equipmentFanCards.Clear();
        }

        /// <summary>
        /// 装备卡堆展开时拖动卡牌（位移超过点击阈值）：收起装备卡堆。
        /// 按下其他卡牌时卡堆已在 BeginPointer 收起，这里只需处理拖动所属单位的情况。
        /// </summary>
        private void CloseEquipmentFanOnDrag(Vector3 pointer)
        {
            if (_equipmentFanUnit != null &&
                Vector2.Distance(_cardPressPointer, pointer) >= ClickDragThresholdPixels)
                CloseEquipmentFan();
        }

        private static void DestroyView(GameObject view)
        {
            if (Application.isPlaying) Destroy(view);
            else DestroyImmediate(view);
        }

        private void HideSlotBorders()
        {
            _sellSlot?.HideBorder();
            foreach (ShopSlotView slot in _shopSlots.Values)
                if (slot != null) slot.HideBorder();
        }

        private void MoveDraggedCards(Vector3 anchorPosition)
        {
            foreach (KeyValuePair<CardView, Vector3> pair in _draggedOffsets)
                if (pair.Key != null) pair.Key.transform.position = anchorPosition + pair.Value;
        }

        /// <summary>
        /// 以被拖卡的碰撞体（而非指针点）做重叠检测，返回重叠面积最大的可堆叠目标卡。
        /// </summary>
        private CardView FindStackTarget(CardView anchor, HashSet<string> movingIds)
        {
            Bounds bounds = anchor.ColliderBounds;
            CardView best = null;
            float bestOverlap = 0f;
            foreach (Collider2D hit in Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f))
            {
                CardView card = hit.GetComponent<CardView>();
                if (card == null || movingIds.Contains(card.InstanceId)) continue;
                Bounds other = card.ColliderBounds;
                float overlap = Mathf.Max(0f, Mathf.Min(bounds.max.x, other.max.x) -
                                            Mathf.Max(bounds.min.x, other.min.x)) *
                                Mathf.Max(0f, Mathf.Min(bounds.max.y, other.max.y) -
                                            Mathf.Max(bounds.min.y, other.min.y));
                if (overlap > bestOverlap)
                {
                    bestOverlap = overlap;
                    best = card;
                }
            }
            return best;
        }

        /// <summary>拖动途中实时高亮当前牌面重叠命中的堆叠目标。</summary>
        private void UpdateStackTargetFeedback()
        {
            CardView target = null;
            if (_draggedCard != null && _draggedOffsets.Count > 0)
            {
                var movingIds = new HashSet<string>(_draggedOffsets.Keys
                    .Where(card => card != null).Select(card => card.InstanceId));
                target = FindStackTarget(_draggedCard, movingIds);
            }
            if (target == _dropTargetCard) return;
            if (_dropTargetCard != null) _dropTargetCard.SetDropTargetFeedback(false);
            _dropTargetCard = target;
            if (_dropTargetCard != null) _dropTargetCard.SetDropTargetFeedback(true);
        }

        private void ClearDropTargetFeedback()
        {
            if (_dropTargetCard != null) _dropTargetCard.SetDropTargetFeedback(false);
            _dropTargetCard = null;
        }

        private void UpdateBoosterDrag(Vector3 pointer)
        {
            if (_draggedBooster == null) return;
            Vector3 position = ScreenToWorld(pointer) + _boosterDragOffset;
            if (!_boosterMoved &&
                Vector2.Distance(_boosterDragStartedPointer, pointer) < ClickDragThresholdPixels) return;
            if (!_boosterMoved)
            {
                _boosterMoved = true;
                _draggedBooster.SetDragSorting(true);
            }
            _draggedBooster.transform.position = position;
        }

        private void SetDraggedCardsSorting(bool active)
        {
            foreach (CardView card in _draggedOffsets.Keys)
                if (card != null) card.SetDragSorting(active);
        }

        private CardView CreateCard(string id)
        {
            var go = new GameObject("Card " + id);
            go.transform.SetParent(transform, false);
            var view = go.AddComponent<CardView>().Initialize(id, _whiteSprite, _font, _cardSprites);
            view.SetBorderSprites(_borderBlack, _borderWhite, _borderYellow);
            return view;
        }

        private BoosterView CreateBooster(string id)
        {
            var go = new GameObject("Booster " + id);
            go.transform.SetParent(transform, false);
            var view = go.AddComponent<BoosterView>().Initialize(id, _whiteSprite, _font);
            if (_boosterSprite != null) view.Background = _boosterSprite;
            return view;
        }

        private SellSlotView CreateSellSlot()
        {
            var go = new GameObject("Sell Slot");
            go.transform.SetParent(transform, false);
            var view = go.AddComponent<SellSlotView>().Initialize(_whiteSprite, _font);
            if (_slotSprite != null) view.Background = _slotSprite;
            view.SetBorderSprites(_slotBorderGreen, _slotBorderRed);
            return view;
        }

        private ShopSlotView CreateShopSlot(string id)
        {
            var go = new GameObject("Shop Slot " + id);
            go.transform.SetParent(transform, false);
            var view = go.AddComponent<ShopSlotView>().Initialize(id, _whiteSprite, _font);
            if (_slotSprite != null) view.Background = _slotSprite;
            view.SetBorderSprites(_slotBorderGreen, _slotBorderRed);
            return view;
        }

        /// <summary>
        /// 出售槽与商店槽作为整体水平居中后再右移 ShopSlotsXOffset，垂直固定在中心与地图上边界中间（ShopShelfY）。
        /// </summary>
        private void LayoutShopSlots()
        {
            if (_sellSlot == null) return;
            float firstX = ShopSlotsXOffset - ShopSlotSpacing * _shopSlots.Count * 0.5f;
            _sellSlot.SetLayout(new Vector3(firstX, ShopShelfY, 0f));

            int index = 0;
            foreach (ShopSlotView slot in _shopSlots.Values.OrderBy(item => item.Order))
                slot.SetLayout(new Vector3(firstX + ShopSlotSpacing * (++index), ShopShelfY, 0f));
        }

        private void CreateBoardFrame()
        {
            var frame = new GameObject("Mint Board");
            frame.transform.SetParent(transform, false);
            var renderer = frame.AddComponent<SpriteRenderer>();
            renderer.sprite = _whiteSprite;
            renderer.color = new Color32(175, 224, 181, 255);
            renderer.sortingOrder = -100;
            _boardFrame = frame.transform;
            UpdateBoardFrame();
        }

        /// <summary>
        /// 牌桌底色矩形为固定尺寸（按最大缩放 MaxOrthoSize 的视野铺满全屏并留 1 单位余量），
        /// 不跟随相机移动；仅在屏幕宽高比变化时重算宽度。
        /// </summary>
        private void UpdateBoardFrame()
        {
            if (_boardFrame == null || _camera == null) return;
            float aspect = _camera.aspect;
            if (_boardFrameAspect == aspect) return;
            _boardFrameAspect = aspect;
            float height = MaxOrthoSize * 2f + 1f;
            _boardFrame.localScale = new Vector3(height * aspect + 1f, height, 1f);
            _boardFrame.position = Vector3.zero;
        }

        /// <summary>
        /// 缩放只改相机 orthographicSize，范围 [MinOrthoSize, MaxOrthoSize]；缩放后按地图边界回收相机位置。
        /// </summary>
        private void Zoom(float amount)
        {
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize + amount, MinOrthoSize, MaxOrthoSize);
            ClampCameraPosition();
        }

        /// <summary>
        /// 地图边界为 MaxOrthoSize 时的视野范围（世界原点为中心）。
        /// 当前视野小于边界时，相机中心只允许在「边界减去当前视野」的剩余空间内移动；
        /// orthographicSize 等于 MaxOrthoSize 时剩余空间为 0，无法移动地图。
        /// </summary>
        private void ClampCameraPosition()
        {
            float slack = Mathf.Max(0f, MaxOrthoSize - _camera.orthographicSize);
            float maxX = slack * _camera.aspect;
            Vector3 position = _camera.transform.position;
            position.x = Mathf.Clamp(position.x, -maxX, maxX);
            position.y = Mathf.Clamp(position.y, -slack, slack);
            _camera.transform.position = position;
        }

        /// <summary>
        /// 从 UIRaw/Atlas/Card 图集加载 14 种颜色的牌面底图；键与 GameConfig.stacklands.ECardColor 名称一致。
        /// 图集贴图为多精灵导入，需按子资源取唯一的牌面 Sprite；加载未完成前 CardView 回退为纯色染色表现。
        /// </summary>
        private async UniTaskVoid LoadCardBackgroundsAsync()
        {
            string[] locations =
            {
                "card_bg_pink", "card_bg_black", "card_bg_red", "card_bg_gold", "card_bg_yellow",
                "card_bg_silver", "card_bg_white", "card_bg_green", "card_bg_blue", "card_bg_orange",
                "card_bg_light_orange", "card_bg_brown", "card_bg_purple", "card_bg_gray",
            };
            const string prefix = "card_bg_";
            await UniTask.WhenAll(locations.Select(async location =>
            {
                var handle = YooAssets.LoadSubAssetsAsync<Sprite>(location);
                bool cancelled = await handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy())
                    .SuppressCancellationThrow();
                Sprite sprite = cancelled ? null : handle.GetSubAssetObjects<Sprite>()?.FirstOrDefault();
                if (sprite == null)
                {
                    handle.Dispose();
                    return;
                }
                _cardSpriteHandles.Add(handle);
                _cardSprites[location.Substring(prefix.Length).ToUpperInvariant()] = sprite;
            }));

            // 默认/选中/整堆拖动三态边框与牌面底图同目录，加载完成后同步已存在的卡牌视图。
            string[] borderLocations = { "card_border_black", "card_border_white", "card_border_yellow" };
            await UniTask.WhenAll(borderLocations.Select(async location =>
            {
                var handle = YooAssets.LoadSubAssetsAsync<Sprite>(location);
                bool cancelled = await handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy())
                    .SuppressCancellationThrow();
                Sprite sprite = cancelled ? null : handle.GetSubAssetObjects<Sprite>()?.FirstOrDefault();
                if (sprite == null)
                {
                    handle.Dispose();
                    return;
                }
                _cardSpriteHandles.Add(handle);
                if (location.EndsWith("black")) _borderBlack = sprite;
                else if (location.EndsWith("white")) _borderWhite = sprite;
                else _borderYellow = sprite;
            }));
            foreach (CardView view in _cards.Values)
                if (view != null) view.SetBorderSprites(_borderBlack, _borderWhite, _borderYellow);

            // 卡槽底图同样走图集子资源加载，加载完成后同步已存在的出售槽和商店槽。
            var slotHandle = YooAssets.LoadSubAssetsAsync<Sprite>("slot_bg_black");
            bool slotCancelled = await slotHandle
                .ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy())
                .SuppressCancellationThrow();
            Sprite slot = slotCancelled ? null : slotHandle.GetSubAssetObjects<Sprite>()?.FirstOrDefault();
            if (slot == null)
            {
                slotHandle.Dispose();
            }
            else
            {
                _cardSpriteHandles.Add(slotHandle);
                _slotSprite = slot;
                if (_sellSlot != null) _sellSlot.Background = slot;
                foreach (ShopSlotView view in _shopSlots.Values)
                    if (view != null) view.Background = slot;
            }

            // 卡槽拖动反馈边框同样走图集子资源加载，加载完成后同步已存在的卡槽。
            string[] slotBorderLocations = { "slot_border_green", "slot_border_red" };
            await UniTask.WhenAll(slotBorderLocations.Select(async location =>
            {
                var handle = YooAssets.LoadSubAssetsAsync<Sprite>(location);
                bool cancelled = await handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy())
                    .SuppressCancellationThrow();
                Sprite sprite = cancelled ? null : handle.GetSubAssetObjects<Sprite>()?.FirstOrDefault();
                if (sprite == null)
                {
                    handle.Dispose();
                    return;
                }
                _cardSpriteHandles.Add(handle);
                if (location.EndsWith("green")) _slotBorderGreen = sprite;
                else _slotBorderRed = sprite;
            }));
            _sellSlot?.SetBorderSprites(_slotBorderGreen, _slotBorderRed);
            foreach (ShopSlotView view in _shopSlots.Values)
                if (view != null) view.SetBorderSprites(_slotBorderGreen, _slotBorderRed);

            // 卡包底图同样走图集子资源加载，加载完成后同步已存在的卡包视图。
            var boosterHandle = YooAssets.LoadSubAssetsAsync<Sprite>("booster_bg_black");
            bool boosterCancelled = await boosterHandle
                .ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy())
                .SuppressCancellationThrow();
            Sprite booster = boosterCancelled ? null : boosterHandle.GetSubAssetObjects<Sprite>()?.FirstOrDefault();
            if (booster == null)
            {
                boosterHandle.Dispose();
                return;
            }
            _cardSpriteHandles.Add(boosterHandle);
            _boosterSprite = booster;
            foreach (BoosterView view in _boosters.Values)
                if (view != null) view.Background = booster;
        }

        private void OnDestroy()
        {
            foreach (SubAssetsHandle handle in _cardSpriteHandles) handle.Dispose();
            _cardSpriteHandles.Clear();
            _cardSprites.Clear();
            _slotSprite = null;
            _slotBorderGreen = null;
            _slotBorderRed = null;
            _borderBlack = null;
            _borderWhite = null;
            _borderYellow = null;
        }

        /// <summary>
        /// EditMode 测试等不走 Awake 的场景下补建基础绘制资源，保证 Render/RenderHud 可直接调用。
        /// </summary>
        private void EnsureAssets()
        {
            if (_whiteSprite == null) _whiteSprite = CreateWhiteSprite();
            if (_font == null) _font = CreateChineseFont();
        }

        private static Sprite CreateWhiteSprite()
        {
            const int size = 32;
            const float radius = 5f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Runtime Rounded Card" };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x, x - (size - 1 - radius), 0f);
                float dy = Mathf.Max(radius - y, y - (size - 1 - radius), 0f);
                pixels[y * size + x] = dx * dx + dy * dy <= radius * radius ? Color.white : Color.clear;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Font CreateChineseFont()
        {
            string[] names = { "Noto Sans CJK SC", "PingFang SC", "Microsoft YaHei", "Arial Unicode MS", "Arial" };
            return Font.CreateDynamicFontFromOSFont(names, 36);
        }

        private Vector3 ScreenToWorld(Vector3 screen)
        {
            screen.z = -_camera.transform.position.z;
            Vector3 value = _camera.ScreenToWorldPoint(screen);
            value.z = 0f;
            return value;
        }

        private static void SendSpeed(float speed) => CoreSystem.SubmitCommand(
            new StacklandsCommandDto { Kind = StacklandsCommandKind.SetSpeed, Number = speed });

        private static bool PointerOverUi(Vector2 screenPosition)
        {
            foreach (UIDocument document in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                VisualElement root = document.rootVisualElement;
                if (root?.panel == null) continue;
                Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(root.panel, screenPosition);
                VisualElement picked = root.panel.Pick(panelPosition);
                if (picked != null && picked.pickingMode == PickingMode.Position && picked.name != "board-input-pass-through")
                    return true;
            }
            return false;
        }

    }
}
