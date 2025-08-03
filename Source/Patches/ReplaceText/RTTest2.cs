using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

public class SiblingComponentsLogger
{
    private const string TargetPath = "_GameUI_TabMenu/Canvas_InGameMenu/Dolly_MenuIndex/_cell_questMenu/_scrollView_questInfo/Viewport/_scrollContent_questInfo/_questRewardPanel";
    private const string TargetComponent = "_text_currencyReward";
    
    private static bool _isInitialized = false;
    private static MonoBehaviourHelper _coroutineHelper;
    private static bool _hasPrintedSiblings = false; // 确保只打印一次

    public static void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        
        try
        {
            // 创建协程助手实例
            GameObject helperObj = new GameObject("SiblingLoggerHelper");
            UnityEngine.Object.DontDestroyOnLoad(helperObj);
            _coroutineHelper = helperObj.AddComponent<MonoBehaviourHelper>();
            
            // 启动协程查找目标组件
            _coroutineHelper.StartCoroutine(FindTargetComponentImpl());
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SiblingLogger] 初始化失败: {ex}");
        }
    }

    public static IEnumerator FindTargetComponentImpl()
    {
        int attempts = 0;
        
        while (attempts < 200) // 减少尝试次数
        {
            yield return new WaitForSeconds(0.5f);
            
            try
            {
                // 尝试通过路径查找目标对象
                GameObject targetPanel = GameObject.Find(TargetPath);
                if (targetPanel == null)
                {
                    // Debug.Log($"[SiblingLogger] 未找到目标面板: {TargetPath}");
                    attempts++;
                    continue;
                }
                
                // 查找目标文本组件
                Transform targetTransform = targetPanel.transform.Find(TargetComponent);
                if (targetTransform == null)
                {
                    // Debug.Log($"[SiblingLogger] 在面板中找到 {targetPanel.transform.childCount} 个子对象，但未找到目标组件: {TargetComponent}");
                    attempts++;
                    continue;
                }
                
                // 找到目标组件，记录其同级组件
                LogSiblingComponents(targetTransform);
                
                yield break; // 找到目标，退出循环
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SiblingLogger] 查找过程中出错: {ex}");
            }
            
            attempts++;
        }
        
        Debug.LogWarning($"[SiblingLogger] 在10秒内未找到目标组件");
    }
    
    // 记录同级组件信息
    private static void LogSiblingComponents(Transform targetTransform)
    {
        if (_hasPrintedSiblings) return;
        _hasPrintedSiblings = true;
        
        try
        {
            Transform parent = targetTransform.parent;
            if (parent == null)
            {
                Debug.Log($"[SiblingLogger] 目标对象没有父级: {targetTransform.name}");
                return;
            }
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[SiblingLogger] 目标对象 '{targetTransform.name}' 的同级组件列表:");
            sb.AppendLine($"父级: {GetFullPath(parent)}");
            sb.AppendLine("同级组件:");
            
            int siblingIndex = 0;
            foreach (Transform child in parent)
            {
                // 获取所有组件类型
                List<string> componentTypes = new List<string>();
                foreach (Component comp in child.GetComponents<Component>())
                {
                    componentTypes.Add(comp.GetType().Name);
                }
                
                string componentsStr = string.Join(", ", componentTypes);
                string isTarget = (child == targetTransform) ? " (目标)" : "";
                
                sb.AppendLine($"[{siblingIndex}] {child.name}{isTarget} - 组件: {componentsStr}");
                siblingIndex++;
            }
            
            Debug.Log(sb.ToString());
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SiblingLogger] 记录同级组件时出错: {ex}");
        }
    }
    
    // 获取完整路径
    private static string GetFullPath(Transform transform)
    {
        if (transform == null) return "";
        if (transform.parent == null) return transform.name;
        return GetFullPath(transform.parent) + "/" + transform.name;
    }
}

// 协程助手类
public class MonoBehaviourHelper : MonoBehaviour
{
    // 不需要额外方法
}