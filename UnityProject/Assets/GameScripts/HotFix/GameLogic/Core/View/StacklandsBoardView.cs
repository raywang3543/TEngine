using System.Collections.Generic;
using System.Linq;
using GameLogic.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLogic.Core.View
{
    /// <summary>
    /// Stacklands 牌桌的 Unity 2D 表现层，只消费快照并发送命令。
    /// </summary>
    public sealed class StacklandsBoardView : MonoBehaviour
    {
        private const float CardWidth = 1.5f;
        private const float CardHeight = 2f;
        private readonly Dictionary<string, CardView> _cards = new Dictionary<string, CardView>();
        private readonly Dictionary<string, BoosterView> _boosters = new Dictionary<string, BoosterView>();
        private Camera _camera;
        private Sprite _whiteSprite;
        private Font _font;
        private CardView _draggedCard;
        private Vector3 _dragOffset;
        private Vector3 _lastPointer;
        private bool _panning;
        private float _touchDragStartedAt;

        private void Awake()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                var cameraObject = new GameObject("Stacklands Board Camera");
                _camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            _camera.orthographic = true;
            _camera.orthographicSize = 7f;
            _camera.transform.position = new Vector3(0f, 0f, -10f);
            _camera.rect = new Rect(0.18f, 0f, 0.82f, 0.86f);
            _camera.backgroundColor = new Color32(170, 218, 174, 255);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            foreach (Camera overlayCamera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (overlayCamera != _camera && overlayCamera.depth > _camera.depth &&
                    overlayCamera.cullingMask == 1 << LayerMask.NameToLayer("UI"))
                    overlayCamera.enabled = false;
            }
            _whiteSprite = CreateWhiteSprite();
            _font = CreateChineseFont();
            CreateBoardFrame();
            CreateDecorations();
        }

        private void Update()
        {
            HandleKeyboard();
            HandleMouse();
            HandleTouch();
        }

        public void Render(BoardSnapshot snapshot)
        {
            if (snapshot == null) return;
            var cardIds = new HashSet<string>(snapshot.Cards.Select(item => item.InstanceId));
            foreach (string id in _cards.Keys.Where(id => !cardIds.Contains(id)).ToArray())
            {
                Destroy(_cards[id].gameObject);
                _cards.Remove(id);
            }

            foreach (CardSnapshot card in snapshot.Cards)
            {
                if (!_cards.TryGetValue(card.InstanceId, out CardView view))
                {
                    view = CreateCard(card.InstanceId);
                    _cards.Add(card.InstanceId, view);
                }
                view.Render(card, card.InstanceId == snapshot.SelectedInstanceId);
            }

            var boosterIds = new HashSet<string>(snapshot.Boosters.Select(item => item.InstanceId));
            foreach (string id in _boosters.Keys.Where(id => !boosterIds.Contains(id)).ToArray())
            {
                Destroy(_boosters[id].gameObject);
                _boosters.Remove(id);
            }

            foreach (BoosterSnapshot booster in snapshot.Boosters)
            {
                if (!_boosters.TryGetValue(booster.InstanceId, out BoosterView view))
                {
                    view = CreateBooster(booster.InstanceId);
                    _boosters.Add(booster.InstanceId, view);
                }
                view.Render(booster);
            }
        }

        private void HandleKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Alpha1)) SendSpeed(0f);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SendSpeed(1f);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SendSpeed(5f);
            if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals)) Zoom(-0.7f);
            if (Input.GetKeyDown(KeyCode.Minus)) Zoom(0.7f);
        }

        private void HandleMouse()
        {
            Vector3 mouse = Input.mousePosition;
            if (Input.mouseScrollDelta.y != 0f && !PointerOverUi(mouse)) Zoom(-Input.mouseScrollDelta.y * 0.5f);

            if ((Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) && !PointerOverUi(mouse))
            {
                _panning = true;
                _lastPointer = mouse;
            }
            if (_panning && (Input.GetMouseButton(1) || Input.GetMouseButton(2)))
            {
                Vector3 before = ScreenToWorld(_lastPointer);
                Vector3 after = ScreenToWorld(mouse);
                _camera.transform.position += before - after;
                _lastPointer = mouse;
            }
            if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2)) _panning = false;

            if (Input.GetMouseButtonDown(0) && !PointerOverUi(mouse)) BeginPointer(mouse);
            if (Input.GetMouseButton(0) && _draggedCard != null)
                _draggedCard.transform.position = ScreenToWorld(mouse) + _dragOffset;
            if (Input.GetMouseButtonUp(0)) EndPointer(mouse, Input.GetKey(KeyCode.LeftShift));
        }

        private void HandleTouch()
        {
            if (Input.touchCount == 2)
            {
                Touch a = Input.GetTouch(0);
                Touch b = Input.GetTouch(1);
                float previous = ((a.position - a.deltaPosition) - (b.position - b.deltaPosition)).magnitude;
                Zoom((previous - (a.position - b.position).magnitude) * 0.012f);
                return;
            }
            if (Input.touchCount != 1) return;
            Touch touch = Input.GetTouch(0);
            if (PointerOverUi(touch.position)) return;
            if (touch.phase == TouchPhase.Began)
            {
                BeginPointer(touch.position);
                _touchDragStartedAt = Time.unscaledTime;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                if (_draggedCard != null) _draggedCard.transform.position = ScreenToWorld(touch.position) + _dragOffset;
                else _camera.transform.position -= (Vector3)touch.deltaPosition * (_camera.orthographicSize / 450f);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                EndPointer(touch.position, _draggedCard != null && Time.unscaledTime - _touchDragStartedAt >= 0.35f);
        }

        private void BeginPointer(Vector3 pointer)
        {
            Vector2 world = ScreenToWorld(pointer);
            Collider2D hit = Physics2D.OverlapPointAll(world)
                .OrderByDescending(item => item.transform.position.z).FirstOrDefault();
            if (hit == null) return;
            BoosterView booster = hit.GetComponent<BoosterView>();
            if (booster != null)
            {
                CoreSystem.SubmitCommand(new StacklandsCommandDto
                {
                    Kind = StacklandsCommandKind.OpenBooster, InstanceId = booster.InstanceId,
                });
                return;
            }
            _draggedCard = hit.GetComponent<CardView>();
            if (_draggedCard == null) return;
            _dragOffset = _draggedCard.transform.position - (Vector3)world;
            CoreSystem.SubmitCommand(new StacklandsCommandDto
            {
                Kind = StacklandsCommandKind.SelectCard, InstanceId = _draggedCard.InstanceId,
            });
        }

        private void EndPointer(Vector3 pointer, bool wholeStack)
        {
            if (_draggedCard == null) return;
            Vector2 world = ScreenToWorld(pointer);
            string draggedId = _draggedCard.InstanceId;
            _draggedCard = null;
            if (IsSellDrop(pointer))
            {
                CoreSystem.SubmitCommand(new StacklandsCommandDto
                {
                    Kind = StacklandsCommandKind.SellCard, InstanceId = draggedId,
                });
                return;
            }
            Collider2D target = Physics2D.OverlapPointAll(world)
                .FirstOrDefault(item => item.GetComponent<CardView>() != null &&
                                        item.GetComponent<CardView>().InstanceId != draggedId);
            CoreSystem.SubmitCommand(new StacklandsCommandDto
            {
                Kind = wholeStack ? StacklandsCommandKind.MoveStack : StacklandsCommandKind.MoveCard,
                InstanceId = draggedId,
                TargetInstanceId = target == null ? null : target.GetComponent<CardView>().InstanceId,
                X = world.x,
                Y = world.y,
            });
        }

        private static bool IsSellDrop(Vector2 pointer)
        {
            float x = pointer.x / Screen.width;
            float y = pointer.y / Screen.height;
            return x >= 0.18f && x <= 0.27f && y >= 0.86f;
        }

        private CardView CreateCard(string id)
        {
            var go = new GameObject("Card " + id);
            go.transform.SetParent(transform, false);
            return go.AddComponent<CardView>().Initialize(id, _whiteSprite, _font);
        }

        private BoosterView CreateBooster(string id)
        {
            var go = new GameObject("Booster " + id);
            go.transform.SetParent(transform, false);
            return go.AddComponent<BoosterView>().Initialize(id, _whiteSprite, _font);
        }

        private void CreateBoardFrame()
        {
            var frame = new GameObject("Mint Board");
            frame.transform.SetParent(transform, false);
            var renderer = frame.AddComponent<SpriteRenderer>();
            renderer.sprite = _whiteSprite;
            renderer.color = new Color32(175, 224, 181, 255);
            renderer.sortingOrder = -100;
            frame.transform.localScale = new Vector3(22f, 13f, 1f);
        }

        private void CreateDecorations()
        {
            Color ink = new Color(0.18f, 0.38f, 0.24f, 0.22f);
            CreateLine("Pine Left", new Vector2(-8.7f, -4.7f), ink,
                new Vector2(0, 0), new Vector2(0.8f, 1.4f), new Vector2(0.35f, 1.25f),
                new Vector2(1f, 2.3f), new Vector2(0.55f, 2.1f), new Vector2(1.15f, 3.1f),
                new Vector2(1.75f, 2.1f), new Vector2(1.3f, 2.3f), new Vector2(1.95f, 1.25f),
                new Vector2(1.5f, 1.4f), new Vector2(2.3f, 0), new Vector2(0, 0));
            CreateLine("Bush Right", new Vector2(7.3f, -4.5f), ink,
                new Vector2(0, 0), new Vector2(0.15f, 0.65f), new Vector2(0.55f, 0.95f),
                new Vector2(0.9f, 0.72f), new Vector2(1.25f, 1.05f), new Vector2(1.7f, 0.7f),
                new Vector2(1.85f, 0), new Vector2(0, 0));
            for (int i = 0; i < 7; i++)
            {
                float x = -7.2f + i * 2.3f;
                float y = i % 2 == 0 ? 4.4f : -3.4f;
                CreateLine("Grass " + i, new Vector2(x, y), ink,
                    new Vector2(-0.3f, 0), new Vector2(-0.1f, 0.45f), new Vector2(0, 0),
                    new Vector2(0.15f, 0.55f), new Vector2(0.2f, 0), new Vector2(0.45f, 0.38f));
            }
        }

        private void CreateLine(string objectName, Vector2 origin, Color color, params Vector2[] points)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(origin.x, origin.y, 0f);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false; line.loop = false; line.positionCount = points.Length;
            line.startWidth = 0.035f; line.endWidth = 0.035f;
            line.startColor = color; line.endColor = color; line.sortingOrder = -90;
            line.material = new Material(Shader.Find("Sprites/Default"));
            for (int i = 0; i < points.Length; i++) line.SetPosition(i, points[i]);
        }

        private static Sprite CreateWhiteSprite()
        {
            const int size = 32;
            const float radius = 5f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Runtime Rounded Card" };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x, x - (size - 1 - radius), 0f);
                float dy = Mathf.Max(radius - y, y - (size - 1 - radius), 0f);
                pixels[y * size + x] = dx * dx + dy * dy <= radius * radius ? Color.white : Color.clear;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Font CreateChineseFont()
        {
            string[] names = { "Noto Sans CJK SC", "PingFang SC", "Microsoft YaHei", "Arial Unicode MS", "Arial" };
            return Font.CreateDynamicFontFromOSFont(names, 36);
        }

        private Vector3 ScreenToWorld(Vector3 screen)
        {
            screen.z = -_camera.transform.position.z;
            Vector3 value = _camera.ScreenToWorldPoint(screen);
            value.z = 0f;
            return value;
        }

        private void Zoom(float amount) => _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize + amount, 3.5f, 14f);
        private static void SendSpeed(float speed) => CoreSystem.SubmitCommand(
            new StacklandsCommandDto { Kind = StacklandsCommandKind.SetSpeed, Number = speed });

        private static bool PointerOverUi(Vector2 screenPosition)
        {
            foreach (UIDocument document in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                VisualElement root = document.rootVisualElement;
                if (root?.panel == null) continue;
                Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(root.panel, screenPosition);
                VisualElement picked = root.panel.Pick(panelPosition);
                if (picked != null && picked.pickingMode == PickingMode.Position && picked.name != "board-input-pass-through")
                    return true;
            }
            return false;
        }

        private sealed class CardView : MonoBehaviour
        {
            private SpriteRenderer _body;
            private SpriteRenderer _outline;
            private TextMesh _title;
            private TextMesh _footer;
            private Transform _progress;
            public string InstanceId { get; private set; }

            public CardView Initialize(string id, Sprite sprite, Font font)
            {
                InstanceId = id;
                _outline = AddSprite("Outline", sprite, Color.black, -1, new Vector3(CardWidth + 0.12f, CardHeight + 0.12f));
                _body = AddSprite("Body", sprite, Color.white, 0, new Vector3(CardWidth, CardHeight));
                _title = AddText("Title", font, 36, TextAnchor.MiddleCenter, new Vector3(0, 0.55f, -0.05f));
                _footer = AddText("Footer", font, 22, TextAnchor.MiddleCenter, new Vector3(0, -0.67f, -0.05f));
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
                transform.position = new Vector3(data.X, data.Y - data.StackOrder * 0.32f, -data.StackOrder * 0.01f);
                _body.color = ParseColor(data.Color, data.Category);
                _outline.color = selected ? Color.white : Color.black;
                _title.text = BreakName(data.NameZh);
                _footer.text = Footer(data);
                float progress = Mathf.Clamp01(data.Progress);
                _progress.localScale = new Vector3(progress, 1f, 1f);
                _progress.localPosition = new Vector3((progress - 1f) * 0.5f, 0f, -0.01f);
                _progress.parent.gameObject.SetActive(progress > 0f && progress < 1f);
            }

            private SpriteRenderer AddSprite(string objectName, Sprite sprite, Color color, int order, Vector3 scale)
            {
                var child = new GameObject(objectName);
                child.transform.SetParent(transform, false);
                child.transform.localScale = scale;
                var renderer = child.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
                return renderer;
            }

            private TextMesh AddText(string objectName, Font font, int size, TextAnchor anchor, Vector3 position)
            {
                var child = new GameObject(objectName);
                child.transform.SetParent(transform, false); child.transform.localPosition = position;
                var mesh = child.AddComponent<TextMesh>();
                mesh.font = font; mesh.fontSize = size; mesh.characterSize = 0.06f; mesh.anchor = anchor;
                mesh.alignment = TextAlignment.Center; mesh.color = Color.black;
                if (font != null) mesh.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                mesh.GetComponent<MeshRenderer>().sortingOrder = 10;
                return mesh;
            }

            private static string BreakName(string name) => string.IsNullOrEmpty(name) ? "未命名" :
                name.Length <= 5 ? name : name.Substring(0, (name.Length + 1) / 2) + "\n" + name.Substring((name.Length + 1) / 2);
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

        private sealed class BoosterView : MonoBehaviour
        {
            private SpriteRenderer _body;
            private TextMesh _text;
            public string InstanceId { get; private set; }
            public BoosterView Initialize(string id, Sprite sprite, Font font)
            {
                InstanceId = id;
                _body = gameObject.AddComponent<SpriteRenderer>();
                _body.sprite = sprite; _body.color = Color.black; _body.sortingOrder = 20;
                transform.localScale = new Vector3(1.6f, 2.2f, 1f);
                var textObject = new GameObject("Text");
                textObject.transform.SetParent(transform, false);
                textObject.transform.localScale = new Vector3(0.625f, 0.455f, 1f);
                textObject.transform.localPosition = new Vector3(0, 0, -0.05f);
                _text = textObject.AddComponent<TextMesh>();
                _text.font = font; _text.fontSize = 34; _text.characterSize = 0.1f;
                _text.anchor = TextAnchor.MiddleCenter; _text.alignment = TextAlignment.Center; _text.color = Color.white;
                if (font != null) _text.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                _text.GetComponent<MeshRenderer>().sortingOrder = 25;
                gameObject.AddComponent<BoxCollider2D>().size = Vector2.one;
                return this;
            }
            public void Render(BoosterSnapshot data)
            {
                transform.position = new Vector3(data.X, data.Y, -0.2f);
                _text.text = data.NameZh + "\n剩余 " + data.Remaining;
            }
        }
    }
}
