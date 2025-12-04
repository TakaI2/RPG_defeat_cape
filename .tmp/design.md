# 設計書: ストーリー・メッセージウィンドウシステム

## 1. アーキテクチャ概要

### 1.1 システム構成図

```
┌─────────────────────────────────────────────────────────────────┐
│                        Story System                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐      │
│  │  StoryData   │───▶│ StoryPlayer  │───▶│ MessageWindow│      │
│  │ (ScriptableObject)│    (Engine)     │    (UI)         │      │
│  └──────────────┘    └──────┬───────┘    └──────────────┘      │
│                             │                                    │
│                             ▼                                    │
│  ┌──────────────────────────────────────────────────────┐      │
│  │                   Command System                      │      │
│  │  ┌─────┐ ┌────┐ ┌─────┐ ┌────┐ ┌─────┐ ┌─────┐     │      │
│  │  │ Say │ │ Bg │ │ Bgm │ │ Se │ │Wait │ │ End │     │      │
│  │  └─────┘ └────┘ └─────┘ └────┘ └─────┘ └─────┘     │      │
│  └──────────────────────────────────────────────────────┘      │
│                                                                  │
│  ┌──────────────┐    ┌──────────────┐                          │
│  │ StoryManager │    │ AudioManager │                          │
│  │ (Singleton)  │    │  (Optional)  │                          │
│  └──────────────┘    └──────────────┘                          │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 レイヤー構成

| レイヤー | 責務 | クラス |
|---------|------|--------|
| Presentation | UI表示・入力処理 | MessageWindow, TypewriterEffect |
| Application | ストーリー再生制御 | StoryPlayer, StoryManager |
| Domain | コマンド実行ロジック | IStoryCommand, *Command classes |
| Data | データ保持・変換 | StoryData, StoryScript |

---

## 2. クラス設計

### 2.1 クラス図

```
┌─────────────────────────────────────────────────────────────┐
│                     <<ScriptableObject>>                     │
│                        StoryData                             │
├─────────────────────────────────────────────────────────────┤
│ + storyId: string                                           │
│ + commands: List<StoryCommandData>                          │
├─────────────────────────────────────────────────────────────┤
│ + ImportFromJson(json: string): void                        │
│ + ExportToJson(): string                                    │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    [Serializable]                            │
│                   StoryCommandData                           │
├─────────────────────────────────────────────────────────────┤
│ + op: string                                                │
│ + parameters: Dictionary<string, object>                    │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                     <<interface>>                            │
│                     IStoryCommand                            │
├─────────────────────────────────────────────────────────────┤
│ + Execute(context: StoryContext): IEnumerator               │
└─────────────────────────────────────────────────────────────┘
           △
           │
    ┌──────┴──────┬──────────┬──────────┬──────────┬─────────┐
    │             │          │          │          │         │
┌───┴───┐  ┌─────┴────┐ ┌───┴───┐ ┌───┴───┐ ┌───┴───┐ ┌───┴───┐
│SayCmd │  │  BgCmd   │ │BgmCmd │ │ SeCmd │ │WaitCmd│ │EndCmd │
└───────┘  └──────────┘ └───────┘ └───────┘ └───────┘ └───────┘

┌─────────────────────────────────────────────────────────────┐
│                     <<MonoBehaviour>>                        │
│                       StoryPlayer                            │
├─────────────────────────────────────────────────────────────┤
│ - currentStory: StoryData                                   │
│ - commandIndex: int                                         │
│ - isPlaying: bool                                           │
│ - context: StoryContext                                     │
├─────────────────────────────────────────────────────────────┤
│ + Play(story: StoryData): void                              │
│ + Stop(): void                                              │
│ + Pause(): void                                             │
│ + Resume(): void                                            │
│ - ExecuteCommands(): IEnumerator                            │
├─────────────────────────────────────────────────────────────┤
│ <<event>> OnStoryStarted: Action<string>                    │
│ <<event>> OnStoryEnded: Action<string>                      │
│ <<event>> OnCommandExecuted: Action<StoryCommandData>       │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                       StoryContext                           │
├─────────────────────────────────────────────────────────────┤
│ + messageWindow: MessageWindow                              │
│ + backgroundImage: Image                                    │
│ + bgmSource: AudioSource                                    │
│ + seSource: AudioSource                                     │
│ + portraitResources: Dictionary<string, Sprite>             │
│ + backgroundResources: Dictionary<string, Sprite>           │
│ + bgmResources: Dictionary<string, AudioClip>               │
│ + seResources: Dictionary<string, AudioClip>                │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                     <<MonoBehaviour>>                        │
│                      MessageWindow                           │
├─────────────────────────────────────────────────────────────┤
│ [SerializeField] nameText: TMP_Text                         │
│ [SerializeField] dialogueText: TMP_Text                     │
│ [SerializeField] leftPortrait: Image                        │
│ [SerializeField] centerPortrait: Image                      │
│ [SerializeField] rightPortrait: Image                       │
│ [SerializeField] advanceIndicator: GameObject               │
│ [SerializeField] windowRoot: CanvasGroup                    │
│ - typewriterEffect: TypewriterEffect                        │
│ - isWaitingForInput: bool                                   │
├─────────────────────────────────────────────────────────────┤
│ + Show(): void                                              │
│ + Hide(): void                                              │
│ + ShowDialogue(name, lines, portrait, pos): IEnumerator     │
│ + SetPortrait(sprite, position, fade): IEnumerator          │
│ + ClearPortraits(): void                                    │
│ + OnAdvanceInput(): void                                    │
├─────────────────────────────────────────────────────────────┤
│ <<event>> OnDialogueCompleted: Action                       │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                     <<MonoBehaviour>>                        │
│                     TypewriterEffect                         │
├─────────────────────────────────────────────────────────────┤
│ [SerializeField] charactersPerSecond: float = 30f           │
│ - targetText: TMP_Text                                      │
│ - fullText: string                                          │
│ - isTyping: bool                                            │
├─────────────────────────────────────────────────────────────┤
│ + StartTyping(text: string): IEnumerator                    │
│ + Skip(): void                                              │
│ + IsTyping: bool { get; }                                   │
├─────────────────────────────────────────────────────────────┤
│ <<event>> OnTypingCompleted: Action                         │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. 詳細クラス仕様

### 3.1 StoryData.cs

```csharp
namespace RPGDefete.Story
{
    [CreateAssetMenu(fileName = "NewStory", menuName = "RPG Defete/Story/Story Data")]
    public class StoryData : ScriptableObject
    {
        [SerializeField] private string storyId;
        [SerializeField] private List<StoryCommandData> commands = new();

        public string StoryId => storyId;
        public IReadOnlyList<StoryCommandData> Commands => commands;

        public void ImportFromJson(string json);
        public string ExportToJson();

        // Editor only
        public void AddCommand(StoryCommandData command);
        public void RemoveCommand(int index);
        public void MoveCommand(int fromIndex, int toIndex);
    }
}
```

### 3.2 StoryCommandData.cs

```csharp
namespace RPGDefete.Story
{
    [Serializable]
    public class StoryCommandData
    {
        public string op;

        // Say command
        public string characterName;
        public string portrait;
        public PortraitPosition portraitPosition = PortraitPosition.Center;
        public float portraitScale = 1f;
        public List<string> lines = new();

        // Bg command
        public string backgroundName;
        public int fadeDuration;

        // Bgm command
        public string bgmName;
        public bool loop = true;
        public float volume = 1f;
        public int fadeInDuration;
        public int fadeOutDuration;

        // Se command
        public string seName;
        public float seVolume = 1f;

        // Wait command
        public int waitDuration;

        // End command
        public string returnTo;
    }

    public enum PortraitPosition
    {
        Left,
        Center,
        Right
    }
}
```

### 3.3 IStoryCommand.cs

```csharp
namespace RPGDefete.Story.Commands
{
    public interface IStoryCommand
    {
        IEnumerator Execute(StoryContext context, StoryCommandData data);
    }
}
```

### 3.4 StoryPlayer.cs

```csharp
namespace RPGDefete.Story
{
    public class StoryPlayer : MonoBehaviour
    {
        [SerializeField] private MessageWindow messageWindow;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource seSource;

        public event Action<string> OnStoryStarted;
        public event Action<string> OnStoryEnded;
        public event Action<StoryCommandData> OnCommandExecuted;

        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }

        private StoryData currentStory;
        private int commandIndex;
        private StoryContext context;
        private Dictionary<string, IStoryCommand> commandHandlers;

        private void Awake()
        {
            InitializeCommandHandlers();
            InitializeContext();
        }

        public void Play(StoryData story)
        {
            if (IsPlaying) Stop();
            currentStory = story;
            commandIndex = 0;
            IsPlaying = true;
            OnStoryStarted?.Invoke(story.StoryId);
            StartCoroutine(ExecuteCommands());
        }

        public void Stop()
        {
            StopAllCoroutines();
            IsPlaying = false;
            messageWindow.Hide();
        }

        private IEnumerator ExecuteCommands()
        {
            while (commandIndex < currentStory.Commands.Count)
            {
                if (IsPaused)
                {
                    yield return null;
                    continue;
                }

                var cmdData = currentStory.Commands[commandIndex];
                if (commandHandlers.TryGetValue(cmdData.op, out var handler))
                {
                    yield return handler.Execute(context, cmdData);
                    OnCommandExecuted?.Invoke(cmdData);
                }
                commandIndex++;
            }

            IsPlaying = false;
            OnStoryEnded?.Invoke(currentStory.StoryId);
        }
    }
}
```

### 3.5 MessageWindow.cs

```csharp
namespace RPGDefete.Story.UI
{
    public class MessageWindow : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup windowRoot;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Image leftPortrait;
        [SerializeField] private Image centerPortrait;
        [SerializeField] private Image rightPortrait;
        [SerializeField] private GameObject advanceIndicator;

        [Header("Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.2f;
        [SerializeField] private float portraitFadeDuration = 0.3f;

        private TypewriterEffect typewriter;
        private bool isWaitingForInput;

        public event Action OnDialogueCompleted;

        public bool IsVisible => windowRoot.alpha > 0;

        public void Show();
        public void Hide();

        public IEnumerator ShowDialogue(
            string characterName,
            List<string> lines,
            Sprite portrait = null,
            PortraitPosition position = PortraitPosition.Center)
        {
            nameText.text = characterName;

            if (portrait != null)
            {
                yield return SetPortrait(portrait, position, true);
            }

            foreach (var line in lines)
            {
                advanceIndicator.SetActive(false);
                yield return typewriter.StartTyping(line);
                advanceIndicator.SetActive(true);

                isWaitingForInput = true;
                yield return new WaitUntil(() => !isWaitingForInput);
            }

            OnDialogueCompleted?.Invoke();
        }

        public void OnAdvanceInput()
        {
            if (typewriter.IsTyping)
            {
                typewriter.Skip();
            }
            else if (isWaitingForInput)
            {
                isWaitingForInput = false;
            }
        }
    }
}
```

### 3.6 TypewriterEffect.cs

```csharp
namespace RPGDefete.Story.UI
{
    public class TypewriterEffect : MonoBehaviour
    {
        [SerializeField] private float charactersPerSecond = 30f;
        [SerializeField] private AudioClip typingSound;
        [SerializeField] private int playSoundEveryNChars = 2;

        private TMP_Text targetText;
        private string fullText;
        private bool isTyping;
        private bool skipRequested;

        public bool IsTyping => isTyping;
        public event Action OnTypingCompleted;

        public void Initialize(TMP_Text text)
        {
            targetText = text;
        }

        public IEnumerator StartTyping(string text)
        {
            fullText = text;
            isTyping = true;
            skipRequested = false;
            targetText.text = "";

            float delay = 1f / charactersPerSecond;
            int charIndex = 0;

            foreach (char c in fullText)
            {
                if (skipRequested)
                {
                    targetText.text = fullText;
                    break;
                }

                targetText.text += c;
                charIndex++;

                // Optional: Play typing sound
                if (typingSound != null && charIndex % playSoundEveryNChars == 0)
                {
                    // Play sound
                }

                yield return new WaitForSeconds(delay);
            }

            isTyping = false;
            OnTypingCompleted?.Invoke();
        }

        public void Skip()
        {
            if (isTyping)
            {
                skipRequested = true;
            }
        }
    }
}
```

---

## 4. コマンド実装

### 4.1 SayCommand.cs

```csharp
namespace RPGDefete.Story.Commands
{
    public class SayCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            Sprite portrait = null;
            if (!string.IsNullOrEmpty(data.portrait))
            {
                context.portraitResources.TryGetValue(data.portrait, out portrait);
            }

            context.messageWindow.Show();
            yield return context.messageWindow.ShowDialogue(
                data.characterName,
                data.lines,
                portrait,
                data.portraitPosition
            );
        }
    }
}
```

### 4.2 BgCommand.cs

```csharp
namespace RPGDefete.Story.Commands
{
    public class BgCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            if (context.backgroundResources.TryGetValue(data.backgroundName, out var sprite))
            {
                if (data.fadeDuration > 0)
                {
                    yield return FadeBackground(context.backgroundImage, sprite, data.fadeDuration / 1000f);
                }
                else
                {
                    context.backgroundImage.sprite = sprite;
                    context.backgroundImage.color = Color.white;
                }
            }
        }

        private IEnumerator FadeBackground(Image image, Sprite newSprite, float duration)
        {
            // Crossfade implementation
            float elapsed = 0;
            Color startColor = image.color;

            // Fade out
            while (elapsed < duration / 2)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1, 0, elapsed / (duration / 2));
                image.color = new Color(1, 1, 1, alpha);
                yield return null;
            }

            image.sprite = newSprite;

            // Fade in
            elapsed = 0;
            while (elapsed < duration / 2)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0, 1, elapsed / (duration / 2));
                image.color = new Color(1, 1, 1, alpha);
                yield return null;
            }

            image.color = Color.white;
        }
    }
}
```

### 4.3 BgmCommand.cs

```csharp
namespace RPGDefete.Story.Commands
{
    public class BgmPlayCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            if (context.bgmResources.TryGetValue(data.bgmName, out var clip))
            {
                context.bgmSource.clip = clip;
                context.bgmSource.loop = data.loop;

                if (data.fadeInDuration > 0)
                {
                    context.bgmSource.volume = 0;
                    context.bgmSource.Play();
                    yield return FadeVolume(context.bgmSource, data.volume, data.fadeInDuration / 1000f);
                }
                else
                {
                    context.bgmSource.volume = data.volume;
                    context.bgmSource.Play();
                }
            }
        }

        private IEnumerator FadeVolume(AudioSource source, float targetVolume, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            source.volume = targetVolume;
        }
    }

    public class BgmStopCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            if (data.fadeOutDuration > 0)
            {
                yield return FadeOutAndStop(context.bgmSource, data.fadeOutDuration / 1000f);
            }
            else
            {
                context.bgmSource.Stop();
            }
        }

        private IEnumerator FadeOutAndStop(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0, elapsed / duration);
                yield return null;
            }

            source.Stop();
            source.volume = startVolume;
        }
    }
}
```

### 4.4 WaitCommand.cs

```csharp
namespace RPGDefete.Story.Commands
{
    public class WaitCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            yield return new WaitForSeconds(data.waitDuration / 1000f);
        }
    }
}
```

### 4.5 EndCommand.cs

```csharp
namespace RPGDefete.Story.Commands
{
    public class EndCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            context.messageWindow.Hide();
            // returnTo handling is done by StoryPlayer's OnStoryEnded event
            yield break;
        }
    }
}
```

---

## 5. シーケンス図

### 5.1 ストーリー再生フロー

```
User          StoryPlayer       SayCommand      MessageWindow    TypewriterEffect
 │                 │                │                │                │
 │  Play(story)    │                │                │                │
 │────────────────▶│                │                │                │
 │                 │                │                │                │
 │                 │ Execute(say)   │                │                │
 │                 │───────────────▶│                │                │
 │                 │                │                │                │
 │                 │                │  ShowDialogue  │                │
 │                 │                │───────────────▶│                │
 │                 │                │                │                │
 │                 │                │                │ StartTyping    │
 │                 │                │                │───────────────▶│
 │                 │                │                │                │
 │                 │                │                │   (typing...)  │
 │                 │                │                │◀───────────────│
 │                 │                │                │                │
 │  Click/Space    │                │                │                │
 │────────────────────────────────────────────────▶│                │
 │                 │                │                │                │
 │                 │                │                │     Skip()     │
 │                 │                │                │───────────────▶│
 │                 │                │                │                │
 │                 │                │                │◀───────────────│
 │                 │                │                │   completed    │
 │                 │                │◀───────────────│                │
 │                 │◀───────────────│                │                │
 │                 │                │                │                │
 │                 │ (next command) │                │                │
 │                 │                │                │                │
```

### 5.2 入力処理フロー

```
Input          MessageWindow    TypewriterEffect
  │                 │                │
  │ OnAdvanceInput  │                │
  │────────────────▶│                │
  │                 │                │
  │                 │──┐ IsTyping?   │
  │                 │  │             │
  │                 │◀─┘             │
  │                 │                │
  │          [if typing]            │
  │                 │    Skip()     │
  │                 │───────────────▶│
  │                 │                │
  │       [if waiting for input]    │
  │                 │                │
  │                 │──┐             │
  │                 │  │ isWaitingForInput = false
  │                 │◀─┘             │
  │                 │                │
```

---

## 6. Prefab構成

### 6.1 MessageWindow.prefab

```
MessageWindow (Canvas)
├── WindowRoot (CanvasGroup)
│   ├── Background (Image) - 半透明黒背景
│   ├── PortraitContainer
│   │   ├── LeftPortrait (Image)
│   │   ├── CenterPortrait (Image)
│   │   └── RightPortrait (Image)
│   └── DialogueBox
│       ├── NameBackground (Image)
│       │   └── NameText (TMP_Text)
│       ├── DialogueBackground (Image)
│       │   └── DialogueText (TMP_Text)
│       └── AdvanceIndicator (Image) - ▼アイコン
└── InputHandler (Component) - クリック/キー入力検出
```

### 6.2 StoryPlayer.prefab

```
StoryPlayer
├── StoryPlayer (Component)
├── BGM (AudioSource)
└── SE (AudioSource)
```

---

## 7. リソース管理

### 7.1 リソースパス規約

| リソース種別 | パス | 例 |
|-------------|------|-----|
| 立ち絵 | Resources/Story/Portraits/{name} | Resources/Story/Portraits/hero_normal |
| 背景 | Resources/Story/Backgrounds/{name} | Resources/Story/Backgrounds/village |
| BGM | Resources/Story/BGM/{name} | Resources/Story/BGM/peaceful |
| SE | Resources/Story/SE/{name} | Resources/Story/SE/click |

### 7.2 StoryResourceLoader.cs

```csharp
namespace RPGDefete.Story
{
    public static class StoryResourceLoader
    {
        private const string PortraitPath = "Story/Portraits/";
        private const string BackgroundPath = "Story/Backgrounds/";
        private const string BgmPath = "Story/BGM/";
        private const string SePath = "Story/SE/";

        public static Sprite LoadPortrait(string name)
            => Resources.Load<Sprite>(PortraitPath + name);

        public static Sprite LoadBackground(string name)
            => Resources.Load<Sprite>(BackgroundPath + name);

        public static AudioClip LoadBgm(string name)
            => Resources.Load<AudioClip>(BgmPath + name);

        public static AudioClip LoadSe(string name)
            => Resources.Load<AudioClip>(SePath + name);

        // Preload all resources for a story
        public static void PreloadStory(StoryData story, StoryContext context)
        {
            foreach (var cmd in story.Commands)
            {
                switch (cmd.op)
                {
                    case "say":
                        if (!string.IsNullOrEmpty(cmd.portrait))
                            context.portraitResources[cmd.portrait] = LoadPortrait(cmd.portrait);
                        break;
                    case "bg":
                        context.backgroundResources[cmd.backgroundName] = LoadBackground(cmd.backgroundName);
                        break;
                    // ... etc
                }
            }
        }
    }
}
```

---

## 8. エディタ拡張

### 8.1 StoryEditorWindow.cs

```csharp
namespace RPGDefete.Story.Editor
{
    public class StoryEditorWindow : EditorWindow
    {
        [MenuItem("Tools/RPG Defete/Story Editor")]
        public static void ShowWindow()
        {
            GetWindow<StoryEditorWindow>("Story Editor");
        }

        private StoryData currentStory;
        private Vector2 scrollPosition;
        private int selectedCommandIndex = -1;
        private ReorderableList commandList;

        private void OnGUI()
        {
            DrawToolbar();
            DrawCommandList();
            DrawCommandInspector();
        }

        private void DrawToolbar()
        {
            // New, Open, Save, Import JSON, Export JSON buttons
        }

        private void DrawCommandList()
        {
            // ReorderableList for commands with drag & drop
        }

        private void DrawCommandInspector()
        {
            // Edit selected command parameters
        }
    }
}
```

### 8.2 StoryDataEditor.cs (Custom Inspector)

```csharp
namespace RPGDefete.Story.Editor
{
    [CustomEditor(typeof(StoryData))]
    public class StoryDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Custom inspector with command list preview
            // Open in Story Editor button
            // Import/Export JSON buttons
        }
    }
}
```

---

## 9. 外部連携API

### 9.1 StoryManager.cs (Singleton)

```csharp
namespace RPGDefete.Story
{
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager Instance { get; private set; }

        [SerializeField] private StoryPlayer storyPlayer;
        [SerializeField] private bool pauseGameDuringStory = true;

        public event Action<string> OnStoryStarted;
        public event Action<string> OnStoryEnded;

        public bool IsStoryPlaying => storyPlayer.IsPlaying;

        public void PlayStory(StoryData story)
        {
            if (pauseGameDuringStory)
                Time.timeScale = 0;

            storyPlayer.Play(story);
        }

        public void PlayStory(string storyId)
        {
            var story = Resources.Load<StoryData>($"Story/Data/{storyId}");
            if (story != null)
                PlayStory(story);
        }

        public void StopStory()
        {
            storyPlayer.Stop();
            if (pauseGameDuringStory)
                Time.timeScale = 1;
        }
    }
}
```

---

## 10. JSON互換性

### 10.1 インポート処理

```csharp
public void ImportFromJson(string json)
{
    var jsonData = JsonUtility.FromJson<StoryJsonData>(json);
    storyId = jsonData.id;
    commands.Clear();

    foreach (var cmd in jsonData.script)
    {
        var commandData = new StoryCommandData
        {
            op = cmd.op
        };

        // Map JSON fields to StoryCommandData
        switch (cmd.op)
        {
            case "say":
                commandData.characterName = cmd.name;
                commandData.portrait = cmd.portrait;
                commandData.portraitPosition = ParsePosition(cmd.portraitPosition);
                commandData.lines = new List<string>(cmd.lines ?? new string[0]);
                break;
            // ... other commands
        }

        commands.Add(commandData);
    }
}
```

---

## 11. ファイル一覧

| ファイル | 説明 |
|---------|------|
| `Scripts/Story/Core/StoryData.cs` | ストーリーデータScriptableObject |
| `Scripts/Story/Core/StoryCommandData.cs` | コマンドデータ構造 |
| `Scripts/Story/Core/StoryPlayer.cs` | ストーリー再生エンジン |
| `Scripts/Story/Core/StoryContext.cs` | 実行コンテキスト |
| `Scripts/Story/Core/StoryManager.cs` | シングルトンマネージャー |
| `Scripts/Story/Core/StoryResourceLoader.cs` | リソース読み込み |
| `Scripts/Story/Commands/IStoryCommand.cs` | コマンドインターフェース |
| `Scripts/Story/Commands/SayCommand.cs` | セリフコマンド |
| `Scripts/Story/Commands/BgCommand.cs` | 背景コマンド |
| `Scripts/Story/Commands/BgmCommand.cs` | BGMコマンド |
| `Scripts/Story/Commands/SeCommand.cs` | SEコマンド |
| `Scripts/Story/Commands/WaitCommand.cs` | 待機コマンド |
| `Scripts/Story/Commands/EndCommand.cs` | 終了コマンド |
| `Scripts/Story/UI/MessageWindow.cs` | メッセージウィンドウUI |
| `Scripts/Story/UI/TypewriterEffect.cs` | タイプライター効果 |
| `Scripts/Story/Editor/StoryEditorWindow.cs` | エディタウィンドウ |
| `Scripts/Story/Editor/StoryDataEditor.cs` | カスタムインスペクタ |
| `Prefabs/Story/MessageWindow.prefab` | UIプレハブ |
| `Prefabs/Story/StoryPlayer.prefab` | プレイヤープレハブ |
