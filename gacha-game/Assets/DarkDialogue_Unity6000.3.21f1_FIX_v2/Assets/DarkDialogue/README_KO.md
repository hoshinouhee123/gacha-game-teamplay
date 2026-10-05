# Dark Dialogue — Unity 6000.3.21f1

림버스에서 영감을 받은 검정·황동색 대화 UI입니다. 자체 제작한 UI와 임시 실루엣을 포함하며 원작 이미지·음원은 포함하지 않습니다.

## 바로 실행

**ZIP 직접 설치판:** `DarkDialogue_Unity6000.3.21f1_FIX_v2.zip`을 압축 해제하고, 그 안의 `Assets/DarkDialogue` 폴더와 `Assets/DarkDialogue.meta`를 내 프로젝트의 `Assets` 폴더에 복사하세요. Unity의 가져오기 창에서 `PackageImportTreeView` 오류가 발생한 경우에도 해당 창을 거치지 않고 파일을 설치할 수 있습니다. 설치 후 아래 2번부터 진행하세요.

**이미 이전 ZIP을 설치한 경우:** 새 ZIP의 `Assets/DarkDialogue/Runtime` 폴더만 기존 프로젝트의 같은 위치에 덮어쓰면 입력 호환성 수정이 반영됩니다. `.meta`도 함께 복사하세요. 대사 데이터·프리팹을 다시 교체할 필요는 없습니다. v2는 Input System 네임스페이스와 타입을 직접 참조하지 않으므로 해당 패키지가 없어도 컴파일할 수 있습니다.

1. `DarkDialogue_Unity6000.3.21f1.unitypackage`를 더블클릭하거나 Unity의 **Assets → Import Package → Custom Package**로 가져옵니다. 항목을 모두 체크하고 Import 합니다.
2. Project에서 `Assets/DarkDialogue/Prefabs/DarkDialogue.prefab`을 **Hierarchy의 최상위로** 드래그합니다. 자체 Canvas를 포함하므로 기존 Canvas 안에 넣을 필요가 없습니다.
3. Play를 누르면 연결된 한글 샘플 대사가 시작됩니다. 클릭 한 번은 현재 대사 전체 표시, 다음 클릭은 다음 대사입니다. 선택지에서는 버튼을 클릭합니다.

샘플만 보려면 **Tools → Dark Dialogue → Open Demo Scene**을 선택하고 Play를 누르세요. 데모 씬에는 Camera와 AudioListener가 있습니다.

## 내 대사로 바꾸기

Project에서 우클릭 → **Create → Dark Dialogue → Dialogue Sequence**로 대사 데이터를 만듭니다. 씬에 놓은 프리팹의 `DialogueManager → Sequence`에 연결하세요. `Lines` 목록에 대사를 추가합니다.

| 필드 | 설정 |
|---|---|
| Speaker | 이름창에 표시할 화자. 빈 값이면 이름 없이 진행 |
| Side | Narration = 두 인물 밝게, Left/Right = 해당 인물 강조 |
| Text | 대사. 기본 크기에서는 한 번에 2~4줄 권장 |
| Character Interval | 글자 사이 간격(초). 0이면 즉시 표시 |
| Auto Delay | AUTO에서 대사 전체 출력 후 기다리는 시간 |
| Next Line | -2: 다음 줄, -1: 대화 종료, 0 이상: 해당 번호로 이동 |
| Choices | 선택지 목록. Label에 문구, Next Line에 이동할 번호 입력 |
| Update Left / Right | 체크할 때만 해당 캐릭터 이미지를 변경 |
| Left / Right Portrait | 표정 포함 캐릭터 Sprite. Update 체크 + None이면 숨김 |
| Update Background | 체크하면 Background Sprite로 교체. None이면 배경 이미지 숨김 |
| Update Bgm | 체크하면 BGM 변경. Bgm이 None이면 음악 중지 |
| Sound Effect | 해당 줄이 시작될 때 한 번 재생할 AudioClip |
| Shake Seconds / Strength | 배경·캐릭터 화면 흔들림 시간 / 기준 해상도에서의 크기 |

**번호는 0부터 시작합니다.** Lines의 Element 0이 첫 번째 대사입니다. 선택지 뒤의 대사는 자동으로 분기에서 제외되지 않으므로 각 분기의 마지막 줄에 합류할 번호나 -1을 지정하세요. 샘플은 3번 줄에서 4번/6번으로 분기한 후 7번에서 합류합니다.

AUTO는 출력 완료 후 자동으로 넘깁니다. SKIP은 빠르게 진행하면서 각 줄의 이벤트를 실행하며 **선택지에서 멈춥니다.** LOG는 대화 이력을 표시하고 출력·AUTO·SKIP을 잠시 정지합니다. 길어진 로그와 선택지는 휠 또는 드래그로 스크롤할 수 있습니다. LOG의 PREVIOUS/NEXT는 3개 대사 단위로 이동합니다.

## 캐릭터·UI 수정

프리팹을 더블클릭하면 실제 UI 계층을 편집할 수 있습니다.

- `Presentation/Stage/CharacterLeft`, `CharacterRight`: Image의 Source Image를 내 Sprite로 교체합니다.
- `Presentation/Stage/BackgroundImage`: 배경 Sprite를 교체합니다.
- `Presentation/DialogueBox`: 대화창 크기·색, SpeakerName/DialogueText의 글자 크기를 조정합니다.
- `Presentation/Controls`: AUTO/SKIP/LOG 버튼입니다.
- `Presentation/ChoiceViewport/ChoicePanel/ChoiceTemplate`: 선택지 디자인 원본입니다. Template은 비활성 상태를 유지하세요.
- `Presentation/LogPanel`: 로그 화면 디자인입니다. 기본 비활성입니다.
- Sprite로 쓸 PNG는 Inspector에서 Texture Type을 **Sprite (2D and UI)**로 설정하세요.

Canvas는 Screen Space Overlay, 기준 해상도 1920×1080, Scale With Screen Size, Match 0.5입니다. 기본 배치는 가로 화면용이며 세로 모바일 화면은 UI 배치를 따로 조정하세요. 기본 Sorting Order는 100입니다.

게임 화면 위에 인물·대화창만 보이고 싶으면 `StageBackdrop`과 `BackgroundImage`를 비활성화하세요. 대사 데이터에서 Update Background를 쓰는 경우 BackgroundImage를 활성 상태로 두고 Image 컴포넌트만 끄세요.

## 게임 진행과 연결

자동 시작을 끄려면 `Play On Start`를 해제합니다. 게임 코드에서 `Begin()` 또는 `Begin(내대사데이터)`를 호출하면 시작합니다. `End()`는 대화를 종료합니다. 종료하면 대화 UI와 입력 차단이 해제되며 오브젝트는 다음 재생을 위해 유지됩니다.

```csharp
using DarkDialogue;
using UnityEngine;

public class StoryTrigger : MonoBehaviour
{
    public DialogueManager dialogue;
    public DialogueSequence story;

    public void StartStory()
    {
        dialogue.Begin(story);
    }
}
```

씬의 `DialogueManager → On Dialogue Finished`에 다음 진행용 함수를 연결하세요. `On Line Entered (int)`는 시작한 줄의 번호를 전달하므로 특정 줄에서 게임 연출을 실행할 수 있습니다. 대화 데이터의 On Enter/On Selected는 에셋을 대상으로 하는 이벤트에 사용합니다. 씬 오브젝트 연결은 Manager의 이벤트를 이용하세요. 플레이어 이동·시간·카메라는 게임마다 다르므로 게임 코드에서 멈추거나 재개해야 합니다. 타이핑과 UI는 Time.timeScale이 0이어도 진행합니다.

## 입력·한글·소리

- Unity UI(uGUI, com.unity.ugui)가 필요합니다. 일반적인 Unity 프로젝트의 UI 패키지를 사용하며 TMP Essentials는 필요하지 않습니다. Text 컴포넌트를 사용합니다.
- 빈 씬에서는 실행 시 EventSystem을 생성합니다. Input System 타입이 실제로 사용 가능한 경우 실행 시 찾아 사용하고, 기존 입력이 활성화된 프로젝트에서는 StandaloneInputModule을 사용합니다. 이미 EventSystem이 있으면 기존 설정을 사용하므로 해당 씬의 입력 모듈이 정상 설정되어 있어야 합니다. Input System은 컴파일 필수 의존성이 아닙니다.
- 프로젝트가 **New Input만 활성화**했는데 실제 Input System 패키지가 없는 경우에는 입력을 받을 수 없습니다. 이 경우 **Edit → Project Settings → Player → Other Settings → Active Input Handling**을 **Both**로 바꾸고 Unity가 재시작을 요청하면 재시작하세요. 시스템은 프로젝트 설정을 자동 변경하지 않습니다.
- 클릭/터치가 기본 조작입니다. Space/Enter는 다른 UI에 포커스가 없을 때만 다음 대사로 진행합니다.
- Windows에서는 맑은 고딕 등 설치된 한글 시스템 폰트를 자동으로 사용합니다. **폰트 파일은 포함하지 않습니다.** Android/iOS/WebGL 또는 시스템 한글 폰트가 없는 환경으로 빌드할 때는 사용할 수 있는 한글 TTF/OTF를 프로젝트에 넣고 `Override Font`에 연결하세요. `Override Font`는 시스템 폰트보다 우선합니다. Prefab Mode의 기본 미리보기 텍스트는 영어이며 Play에서는 한글 샘플 대사가 표시됩니다.
- BGM/SFX는 AudioClip을 직접 연결해야 합니다. 씬에는 활성 AudioListener가 하나 필요합니다(일반적으로 Main Camera). 데모에는 포함되어 있습니다.

## 검증과 범위

제작 시 C# 구문, 프리팹 내부 참조, GUID·메타 연결, 샘플 분기, 패키지 구조를 정적으로 검사했습니다. 제작 환경에는 Unity Editor가 없어 **6000.3.21f1에서의 실제 Import·Play 테스트는 수행하지 못했습니다.**

가져온 뒤 **Tools → Dark Dialogue → Validate Package**로 Unity 내부의 스크립트·UI 참조·샘플 대사 연결을 확인할 수 있습니다. 샘플을 Play하여 타이핑 → 즉시 출력 → 다음 대사 → 선택지의 두 분기 → 종료를 확인하고 AUTO/SKIP/LOG도 사용할 수 있습니다.

유니티 프로젝트 설정을 변경하지 않습니다. 외부 대사 파서, 성우 음성 자동 동기화, 대화 저장·불러오기 기능은 포함하지 않습니다.

## 사용 권한

이 패키지의 자체 제작 C# 코드·UI·임시 이미지는 본인의 게임에 수정하여 사용할 수 있습니다. 연결하는 캐릭터 이미지·음악·폰트의 사용 권한은 해당 에셋에 따릅니다.
