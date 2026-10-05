using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkDialogue
{
    [DisallowMultipleComponent]
    public sealed class DialogueManager : MonoBehaviour
    {
        [Header("Playback")]
        public DialogueSequence sequence;
        public bool playOnStart = true;
        public bool keyboardAdvance = true;
        [Tooltip("Uses a Korean system font on Windows/macOS when available.")]
        public bool useSystemKoreanFont = true;
        [Tooltip("Assign a font here for builds without Korean system fonts.")]
        public Font overrideFont;
        [Min(0.01f)] public float skipInterval = 0.12f;

        [Header("UI - already wired in the prefab")]
        public CanvasGroup presentation;
        public Text speakerName;
        public Text dialogueText;
        public Text progressText;
        public GameObject nextIcon;
        public Button advanceButton;
        public Button autoButton;
        public Button skipButton;
        public Button logButton;
        public Text autoLabel;
        public Text skipLabel;
        public Image background;
        public Image leftPortrait;
        public Image rightPortrait;
        public RectTransform stage;
        public RectTransform choicePanel;
        public Button choiceTemplate;
        public GameObject logPanel;
        public Text logText;
        public Text logPageText;
        public Button closeLogButton;
        public Button previousLogButton;
        public Button nextLogButton;
        public AudioSource musicSource;
        public AudioSource effectSource;
        [Tooltip("Scene event with the current zero-based line index.")]
        public UnityEvent<int> onLineEntered = new UnityEvent<int>();
        public UnityEvent onDialogueFinished = new UnityEvent();

        public bool IsRunning { get; private set; }
        public bool IsTyping { get; private set; }
        public int CurrentLineIndex { get; private set; } = -1;
        public bool IsWaitingForChoice { get; private set; }

        readonly List<Button> liveChoices = new List<Button>();
        readonly List<string> history = new List<string>();
        Coroutine typingRoutine;
        Coroutine shakeRoutine;
        Vector2 stageOrigin;
        Color leftTarget = Color.white;
        Color rightTarget = Color.white;
        Font ownedFont;
        ScrollRect choiceScroll;
        ScrollRect historyScroll;
        bool automatic;
        bool skipping;
        bool logOpen;
        float elapsed;
        int logPage;
        int generation;
        const int LogEntriesPerPage = 3;

        // Optional package types are resolved at runtime. A player-setting define
        // alone does not guarantee that the package is installed or referenced.
        static readonly Type OptionalUiModule = Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem", false);
        static readonly Type OptionalKeyboard = Type.GetType(
            "UnityEngine.InputSystem.Keyboard, Unity.InputSystem", false);
        static readonly PropertyInfo KeyboardCurrent = OptionalKeyboard?.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
        static readonly PropertyInfo SpaceKey = OptionalKeyboard?.GetProperty("spaceKey");
        static readonly PropertyInfo EnterKey = OptionalKeyboard?.GetProperty("enterKey");

        void Awake()
        {
            if (!ValidateReferences()) { enabled = false; return; }
            stageOrigin = stage.anchoredPosition;
            dialogueText.supportRichText = false;
            speakerName.supportRichText = false;
            logText.supportRichText = false;
            choiceTemplate.gameObject.SetActive(false);
            choicePanel.gameObject.SetActive(false);
            logPanel.SetActive(false);
            choiceScroll = ConfigureScroll(choicePanel);
            historyScroll = ConfigureScroll(logText.rectTransform);
            choiceScroll.gameObject.SetActive(false);
            advanceButton.onClick.AddListener(Advance);
            autoButton.onClick.AddListener(ToggleAuto);
            skipButton.onClick.AddListener(ToggleSkip);
            logButton.onClick.AddListener(OpenLog);
            closeLogButton.onClick.AddListener(CloseLog);
            previousLogButton.onClick.AddListener(PreviousLogPage);
            nextLogButton.onClick.AddListener(NextLogPage);
            SetVisible(false);
        }

        static ScrollRect ConfigureScroll(RectTransform content)
        {
            var viewport = content.parent as RectTransform;
            if (viewport.GetComponent<RectMask2D>() == null) viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.GetComponent<ScrollRect>();
            if (scroll == null) scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35f;
            return scroll;
        }

        void Start()
        {
            SetupFont();
            EnsureEventSystem();
            if (playOnStart && sequence != null) Begin(sequence);
        }

        bool ValidateReferences()
        {
            bool valid = presentation && speakerName && dialogueText && progressText && nextIcon &&
                advanceButton && autoButton && skipButton && logButton && autoLabel && skipLabel &&
                background && leftPortrait && rightPortrait && stage && choicePanel && choiceTemplate &&
                logPanel && logText && logPageText && closeLogButton && previousLogButton && nextLogButton &&
                musicSource && effectSource;
            if (!valid) Debug.LogError("Dark Dialogue: UI reference is missing. Use the supplied prefab.", this);
            return valid;
        }

        void SetupFont()
        {
            Font font = overrideFont;
            if (font == null && useSystemKoreanFont)
            {
                string[] installed = Font.GetOSInstalledFontNames();
                string[] candidates = { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Noto Sans KR", "NanumGothic" };
                foreach (string candidate in candidates)
                {
                    if (Array.Exists(installed, name => string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase)))
                    {
                        ownedFont = Font.CreateDynamicFontFromOSFont(candidate, 32);
                        font = ownedFont;
                        break;
                    }
                }
                if (font == null) Debug.LogWarning("Dark Dialogue: no Korean system font found. Assign Override Font for Korean text.", this);
            }
            if (font == null) return;
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = font;
        }

        void EnsureEventSystem()
        {
            // Keep the scene's existing input setup. Only create one for an empty scene.
            if (EventSystem.current != null) return;
            var inputObject = new GameObject("DialogueEventSystem", typeof(EventSystem));
            inputObject.transform.SetParent(transform, false);
#if ENABLE_INPUT_SYSTEM
            if (OptionalUiModule != null && typeof(BaseInputModule).IsAssignableFrom(OptionalUiModule))
            {
                // OnEnable on the Input System UI module assigns default actions.
                inputObject.AddComponent(OptionalUiModule);
                return;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
            inputObject.AddComponent<StandaloneInputModule>();
#else
            Debug.LogError("Dark Dialogue: the project selects New Input only but its UI input module is unavailable. " +
                "Set Edit > Project Settings > Player > Other Settings > Active Input Handling to Both, " +
                "or install/enable the Input System package.", this);
#endif
        }

        public void Begin() { Begin(sequence); }

        public void Begin(DialogueSequence data)
        {
            if (!enabled || !ValidateReferences()) return;
            if (data == null || data.lines == null || data.lines.Count == 0 ||
                data.startLine < 0 || data.startLine >= data.lines.Count)
            {
                Debug.LogError("Dark Dialogue: sequence is empty or Start Line is invalid.", this);
                return;
            }
            CancelPlayback();
            sequence = data;
            history.Clear();
            automatic = false;
            skipping = false;
            logOpen = false;
            logPanel.SetActive(false);
            UpdateControlLabels();
            SetVisible(true);
            IsRunning = true;
            ShowLine(data.startLine);
        }

        void Update()
        {
            if (!IsRunning) return;
            float blend = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            leftPortrait.color = Color.Lerp(leftPortrait.color, leftTarget, blend);
            rightPortrait.color = Color.Lerp(rightPortrait.color, rightTarget, blend);
            if (logOpen) return;
            if (keyboardAdvance && !IsWaitingForChoice && AdvanceKeyPressed()) Advance();
            if (!IsRunning || IsTyping || IsWaitingForChoice) return;
            if (!automatic && !skipping) return;
            elapsed += Time.unscaledDeltaTime;
            DialogueLine line = sequence.lines[CurrentLineIndex];
            float delay = skipping ? Mathf.Max(0.01f, skipInterval) : Mathf.Max(0.1f, line.autoDelay);
            if (elapsed >= delay) Advance();
        }

        bool AdvanceKeyPressed()
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                // Let the UI module handle focused controls, preventing a double advance.
                return false;
            }
#if ENABLE_INPUT_SYSTEM
            object keyboard = KeyboardCurrent?.GetValue(null, null);
            if (keyboard != null)
                return WasPressed(SpaceKey?.GetValue(keyboard, null)) || WasPressed(EnterKey?.GetValue(keyboard, null));
#endif
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
#else
            return false;
#endif
        }

        static bool WasPressed(object control)
        {
            if (control == null) return false;
            PropertyInfo pressed = control.GetType().GetProperty("wasPressedThisFrame");
            return pressed != null && pressed.GetValue(control, null) is bool value && value;
        }

        void ShowLine(int index)
        {
            if (index < 0 || index >= sequence.lines.Count || sequence.lines[index] == null)
            {
                Debug.LogError("Dark Dialogue: invalid target line " + index, this);
                End();
                return;
            }
            if (typingRoutine != null) StopCoroutine(typingRoutine);
            typingRoutine = null;
            ClearChoices();
            int token = ++generation;
            CurrentLineIndex = index;
            IsTyping = false;
            elapsed = 0;
            var line = sequence.lines[index];
            speakerName.text = line.speaker ?? string.Empty;
            dialogueText.text = string.Empty;
            progressText.text = (index + 1).ToString("00") + " / " + sequence.lines.Count.ToString("00");
            nextIcon.SetActive(false);
            ApplyPresentation(line);
            line.onEnter?.Invoke();
            if (token != generation || !IsRunning) return;
            onLineEntered?.Invoke(index);
            if (token != generation || !IsRunning) return;
            string entry = (string.IsNullOrEmpty(line.speaker) ? "" : "[" + line.speaker + "]\n") + (line.text ?? "");
            history.Add(entry);
            IsTyping = true;
            typingRoutine = StartCoroutine(TypeLine(line, token));
        }

        IEnumerator TypeLine(DialogueLine line, int token)
        {
            string text = line.text ?? string.Empty;
            TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(text);
            var output = new StringBuilder();
            while (elements.MoveNext())
            {
                if (token != generation || !IsRunning) yield break;
                while (logOpen) yield return null;
                output.Append(elements.GetTextElement());
                dialogueText.text = output.ToString();
                float wait = Mathf.Max(0f, line.characterInterval);
                while (!skipping && wait > 0f)
                {
                    yield return null;
                    if (!logOpen) wait -= Time.unscaledDeltaTime;
                }
            }
            FinishTyping(line);
        }

        void FinishTyping(DialogueLine line)
        {
            dialogueText.text = line.text ?? string.Empty;
            IsTyping = false;
            typingRoutine = null;
            elapsed = 0;
            if (line.choices != null && line.choices.Count > 0) ShowChoices(line);
            else nextIcon.SetActive(true);
        }

        public void Advance()
        {
            if (!IsRunning || logOpen || IsWaitingForChoice) return;
            var line = sequence.lines[CurrentLineIndex];
            if (IsTyping)
            {
                if (typingRoutine != null) StopCoroutine(typingRoutine);
                FinishTyping(line);
                return;
            }
            GoTo(line.nextLine);
        }

        void GoTo(int target)
        {
            if (target == -1) { End(); return; }
            int next = target == -2 ? CurrentLineIndex + 1 : target;
            if (target == -2 && next == sequence.lines.Count) { End(); return; }
            ShowLine(next);
        }

        void ShowChoices(DialogueLine line)
        {
            IsWaitingForChoice = true;
            skipping = false;
            UpdateControlLabels();
            nextIcon.SetActive(false);
            choicePanel.gameObject.SetActive(true);
            choiceScroll.gameObject.SetActive(true);
            // Scroll list keeps larger sets of choices within the viewport.
            float rowHeight = choiceTemplate.GetComponent<RectTransform>().sizeDelta.y;
            int count = line.choices.Count;
            choicePanel.sizeDelta = new Vector2(choicePanel.sizeDelta.x, count * (rowHeight + 12f));
            choicePanel.anchoredPosition = Vector2.zero;
            choiceScroll.StopMovement();
            choiceScroll.verticalNormalizedPosition = 1f;
            for (int i = 0; i < count; i++)
            {
                DialogueChoice choice = line.choices[i];
                if (choice == null) continue;
                var button = Instantiate(choiceTemplate, choicePanel);
                button.name = "Choice_" + i;
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -i * (rowHeight + 12f));
                rect.sizeDelta = new Vector2(0f, rowHeight);
                button.GetComponentInChildren<Text>(true).text = choice.label ?? "";
                button.onClick = new Button.ButtonClickedEvent();
                int token = generation;
                button.onClick.AddListener(() =>
                {
                    if (!IsRunning || !IsWaitingForChoice || token != generation) return;
                    IsWaitingForChoice = false;
                    foreach (Button live in liveChoices) live.interactable = false;
                    choice.onSelected?.Invoke();
                    if (token != generation || !IsRunning) return;
                    if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                    GoTo(choice.nextLine);
                });
                button.gameObject.SetActive(true);
                liveChoices.Add(button);
            }
        }

        void ClearChoices()
        {
            foreach (Button button in liveChoices)
                if (button != null) { button.gameObject.SetActive(false); Destroy(button.gameObject); }
            liveChoices.Clear();
            IsWaitingForChoice = false;
            if (choicePanel != null) choicePanel.gameObject.SetActive(false);
            if (choiceScroll != null) choiceScroll.gameObject.SetActive(false);
        }

        void ApplyPresentation(DialogueLine line)
        {
            if (line.updateLeft) SetPortrait(leftPortrait, line.leftPortrait);
            if (line.updateRight) SetPortrait(rightPortrait, line.rightPortrait);
            if (line.updateBackground) { background.sprite = line.background; background.enabled = line.background != null; }
            Color dim = new Color(0.38f, 0.38f, 0.42f, 1f);
            leftTarget = line.side == SpeakerSide.Right ? dim : Color.white;
            rightTarget = line.side == SpeakerSide.Left ? dim : Color.white;
            if (line.updateBgm)
            {
                musicSource.Stop();
                musicSource.clip = line.bgm;
                if (line.bgm != null) musicSource.Play();
            }
            if (line.soundEffect != null) effectSource.PlayOneShot(line.soundEffect);
            if (shakeRoutine != null) { StopCoroutine(shakeRoutine); shakeRoutine = null; stage.anchoredPosition = stageOrigin; }
            if (line.shakeSeconds > 0) shakeRoutine = StartCoroutine(Shake(line.shakeSeconds, line.shakeStrength));
        }

        static void SetPortrait(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        IEnumerator Shake(float seconds, float strength)
        {
            float time = 0f;
            while (time < seconds)
            {
                if (!logOpen)
                {
                    time += Time.unscaledDeltaTime;
                    float falloff = Mathf.Clamp01(1f - time / seconds);
                    stage.anchoredPosition = stageOrigin + UnityEngine.Random.insideUnitCircle * strength * falloff;
                }
                yield return null;
            }
            stage.anchoredPosition = stageOrigin;
            shakeRoutine = null;
        }

        public void ToggleAuto()
        {
            if (!IsRunning) return;
            automatic = !automatic;
            skipping = false;
            elapsed = 0;
            UpdateControlLabels();
        }

        public void ToggleSkip()
        {
            if (!IsRunning || IsWaitingForChoice || logOpen) return;
            skipping = !skipping;
            automatic = false;
            elapsed = 0;
            UpdateControlLabels();
            if (skipping && IsTyping) Advance();
        }

        void UpdateControlLabels()
        {
            autoLabel.text = automatic ? "AUTO : ON" : "AUTO";
            skipLabel.text = skipping ? "SKIP : ON" : "SKIP";
            autoLabel.color = automatic ? new Color(1f, 0.75f, 0.25f) : Color.white;
            skipLabel.color = skipping ? new Color(1f, 0.75f, 0.25f) : Color.white;
        }

        public void OpenLog()
        {
            if (!IsRunning) return;
            logOpen = true;
            logPanel.SetActive(true);
            logPage = Mathf.Max(0, (history.Count - 1) / LogEntriesPerPage);
            RenderLog();
        }

        public void CloseLog()
        {
            logOpen = false;
            logPanel.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        public void PreviousLogPage() { logPage = Mathf.Max(0, logPage - 1); RenderLog(); }
        public void NextLogPage() { logPage = Mathf.Min(Mathf.Max(0, (history.Count - 1) / LogEntriesPerPage), logPage + 1); RenderLog(); }

        void RenderLog()
        {
            var output = new StringBuilder();
            int start = logPage * LogEntriesPerPage;
            for (int i = start; i < Mathf.Min(start + LogEntriesPerPage, history.Count); i++)
            {
                if (i > start) output.Append("\n\n────────────────────────\n\n");
                output.Append(history[i]);
            }
            logText.text = output.ToString();
            float height = Mathf.Max(historyScroll.viewport.rect.height, logText.preferredHeight + 20f);
            logText.rectTransform.sizeDelta = new Vector2(logText.rectTransform.sizeDelta.x, height);
            logText.rectTransform.anchoredPosition = Vector2.zero;
            historyScroll.StopMovement();
            historyScroll.verticalNormalizedPosition = 1f;
            int pages = Mathf.Max(1, (history.Count + LogEntriesPerPage - 1) / LogEntriesPerPage);
            logPageText.text = (logPage + 1) + " / " + pages;
            previousLogButton.interactable = logPage > 0;
            nextLogButton.interactable = logPage < pages - 1;
        }

        public void End()
        {
            if (!IsRunning) return;
            CancelPlayback();
            SetVisible(false);
            onDialogueFinished?.Invoke();
        }

        void CancelPlayback()
        {
            generation++;
            StopAllCoroutines();
            typingRoutine = null;
            shakeRoutine = null;
            IsRunning = false;
            IsTyping = false;
            automatic = false;
            skipping = false;
            logOpen = false;
            if (stage != null) stage.anchoredPosition = stageOrigin;
            if (logPanel != null) logPanel.SetActive(false);
            ClearChoices();
            if (musicSource != null) musicSource.Stop();
            if (effectSource != null) effectSource.Stop();
        }

        void SetVisible(bool visible)
        {
            presentation.alpha = visible ? 1f : 0f;
            presentation.interactable = visible;
            presentation.blocksRaycasts = visible;
        }

        void OnDisable()
        {
            CancelPlayback();
            if (presentation != null) SetVisible(false);
        }

        void OnDestroy()
        {
            if (ownedFont != null) Destroy(ownedFont);
        }
    }
}
