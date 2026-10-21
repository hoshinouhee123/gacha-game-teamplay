using System.Collections;
using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    public float moveSpeed = 80f;     // 위로 떠오르는 속도
    public float lifeTime = 0.9f;     // 유지 시간 (넉넉하게 0.9초)

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    public void Setup(int amount, Color textColor)
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshProUGUI>();

        textMesh.text = amount.ToString();
        // 알파값을 100%(1.0f)로 강제 고정하여 또렷하게 만듦
        textMesh.color = new Color(textColor.r, textColor.g, textColor.b, 1f);

        StartCoroutine(FloatAndFadeRoutine());
    }

    private IEnumerator FloatAndFadeRoutine()
    {
        float elapsed = 0f;
        Color startColor = textMesh.color;
        Vector3 startScale = Vector3.one;

        // 시작할 때 1.4배로 팡 튀어오름
        transform.localScale = startScale * 1.4f;

        while (elapsed < lifeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifeTime;

            // 1. 화면 위로 둥둥 떠오름
            transform.position += Vector3.up * (moveSpeed * Time.deltaTime);

            // 2. 크기는 원래대로 복구
            transform.localScale = Vector3.Lerp(startScale * 1.4f, startScale, t * 4f);

            // 수명의 70%가 지날 때까지는 알파값 100% 유지 (흐려짐 방지)
            if (t > 0.7f)
            {
                float alpha = Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);
                textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}