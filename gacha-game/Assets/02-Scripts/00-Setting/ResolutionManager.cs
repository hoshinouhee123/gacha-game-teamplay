using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResolutionManager : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle windowModeToggle; // 체크 시 '창모드', 해제 시 '전체화면'

    // 중복 제거된 해상도 목록
    private List<Resolution> resolutions = new List<Resolution>();

    private void Start()
    {
        InitSettings();
    }

    private void InitSettings()
    {
        // 1. 지원하는 해상도 목록 가져오기 및 가로/세로 중복 제거
        Resolution[] allResolutions = Screen.resolutions;
        resolutions.Clear();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < allResolutions.Length; i++)
        {
            // 주사율(Hz)만 다르고 크기가 같은 해상도 필터링
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
                options.Add(allResolutions[i].width + " x " + allResolutions[i].height);

                // 현재 게임 창 해상도와 일치하는 항목 찾기
                if (allResolutions[i].width == Screen.width &&
                    allResolutions[i].height == Screen.height)
                {
                    currentResolutionIndex = resolutions.Count - 1;
                }
            }
        }

        // 2. 드롭다운 목록 추가 및 현재 해상도로 표시 (이벤트 발생 없이 값만 세팅)
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(currentResolutionIndex);
        resolutionDropdown.RefreshShownValue();

        // 3. 현재 창모드 여부 확인 후 토글 상태 세팅
        if (windowModeToggle != null)
        {
            // 현재 창모드 상태라면 토글을 켬(true)
            bool isCurrentlyWindowed = (Screen.fullScreenMode == FullScreenMode.Windowed);
            windowModeToggle.SetIsOnWithoutNotify(isCurrentlyWindowed);
        }

        // 4. UI 변경 이벤트 리스너 등록
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        if (windowModeToggle != null)
        {
            windowModeToggle.onValueChanged.AddListener(SetWindowedMode);
        }
    }

    /// <summary>
    /// 드롭다운에서 해상도를 변경했을 때 호출
    /// </summary>
    public void SetResolution(int resolutionIndex)
    {
        Resolution selectedResolution = resolutions[resolutionIndex];

        // 창모드 토글이 켜져 있으면 Windowed, 꺼져 있으면 FullScreenWindow(전체화면)
        FullScreenMode mode = (windowModeToggle != null && windowModeToggle.isOn)
            ? FullScreenMode.Windowed
            : FullScreenMode.FullScreenWindow;

        Screen.SetResolution(selectedResolution.width, selectedResolution.height, mode);
    }

    /// <summary>
    /// 창모드 토글 상태가 변경되었을 때 호출
    /// </summary>
    public void SetWindowedMode(bool isWindowed)
    {
        // 현재 드롭다운에서 선택되어 있는 해상도 가져오기
        Resolution currentResolution = resolutions[resolutionDropdown.value];

        // true = 창모드 / false = 전체화면
        FullScreenMode mode = isWindowed
            ? FullScreenMode.Windowed
            : FullScreenMode.FullScreenWindow;

        Screen.SetResolution(currentResolution.width, currentResolution.height, mode);
    }
}