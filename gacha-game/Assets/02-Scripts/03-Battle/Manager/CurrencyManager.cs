using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 1. JSON 파일로 저장될 데이터 클래스 (나중에 가챠 캐릭터 목록 등도 여기에 추가하면 됨)
[System.Serializable]
public class PlayerData
{
    public int renown = 0; // 명성 재화
    // 보유 캐릭터 (가챠 획득). SO 자체가 아닌 고유 ID만 저장합니다.
    public List<string> ownedCharacterIds = new List<string>();

    // 무료 시작 캐릭터 지급 여부. 기존 저장 파일에는 없으므로 false로 읽힙니다.
    public bool starterCharactersClaimed = false;

    // 스테이지별 기록 리스트
    public List<StageRecord> stageRecords = new List<StageRecord>();
}

// 2. 스테이지별 기록 데이터 클래스
[System.Serializable]
public class StageRecord
{
    public int stageId;          // 스테이지 번호 (예: 1, 2, 3...)
    public int bestStarPower;     // 기록된 최고 스타성 (0~120%)
    public bool isCleared;        // 클리어 여부
}



public static class CurrencyManager
{
    // PC/모바일 어디서든 쓰기 권한이 보장되는 안전한 저장 경로
    private static string SavePath => Path.Combine(Application.persistentDataPath, "SaveData.json");

    private static PlayerData cachedData = null;
    private static bool saveLoadFailed = false;

    // 데이터 로드 (메모리에 없으면 파일에서 읽어옴)
    private static void EnsureLoaded()
    {
        if (cachedData != null) return;

        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                cachedData = JsonUtility.FromJson<PlayerData>(json);
                if (cachedData == null)
                    throw new System.Exception("세이브 데이터가 비어 있습니다.");
            }
            catch (System.Exception e)
            {
                // 손상된 파일을 빈 데이터로 덮어쓰지 않도록 가챠 및 초기 지급을 중단합니다.
                saveLoadFailed = true;
                Debug.LogError($"[JSON 로드 실패] 오류: {e.Message}. SaveData.json을 백업/복구해 주세요.");
                cachedData = new PlayerData();
            }
        }
        else
        {
            // 파일이 없으면 새 데이터 생성
            cachedData = new PlayerData();
        }

        // 기존 버전 JSON에는 아래 신규 필드가 없을 수 있습니다.
        if (cachedData.ownedCharacterIds == null)
            cachedData.ownedCharacterIds = new List<string>();
        if (cachedData.stageRecords == null)
            cachedData.stageRecords = new List<StageRecord>();
    }

    // 파일로 영구 저장
    private static void SaveToFile()
    {
        if (cachedData == null) return;

        // true를 넣으면 줄바꿈/들여쓰기 적용되어 메모장으로 보기 편해짐
        string json = JsonUtility.ToJson(cachedData, true);
        File.WriteAllText(SavePath, json);
    }

    // ================= [ 외부 호출 함수들 ] =================

    // 현재 보유 명성 가져오기
    public static int GetRenown()
    {
        EnsureLoaded();
        return cachedData.renown;
    }

    // 명성 획득
    public static void AddRenown(int amount)
    {
        if (amount <= 0) return;

        EnsureLoaded();
        cachedData.renown += amount;
        SaveToFile();

        Debug.Log($"<color=#00FFFF>[JSON 저장 완료] 명성 +{amount}! (현재 보유 명성: {cachedData.renown})</color>");
        Debug.Log($"<color=grey>저장 경로: {SavePath}</color>");
    }

    // 명성 소모 (가챠 시 사용)
    public static bool SpendRenown(int amount)
    {
        EnsureLoaded();

        if (cachedData.renown >= amount)
        {
            cachedData.renown -= amount;
            SaveToFile();
            Debug.Log($"<color=#FF69B4>[JSON 저장 완료] 명성 -{amount} 사용 (남은 명성: {cachedData.renown})</color>");
            return true;
        }

        Debug.LogWarning("<color=red>[명성 부족] 명성이 부족합니다!</color>");
        return false;
    }

    // (테스트용) 데이터 리셋
    public static void ResetRenown()
    {
        EnsureLoaded();
        cachedData.renown = 0; // 캐릭터와 스테이지 기록은 유지합니다.
        SaveToFile();
        Debug.Log("<color=yellow>[초기화] 명성만 0으로 초기화되었습니다.</color>");
    }



    // ================= [ 캐릭터 가챠 / 보유 기능 ] =================

    public static bool OwnsCharacter(string characterId)
    {
        EnsureLoaded();
        return !string.IsNullOrWhiteSpace(characterId) &&
               cachedData.ownedCharacterIds.Contains(characterId);
    }

    public static int GetOwnedCharacterCount()
    {
        EnsureLoaded();
        return cachedData.ownedCharacterIds.Count;
    }

    /// <summary>
    /// 가챠 1회/10회 결과를 기존 명성, 스테이지 데이터와 동일한 JSON에 저장합니다.
    /// 실제 명성 차감액만 realCost로 전달하세요 (테스트 명성 차감액은 제외).
    /// 새 획득 여부는 같은 10연차 안에서 중복된 경우도 false입니다.
    /// </summary>
    public static bool TrySpendAndUnlockCharacters(
        int realCost, IList<string> drawnCharacterIds, out bool[] newlyOwned)
    {
        newlyOwned = null;
        EnsureLoaded();
        if (saveLoadFailed) return false;

        if (realCost < 0 || drawnCharacterIds == null || drawnCharacterIds.Count == 0 ||
            cachedData.renown < realCost)
            return false;

        foreach (string id in drawnCharacterIds)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogError("[가챠] 캐릭터 ID가 비어 있어 구매를 취소합니다.");
                return false;
            }
        }

        int previousRenown = cachedData.renown;
        var previousOwned = new List<string>(cachedData.ownedCharacterIds);
        var known = new HashSet<string>(previousOwned);
        var flags = new bool[drawnCharacterIds.Count];

        for (int i = 0; i < drawnCharacterIds.Count; i++)
        {
            string id = drawnCharacterIds[i];
            flags[i] = known.Add(id);
            if (flags[i]) cachedData.ownedCharacterIds.Add(id);
        }

        cachedData.renown -= realCost;
        try
        {
            SaveToFile();
        }
        catch (System.Exception ex)
        {
            cachedData.renown = previousRenown;
            cachedData.ownedCharacterIds = previousOwned;
            Debug.LogError("[가챠] 저장 실패로 구매를 취소했습니다: " + ex.Message);
            return false;
        }

        newlyOwned = flags;
        return true;
    }

    /// <summary>
    /// 최초 1회만 roster.starterCharacters를 무료 지급합니다.
    /// roster를 지정하기 전에 실행하면 초기화되지 않습니다.
    /// </summary>
    public static bool TryGrantStarterCharacters(CharacterRosterSO roster)
    {
        if (roster == null) return false;
        EnsureLoaded();
        if (saveLoadFailed) return false;
        if (cachedData.starterCharactersClaimed) return true;

        var previousOwned = new List<string>(cachedData.ownedCharacterIds);
        var known = new HashSet<string>(previousOwned);
        BattleUnit[] starters = roster.starterCharacters;

        if (starters != null)
        {
            foreach (BattleUnit character in starters)
            {
                if (character == null) continue;
                if (character.team != Team.Ally ||
                    string.IsNullOrWhiteSpace(character.characterId) ||
                    roster.FindById(character.characterId) != character)
                {
                    Debug.LogWarning("[가챠] 시작 캐릭터가 Roster에 없거나 ID가 유효하지 않습니다: " + character.name);
                    continue;
                }

                if (known.Add(character.characterId))
                    cachedData.ownedCharacterIds.Add(character.characterId);
            }
        }

        cachedData.starterCharactersClaimed = true;
        try
        {
            SaveToFile();
            return true;
        }
        catch (System.Exception ex)
        {
            cachedData.ownedCharacterIds = previousOwned;
            cachedData.starterCharactersClaimed = false;
            Debug.LogError("[가챠] 시작 캐릭터 저장 실패: " + ex.Message);
            return false;
        }
    }

    // 스테이지 클리어 시 스타성 기록 저장 (신기록 갱신)
    public static void SaveStageRecord(int stageId, int starPower)
    {
        EnsureLoaded();

        // 기존 기록이 있는지 확인
        StageRecord record = cachedData.stageRecords.Find(r => r.stageId == stageId);

        if (record == null)
        {
            // 첫 클리어
            record = new StageRecord { stageId = stageId, bestStarPower = starPower, isCleared = true };
            cachedData.stageRecords.Add(record);
            Debug.Log($"<color=#00FF7F>[JSON 저장] 스테이지 {stageId} 첫 클리어! 스타성: {starPower}%</color>");
        }
        else
        {
            // 신기록일 때만 스타성 갱신
            if (starPower > record.bestStarPower)
            {
                Debug.Log($"<color=#00FF7F>[JSON 신기록!] 스테이지 {stageId} 스타성 갱신: {record.bestStarPower}% > {starPower}%</color>");
                record.bestStarPower = starPower;
            }
            record.isCleared = true;
        }

        SaveToFile();
    }

    // 특정 스테이지의 최고 스타성 불러오기
    public static int GetBestStarPower(int stageId)
    {
        EnsureLoaded();
        StageRecord record = cachedData.stageRecords.Find(r => r.stageId == stageId);
        return record != null ? record.bestStarPower : 0;
    }
}