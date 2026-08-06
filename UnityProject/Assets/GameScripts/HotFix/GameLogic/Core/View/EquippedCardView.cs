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
        private const int FanSortingOrder = 500;
        private BoxCollider2D _collider;

        public EquipmentSlotKind Slot { get; private set; }

        /// <summary>
        /// 底图按图片原始尺寸渲染，碰撞体与底图同尺寸。
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
            view._collider.size = body.sprite.bounds.size;
            return view;
        }

        public bool Contains(Vector2 world)
        {
            return _collider.OverlapPoint(world);
        }
    }
}
