using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

/// <summary>
/// VR可抓取物体 - 用于MainMenu场景的场景选择物体
/// 当抓取时显示预览信息，按住A键+抓取可进入对应场景
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class VRGrabbableObject : MonoBehaviour
{
    [Header("场景信息")]
    public SceneType targetScene; // 目标场景类型
    public string sceneDescription; // 场景描述
    public Texture2D previewImage; // 预览图片

    [Header("UI显示")]
    public Canvas infoCanvas; // 信息显示Canvas
    public TextMeshProUGUI descriptionText; // 描述文本
    public UnityEngine.UI.RawImage imageDisplay; // 图片显示
    public float showDistance = 0.5f; // UI显示距离

    [Header("视觉效果")]
    public Material highlightMaterial; // 高亮材质
    public float highlightIntensity = 2f; // 高亮强度
    public Vector3 hoverScale = Vector3.one * 1.1f; // 悬停缩放

    [Header("音频反馈")]
    public AudioClip grabSound; // 抓取音效
    public AudioClip hoverSound; // 悬停音效
    public AudioClip confirmSound; // 确认音效

    // 私有变量
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private Material originalMaterial;
    private Vector3 originalScale;
    private AudioSource audioSource;
    private bool isGrabbed = false;
    // private bool isHovered = false; // 暂时注释掉避免警告
    private float grabStartTime = 0f;
    private Coroutine showInfoCoroutine;

    // 场景类型枚举
    public enum SceneType
    {
        PhotoScene,
        Museum
    }

    void Awake()
    {
        // 获取组件引用
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 保存原始状态
        originalScale = transform.localScale;
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            originalMaterial = renderer.material;
        }
    }

    void Start()
    {
        // 设置XR交互事件
        SetupInteractionEvents();

        // 初始隐藏信息Canvas
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(false);
        }

        // 设置描述文本
        if (descriptionText != null)
        {
            descriptionText.text = sceneDescription;
        }

        // 设置预览图片
        if (imageDisplay != null && previewImage != null)
        {
            imageDisplay.texture = previewImage;
        }
    }

    /// <summary>
    /// 设置交互事件监听
    /// </summary>
    private void SetupInteractionEvents()
    {
        // 悬停进入事件
        grabInteractable.hoverEntered.AddListener(args =>
        {
            OnHoverEntered(args);
        });

        // 悬停退出事件
        grabInteractable.hoverExited.AddListener(args =>
        {
            OnHoverExited(args);
        });

        // 抓取事件
        grabInteractable.activated.AddListener(args =>
        {
            OnGrabbed(args);
        });

        // 释放事件
        grabInteractable.deactivated.AddListener(args =>
        {
            OnReleased(args);
        });
    }

    /// <summary>
    /// 悬停进入处理
    /// </summary>
    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (isGrabbed) return;

        // isHovered = true; // 暂时注释避免警告

        // 视觉效果
        transform.localScale = hoverScale;
        ApplyHighlight();

        // 音效
        if (hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }

        Debug.Log("悬停在物体上: " + gameObject.name);
    }

    /// <summary>
    /// 悬停退出处理
    /// </summary>
    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (isGrabbed) return;

        // isHovered = false; // 暂时注释避免警告

        // 恢复视觉效果
        transform.localScale = originalScale;
        RemoveHighlight();

        Debug.Log("离开物体悬停: " + gameObject.name);
    }

    /// <summary>
    /// 抓取处理
    /// </summary>
    private void OnGrabbed(ActivateEventArgs args)
    {
        isGrabbed = true;
        grabStartTime = Time.time;

        // 音效
        if (grabSound != null)
        {
            audioSource.PlayOneShot(grabSound);
        }

        // 显示信息Canvas
        ShowInfoCanvas();

        // 开始检测A键按住
        StartCoroutine(CheckAKeyHold());

        Debug.Log("抓取物体: " + gameObject.name + "，目标场景: " + targetScene);
    }

    /// <summary>
    /// 释放处理
    /// </summary>
    private void OnReleased(DeactivateEventArgs args)
    {
        isGrabbed = false;

        // 隐藏信息Canvas
        HideInfoCanvas();

        // 停止所有协程
        StopAllCoroutines();

        // 恢复原始位置（可选）
        ReturnToOriginalPosition();

        Debug.Log("释放物体: " + gameObject.name);
    }

    /// <summary>
    /// 显示信息Canvas
    /// </summary>
    private void ShowInfoCanvas()
    {
        if (infoCanvas == null) return;

        infoCanvas.gameObject.SetActive(true);

        // 设置Canvas位置和朝向
        StartCoroutine(UpdateCanvasPosition());
    }

    /// <summary>
    /// 隐藏信息Canvas
    /// </summary>
    private void HideInfoCanvas()
    {
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 更新Canvas位置和朝向
    /// </summary>
    private IEnumerator UpdateCanvasPosition()
    {
        while (isGrabbed && infoCanvas != null)
        {
            // 将Canvas放置在物体前方
            Vector3 targetPosition = transform.position + transform.forward * showDistance;
            infoCanvas.transform.position = targetPosition;

            // 让Canvas面向玩家（假设主相机是玩家视角）
            if (Camera.main != null)
            {
                infoCanvas.transform.LookAt(Camera.main.transform);
                infoCanvas.transform.Rotate(0, 180, 0); // 翻转使其正确显示
            }

            yield return null;
        }
    }

    /// <summary>
    /// 检测A键按住
    /// </summary>
    private IEnumerator CheckAKeyHold()
    {
        float holdTime = 0f;
        const float requiredHoldTime = 0.5f; // 需要按住0.5秒

        while (isGrabbed)
        {
            // 检测A键状态（右手控制器A键）
            bool aButtonPressed = Input.GetKey(KeyCode.JoystickButton0) || // 通用A键
                                  OVRInput.Get(OVRInput.Button.One); // Meta Quest右手A键

            if (aButtonPressed)
            {
                holdTime += Time.deltaTime;

                // 达到按住时间，进入场景
                if (holdTime >= requiredHoldTime)
                {
                    EnterTargetScene();
                    break;
                }

                // 显示进度反馈（可选）
                float progress = holdTime / requiredHoldTime;
                UpdateProgressFeedback(progress);
            }
            else
            {
                // A键被释放，重置计时
                holdTime = 0f;
                ResetProgressFeedback();
            }

            yield return null;
        }
    }

    /// <summary>
    /// 进入目标场景
    /// </summary>
    private void EnterTargetScene()
    {
        // 播放确认音效
        if (confirmSound != null)
        {
            audioSource.PlayOneShot(confirmSound);
        }

        Debug.Log("进入场景: " + targetScene);

        // 通过VRSceneManager加载场景
        if (VRSceneManager.Instance != null)
        {
            switch (targetScene)
            {
                case SceneType.PhotoScene:
                    VRSceneManager.Instance.LoadPhotoScene();
                    break;
                case SceneType.Museum:
                    VRSceneManager.Instance.LoadMuseumScene();
                    break;
            }
        }
        else
        {
            Debug.LogError("VRSceneManager实例未找到！");
        }
    }

    /// <summary>
    /// 更新进度反馈
    /// </summary>
    private void UpdateProgressFeedback(float progress)
    {
        // 可以在这里添加视觉效果，如改变材质颜色、发光等
        if (originalMaterial != null)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                // 根据进度调整材质的发光程度
                Color color = originalMaterial.color;
                color.g = Mathf.Lerp(1f, 0f, progress); // 从绿色渐变到红色
                renderer.material.color = color;
            }
        }
    }

    /// <summary>
    /// 重置进度反馈
    /// </summary>
    private void ResetProgressFeedback()
    {
        // 恢复原始材质颜色
        if (originalMaterial != null)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = originalMaterial.color;
            }
        }
    }

    /// <summary>
    /// 应用高亮效果
    /// </summary>
    private void ApplyHighlight()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null && highlightMaterial != null)
        {
            renderer.material = highlightMaterial;

            // 调整高亮强度
            if (highlightMaterial.HasProperty("_EmissionColor"))
            {
                Color emissionColor = highlightMaterial.GetColor("_EmissionColor");
                emissionColor *= highlightIntensity;
                highlightMaterial.SetColor("_EmissionColor", emissionColor);
            }
        }
    }

    /// <summary>
    /// 移除高亮效果
    /// </summary>
    private void RemoveHighlight()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null && originalMaterial != null)
        {
            renderer.material = originalMaterial;
        }
    }

    /// <summary>
    /// 返回原始位置
    /// </summary>
    private void ReturnToOriginalPosition()
    {
        // 这里可以实现物体返回原始位置的逻辑
        // 比如使用Lerp平滑移动回到初始位置
    }

    void OnDestroy()
    {
        // 清理事件监听
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.RemoveListener(args => OnHoverEntered(args));
            grabInteractable.hoverExited.RemoveListener(args => OnHoverExited(args));
            grabInteractable.activated.RemoveListener(args => OnGrabbed(args));
            grabInteractable.deactivated.RemoveListener(args => OnReleased(args));
        }
    }
}