using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용 시 (일반 Text를 쓴다면 UnityEngine.UI.Text 사용)

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance; // 피격 등에서 쉽게 접근 가능하도록 싱글톤 적용

    [Header("전투 유닛들")]
    public List<Unit> fieldUnits = new List<Unit>();       // 현재 필드에 나와있는 유닛들
    public List<Unit> reserveUnits = new List<Unit>();   // 대기석에 있는 유닛들 (최대 2명)

    [Header("SP 시스템")]
    public int currentSP = 9;   // 첫 턴 시작 시 기본 9
    public int maxSP = 10;

    [Header(" 스타성 시스템")]
    public float currentStarPower = 0f; // 내부 계산용 (소수점 유지)
    public const float MAX_STAR_POWER = 120f;
    public TextMeshProUGUI starPowerText; // 상단 스타성 텍스트


    [Header("UI 연결")]
    public GameObject skillPanel;           // 스킬 카드들을 감싸는 패널
    public Button[] skillButtons;           // 3개의 스킬 버튼
    public TextMeshProUGUI[] skillButtonTexts; // 각 버튼의 텍스트 (TMP)
    public TextMeshProUGUI spText;          // 현재 SP 표시 텍스트

    [Header("교대 UI 연결")]
    public GameObject swapPanel;                     // 교대 선택 팝업창
    public Button[] swapButtons;                     // 대기 유닛 선택 버튼 2개
    public TextMeshProUGUI[] swapButtonTexts;        // 버튼 위 텍스트 2개

    private Queue<Unit> turnQueue = new Queue<Unit>();
    private SkillDataSO selectedSkill = null; // 플레이어가 선택한 스킬 임시 저장
    private Unit selectedSwapUnit = null;            // 플레이어가 고른 교대 유닛
    private int currentRound = 0;
    private bool isBattleCleared = false;

    private void Start()
    {
        // 시작 시 스킬 UI는 숨김
        if (skillPanel != null) skillPanel.SetActive(false);
        if (swapPanel != null) swapPanel.SetActive(false);

        // 대기 유닛들은 게임 시작 시 화면에서 숨겨둠
        foreach (var unit in reserveUnits)
        {
            if (unit != null) unit.gameObject.SetActive(false);
        }

        UpdateSPUI();
        UpdateStarPowerUI();

        StartCoroutine(BattleRoutine());
    }

    // ================= [ 스타성 관리 함수 ] =================
    public void AddStarPower(float amount, string reason = "")
    {
        currentStarPower += amount;
        currentStarPower = Mathf.Clamp(currentStarPower, 0f, MAX_STAR_POWER); // 0% ~ 120% 제한

        string sign = amount >= 0 ? $"+{amount:F1}%" : $"{amount:F1}%";
        Debug.Log($"<color=#FFD700>[스타성 {sign}] {reason} (현재: {(int)currentStarPower}%)</color>");

        UpdateStarPowerUI();
    }

    private void UpdateStarPowerUI()
    {
        if (starPowerText != null)
        {
            // 소수점은 제외하고 정수로만 표시
            starPowerText.text = $"스타성 : {(int)currentStarPower}%";
        }
    }

    private void UpdateSPUI()
    {
        if (spText != null)
        {
            spText.text = $"SP: {currentSP} / {maxSP}";
        }
    }

    private IEnumerator BattleRoutine()
    {
        while (true)
        {
            currentRound++;

            // 스피드 순 정렬
            var sortedList = fieldUnits
                .Where(u => u.IsAlive)
                .OrderByDescending(u => u.currentSpeed)
                .ThenBy(u => Random.value)
                .ToList();

            turnQueue.Clear();
            foreach (var unit in sortedList) turnQueue.Enqueue(unit);

            while (turnQueue.Count > 0)
            {
                Unit currentUnit = turnQueue.Dequeue();
                if (!currentUnit.IsAlive) continue;

                // 턴 행동 진행
                yield return StartCoroutine(ExecuteUnitTurn(currentUnit));

                // 행동 직후 승리 조건 체크
                if (CheckAllEnemiesDead())
                {
                    yield return StartCoroutine(BattleClearRoutine());
                    yield break;
                }

                yield return new WaitForSeconds(0.3f);
            }

            yield return new WaitForSeconds(1.0f);
        }
    }

    private IEnumerator ExecuteUnitTurn(Unit actor)
    {
        if (actor.Team == Team.Ally)
        {
            // ================= 1. 플레이어 턴 =================
            Debug.Log($"<color=yellow>[플레이어] {actor.UnitName}의 턴! 스킬을 선택하세요.</color>");
            selectedSkill = null;

            // 스킬 버튼 세팅 & 패널 오픈
            OpenSkillUI(actor);

            // 플레이어가 스킬 버튼을 누를 때까지 코루틴 일시 정지(대기)
            yield return new WaitUntil(() => selectedSkill != null);

            // 선택 완료 후 패널 닫기
            skillPanel.SetActive(false);

            // SP 처리
            currentSP += selectedSkill.spGain;
            currentSP -= selectedSkill.spCost;
            currentSP = Mathf.Clamp(currentSP, 0, maxSP);
            UpdateSPUI();

            // 데미지 계산: 공격력 * 퍼센트
            int finalDamage = Mathf.RoundToInt(actor.unitData.attackPower * selectedSkill.damageMultiplier);

            // 첫 번째 살아있는 적 공격
            Unit target = fieldUnits.FirstOrDefault(u => u.Team == Team.Enemy && u.IsAlive);

            // 3번째 스킬(스킬 3)을 썼고, 대기 중인 유닛이 살아있다면 교대 진행!
            bool isSkill3 = (actor.unitData.skills.Length >= 3 && selectedSkill == actor.unitData.skills[2]);
            

            if (target != null)
            {
                Debug.Log($"<color=cyan>[{actor.UnitName}]이(가) [{selectedSkill.skillName}] 사용! -> {target.UnitName}에게 {finalDamage} 피해!</color>");
                target.TakeDamage(finalDamage);

                if (target != null)
                {
                    bool targetWasAlive = target.IsAlive;
                    target.TakeDamage(finalDamage);

                    // 스타성 스킬 데미지 적립
                    actor.skillUseCount++;

                    if (isSkill3)
                    {
                        // 3스킬 태그: 입힌 데미지의 20%
                        float starGain = finalDamage * 0.20f;
                        AddStarPower(starGain, $"3스킬 사용 ({finalDamage} 피해의 20%)");
                    }
                    else
                    {
                        // 일반 스킬: 기본 10%
                        // 첫 스킬 -> 2배(20%), 2번째 -> 1배(10%), 3번째 이상 -> 0.5배(5%)
                        float multiplier = 1.0f;
                        if (actor.skillUseCount == 1) multiplier = 2.0f;
                        else if (actor.skillUseCount >= 3) multiplier = 0.5f;

                        float starGain = (finalDamage * 0.10f) * multiplier;
                        AddStarPower(starGain, $"{actor.UnitName} {actor.skillUseCount}번째 스킬 데미지 적립");
                    }

                    // 적 처치 시 5% 추가
                    if (targetWasAlive && !target.IsAlive)
                    {
                        AddStarPower(5f, $"적 [{target.UnitName}] 처치 보너스");
                    }
                }
            }

            yield return new WaitForSeconds(1.0f); // 스킬 연출 대기


            bool hasAliveReserves = reserveUnits.Any(u => u != null && u.IsAlive);

            if (isSkill3 && hasAliveReserves)
            {
                yield return StartCoroutine(SwapProcessRoutine(actor));
            }
        }
        else
        {
            // [적 턴 AI 예시]
            Debug.Log($"[적] {actor.UnitName}의 턴!");
            yield return new WaitForSeconds(1.0f);

            // 스킬이 등록되어 있다면 스킬 중 랜덤 1개 선택, 없으면 평타(계수 1.0)
            var availableSkills = actor.unitData.skills.Where(s => s != null).ToList();
            float multiplier = 1.0f;
            string skillName = "일반 공격";

            if (availableSkills.Count > 0)
            {
                var randomSkill = availableSkills[Random.Range(0, availableSkills.Count)];
                multiplier = randomSkill.damageMultiplier;
                skillName = randomSkill.skillName;
            }

            int damage = Mathf.RoundToInt(actor.unitData.attackPower * multiplier);

            Unit target = fieldUnits.FirstOrDefault(u => u.Team == Team.Ally && u.IsAlive);
            if (target != null)
            {
                Debug.Log($"<color=red>[적] {actor.UnitName}이(가) [{skillName}] 사용! -> {damage} 피해!</color>");
                target.TakeDamage(damage);

                // 아군 피격 스타성 처리
                target.hitCount++;
                if (target.hitCount >= 3)
                {
                    // 3번 이상 피격 시 2% 감소
                    AddStarPower(-2f, $"{target.UnitName} 누적 3회 이상 피격 감점");
                }
                else
                {
                    // 1, 2번째 피격은 1% 증가
                    AddStarPower(1f, $"{target.UnitName} 피격 획득");
                }

                // 사망 시 8% 감소
                if (!target.IsAlive)
                {
                    AddStarPower(-8f, $"{target.UnitName} 사망 감점");
                }


            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    // ================= [ 클리어 판정 및 보너스 정산 ] =================
    private bool CheckAllEnemiesDead()
    {
        return !fieldUnits.Any(u => u.Team == Team.Enemy && u.IsAlive);
    }

    private IEnumerator BattleClearRoutine()
    {
        isBattleCleared = true;
        Debug.Log("<color=green>================ STAGE CLEAR! ================</color>");
        yield return new WaitForSeconds(0.5f);

        // 1. 턴 수(라운드) 보너스
        if (currentRound <= 3)
        {
            AddStarPower(20f, $"3턴 이내 클리어 보너스 ({currentRound}턴)");
        }
        else if (currentRound <= 10)
        {
            AddStarPower(5f, $"10턴 이내 클리어 보너스 ({currentRound}턴)");
        }
        else if (currentRound >= 15)
        {
            AddStarPower(-5f, $"15턴 이상 지연 클리어 감점 ({currentRound}턴)");
        }

        // 2. 단 1명만 생존 보너스
        // 필드와 대기석을 합쳐서 살아있는 아군 수 카운트
        int aliveAlliesCount = fieldUnits.Concat(reserveUnits).Count(u => u != null && u.Team == Team.Ally && u.IsAlive);
        if (aliveAlliesCount == 1)
        {
            AddStarPower(25f, "외줄타기 승리! (생존 아군 1명 보너스)");
        }

        // 3. 퍼포먼스 값 총합 보너스
        // 전투에 참여한 모든 아군 캐릭터(필드 + 대기)의 퍼포먼스 합산
        int totalPerformance = fieldUnits.Concat(reserveUnits)
            .Where(u => u != null && u.Team == Team.Ally)
            .Sum(u => u.Performance);

        AddStarPower(totalPerformance, $"아군 전체 퍼포먼스 스탯 합산 보너스 (+{totalPerformance}%)");

        Debug.Log($"<color=cyan> 최종 클리어 스타성: {(int)currentStarPower}% </color>");
    }


    // ================= [ 교대 및 UI 관련 ] =================

    // 교대 처리 코루틴
    private IEnumerator SwapProcessRoutine(Unit currentActor)
    {
        Debug.Log("<color=yellow>3스킬 발동! 교대할 대기 캐릭터를 선택하세요.</color>");
        selectedSwapUnit = null;

        // 교대 UI 열기
        OpenSwapUI();

        // 플레이어가 교대 버튼을 누를 때까지 대기
        yield return new WaitUntil(() => selectedSwapUnit != null);

        // 교대 UI 닫기
        swapPanel.SetActive(false);

        // 1. 위치 맞바꾸기
        Vector3 fieldPos = currentActor.transform.position;
        selectedSwapUnit.transform.position = fieldPos;

        // 2. 오브젝트 켜고 끄기
        currentActor.gameObject.SetActive(false);
        selectedSwapUnit.gameObject.SetActive(true);

        // 3. 리스트 스왑 (필드 <-> 대기석)
        fieldUnits.Remove(currentActor);
        reserveUnits.Remove(selectedSwapUnit);

        fieldUnits.Add(selectedSwapUnit);
        reserveUnits.Add(currentActor);

        Debug.Log($"<color=green> [{currentActor.UnitName}] 을 [{selectedSwapUnit.UnitName}] 으로 교대 완료! </color>");

        yield return new WaitForSeconds(0.8f);
    }

    // 교대 UI 창 세팅
    private void OpenSwapUI()
    {
        swapPanel.SetActive(true);

        for (int i = 0; i < swapButtons.Length; i++)
        {
            if (i < reserveUnits.Count && reserveUnits[i] != null)
            {
                Unit reserveUnit = reserveUnits[i];
                swapButtons[i].gameObject.SetActive(true);
                swapButtonTexts[i].text = $"{reserveUnit.UnitName}\n(HP: {reserveUnit.currentHp}/{reserveUnit.maxHp})";

                // 죽은 대기 캐릭터는 선택 불가
                swapButtons[i].interactable = reserveUnit.IsAlive;

                swapButtons[i].onClick.RemoveAllListeners();
                swapButtons[i].onClick.AddListener(() => OnSwapSelected(reserveUnit));
            }
            else
            {
                swapButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnSwapSelected(Unit chosenUnit)
    {
        selectedSwapUnit = chosenUnit;
    }

    // 캐릭터의 스킬 3개를 UI 버튼에 주입
    private void OpenSkillUI(Unit actor)
    {
        if (skillPanel == null)
        {
            Debug.LogError("<color=red>[오류] TurnManager에 SkillPanel이 연결되지 않았습니다!</color>");
            return;
        }

        skillPanel.SetActive(true);

        for (int i = 0; i < 3; i++)
        {
            // 인스펙터에 버튼이나 텍스트가 안 꽂혀 있으면 경고 출력
            if (i >= skillButtons.Length || skillButtons[i] == null)
            {
                Debug.LogError($"<color=red>[오류] Skill Buttons의 {i}번 슬롯이 비어있습니다!</color>");
                continue;
            }

            // 해당 캐릭터의 스킬 슬롯 확인
            SkillDataSO skill = (actor.unitData != null && i < actor.unitData.skills.Length)
                                ? actor.unitData.skills[i]
                                : null;

            if (skill != null)
            {
                skillButtons[i].gameObject.SetActive(true);

                // 텍스트 컴포넌트가 연결되어 있을 때만 텍스트 변경
                if (i < skillButtonTexts.Length && skillButtonTexts[i] != null)
                {
                    string spInfo = skill.spGain > 0 ? $"(+SP {skill.spGain})" : $"(-SP {skill.spCost})";
                    skillButtonTexts[i].text = $"{skill.skillName}\n{spInfo}";
                }

                // SP 조건 체크
                bool canUse = (currentSP >= skill.spCost);
                skillButtons[i].interactable = canUse;

                // 클릭 이벤트 등록
                skillButtons[i].onClick.RemoveAllListeners();
                skillButtons[i].onClick.AddListener(() => OnSkillSelected(skill));
            }
            else
            {
                // 스킬이 등록 안 된 버튼은 숨김 처리
                skillButtons[i].gameObject.SetActive(false);
            }
        }
    }
    private void OnSkillSelected(SkillDataSO skill)
    {
        selectedSkill = skill; // 루프의 WaitUntil 조건을 만족시킴
    }
}