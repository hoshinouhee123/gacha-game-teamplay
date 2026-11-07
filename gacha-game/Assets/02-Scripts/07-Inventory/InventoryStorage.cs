
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

    // JSON 불러오기
    public static void Load()
    {
        if (loaded)
            return;

        data = new InventorySaveData();

        if (!File.Exists(SavePath))
        {
            loaded = true;
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);

            InventorySaveData saved =
                JsonUtility.FromJson<InventorySaveData>(json);

            if (saved == null || saved.items == null)
            {
                Debug.LogError("인벤토리 JSON 형식 오류");
                return;
            }

            data = saved;
            loaded = true;
        }
        catch (Exception e)
        {
            Debug.LogError(
                "인벤토리 로드 실패: " + e.Message
            );
        }
    }

    // 기존 AddStars 호출 호환
    public static bool AddStars(int[] stars)
    {
        if (stars == null || stars.Length == 0)
            return false;

        int[] amounts = new int[stars.Length];

        for (int i = 0; i < amounts.Length; i++)
        {
            amounts[i] = 1;
        }

        return AddRewards(stars, amounts);
    }

    // 등급과 지급 개수를 함께 저장
    public static bool AddRewards(
        int[] stars,
        int[] amounts)
    {
        Load();

        if (!loaded)
            return false;

        if (stars == null || amounts == null)
            return false;

        if (stars.Length == 0 ||
            stars.Length != amounts.Length)
            return false;

        // 지급량을 등급별로 합산
        long[] totals = new long[4];

        for (int i = 0; i < stars.Length; i++)
        {
            int star = stars[i];
            int amount = amounts[i];

            if (star < 1 || star > 3 || amount <= 0)
                return false;

            totals[star] += amount;
        }

        // 원래 데이터 백업
        string backup = JsonUtility.ToJson(data);

        for (int star = 1; star <= 3; star++)
        {
            if (totals[star] == 0)
                continue;

            string id = "star_" + star;

            InventoryItemData item =
                data.items.Find(x => x.itemId == id);

            long currentAmount =
                item != null ? item.amount : 0;

            long newAmount =
                currentAmount + totals[star];

            // int 최대값 초과 방지
            if (newAmount > int.MaxValue)
            {
                RestoreData(backup);
                return false;
            }

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

            item.amount = (int)newAmount;
        }

        // JSON 저장
        if (!Save())
        {
            RestoreData(backup);
            return false;
        }

        // 인벤토리 UI 갱신 알림
        OnInventoryChanged?.Invoke();

        return true;
    }

    // 데이터 복원
    private static void RestoreData(string backup)
    {
        data = JsonUtility.FromJson<InventorySaveData>(
            backup
        );
    }

    // 전체 아이템 불러오기
    public static List<InventoryItemData> GetItems()
    {
        Load();

        List<InventoryItemData> result =
            new List<InventoryItemData>();

        if (!loaded)
            return result;

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

    // 특정 등급 보유 수량
    public static int GetItemCount(int star)
    {
        Load();

        if (!loaded)
            return 0;

        InventoryItemData item =
            data.items.Find(
                x => x.itemId == "star_" + star
            );

        return item != null ? item.amount : 0;
    }

    // JSON 파일 저장

    private static bool Save()
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);

            string tempPath = SavePath + ".tmp";

            // 먼저 임시 파일에 저장
            File.WriteAllText(tempPath, json);

            // 기존 파일이 있다면 교체
            if (File.Exists(SavePath))
            {
                File.Replace(tempPath, SavePath, null);
            }
            else
            {
                File.Move(tempPath, SavePath);
            }

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

    // 테스트용 인벤토리 전체 초기화
    public static bool ClearInventory()
    {
        Load();

        if (!loaded)
        {
            Debug.LogError("인벤토리 로드 실패로 초기화 취소");
            return false;
        }

        // 기존 데이터 백업
        string backup = JsonUtility.ToJson(data);

        // 모든 아이템 삭제
        data.items.Clear();

        // JSON 저장
        if (!Save())
        {
            // 저장 실패 시 데이터 복원
            data = JsonUtility.FromJson<InventorySaveData>(backup);
            return false;
        }

        // 인벤토리 UI 즉시 갱신
        OnInventoryChanged?.Invoke();

        Debug.Log("인벤토리 전체 삭제 완료!");

        return true;
    }

}
