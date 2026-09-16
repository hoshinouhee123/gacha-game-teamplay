# 2학기 게임프로젝트응용 팀플 제작

## 팀원 소개
- **박세현**: 팀장, 게임 기획 담당
- **이동헌**: 프로그래밍 담당
- **장현태**: 프로그래밍 담당
- **민효주**: 아트 담당
- **이정헌**: 아트 담당
- **이진헌**: 아트 담당

## 게임 기획 설명
- 아직 게임 제목도 미정ㄷㄷㄷㄷㄷㄷ

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
