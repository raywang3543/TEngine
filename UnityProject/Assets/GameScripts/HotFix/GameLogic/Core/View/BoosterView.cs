using GameLogic.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameLogic.Core.View
{
    /// <summary>
    /// 已购买卡包的 Unity 2D 表现组件。
    /// </summary>
    internal sealed class BoosterView : MonoBehaviour
    {
        private const int NormalSortingOrder = 20;
        private const int DragSortingOrder = 30000;
        private SortingGroup _sortingGroup;
        private TextMesh _text;

        public string InstanceId { get; private set; }

        public BoosterView Initialize(string id, Sprite sprite, Font font)
        {
            InstanceId = id;
            _sortingGroup = gameObject.AddComponent<SortingGroup>();
            _sortingGroup.sortingOrder = NormalSortingOrder;
            var body = gameObject.AddComponent<SpriteRenderer>();
            body.sprite = sprite;
            body.color = Color.black;
            body.sortingOrder = 0;
            transform.localScale = new Vector3(1.6f, 2.2f, 1f);

            var textObject = new GameObject("Text");
            textObject.transform.SetParent(transform, false);
            textObject.transform.localScale = new Vector3(0.625f, 0.455f, 1f);
            textObject.transform.localPosition = new Vector3(0, 0, -0.05f);
            _text = textObject.AddComponent<TextMesh>();
            _text.font = font;
            _text.fontSize = 34;
            _text.characterSize = 0.1f;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.color = Color.white;
            if (font != null) _text.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            _text.GetComponent<MeshRenderer>().sortingOrder = 1;
            gameObject.AddComponent<BoxCollider2D>().size = Vector2.one;
            return this;
        }

        public void Render(BoosterSnapshot data)
        {
            transform.position = new Vector3(data.X, data.Y, -0.2f);
            _text.text = data.NameZh + "\n剩余 " + data.Remaining;
        }

        public void SetDragSorting(bool active)
        {
            _sortingGroup.sortingOrder = active ? DragSortingOrder : NormalSortingOrder;
        }
    }
}
