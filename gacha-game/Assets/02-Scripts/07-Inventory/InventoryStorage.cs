using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class InventoryItemData
{
    public string itemId;
    public int star;
    public int amount;
}

[Serializable]
public class CharacterLevelData
{
    public int characterId;
    public int level;
    public int experience;
}

[Serializable]
public class InventorySaveData
{
    public List<InventoryItemData> items = new List<InventoryItemData>();
    public List<CharacterLevelData> characterLevels = new List<CharacterLevelData>();
}

public static class InventoryStorage
{
    public const int CharacterCount = 5;
    public const int ExperiencePerLevel = 100;

    // 1성, 2성, 3성 재료 한 개당 경험치
    private static readonly int[] MaterialExperience = { 10, 25, 50 };

    private static InventorySaveData data;
    private static bool loaded;

    public static event Action OnInventoryChanged;

    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, "InventoryData.json");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        data = null;
        loaded = false;
        OnInventoryChanged = null;
    }

    public static void Load()
    {
        if (loaded)
            return;

        try
        {
            string path = SavePath;
            InventorySaveData saved = File.Exists(path)
                ? JsonUtility.FromJson<InventorySaveData>(File.ReadAllText(path))
                : new InventorySaveData();

            if (saved == null || saved.items == null)
            {
                Debug.LogError("인벤토리 JSON 형식 오류");
                return;
            }

            if (saved.characterLevels == null)
                saved.characterLevels = new List<CharacterLevelData>();

            data = saved;
            loaded = true;
            CharacterLevelJson.Sync(data.characterLevels);
        }
        catch (Exception exception)
        {
            Debug.LogError("인벤토리 로드 실패: " + exception.Message);
        }
    }

    public static bool AddStars(int[] stars)
    {
        if (stars == null || stars.Length == 0)
            return false;

        int[] amounts = new int[stars.Length];

        for (int i = 0; i < amounts.Length; i++)
            amounts[i] = 1;

        return AddRewards(stars, amounts);
    }

    public static bool AddRewards(int[] stars, int[] amounts)
    {
        if (stars == null || amounts == null ||
            stars.Length == 0 || stars.Length != amounts.Length)
        {
            return false;
        }

        long[] totals = new long[MaterialExperience.Length];

        for (int i = 0; i < stars.Length; i++)
        {
            int star = stars[i];

            if (star < 1 || star > totals.Length || amounts[i] <= 0)
                return false;

            totals[star - 1] += amounts[i];
        }

        Load();

        if (!loaded)
            return false;

        InventorySaveData updated = CopyData();

        for (int i = 0; i < totals.Length; i++)
        {
            if (totals[i] == 0)
                continue;

            int star = i + 1;
            InventoryItemData item = FindStarItem(updated, star);
            long newAmount = (item != null ? item.amount : 0) + totals[i];

            if (newAmount > int.MaxValue)
                return false;

            if (item == null)
            {
                item = new InventoryItemData
                {
                    itemId = "star_" + star,
                    star = star
                };

                updated.items.Add(item);
            }

            item.amount = (int)newAmount;
        }

        return Save(updated);
    }

    public static List<InventoryItemData> GetItems()
    {
        Load();

        List<InventoryItemData> result = new List<InventoryItemData>();

        if (!loaded)
            return result;

        foreach (InventoryItemData item in data.items)
        {
            if (item == null)
                continue;

            result.Add(new InventoryItemData
            {
                itemId = item.itemId,
                star = item.star,
                amount = item.amount
            });
        }

        return result;
    }

    public static int GetItemCount(int star)
    {
        Load();

        if (!loaded)
            return 0;

        InventoryItemData item = FindStarItem(data, star);
        return item != null ? item.amount : 0;
    }

    public static bool ClearInventory()
    {
        Load();

        if (!loaded)
            return false;

        InventorySaveData updated = CopyData();
        updated.items.Clear();

        return Save(updated);
    }

    public static int GetCharacterLevel(int characterId)
    {
        if (characterId < 1 || characterId > CharacterCount)
            return 0;

        Load();

        if (!loaded)
            return 1;

        CharacterLevelData character = FindCharacter(data, characterId);
        return character != null ? Mathf.Max(1, character.level) : 1;
    }

    public static int GetCharacterExperience(int characterId)
    {
        if (characterId < 1 || characterId > CharacterCount)
            return 0;

        Load();

        if (!loaded)
            return 0;

        CharacterLevelData character = FindCharacter(data, characterId);

        if (character == null || character.level == int.MaxValue)
            return 0;

        return Mathf.Clamp(character.experience, 0, ExperiencePerLevel - 1);
    }

    public static void GetMaterialUpgradePreview(
        int characterId,
        int[] amounts,
        out long gainedExperience,
        out int nextLevel,
        out int nextExperience)
    {
        gainedExperience = 0;
        nextLevel = GetCharacterLevel(characterId);
        nextExperience = GetCharacterExperience(characterId);

        if (nextLevel == 0 || nextLevel == int.MaxValue ||
            amounts == null || amounts.Length != MaterialExperience.Length)
        {
            return;
        }

        long selectedExperience = 0;

        for (int i = 0; i < amounts.Length; i++)
        {
            if (amounts[i] < 0)
                return;

            selectedExperience += (long)amounts[i] * MaterialExperience[i];
        }

        long remainingExperience =
            (long)(int.MaxValue - nextLevel) * ExperiencePerLevel - nextExperience;

        gainedExperience = Math.Min(selectedExperience, remainingExperience);

        long totalExperience = nextExperience + gainedExperience;
        nextLevel += (int)(totalExperience / ExperiencePerLevel);
        nextExperience = (int)(totalExperience % ExperiencePerLevel);
    }

    public static bool TryUpgradeWithMaterials(int characterId, int[] amounts)
    {
        GetMaterialUpgradePreview(
            characterId,
            amounts,
            out long gainedExperience,
            out int nextLevel,
            out int nextExperience
        );

        if (!loaded || gainedExperience <= 0)
            return false;

        InventorySaveData updated = CopyData();

        if (!TrySpendMaterials(updated, amounts))
            return false;

        CharacterLevelData character = GetOrAddCharacter(updated, characterId);
        character.level = nextLevel;
        character.experience = nextExperience;

        return Save(updated);
    }

    // 기존 고정 비용 강화 코드에서 사용하던 함수
    public static bool TryUpgradeCharacter(
        int characterId,
        int oneStarCost,
        int twoStarCost,
        int threeStarCost)
    {
        if (characterId < 1 || characterId > CharacterCount ||
            oneStarCost <= 0 || twoStarCost <= 0 || threeStarCost <= 0)
        {
            return false;
        }

        int currentLevel = GetCharacterLevel(characterId);

        if (!loaded || currentLevel == int.MaxValue)
            return false;

        InventorySaveData updated = CopyData();
        int[] costs = { oneStarCost, twoStarCost, threeStarCost };

        if (!TrySpendMaterials(updated, costs))
            return false;

        CharacterLevelData character = GetOrAddCharacter(updated, characterId);
        character.level = currentLevel + 1;

        if (character.level == int.MaxValue)
            character.experience = 0;

        return Save(updated);
    }

    private static bool TrySpendMaterials(InventorySaveData updated, int[] amounts)
    {
        for (int i = 0; i < amounts.Length; i++)
        {
            int amount = amounts[i];

            if (amount == 0)
                continue;

            InventoryItemData item = FindStarItem(updated, i + 1);

            if (amount < 0 || item == null || item.amount < amount)
                return false;

            item.amount -= amount;
        }

        return true;
    }

    private static InventoryItemData FindStarItem(InventorySaveData source, int star)
    {
        string itemId = "star_" + star;
        return source.items.Find(item => item != null && item.itemId == itemId);
    }

    private static CharacterLevelData FindCharacter(
        InventorySaveData source,
        int characterId)
    {
        return source.characterLevels.Find(
            entry => entry != null && entry.characterId == characterId
        );
    }

    private static CharacterLevelData GetOrAddCharacter(
        InventorySaveData source,
        int characterId)
    {
        CharacterLevelData character = FindCharacter(source, characterId);

        if (character == null)
        {
            character = new CharacterLevelData
            {
                characterId = characterId,
                level = 1
            };

            source.characterLevels.Add(character);
        }

        return character;
    }

    private static InventorySaveData CopyData()
    {
        return JsonUtility.FromJson<InventorySaveData>(JsonUtility.ToJson(data));
    }

    private static bool Save(InventorySaveData updated)
    {
        try
        {
            string path = SavePath;
            string tempPath = path + ".tmp";

            File.WriteAllText(tempPath, JsonUtility.ToJson(updated, true));

            if (File.Exists(path))
                File.Replace(tempPath, path, null);
            else
                File.Move(tempPath, path);
        }
        catch (Exception exception)
        {
            Debug.LogError("인벤토리 저장 실패: " + exception.Message);
            return false;
        }

        data = updated;
        CharacterLevelJson.Sync(data.characterLevels);

        Action handlers = OnInventoryChanged;

        if (handlers != null)
        {
            foreach (Action handler in handlers.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        return true;
    }
}