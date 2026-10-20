using System.Collections;
using UnityEngine;

public class EnvironmentDimmer : MonoBehaviour
{
    public static EnvironmentDimmer Instance;

    [Header("Á¶¸í ¿¬°á")]
    public Light directionalLight; // ¾ÀÀÇ Directional Light

    [Header("¹à±â ¼³Á¤")]
    public float normalIntensity = 1.0f; // Æò¼Ò ±âº» ¹à±â
    public float dimmedIntensity = 0.2f; // Å¸°ÙÆÃ ½Ã ¾îµÎ¿öÁú ¹à±â

    private Coroutine dimCoroutine;

    private void Awake()
    {
        Instance = this;
        if (directionalLight == null)
            directionalLight = FindFirstObjectByType<Light>();
    }

    // ¾ÏÀü ÄÑ°í ²ô±â (enable: true¸é ¾îµÎ¿öÁü, false¸é ¹à¾ÆÁü)
    public void SetDim(bool enable, float duration = 0.35f)
    {
        if (directionalLight == null) return;

        float targetIntensity = enable ? dimmedIntensity : normalIntensity;
        if (dimCoroutine != null) StopCoroutine(dimCoroutine);
        dimCoroutine = StartCoroutine(DimLightRoutine(targetIntensity, duration));
    }

    private IEnumerator DimLightRoutine(float targetVal, float duration)
    {
        float startVal = directionalLight.intensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            directionalLight.intensity = Mathf.Lerp(startVal, targetVal, t);
            yield return null;
        }

        directionalLight.intensity = targetVal;
    }
}