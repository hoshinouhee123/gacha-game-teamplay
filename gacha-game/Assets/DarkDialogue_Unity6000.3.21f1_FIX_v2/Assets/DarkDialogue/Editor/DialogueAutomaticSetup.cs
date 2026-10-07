using System;
using UnityEditor;
using UnityEngine;

namespace DarkDialogue.Editor
{
    // Asset loading is deferred until imports and domain reload have completed.
    public sealed class DialogueAutomaticSetup : AssetPostprocessor
    {
        const string RootPath = "Assets/DarkDialogue/";
        const string PrefabPath = RootPath + "Prefabs/DarkDialogue.prefab";
        const string ManagerPath = RootPath + "Runtime/DialogueManager.cs";
        static bool scheduled;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            bool relevant = didDomainReload;
            foreach (string path in importedAssets)
                if (path.StartsWith(RootPath, StringComparison.Ordinal)) { relevant = true; break; }
            if (relevant) Schedule();
        }

        static void Schedule()
        {
            if (scheduled) return;
            scheduled = true;
            EditorApplication.delayCall += CheckConnections;
        }

        static void CheckConnections()
        {
            scheduled = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { Schedule(); return; }
            string key = "DarkDialogue.v4.AutoRepair." + Application.dataPath;
            if (SessionState.GetBool(key, false)) return;
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(ManagerPath);
            if (script == null || script.GetClass() != typeof(DialogueManager)) return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) return;
            // Mark before any importing/saving to prevent recursive repairs.
            SessionState.SetBool(key, true);
            if (!NeedsRepair(prefab)) return;
            try { DialoguePrefabRepair.Repair(); }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("Dark Dialogue: automatic repair could not finish. After fixing Console errors, use Tools > Dark Dialogue > Repair Script Links.");
            }
        }

        static bool NeedsRepair(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0) return true;
            var manager = root.GetComponent<DialogueManager>();
            if (manager == null) return true;
            var serialized = new SerializedObject(manager);
            string[] required = { "sequence", "presentation", "speakerName", "dialogueText", "progressText",
                "nextIcon", "advanceButton", "autoButton", "skipButton", "logButton", "autoLabel", "skipLabel",
                "background", "leftPortrait", "rightPortrait", "stage", "choicePanel", "choiceTemplate",
                "logPanel", "logText", "logPageText", "closeLogButton", "previousLogButton", "nextLogButton",
                "musicSource", "effectSource" };
            foreach (string field in required)
            {
                SerializedProperty property = serialized.FindProperty(field);
                if (property == null || property.objectReferenceValue == null) return true;
            }
            return false;
        }
    }
}
