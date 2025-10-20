using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Events;

namespace VRPhotography
{
    /// <summary>
    /// VR照片捕获和保存系统
    /// </summary>
    public class VRPhotoCapture : MonoBehaviour
    {
        [Header("Capture Settings")]
        [SerializeField] private int photoWidth = 1920;
        [SerializeField] private int photoHeight = 1080;
        [SerializeField] private bool captureHighRes = true;
        [SerializeField] private int highResMultiplier = 2;
        
        [Header("Save Settings")]
        [SerializeField] private string saveFolderName = "VRPhotos";
        [SerializeField] private bool includeMetadata = true;
        [SerializeField] private bool autoSave = true;
        
        [Header("File Format")]
        [SerializeField] private PhotoFormat format = PhotoFormat.JPG;
        
        [Header("Events")]
        public UnityEvent<Texture2D> onPhotoCaptured;
        public UnityEvent<string> onPhotoSaved;
        public UnityEvent onCaptureStart;
        public UnityEvent onCaptureComplete;
        
        // Photo format options
        public enum PhotoFormat
        {
            JPG,
            PNG,
            RAW
        }
        
        // Photo metadata structure
        [System.Serializable]
        public struct PhotoMetadata
        {
            public float focalLength;
            public int iso;
            public float shutterSpeed;
            public float aperture;
            public Vector3 position;
            public Quaternion rotation;
            public DateTime timestamp;
            public string sceneName;
        }
        
        // Private variables
        private VRCamera vrCamera;
        private Camera captureCamera;
        private string savePath;
        private List<CapturedPhoto> capturedPhotos = new List<CapturedPhoto>();
        private bool isCapturing = false;
        
        // Photo data structure
        [System.Serializable]
        public class CapturedPhoto
        {
            public string filePath;
            public Texture2D thumbnail;
            public PhotoMetadata metadata;
            public DateTime captureTime;
        }
        
        public List<CapturedPhoto> CapturedPhotos => capturedPhotos;
        public bool IsCapturing => isCapturing;
        
        private void Awake()
        {
            vrCamera = GetComponentInParent<VRCamera>();
            captureCamera = vrCamera?.CaptureCamera;
            
            SetupSaveDirectory();
        }
        
        private void SetupSaveDirectory()
        {
            // 创建保存目录
            savePath = Path.Combine(Application.persistentDataPath, saveFolderName);
            
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                Debug.Log($"创建照片保存目录: {savePath}");
            }
        }
        
        /// <summary>
        /// 拍照
        /// </summary>
        public void TakePhoto()
        {
            if (isCapturing || captureCamera == null)
            {
                Debug.LogWarning("无法拍照: 正在处理中或相机未就绪");
                return;
            }
            
            StartCoroutine(CapturePhotoCoroutine());
        }
        
        private IEnumerator CapturePhotoCoroutine()
        {
            isCapturing = true;
            onCaptureStart?.Invoke();
            
            yield return new WaitForEndOfFrame();
            
            try
            {
                // 获取相机参数
                var metadata = CaptureMetadata();
                
                // 创建渲染纹理
                int width = captureHighRes ? photoWidth * highResMultiplier : photoWidth;
                int height = captureHighRes ? photoHeight * highResMultiplier : photoHeight;
                
                RenderTexture rt = new RenderTexture(width, height, 24);
                captureCamera.targetTexture = rt;
                
                // 渲染到纹理
                captureCamera.Render();
                
                // 读取纹理数据
                RenderTexture.active = rt;
                Texture2D photoTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
                photoTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                photoTexture.Apply();
                
                // 恢复相机设置
                captureCamera.targetTexture = null;
                RenderTexture.active = null;
                
                // 处理照片
                yield return StartCoroutine(ProcessPhoto(photoTexture, metadata));
                
                // 清理
                Destroy(rt);
                
                onCaptureComplete?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"拍照失败: {e.Message}");
            }
            finally
            {
                isCapturing = false;
            }
        }
        
        private PhotoMetadata CaptureMetadata()
        {
            if (vrCamera == null)
                return new PhotoMetadata();
            
            return new PhotoMetadata
            {
                focalLength = vrCamera.CurrentFocalLength,
                iso = vrCamera.ISO,
                shutterSpeed = vrCamera.ShutterSpeed,
                aperture = vrCamera.Aperture,
                position = transform.position,
                rotation = transform.rotation,
                timestamp = DateTime.Now,
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            };
        }
        
        private IEnumerator ProcessPhoto(Texture2D photoTexture, PhotoMetadata metadata)
        {
            // 触发拍照完成事件
            onPhotoCaptured?.Invoke(photoTexture);
            
            if (autoSave)
            {
                yield return StartCoroutine(SavePhotoCoroutine(photoTexture, metadata));
            }
            
            // 添加到已拍照列表
            var capturedPhoto = new CapturedPhoto
            {
                thumbnail = CreateThumbnail(photoTexture),
                metadata = metadata,
                captureTime = metadata.timestamp
            };
            
            capturedPhotos.Add(capturedPhoto);
            
            // 限制内存中保存的照片数量
            if (capturedPhotos.Count > 50)
            {
                var oldPhoto = capturedPhotos[0];
                if (oldPhoto.thumbnail != null)
                    Destroy(oldPhoto.thumbnail);
                capturedPhotos.RemoveAt(0);
            }
        }
        
        private IEnumerator SavePhotoCoroutine(Texture2D photoTexture, PhotoMetadata metadata)
        {
            // 生成文件名
            string fileName = GenerateFileName(metadata);
            string filePath = Path.Combine(savePath, fileName);
            
            // 保存照片
            byte[] fileData;
            
            switch (format)
            {
                case PhotoFormat.JPG:
                    fileData = photoTexture.EncodeToJPG(90);
                    break;
                case PhotoFormat.PNG:
                    fileData = photoTexture.EncodeToPNG();
                    break;
                default:
                    fileData = photoTexture.EncodeToJPG(90);
                    break;
            }
            
            yield return new WaitForEndOfFrame();
            
            File.WriteAllBytes(filePath, fileData);
            
            // 保存元数据
            if (includeMetadata)
            {
                yield return StartCoroutine(SaveMetadataCoroutine(filePath, metadata));
            }
            
            // 更新照片对象
            var photo = capturedPhotos[capturedPhotos.Count - 1];
            photo.filePath = filePath;
            
            Debug.Log($"照片已保存: {filePath}");
            onPhotoSaved?.Invoke(filePath);
        }
        
        private IEnumerator SaveMetadataCoroutine(string photoPath, PhotoMetadata metadata)
        {
            string metadataPath = Path.ChangeExtension(photoPath, ".json");
            string json = JsonUtility.ToJson(metadata, true);
            
            yield return new WaitForEndOfFrame();
            
            File.WriteAllText(metadataPath, json);
        }
        
        private string GenerateFileName(PhotoMetadata metadata)
        {
            string timestamp = metadata.timestamp.ToString("yyyyMMdd_HHmmss");
            string extension = format.ToString().ToLower();
            
            return $"VR_Photo_{timestamp}_{(int)metadata.focalLength}mm.{extension}";
        }
        
        private Texture2D CreateThumbnail(Texture2D originalTexture)
        {
            int thumbnailSize = 256;
            Texture2D thumbnail = new Texture2D(thumbnailSize, thumbnailSize);
            
            // 计算缩放和裁剪
            float aspectRatio = (float)originalTexture.width / originalTexture.height;
            int cropWidth, cropHeight;
            
            if (aspectRatio > 1f)
            {
                cropHeight = originalTexture.height;
                cropWidth = (int)(originalTexture.height * aspectRatio);
            }
            else
            {
                cropWidth = originalTexture.width;
                cropHeight = (int)(originalTexture.width / aspectRatio);
            }
            
            // 简化的缩放处理
            Color[] pixels = originalTexture.GetPixels(0, 0, cropWidth, cropHeight);
            Color[] thumbnailPixels = new Color[thumbnailSize * thumbnailSize];
            
            for (int y = 0; y < thumbnailSize; y++)
            {
                for (int x = 0; x < thumbnailSize; x++)
                {
                    float sourceX = (float)x / thumbnailSize * cropWidth;
                    float sourceY = (float)y / thumbnailSize * cropHeight;
                    
                    int srcX = Mathf.FloorToInt(sourceX);
                    int srcY = Mathf.FloorToInt(sourceY);
                    
                    thumbnailPixels[y * thumbnailSize + x] = pixels[srcY * cropWidth + srcX];
                }
            }
            
            thumbnail.SetPixels(thumbnailPixels);
            thumbnail.Apply();
            
            return thumbnail;
        }
        
        // Public Methods
        
        /// <summary>
        /// 获取保存的文件夹路径
        /// </summary>
        public string GetSaveFolderPath()
        {
            return savePath;
        }
        
        /// <summary>
        /// 打开保存文件夹
        /// </summary>
        public void OpenSaveFolder()
        {
            Application.OpenURL($"file://{savePath}");
        }
        
        /// <summary>
        /// 删除照片
        /// </summary>
        public void DeletePhoto(CapturedPhoto photo)
        {
            if (photo == null) return;
            
            // 删除文件
            if (File.Exists(photo.filePath))
            {
                File.Delete(photo.filePath);
            }
            
            // 删除元数据文件
            string metadataPath = Path.ChangeExtension(photo.filePath, ".json");
            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }
            
            // 从列表中移除
            capturedPhotos.Remove(photo);
            
            // 清理缩略图
            if (photo.thumbnail != null)
            {
                Destroy(photo.thumbnail);
            }
            
            Debug.Log($"照片已删除: {photo.filePath}");
        }
        
        /// <summary>
        /// 清空所有照片
        /// </summary>
        public void ClearAllPhotos()
        {
            foreach (var photo in capturedPhotos)
            {
                DeletePhoto(photo);
            }
            
            capturedPhotos.Clear();
            
            // 清空文件夹
            if (Directory.Exists(savePath))
            {
                var files = Directory.GetFiles(savePath);
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            
            Debug.Log("所有照片已清空");
        }
        
        /// <summary>
        /// 获取照片数量
        /// </summary>
        public int GetPhotoCount()
        {
            return capturedPhotos.Count;
        }
        
        /// <summary>
        /// 加载已保存的照片
        /// </summary>
        public void LoadSavedPhotos()
        {
            if (!Directory.Exists(savePath))
                return;
            
            var imageFiles = Directory.GetFiles(savePath, "*.jpg")
                .Concat(Directory.GetFiles(savePath, "*.png"))
                .ToArray();
            
            foreach (var filePath in imageFiles)
            {
                try
                {
                    // 加载照片
                    byte[] fileData = File.ReadAllBytes(filePath);
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(fileData);
                    
                    // 加载元数据
                    string metadataPath = Path.ChangeExtension(filePath, ".json");
                    PhotoMetadata metadata = new PhotoMetadata();
                    
                    if (File.Exists(metadataPath))
                    {
                        string json = File.ReadAllText(metadataPath);
                        metadata = JsonUtility.FromJson<PhotoMetadata>(json);
                    }
                    
                    // 创建照片对象
                    var photo = new CapturedPhoto
                    {
                        filePath = filePath,
                        thumbnail = CreateThumbnail(texture),
                        metadata = metadata,
                        captureTime = metadata.timestamp
                    };
                    
                    capturedPhotos.Add(photo);
                    
                    // 清理大纹理
                    if (texture.width > 1024 || texture.height > 1024)
                        Destroy(texture);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载照片失败 {filePath}: {e.Message}");
                }
            }
            
            Debug.Log($"加载了 {capturedPhotos.Count} 张照片");
        }
    }
}