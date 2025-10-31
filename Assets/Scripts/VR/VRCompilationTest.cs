using UnityEngine;

/// <summary>
/// VR脚本编译测试
/// 用于验证所有VR脚本是否可以正常编译
/// </summary>
public class VRCompilationTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("VR Scripts Compilation Test Started");

        // 测试所有VR脚本是否可以正常实例化
        TestVRMenuController();
        TestVRCameraController();
        TestVRGrabbable();
        TestVRCameraModel();
        TestCameraUIManager();

        Debug.Log("VR Scripts Compilation Test Completed Successfully!");
    }

    private void TestVRMenuController()
    {
        var menuController = gameObject.AddComponent<VRMenuController>();
        if (menuController != null)
        {
            Debug.Log("✅ VRMenuController compiled successfully");
            DestroyImmediate(menuController);
        }
    }

    private void TestVRCameraController()
    {
        var cameraController = gameObject.AddComponent<VRCameraController>();
        if (cameraController != null)
        {
            Debug.Log("✅ VRCameraController compiled successfully");
            DestroyImmediate(cameraController);
        }
    }

    private void TestVRGrabbable()
    {
        var grabbable = gameObject.AddComponent<VRGrabbable>();
        if (grabbable != null)
        {
            Debug.Log("✅ VRGrabbable compiled successfully");
            DestroyImmediate(grabbable);
        }
    }

    private void TestVRCameraModel()
    {
        var cameraModel = gameObject.AddComponent<VRCameraModel>();
        if (cameraModel != null)
        {
            Debug.Log("✅ VRCameraModel compiled successfully");
            DestroyImmediate(cameraModel);
        }
    }

    private void TestCameraUIManager()
    {
        var uiManager = gameObject.AddComponent<CameraUIManager>();
        if (uiManager != null)
        {
            Debug.Log("✅ CameraUIManager compiled successfully");
            DestroyImmediate(uiManager);
        }
    }
}