using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PartyFormationSaveData
{
    // [0..2] 전투 멤버, [3..4] 교대 멤버
    public string[] memberIds = new string[5];
}

public static class PartyFormationStorage
{
    private const string SaveKey = "PartyFormation_v1";
    public const int ActiveCount = 3;
    public const int ReserveCount = 2;
    public const int TotalCount = ActiveCount + ReserveCount;

    public static bool Save(CharacterRosterSO roster, BattleUnit[] members)
    {
        if (!IsValidMembers(roster, members))
        {
            Debug.LogWarning("[편성] 보유 중인 서로 다른 아군 캐릭터 5명을 배치해야 저장할 수 있습니다.");
            return false;
        }

        var saveData = new PartyFormationSaveData();
        for (int i = 0; i < TotalCount; i++)
            saveData.memberIds[i] = members[i].characterId;

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saveData));
        PlayerPrefs.Save();
        return true;
    }

    public static bool TryLoad(CharacterRosterSO roster, out BattleUnit[] members)
    {
        members = new BattleUnit[TotalCount];
        if (roster == null || !PlayerPrefs.HasKey(SaveKey)) return false;

        PartyFormationSaveData saveData;
        try
        {
            saveData = JsonUtility.FromJson<PartyFormationSaveData>(
                PlayerPrefs.GetString(SaveKey));
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[편성] 저장 데이터 읽기 실패: " + ex.Message);
            return false;
        }

        if (saveData == null || saveData.memberIds == null ||
            saveData.memberIds.Length != TotalCount) return false;

        for (int i = 0; i < TotalCount; i++)
            members[i] = roster.FindById(saveData.memberIds[i]);

        if (!IsValidMembers(roster, members))
        {
            Debug.LogWarning("[편성] 저장된 캐릭터를 찾지 못하거나 편성이 유효하지 않습니다.");
            return false;
        }
        return true;
    }

    public static bool IsValidMembers(CharacterRosterSO roster, BattleUnit[] members)
    {
        if (roster == null || members == null || members.Length != TotalCount)
            return false;

        var usedIds = new HashSet<string>();
        foreach (BattleUnit unit in members)
        {
            if (unit == null || unit.team != Team.Ally ||
                string.IsNullOrWhiteSpace(unit.characterId) ||
                roster.FindById(unit.characterId) != unit ||
                !CurrencyManager.OwnsCharacter(unit.characterId) ||
                !usedIds.Add(unit.characterId))
                return false;
        }
        return true;
    }
}
