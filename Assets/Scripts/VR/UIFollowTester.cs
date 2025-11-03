using UnityEngine;

/// <summary>
/// UI跟随测试器 - 方便在运行时测试UI跟随功能
/// </summary>
public class UIFollowTester : MonoBehaviour
{
    [Header("引用")]
    public VRCameraRig vrCameraRig;
    public UnityEngine.UI.Button toggleButton;
    public UnityEngine.UI.Text statusText;

    [Header("快捷键")]
    public KeyCode toggleKey = KeyCode.U; // 切换跟随模式
    public KeyCode toggleKey2 = KeyCode.I; // 切换控制模式

    void Start()
    {
        // 如果没有手动指定，尝试查找VRCameraRig
        if (vrCameraRig == null)
        {
            vrCameraRig = FindObjectOfType<VRCameraRig>();
        }

        // 设置按钮事件
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(OnToggleClicked);
        }

        // 更新状态显示
        UpdateStatusDisplay();
    }

    void Update()
    {
        // 按快捷键切换
        if (Input.GetKeyDown(toggleKey))
        {
            OnToggleClicked();
        }

        // 按第二个快捷键切换控制模式
        if (Input.GetKeyDown(toggleKey2))
        {
            OnToggleControlClicked();
        }
    }

    /// <summary>
    /// 切换UI跟随模式
    /// </summary>
    public void OnToggleClicked()
    {
        if (vrCameraRig != null)
        {
            vrCameraRig.ToggleUIFollowing();
            UpdateStatusDisplay();
        }
        else
        {
            Debug.LogError("[UIFollowTester] VRCameraRig组件未找到！");
        }
    }

    /// <summary>
    /// 更新状态显示
    /// </summary>
    private void UpdateStatusDisplay()
    {
        if (statusText != null && vrCameraRig != null)
        {
            bool isFollowing = vrCameraRig.GetUIFollowingMode();
            bool allowControl = vrCameraRig.GetUIControlMode();
            string controlStatus = allowControl ? "脚本控制" : "Editor控制";
            string followStatus = isFollowing ? "跟随相机" : "固定位置";

            statusText.text = $"UI控制: {controlStatus}\\n跟随模式: {followStatus}\\n\\n按 {toggleKey} 切换跟随\\n按 {toggleKey2} 切换控制";
        }
    }

    /// <summary>
    /// 设置跟随模式
    /// </summary>
    public void SetFollowing(bool follow)
    {
        if (vrCameraRig != null)
        {
            vrCameraRig.SetUIFollowing(follow);
            UpdateStatusDisplay();
        }
    }

    /// <summary>
    /// 切换UI控制模式
    /// </summary>
    public void OnToggleControlClicked()
    {
        if (vrCameraRig != null)
        {
            vrCameraRig.ToggleUIControl();
            UpdateStatusDisplay();
        }
        else
        {
            Debug.LogError("[UIFollowTester] VRCameraRig组件未找到！");
        }
    }

    /// <summary>
    /// 设置UI控制模式
    /// </summary>
    public void SetUIControl(bool allowControl)
    {
        if (vrCameraRig != null)
        {
            vrCameraRig.SetUIControl(allowControl);
            UpdateStatusDisplay();
        }
    }

    void OnValidate()
    {
        // 在Inspector中显示帮助信息
        #if UNITY_EDITOR
        if (vrCameraRig == null)
        {
            Debug.LogWarning("[UIFollowTester] 建议分配VRCameraRig组件引用");
        }
        #endif
    }

    void OnGUI()
    {
        // 显示调试信息（仅在编辑器中）
        #if UNITY_EDITOR
        GUILayout.BeginArea(new Rect(10, 10, 200, 150));
        GUILayout.Label("UI跟随测试器");

        if (vrCameraRig != null)
        {
            bool isFollowing = vrCameraRig.GetUIFollowingMode();
            GUILayout.Label($"当前状态: {(isFollowing ? "跟随相机" : "保持原始位置")}");

            if (GUILayout.Button("切换模式"))
            {
                OnToggleClicked();
            }
        }
        else
        {
            GUILayout.Label("VRCameraRig未找到");
        }

        GUILayout.Space(10);
        GUILayout.Label($"快捷键: {toggleKey}");

        GUILayout.EndArea();
        #endif
    }
}