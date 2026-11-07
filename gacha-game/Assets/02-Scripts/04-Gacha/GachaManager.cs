
using UnityEngine;
using TMPro;
using System.Collections;

public class GachaManager : MonoBehaviour
{
    [Header("가챠 가격")]
    [Min(1)] public int singleDrawCost = 120;
    [Min(1)] public int tenDrawCost = 1200;

    [Header("1성 지급 범위")]
    [Min(1)] public int oneStarMin = 30;
    [Min(1)] public int oneStarMax = 40;

    [Header("2성 지급 범위")]
    [Min(1)] public int twoStarMin = 50;
    [Min(1)] public int twoStarMax = 70;

    [Header("3성 지급 범위")]
    [Min(1)] public int threeStarMin = 70;
    [Min(1)] public int threeStarMax = 100;

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

    private int testRenown = 0;
    private bool isDrawing = false;
    private Coroutine drawCoroutine;
    private int lastDisplayedRenown = -1;

    private void Start()
    {
        InventoryStorage.Load();

        testRenown = 0;
        isDrawing = false;

        SetGachaUI(true);
        SetResult("");
        UpdateUI(true);
    }

    private void Update()
    {
        UpdateUI();
    }

    // 실제 명성 + 테스트 명성
    public int GetTotalRenown()
    {
        int real = Mathf.Max(
            0,
            CurrencyManager.GetRenown()
        );

        long total = (long)real + testRenown;

        return (int)Mathf.Min(total, int.MaxValue);
    }

    // 테스트 명성 추가
    public void AddTestRenown()
    {
        if (isDrawing || testRewardAmount <= 0)
            return;

        if (testRenown > int.MaxValue - testRewardAmount)
            return;

        testRenown += testRewardAmount;

        UpdateUI(true);
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

    // 가챠 시작
    private void StartDraw(int count, int cost)
    {
        if (isDrawing)
            return;

        if (cost <= 0)
            return;

        if (GetTotalRenown() < cost)
        {
            SetResult("명성이 부족합니다!");
            return;
        }

        // 중복 실행 방지
        isDrawing = true;

        // 가챠 버튼과 가격 숨기기
        SetGachaUI(false);

        // 등급과 지급 수량 저장
        int[] stars = new int[count];
        int[] amounts = new int[count];

        for (int i = 0; i < count; i++)
        {
            int star = GetRandomStar();

            stars[i] = star;
            amounts[i] = GetRewardAmount(star);
        }

        // 테스트 명성 우선 차감
        int useTest = Mathf.Min(testRenown, cost);
        int useReal = cost - useTest;

        if (useReal > 0)
        {
            if (!RenownControl.Spend(useReal))
            {
                CancelDraw("명성 차감 실패!");
                return;
            }
        }

        testRenown -= useTest;

        // JSON 인벤토리에 지급량 저장
        if (!InventoryStorage.AddRewards(stars, amounts))
        {
            testRenown += useTest;

            // 실제 명성 환불 시도
            if (useReal > 0)
            {
                int before = CurrencyManager.GetRenown();

                CurrencyManager.AddRenown(useReal);

                int after = CurrencyManager.GetRenown();

                if ((long)after != (long)before + useReal)
                {
                    Debug.LogError(
                        "가챠 저장 실패 후 명성 환불도 실패했습니다."
                    );
                }
            }

            CancelDraw("인벤토리 저장 실패!");
            return;
        }

        UpdateUI(true);

        // 가챠 연출 시작
        drawCoroutine = StartCoroutine(
            DrawCoroutine(stars, amounts)
        );
    }

    // 등급별 랜덤 지급 수량
    private int GetRewardAmount(int star)
    {
        int min;
        int max;

        switch (star)
        {
            case 1:
                min = oneStarMin;
                max = oneStarMax;
                break;

            case 2:
                min = twoStarMin;
                max = twoStarMax;
                break;

            case 3:
                min = threeStarMin;
                max = threeStarMax;
                break;

            default:
                return 1;
        }

        min = Mathf.Max(1, min);
        max = Mathf.Max(min, max);

        // int Random.Range의 최대값은 미포함
        // max + 1로 최대 지급량도 포함
        return Random.Range(min, max + 1);
    }

    // 가챠 연출
    private IEnumerator DrawCoroutine(
        int[] stars,
        int[] amounts)
    {
        SetResult("");

        long oneTotal = 0;
        long twoTotal = 0;
        long threeTotal = 0;

        for (int i = 0; i < stars.Length; i++)
        {
            int star = stars[i];
            int amount = amounts[i];

            switch (star)
            {
                case 1:
                    oneTotal += amount;
                    break;

                case 2:
                    twoTotal += amount;
                    break;

                case 3:
                    threeTotal += amount;
                    break;
            }

            // 현재 뽑기 결과와 지급 수량 표시
            SetResult(
                star + "성 획득!\n" +
                "획득 개수: " + amount + "개"
            );

            yield return new WaitForSecondsRealtime(
                drawDelay
            );

            if (i < stars.Length - 1)
            {
                SetResult("");

                yield return new WaitForSecondsRealtime(
                    nextDrawDelay
                );
            }
        }

        // 10회 뽑기 결과 요약
        if (stars.Length > 1)
        {
            SetResult(
                "10회 뽑기 완료!\n" +
                "1성: " + oneTotal + "개\n" +
                "2성: " + twoTotal + "개\n" +
                "3성: " + threeTotal + "개"
            );
        }

        FinishDraw();
    }

    // 가챠 취소
    private void CancelDraw(string message)
    {
        drawCoroutine = null;
        isDrawing = false;

        SetGachaUI(true);
        SetResult(message);
        UpdateUI(true);
    }

    // 가챠 종료
    private void FinishDraw()
    {
        drawCoroutine = null;
        isDrawing = false;

        SetGachaUI(true);
        UpdateUI(true);
    }

    // 버튼 및 가격 표시
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

    // 가챠 확률
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
        int current = GetTotalRenown();

        if (!force && current == lastDisplayedRenown)
            return;

        lastDisplayedRenown = current;

        if (fameText != null)
        {
            fameText.text = "명성 : " + current;
        }
    }

    // 오브젝트 비활성화 시 처리
    private void OnDisable()
    {
        if (drawCoroutine != null)
        {
            StopCoroutine(drawCoroutine);
            drawCoroutine = null;
        }

        isDrawing = false;
    }
}
