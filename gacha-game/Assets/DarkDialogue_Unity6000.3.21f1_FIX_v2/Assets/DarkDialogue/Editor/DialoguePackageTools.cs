using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DarkDialogue.Editor
{
    public static class DialoguePackageTools
    {
        public const string PrefabPath = "Assets/DarkDialogue/Prefabs/DarkDialogue.prefab";

        [MenuItem("Tools/Dark Dialogue/Open Demo Scene")]
        static void OpenDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/DarkDialogue/Demo/DialogueDemo.unity");
        }

        [MenuItem("Tools/Dark Dialogue/Add Dialogue Prefab to Scene")]
        static void AddPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) { Debug.LogError("Dark Dialogue prefab was not found."); return; }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Add Dialogue Prefab");
            Selection.activeGameObject = instance;
        }

        [MenuItem("Tools/Dark Dialogue/Validate Package")]
        static void ValidatePackage()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) { Debug.LogError("FAIL: prefab could not be loaded."); return; }
            var manager = prefab.GetComponent<DialogueManager>();
            if (manager == null) { Debug.LogError("FAIL: DialogueManager is missing."); return; }
            var serialized = new SerializedObject(manager);
            string[] required = {
                "sequence", "presentation", "speakerName", "dialogueText", "progressText", "nextIcon", "advanceButton",
                "autoButton", "skipButton", "logButton", "autoLabel", "skipLabel", "background", "leftPortrait",
                "rightPortrait", "stage", "choicePanel", "choiceTemplate", "logPanel", "logText", "logPageText",
                "closeLogButton", "previousLogButton", "nextLogButton", "musicSource", "effectSource"
            };
            int errors = 0;
            foreach (string name in required)
            {
                var property = serialized.FindProperty(name);
                if (property == null || property.objectReferenceValue == null)
                { Debug.LogError("FAIL: missing reference " + name, prefab); errors++; }
            }
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                if (missing > 0) { Debug.LogError("FAIL: missing script on " + child.name, prefab); errors += missing; }
            }
            if (manager.sequence != null)
            {
                var data = manager.sequence;
                if (data.startLine < 0 || data.startLine >= data.lines.Count)
                { Debug.LogError("FAIL: invalid Start Line.", data); errors++; }
                for (int i = 0; i < data.lines.Count; i++)
                {
                    var line = data.lines[i];
                    if (line == null) { Debug.LogError("FAIL: null line " + i, data); errors++; continue; }
                    if (!ValidTarget(line.nextLine, data.lines.Count))
                    { Debug.LogError("FAIL: line " + i + " has invalid Next Line.", data); errors++; }
                    foreach (var choice in line.choices)
                        if (choice == null || !ValidTarget(choice.nextLine, data.lines.Count))
                        { Debug.LogError("FAIL: invalid choice on line " + i, data); errors++; }
                }
            }
            if (prefab.GetComponent<Canvas>() == null || prefab.GetComponent<CanvasScaler>() == null || prefab.GetComponent<GraphicRaycaster>() == null)
            { Debug.LogError("FAIL: Canvas configuration is incomplete.", prefab); errors++; }
            if (errors == 0) Debug.Log("PASS: Dark Dialogue prefab, UI references, scripts, and demo branches are valid. Run Demo Scene to verify playback.", prefab);
        }

        static bool ValidTarget(int value, int count) { return value >= -2 && value < count; }
    }
}
