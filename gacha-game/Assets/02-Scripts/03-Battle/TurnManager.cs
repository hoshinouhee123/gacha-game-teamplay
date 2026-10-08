using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용 시 (일반 Text를 쓴다면 UnityEngine.UI.Text 사용)

public class TurnManager : MonoBehaviour
{
    [Header("전투 유닛들")]
    public List<Unit> fieldUnits = new List<Unit>();       // 현재 필드에 나와있는 유닛들
    public List<Unit> reserveUnits = new List<Unit>();   // 대기석에 있는 유닛들 (최대 2명)

    [Header("SP 시스템")]
    public int currentSP = 9;   // 첫 턴 시작 시 기본 9
    public int maxSP = 10;

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


        StartCoroutine(BattleRoutine());
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
            if (target != null)
            {
                Debug.Log($"<color=cyan>[{actor.UnitName}]이(가) [{selectedSkill.skillName}] 사용! -> {target.UnitName}에게 {finalDamage} 피해!</color>");
                target.TakeDamage(finalDamage);
            }

            yield return new WaitForSeconds(1.0f); // 스킬 연출 대기

            // 3번째 스킬(스킬 3)을 썼고, 대기 중인 유닛이 살아있다면 교대 진행!
            bool isSkill3 = (actor.unitData.skills.Length >= 3 && selectedSkill == actor.unitData.skills[2]);
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
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

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