using System.Threading;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLogic
{
    /// <summary>
    /// UI 工厂类，用于运行时通过代码创建带 UIDocument 的 GameObject（UI Prefab）。
    /// UXML 由 TEngine 资源模块通过 YooAsset 加载。
    /// </summary>
    public static class UIFactory
    {
        /// <summary>
        /// 同步加载 UXML 并创建 UI。仅适用于资源已经在本地可用的场景。
        /// </summary>
        /// <param name="location">YooAsset 资源地址。</param>
        /// <param name="panelSettings">PanelSettings，为空则使用系统默认</param>
        /// <param name="parent">父 Transform</param>
        /// <param name="name">GameObject 名称，为空则自动生成</param>
        public static T CreateUIPrefab<T>(
            string location,
            PanelSettings panelSettings = null,
            Transform parent = null,
            string name = null) where T : MonoBehaviour
        {
            var resourceModule = GameModule.Resource;
            var uxmlAsset = resourceModule.LoadAsset<VisualTreeAsset>(location);
            if (uxmlAsset == null)
            {
                Log.Error($"[UIFactory] YooAsset 无法加载 UXML: {location}");
                return null;
            }

            return CreateFromAsset<T>(uxmlAsset, resourceModule, panelSettings, parent, name);
        }

        /// <summary>
        /// 异步加载 UXML 并创建 UI，支持 YooAsset 远程资源。
        /// </summary>
        public static async UniTask<T> CreateUIPrefabAsync<T>(
            string location,
            PanelSettings panelSettings = null,
            Transform parent = null,
            string name = null,
            CancellationToken cancellationToken = default) where T : MonoBehaviour
        {
            var resourceModule = GameModule.Resource;
            var uxmlAsset = await resourceModule.LoadAssetAsync<VisualTreeAsset>(
                location,
                cancellationToken);

            if (uxmlAsset == null)
            {
                if (!cancellationToken.IsCancellationRequested)
                    Log.Error($"[UIFactory] YooAsset 异步加载 UXML 失败: {location}");
                return null;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                resourceModule.UnloadAsset(uxmlAsset);
                return null;
            }

            return CreateFromAsset<T>(uxmlAsset, resourceModule, panelSettings, parent, name);
        }

        private static T CreateFromAsset<T>(
            VisualTreeAsset uxmlAsset,
            IResourceModule resourceModule,
            PanelSettings panelSettings,
            Transform parent,
            string name) where T : MonoBehaviour
        {
            string goName = string.IsNullOrEmpty(name) ? $"UI_{uxmlAsset.name}" : name;
            var go = new GameObject(goName);
            if (parent != null)
                go.transform.SetParent(parent, false);

            var resourceReference = go.AddComponent<YooAssetUIResourceReference>();
            resourceReference.Initialize(resourceModule, uxmlAsset);

            var uiDocument = go.AddComponent<UIDocument>();
            uiDocument.panelSettings = panelSettings;
            uiDocument.visualTreeAsset = uxmlAsset;

            return go.AddComponent<T>();
        }

        /// <summary>
        /// 销毁 UI Prefab（通过控制器组件）。
        /// </summary>
        public static void DestroyUIPrefab<T>(T controller) where T : MonoBehaviour
        {
            if (controller != null)
                Object.Destroy(controller.gameObject);
        }
    }

    /// <summary>
    /// 保持 YooAsset UXML 及其 USS/字体/图片依赖存活，UI 销毁时归还资源引用。
    /// </summary>
    internal sealed class YooAssetUIResourceReference : MonoBehaviour
    {
        private IResourceModule _resourceModule;
        private VisualTreeAsset _uxmlAsset;

        public void Initialize(IResourceModule resourceModule, VisualTreeAsset uxmlAsset)
        {
            _resourceModule = resourceModule;
            _uxmlAsset = uxmlAsset;
        }

        private void OnDestroy()
        {
            if (_resourceModule != null && _uxmlAsset != null)
                _resourceModule.UnloadAsset(_uxmlAsset);

            _resourceModule = null;
            _uxmlAsset = null;
        }
    }
}
