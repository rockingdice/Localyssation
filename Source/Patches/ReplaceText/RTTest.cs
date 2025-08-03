using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Localyssation.Patches.ReplaceText
{
    public class TextCaptureSystem
    {
        // 缓存已记录的调用栈+文本组合
        private static readonly HashSet<string> _recordedEntries = new HashSet<string>();
        private static readonly object _lock = new object();

        // 配置参数
        private const int MaxCacheSize = 2000;
        private const int StackTraceLinesToKeep = 8;

        private static bool _isInitialized = false;

        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            
            var harmony = new Harmony("com.yourname.textcapture");
            HookAllTextSetters(harmony);
            HookCustomTextComponents(harmony);
        }

        private static void HookAllTextSetters(Harmony harmony)
        {
            // Hook标准Text组件
            HookComponentSetter<Text>(harmony, "text");

            // Hook InputField组件
            HookComponentSetter<InputField>(harmony, "text");

            // Hook Dropdown组件
            HookComponentSetter<Dropdown>(harmony, "captionText");

            // Hook TextMeshPro组件
            HookTextMeshProSetters(harmony);
        }

        private static void HookCustomTextComponents(Harmony harmony)
        {
            // Hook LangAdjustableUIText
            HookLangAdjustableText(harmony);
            
            // Hook其他可能存在的自定义文本组件
            HookComponentType(harmony, "LocalizedText", "Text");
            HookComponentType(harmony, "TranslatableText", "SetText");
        }

        private static void HookLangAdjustableText(Harmony harmony)
        {
            try
            {
                Type langTextType = GetLangTextType();
                if (langTextType == null)
                {
                    Debug.LogError("[TextCapture] 未找到LangAdjustableUIText类型");
                    return;
                }

                // Hook SetText方法
                MethodInfo setTextMethod = langTextType.GetMethod("SetText", new[] { typeof(string) });
                if (setTextMethod != null)
                {
                    harmony.Patch(setTextMethod,
                        prefix: new HarmonyMethod(typeof(TextCaptureSystem), nameof(RecordCustomTextAssignment)));
                }

                // Hook Text属性
                PropertyInfo textProperty = langTextType.GetProperty("Text");
                if (textProperty != null && textProperty.SetMethod != null)
                {
                    harmony.Patch(textProperty.SetMethod,
                        prefix: new HarmonyMethod(typeof(TextCaptureSystem), nameof(RecordCustomTextAssignment)));
                }

                Debug.Log("[TextCapture] 已Hook LangAdjustableUIText");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TextCapture] Hook LangAdjustableUIText失败: {ex}");
            }
        }

        private static void HookComponentType(Harmony harmony, string typeName, string propertyOrMethodName)
        {
            try
            {
                Type componentType = AccessTools.TypeByName(typeName);
                if (componentType == null) return;

                // 尝试作为属性Hook
                PropertyInfo property = componentType.GetProperty(propertyOrMethodName);
                if (property != null && property.SetMethod != null)
                {
                    harmony.Patch(property.SetMethod,
                        prefix: new HarmonyMethod(typeof(TextCaptureSystem), nameof(RecordCustomTextAssignment)));
                    Debug.Log($"[TextCapture] Hooked {typeName}.{propertyOrMethodName} property");
                    return;
                }

                // 尝试作为方法Hook
                MethodInfo method = componentType.GetMethod(propertyOrMethodName, new[] { typeof(string) });
                if (method != null)
                {
                    harmony.Patch(method,
                        prefix: new HarmonyMethod(typeof(TextCaptureSystem), nameof(RecordCustomTextAssignment)));
                    Debug.Log($"[TextCapture] Hooked {typeName}.{propertyOrMethodName} method");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TextCapture] Hook {typeName}失败: {ex}");
            }
        }

        private static Type GetLangTextType()
        {
            return Type.GetType("LangAdjustableUIText, Assembly-CSharp") ?? 
                   AccessTools.TypeByName("LangAdjustableUIText");
        }

        private static void HookComponentSetter<T>(Harmony harmony, string propertyName) where T : Component
        {
            var property = typeof(T).GetProperty(propertyName);
            if (property == null) return;

            var setter = property.GetSetMethod();
            if (setter == null) return;

            harmony.Patch(setter,
                prefix: new HarmonyMethod(typeof(TextCaptureSystem), nameof(RecordTextAssignment)));
        }

        private static void HookTextMeshProSetters(Harmony harmony)
        {
            // 尝试获取TextMeshPro类型
            var tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro") ??
                          Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");

            if (tmpType == null) return;

            // Hook TMP组件的text属性
            var textProperty = tmpType.GetProperty("text");
            if (textProperty != null)
            {
                var setter = textProperty.GetSetMethod();
                if (setter != null)
                {
                    harmony.Patch(setter,
                        prefix: new HarmonyMethod(typeof(TextCaptureSystem), nameof(RecordTextAssignment)));
                }
            }
        }

        // 记录所有文本赋值操作
        public static void RecordTextAssignment(object __instance, ref string value)
        {
            LogTextAssignment(__instance, value);
        }

        // 记录自定义文本组件的赋值操作
        public static void RecordCustomTextAssignment(object __instance, string value)
        {
            LogTextAssignment(__instance, value);
        }

        // 公共文本记录逻辑
        private static void LogTextAssignment(object instance, string value)
        {
            try
            {
                // 获取调用栈信息
                var stackTrace = Environment.StackTrace;

                // 生成唯一缓存键
                var cacheKey = GenerateCacheKey(stackTrace, value);

                lock (_lock)
                {
                    // 检查是否已记录过此操作
                    if (_recordedEntries.Contains(cacheKey)) return;

                    // 添加到记录集合
                    _recordedEntries.Add(cacheKey);

                    // 清理旧记录
                    if (_recordedEntries.Count > MaxCacheSize)
                    {
                        var toRemove = _recordedEntries.Take(100).ToList();
                        foreach (var key in toRemove) _recordedEntries.Remove(key);
                    }
                }

                // 获取组件类型信息
                string componentInfo = GetComponentInfo(instance);

                // 记录详细信息
                Debug.Log($"[Text Assignment]\n" +
                          $"Component: {componentInfo}\n" +
                          $"Value: {value}\n" +
                          $"Call Stack:\n{GetSimplifiedStackTrace(stackTrace)}\n" +
                          $"----------------------------------");
            }
            catch
            {
                // 防止记录过程导致崩溃
            }
        }

        private static string GenerateCacheKey(string stackTrace, string value)
        {
            // 使用调用栈特征和文本内容生成唯一键
            return $"{GetStackSignature(stackTrace)}|{value}";
        }

        private static string GetStackSignature(string stackTrace)
        {
            // 提取关键调用栈行
            var keyLines = stackTrace.Split('\n')
                .Where(line => line.Contains(" at ") &&
                            !line.Contains("System.") &&
                            !line.Contains("Harmony."))
                .Select(SimplifyStackLine)
                .Take(StackTraceLinesToKeep)
                .ToArray();

            return string.Join(";", keyLines);
        }

        private static string GetSimplifiedStackTrace(string stackTrace)
        {
            return string.Join("\n", stackTrace.Split('\n')
                .Where(line => line.Contains(" at ") &&
                            !line.Contains("System.") &&
                            !line.Contains("Harmony."))
                .Select(SimplifyStackLine)
                .Take(StackTraceLinesToKeep));
        }

        private static string SimplifyStackLine(string line)
        {
            // 提取 "at " 之后的内容
            int start = line.IndexOf(" at ") + 4;
            if (start < 4) return line;

            string cleaned = line.Substring(start);

            // 移除参数列表
            int paren = cleaned.IndexOf('(');
            if (paren > 0) cleaned = cleaned.Substring(0, paren);

            // 移除泛型参数
            int generic = cleaned.IndexOf('[');
            if (generic > 0) cleaned = cleaned.Substring(0, generic);

            return cleaned.Trim();
        }

        private static string GetComponentInfo(object instance)
        {
            if (instance is Component component)
            {
                // 获取组件路径
                string path = GetGameObjectPath(component.gameObject);

                // 获取场景信息
                string scene = component.gameObject.scene.IsValid() ?
                    component.gameObject.scene.name : "DontDestroyOnLoad";

                return $"{component.GetType().Name} on {path} (Scene: {scene})";
            }

            return instance?.GetType().Name ?? "Unknown";
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            try
            {
                string path = obj.name;
                Transform parent = obj.transform.parent;

                while (parent != null)
                {
                    path = parent.name + "/" + path;
                    parent = parent.parent;
                }

                return path;
            }
            catch
            {
                return obj.name;
            }
        }
    }
}