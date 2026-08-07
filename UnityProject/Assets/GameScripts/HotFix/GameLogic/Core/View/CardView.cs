using System.Collections.Generic;
using GameLogic.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameLogic.Core.View
{
    /// <summary>
    /// 单张卡牌的 Unity 2D 表现组件。
    /// </summary>
    internal sealed class CardView : MonoBehaviour
    {
        private const int DragSortingBase = 30000;
        // 装备槽指示圆点：中下位置，左到右对应 Hand / Head / Body。
        private const float EquipmentDotY = -0.35f;
        private const float EquipmentDotSpacing = 0.2f;
        private const float EquipmentDotSize = 0.14f;
        private static Sprite _equipmentDotSprite;
        private readonly SpriteRenderer[] _equipmentDots = new SpriteRenderer[3];
        private SpriteRenderer _body;
        private SpriteRenderer _outline;
        private SortingGroup _sortingGroup;
        private TextMesh _title;
        private TextMesh _footer;
        private TextMesh _wholeStackBadge;
        private Transform _progress;
        private Sprite _fallbackSprite;
        private IReadOnlyDictionary<string, Sprite> _cardSprites;
        private Sprite _borderBlack;
        private Sprite _borderWhite;
        private Sprite _borderYellow;
        private BoxCollider2D _collider;
        private bool _selected;
        private bool _dragSorting;
        private bool _wholeStackDragFeedback;
        private bool _dropTargetFeedback;
        private int _stackOrder;

        public string InstanceId { get; private set; }
        public string StackId { get; private set; }

        /// <summary>牌面碰撞体的世界包围盒，用于拖动重叠检测。</summary>
        public Bounds ColliderBounds => _collider.bounds;

        public CardView Initialize(string id, Sprite sprite, Font font,
            IReadOnlyDictionary<string, Sprite> cardSprites)
        {
            InstanceId = id;
            _fallbackSprite = sprite;
            _cardSprites = cardSprites;
            _sortingGroup = gameObject.AddComponent<SortingGroup>();
            _outline = AddSprite("Outline", sprite, Color.black, -1, Vector3.one);
            _body = AddSprite("Body", sprite, Color.white, 0, Vector3.one);
            _title = AddText("Title", font, 36, TextAnchor.MiddleCenter, new Vector3(0, 0.55f, -0.05f));
            _footer = AddText("Footer", font, 22, TextAnchor.MiddleCenter, new Vector3(0, -0.67f, -0.05f));
            _wholeStackBadge = AddText("WholeStackBadge", font, 20, TextAnchor.UpperCenter,
                new Vector3(0, 0.96f, -0.08f));
            _wholeStackBadge.text = StacklandsTexts.WholeStackBadge;
            _wholeStackBadge.color = new Color32(255, 226, 92, 255);
            _wholeStackBadge.gameObject.SetActive(false);
            // 碰撞体跟随牌面图片的原始世界尺寸（图片按 PPU 烘焙为目标大小，不再代码缩放）。
            _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.size = sprite.bounds.size;
            Transform track = AddSprite("ProgressTrack", sprite, Color.black, 2, new Vector3(1.25f, 0.09f)).transform;
            track.localPosition = new Vector3(0, -0.87f, -0.1f);
            _progress = AddSprite("Progress", sprite, new Color32(255, 238, 150, 255), 3,
                new Vector3(1.2f, 0.055f)).transform;
            _progress.SetParent(track, false);
            if (_equipmentDotSprite == null) _equipmentDotSprite = CreateCircleSprite();
            for (int i = 0; i < _equipmentDots.Length; i++)
            {
                _equipmentDots[i] = AddSprite("EquipmentDot" + i, _equipmentDotSprite, Color.white, 2,
                    new Vector3(EquipmentDotSize, EquipmentDotSize, 1f));
                _equipmentDots[i].transform.localPosition =
                    new Vector3((i - 1) * EquipmentDotSpacing, EquipmentDotY, -0.08f);
                _equipmentDots[i].gameObject.SetActive(false);
            }
            return this;
        }

        public void Render(CardSnapshot data, bool selected)
        {
            StackId = data.StackId;
            _stackOrder = data.StackOrder;
            _selected = selected;
            transform.position = new Vector3(data.X, data.Y - data.StackOrder * 0.32f, -data.StackOrder * 0.01f);
            RefreshSorting();
            Sprite body = ResolveBodySprite(data, out Color tint, out Color textColor);
            if (_body.sprite != body)
            {
                _body.sprite = body;
                _collider.size = body.bounds.size;
            }
            _body.color = tint;
            RefreshOutline();
            _title.text = BreakName(data.NameZh);
            _title.color = textColor;
            _footer.text = Footer(data);
            _footer.color = textColor;
            RenderEquipmentDots(data);
            RenderProgress(data.Progress);
        }

        /// <summary>
        /// 可佩戴装备的单位在牌面中下位置显示 3 个实心圆，左到右对应 Hand / Head / Body；
        /// 白色为空槽，黑色为已有装备。非佩戴单位隐藏圆点。
        /// </summary>
        private void RenderEquipmentDots(CardSnapshot data)
        {
            bool[] filled = { data.HasHandEquipment, data.HasHeadEquipment, data.HasBodyEquipment };
            for (int i = 0; i < _equipmentDots.Length; i++)
            {
                _equipmentDots[i].gameObject.SetActive(data.CanEquip);
                if (data.CanEquip) _equipmentDots[i].color = filled[i] ? Color.black : Color.white;
            }
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 32;
            const float radius = (size - 1) * 0.5f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Runtime Equipment Dot" };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Mathf.Sqrt((x - radius) * (x - radius) + (y - radius) * (y - radius));
                float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// 按卡牌颜色选取图集牌面底图；底图未加载时回退为纯色染色 sprite。
        /// </summary>
        private Sprite ResolveBodySprite(CardSnapshot data, out Color tint, out Color textColor)
        {
            if (!string.IsNullOrEmpty(data.Color) && _cardSprites != null &&
                _cardSprites.TryGetValue(data.Color, out Sprite sprite) && sprite != null)
            {
                tint = Color.white;
                // 黑色资源节点卡底图过暗，文字反白。
                textColor = data.Color == "BLACK" ? Color.white : Color.black;
                return sprite;
            }

            tint = ParseColor(data.Color, data.Category);
            textColor = Color.black;
            return _fallbackSprite;
        }

        public void RenderProgress(float value)
        {
            float progress = Mathf.Clamp01(value);
            _progress.localScale = new Vector3(progress, 1f, 1f);
            _progress.localPosition = new Vector3((progress - 1f) * 0.5f, 0f, -0.01f);
            _progress.parent.gameObject.SetActive(progress > 0f && progress < 1f);
        }

        public void SetWholeStackDragFeedback(bool active, bool showBadge)
        {
            _wholeStackDragFeedback = active;
            _wholeStackBadge.gameObject.SetActive(active && showBadge);
            RefreshOutline();
        }

        /// <summary>拖动途中被牌面重叠命中的堆叠目标：黄色边框提示，与整堆拖动同色但不显示角标。</summary>
        public void SetDropTargetFeedback(bool active)
        {
            if (_dropTargetFeedback == active) return;
            _dropTargetFeedback = active;
            RefreshOutline();
        }

        public void SetDragSorting(bool active)
        {
            _dragSorting = active;
            RefreshSorting();
        }

        /// <summary>进食飞行等脚本动画期间关闭碰撞，避免飞行动画中的卡牌被指针选中或拖动。</summary>
        public void SetColliderEnabled(bool value) => _collider.enabled = value;

        private void RefreshSorting()
        {
            _sortingGroup.sortingOrder = (_dragSorting ? DragSortingBase : 0) + _stackOrder;
        }

        /// <summary>
        /// 设置默认/选中/整堆拖动三态边框图集 Sprite；为 null 时回退为代码纯色描边。
        /// </summary>
        public void SetBorderSprites(Sprite black, Sprite white, Sprite yellow)
        {
            _borderBlack = black;
            _borderWhite = white;
            _borderYellow = yellow;
            RefreshOutline();
        }

        private void RefreshOutline()
        {
            bool highlight = _wholeStackDragFeedback || _dropTargetFeedback;
            Sprite border = highlight ? _borderYellow
                : _selected ? _borderWhite : _borderBlack;
            if (border != null)
            {
                // 边框图与牌面同尺寸、内芯透明，按原始大小叠在牌面正上方。
                _outline.sprite = border;
                _outline.color = Color.white;
                _outline.sortingOrder = 4;
                _outline.transform.localScale = Vector3.one;
                return;
            }

            // 边框图集未加载完成前的回退：纯色 Sprite 放大后垫在牌面底下。
            Vector2 bodySize = _body.sprite.bounds.size;
            _outline.sprite = _fallbackSprite;
            _outline.sortingOrder = -1;
            _outline.color = highlight
                ? new Color32(255, 226, 92, 255)
                : _selected ? Color.white : Color.black;
            _outline.transform.localScale = highlight
                ? new Vector3(bodySize.x + 0.2f, bodySize.y + 0.2f, 1f)
                : new Vector3(bodySize.x + 0.12f, bodySize.y + 0.12f, 1f);
        }

        private SpriteRenderer AddSprite(string objectName, Sprite sprite, Color color, int order, Vector3 scale)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            child.transform.localScale = scale;
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private TextMesh AddText(string objectName, Font font, int size, TextAnchor anchor, Vector3 position)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            child.transform.localPosition = position;
            var mesh = child.AddComponent<TextMesh>();
            mesh.font = font;
            mesh.fontSize = size;
            mesh.characterSize = 0.06f;
            mesh.anchor = anchor;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.black;
            if (font != null) mesh.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            mesh.GetComponent<MeshRenderer>().sortingOrder = 10;
            return mesh;
        }

        private static string BreakName(string name) => string.IsNullOrEmpty(name) ? StacklandsTexts.Unnamed :
            name.Length <= 5 ? name : name.Substring(0, (name.Length + 1) / 2) + "\n" +
                                       name.Substring((name.Length + 1) / 2);

        private static string Footer(CardSnapshot data)
        {
            if (data.MaxHp > 0) return StacklandsTexts.CardHp(data.Hp, data.MaxHp);
            if (data.FoodValue > 0) return StacklandsTexts.CardFoodFooter(data.FoodValue, data.SellPrice);
            return (data.IsFoil ? StacklandsTexts.FoilPrefix : string.Empty) +
                   StacklandsTexts.CardSellFooter(data.SellPrice);
        }

        private static Color ParseColor(string color, string category)
        {
            string key = (color + category).ToUpperInvariant();
            if (key.Contains("FOOD")) return new Color32(244, 166, 104, 255);
            if (key.Contains("STRUCTURE")) return new Color32(242, 150, 146, 255);
            if (key.Contains("ANIMAL")) return new Color32(164, 112, 75, 255);
            if (key.Contains("VILLAGER")) return new Color32(246, 211, 107, 255);
            if (key.Contains("LOCATION")) return new Color32(168, 143, 197, 255);
            if (key.Contains("ENEMY")) return new Color32(205, 91, 81, 255);
            if (key.Contains("IDEA") || key.Contains("RUMOR")) return new Color32(115, 139, 164, 255);
            return new Color32(131, 151, 175, 255);
        }
    }
}
