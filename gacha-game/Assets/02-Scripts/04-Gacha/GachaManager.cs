
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class GachaManager : MonoBehaviour
{
    [Header("가챠 가격")]
    public int singleDrawCost = 120;
    public int tenDrawCost = 1200;

    [Header("10뽑 연출")]
    public float drawDelay = 0.7f;

    [Header("가챠 버튼")]
    public GameObject singleDrawButton;
    public GameObject tenDrawButton;

    [Header("가챠 가격 UI")]
    public GameObject singlePriceText;
    public GameObject tenPriceText;

    [Header("UI")]
    public TMP_Text fameText;
    public TMP_Text resultText;

    private bool isDrawing = false;

    private void Start()
    {
        UpdateUI();
        SetGachaUI(true);
    }

    // 1회 뽑기
    public void DrawOne()
    {
        if (isDrawing)
            return;

        int fame = CurrencyManager.GetRenown();

        if (fame < singleDrawCost)
        {
            resultText.text = "명성이 부족합니다!";
            return;
        }

        // 명성 차감
        CurrencyManager.AddRenown(-singleDrawCost);

        StartCoroutine(DrawOneCoroutine());
    }

    private IEnumerator DrawOneCoroutine()
    {
        isDrawing = true;
        SetGachaUI(false);

        resultText.text = "";

        int star = GetRandomStar();

        resultText.text = $"{star}성 획득!";

        UpdateUI();

        yield return new WaitForSeconds(drawDelay);

        isDrawing = false;
        SetGachaUI(true);
    }

    // 10회 뽑기
    public void DrawTen()
    {
        if (isDrawing)
            return;

        int fame = CurrencyManager.GetRenown();

        if (fame < tenDrawCost)
        {
            resultText.text = "명성이 부족합니다!";
            return;
        }

        // 명성 차감
        CurrencyManager.AddRenown(-tenDrawCost);

        StartCoroutine(DrawTenCoroutine());
    }

    private IEnumerator DrawTenCoroutine()
    {
        isDrawing = true;
        SetGachaUI(false);

        resultText.text = "";
        UpdateUI();

        for (int i = 0; i < 10; i++)
        {
            int star = GetRandomStar();

            resultText.text = $"{star}성";

            yield return new WaitForSeconds(drawDelay);

            resultText.text = "";

            yield return new WaitForSeconds(0.15f);
        }

        isDrawing = false;
        SetGachaUI(true);
    }

    // 버튼 및 가격 표시 설정
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

    // 등급 확률
    private int GetRandomStar()
    {
        float random = Random.Range(0f, 100f);

        if (random < 5f)
            return 3;

        if (random < 30f)
            return 2;

        return 1;
    }

    private void UpdateUI()
    {
        if (fameText != null)
        {
            fameText.text =
                $"명성 : {CurrencyManager.GetRenown()}";
        }
    }
}
