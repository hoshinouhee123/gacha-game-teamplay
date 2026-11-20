using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleLoadingUI : MonoBehaviour
{
    [Header("이동할 씬")]
    [SerializeField] private string mainSceneName = "MainMenuScene";

    [Header("로딩 연출 시간")]
    [SerializeField, Range(0.1f, 9.5f)] private float minLoadingSeconds = 3f;
    [SerializeField, Range(0.1f, 9.5f)] private float maxLoadingSeconds = 8f;

    [Header("타이틀 UI")]
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TMP_Text loadingPercentText;
    [SerializeField] private Button startButton;

    private bool isReady;
    private bool isEnteringMainScene;

    private void Start()
    {
        if (startButton != null)
            startButton.gameObject.SetActive(false);

        if (loadingBar != null)
        {
            loadingBar.gameObject.SetActive(true);
            loadingBar.interactable = false;
        }

        if (loadingPercentText != null)
            loadingPercentText.gameObject.SetActive(true);

        SetProgress(0);
        StartCoroutine(PlayLoadingAnimation());
    }

    private IEnumerator PlayLoadingAnimation()
    {
        float min = Mathf.Clamp(minLoadingSeconds, 0.1f, 9.5f);
        float max = Mathf.Clamp(maxLoadingSeconds, min, 9.5f);
        float duration = Random.Range(min, max);

        float startTime = Time.realtimeSinceStartup;
        float nextUpdateTime = startTime + Random.Range(0.08f, 0.25f);
        int displayedPercent = 0;

        while (true)
        {
            float now = Time.realtimeSinceStartup;
            float elapsed = now - startTime;

            if (elapsed >= duration)
                break;

            if (now >= nextUpdateTime)
            {
                int targetPercent = Mathf.FloorToInt(elapsed / duration * 100f);
                int offset = Random.Range(-3, 5);

                displayedPercent = Mathf.Clamp(
                    targetPercent + offset,
                    displayedPercent,
                    99
                );

                SetProgress(displayedPercent);
                nextUpdateTime = now + Random.Range(0.08f, 0.30f);
            }

            yield return null;
        }

        SetProgress(100);
        yield return null;

        if (loadingPercentText != null)
            loadingPercentText.gameObject.SetActive(false);

        if (loadingBar != null)
            loadingBar.gameObject.SetActive(false);

        isReady = true;

        if (startButton != null)
            startButton.gameObject.SetActive(true);
    }

    private void SetProgress(int percent)
    {
        if (loadingPercentText != null)
            loadingPercentText.text = $"{percent}%";

        if (loadingBar != null)
            loadingBar.normalizedValue = percent / 100f;
    }

    public void EnterMainScene()
    {
        if (!isReady || isEnteringMainScene)
            return;

        if (string.IsNullOrWhiteSpace(mainSceneName) ||
            !Application.CanStreamedLevelBeLoaded(mainSceneName))
        {
            Debug.LogError(
                $"'{mainSceneName}' 씬을 열 수 없습니다. Build Profiles의 Scene List를 확인하세요.",
                this
            );
            return;
        }

        isEnteringMainScene = true;

        if (startButton != null)
            startButton.interactable = false;

        SceneManager.LoadSceneAsync(mainSceneName);
    }
}