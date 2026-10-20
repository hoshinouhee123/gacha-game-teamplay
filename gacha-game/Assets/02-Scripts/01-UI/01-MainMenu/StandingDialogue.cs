using UnityEngine;

public class StandingDialogue : MonoBehaviour
{

    [SerializeField] private BattleUnit battleUnit;



    public string GetRandomMenuDialogue()
    {
        // null 또는 비어있는지 체크 (.Length 사용)
        if (battleUnit.mainMenuDialogue == null || battleUnit.mainMenuDialogue.Length == 0)
        {
            Debug.LogWarning($"[{name}] 대사 목록이 비어 있습니다.");
            return string.Empty;
        }

        // Length를 사용하여 랜덤 인덱스 추출
        int randomIndex = Random.Range(0, battleUnit.mainMenuDialogue.Length);

        return battleUnit.mainMenuDialogue[randomIndex];
    }
}
