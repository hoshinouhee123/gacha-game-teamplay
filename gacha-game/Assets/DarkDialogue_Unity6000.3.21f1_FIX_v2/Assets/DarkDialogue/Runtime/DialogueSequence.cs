using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DarkDialogue
{
    public enum SpeakerSide { Narration, Left, Right }

    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Dark Dialogue/Dialogue Sequence")]
    public sealed class DialogueSequence : ScriptableObject
    {
        [Tooltip("Starting line, zero-based.")]
        [Min(0)] public int startLine;
        public List<DialogueLine> lines = new List<DialogueLine>();
    }

    [Serializable]
    public sealed class DialogueLine
    {
        public string speaker = "화자";
        public SpeakerSide side = SpeakerSide.Narration;
        [TextArea(2, 8)] public string text = "대사를 입력하세요.";
        [Tooltip("-2 = next line, -1 = finish, 0+ = jump to this line.")]
        public int nextLine = -2;
        [Min(0)] public float characterInterval = 0.035f;
        [Min(0)] public float autoDelay = 1.5f;

        [Header("Portraits: changed only when Update is checked")]
        public bool updateLeft;
        [Tooltip("Null with Update checked hides this portrait.")]
        public Sprite leftPortrait;
        public bool updateRight;
        public Sprite rightPortrait;
        public bool updateBackground;
        public Sprite background;

        [Header("Sound & screen shake")]
        public bool updateBgm;
        [Tooltip("Null with Update Bgm checked stops music.")]
        public AudioClip bgm;
        public AudioClip soundEffect;
        [Min(0)] public float shakeSeconds;
        [Min(0)] public float shakeStrength = 12f;

        [Header("Events & branches")]
        public UnityEvent onEnter = new UnityEvent();
        public List<DialogueChoice> choices = new List<DialogueChoice>();
    }

    [Serializable]
    public sealed class DialogueChoice
    {
        public string label = "선택지";
        [Tooltip("-2 = next line, -1 = finish, 0+ = jump to this line.")]
        public int nextLine = -1;
        public UnityEvent onSelected = new UnityEvent();
    }
}
