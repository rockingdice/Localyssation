// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Reflection;
// using HarmonyLib;
// using UnityEngine;
// using UnityEngine.UI;

// public class TargetedTextCapture
// {
//     private const string TargetPath = "_GameUI_TabMenu/Canvas_InGameMenu/Dolly_MenuIndex/_cell_questMenu/_scrollView_questInfo/Viewport/_scrollContent_questInfo/_questRewardPanel";
//     private const string TargetComponent = "_text_currencyReward";
    
//     private static bool _isInitialized = false;
//     private static Text _targetTextComponent;
//     private static FieldInfo _textFieldInfo;
//     private static MonoBehaviourHelper _coroutineHelper;

//     public static void Initialize()
//     {
//         if (_isInitialized) return;
//         _isInitialized = true;
        
//         try
//         {
//             // 创建协程助手实例
//             GameObject helperObj = new GameObject("TextCaptureHelper");
//             UnityEngine.Object.DontDestroyOnLoad(helperObj);
//             _coroutineHelper = helperObj.AddComponent<MonoBehaviourHelper>();
            
//             // 启动协程查找目标组件
//             _coroutineHelper.StartCoroutine(FindTargetComponentImpl());
            
//             // Hook 相关方法
//             var harmony = new Harmony("com.yourname.targetedcapture");
//             HookQuestMenuMethods(harmony);
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError($"[TargetCapture] 初始化失败: {ex}");
//         }
//     }

//     // 将协程方法设为public
//     public static IEnumerator FindTargetComponentImpl()
//     {
//         int attempts = 0;
        
//         while (attempts < 50)
//         {
//             yield return new WaitForSeconds(0.5f);
            
//             try
//             {
//                 // 尝试通过路径查找目标对象
//                 GameObject targetPanel = GameObject.Find(TargetPath);
//                 if (targetPanel == null)
//                 {
//                     Debug.Log($"[TargetCapture] 未找到目标面板: {TargetPath}");
//                     attempts++;
//                     continue;
//                 }
                
//                 // 查找目标文本组件
//                 Transform targetTransform = FindDeepChild(targetPanel.transform, TargetComponent);
//                 if (targetTransform == null)
//                 {
//                     Debug.Log($"[TargetCapture] 在面板中找到 {targetPanel.transform.childCount} 个子对象，但未找到目标组件: {TargetComponent}");
//                     attempts++;
//                     continue;
//                 }
                
//                 // 获取文本组件
//                 _targetTextComponent = targetTransform.GetComponent<Text>();
//                 if (_targetTextComponent == null)
//                 {
//                     Debug.Log($"[TargetCapture] 找到目标对象，但不是Text组件");
//                     attempts++;
//                     continue;
//                 }
                
//                 // 使用反射获取私有字段
//                 _textFieldInfo = typeof(Text).GetField("m_Text", BindingFlags.NonPublic | BindingFlags.Instance);
                
//                 if (_textFieldInfo != null)
//                 {
//                     Debug.Log($"[TargetCapture] 成功定位目标文本组件！");
//                     // 添加监听器捕获所有文本变化
//                     _targetTextComponent.RegisterDirtyLayoutCallback(OnTextChanged);
//                     yield break; // 找到目标，退出循环
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[TargetCapture] 查找过程中出错: {ex}");
//             }
            
//             attempts++;
//         }
        
//         Debug.LogWarning($"[TargetCapture] 在25秒内未找到目标组件");
//     }
    
//     // 深度查找子对象
//     private static Transform FindDeepChild(Transform parent, string name)
//     {
//         if (parent == null) return null;
        
//         // 先检查直接子对象
//         Transform result = parent.Find(name);
//         if (result != null) return result;
        
//         // 递归检查所有子对象
//         foreach (Transform child in parent)
//         {
//             result = FindDeepChild(child, name);
//             if (result != null) return result;
//         }
        
//         return null;
//     }

//     // 文本变化回调
//     private static void OnTextChanged()
//     {
//         if (_targetTextComponent == null) return;
        
//         try
//         {
//             // 使用反射获取实际设置的文本值
//             string actualValue = _textFieldInfo?.GetValue(_targetTextComponent) as string;
            
//             if (!string.IsNullOrEmpty(actualValue))
//             {
//                 Debug.Log($"[TargetCapture] 文本变化捕获:\n" +
//                          $"路径: {TargetPath}/{TargetComponent}\n" +
//                          $"值: {actualValue}\n" +
//                          $"调用栈: {Environment.StackTrace}");
//             }
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError($"[TargetCapture] 捕获文本变化时出错: {ex}");
//         }
//     }

//     // Hook相关方法
//     private static void HookQuestMenuMethods(Harmony harmony)
//     {
//         try
//         {
//             // Hook QuestMenuCell.Select_QuestSlot 方法
//             var questMenuCellType = AccessTools.TypeByName("QuestMenuCell");
//             if (questMenuCellType != null)
//             {
//                 var selectMethod = AccessTools.Method(questMenuCellType, "Select_QuestSlot");
//                 if (selectMethod != null)
//                 {
//                     harmony.Patch(selectMethod,
//                         prefix: new HarmonyMethod(typeof(TargetedTextCapture), nameof(Prefix_SelectQuestSlot)));
//                 }
//             }
            
//             // Hook QuestMenuCell.Apply_QuestInfo 方法
//             var applyMethod = AccessTools.Method(questMenuCellType, "Apply_QuestInfo");
//             if (applyMethod != null)
//             {
//                 harmony.Patch(applyMethod,
//                     prefix: new HarmonyMethod(typeof(TargetedTextCapture), nameof(Prefix_ApplyQuestInfo)));
//             }
            
//             // Hook QuestMenuCellSlot.OnClick_QuestCellButton 方法
//             var questMenuCellSlotType = AccessTools.TypeByName("QuestMenuCellSlot");
//             if (questMenuCellSlotType != null)
//             {
//                 var onClickMethod = AccessTools.Method(questMenuCellSlotType, "OnClick_QuestCellButton");
//                 if (onClickMethod != null)
//                 {
//                     harmony.Patch(onClickMethod,
//                         prefix: new HarmonyMethod(typeof(TargetedTextCapture), nameof(Prefix_OnClickQuestCell)));
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError($"[TargetCapture] Hook失败: {ex}");
//         }
//     }

//     #region Hook 方法
//     public static void Prefix_SelectQuestSlot()
//     {
//         Debug.Log($"[TargetCapture] Select_QuestSlot 被调用");
//         CaptureTargetText();
//     }
    
//     public static void Prefix_ApplyQuestInfo()
//     {
//         Debug.Log($"[TargetCapture] Apply_QuestInfo 被调用");
//         CaptureTargetText();
//     }
    
//     public static void Prefix_OnClickQuestCell()
//     {
//         Debug.Log($"[TargetCapture] OnClick_QuestCellButton 被调用");
//         CaptureTargetText();
//     }
//     #endregion

//     // 捕获目标文本
//     public static void CaptureTargetText()
//     {
//         if (_targetTextComponent == null) return;
        
//         try
//         {
//             // 获取当前文本值
//             string textValue = _targetTextComponent.text;
            
//             if (!string.IsNullOrEmpty(textValue))
//             {
//                 Debug.Log($"[TargetCapture] 目标文本状态:\n" +
//                          $"路径: {TargetPath}/{TargetComponent}\n" +
//                          $"值: {textValue}\n" +
//                          $"调用栈: {Environment.StackTrace}");
//             }
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError($"[TargetCapture] 捕获目标文本时出错: {ex}");
//         }
//     }
// }
 