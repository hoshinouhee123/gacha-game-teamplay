using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TurnManager))]
public class BattlePartyLoader : MonoBehaviour
{
    [SerializeField] private CharacterRosterSO roster;

    // TurnManager.Start()에서 직접 호출하여 실행 순서를 보장
    public void ApplyTo(TurnManager manager)
    {
        if (!PartyFormationStorage.TryLoad(roster, out BattleUnit[] members))
        {
            Debug.Log("[편성] 저장된 유효한 편성이 없어서 씬의 기본 아군을 사용합니다.");
            return;
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
            return;
        }

        for (int i = 0; i < PartyFormationStorage.ActiveCount; i++)
            activeSlots[i].Initialize(members[i]);

        for (int i = 0; i < PartyFormationStorage.ReserveCount; i++)
            reserveSlots[i].Initialize(members[i + PartyFormationStorage.ActiveCount]);

        Debug.Log("[편성] 저장된 전투 3명 + 교대 2명을 적용했습니다.");
    }
}
