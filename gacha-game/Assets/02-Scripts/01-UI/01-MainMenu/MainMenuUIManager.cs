using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUIManager : MonoBehaviour
{
    public Button stageButton;

    [Header("스탠딩 일러스트 UI")]
    public Button standingCharacter;
    public Image speechBubble;          // 말풍선 이미지
    public TextMeshProUGUI bubbleText;  // 말풍선 안에 담길 텍스트

    public float fadeDuration = 1.0f;   // 페이드 인, 아웃 걸리는 시간

    [SerializeField] private StandingDialogue standingDialogue;

    private bool standingClicked;       // 스탠딩 일러스트 클릭 여부

    void Start()
    {
        standingClicked = false;

        // 시작할 때 말풍선 이미지와 텍스트 둘 다 투명(0)으로 초기화
        SetAlpha(0f);
    }

    public void ClickStageBT()
    {
        if (stageButton != null)
        {
            SceneManager.LoadScene("StageScene");
        }
    }

    public void ClickGachaBT()
    {
        SceneManager.LoadScene("GachaScene");
    }

    public void ClickStandingCharacter()
    {
        if (standingCharacter != null && !standingClicked)
        {
            standingClicked = true;

            string text = standingDialogue.GetRandomMenuDialogue();
            bubbleText.text = text;

            // 말풍선 페이드 연출 코루틴 실행
            StartCoroutine(ShowSpeechBubbleRoutine());
        }
    }

    // FadeIn -> 읽는 시간 대기 -> FadeOut -> 다시 클릭 가능
    private IEnumerator ShowSpeechBubbleRoutine()
    {
        yield return StartCoroutine(SpeechBubbleFadeIn());

        yield return new WaitForSeconds(1.5f); // 1.5초 대기

        yield return StartCoroutine(SpeechBubbleFadeOut());

        standingClicked = false;
    }

    // 페이드 인 코루틴
    private IEnumerator SpeechBubbleFadeIn()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            SetAlpha(alpha); // 이미지와 텍스트 알파값 동시에 갱신
            yield return null;
        }

        SetAlpha(1f); // 확실하게 불투명(1)으로 설정
    }

    // 페이드 아웃 코루틴
    private IEnumerator SpeechBubbleFadeOut()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - (elapsedTime / fadeDuration)); // 1에서 0으로 감소
            SetAlpha(alpha); // 이미지와 텍스트 알파값 동시에 갱신
            yield return null;
        }

        SetAlpha(0f); // 확실하게 투명(0)으로 설정
    }

    private void SetAlpha(float alpha)
    {
        // 말풍선 이미지 투명도 조절
        if (speechBubble != null)
        {
            Color imgColor = speechBubble.color;
            imgColor.a = alpha;
            speechBubble.color = imgColor;
        }

        // 텍스트 투명도 조절
        if (bubbleText != null)
        {
            Color txtColor = bubbleText.color;
            txtColor.a = alpha;
            bubbleText.color = txtColor;
        }
    }
}