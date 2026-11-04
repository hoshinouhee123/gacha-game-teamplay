
using UnityEngine;
using System;
using System.Reflection;

public class RenownControl : MonoBehaviour
{
    [Header("명성 전체 삭제 버튼")]
    public UnityEngine.UI.Button resetButton;

    private static readonly string[] SpendNames =
    {
        "SpendRenown",
        "TrySpendRenown",
        "UseRenown",
        "RemoveRenown"
    };

    private static readonly string[] SetNames =
    {
        "SetRenown"
    };

    private void Start()
    {
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetAllRenown);
    }

    private void OnDestroy()
    {
        if (resetButton != null)
            resetButton.onClick.RemoveListener(ResetAllRenown);
    }

    public static bool Spend(int amount)
    {
        int before = CurrencyManager.GetRenown();

        if (amount <= 0 || before < amount)
            return false;

        return ChangeRenown(before - amount, amount);
    }

    public void ResetAllRenown()
    {
        int current = CurrencyManager.GetRenown();

        if (current == 0)
        {
            Debug.Log("명성이 이미 0입니다.");
            return;
        }

        if (current < 0)
        {
            Debug.LogError("명성이 음수입니다. 초기화를 중단합니다.");
            return;
        }

        if (ChangeRenown(0, current))
        {
            Debug.Log("명성 전체 초기화 완료!");
        }
        else
        {
            Debug.LogError(
                "명성 초기화 실패! " +
                "CurrencyManager에 사용 가능한 " +
                "차감 또는 설정 함수가 필요합니다."
            );
        }
    }

    private static bool ChangeRenown(
        int target,
        int amountToSpend)
    {
        Type type = typeof(CurrencyManager);

        BindingFlags flags =
            BindingFlags.Public |
            BindingFlags.Static;

        // 1. 기존 차감 함수 찾기
        foreach (string name in SpendNames)
        {
            MethodInfo method = type.GetMethod(
                name,
                flags,
                null,
                new Type[] { typeof(int) },
                null
            );

            if (method == null)
                continue;

            if (method.ReturnType != typeof(bool) &&
                method.ReturnType != typeof(void))
                continue;

            try
            {
                object result = method.Invoke(
                    null,
                    new object[] { amountToSpend }
                );

                if (method.ReturnType == typeof(bool) &&
                    !(bool)result)
                    return false;

                return Verify(target);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        // 2. 명성 직접 설정 함수 찾기
        foreach (string name in SetNames)
        {
            MethodInfo method = type.GetMethod(
                name,
                flags,
                null,
                new Type[] { typeof(int) },
                null
            );

            if (method == null)
                continue;

            try
            {
                method.Invoke(
                    null,
                    new object[] { target }
                );

                return Verify(target);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        Debug.LogError(
            "CurrencyManager에서 SpendRenown, " +
            "UseRenown, RemoveRenown, " +
            "SetRenown 등의 함수를 찾지 못했습니다."
        );

        return false;
    }

    private static bool Verify(int expected)
    {
        int actual = CurrencyManager.GetRenown();

        if (actual != expected)
        {
            Debug.LogError(
                "명성 변경 실패! 목표: " + expected +
                " / 실제: " + actual
            );

            return false;
        }

        return true;
    }
}
