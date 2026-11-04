
using UnityEngine;
using TMPro;
using System.Collections;

public class GachaManager : MonoBehaviour
{
    [Header("가챠 가격")]
    [Min(1)] public int singleDrawCost = 120;
    [Min(1)] public int tenDrawCost = 1200;

    [Header("가챠 연출")]
    [Min(0f)] public float drawDelay = 0.7f;
    [Min(0f)] public float nextDrawDelay = 0.15f;

    [Header("가챠 버튼")]
    public GameObject singleDrawButton;
    public GameObject tenDrawButton;

    [Header("가챠 가격 UI")]
    public GameObject singlePriceText;
    public GameObject tenPriceText;

    [Header("UI")]
    public TMP_Text fameText;
    public TMP_Text resultText;

    [Header("테스트 기능")]
    public int testRewardAmount = 1200;

    // 테스트 명성은 저장하지 않음
    private int testRenown = 0;

    private bool isDrawing = false;
    private Coroutine drawCoroutine;

    private int lastDisplayedRenown = -1;

    private void Start()
    {
        isDrawing = false;
        testRenown = 0;

        SetGachaUI(true);
        SetResult("");

        UpdateUI(true);
    }

    private void Update()
    {
        // 명성이 바뀌었을 때만 UI 갱신
        UpdateUI();
    }

    // 실제 명성 + 테스트 명성
    public int GetTotalRenown()
    {
        int realRenown = Mathf.Max(
            0,
            CurrencyManager.GetRenown()
        );

        long total = (long)realRenown + testRenown;

        return (int)Mathf.Min(total, int.MaxValue);
    }

    // 테스트 명성 지급 버튼
    public void AddTestRenown()
    {
        if (isDrawing)
            return;

        if (testRewardAmount <= 0)
            return;

        if (testRenown > int.MaxValue - testRewardAmount)
        {
            Debug.LogWarning("테스트 명성 최대치 초과");
            return;
        }

        testRenown += testRewardAmount;

        UpdateUI(true);

        Debug.Log(
            "테스트 명성 지급: +" + testRewardAmount
        );
    }

    // 1회 뽑기
    public void DrawOne()
    {
        StartDraw(1, singleDrawCost);
    }

    // 10회 뽑기
    public void DrawTen()
    {
        StartDraw(10, tenDrawCost);
    }

    // 가챠 시작 처리
    private void StartDraw(int count, int cost)
    {
        // 가챠 진행 중이면 실행 금지
        if (isDrawing)
            return;

        if (cost <= 0)
            return;

        if (GetTotalRenown() < cost)
        {
            SetResult("명성이 부족합니다!");
            return;
        }

        // 가챠 시작 즉시 잠금
        isDrawing = true;

        // 버튼과 가격 즉시 숨기기
        SetGachaUI(false);

        // 명성 차감
        if (!TrySpendRenown(cost))
        {
            isDrawing = false;
            SetGachaUI(true);
            SetResult("명성 차감에 실패했습니다!");
            return;
        }

        UpdateUI(true);

        drawCoroutine = StartCoroutine(DrawCoroutine(count));
    }

    // 명성 차감
    private bool TrySpendRenown(int amount)
    {
        if (amount <= 0)
            return false;

        if (GetTotalRenown() < amount)
            return false;

        int useTest = Mathf.Min(testRenown, amount);
        int useReal = amount - useTest;

        if (useReal > 0)
        {
            if (!RenownControl.Spend(useReal))
            {
                Debug.LogError("실제 명성 차감 실패!");
                return false;
            }
        }

        testRenown -= useTest;

        UpdateUI(true);

        return true;
    }

    // 가챠 연출
    private IEnumerator DrawCoroutine(int count)
    {
        SetResult("");

        for (int i = 0; i < count; i++)
        {
            int star = GetRandomStar();

            if (count == 1)
            {
                SetResult($"{star}성 획득!");
            }
            else
            {
                SetResult($"{star}성");
            }

            // 게임이 일시정지되어도 연출 진행
            yield return new WaitForSecondsRealtime(
                drawDelay
            );

            // 10회 뽑기일 때만 중간 결과 제거
            if (count > 1)
            {
                SetResult("");

                if (i < count - 1)
                {
                    yield return new WaitForSecondsRealtime(
                        nextDrawDelay
                    );
                }
            }
        }

        // 종료 처리
        FinishDraw();
    }

    // 가챠 종료
    private void FinishDraw()
    {
        drawCoroutine = null;

        // 모든 가챠 연출이 끝난 후 잠금 해제
        isDrawing = false;

        // 버튼과 가격 다시 표시
        SetGachaUI(true);

        UpdateUI(true);
    }

    // 가챠 버튼과 가격 표시
    private void SetGachaUI(bool show)
    {
        if (singleDrawButton != null)
            singleDrawButton.SetActive(show);

        if (tenDrawButton != null)
            tenDrawButton.SetActive(show);

        if (singlePriceText != null)
            singlePriceText.SetActive(show);

        if (tenPriceText != null)
            tenPriceText.SetActive(show);
    }

    // 확률
    private int GetRandomStar()
    {
        float random = Random.Range(0f, 100f);

        // 3성 5%
        if (random < 5f)
            return 3;

        // 2성 25%
        if (random < 30f)
            return 2;

        // 1성 70%
        return 1;
    }

    // 결과 텍스트
    private void SetResult(string message)
    {
        if (resultText != null)
        {
            resultText.text = message;
        }
    }

    // 명성 UI 갱신
    private void UpdateUI(bool force = false)
    {
        int currentRenown = GetTotalRenown();

        if (!force && currentRenown == lastDisplayedRenown)
            return;

        lastDisplayedRenown = currentRenown;

        if (fameText != null)
        {
            fameText.text = "명성 : " + currentRenown;
        }
    }

    // 오브젝트 비활성화 대응
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
