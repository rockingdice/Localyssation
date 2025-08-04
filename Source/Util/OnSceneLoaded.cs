using Localyssation.LangAdjutable;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Localyssation.Util
{
    internal static class OnSceneLoaded
    {
        public static void Init()
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
        {
            List<GameObject> GetRootGameObjects()
            {
                var rootGameObjs = new List<GameObject>();
                scene.GetRootGameObjects(rootGameObjs);
                return rootGameObjs;
            }

            switch (scene.name)
            {
                case "00_bootStrapper":
                    var rootGameObjs = GetRootGameObjects();
                    var obj_Canvas_loading = rootGameObjs.First(x => x.name == "Canvas_loading");
                    if (obj_Canvas_loading)
                    {
                        foreach (var text in obj_Canvas_loading.GetComponentsInChildren<UnityEngine.UI.Text>())
                        {
                            if (text.text == "Loading...")
                            {
                                LangAdjustables.RegisterText(text, LangAdjustables.GetStringFunc("GAME_LOADING", text.text));
                                text.alignment = TextAnchor.MiddleRight;
                            }
                        }
                    }
                    break;
                case "01_rootScene":
                    break;
            }

            //Handle all npc names
            ProcessSceneNPCs(scene);
        }
        private static void ProcessSceneNPCs(Scene scene)
        {
            var rootObjects = new List<GameObject>();
            scene.GetRootGameObjects(rootObjects);

            foreach (var root in rootObjects)
            {
                var npcs = root.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name.StartsWith("_npc_"))
                    .Select(t => t.gameObject);

                foreach (var npc in npcs)
                {
                    ProcessSingleNPC(npc);
                }
            }
        }
        private static void ProcessSingleNPC(GameObject npc)
        { 
             Transform visual = npc.transform.Find("_visual");
            if (visual == null) return;

            Transform nameNode = visual.Find("_text_npcName");
            if (nameNode == null) return;
             
            var tmpText = nameNode.GetComponent<TMPro.TextMeshPro>();
            var npcName = "";
            if (tmpText != null)
            {
                npcName = tmpText.text;
                tmpText.font = FontManager.UNIFONT_SDF;
                tmpText.text = Localyssation.GetString(KeyUtil.GetForNpcName(tmpText.text), tmpText.text);                
            }

            Transform subtitleNode = nameNode.Find("_text_npcSubtitle");
            if (subtitleNode == null) return;
             
            tmpText = subtitleNode.GetComponent<TMPro.TextMeshPro>();
            if (tmpText != null)
            {
                tmpText.font = FontManager.UNIFONT_SDF;
                tmpText.text = Localyssation.GetString(KeyUtil.GetForNpcSubtitle(npcName), tmpText.text);
            }
            
        }
    }
}