# 2학기 게임프로젝트응용 팀플 제작

## 팀원 소개
- **박세현**: 팀장, 게임 기획 담당
- **이동헌**: 프로그래밍 담당
- **장현태**: 프로그래밍 담당
- **민효주**: 아트 담당
- **이정헌**: 아트 담당
- **이진헌**: 아트 담당

## 게임 기획 설명
- 

## 프로그래밍 할때 참고할 내용 정리

### AudioManager를 통한 사운드 재생 방법 예시 코드
``` C#
// BGM 재생
AudioManager.Instance.PlayBGM(bgmClip);

// 효과음 재생
AudioManager.Instance.PlaySFX(sfxClip);

// 효과음 볼륨 50% 재생
AudioManager.Instance.PlaySFX(sfxClip, 0.5f);

// 보이스 재생
AudioManager.Instance.PlayVoice(voiceClip);

// 정지
AudioManager.Instance.StopBGM();
AudioManager.Instance.StopVoice();
AudioManager.Instance.StopAllSFX();
AudioManager.Instance.StopAllAudio();
```

### 팀원들

- 기획: 기획 제대로 구상안함, 시스템 기믹 짜지도 않음, 일을 시키지도 않아서 스스로 만들수밖에 없었다.
- 나머지 프로그래머 1명 : 작업하라는거 작업 안함, ui랑 스테이지 기능 결국 본인이 제작, 심지어 메인메뉴씬 ui 작업중이라고 말하였고 본인이 코드만 건든다고 해놓고 씬 파일을 건드는 바람에 여태까지 UI작업한것들 전부 다시 해야함
- 아트 1명: 본인이 팀 진행에 대해 비난하며 스스로 기획까지 맡겠다해놓고 한게 없음, 스파인 애니메이션으로 작업한다고 해서 코드까지 준비하였으나, 정작 애니메이션의 애자도 안나옴, 결국 아예 학교를 나오지도 않는 상황.
- 아트 1명: 학교를 제대로 나오지도 않음, UI를 그린다고 하였으나 정작 만들어진게 없고, 결국 본인이 ui까지 그려넣어야했음
- 아트 1명: 작업중이라곤 하지만 작업 결과물이 나오질 않음, 출시까지 2주남은 상황에서 캐릭터 스프라이트 조차 제대로 나오지 않았고, 나온거라곤 캐릭터 하나의 스프라이트 뿐이다.


