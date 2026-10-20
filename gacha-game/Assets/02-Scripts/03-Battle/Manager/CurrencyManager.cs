using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 1. JSON 파일로 저장될 데이터 클래스 (나중에 가챠 캐릭터 목록 등도 여기에 추가하면 됨)
[System.Serializable]
public class PlayerData
{
    public int renown = 0; // 명성 재화
    // public List<string> ownedCharacters = new List<string>(); // <- 추후 가챠 캐릭터 저장 확장용

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
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[JSON 로드 실패] 오류: {e.Message}. 새 데이터를 생성합니다.");
                cachedData = new PlayerData();
            }
        }
        else
        {
            // 파일이 없으면 새 데이터 생성
            cachedData = new PlayerData();
        }
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
        cachedData = new PlayerData();
        SaveToFile();
        Debug.Log("<color=yellow>[초기화] JSON 파일의 명성이 0으로 초기화되었습니다.</color>");
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