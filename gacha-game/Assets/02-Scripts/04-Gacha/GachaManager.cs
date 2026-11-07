
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

    public void DrawOne()
    {
        StartDraw(1, singleDrawCost);
    }

    public void DrawTen()
    {
        StartDraw(10, tenDrawCost);
    }

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

        // 가챠 시작 즉시 잠금
        isDrawing = true;
        SetGachaUI(false);

        // 결과 미리 생성
        int[] results = new int[count];

        for (int i = 0; i < count; i++)
        {
            results[i] = GetRandomStar();
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

        // 결과를 JSON 인벤토리에 저장
        if (!InventoryStorage.AddStars(results))
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

        // 저장에 성공한 경우에만 결과 연출
        drawCoroutine = StartCoroutine(
            DrawCoroutine(results)
        );
    }

    private void CancelDraw(string message)
    {
        drawCoroutine = null;
        isDrawing = false;

        SetGachaUI(true);
        SetResult(message);
        UpdateUI(true);
    }

    private IEnumerator DrawCoroutine(int[] results)
    {
        SetResult("");

        int one = 0;
        int two = 0;
        int three = 0;

        for (int i = 0; i < results.Length; i++)
        {
            int star = results[i];

            if (star == 1) one++;
            if (star == 2) two++;
            if (star == 3) three++;

            SetResult(star + "성 획득!");

            yield return new WaitForSecondsRealtime(
                drawDelay
            );

            if (i < results.Length - 1)
            {
                SetResult("");

                yield return new WaitForSecondsRealtime(
                    nextDrawDelay
                );
            }
        }

        if (results.Length > 1)
        {
            SetResult(
                "10회 뽑기 완료!\n" +
                "1성: " + one + "개\n" +
                "2성: " + two + "개\n" +
                "3성: " + three + "개"
            );
        }

        FinishDraw();
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
        if (singleDrawButton != null)
            singleDrawButton.SetActive(show);

        if (tenDrawButton != null)
            tenDrawButton.SetActive(show);

        if (singlePriceText != null)
            singlePriceText.SetActive(show);

        if (tenPriceText != null)
            tenPriceText.SetActive(show);
    }

    private int GetRandomStar()
    {
        float random = Random.Range(0f, 100f);

        if (random < 5f)
            return 3;

        if (random < 30f)
            return 2;

        return 1;
    }

    private void SetResult(string message)
    {
        if (resultText != null)
            resultText.text = message;
    }

    private void UpdateUI(bool force = false)
    {
        int current = GetTotalRenown();

        if (!force && current == lastDisplayedRenown)
            return;

        lastDisplayedRenown = current;

        if (fameText != null)
            fameText.text = "명성 : " + current;
    }

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
