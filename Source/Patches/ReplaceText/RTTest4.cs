using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using HarmonyLib;

public class TextComponentScanner
{
    private static readonly HashSet<int> processedComponents = new HashSet<int>();
    private static bool initialized = false;

    public static void Init()
    {
        if (initialized) return;
        initialized = true;

        var harmony = new Harmony("com.example.textscanner");

        // 更可靠的方法：使用标准的 Unity 事件
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 对 GameObject 的构造函数进行补丁
        var ctorOriginal = AccessTools.Constructor(typeof(GameObject), new Type[] { typeof(string) });
        harmony.Patch(ctorOriginal, postfix: new HarmonyMethod(AccessTools.Method(typeof(TextComponentScanner), nameof(AfterGameObjectCreated))));

        // 处理当前场景
        ProcessAllScenes();

        // 启动协程定期检查
        CreateCoroutine();
    }

    // 创建协程（独立于 MonoBehaviour）
    static void CreateCoroutine()
    {
        if (UnityEngine.Object.FindObjectOfType<CoroutineRunner>() == null)
        {
            var runner = new GameObject("TextScanner_CoroutineRunner").AddComponent<CoroutineRunner>();
            runner.gameObject.hideFlags = HideFlags.HideAndDontSave;
            runner.StartCoroutine(PeriodicScanRoutine());
        }
    }

    // 定期扫描的协程
    static IEnumerator PeriodicScanRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1); // 每秒扫描一次
            ProcessAllScenes();
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ProcessSceneTexts(scene);
    }

    // 处理所有活动场景
    static void ProcessAllScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            ProcessSceneTexts(SceneManager.GetSceneAt(i));
        }
    }

    static void ProcessSceneTexts(Scene scene)
    {
        if (!scene.isLoaded) return;

        var rootObjects = scene.GetRootGameObjects();
        foreach (var root in rootObjects)
        {
            ProcessTextComponents(root);
        }
    }

    [HarmonyPostfix]
    public static void AfterGameObjectCreated(GameObject __instance)
    {
        if (__instance != null)
        {
            ProcessTextComponents(__instance);
        }
    }

    // 处理所有文本组件
    static void ProcessTextComponents(GameObject root)
    {
        if (root == null) return;

        var texts = root.GetComponentsInChildren<Text>(true);
        foreach (var text in texts)
        {
            if (text == null || string.IsNullOrEmpty(text.text)) continue;

            var instanceID = text.GetInstanceID();
            if (processedComponents.Contains(instanceID)) continue;

            processedComponents.Add(instanceID);

            string path = GetFullPath(text.transform);
            string value = text.text;

            string prefabName = "Scene Object";
            if (text.gameObject.scene.name == null)
            {
                prefabName = "Prefab Preview";
            }
            else if (text.transform.root != text.transform && text.transform.root != null)
            {
                prefabName = text.transform.root.name;
            }

            string logMessage = $"Text Component Found:" +
                              $"\nPrefab: {prefabName}" +
                              $"\nPath: {path}" +
                              $"\nValue: \"{value}\"" +
                              $"\n===========================";

            Debug.Log(logMessage);
        }
    }

    // 获取完整路径
    static string GetFullPath(Transform transform)
    {
        if (transform == null) return string.Empty;
        var path = transform.name;
        var parent = transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}

// 用于运行协程的辅助组件
public class CoroutineRunner : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}

// BepInEx 插件入口
[BepInEx.BepInPlugin("com.example.TextScanner", "Text Scanner", "1.0.0")]
public class TextScannerPlugin : BepInEx.BaseUnityPlugin
{
    void Awake()
    {
        TextComponentScanner.Init();
        Logger.LogInfo("Text Scanner plugin loaded");
    }
}