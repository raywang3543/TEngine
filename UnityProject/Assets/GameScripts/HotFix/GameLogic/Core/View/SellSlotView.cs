using UnityEngine;

namespace GameLogic.Core.View
{
    /// <summary>
    /// 出售投放槽的 Unity 2D 表现组件。
    /// </summary>
    internal sealed class SellSlotView : MonoBehaviour
    {
        private const float SlotWidth = 1.4f;
        private const float SlotHeight = 1.55f;
        private BoxCollider2D _collider;
        private SpriteRenderer _body;
        private TextMesh _text;

        /// <summary>
        /// 图集卡槽底图；底图已烘焙出售槽目标色，为 null 时保持白色 Sprite 染色的回退表现。
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

        public SellSlotView Initialize(Sprite sprite, Font font)
        {
            AddBackground(sprite, new Color32(10, 11, 10, 255));
            _text = AddSlotText(font);
            _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.size = new Vector2(SlotWidth, SlotHeight);
            Render(0);
            return this;
        }

        public void Render(int coins)
        {
            _text.text = "出售\n金币 " + coins;
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
            FitToSprite(sprite);
        }

        /// <summary>
        /// 按底图实际尺寸换算缩放，不依赖贴图 Pixels Per Unit 设置。
        /// </summary>
        private void FitToSprite(Sprite sprite)
        {
            Vector2 size = sprite.bounds.size;
            _body.transform.localScale = new Vector3(SlotWidth / size.x, SlotHeight / size.y, 1f);
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
