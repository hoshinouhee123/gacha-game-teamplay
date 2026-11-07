
using UnityEngine;
using UnityEngine.UI;

public class InventoryResetButton : MonoBehaviour
{
    [Header("인벤토리 전체 삭제 버튼")]
    public Button resetButton;

    private void OnEnable()
    {
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetInventory);
        }
    }

    private void OnDisable()
    {
        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetInventory);
        }
    }

    public void ResetInventory()
    {
        if (InventoryStorage.ClearInventory())
        {
            Debug.Log("테스트 인벤토리 초기화 성공");
        }
        else
        {
            Debug.LogError("테스트 인벤토리 초기화 실패");
        }
    }
}
