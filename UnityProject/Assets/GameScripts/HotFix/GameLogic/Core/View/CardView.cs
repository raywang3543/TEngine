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
        private const float CardWidth = 1.5f;
        private const float CardHeight = 2f;
        private const int DragSortingBase = 30000;
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
        private bool _selected;
        private bool _dragSorting;
        private bool _wholeStackDragFeedback;
        private int _stackOrder;

        public string InstanceId { get; private set; }
        public string StackId { get; private set; }

        public CardView Initialize(string id, Sprite sprite, Font font,
            IReadOnlyDictionary<string, Sprite> cardSprites)
        {
            InstanceId = id;
            _fallbackSprite = sprite;
            _cardSprites = cardSprites;
            _sortingGroup = gameObject.AddComponent<SortingGroup>();
            _outline = AddSprite("Outline", sprite, Color.black, -1,
                new Vector3(CardWidth + 0.12f, CardHeight + 0.12f));
            _body = AddSprite("Body", sprite, Color.white, 0, new Vector3(CardWidth, CardHeight));
            _title = AddText("Title", font, 36, TextAnchor.MiddleCenter, new Vector3(0, 0.55f, -0.05f));
            _footer = AddText("Footer", font, 22, TextAnchor.MiddleCenter, new Vector3(0, -0.67f, -0.05f));
            _wholeStackBadge = AddText("WholeStackBadge", font, 20, TextAnchor.UpperCenter,
                new Vector3(0, 0.96f, -0.08f));
            _wholeStackBadge.text = "整堆";
            _wholeStackBadge.color = new Color32(255, 226, 92, 255);
            _wholeStackBadge.gameObject.SetActive(false);
            gameObject.AddComponent<BoxCollider2D>().size = new Vector2(CardWidth, CardHeight);
            Transform track = AddSprite("ProgressTrack", sprite, Color.black, 2, new Vector3(1.25f, 0.09f)).transform;
            track.localPosition = new Vector3(0, -0.87f, -0.1f);
            _progress = AddSprite("Progress", sprite, new Color32(255, 238, 150, 255), 3,
                new Vector3(1.2f, 0.055f)).transform;
            _progress.SetParent(track, false);
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
                Vector2 size = body.bounds.size;
                _body.transform.localScale = new Vector3(CardWidth / size.x, CardHeight / size.y, 1f);
            }
            _body.color = tint;
            RefreshOutline();
            _title.text = BreakName(data.NameZh);
            _title.color = textColor;
            _footer.text = Footer(data);
            _footer.color = textColor;
            RenderProgress(data.Progress);
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

        public void SetDragSorting(bool active)
        {
            _dragSorting = active;
            RefreshSorting();
        }

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
            Sprite border = _wholeStackDragFeedback ? _borderYellow
                : _selected ? _borderWhite : _borderBlack;
            if (border != null)
            {
                // 边框图与牌面同尺寸、内芯透明，叠在牌面正上方。
                _outline.sprite = border;
                _outline.color = Color.white;
                _outline.sortingOrder = 4;
                Vector2 size = border.bounds.size;
                _outline.transform.localScale = new Vector3(CardWidth / size.x, CardHeight / size.y, 1f);
                return;
            }

            // 边框图集未加载完成前的回退：纯色 Sprite 放大后垫在牌面底下。
            _outline.sprite = _fallbackSprite;
            _outline.sortingOrder = -1;
            _outline.color = _wholeStackDragFeedback
                ? new Color32(255, 226, 92, 255)
                : _selected ? Color.white : Color.black;
            _outline.transform.localScale = _wholeStackDragFeedback
                ? new Vector3(CardWidth + 0.2f, CardHeight + 0.2f, 1f)
                : new Vector3(CardWidth + 0.12f, CardHeight + 0.12f, 1f);
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

        private static string BreakName(string name) => string.IsNullOrEmpty(name) ? "未命名" :
            name.Length <= 5 ? name : name.Substring(0, (name.Length + 1) / 2) + "\n" +
                                       name.Substring((name.Length + 1) / 2);

        private static string Footer(CardSnapshot data)
        {
            if (data.MaxHp > 0) return $"HP {data.Hp}/{data.MaxHp}";
            if (data.FoodValue > 0) return $"食 {data.FoodValue}  售 {data.SellPrice}";
            return (data.IsFoil ? "闪  " : string.Empty) + $"售 {data.SellPrice}";
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
