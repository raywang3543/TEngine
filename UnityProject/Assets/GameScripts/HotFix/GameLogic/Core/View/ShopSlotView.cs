using GameLogic.Core;
using UnityEngine;

namespace GameLogic.Core.View
{
    /// <summary>
    /// 卡包商店槽位的 Unity 2D 表现组件。
    /// </summary>
    internal sealed class ShopSlotView : MonoBehaviour
    {
        private const float SlotWidth = 1.35f;
        private const float SlotHeight = 1.55f;
        // 图集底图 slot_bg_black 的烘焙填充色；底图未加载时回退为白色 Sprite 直接染色。
        private static readonly Color32 SlotImageBase = new Color32(10, 11, 10, 255);
        private SpriteRenderer _body;
        private TextMesh _text;
        private Color _tintBase = Color.white;
        private Color32 _lastTarget = SlotImageBase;

        public string BoosterId { get; private set; }
        public bool CanBuy { get; private set; }
        public int Order { get; set; }

        /// <summary>
        /// 图集卡槽底图；为 null 时保持白色 Sprite 染色的回退表现。
        /// </summary>
        public Sprite Background
        {
            set
            {
                if (value == null || _body == null || _body.sprite == value) return;
                _body.sprite = value;
                _tintBase = SlotImageBase;
                FitToSprite(value);
                _body.color = TintFor(_lastTarget);
            }
        }

        public ShopSlotView Initialize(string boosterId, Sprite sprite, Font font)
        {
            BoosterId = boosterId;
            var background = new GameObject("Background");
            background.transform.SetParent(transform, false);
            _body = background.AddComponent<SpriteRenderer>();
            _body.sprite = sprite;
            _body.sortingOrder = -60;
            FitToSprite(sprite);

            var textObject = new GameObject("Text");
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            _text = textObject.AddComponent<TextMesh>();
            _text.font = font;
            _text.fontSize = 25;
            _text.characterSize = 0.05f;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.color = Color.white;
            if (font != null) _text.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            _text.GetComponent<MeshRenderer>().sortingOrder = -55;

            gameObject.AddComponent<BoxCollider2D>().size = new Vector2(SlotWidth, SlotHeight);
            return this;
        }

        public void Render(BoosterShopSnapshot snapshot, int coins)
        {
            CanBuy = snapshot.Unlocked && coins >= snapshot.Price;
            _lastTarget = !snapshot.Unlocked
                ? new Color32(50, 52, 48, 255)
                : CanBuy ? new Color32(8, 9, 8, 255) : new Color32(28, 29, 27, 255);
            _body.color = TintFor(_lastTarget);
            _text.color = snapshot.Unlocked ? Color.white : new Color32(155, 155, 149, 255);
            _text.text = FormatSlotName(snapshot.NameZh) + "\n" +
                         (snapshot.Unlocked ? snapshot.Price + " 金币" : snapshot.LockText);
        }

        public void SetLayout(Vector3 position)
        {
            transform.position = position;
        }

        /// <summary>
        /// 按底图实际尺寸换算缩放，不依赖贴图 Pixels Per Unit 设置。
        /// </summary>
        private void FitToSprite(Sprite sprite)
        {
            Vector2 size = sprite.bounds.size;
            _body.transform.localScale = new Vector3(SlotWidth / size.x, SlotHeight / size.y, 1f);
        }

        /// <summary>
        /// 底图已烘焙近黑色时按倍率还原目标色，白色回退 Sprite 时等价于直接染色。
        /// </summary>
        private Color TintFor(Color32 target)
        {
            return new Color(target.r / 255f / _tintBase.r,
                target.g / 255f / _tintBase.g, target.b / 255f / _tintBase.b);
        }

        private static string FormatSlotName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "未命名";
            return name.Length <= 5
                ? name
                : name.Substring(0, (name.Length + 1) / 2) + "\n" + name.Substring((name.Length + 1) / 2);
        }
    }
}
