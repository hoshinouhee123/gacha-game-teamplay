using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpeedTurnTester : MonoBehaviour
{
    [Header("테스트할 유닛 SO들을 여기에 등록")]
    public List<BattleUnit> testUnits = new List<BattleUnit>();

    [Header("턴 넘어가는 딜레이 (초)")]
    public float turnDelay = 0.8f;

    private int round = 0;

    private void Start()
    {
        if (testUnits == null || testUnits.Count == 0)
        {
            Debug.LogError("테스트할 유닛 SO를 인스펙터에 하나 이상 등록해주세요!");
            return;
        }

        // 스피드 턴 테스트 루프 시작
        StartCoroutine(TestTurnRoutine());
    }

    private IEnumerator TestTurnRoutine()
    {
        // 3라운드 동안 순서가 어떻게 돌아가는지만 시뮬레이션
        while (round < 3)
        {
            round++;
            Debug.Log($"<color=cyan>================ [ ROUND {round} ] ================</color>");

            // 1. 스피드 내림차순(높은 순) 정렬 (동률이면 랜덤)
            var sortedQueue = testUnits
                .OrderByDescending(u => u.speed)
                .ThenBy(u => Random.value)
                .ToList();

            // 2. 이번 라운드 정렬 순서 한눈에 출력
            Debug.Log("<color=white>▶ 이번 라운드 행동 순서:</color>");
            for (int i = 0; i < sortedQueue.Count; i++)
            {
                Debug.Log($"   [{i + 1}번째] {sortedQueue[i].unitName} (팀: {sortedQueue[i].team} / Spd: {sortedQueue[i].speed})");
            }

            yield return new WaitForSeconds(turnDelay);

            // 3. 차례대로 턴 진행
            foreach (var unit in sortedQueue)
            {
                // 팀에 따라 콘솔 색상 다르게 표시
                string color = (unit.team == Team.Ally) ? "green" : "red";
                Debug.Log($"<color={color}>▶ [{unit.team}] {unit.unitName} (스피드 {unit.speed})의 턴 실행 중...</color>");

                // 턴 진행 대기
                yield return new WaitForSeconds(turnDelay);
            }

            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("<color=yellow>테스트 3라운드가 정상적으로 종료되었습니다!</color>");
    }
}