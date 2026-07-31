using UnityEngine;
using UnityEngine.UIElements;

namespace GameLogic
{
    /// <summary>
    /// Hello World 测试界面控制器。
    /// 经 UIToolkitModule 以 UITypes.HelloWorldScreen 打开，
    /// UXML 资源地址为 "HelloWorldScreen"。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HelloWorldScreenController : MonoBehaviour
    {
        private UIDocument _doc;
        private Button _closeButton;

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = _doc.rootVisualElement;
            _closeButton = root.Q<Button>("hello-close-btn");
            if (_closeButton != null)
                _closeButton.clicked += OnCloseClicked;
        }

        private void OnDisable()
        {
            if (_closeButton != null)
                _closeButton.clicked -= OnCloseClicked;
        }

        private void OnCloseClicked()
        {
            UIToolkitModule.Instance.CloseUI(UITypes.HelloWorldScreen, destroy: true);
        }
    }
}
