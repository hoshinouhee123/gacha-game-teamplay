using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class CharacterLevelJson
{
    public static void Sync(IReadOnlyList<CharacterLevelData> savedLevels)
    {
        string directory = Path.Combine(
            Application.persistentDataPath,
            "CharacterLevels"
        );

        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("캐릭터 레벨 폴더 생성 실패: " + exception.Message);
            return;
        }

        for (int characterId = 1;
             characterId <= InventoryStorage.CharacterCount;
             characterId++)
        {
            CharacterLevelData snapshot = new CharacterLevelData
            {
                characterId = characterId,
                level = 1,
                experience = 0
            };

            foreach (CharacterLevelData saved in savedLevels)
            {
                if (saved == null || saved.characterId != characterId)
                    continue;

                snapshot.level = Mathf.Max(1, saved.level);
                snapshot.experience = snapshot.level == int.MaxValue
                    ? 0
                    : Mathf.Clamp(
                        saved.experience,
                        0,
                        InventoryStorage.ExperiencePerLevel - 1
                    );

                break;
            }

            string path = Path.Combine(directory, $"Character_{characterId}.json");
            WriteIfChanged(path, snapshot);
        }
    }

    private static void WriteIfChanged(string path, CharacterLevelData snapshot)
    {
        try
        {
            string json = JsonUtility.ToJson(snapshot, true);
            bool exists = File.Exists(path);

            if (exists && File.ReadAllText(path) == json)
                return;

            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);

            if (exists)
                File.Replace(tempPath, path, null);
            else
                File.Move(tempPath, path);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"{Path.GetFileName(path)} 갱신 실패: {exception.Message}"
            );
        }
    }
}