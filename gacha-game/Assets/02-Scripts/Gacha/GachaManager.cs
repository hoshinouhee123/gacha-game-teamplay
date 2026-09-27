using UnityEngine;
using TMPro;
using System.Collections;

public class GachaManager : MonoBehaviour
{
    [Header("명성")]
    public int fame = 0;

    [Header("가챠 가격")]
    public int singleDrawCost = 120;
    public int tenDrawCost = 1200;

    [Header("10뽑 연출")]
    public float drawDelay = 0.7f;

    [Header("UI")]
    public TMP_Text fameText;
    public TMP_Text resultText;

    private bool isDrawing = false;

    private void Start()
    {
        UpdateUI();
    }

    // 1회 뽑기
    public void DrawOne()
    {
        if (isDrawing)
            return;

        if (fame < singleDrawCost)
        {
            resultText.text = "명성이 부족합니다!";
            return;
        }

        fame -= singleDrawCost;

        int star = GetRandomStar();

        resultText.text = $"{star}성 획득!";

        UpdateUI();
    }

    // 10회 뽑기
    public void DrawTen()
    {
        if (isDrawing)
            return;

        if (fame < tenDrawCost)
        {
            resultText.text = "명성이 부족합니다!";
            return;
        }

        fame -= tenDrawCost;
        UpdateUI();

        StartCoroutine(DrawTenCoroutine());
    }

    private IEnumerator DrawTenCoroutine()
    {
        isDrawing = true;

        resultText.text = "";

        for (int i = 0; i < 10; i++)
        {
            int star = GetRandomStar();

            // 이번 결과 하나만 표시
            resultText.text = $"{star}성";

            // 0.7초 동안 보여주기
            yield return new WaitForSeconds(drawDelay);

            // 지우기
            resultText.text = "";

            // 다음 결과 나오기 전 약간 텀
            yield return new WaitForSeconds(0.15f);
        }

        isDrawing = false;
    }

    // 등급 확률
    private int GetRandomStar()
    {
        float random = Random.Range(0f, 100f);

        // 3성 5%
        if (random < 5f)
        {
            return 3;
        }

        // 2성 25%
        if (random < 30f)
        {
            return 2;
        }

        // 1성 70%
        return 1;
    }

    public void AddFame(int amount)
    {
        fame += amount;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (fameText != null)
        {
            fameText.text = $"명성 : {fame}";
        }
    }
}