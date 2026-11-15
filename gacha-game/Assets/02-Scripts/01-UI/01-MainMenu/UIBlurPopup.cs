
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIBlurPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage blurBackground;
    [SerializeField] private GameObject popupPanel;

    [Header("Blur")]
    [SerializeField] private Material blurMaterial;

    [Range(1, 4)]
    [SerializeField] private int downsample = 2;

    [Range(1, 6)]
    [SerializeField] private int iterations = 3;

    [Range(0.5f, 5f)]
    [SerializeField] private float blurSize = 1.5f;

    private RenderTexture blurTexture;
    private Coroutine openingCoroutine;

    private void Awake()
    {
        blurBackground.gameObject.SetActive(false);
        popupPanel.SetActive(false);
    }

    public void Open()
    {
        if (openingCoroutine != null)
            return;

        if (blurMaterial == null)
        {
            Debug.LogError("Blur Material이 없습니다.");
            return;
        }

        openingCoroutine = StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        // 캡처에 팝업이 포함되지 않도록 숨김
        blurBackground.gameObject.SetActive(false);
        popupPanel.SetActive(false);

        // 모든 카메라와 UI 렌더링이 끝날 때까지 대기
        yield return new WaitForEndOfFrame();

        Texture2D screenshot =
            ScreenCapture.CaptureScreenshotAsTexture();

        int width = Mathf.Max(1, screenshot.width / downsample);
        int height = Mathf.Max(1, screenshot.height / downsample);

        ReleaseBlur();

        blurTexture = new RenderTexture(
            width, height, 0, RenderTextureFormat.ARGB32);

        blurTexture.filterMode = FilterMode.Bilinear;
        blurTexture.wrapMode = TextureWrapMode.Clamp;
        blurTexture.Create();

        RenderTexture a = RenderTexture.GetTemporary(
            width, height, 0, RenderTextureFormat.ARGB32);

        RenderTexture b = RenderTexture.GetTemporary(
            width, height, 0, RenderTextureFormat.ARGB32);

        a.filterMode = FilterMode.Bilinear;
        b.filterMode = FilterMode.Bilinear;

        Graphics.Blit(screenshot, a);

        blurMaterial.SetFloat("_BlurSize", blurSize);

        // 반복할수록 블러가 강해짐
        for (int i = 0; i < iterations; i++)
        {
            Graphics.Blit(a, b, blurMaterial, 0);

            RenderTexture temp = a;
            a = b;
            b = temp;
        }

        Graphics.Blit(a, blurTexture);

        RenderTexture.ReleaseTemporary(a);
        RenderTexture.ReleaseTemporary(b);

        Destroy(screenshot);

        // 블러 적용된 화면 표시
        blurBackground.texture = blurTexture;
        blurBackground.color = Color.white;

        blurBackground.gameObject.SetActive(true);
        popupPanel.SetActive(true);

        openingCoroutine = null;
    }

    public void Close()
    {
        if (openingCoroutine != null)
        {
            StopCoroutine(openingCoroutine);
            openingCoroutine = null;
        }

        popupPanel.SetActive(false);
        blurBackground.gameObject.SetActive(false);

        ReleaseBlur();
    }

    private void ReleaseBlur()
    {
        if (blurBackground != null)
            blurBackground.texture = null;

        if (blurTexture != null)
        {
            blurTexture.Release();
            Destroy(blurTexture);
            blurTexture = null;
        }
    }

    private void OnDestroy()
    {
        ReleaseBlur();
    }
}
