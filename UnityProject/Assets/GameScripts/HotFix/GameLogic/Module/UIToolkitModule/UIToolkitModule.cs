using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace GameLogic
{
    /// <summary>
    /// UI Toolkit 管理模块，负责 UI 实例缓存和基于栈的导航。
    /// </summary>
    public sealed class UIToolkitModule : Singleton<UIToolkitModule>
    {
        private sealed class UIEntry
        {
            public GameObject GameObject;
            public MonoBehaviour Controller;
            public bool IsVisible;
        }

        private readonly Dictionary<UITypes, UIEntry> _uiCache = new Dictionary<UITypes, UIEntry>();
        private readonly List<UITypes> _uiStack = new List<UITypes>();
        private Transform _instanceRoot;
        private PanelSettings _defaultPanelSettings;
        private CancellationTokenSource _lifetimeCancellation;

        /// <summary>
        /// UI Toolkit 运行时实例的根节点。
        /// </summary>
        public Transform UIRoot => _instanceRoot;

        /// <summary>
        /// 所有新建 UIDocument 使用的默认 PanelSettings。
        /// </summary>
        public PanelSettings DefaultPanelSettings
        {
            get => _defaultPanelSettings;
            set => _defaultPanelSettings = value;
        }

        /// <summary>
        /// 当前导航栈深度。
        /// </summary>
        public int StackCount => _uiStack.Count;

        protected override void OnInit()
        {
            var root = new GameObject(nameof(UIToolkitModule));
            Object.DontDestroyOnLoad(root);
            _instanceRoot = root.transform;
            _defaultPanelSettings = Resources.Load<PanelSettings>("PanelSettings");
            _lifetimeCancellation = new CancellationTokenSource();
        }

        protected override void OnRelease()
        {
            _lifetimeCancellation?.Cancel();
            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = null;

            ClearAll();

            if (_instanceRoot != null)
                Object.Destroy(_instanceRoot.gameObject);

            _instanceRoot = null;
            _defaultPanelSettings = null;
        }

        /// <summary>
        /// 获取或同步创建 UI。仅适用于资源已经在本地可用的场景。
        /// </summary>
        public T GetUI<T>(UITypes type) where T : MonoBehaviour
        {
            if (_uiCache.TryGetValue(type, out var entry))
                return GetCachedController<T>(type, entry);

            string location = type.GetAssetLocation();
            if (string.IsNullOrEmpty(location))
                return null;

            T controller = UIFactory.CreateUIPrefab<T>(
                location,
                _defaultPanelSettings,
                _instanceRoot);
            if (controller == null)
            {
                Log.Error($"[{nameof(UIToolkitModule)}] 创建 UI 失败: {type}");
                return null;
            }

            Cache(type, controller);
            return controller;
        }

        /// <summary>
        /// 获取或异步创建 UI，支持 YooAsset 下载并加载远程资源。
        /// </summary>
        public async UniTask<T> GetUIAsync<T>(
            UITypes type,
            CancellationToken cancellationToken = default) where T : MonoBehaviour
        {
            if (_uiCache.TryGetValue(type, out var entry))
                return GetCachedController<T>(type, entry);

            string location = type.GetAssetLocation();
            if (string.IsNullOrEmpty(location))
                return null;

            CancellationToken lifetimeToken = _lifetimeCancellation?.Token ?? default;
            using (var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                       cancellationToken,
                       lifetimeToken))
            {
                T controller = await UIFactory.CreateUIPrefabAsync<T>(
                    location,
                    _defaultPanelSettings,
                    _instanceRoot,
                    cancellationToken: linkedCancellation.Token);

                if (controller == null)
                {
                    if (!linkedCancellation.IsCancellationRequested)
                        Log.Error($"[{nameof(UIToolkitModule)}] 异步创建 UI 失败: {type}");
                    return null;
                }

                Cache(type, controller);
                return controller;
            }
        }

        /// <summary>
        /// 显示 UI 并将其移动到导航栈顶。
        /// </summary>
        public T ShowUI<T>(UITypes type, bool hideBelow = true) where T : MonoBehaviour
        {
            T controller = GetUI<T>(type);
            if (controller == null)
                return null;

            ShowEntry(type, hideBelow);
            return controller;
        }

        /// <summary>
        /// 异步显示 UI。远程热更资源应使用此接口。
        /// </summary>
        public async UniTask<T> ShowUIAsync<T>(
            UITypes type,
            bool hideBelow = true,
            CancellationToken cancellationToken = default) where T : MonoBehaviour
        {
            T controller = await GetUIAsync<T>(type, cancellationToken);
            if (controller == null)
                return null;

            ShowEntry(type, hideBelow);
            return controller;
        }

        /// <summary>
        /// 关闭栈顶 UI。
        /// </summary>
        public void CloseTopUI(bool destroy = false)
        {
            if (_uiStack.Count == 0)
            {
                Log.Warning($"[{nameof(UIToolkitModule)}] 栈为空，无法关闭");
                return;
            }

            CloseUI(_uiStack[_uiStack.Count - 1], destroy);
        }

        /// <summary>
        /// 关闭指定 UI；可选择销毁资源实例或仅隐藏缓存。
        /// </summary>
        public void CloseUI(UITypes type, bool destroy = false)
        {
            if (!_uiCache.TryGetValue(type, out var entry))
            {
                Log.Warning($"[{nameof(UIToolkitModule)}] UI 未打开: {type}");
                return;
            }

            bool wasTop = _uiStack.Count > 0 && _uiStack[_uiStack.Count - 1] == type;
            _uiStack.Remove(type);

            if (destroy)
            {
                if (entry.GameObject != null)
                    Object.Destroy(entry.GameObject);
                _uiCache.Remove(type);
            }
            else if (entry.GameObject != null)
            {
                entry.GameObject.SetActive(false);
                entry.IsVisible = false;
            }

            if (wasTop)
                ShowStackTop();
        }

        /// <summary>
        /// 获取当前栈顶 UI 控制器。
        /// </summary>
        public T GetCurrentUI<T>() where T : MonoBehaviour
        {
            if (_uiStack.Count == 0)
                return null;

            UITypes type = _uiStack[_uiStack.Count - 1];
            return _uiCache.TryGetValue(type, out var entry)
                ? entry.Controller as T
                : null;
        }

        /// <summary>
        /// UI 是否已创建并缓存。
        /// </summary>
        public bool IsOpen(UITypes type)
        {
            return _uiCache.ContainsKey(type);
        }

        public void Back(bool destroy = false)
        {
            CloseTopUI(destroy);
        }

        /// <summary>
        /// 销毁全部 UI，并释放对应的 YooAsset 资源引用。
        /// </summary>
        public void ClearAll()
        {
            foreach (UIEntry entry in _uiCache.Values)
            {
                if (entry.GameObject != null)
                    Object.Destroy(entry.GameObject);
            }

            _uiCache.Clear();
            _uiStack.Clear();
        }

        private T GetCachedController<T>(UITypes type, UIEntry entry) where T : MonoBehaviour
        {
            if (entry.Controller is T controller)
                return controller;

            Log.Error(
                $"[{nameof(UIToolkitModule)}] 缓存的 UI 类型不匹配: {type} " +
                $"(期望 {typeof(T).Name}, 实际 {entry.Controller?.GetType().Name})");
            return null;
        }

        private void Cache<T>(UITypes type, T controller) where T : MonoBehaviour
        {
            controller.gameObject.SetActive(false);
            _uiCache[type] = new UIEntry
            {
                GameObject = controller.gameObject,
                Controller = controller,
                IsVisible = false
            };
        }

        private void ShowEntry(UITypes type, bool hideBelow)
        {
            _uiStack.Remove(type);
            _uiStack.Add(type);

            if (hideBelow)
            {
                for (int i = 0; i < _uiStack.Count - 1; i++)
                {
                    if (_uiCache.TryGetValue(_uiStack[i], out var belowEntry) &&
                        belowEntry.GameObject != null)
                    {
                        belowEntry.GameObject.SetActive(false);
                        belowEntry.IsVisible = false;
                    }
                }
            }

            UIEntry entry = _uiCache[type];
            entry.GameObject.SetActive(true);
            entry.IsVisible = true;
        }

        private void ShowStackTop()
        {
            if (_uiStack.Count == 0)
                return;

            UITypes topType = _uiStack[_uiStack.Count - 1];
            if (_uiCache.TryGetValue(topType, out var topEntry) &&
                topEntry.GameObject != null)
            {
                topEntry.GameObject.SetActive(true);
                topEntry.IsVisible = true;
            }
        }
    }
}
