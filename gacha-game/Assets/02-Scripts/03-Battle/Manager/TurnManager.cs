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

    [Header("전투 결과 통계 UI 연결")]
    public GameObject resultPanel;               // 결과창 팝업 패널
    public TextMeshProUGUI resultSummaryText;    // 클리어 턴, 스타성, 명성 요약 텍스트
    public TextMeshProUGUI statsTableText;       // 캐릭터별 상세 통계 텍스트
    public Button confirmButton;                 // 확인/닫기 버튼

    [Header("스킬 연출 배너 UI 연결")]
    public GameObject skillBannerPanel;              // 배너 전체 패널
    public TextMeshProUGUI bannerCharacterNameText;  // 캐릭터 이름 텍스트
    public TextMeshProUGUI bannerSkillNameText;      // 스킬 이름 텍스트
    public CanvasGroup bannerCanvasGroup;            // 부드러운 투명도 조절용

    [Header("연출 속도 및 텀 조절 (초)")]
    [Tooltip("카메라가 줌인되며 시전자를 비추는 시간")]
    public float zoomTime = 0.4f;

    [Tooltip("카메라 줌 완료 후 돌진하기 전 뜸 들이는 시간 (기 모으기)")]
    public float preDashDelay = 0.25f;

    [Tooltip("적 앞으로 돌진해 다가가는 시간")]
    public float dashTime = 0.45f;

    [Tooltip("공격 타격 후 그 자리에서 폼 잡고 머무는 시간 (여운/후딜)")]
    public float postAttackWait = 0.35f;

    [Tooltip("원래 자기 자리로 복귀하는 시간")]
    public float returnTime = 0.4f;

    [Tooltip("한 턴이 완전히 끝나고 다음 턴으로 넘어가기 전 휴식 텀")]
    public float postTurnDelay = 0.5f;

    private Queue<Unit> turnQueue = new Queue<Unit>();
    private SkillDataSO selectedSkill = null; // 플레이어가 선택한 스킬 임시 저장
    private Unit selectedSwapUnit = null;            // 플레이어가 고른 교대 유닛
    private int currentRound = 0;
    private bool isBattleCleared = false;



    private void Start()
    {
        if (skillPanel != null) skillPanel.SetActive(false);
        if (swapPanel != null) swapPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false); // 결과창 숨김

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

            // 살아있는 적 목록 가져오기
            var aliveEnemies = fieldUnits.Where(u => u.Team == Team.Enemy && u.IsAlive).ToList();
            if (aliveEnemies.Count == 0) yield break;

            Unit target = null;

            // 적이 1명이면 자동으로 그 적 타겟팅
            if (aliveEnemies.Count == 1)
            {
                target = aliveEnemies[0];
            }
            else
            {
                // 적이 2명 이상이면 마우스 클릭을 받을 때까지 대기!
                yield return StartCoroutine(WaitForTargetSelection(aliveEnemies, (chosen) => target = chosen));
            }

            // 타겟이 확정된 후 SP 차감 (잘못 누르는 상황 방지)
            currentSP += selectedSkill.spGain;
            currentSP -= selectedSkill.spCost;
            currentSP = Mathf.Clamp(currentSP, 0, maxSP);
            UpdateSPUI();

            // 데미지 계산: 공격력 * 퍼센트
            int finalDamage = Mathf.RoundToInt(actor.unitData.attackPower * selectedSkill.damageMultiplier);

            // 스킬 3 사용 시 교대 처리
            bool isSkill3 = (actor.unitData.skills.Length >= 3 && selectedSkill == actor.unitData.skills[2]);

            if (target != null)
            {
                Debug.Log($"<color=cyan>[{actor.UnitName}]이(가) [{selectedSkill.skillName}] 사용! -> {target.UnitName}에게 {finalDamage} 피해!</color>");

                if (target != null)
                {
                    bool targetWasAlive = target.IsAlive;
                    string displayName = !string.IsNullOrEmpty(selectedSkill.skillName) ? selectedSkill.skillName : selectedSkill.name;

                    Debug.Log($"<color=cyan>[{actor.UnitName}]이(가) [{displayName}] 사용! -> 타겟: [{target.UnitName}]</color>");

                    // 여기서 연출 시퀀스 실행 (카메라 줌 -> 이동 -> 공격 -> 복귀)
                    yield return StartCoroutine(PerformAttackSequence(actor, target, finalDamage, isSkill3, selectedSkill.skillName));

                    // 공격한 캐릭터의 누적 딜량 기록
                    actor.totalDamageDealt += finalDamage;

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


            // 적이 전멸했는지 먼저 확인
            bool isAllEnemiesDead = CheckAllEnemiesDead();
            bool hasAliveReserves = reserveUnits.Any(u => u != null && u.IsAlive);

            // 대기 유닛이 있고 + 적이 아직 살아있을 때만 교대창을 띄움
            if (isSkill3 && hasAliveReserves && !isAllEnemiesDead)
            {
                yield return StartCoroutine(SwapProcessRoutine(actor));
            }
        }
        else
        {
            // ================= 2. 적(Enemy) 턴 =================
            Debug.Log($"<color=red>[적] {actor.UnitName}의 턴!</color>");
            yield return new WaitForSeconds(0.4f); // 생각하는 척 딜레이

            // 스킬 중 무작위 1개 선택 (없으면 평타 계수 1.0)
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

            // 살아있는 아군 중 랜덤으로 1명 타겟팅 (다양한 아군 공격)
            var aliveAllies = fieldUnits.Where(u => u.Team == Team.Ally && u.IsAlive).ToList();
            Unit target = (aliveAllies.Count > 0) ? aliveAllies[Random.Range(0, aliveAllies.Count)] : null;

            if (target != null)
            {
                Debug.Log($"<color=red>[적] {actor.UnitName}이(가) [{skillName}] 사용! -> 타겟: {target.UnitName}</color>");

                // 아군과 똑같은 연출 시퀀스 실행
                // 카메라가 적에게 줌인 -> 아군 앞으로 돌진 -> 타격 & 셰이크 -> 복귀
                yield return StartCoroutine(PerformAttackSequence(actor, target, damage, false, skillName));

                // 아군 피격 스타성 처리
                target.hitCount++;
                if (target.hitCount >= 3)
                    AddStarPower(-2f, $"{target.UnitName} 누적 3회 이상 피격 감점");
                else
                    AddStarPower(1f, $"{target.UnitName} 피격 획득");

                if (!target.IsAlive)
                    AddStarPower(-8f, $"{target.UnitName} 사망 감점");
            }

            yield return new WaitForSeconds(0.3f);
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

        // ================= [ 4. 명성 재화 정산 및 저장 ] =================
        int finalStarPower = (int)currentStarPower; // 소수점 제외 최종 스타성

        // 스타성 x 3을 하되, 최대 300까지만 획득하도록 제한 (100% 초과해도 300 고정)
        int earnedRenown = Mathf.Min(finalStarPower * 3, 300);

        // 명성 지급 및 PlayerPrefs 영구 저장
        CurrencyManager.AddRenown(earnedRenown);

        Debug.Log($"<color=#FFD700>==================================================</color>");
        Debug.Log($"<color=#FFD700>최종 스타성: {finalStarPower}%</color>");
        Debug.Log($"<color=#00FFFF>획득 명성: {finalStarPower} x 3 = +{earnedRenown}</color>");
        Debug.Log($"<color=#00FFFF>누적 총 보유 명성: {CurrencyManager.GetRenown()}</color>");
        Debug.Log($"<color=#FFD700>==================================================</color>");

        yield return new WaitForSeconds(1.0f);

        // 5. 전투 통계표 화면에 표시
        ShowBattleResultUI(finalStarPower, earnedRenown);
    }

    // 통계창 UI 오픈 및 텍스트 작성 함수
    private void ShowBattleResultUI(int starPower, int renown)
    {
        if (resultPanel == null) return;

        resultPanel.SetActive(true);

        // 좌측 상단에서 중앙으로 스르륵 나오는 애니메이션 실행
        StartCoroutine(SlideInResultPanelRoutine());

        // 1. 상단 요약 정보
        if (resultSummaryText != null)
        {
            resultSummaryText.text = $"<b>[ STAGE CLEAR ]</b>\n" +
                                     $"소요 턴: <color=#D97706>{currentRound}턴</color> | " +
                                     $"최종 스타성: <color=#B45309>{starPower}%</color> | " +
                                     $"획득 명성: <color=#0284C7>+{renown}</color>";
        }

        // 2. 캐릭터별 상세 통계표 (특수 공백 제거 & 가독성 개선)
        if (statsTableText != null)
        {
            var allAllies = fieldUnits.Concat(reserveUnits)
                .Where(u => u != null && u.Team == Team.Ally)
                .Distinct()
                .ToList();

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (var ally in allAllies)
            {
                string status = ally.IsAlive ? "" : " <color=red>(사망)</color>";

                // 캐릭터 이름 (검은색 굵게)
                sb.AppendLine($"<b><color=#222222>{ally.UnitName}</color></b>{status}");

                // 상세 수치 (색상 대비를 높여 흰 배경에서도 또렷하게 보이도록 조정)
                sb.AppendLine($"   <color=#DC2626>입힌 피해: {ally.totalDamageDealt}</color>  |  " +
                              $"<color=#2563EB>받은 피해: {ally.totalDamageTaken}</color>  |  " +
                              $"<color=#16A34A>회복량: {ally.totalHealing}</color>");
                sb.AppendLine(); // 줄바꿈
            }

            statsTableText.text = sb.ToString();
        }

        // 확인 버튼
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() => {
                resultPanel.SetActive(false);
            });
        }
    }

    // 좌측 상단 -> 중앙으로 스르륵 슬라이드되는 애니메이션
    private IEnumerator SlideInResultPanelRoutine()
    {
        RectTransform rect = resultPanel.GetComponent<RectTransform>();
        if (rect == null) yield break;

        // 800x1000 패널이 화면 밖 좌측 하단에 완전히 숨겨진 시작 위치
        Vector2 startPos = new Vector2(-1500f, -1200f);
        Vector2 targetPos = Vector2.zero; // 화면 정중앙 (Pos X: 0, Pos Y: 0)

        float duration = 1f; // 스르륵 들어오는 시간 (1초)
        float elapsed = 0f;

        // 시작 위치로 먼저 이동
        rect.anchoredPosition = startPos;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Ease-Out Cubic: 처음에 빠르게 나오다가 끝에서 부드럽게 감속
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);
            yield return null;
        }

        // 마지막 오차 방지용으로 정확히 중앙 배치
        rect.anchoredPosition = targetPos;
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

        // 들어온 캐릭터의 원래 자리(originPos)를 이 필드 자리로 새로 기억시킵니다!
        selectedSwapUnit.originPos = fieldPos;

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

    // 여유롭고 묵직한 템포가 적용된 연출 시퀀스
    // PerformAttackSequence 매개변수에 string skillName 추가
    private IEnumerator PerformAttackSequence(Unit attacker, Unit target, int damage, bool isSkill3, string skillName)
    {
        UnitAnimator attackerAnim = attacker.GetComponent<UnitAnimator>();
        UnitAnimator targetAnim = target.GetComponent<UnitAnimator>();

        // 1. 카메라 줌인 & 스킬 컷인 배너 등장!
        if (BattleCamera.Instance != null)
        {
            BattleCamera.Instance.StartFollowing(attacker.transform, 0.78f);
        }

        // ★ [추가] 줌인과 동시에 화면에 캐릭터명과 스킬명 출력
        ShowSkillBanner(attacker.UnitName, skillName);

        yield return new WaitForSeconds(zoomTime);
        yield return new WaitForSeconds(preDashDelay);

        // ★ [추가] 돌진 시작할 때 배너를 부드럽게 페이드아웃
        HideSkillBanner();

        // 2. 적 앞으로 묵직하게 가감속 돌진
        float stopOffset = (attacker.Team == Team.Ally) ? -1.8f : 1.8f;
        Vector3 attackPos = new Vector3(target.transform.position.x + stopOffset, target.transform.position.y, attacker.transform.position.z);

        yield return StartCoroutine(MoveToPositionRoutine(attacker.transform, attackPos, dashTime));

        // 3. 공격 모션 및 타격
        string animName = isSkill3 ? "Skill_3" : "Attack";

        if (attackerAnim != null)
        {
            yield return StartCoroutine(attackerAnim.PlayAttackAnimation(animName, () =>
            {
                target.TakeDamage(damage);
                attacker.totalDamageDealt += damage;

                if (BattleCamera.Instance != null) BattleCamera.Instance.Shake(0.12f, 0.12f);
                if (targetAnim != null) StartCoroutine(targetAnim.PlayHitAnimation());
            }));
        }
        else
        {
            target.TakeDamage(damage);
            attacker.totalDamageDealt += damage;
            if (BattleCamera.Instance != null) BattleCamera.Instance.Shake(0.12f, 0.12f);
            yield return new WaitForSeconds(0.35f);
        }

        yield return new WaitForSeconds(postAttackWait);

        // 4. 복귀 및 카메라 원복
        if (BattleCamera.Instance != null)
        {
            BattleCamera.Instance.ResetCamera(returnTime + 0.1f);
        }

        Vector3 returnPos = (attacker.originPos != Vector3.zero) ? attacker.originPos : attacker.transform.position;
        yield return StartCoroutine(MoveToPositionRoutine(attacker.transform, returnPos, returnTime));

        yield return new WaitForSeconds(postTurnDelay);
    }

    // 가속/감속(Ease-In-Out)이 적용된 부드러운 위치 이동 보조 코루틴
    private IEnumerator MoveToPositionRoutine(Transform targetTransform, Vector3 endPos, float duration)
    {
        Vector3 startPos = targetTransform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Ease-In-Out Cubic: 천천히 부드럽게 출발 -> 중간에서 가속 -> 끝에서 스르륵 감속
            float ease = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

            targetTransform.position = Vector3.Lerp(startPos, endPos, ease);
            yield return null;
        }
        targetTransform.position = endPos;
    }

    // 스킬 배너 표시 (페이드인)
    public void ShowSkillBanner(string charName, string skillName)
    {
        if (skillBannerPanel == null) return;

        if (bannerCharacterNameText != null) bannerCharacterNameText.text = charName;
        if (bannerSkillNameText != null) bannerSkillNameText.text = skillName;

        skillBannerPanel.SetActive(true);
        StopCoroutine("FadeBannerRoutine");
        StartCoroutine(FadeBannerRoutine(1f, 0.15f)); // 0.15초 만에 선명하게 페이드인
    }

    // 스킬 배너 숨김 (페이드아웃)
    public void HideSkillBanner()
    {
        if (skillBannerPanel == null || !skillBannerPanel.activeSelf) return;

        StopCoroutine("FadeBannerRoutine");
        StartCoroutine(FadeBannerRoutine(0f, 0.2f, () => skillBannerPanel.SetActive(false)));
    }

    private IEnumerator FadeBannerRoutine(float targetAlpha, float duration, System.Action onComplete = null)
    {
        if (bannerCanvasGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float startAlpha = bannerCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bannerCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        bannerCanvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    // 플레이어가 마우스로 적을 클릭할 때까지 대기하는 코루틴
    private IEnumerator WaitForTargetSelection(List<Unit> enemies, System.Action<Unit> onTargetSelected)
    {
        Debug.Log("<color=yellow>▶ 공격할 적을 마우스로 클릭하세요!</color>");

        // 살아있는 모든 적에게 타겟팅 모드 켜기 (마우스 올리면 붉게 반응)
        foreach (var enemy in enemies)
        {
            enemy.SetTargetable(true);
        }

        Unit selectedUnit = null;

        while (selectedUnit == null)
        {
            // 마우스 좌클릭 감지
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                // 1. 3D 레이캐스트 검사 (BoxCollider 등)
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Unit hitUnit = hit.collider.GetComponentInParent<Unit>();
                    if (hitUnit != null && enemies.Contains(hitUnit))
                    {
                        selectedUnit = hitUnit;
                    }
                }

                // 2. 2D 레이캐스트 검사 (BoxCollider2D 대비)
                if (selectedUnit == null)
                {
                    RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray);
                    if (hit2D.collider != null)
                    {
                        Unit hitUnit = hit2D.collider.GetComponentInParent<Unit>();
                        if (hitUnit != null && enemies.Contains(hitUnit))
                        {
                            selectedUnit = hitUnit;
                        }
                    }
                }
            }

            yield return null;
        }

        // 타겟 선택 완료 -> 모든 적 타겟팅 모드 해제
        foreach (var enemy in enemies)
        {
            enemy.SetTargetable(false);
        }

        onTargetSelected?.Invoke(selectedUnit);
    }
}