using UnityEngine;
using UnityEngine.UIElements;

public class BetDialogController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;
    
    [Header("Settings")]
    [SerializeField] private bool startVisible = true;
    [SerializeField] private string dialogTitle = "Let's make a bet!";
    [SerializeField] private string description = "Pick a vibe, place a bet. If the next avatar matches, you win!";
    [SerializeField] private string okButtonText = "ok";
    
    // UI元素引用
    private VisualElement root;
    private Button closeButton;
    private Button okButton;
    private VisualElement checkbox;
    private Label checkmark;
    private Label titleLabel;
    private Label descriptionLine1;
    private Label descriptionLine2;
    
    // 状态
    private bool isDontShowAgain = false;
    private bool isVisible = false;
    
    // 事件
    public System.Action OnOkClicked;
    public System.Action OnCloseClicked;
    public System.Action<bool> OnDontShowAgainChanged;
    
    private void OnEnable()
    {
        InitializeUI();
    }
    
    private void Start()
    {
        if (startVisible)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }
    
    private void InitializeUI()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }
        
        if (uiDocument == null)
        {
            Debug.LogError("[BetDialog] UIDocument not found!");
            return;
        }
        
        root = uiDocument.rootVisualElement;
        
        // 获取UI元素
        closeButton = root.Q<Button>("btn-close");
        okButton = root.Q<Button>("btn-ok");
        checkbox = root.Q<VisualElement>("checkbox-dont-show");
        checkmark = root.Q<Label>("checkmark");
        
        // 更新文本
        var titleArea = root.Q<VisualElement>("title-area");
        if (titleArea != null)
        {
            // 可以在这里动态更新标题
        }
        
        var descArea = root.Q<VisualElement>("description-area");
        if (descArea != null)
        {
            descriptionLine1 = descArea.Q<Label>(className: "description-line1");
            descriptionLine2 = descArea.Q<Label>(className: "description-line2");
            
            if (descriptionLine1 != null) descriptionLine1.text = "Pick a vibe, place a bet.";
            if (descriptionLine2 != null) descriptionLine2.text = "If the next avatar matches, you win!";
        }
        
        if (okButton != null)
        {
            okButton.text = okButtonText;
        }
        
        // 注册事件
        if (closeButton != null)
        {
            closeButton.clicked += OnCloseButtonClicked;
        }
        
        if (okButton != null)
        {
            okButton.clicked += OnOkButtonClicked;
        }
        
        if (checkbox != null)
        {
            checkbox.RegisterCallback<ClickEvent>(OnCheckboxClicked);
        }
    }
    
    private void OnDisable()
    {
        // 注销事件
        if (closeButton != null)
        {
            closeButton.clicked -= OnCloseButtonClicked;
        }
        
        if (okButton != null)
        {
            okButton.clicked -= OnOkButtonClicked;
        }
        
        if (checkbox != null)
        {
            checkbox.UnregisterCallback<ClickEvent>(OnCheckboxClicked);
        }
    }
    
    private void OnCloseButtonClicked()
    {
        Hide();
        OnCloseClicked?.Invoke();
    }
    
    private void OnOkButtonClicked()
    {
        Hide();
        OnOkClicked?.Invoke();
    }
    
    private void OnCheckboxClicked(ClickEvent evt)
    {
        isDontShowAgain = !isDontShowAgain;
        
        if (isDontShowAgain)
        {
            checkbox.AddToClassList("checked");
        }
        else
        {
            checkbox.RemoveFromClassList("checked");
        }
        
        OnDontShowAgainChanged?.Invoke(isDontShowAgain);
        
        Debug.Log($"[BetDialog] Don't show again: {isDontShowAgain}");
    }
    
    public void Show()
    {
        if (root != null)
        {
            root.style.display = DisplayStyle.Flex;
            isVisible = true;
        }
    }
    
    public void Hide()
    {
        if (root != null)
        {
            root.style.display = DisplayStyle.None;
            isVisible = false;
        }
    }
    
    public void Toggle()
    {
        if (isVisible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }
    
    public bool IsVisible => isVisible;
    public bool IsDontShowAgain => isDontShowAgain;
    
    // 测试方法
    [ContextMenu("Test Show")]
    private void TestShow()
    {
        Show();
    }
    
    [ContextMenu("Test Hide")]
    private void TestHide()
    {
        Hide();
    }
}
