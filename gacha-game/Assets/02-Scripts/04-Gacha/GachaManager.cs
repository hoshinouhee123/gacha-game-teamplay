using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 캐릭터 가챠: 1회 / 10회, 1성 70% / 2성 25% / 3성 5%.
/// 해당 등급 캐릭터가 없으면 존재하는 등급끼리 비율을 재정규화합니다.
/// 결과는 CurrencyManager.SaveData.json의 ownedCharacterIds로 저장합니다.
/// </summary>
public class GachaManager : MonoBehaviour
{
    [Header("캐릭터 목록")]
    [SerializeField] private CharacterRosterSO roster;

    [Header("가챠 가격")]
    [Min(1)] public int singleDrawCost = 120;
    [Min(1)] public int tenDrawCost = 1200;

    [Header("연출 속도")]
    [Min(0f)] public float drawDelay = 0.7f;
    [Min(0f)] public float nextDrawDelay = 0.15f;

    [Header("기존 가챠 버튼")]
    public GameObject singleDrawButton;
    public GameObject tenDrawButton;

    [Header("기존 가격 UI")]
    public GameObject singlePriceText;
    public GameObject tenPriceText;

    [Header("텍스트 UI")]
    public TMP_Text fameText;
    public TMP_Text resultText;
    [SerializeField] private TMP_Text ownedCountText; // 선택 사항

    [Header("획득 캐릭터 초상화 (선택 사항)")]
    [SerializeField] private Image resultPortrait;

    [Header("테스트 기능")]
    [Min(0)] public int testRewardAmount = 1200;

    private readonly List<BattleUnit> oneStar = new List<BattleUnit>();
    private readonly List<BattleUnit> twoStar = new List<BattleUnit>();
    private readonly List<BattleUnit> threeStar = new List<BattleUnit>();

    private int testRenown;
    private bool isDrawing;
    private bool initialized;
    private Coroutine drawCoroutine;
    private int lastDisplayedRenown = -1;
    private int lastDisplayedOwned = -1;

    private void Start()
    {
        testRenown = 0;
        isDrawing = false;
        SetGachaUI(true);
        SetResult("");
        if (resultPortrait != null) resultPortrait.enabled = false;

        if (roster == null)
        {
            SetResult("GachaManager의 Roster를 지정해 주세요.");
            Debug.LogError("[가챠] CharacterRosterSO 연결 누락");
            return;
        }

        if (!CurrencyManager.TryGrantStarterCharacters(roster))
        {
            SetResult("캐릭터 데이터 저장 오류");
            return;
        }

        BuildPools();
        initialized = true;
        UpdateUI(true);
    }

    private void Update()
    {
        UpdateUI();
    }

    private void BuildPools()
    {
        oneStar.Clear();
        twoStar.Clear();
        threeStar.Clear();

        if (roster == null || roster.characters == null) return;

        var seenIds = new HashSet<string>();
        foreach (BattleUnit character in roster.characters)
        {
            if (character == null || character.team != Team.Ally ||
                string.IsNullOrWhiteSpace(character.characterId))
                continue;

            if (!seenIds.Add(character.characterId))
            {
                Debug.LogWarning("[가챠] 중복 characterId: " + character.characterId);
                continue;
            }

            switch (character.rarity)
            {
                case 1: oneStar.Add(character); break;
                case 2: twoStar.Add(character); break;
                case 3: threeStar.Add(character); break;
                default:
                    Debug.LogWarning("[가챠] 등급은 1~3이어야 합니다: " + character.unitName);
                    break;
            }
        }

        Debug.Log($"[가챠] 캐릭터 풀: 1성 {oneStar.Count}, 2성 {twoStar.Count}, 3성 {threeStar.Count}");
    }

    public int GetTotalRenown()
    {
        int real = Mathf.Max(0, CurrencyManager.GetRenown());
        long combined = (long)real + testRenown;
        return (int)System.Math.Min(combined, int.MaxValue);
    }

    public void AddTestRenown()
    {
        if (isDrawing || testRewardAmount <= 0) return;
        if (testRenown > int.MaxValue - testRewardAmount) return;
        testRenown += testRewardAmount;
        UpdateUI(true);
    }

    public void DrawOne()
    {
        StartDraw(1, singleDrawCost);
    }

    public void DrawTen()
    {
        StartDraw(10, tenDrawCost);
    }

    // 선택 사항: 가챠 화면의 "편성으로 이동" 버튼에 연결하세요.
    public void OpenFormationScene()
    {
        if (isDrawing) return;
        SceneManager.LoadScene("PartyFormationScene");
    }

    private void StartDraw(int count, int price)
    {
        if (isDrawing) return;
        if (!initialized)
        {
            SetResult("가챠 초기화 실패: Roster 또는 저장 파일을 확인해 주세요.");
            return;
        }
        if (roster == null || price <= 0)
        {
            SetResult("캐릭터 목록 또는 가챠 가격을 확인해 주세요.");
            return;
        }

        if (oneStar.Count + twoStar.Count + threeStar.Count == 0)
        {
            SetResult("가챠에 등록된 아군 캐릭터가 없어요.");
            return;
        }

        if (GetTotalRenown() < price)
        {
            SetResult("명성이 부족합니다!");
            return;
        }

        isDrawing = true;
        SetGachaUI(false);

        BattleUnit[] results = new BattleUnit[count];
        string[] ids = new string[count];

        for (int i = 0; i < count; i++)
        {
            if (!TryPickCharacter(out BattleUnit character))
            {
                CancelDraw("가챠에 등록된 캐릭터가 없습니다.");
                return;
            }
            results[i] = character;
            ids[i] = character.characterId;
        }

        // 테스트 명성 사용 후 부족한 금액만 실제 명성으로 결제
        int testSpend = Mathf.Min(testRenown, price);
        int realSpend = price - testSpend;

        if (!CurrencyManager.TrySpendAndUnlockCharacters(realSpend, ids, out bool[] newlyOwned))
        {
            CancelDraw("명성 차감 또는 캐릭터 저장에 실패했습니다.");
            return;
        }

        // 저장 성공 이후에 테스트 명성 확정 차감
        testRenown -= testSpend;
        UpdateUI(true);
        drawCoroutine = StartCoroutine(DrawCoroutine(results, newlyOwned));
    }

    private bool TryPickCharacter(out BattleUnit character)
    {
        character = null;
        int weight1 = oneStar.Count > 0 ? 70 : 0;
        int weight2 = twoStar.Count > 0 ? 25 : 0;
        int weight3 = threeStar.Count > 0 ? 5 : 0;
        int totalWeight = weight1 + weight2 + weight3;
        if (totalWeight <= 0) return false;

        int roll = Random.Range(0, totalWeight);
        List<BattleUnit> pool;
        if (roll < weight1) pool = oneStar;
        else if (roll < weight1 + weight2) pool = twoStar;
        else pool = threeStar;

        character = pool[Random.Range(0, pool.Count)];
        return character != null;
    }

    private IEnumerator DrawCoroutine(BattleUnit[] results, bool[] newlyOwned)
    {
        int newCount = 0;
        int duplicateCount = 0;
        var summary = new StringBuilder();

        for (int i = 0; i < results.Length; i++)
        {
            BattleUnit unit = results[i];
            bool isNew = newlyOwned[i];
            if (isNew) newCount++;
            else duplicateCount++;

            SetResult($"{unit.rarity}성 캐릭터 획득!\n{unit.unitName}\n" +
                      (isNew ? "NEW! 편성에서 사용할 수 있어요." : "이미 보유 중인 중복 캐릭터"));

            if (resultPortrait != null)
            {
                resultPortrait.sprite = unit.characterSprite;
                resultPortrait.enabled = unit.characterSprite != null;
            }

            summary.AppendLine($"{i + 1}. {unit.rarity}성 {unit.unitName} " +
                               (isNew ? "[NEW]" : "[중복]"));

            yield return new WaitForSecondsRealtime(drawDelay);
            if (i < results.Length - 1)
            {
                SetResult("");
                yield return new WaitForSecondsRealtime(nextDrawDelay);
            }
        }

        if (results.Length > 1)
        {
            SetResult($"10회 뽑기 완료!  신규 {newCount} / 중복 {duplicateCount}\n" +
                      summary.ToString());
        }

        FinishDraw();
    }

    private void CancelDraw(string message)
    {
        drawCoroutine = null;
        isDrawing = false;
        SetGachaUI(true);
        SetResult(message);
        UpdateUI(true);
    }

    private void FinishDraw()
    {
        drawCoroutine = null;
        isDrawing = false;
        SetGachaUI(true);
        UpdateUI(true);
    }

    private void SetGachaUI(bool show)
    {
        if (singleDrawButton != null) singleDrawButton.SetActive(show);
        if (tenDrawButton != null) tenDrawButton.SetActive(show);
        if (singlePriceText != null) singlePriceText.SetActive(show);
        if (tenPriceText != null) tenPriceText.SetActive(show);
    }

    private void SetResult(string message)
    {
        if (resultText != null) resultText.text = message;
    }

    private void UpdateUI(bool force = false)
    {
        int renown = GetTotalRenown();
        if (force || lastDisplayedRenown != renown)
        {
            lastDisplayedRenown = renown;
            if (fameText != null) fameText.text = "명성 : " + renown;
        }

        int owned = CurrencyManager.GetOwnedCharacterCount();
        if (force || lastDisplayedOwned != owned)
        {
            lastDisplayedOwned = owned;
            if (ownedCountText != null) ownedCountText.text = "보유 캐릭터 : " + owned + "명";
        }
    }

    private void OnDisable()
    {
        if (drawCoroutine != null)
        {
            StopCoroutine(drawCoroutine);
            drawCoroutine = null;
        }
        isDrawing = false;
        SetGachaUI(true);
    }
}
