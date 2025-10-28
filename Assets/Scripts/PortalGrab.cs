using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PortalGrab : MonoBehaviour
{
    [Header("Scene")]
    public string sceneName;

    [Header("UI")]
    public GameObject sceneInfoPanel;   // 固定 UI（头或手）
    public TMP_Text sceneNameText;

    [Header("Return")]
    public float returnTime = 0.25f;    // 松手回原位的插值时间
    public AnimationCurve returnCurve = AnimationCurve.EaseInOut(0,0,1,1);

    Rigidbody _rb;
    bool _held;
    Vector3 _originPos; 
    Quaternion _originRot;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _originPos = transform.position;
        _originRot = transform.rotation;
        if (sceneInfoPanel) sceneInfoPanel.SetActive(false);
    }

    void Update()
    {
        // 仅在“被抓住”时响应 A 键
        if (_held && (OVRInput.GetDown(OVRInput.RawButton.A) ||
                      OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch)))
        {
            if (!string.IsNullOrEmpty(sceneName))
                SceneManager.LoadSceneAsync(sceneName);
        }
    }

    // —— 这些方法用 Interactable Unity Event Wrapper 接上 —— //
    // On Hovered/Unhovered: 可做高亮
    public void OnHover(bool on)
    {
        // 可选：切换Emission/描边等
    }

    // On Selected: 被抓住（ISDK 会自动对齐到默认挂点）
    public void OnSelected()
    {
        _held = true;

        // 记录“抓起前”的原位，后续松手回去
        _originPos = transform.position;
        _originRot = transform.rotation;

        if (sceneInfoPanel) sceneInfoPanel.SetActive(true);
        if (sceneNameText) sceneNameText.text = sceneName;
    }

    // On Unselected: 松手（ISDK 已经把抓取关系解除）
    public void OnUnselected()
    {
        _held = false;
        if (sceneInfoPanel) sceneInfoPanel.SetActive(false);

        // 平滑回原位（临时设为运动学以避免物理抖动）
        StartCoroutine(ReturnToOrigin());
    }

    System.Collections.IEnumerator ReturnToOrigin()
    {
        var fromP = transform.position; 
        var fromR = transform.rotation;
        float t = 0f; 
        bool wasKinematic = _rb.isKinematic;
        _rb.isKinematic = true;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, returnTime);
            float k = returnCurve.Evaluate(Mathf.Clamp01(t));
            transform.position = Vector3.LerpUnclamped(fromP, _originPos, k);
            transform.rotation = Quaternion.SlerpUnclamped(fromR, _originRot, k);
            yield return null;
        }

        _rb.isKinematic = wasKinematic;
    }
}
