using GameLogic.Core;
using UnityEngine;

namespace GameLogic.Core.View
{
    /// <summary>
    /// 卡包商店槽位的 Unity 2D 表现组件。
    /// </summary>
    internal sealed class ShopSlotView : MonoBehaviour
    {
        private SpriteRenderer _body;
        private TextMesh _text;

        public string BoosterId { get; private set; }
        public bool CanBuy { get; private set; }
        public int Order { get; set; }

        public ShopSlotView Initialize(string boosterId, Sprite sprite, Font font)
        {
            BoosterId = boosterId;
            var background = new GameObject("Background");
            background.transform.SetParent(transform, false);
            background.transform.localScale = new Vector3(1.35f, 1.55f, 1f);
            _body = background.AddComponent<SpriteRenderer>();
            _body.sprite = sprite;
            _body.sortingOrder = -60;

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

            gameObject.AddComponent<BoxCollider2D>().size = new Vector2(1.35f, 1.55f);
            return this;
        }

        public void Render(BoosterShopSnapshot snapshot, int coins)
        {
            CanBuy = snapshot.Unlocked && coins >= snapshot.Price;
            _body.color = !snapshot.Unlocked
                ? new Color32(50, 52, 48, 255)
                : CanBuy ? new Color32(8, 9, 8, 255) : new Color32(28, 29, 27, 255);
            _text.color = snapshot.Unlocked ? Color.white : new Color32(155, 155, 149, 255);
            _text.text = FormatSlotName(snapshot.NameZh) + "\n" +
                         (snapshot.Unlocked ? snapshot.Price + " 金币" : snapshot.LockText);
        }

        public void SetLayout(Vector3 position)
        {
            transform.position = position;
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
