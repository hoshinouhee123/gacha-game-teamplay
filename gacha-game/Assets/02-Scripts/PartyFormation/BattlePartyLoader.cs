using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TurnManager))]
public class BattlePartyLoader : MonoBehaviour
{
    [SerializeField] private CharacterRosterSO roster;

    [Tooltip("에디터 테스트 용도. 편성 없이 씬에 설정한 기본 아군으로 전투합니다.")]
    [SerializeField] private bool allowSceneDefaultTeamForTesting = false;

    // TurnManager.Start()에서 직접 호출하여 실행 순서를 보장
    public bool ApplyTo(TurnManager manager)
    {
        if (roster == null || !CurrencyManager.TryGrantStarterCharacters(roster))
        {
            Debug.LogError("[편성] BattlePartyLoader의 CharacterRosterSO를 확인하세요.");
            return false;
        }

        if (!PartyFormationStorage.TryLoad(roster, out BattleUnit[] members))
        {
            Debug.LogWarning("[편성] 보유 캐릭터 5명의 저장된 편성이 없습니다.");
            return allowSceneDefaultTeamForTesting;
        }

        var activeSlots = new List<Unit>();
        var reserveSlots = new List<Unit>();

        foreach (Unit unit in manager.fieldUnits)
        {
            if (unit != null && unit.Team == Team.Ally)
                activeSlots.Add(unit);
        }

        foreach (Unit unit in manager.reserveUnits)
        {
            if (unit != null && unit.Team == Team.Ally)
                reserveSlots.Add(unit);
        }

        if (activeSlots.Count != PartyFormationStorage.ActiveCount ||
            reserveSlots.Count != PartyFormationStorage.ReserveCount)
        {
            Debug.LogError("[편성] TurnManager에 아군 fieldUnits 3개, reserveUnits 2개가 필요합니다.");
            return false;
        }

        for (int i = 0; i < PartyFormationStorage.ActiveCount; i++)
            activeSlots[i].Initialize(members[i]);

        for (int i = 0; i < PartyFormationStorage.ReserveCount; i++)
            reserveSlots[i].Initialize(members[i + PartyFormationStorage.ActiveCount]);

        Debug.Log("[편성] 저장된 전투 3명 + 교대 2명을 적용했습니다.");
        return true;
    }
}
