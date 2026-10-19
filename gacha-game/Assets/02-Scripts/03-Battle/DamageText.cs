using System.Collections;
using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    public float moveSpeed = 60f;    // 위로 떠오르는 속도
    public float lifeTime = 0.7f;     // 화면에 머무는 시간

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    public void Setup(int amount, Color textColor)
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshProUGUI>();

        textMesh.text = amount.ToString();
        textMesh.color = textColor;

        StartCoroutine(FloatAndFadeRoutine());
    }

    private IEnumerator FloatAndFadeRoutine()
    {
        float elapsed = 0f;
        Color startColor = textMesh.color;
        Vector3 startScale = transform.localScale;

        // 처음에 살짝 퐁 커졌다가 원래대로 돌아오는 팝(Pop) 효과
        transform.localScale = startScale * 1.3f;

        while (elapsed < lifeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifeTime;

            // 1. 위로 둥둥 떠오름
            transform.localPosition += Vector3.up * (moveSpeed * Time.deltaTime);

            // 2. 크기 복구
            transform.localScale = Vector3.Lerp(startScale * 1.3f, startScale, t * 3f);

            // 3. 수명의 절반 이후부터 서서히 페이드아웃
            if (t > 0.4f)
            {
                float alpha = Mathf.Lerp(1f, 0f, (t - 0.4f) / 0.6f);
                textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }

            yield return null;
        }

        Destroy(gameObject); // 애니메이션 종료 후 오브젝트 파괴
    }
}