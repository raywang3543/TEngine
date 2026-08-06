using UnityEngine;

namespace GameLogic.Core.View
{
    /// <summary>
    /// 出售投放槽的 Unity 2D 表现组件。
    /// </summary>
    internal sealed class SellSlotView : MonoBehaviour
    {
        private BoxCollider2D _collider;
        private SpriteRenderer _body;
        private SpriteRenderer _border;
        private Sprite _borderGreen;
        private Sprite _borderRed;
        private TextMesh _text;

        /// <summary>
        /// 拖动悬停反馈边框图（绿=可出售，红=不可出售），默认隐藏。
        /// </summary>
        public void SetBorderSprites(Sprite green, Sprite red)
        {
            _borderGreen = green;
            _borderRed = red;
        }

        /// <summary>
        /// positive 显示绿色边框，否则红色；边框图未加载完成时不显示。
        /// </summary>
        public void ShowBorder(bool positive)
        {
            if (_border == null) return;
            Sprite sprite = positive ? _borderGreen : _borderRed;
            if (sprite == null) return;
            _border.sprite = sprite;
            _border.gameObject.SetActive(true);
        }

        public void HideBorder()
        {
            if (_border != null) _border.gameObject.SetActive(false);
        }

        /// <summary>
        /// 图集卡槽底图，按图片原始尺寸渲染；底图已烘焙出售槽目标色，为 null 时保持白色 Sprite 染色的回退表现。
        /// </summary>
        public Sprite Background
        {
            set
            {
                if (value == null || _body == null || _body.sprite == value) return;
                _body.sprite = value;
                _body.color = Color.white;
                _collider.size = value.bounds.size;
            }
        }

        public SellSlotView Initialize(Sprite sprite, Font font)
        {
            AddBackground(sprite, new Color32(10, 11, 10, 255));
            _text = AddSlotText(font);
            _collider = gameObject.AddComponent<BoxCollider2D>();
            // 碰撞体跟随底图的原始世界尺寸（EditMode 测试等场景下底图为 null 时保持默认 1x1）。
            if (_body.sprite != null) _collider.size = _body.sprite.bounds.size;
            Render(0);
            return this;
        }

        public void Render(int coins)
        {
            _text.text = StacklandsTexts.SellSlot(coins);
        }

        public bool Contains(Vector2 worldPosition) => _collider != null && _collider.OverlapPoint(worldPosition);

        public void SetLayout(Vector3 position)
        {
            transform.position = position;
        }

        private void AddBackground(Sprite sprite, Color color)
        {
            var child = new GameObject("Background");
            child.transform.SetParent(transform, false);
            _body = child.AddComponent<SpriteRenderer>();
            _body.sprite = sprite;
            _body.color = color;
            _body.sortingOrder = -60;

            // 边框与底图同为原始尺寸，作为子节点即可对齐，默认隐藏。
            var border = new GameObject("Border");
            border.transform.SetParent(child.transform, false);
            _border = border.AddComponent<SpriteRenderer>();
            _border.sortingOrder = -54;
            border.SetActive(false);
        }

        private TextMesh AddSlotText(Font font)
        {
            var child = new GameObject("Text");
            child.transform.SetParent(transform, false);
            child.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            var mesh = child.AddComponent<TextMesh>();
            mesh.font = font;
            mesh.fontSize = 30;
            mesh.characterSize = 0.055f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
            if (font != null) mesh.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            mesh.GetComponent<MeshRenderer>().sortingOrder = -55;
            return mesh;
        }
    }
}
