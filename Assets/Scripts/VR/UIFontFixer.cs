using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI字体修复器 - 解决中文字符显示问题
/// </summary>
public class UIFontFixer : MonoBehaviour
{
    [Header("字体设置")]
    public TMP_FontAsset chineseFont; // 中文字体资源

    [Header("自动修复")]
    public bool fixOnStart = true;
    public bool fixChildrenTexts = true;

    // 中文文本映射
    private Dictionary<string, string> chineseToEnglish = new Dictionary<string, string>
    {
        {"快门", "Shutter"},
        {"光圈", "Aperture"},
        {"对焦", "Focus"},
        {"焦段", "Focal"},
        {"ISO", "ISO"},
        {"速度", "Speed"}
    };

    void Start()
    {
        if (fixOnStart)
        {
            FixAllTexts();
        }
    }

    /// <summary>
    /// 修复所有TextMeshPro文本
    /// </summary>
    public void FixAllTexts()
    {
        // 查找所有TextMeshPro组件
        TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>();

        foreach (TextMeshProUGUI text in texts)
        {
            FixText(text);
        }

        Debug.Log($"[UIFontFixer] 修复了 {texts.Length} 个TextMeshPro文本组件");
    }

    /// <summary>
    /// 修复单个文本组件
    /// </summary>
    public void FixText(TextMeshProUGUI textComponent)
    {
        if (textComponent == null) return;

        string originalText = textComponent.text;
        string fixedText = FixChineseText(originalText);

        // 如果有中文字体资源，使用中文字体
        if (chineseFont != null)
        {
            textComponent.font = chineseFont;
        }
        else
        {
            // 否则使用英文替换
            textComponent.text = fixedText;
        }
    }

    /// <summary>
    /// 修复中文文本
    /// </summary>
    private string FixChineseText(string text)
    {
        string result = text;

        foreach (var kvp in chineseToEnglish)
        {
            result = result.Replace(kvp.Key, kvp.Value);
        }

        return result;
    }

    /// <summary>
    /// 手动添加中英文映射
    /// </summary>
    public void AddChineseMapping(string chinese, string english)
    {
        chineseToEnglish[chinese] = english;
    }

    /// <summary>
    /// 查找并设置中文字体
    /// </summary>
    [ContextMenu("查找中文字体")]
    public void FindChineseFont()
    {
        // 尝试查找常见的中文字体
        string[] fontNames = { "Arial Unicode MS", "Microsoft YaHei", "SimHei", "SimSun" };

        foreach (string fontName in fontNames)
        {
            Font font = Resources.GetBuiltinResource<Font>(fontName + ".ttf");
            if (font != null)
            {
                Debug.Log($"[UIFontFixer] 找到字体: {fontName}");
                // 这里可以转换为TMP_FontAsset
                break;
            }
        }
    }

    void OnValidate()
    {
        // 在Inspector中显示帮助信息
        #if UNITY_EDITOR
        if (chineseFont == null)
        {
            Debug.LogWarning("[UIFontFixer] 建议分配中文字体资源以正确显示中文文本");
        }
        #endif
    }
}