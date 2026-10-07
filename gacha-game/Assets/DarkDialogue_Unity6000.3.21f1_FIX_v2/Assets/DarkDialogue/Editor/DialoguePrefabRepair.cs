using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDialogue.Editor
{
    public static class DialoguePrefabRepair
    {
        const string RootPath = "Assets/DarkDialogue";
        const string PrefabPath = RootPath + "/Prefabs/DarkDialogue.prefab";
        const string DemoPath = RootPath + "/Demo/ExampleDialogue.asset";
        const string ManagerPath = RootPath + "/Runtime/DialogueManager.cs";
        const string SequencePath = RootPath + "/Runtime/DialogueSequence.cs";

        [MenuItem("Tools/Dark Dialogue/Repair Script Links")]
        public static void Repair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            { Debug.LogWarning("Dark Dialogue: stop Play mode and wait for compilation, then run Repair Script Links."); return; }
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(ManagerPath);
            if (script == null || script.GetClass() != typeof(DialogueManager))
            { Debug.LogError("Dark Dialogue: DialogueManager.cs must exist and compile first. Copy the supplied Runtime folder with .meta files, then fix any Console compilation errors."); return; }
            if (!File.Exists(PrefabPath))
            { Debug.LogError("Dark Dialogue: the supplied prefab is missing at " + PrefabPath); return; }

            GameObject selectedRoot = FindDialogueRoot(Selection.activeGameObject);
            var remap = CurrentGuidMap();
            string backup = Path.Combine("Library", "DarkDialogueRepair", DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
            int files = 0;
            // Repair only known package GUIDs. Other assets/fields are preserved.
            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !File.Exists(path)) continue;
                bool dialogueAsset = path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase);
                bool ownPrefabOrScene = path.StartsWith(RootPath + "/", StringComparison.Ordinal) &&
                    (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase));
                if (!dialogueAsset && !ownPrefabOrScene) continue;
                string original = File.ReadAllText(path);
                if (!original.StartsWith("%YAML", StringComparison.Ordinal)) continue;
                string updated = Regex.Replace(original, @"\bguid:\s*([0-9a-fA-F]{32})", match =>
                {
                    string oldGuid = match.Groups[1].Value.ToLowerInvariant();
                    return remap.TryGetValue(oldGuid, out string actualGuid) ? "guid: " + actualGuid : match.Value;
                });
                if (updated == original) continue;
                BackupFile(path, backup);
                File.WriteAllText(path, updated, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                files++;
            }
            AssetDatabase.ImportAsset(DemoPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);

            BackupFile(PrefabPath, backup);
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            int prefabErrors;
            try
            {
                prefabErrors = RepairRoot(contents, false);
                // Save through Unity so MonoScript references use its current GUIDs.
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }

            int sceneErrors = 0;
            if (selectedRoot != null && !EditorUtility.IsPersistent(selectedRoot))
            {
                Undo.RegisterFullObjectHierarchyUndo(selectedRoot, "Repair Dark Dialogue");
                sceneErrors = RepairRoot(selectedRoot, true);
                EditorSceneManager.MarkSceneDirty(selectedRoot.scene);
                var manager = selectedRoot.GetComponent<DialogueManager>();
                if (PrefabUtility.IsPartOfPrefabInstance(selectedRoot) && manager != null)
                    PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
                Selection.activeGameObject = selectedRoot;
            }
            AssetDatabase.SaveAssets();
            if (prefabErrors + sceneErrors == 0)
                Debug.Log("Dark Dialogue: script links and UI references repaired. " + files +
                    " serialized files relinked. Save your scene, then press Play. Backup: " + backup);
            else Debug.LogError("Dark Dialogue: repair found " + (prefabErrors + sceneErrors) + " missing objects. See the named errors above.");
        }

        static GameObject FindDialogueRoot(GameObject selected)
        {
            if (selected == null) return null;
            Transform current = selected.transform;
            while (current != null)
            {
                if (current.Find("Presentation/DialogueBox") != null && current.Find("Presentation/Stage") != null)
                    return current.gameObject;
                current = current.parent;
            }
            return null;
        }

        static Dictionary<string, string> CurrentGuidMap()
        {
            var result = new Dictionary<string, string>();
            string[] paths = { ManagerPath, SequencePath, PrefabPath, DemoPath,
                RootPath + "/Art/Station.png", RootPath + "/Art/PortraitLeft.png", RootPath + "/Art/PortraitRight.png" };
            foreach (string path in paths)
            {
                string actual = AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(actual)) result[OriginalGuid(path)] = actual;
            }
            var ui = new Dictionary<string, string>
            {
                { "UnityEngine.UI.Image", "fe87c0e1cc204ed48ad3b37840f39efc" },
                { "UnityEngine.UI.Text", "5f7201a12d95ffc409449d95f23cf332" },
                { "UnityEngine.UI.Button", "4e29b1a8efbd4b44bb3f3716e73f07ff" },
                { "UnityEngine.UI.CanvasScaler", "0cd44c1031e13a943bb63640046fad76" },
                { "UnityEngine.UI.GraphicRaycaster", "dc42784cf147c0c48a680349fa168899" }
            };
            foreach (MonoScript script in MonoImporter.GetAllRuntimeMonoScripts())
            {
                Type type = script.GetClass();
                if (type == null || !ui.TryGetValue(type.FullName, out string old)) continue;
                string actual = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(script));
                if (!string.IsNullOrEmpty(actual)) result[old] = actual;
            }
            return result;
        }

        static string OriginalGuid(string path)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes("DarkDialogue.v1/" + path));
                var output = new StringBuilder(32);
                foreach (byte value in hash) output.Append(value.ToString("x2"));
                return output.ToString();
            }
        }

        static void BackupFile(string path, string directory)
        {
            string target = Path.Combine(directory, path);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            // Preserve the original snapshot if a file is backed up twice.
            if (!File.Exists(target)) File.Copy(path, target, false);
        }

        static int RepairRoot(GameObject root, bool sceneObject)
        {
            if (FindDialogueRoot(root) != root)
            { Debug.LogError("Dark Dialogue: expected Presentation/Stage and Presentation/DialogueBox.", root); return 1; }
            // Scope is the supplied dialogue root, with script lookup already verified.
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = sceneObject ? Undo.AddComponent<CanvasScaler>(root) : root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            if (root.GetComponent<GraphicRaycaster>() == null)
            {
                if (sceneObject) Undo.AddComponent<GraphicRaycaster>(root);
                else root.AddComponent<GraphicRaycaster>();
            }
            var manager = root.GetComponent<DialogueManager>();
            if (manager == null)
                manager = sceneObject ? Undo.AddComponent<DialogueManager>(root) : root.AddComponent<DialogueManager>();
            var serialized = new SerializedObject(manager);
            int errors = 0;
            Bind(serialized, "presentation", ComponentAt<CanvasGroup>(root, "Presentation"), ref errors);
            Bind(serialized, "speakerName", ComponentAt<Text>(root, "Presentation/DialogueBox/NamePlate/SpeakerName"), ref errors);
            Bind(serialized, "dialogueText", ComponentAt<Text>(root, "Presentation/DialogueBox/DialogueText"), ref errors);
            Bind(serialized, "progressText", ComponentAt<Text>(root, "Presentation/DialogueBox/Progress"), ref errors);
            Bind(serialized, "nextIcon", ObjectAt(root, "Presentation/DialogueBox/NextIcon"), ref errors);
            Bind(serialized, "advanceButton", ComponentAt<Button>(root, "Presentation/AdvanceArea"), ref errors);
            Bind(serialized, "autoButton", ComponentAt<Button>(root, "Presentation/Controls/AutoButton"), ref errors);
            Bind(serialized, "skipButton", ComponentAt<Button>(root, "Presentation/Controls/SkipButton"), ref errors);
            Bind(serialized, "logButton", ComponentAt<Button>(root, "Presentation/Controls/LogButton"), ref errors);
            Bind(serialized, "autoLabel", ComponentAt<Text>(root, "Presentation/Controls/AutoButton/Label"), ref errors);
            Bind(serialized, "skipLabel", ComponentAt<Text>(root, "Presentation/Controls/SkipButton/Label"), ref errors);
            Bind(serialized, "background", ComponentAt<Image>(root, "Presentation/Stage/BackgroundImage"), ref errors);
            Bind(serialized, "leftPortrait", ComponentAt<Image>(root, "Presentation/Stage/CharacterLeft"), ref errors);
            Bind(serialized, "rightPortrait", ComponentAt<Image>(root, "Presentation/Stage/CharacterRight"), ref errors);
            Bind(serialized, "stage", ComponentAt<RectTransform>(root, "Presentation/Stage"), ref errors);
            Bind(serialized, "choicePanel", ComponentAt<RectTransform>(root, "Presentation/ChoiceViewport/ChoicePanel"), ref errors);
            Bind(serialized, "choiceTemplate", ComponentAt<Button>(root, "Presentation/ChoiceViewport/ChoicePanel/ChoiceTemplate"), ref errors);
            Bind(serialized, "logPanel", ObjectAt(root, "Presentation/LogPanel"), ref errors);
            Bind(serialized, "logText", ComponentAt<Text>(root, "Presentation/LogPanel/LogViewport/LogText"), ref errors);
            Bind(serialized, "logPageText", ComponentAt<Text>(root, "Presentation/LogPanel/LogPage"), ref errors);
            Bind(serialized, "closeLogButton", ComponentAt<Button>(root, "Presentation/LogPanel/CloseLogButton"), ref errors);
            Bind(serialized, "previousLogButton", ComponentAt<Button>(root, "Presentation/LogPanel/PreviousLogButton"), ref errors);
            Bind(serialized, "nextLogButton", ComponentAt<Button>(root, "Presentation/LogPanel/NextLogButton"), ref errors);
            Bind(serialized, "musicSource", ComponentAt<AudioSource>(root, "MusicSource"), ref errors);
            Bind(serialized, "effectSource", ComponentAt<AudioSource>(root, "EffectSource"), ref errors);
            Bind(serialized, "sequence", AssetDatabase.LoadAssetAtPath<DialogueSequence>(DemoPath), ref errors);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            RestoreDefaultSprite(manager.background, RootPath + "/Art/Station.png");
            RestoreDefaultSprite(manager.leftPortrait, RootPath + "/Art/PortraitLeft.png");
            RestoreDefaultSprite(manager.rightPortrait, RootPath + "/Art/PortraitRight.png");
            EditorUtility.SetDirty(manager);
            return errors;
        }

        static void Bind(SerializedObject target, string field, UnityEngine.Object value, ref int errors)
        {
            SerializedProperty property = target.FindProperty(field);
            if (property != null && property.objectReferenceValue != null) return;
            if (property == null || value == null)
            { errors++; Debug.LogError("Dark Dialogue: cannot restore " + field + ". Check the supplied hierarchy/assets.", target.targetObject); return; }
            property.objectReferenceValue = value;
        }

        static T ComponentAt<T>(GameObject root, string path) where T : Component
        {
            Transform child = root.transform.Find(path);
            return child == null ? null : child.GetComponent<T>();
        }

        static GameObject ObjectAt(GameObject root, string path)
        {
            Transform child = root.transform.Find(path);
            return child == null ? null : child.gameObject;
        }

        static void RestoreDefaultSprite(Image image, string path)
        {
            if (image != null && image.sprite == null)
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
