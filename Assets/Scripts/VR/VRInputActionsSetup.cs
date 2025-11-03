using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// VR输入动作设置助手
/// 用于配置和验证VR输入动作引用
/// </summary>
public class VRInputActionsSetup : MonoBehaviour
{
    [Header("自动配置设置")]
    public bool autoConfigureOnStart = true;
    public bool generateInputActionsAsset = false;

    [Header("输入动作引用")]
    public InputActionReference leftGrabAction;
    public InputActionReference rightGrabAction;
    public InputActionReference leftTriggerAction;
    public InputActionReference rightTriggerAction;
    public InputActionReference aButtonAction;
    public InputActionReference bButtonAction;
    public InputActionReference xButtonAction;
    public InputActionReference yButtonAction;
    public InputActionReference menuButtonAction;
    public InputActionReference leftThumbstickAction;
    public InputActionReference rightThumbstickAction;

    void Start()
    {
        if (autoConfigureOnStart)
        {
            ConfigureInputActions();
        }
    }

    /// <summary>
    /// 配置输入动作
    /// </summary>
    public void ConfigureInputActions()
    {
        Debug.Log("[VRInputActionsSetup] 开始配置VR输入动作...");

        // 如果没有配置Input Action References，尝试自动查找
        if (leftGrabAction == null)
        {
            Debug.LogWarning("[VRInputActionsSetup] 请在Inspector中配置Input Action References");
            return;
        }

        // 验证所有动作引用
        ValidateActionReferences();

        Debug.Log("[VRInputActionsSetup] VR输入动作配置完成");
    }

    /// <summary>
    /// 验证动作引用
    /// </summary>
    private void ValidateActionReferences()
    {
        var actions = new[]
        {
            ("LeftGrab", leftGrabAction),
            ("RightGrab", rightGrabAction),
            ("LeftTrigger", leftTriggerAction),
            ("RightTrigger", rightTriggerAction),
            ("AButton", aButtonAction),
            ("BButton", bButtonAction),
            ("XButton", xButtonAction),
            ("YButton", yButtonAction),
            ("MenuButton", menuButtonAction),
            ("LeftThumbstick", leftThumbstickAction),
            ("RightThumbstick", rightThumbstickAction)
        };

        foreach (var (name, action) in actions)
        {
            if (action != null && action.action != null)
            {
                Debug.Log($"[VRInputActionsSetup] {name}: {action.action.name} ✓");
            }
            else
            {
                Debug.LogWarning($"[VRInputActionsSetup] {name}: 未配置 ✗");
            }
        }
    }

    /// <summary>
    /// 应用配置到统一VR输入管理器
    /// </summary>
    public void ApplyToUnifiedVRInputManager()
    {
        if (UnifiedVRInputManager.Instance != null)
        {
            var manager = UnifiedVRInputManager.Instance;

            // 应用动作引用
            manager.leftGrabAction = leftGrabAction;
            manager.rightGrabAction = rightGrabAction;
            manager.leftTriggerAction = leftTriggerAction;
            manager.rightTriggerAction = rightTriggerAction;
            manager.aButtonAction = aButtonAction;
            manager.bButtonAction = bButtonAction;
            manager.xButtonAction = xButtonAction;
            manager.yButtonAction = yButtonAction;
            manager.menuButtonAction = menuButtonAction;
            manager.leftThumbstickAction = leftThumbstickAction;
            manager.rightThumbstickAction = rightThumbstickAction;

            Debug.Log("[VRInputActionsSetup] 已应用配置到统一VR输入管理器");
        }
        else
        {
            Debug.LogError("[VRInputActionsSetup] 统一VR输入管理器未找到！");
        }
    }

    /// <summary>
    /// 获取动作引用数组
    /// </summary>
    public InputActionReference[] GetAllActionReferences()
    {
        return new InputActionReference[]
        {
            leftGrabAction,
            rightGrabAction,
            leftTriggerAction,
            rightTriggerAction,
            aButtonAction,
            bButtonAction,
            xButtonAction,
            yButtonAction,
            menuButtonAction,
            leftThumbstickAction,
            rightThumbstickAction
        };
    }

    void OnValidate()
    {
        // 在Inspector中验证配置
        #if UNITY_EDITOR
        if (Application.isPlaying) return;

        // 检查是否所有必要的动作都已配置
        var requiredActions = new[]
        {
            ("LeftGrab", leftGrabAction),
            ("RightGrab", rightGrabAction),
            ("RightTrigger", rightTriggerAction),
            ("AButton", aButtonAction),
            ("BButton", bButtonAction),
            ("XButton", xButtonAction),
            ("YButton", yButtonAction),
            ("MenuButton", menuButtonAction),
            ("LeftThumbstick", leftThumbstickAction)
        };

        bool allConfigured = true;
        foreach (var (name, action) in requiredActions)
        {
            if (action == null)
            {
                allConfigured = false;
                break;
            }
        }

        if (!allConfigured)
        {
            Debug.LogWarning($"[VRInputActionsSetup] 请配置所有必要的VR输入动作引用");
        }
        #endif
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(VRInputActionsSetup))]
public class VRInputActionsSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("快速配置工具", EditorStyles.boldLabel);

        VRInputActionsSetup setup = (VRInputActionsSetup)target;

        if (GUILayout.Button("配置输入动作"))
        {
            setup.ConfigureInputActions();
        }

        if (GUILayout.Button("应用到统一VR输入管理器"))
        {
            setup.ApplyToUnifiedVRInputManager();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("配置说明", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("1. 创建Input Actions资产文件");
        EditorGUILayout.LabelField("2. 配置所有VR控制器输入");
        EditorGUILayout.LabelField("3. 将Input Action References拖拽到对应字段");
        EditorGUILayout.LabelField("4. 点击'应用到统一VR输入管理器'");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("必需的动作:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• LeftGrab/RightGrab - 左右手抓取");
        EditorGUILayout.LabelField("• RightTrigger - 右手扳机(拍照)");
        EditorGUILayout.LabelField("• A/B/X/Y - 按钮组合");
        EditorGUILayout.LabelField("• MenuButton - 菜单键");
        EditorGUILayout.LabelField("• LeftThumbstick - 左摇杆移动");
    }
}
#endif