
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResolutionManager : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle windowModeToggle;

    // 최소 허용 해상도
    private const int MinWidth = 1600;
    private const int MinHeight = 900;

    // PlayerPrefs 키
    private const string ResKey = "SavedResolutionIndex";
    private const string WindowKey = "SavedIsWindowed";

    private List<Resolution> resolutions = new List<Resolution>();

    private void Start()
    {
        InitSettings();
    }

    private void InitSettings()
    {
        Resolution[] allResolutions = Screen.resolutions;

        resolutions.Clear();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int defaultIndex = 0;

        for (int i = 0; i < allResolutions.Length; i++)
        {
            // 1. 최소 해상도 이하인 항목 제외
            if (allResolutions[i].width <= MinWidth ||
                allResolutions[i].height <= MinHeight)
            {
                continue;
            }

            // 2. 중복 해상도 제거
            bool isDuplicate = false;

            for (int j = 0; j < resolutions.Count; j++)
            {
                if (resolutions[j].width == allResolutions[i].width &&
                    resolutions[j].height == allResolutions[i].height)
                {
                    isDuplicate = true;
                    break;
                }
            }

            if (!isDuplicate)
            {
                resolutions.Add(allResolutions[i]);

                options.Add(
                    allResolutions[i].width + " x " +
                    allResolutions[i].height
                );

                // 현재 화면 해상도와 일치하는 기본 인덱스
                if (allResolutions[i].width == Screen.width &&
                    allResolutions[i].height == Screen.height)
                {
                    defaultIndex = resolutions.Count - 1;
                }
            }
        }

        // 사용 가능한 해상도가 없는 경우 예외 처리
        if (resolutions.Count == 0)
        {
            Debug.LogError("설정 가능한 해상도가 없습니다.");

            resolutionDropdown.interactable = false;
            return;
        }

        resolutionDropdown.interactable = true;
        resolutionDropdown.AddOptions(options);

        // 3. 저장된 해상도 불러오기
        int targetIndex = defaultIndex;

        if (PlayerPrefs.HasKey(ResKey))
        {
            targetIndex = PlayerPrefs.GetInt(ResKey);
            targetIndex = Mathf.Clamp(
                targetIndex, 0, resolutions.Count - 1
            );
        }

        // 4. 창모드 여부 불러오기
        bool isWindowed =
            Screen.fullScreenMode == FullScreenMode.Windowed;

        if (PlayerPrefs.HasKey(WindowKey))
        {
            isWindowed = PlayerPrefs.GetInt(WindowKey) == 1;
        }

        // 5. UI 초기화
        resolutionDropdown.SetValueWithoutNotify(targetIndex);
        resolutionDropdown.RefreshShownValue();

        if (windowModeToggle != null)
        {
            windowModeToggle.SetIsOnWithoutNotify(isWindowed);
        }

        // 6. 저장된 설정 적용
        ApplyResolution(targetIndex, isWindowed);

        // 7. 이벤트 등록
        resolutionDropdown.onValueChanged.AddListener(SetResolution);

        if (windowModeToggle != null)
        {
            windowModeToggle.onValueChanged.AddListener(SetWindowedMode);
        }
    }

    /// <summary>
    /// 해상도 변경
    /// </summary>
    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex < 0 ||
            resolutionIndex >= resolutions.Count)
        {
            return;
        }

        bool isWindowed =
            windowModeToggle != null && windowModeToggle.isOn;

        ApplyResolution(resolutionIndex, isWindowed);

        PlayerPrefs.SetInt(ResKey, resolutionIndex);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 창모드 변경
    /// </summary>
    public void SetWindowedMode(bool isWindowed)
    {
        if (resolutions.Count == 0)
            return;

        int currentResolutionIndex = resolutionDropdown.value;

        ApplyResolution(currentResolutionIndex, isWindowed);

        PlayerPrefs.SetInt(WindowKey, isWindowed ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 실제 해상도 적용
    /// </summary>
    private void ApplyResolution(int resolutionIndex, bool isWindowed)
    {
        if (resolutionIndex < 0 ||
            resolutionIndex >= resolutions.Count)
        {
            return;
        }

        Resolution selectedResolution = resolutions[resolutionIndex];

        FullScreenMode mode = isWindowed
            ? FullScreenMode.Windowed
            : FullScreenMode.FullScreenWindow;

        Screen.SetResolution(
            selectedResolution.width,
            selectedResolution.height,
            mode
        );
    }
}
