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
        private const float BoosterWidth = 1.6f;
        private const float BoosterHeight = 2.2f;
        private const int NormalSortingOrder = 20;
        private const int DragSortingOrder = 30000;
        private SortingGroup _sortingGroup;
        private SpriteRenderer _body;
        private TextMesh _text;
        private BoxCollider2D _collider;

        public string InstanceId { get; private set; }

        /// <summary>
        /// 图集卡包底图；为 null 时保持纯色回退表现。
        /// </summary>
        public Sprite Background
        {
            set
            {
                if (value == null || _body == null || _body.sprite == value) return;
                _body.sprite = value;
                _body.color = Color.white;
                FitToSprite(value);
            }
        }

        public BoosterView Initialize(string id, Sprite sprite, Font font)
        {
            InstanceId = id;
            _sortingGroup = gameObject.AddComponent<SortingGroup>();
            _sortingGroup.sortingOrder = NormalSortingOrder;
            _body = gameObject.AddComponent<SpriteRenderer>();
            _body.sprite = sprite;
            _body.color = Color.black;
            _body.sortingOrder = 0;
            transform.localScale = new Vector3(BoosterWidth, BoosterHeight, 1f);

            var textObject = new GameObject("Text");
            textObject.transform.SetParent(transform, false);
            _text = textObject.AddComponent<TextMesh>();
            _text.font = font;
            _text.fontSize = 34;
            _text.characterSize = 0.1f;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.color = Color.white;
            if (font != null) _text.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            _text.GetComponent<MeshRenderer>().sortingOrder = 1;
            FitText();
            _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.size = Vector2.one;
            return this;
        }

        /// <summary>
        /// 按底图实际尺寸换算缩放，不依赖贴图 Pixels Per Unit 设置。
        /// 碰撞体尺寸同步为精灵 bounds，保证触摸范围始终覆盖整个卡包显示区域。
        /// </summary>
        private void FitToSprite(Sprite sprite)
        {
            Vector2 size = sprite.bounds.size;
            transform.localScale = new Vector3(BoosterWidth / size.x, BoosterHeight / size.y, 1f);
            _collider.size = size;
            FitText();
        }

        /// <summary>
        /// 抵消卡包节点缩放，让文字保持 1:1 的世界尺寸。
        /// </summary>
        private void FitText()
        {
            Vector3 scale = transform.localScale;
            _text.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f);
            _text.transform.localPosition = new Vector3(0, 0, -0.05f);
        }

        public void Render(BoosterSnapshot data)
        {
            transform.position = new Vector3(data.X, data.Y, -0.2f);
            _text.text = StacklandsTexts.BoosterText(data.NameZh, data.Remaining);
        }

        public void SetDragSorting(bool active)
        {
            _sortingGroup.sortingOrder = active ? DragSortingOrder : NormalSortingOrder;
        }
    }
}
