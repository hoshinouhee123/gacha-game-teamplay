using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("전투 참가 유닛들")]
    public List<Unit> allUnits = new List<Unit>();

    // 이번 라운드에 행동할 유닛 큐
    private Queue<Unit> turnQueue = new Queue<Unit>();
    private int roundCount = 0;
    private bool isBattleOver = false;

    private void Start()
    {
        // 씬 시작 시 바로 턴 루프 가동
        StartCoroutine(BattleRoutine());
    }

    private IEnumerator BattleRoutine()
    {
        while (!isBattleOver)
        {
            // ---------------- [ 1. 라운드 시작 및 스피드 정렬 ] ----------------
            roundCount++;
            Debug.Log($"\n<color=cyan>================ [ ROUND {roundCount} 시작 ] ================</color>");

            // 살아있는 유닛만 골라서 스피드 내림차순(높은 순) 정렬
            // 스피드가 같으면(동률) Random.value로 무작위 처리
            var sortedList = allUnits
            .Where(u => u.IsAlive)
            .OrderByDescending(u => u.currentSpeed) // currentSpeed 기준으로 정렬!
            .ThenBy(u => Random.value)
            .ToList();

            turnQueue.Clear();
            Debug.Log("--- 턴 순서 ---");
            for (int i = 0; i < sortedList.Count; i++)
            {
                turnQueue.Enqueue(sortedList[i]);
                Debug.Log($"{i + 1}위: [{sortedList[i].Team}] {sortedList[i].UnitName} (Spd: {sortedList[i].currentSpeed})");
            }

            yield return new WaitForSeconds(1.0f); // 라운드 시작 연출 대기

            // ---------------- [ 2. 라운드 내 턴 진행 ] ----------------
            while (turnQueue.Count > 0)
            {
                Unit currentUnit = turnQueue.Dequeue();

                // 차례가 오기 전에 이미 사망했으면 스킵
                if (!currentUnit.IsAlive) continue;

                Debug.Log($"<color=yellow> [{currentUnit.Team}] {currentUnit.UnitName}의 턴 시작!</color>");

                // 유닛의 행동 처리 (플레이어 입력 or 적 AI 대기)
                yield return StartCoroutine(ExecuteUnitTurn(currentUnit));

                // 행동 후 전투 종료 체크
                if (CheckBattleEnd())
                {
                    isBattleOver = true;
                    yield break; // 전투 루프 종료
                }

                yield return new WaitForSeconds(0.5f); // 턴과 턴 사이 딜레이
            }

            Debug.Log($"<color=cyan>=== [ ROUND {roundCount} 종료 ] ===</color>\n");
            yield return new WaitForSeconds(1.0f);
        }
    }

    private IEnumerator ExecuteUnitTurn(Unit actor)
    {
        if (actor.Team == Team.Ally)
        {
            // ================= 플레이어 턴 =================
            Debug.Log($"[{actor.UnitName}] 플레이어 조작 대기 중...");

            // ※ 나중에 UI 버튼을 누를 때까지 기다리려면:
            // yield return new WaitUntil(() => isButtonPressed);
            // 지금은 프로토타입이므로 1초 대기 후 가장 첫 번째 살아있는 적 공격
            yield return new WaitForSeconds(1.0f);

            Unit target = allUnits.FirstOrDefault(u => u.Team == Team.Enemy && u.IsAlive);
            if (target != null)
            {
                Debug.Log($"[{actor.UnitName}]이(가) [{target.UnitName}]을(를) 기본 공격!");
                target.TakeDamage(25);
            }
        }
        else
        {
            // ================= 적(Enemy) 턴 (간단 AI) =================
            Debug.Log($"[{actor.UnitName}] 적 AI가 행동을 결정하는 중...");
            yield return new WaitForSeconds(1.0f); // 생각하는 척 딜레이

            // 살아있는 아군 중 랜덤으로 한 명 타겟팅
            var aliveAllies = allUnits.Where(u => u.Team == Team.Ally && u.IsAlive).ToList();
            if (aliveAllies.Count > 0)
            {
                Unit target = aliveAllies[Random.Range(0, aliveAllies.Count)];
                Debug.Log($"[{actor.UnitName}]이(가) [{target.UnitName}]을(를) 공격!");
                target.TakeDamage(15);
            }
        }
    }

    private bool CheckBattleEnd()
    {
        bool anyAllyAlive = allUnits.Any(u => u.Team == Team.Ally && u.IsAlive);
        bool anyEnemyAlive = allUnits.Any(u => u.Team == Team.Enemy && u.IsAlive);

        if (!anyEnemyAlive)
        {
            Debug.Log("<color=green>  모든 적을 처치했습니다. </color>");
            return true;
        }

        if (!anyAllyAlive)
        {
            Debug.Log("<color=red>아군이 전멸했습니다. </color>");
            return true;
        }

        return false;
    }
}