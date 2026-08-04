using UnityEngine;

namespace GameLogic.Core.View
{
    /// <summary>
    /// 出售投放槽的 Unity 2D 表现组件。
    /// </summary>
    internal sealed class SellSlotView : MonoBehaviour
    {
        private BoxCollider2D _collider;
        private TextMesh _text;

        public SellSlotView Initialize(Sprite sprite, Font font)
        {
            AddBackground(sprite, new Color32(10, 11, 10, 255));
            _text = AddSlotText(font);
            _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.size = new Vector2(1.4f, 1.55f);
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
            child.transform.localScale = new Vector3(1.4f, 1.55f, 1f);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = -60;
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
