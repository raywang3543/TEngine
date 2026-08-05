using GameLogic.Core.Model;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameLogic.Core.View
{
    /// <summary>
    /// 点击可佩戴单位后展开的装备卡堆中的单张装备卡；仅用于展示与点击卸下，不对应牌桌卡牌实例。
    /// </summary>
    internal sealed class EquippedCardView : MonoBehaviour
    {
        private const float Width = 1.1f;
        private const float Height = 1.5f;
        private const int FanSortingOrder = 500;
        private BoxCollider2D _collider;

        public EquipmentSlotKind Slot { get; private set; }

        /// <summary>
        /// 根节点不缩放，底图作为子节点按精灵实际尺寸换算缩放，碰撞体直接按世界尺寸设置。
        /// </summary>
        public static EquippedCardView Create(Transform parent, EquippedItemSnapshot data, Sprite fallback,
            Font font, Sprite background)
        {
            var go = new GameObject("Equipped " + data.CardId);
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<EquippedCardView>();
            view.Slot = data.Slot;
            var group = go.AddComponent<SortingGroup>();
            group.sortingOrder = FanSortingOrder;

            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(go.transform, false);
            var body = bodyObject.AddComponent<SpriteRenderer>();
            body.sprite = background != null ? background : fallback;
            body.color = Color.white;
            Vector2 size = body.sprite.bounds.size;
            bodyObject.transform.localScale = new Vector3(Width / size.x, Height / size.y, 1f);

            var textObject = new GameObject("Text");
            textObject.transform.SetParent(go.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            var text = textObject.AddComponent<TextMesh>();
            text.font = font;
            text.fontSize = 30;
            text.characterSize = 0.06f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.black;
            text.text = data.NameZh;
            if (font != null) text.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            text.GetComponent<MeshRenderer>().sortingOrder = 1;

            view._collider = go.AddComponent<BoxCollider2D>();
            view._collider.size = new Vector2(Width, Height);
            return view;
        }

        public bool Contains(Vector2 world)
        {
            return _collider.OverlapPoint(world);
        }
    }
}
