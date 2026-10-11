using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResolutionManager : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle windowModeToggle; // 체크 시 '창모드', 해제 시 '전체화면'

    // PlayerPrefs에 사용할 키 이름
    private const string ResKey = "SavedResolutionIndex";
    private const string WindowKey = "SavedIsWindowed";

    private List<Resolution> resolutions = new List<Resolution>();

    private void Start()
    {
        InitSettings();
    }

    private void InitSettings()
    {
        // 1. 지원하는 해상도 목록 가져오기 및 중복 제거
        Resolution[] allResolutions = Screen.resolutions;
        resolutions.Clear();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int defaultIndex = 0;

        for (int i = 0; i < allResolutions.Length; i++)
        {
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

                // 현재 화면 해상도와 일치하는 기본 인덱스 탐색
                if (allResolutions[i].width == Screen.width &&
                    allResolutions[i].height == Screen.height)
                {
                    defaultIndex = resolutions.Count - 1;
                }
            }
        }

        resolutionDropdown.AddOptions(options);

        // 2. PlayerPrefs에서 저장된 값 불러오기
        // (저장된 값이 있으면 불러오고, 없으면 위에서 찾은 기본값 사용)
        int targetIndex = defaultIndex;
        if (PlayerPrefs.HasKey(ResKey))
        {
            targetIndex = PlayerPrefs.GetInt(ResKey);
            // 해상도 목록 범위를 벗어나지 않도록 안전 제한
            targetIndex = Mathf.Clamp(targetIndex, 0, resolutions.Count - 1);
        }

        // 창모드 여부 불러오기 (1: 창모드, 0: 전체화면)
        bool isWindowed = (Screen.fullScreenMode == FullScreenMode.Windowed);
        if (PlayerPrefs.HasKey(WindowKey))
        {
            isWindowed = PlayerPrefs.GetInt(WindowKey) == 1;
        }

        // 3. UI에 표시 세팅 (이벤트 발생 없이 값만 세팅)
        resolutionDropdown.SetValueWithoutNotify(targetIndex);
        resolutionDropdown.RefreshShownValue();

        if (windowModeToggle != null)
        {
            windowModeToggle.SetIsOnWithoutNotify(isWindowed);
        }

        // 4. 저장되어 있던 설정값으로 실제 게임 화면 즉시 적용
        ApplyResolution(targetIndex, isWindowed);

        // 5. 사용자가 바꿀 때 감지할 이벤트 리스너 등록
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        if (windowModeToggle != null)
        {
            windowModeToggle.onValueChanged.AddListener(SetWindowedMode);
        }
    }

    /// <summary>
    /// 드롭다운 선택 시 실행 & 저장
    /// </summary>
    public void SetResolution(int resolutionIndex)
    {
        bool isWindowed = (windowModeToggle != null && windowModeToggle.isOn);
        ApplyResolution(resolutionIndex, isWindowed);

        // PlayerPrefs에 저장
        PlayerPrefs.SetInt(ResKey, resolutionIndex);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 창모드 토글 누를 때 실행 & 저장
    /// </summary>
    public void SetWindowedMode(bool isWindowed)
    {
        int currentResolutionIndex = resolutionDropdown.value;
        ApplyResolution(currentResolutionIndex, isWindowed);

        // PlayerPrefs에 저장 (true는 1, false는 0)
        PlayerPrefs.SetInt(WindowKey, isWindowed ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 실제 화면 해상도 적용 함수
    /// </summary>
    private void ApplyResolution(int resolutionIndex, bool isWindowed)
    {
        Resolution selectedResolution = resolutions[resolutionIndex];
        FullScreenMode mode = isWindowed ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;

        Screen.SetResolution(selectedResolution.width, selectedResolution.height, mode);
    }
}