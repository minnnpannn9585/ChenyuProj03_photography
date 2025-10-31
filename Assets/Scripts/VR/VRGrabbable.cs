using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using DG.Tweening;

/// <summary>
/// VR可抓取物品组件
/// 配合VRMenuController使用
/// </summary>
public class VRGrabbable : MonoBehaviour
{
    [Header("抓取设置")]
    public float grabRange = 0.2f;
    public LayerMask grabLayer = -1;
    public bool enableHover = true;
    public bool enableHighlight = true;

    [Header("视觉效果")]
    public Color hoverColor = Color.yellow;
    public Color grabColor = Color.green;
    public float highlightIntensity = 2f;

    [Header("事件")]
    public UnityEvent OnGrabbed = new UnityEvent();
    public UnityEvent OnReleased = new UnityEvent();
    public UnityEvent OnHovered = new UnityEvent();
    public UnityEvent OnUnhovered = new UnityEvent();

    private VRMenuController menuController;
    private VRMenuController.SceneItem sceneItem;
    private Rigidbody rb;
    private Collider[] colliders;
    private Renderer[] renderers;
    private Color[][] originalColors;
    private bool isGrabbed = false;
    private bool isHovered = false;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    private Vector3 originalScale;
    private bool isReturningToOrigin = false;

    void Start()
    {
        InitializeComponents();
    }

    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();

        // 保存原始颜色
        if (renderers != null && renderers.Length > 0)
        {
            originalColors = new Color[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    Material mat = renderers[i].material;
                    originalColors[i] = new Color[1];

                    if (mat.HasColor("_BaseColor"))
                    {
                        originalColors[i][0] = mat.GetColor("_BaseColor");
                    }
                    else if (mat.HasProperty("_Color"))
                    {
                        originalColors[i][0] = mat.color;
                    }
                }
            }
        }

        // 保存原始位置、旋转和缩放
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;
        originalParent = transform.parent;
    }

    /// <summary>
    /// 初始化抓取物品
    /// </summary>
    public void Initialize(VRMenuController controller, VRMenuController.SceneItem item)
    {
        menuController = controller;
        sceneItem = item;

        // 确保有必要的物理组件
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true; // 初始设置为运动学模式
        }

        // 确保有碰撞体
        if (colliders == null || colliders.Length == 0)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = 0.1f;
            collider.isTrigger = true;
            colliders = new Collider[] { collider };
        }

        // 设置抓取层级
        gameObject.layer = LayerMask.NameToLayer("Grabbable");
    }

    void Update()
    {
        if (!isGrabbed && enableHover)
        {
            CheckHover();
        }
    }

    /// <summary>
    /// 检查悬浮状态
    /// </summary>
    private void CheckHover()
    {
        // 检测手柄 proximity
        bool wasHovered = isHovered;
        isHovered = false;

        // 这里可以添加手柄检测逻辑
        // 由于使用Meta Building Blocks，这个检测可能会被覆盖

        if (isHovered != wasHovered)
        {
            if (isHovered)
            {
                OnHover();
            }
            else
            {
                OnUnhover();
            }
        }
    }

    /// <summary>
    /// 物品被抓住
    /// </summary>
    public void Grab(Transform grabber)
    {
        if (isGrabbed) return;

        isGrabbed = true;

        // 停止物理模拟
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }

        // 设置父级
        transform.SetParent(grabber);

        // 通知控制器
        if (menuController != null)
        {
            menuController.OnItemGrabbed(gameObject, sceneItem);
        }

        // 视觉效果
        if (enableHighlight)
        {
            SetHighlightColor(grabColor);
        }

        // 触发事件
        OnGrabbed.Invoke();

        Debug.Log($"Item {gameObject.name} grabbed");
    }

    /// <summary>
    /// 物品被松开
    /// </summary>
    public void Release()
    {
        if (!isGrabbed) return;

        isGrabbed = false;
        isReturningToOrigin = true;

        // 停止所有正在进行的动画
        transform.DOKill();

        // 恢复物理设置（暂时设为运动学模式以避免物理抖动）
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 恢复父级
        transform.SetParent(originalParent);

        // 通知控制器
        if (menuController != null)
        {
            menuController.OnItemReleased(gameObject);
        }

        // 平滑返回原位
        StartCoroutine(SmoothReturnToOrigin());

        // 恢复视觉效果
        if (enableHighlight)
        {
            RestoreOriginalColors();
        }

        // 触发事件
        OnReleased.Invoke();

        Debug.Log($"Item {gameObject.name} released and returning to origin");
    }

    /// <summary>
    /// 悬浮状态
    /// </summary>
    private void OnHover()
    {
        if (enableHighlight)
        {
            SetHighlightColor(hoverColor);
        }

        OnHovered.Invoke();
    }

    /// <summary>
    /// 取消悬浮
    /// </summary>
    private void OnUnhover()
    {
        if (enableHighlight && !isGrabbed)
        {
            RestoreOriginalColors();
        }

        OnUnhovered.Invoke();
    }

    /// <summary>
    /// 设置高亮颜色
    /// </summary>
    private void SetHighlightColor(Color color)
    {
        if (renderers == null) return;

        foreach (var renderer in renderers)
        {
            if (renderer == null || renderer.material == null) continue;

            Material mat = renderer.material;

            if (mat.HasColor("_BaseColor"))
            {
                Color originalColor = mat.GetColor("_BaseColor");
                Color highlightColor = Color.Lerp(originalColor, color, 0.5f);
                mat.SetColor("_BaseColor", highlightColor);

                // 增加发光
                if (mat.HasColor("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", color * highlightIntensity * 0.3f);
                    mat.EnableKeyword("_EMISSION");
                }
            }
            else if (mat.HasProperty("_Color"))
            {
                Color originalColor = mat.color;
                Color highlightColor = Color.Lerp(originalColor, color, 0.5f);
                mat.color = highlightColor;
            }
        }
    }

    /// <summary>
    /// 恢复原始颜色
    /// </summary>
    private void RestoreOriginalColors()
    {
        if (renderers == null || originalColors == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || renderers[i].material == null || originalColors[i] == null) continue;

            Material mat = renderers[i].material;

            if (mat.HasColor("_BaseColor"))
            {
                mat.SetColor("_BaseColor", originalColors[i][0]);
            }
            else if (mat.HasProperty("_Color"))
            {
                mat.color = originalColors[i][0];
            }

            // 关闭发光
            if (mat.HasColor("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    /// <summary>
    /// 平滑返回原位协程
    /// </summary>
    private IEnumerator SmoothReturnToOrigin()
    {
        float returnDuration = menuController?.returnToOriginDuration ?? 0.8f;
        AnimationCurve curve = menuController?.returnCurve ?? AnimationCurve.EaseInOut(0, 0, 1, 1);
        bool enableFloating = menuController?.enableFloatingOnReturn ?? true;

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 startScale = transform.localScale;

        // 使用自定义曲线创建平滑的返回动画
        transform.DOMove(originalPosition, returnDuration)
            .SetEase(curve)
            .SetUpdate(true);

        transform.DORotateQuaternion(originalRotation, returnDuration)
            .SetEase(curve)
            .SetUpdate(true);

        transform.DOScale(originalScale, returnDuration)
            .SetEase(curve)
            .SetUpdate(true);

        // 等待动画完成
        yield return new WaitForSeconds(returnDuration);

        // 恢复物理状态
        if (rb != null)
        {
            rb.isKinematic = true; // 保持运动学模式以避免物理干扰
        }

        isReturningToOrigin = false;

        // 重新启动悬浮动画（如果启用）
        if (enableFloating)
        {
            if (menuController != null)
            {
                menuController.AddFloatingEffect(gameObject);
            }
            else
            {
                AddDefaultFloatingEffect();
            }
        }

        Debug.Log($"Item {gameObject.name} returned to origin in {returnDuration:F2}s");
    }

    
    /// <summary>
    /// 添加默认悬浮动画（当没有menuController时使用）
    /// </summary>
    private void AddDefaultFloatingEffect()
    {
        // 停止之前的动画
        transform.DOKill();

        // 悬浮动画
        transform.DOMoveY(transform.position.y + 0.05f, 2f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        // 轻微旋转
        transform.DORotate(new Vector3(0, 360, 0), 20f, RotateMode.WorldAxisAdd)
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.Linear);
    }

    /// <summary>
    /// 重置到原始位置
    /// </summary>
    public void ResetToOriginalPosition()
    {
        // 停止所有动画
        transform.DOKill();

        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = originalScale;
        transform.SetParent(originalParent);

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        isGrabbed = false;
        isReturningToOrigin = false;
        RestoreOriginalColors();
    }

    /// <summary>
    /// 获取是否被抓取
    /// </summary>
    public bool IsGrabbed()
    {
        return isGrabbed;
    }

    /// <summary>
    /// 获取场景信息
    /// </summary>
    public VRMenuController.SceneItem GetSceneItem()
    {
        return sceneItem;
    }

    /// <summary>
    /// 设置悬浮状态（供外部调用）
    /// </summary>
    public void SetHovered(bool hovered)
    {
        if (isHovered == hovered) return;

        isHovered = hovered;

        if (hovered)
        {
            OnHover();
        }
        else
        {
            OnUnhover();
        }
    }

    void OnDestroy()
    {
        // 清理材质实例
        if (renderers != null)
        {
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    DestroyImmediate(renderer.material);
                }
            }
        }
    }
}