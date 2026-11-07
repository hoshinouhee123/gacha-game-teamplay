
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
public class InventorySaveData
{
    public List<InventoryItemData> items =
        new List<InventoryItemData>();
}

public static class InventoryStorage
{
    private static InventorySaveData data;
    private static bool loaded = false;

    public static event Action OnInventoryChanged;

    private static string SavePath =>
        Path.Combine(
            Application.persistentDataPath,
            "InventoryData.json"
        );

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
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

        loaded = true;
        data = new InventorySaveData();

        if (!File.Exists(SavePath))
            return;

        try
        {
            string json = File.ReadAllText(SavePath);

            InventorySaveData saved =
                JsonUtility.FromJson<InventorySaveData>(json);

            if (saved != null && saved.items != null)
                data = saved;
        }
        catch (Exception e)
        {
            Debug.LogError(
                "인벤토리 로드 실패: " + e.Message
            );
        }
    }

    public static bool AddStars(int[] stars)
    {
        Load();

        if (stars == null || stars.Length == 0)
            return false;

        // 저장 실패 시 복원할 데이터
        string backup = JsonUtility.ToJson(data);

        foreach (int star in stars)
        {
            if (star < 1 || star > 3)
                return RestoreAndFail(backup);

            string id = "star_" + star;

            InventoryItemData item =
                data.items.Find(x => x.itemId == id);

            if (item == null)
            {
                item = new InventoryItemData
                {
                    itemId = id,
                    star = star,
                    amount = 0
                };

                data.items.Add(item);
            }

            if (item.amount == int.MaxValue)
                return RestoreAndFail(backup);

            item.amount++;
        }

        if (!Save())
            return RestoreAndFail(backup);

        OnInventoryChanged?.Invoke();

        return true;
    }

    private static bool RestoreAndFail(string backup)
    {
        data = JsonUtility.FromJson<InventorySaveData>(
            backup
        );

        return false;
    }

    public static List<InventoryItemData> GetItems()
    {
        Load();

        List<InventoryItemData> result =
            new List<InventoryItemData>();

        foreach (InventoryItemData item in data.items)
        {
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

        InventoryItemData item = data.items.Find(
            x => x.itemId == "star_" + star
        );

        return item != null ? item.amount : 0;
    }

    private static bool Save()
    {
        try
        {
            string json = JsonUtility.ToJson(
                data,
                true
            );

            File.WriteAllText(SavePath, json);

            Debug.Log(
                "인벤토리 JSON 저장 완료: " + SavePath
            );

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(
                "인벤토리 저장 실패: " + e.Message
            );

            return false;
        }
    }
}
