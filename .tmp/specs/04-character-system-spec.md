# 仕様書: キャラクターシステム

## 概要

プレイヤーとNPCを統一的に管理するキャラクターシステム。
VRM制御コンポーネント群を統合し、HP・感情による表情変化を実現する。

---

## 1. GameCharacter クラス

```csharp
public enum CharacterType
{
    Player,
    NPC
}

public enum CharacterState
{
    Idle,
    Walking,
    Running,
    Sitting,
    InCombat,
    Talking,
    Interacting,
    Incapacitated
}

public class GameCharacter : MonoBehaviour
{
    [Header("基本設定")]
    [SerializeField] private CharacterType characterType = CharacterType.NPC;
    [SerializeField] private string characterName;
    [SerializeField] private GameObject vrmModel;

    [Header("ステータス")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP;
    [SerializeField] private Emotion currentEmotion = Emotion.Neutral;

    [Header("コントローラー参照")]
    [SerializeField] private VRMExpressionController expressionController;
    [SerializeField] private VRMAnimationController animationController;
    [SerializeField] private VRMFinalIKController ikController;
    [SerializeField] private VRMEyeGazeController eyeGazeController;
    [SerializeField] private CharacterNavigator navigator;

    [Header("インタラクション")]
    [SerializeField] private List<InteractionPoint> interactionPoints;
    [SerializeField] private Interactable currentHeldObject;

    [Header("AI（NPCのみ）")]
    [SerializeField] private NPCBehaviorController npcBehavior;

    // プロパティ
    public CharacterType Type => characterType;
    public string Name => characterName;
    public CharacterState State { get; set; } = CharacterState.Idle;
    public float HP => currentHP;
    public float HPRatio => currentHP / maxHP;
    public Emotion CurrentEmotion => currentEmotion;
    public Interactable CurrentHeldObject
    {
        get => currentHeldObject;
        set => currentHeldObject = value;
    }
    public CharacterNavigator Navigator => navigator;

    // イベント
    public event Action<float, float> OnHPChanged;  // current, max
    public event Action<Emotion> OnEmotionChanged;
    public event Action OnDeath;

    private void Awake()
    {
        InitializeComponents();
        CollectInteractionPoints();
    }

    private void Start()
    {
        currentHP = maxHP;

        if (characterType == CharacterType.NPC && npcBehavior != null)
        {
            npcBehavior.Initialize(this);
        }
    }

    private void Update()
    {
        UpdateExpressionByState();
    }

    /// <summary>
    /// コンポーネント参照の初期化
    /// </summary>
    private void InitializeComponents()
    {
        if (expressionController == null)
            expressionController = GetComponentInChildren<VRMExpressionController>();
        if (animationController == null)
            animationController = GetComponentInChildren<VRMAnimationController>();
        if (ikController == null)
            ikController = GetComponentInChildren<VRMFinalIKController>();
        if (eyeGazeController == null)
            eyeGazeController = GetComponentInChildren<VRMEyeGazeController>();
        if (navigator == null)
            navigator = GetComponent<CharacterNavigator>();
    }

    /// <summary>
    /// インタラクションポイントを収集
    /// </summary>
    private void CollectInteractionPoints()
    {
        interactionPoints = new List<InteractionPoint>(
            GetComponentsInChildren<InteractionPoint>()
        );
    }

    /// <summary>
    /// 指定タイプのインタラクションポイントを取得
    /// </summary>
    public InteractionPoint GetInteractionPoint(InteractionPointType type)
    {
        return interactionPoints.Find(p => p.pointType == type);
    }

    /// <summary>
    /// 手のTransformを取得
    /// </summary>
    public Transform GetHandTransform(HandSide side)
    {
        var type = side == HandSide.Right ?
            InteractionPointType.Hand : InteractionPointType.Hand;
        var point = interactionPoints.Find(p =>
            p.pointType == type && p.name.Contains(side.ToString()));
        return point?.transform;
    }

    /// <summary>
    /// ダメージを受ける
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHP = Mathf.Max(0, currentHP - damage);
        OnHPChanged?.Invoke(currentHP, maxHP);

        // ダメージリアクション
        PlayDamageReaction();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// HP回復
    /// </summary>
    public void Heal(float amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    /// <summary>
    /// 感情を設定
    /// </summary>
    public void SetEmotion(Emotion emotion)
    {
        if (currentEmotion != emotion)
        {
            currentEmotion = emotion;
            OnEmotionChanged?.Invoke(emotion);
            ApplyEmotionExpression();
        }
    }

    /// <summary>
    /// HP/感情に応じた表情更新
    /// </summary>
    private void UpdateExpressionByState()
    {
        // HP連動は常時適用
        ApplyHPExpression();
    }

    /// <summary>
    /// HP連動表情
    /// </summary>
    private void ApplyHPExpression()
    {
        if (expressionController == null) return;

        float hpRatio = HPRatio;

        if (hpRatio > 0.7f)
        {
            // 通常
        }
        else if (hpRatio > 0.4f)
        {
            // やや辛そう
            expressionController.SetExpressionWeight("sad", 0.2f);
        }
        else if (hpRatio > 0.2f)
        {
            // 苦しそう
            expressionController.SetExpressionWeight("sad", 0.4f);
            expressionController.SetExpressionWeight("angry", 0.2f);
        }
        else
        {
            // 瀕死
            expressionController.SetExpressionWeight("sad", 0.6f);
        }
    }

    /// <summary>
    /// 感情表情適用
    /// </summary>
    private void ApplyEmotionExpression()
    {
        if (expressionController == null) return;

        string preset = currentEmotion switch
        {
            Emotion.Joy => "happy",
            Emotion.Anger => "angry",
            Emotion.Sadness => "sad",
            Emotion.Fear => "shocked",
            Emotion.Neutral => "neutral",
            _ => "neutral"
        };

        expressionController.StartPresetTransition(preset, 0.3f);
    }

    /// <summary>
    /// ダメージリアクション再生
    /// </summary>
    private void PlayDamageReaction()
    {
        animationController?.PlayAnimation("Hit");
        expressionController?.StartPresetTransition("shocked", 0.1f);
        StartCoroutine(ResetAfterDamage());
    }

    private IEnumerator ResetAfterDamage()
    {
        yield return new WaitForSeconds(0.5f);
        ApplyEmotionExpression();
    }

    /// <summary>
    /// 死亡処理
    /// </summary>
    private void Die()
    {
        State = CharacterState.Incapacitated;
        OnDeath?.Invoke();

        // ラグドール化などの処理
        animationController?.PlayAnimation("Death");
    }
}

public enum HandSide
{
    Right,
    Left
}
```

---

## 2. 感情システム

### 2.1 Emotion enum

```csharp
public enum Emotion
{
    Neutral,    // 通常
    Joy,        // 喜び
    Anger,      // 怒り
    Sadness,    // 悲しみ
    Fear,       // 恐怖
    Surprise,   // 驚き
    Disgust,    // 嫌悪
    Trust,      // 信頼
    Anticipation // 期待
}
```

### 2.2 EmotionSystem クラス

```csharp
public class EmotionSystem : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private GameCharacter character;
    [SerializeField] private VRMExpressionController expressionController;

    [Header("感情設定")]
    [SerializeField] private Emotion baseEmotion = Emotion.Neutral;
    [SerializeField] private float emotionDecayRate = 0.1f; // 感情の減衰速度

    // 感情値（0-1）
    private Dictionary<Emotion, float> _emotionValues = new Dictionary<Emotion, float>();

    // 感情→表情プリセット マッピング
    private static readonly Dictionary<Emotion, string> EmotionPresetMap = new Dictionary<Emotion, string>
    {
        { Emotion.Neutral, "neutral" },
        { Emotion.Joy, "happy" },
        { Emotion.Anger, "furious" },
        { Emotion.Sadness, "dejected" },
        { Emotion.Fear, "shocked" },
        { Emotion.Surprise, "confused" },
        { Emotion.Disgust, "grumpy" },
        { Emotion.Trust, "smile" },
        { Emotion.Anticipation, "expectant" }
    };

    private void Awake()
    {
        // 全感情を初期化
        foreach (Emotion emotion in Enum.GetValues(typeof(Emotion)))
        {
            _emotionValues[emotion] = emotion == baseEmotion ? 1f : 0f;
        }
    }

    private void Update()
    {
        DecayEmotions();
        UpdateDominantEmotion();
    }

    /// <summary>
    /// 感情を追加（イベントなどから呼び出し）
    /// </summary>
    public void AddEmotion(Emotion emotion, float amount)
    {
        _emotionValues[emotion] = Mathf.Clamp01(_emotionValues[emotion] + amount);
        UpdateDominantEmotion();
    }

    /// <summary>
    /// 感情を設定（即座に）
    /// </summary>
    public void SetEmotion(Emotion emotion, float value)
    {
        _emotionValues[emotion] = Mathf.Clamp01(value);
        UpdateDominantEmotion();
    }

    /// <summary>
    /// 感情の減衰
    /// </summary>
    private void DecayEmotions()
    {
        foreach (var emotion in _emotionValues.Keys.ToList())
        {
            if (emotion != baseEmotion)
            {
                _emotionValues[emotion] = Mathf.Max(0,
                    _emotionValues[emotion] - emotionDecayRate * Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// 支配的な感情を更新
    /// </summary>
    private void UpdateDominantEmotion()
    {
        Emotion dominant = baseEmotion;
        float maxValue = 0f;

        foreach (var kvp in _emotionValues)
        {
            if (kvp.Value > maxValue)
            {
                maxValue = kvp.Value;
                dominant = kvp.Key;
            }
        }

        character.SetEmotion(dominant);
    }

    /// <summary>
    /// 現在の感情値を取得
    /// </summary>
    public float GetEmotionValue(Emotion emotion)
    {
        return _emotionValues.TryGetValue(emotion, out float value) ? value : 0f;
    }

    /// <summary>
    /// 表情プリセット名を取得
    /// </summary>
    public string GetPresetForEmotion(Emotion emotion)
    {
        return EmotionPresetMap.TryGetValue(emotion, out string preset) ? preset : "neutral";
    }
}
```

### 2.3 感情トリガー例

| トリガー | 感情 | 強度 |
|---------|------|------|
| 戦闘勝利 | Joy | +0.5 |
| ダメージ受け | Anger | +0.3 |
| HP低下 | Fear | +0.2 |
| 成功 | Joy | +0.3 |
| 失敗 | Sadness | +0.3 |
| 強敵遭遇 | Fear | +0.4 |
| 仲間支援 | Trust | +0.2 |

---

## 3. HP連動表情

```
┌───────────────────────────────────────────┐
│              HP連動表情                    │
├───────────────────────────────────────────┤
│                                           │
│  HP 100-70%: 通常表情                     │
│  ┌─────────────────────────────┐         │
│  │ Expression: neutral         │         │
│  │ Blend: なし                 │         │
│  └─────────────────────────────┘         │
│                                           │
│  HP 70-40%: やや辛そう                    │
│  ┌─────────────────────────────┐         │
│  │ Expression: sad (0.2)       │         │
│  │ Blend: neutral + sad        │         │
│  └─────────────────────────────┘         │
│                                           │
│  HP 40-20%: 苦しそう                      │
│  ┌─────────────────────────────┐         │
│  │ Expression: sad (0.4)       │         │
│  │            angry (0.2)      │         │
│  │ Blend: sad + angry          │         │
│  └─────────────────────────────┘         │
│                                           │
│  HP 20-0%: 瀕死                           │
│  ┌─────────────────────────────┐         │
│  │ Expression: sad (0.6)       │         │
│  │            blink (0.3)      │         │
│  │ Blend: sad + 目を細める     │         │
│  └─────────────────────────────┘         │
│                                           │
└───────────────────────────────────────────┘
```

---

## 4. キャラクターPrefab構成

```
GameCharacter (Prefab)
├── [GameCharacter Component]
│   ├── Character Type: Player/NPC
│   ├── Character Name
│   └── Max HP
│
├── VRM Model
│   ├── [VRMExpressionController]
│   ├── [VRMAnimationController]
│   ├── [VRMFinalIKController]
│   ├── [VRMEyeGazeController]
│   └── Armature
│       ├── Hips
│       ├── Spine
│       ├── ...
│       └── (VRM標準ボーン)
│
├── InteractionPoints
│   ├── eye_point      (Head子要素)
│   ├── mouth_point    (Head子要素)
│   ├── shoulder_R     (RightShoulder子要素)
│   ├── shoulder_L     (LeftShoulder子要素)
│   ├── hip_point      (Hips子要素)
│   ├── hand_R         (RightHand子要素)
│   ├── hand_L         (LeftHand子要素)
│   ├── foot_R         (RightFoot子要素)
│   └── foot_L         (LeftFoot子要素)
│
├── [CharacterNavigator]
├── [NavMeshAgent]
├── [ActionExecutor]
├── [EmotionSystem]
│
└── [NPCBehaviorController] (NPCのみ)
```

---

## 5. NPC自律行動

### 5.1 NPCBehaviorController

```csharp
public enum NPCState
{
    Idle,
    Patrol,
    Follow,
    Combat,
    Interact,
    Flee
}

public class NPCBehaviorController : MonoBehaviour
{
    [Header("参照")]
    private GameCharacter character;

    [Header("行動設定")]
    [SerializeField] private NPCState defaultState = NPCState.Idle;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float combatRange = 5f;

    [Header("パトロール")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 2f;

    [Header("追従")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private float followDistance = 2f;

    public NPCState CurrentState { get; private set; }

    private int _currentPatrolIndex;
    private float _stateTimer;

    public void Initialize(GameCharacter chara)
    {
        character = chara;
        CurrentState = defaultState;
    }

    private void Update()
    {
        if (character.Type != CharacterType.NPC) return;

        // 状態遷移チェック
        CheckStateTransitions();

        // 現在状態の行動
        ExecuteCurrentState();
    }

    /// <summary>
    /// 状態遷移判定
    /// </summary>
    private void CheckStateTransitions()
    {
        // 敵検出
        var nearbyEnemies = DetectEnemies();
        if (nearbyEnemies.Count > 0 && CurrentState != NPCState.Combat)
        {
            TransitionTo(NPCState.Combat);
            return;
        }

        // HP低下で逃走
        if (character.HPRatio < 0.2f && CurrentState == NPCState.Combat)
        {
            TransitionTo(NPCState.Flee);
            return;
        }
    }

    /// <summary>
    /// 状態ごとの行動実行
    /// </summary>
    private void ExecuteCurrentState()
    {
        switch (CurrentState)
        {
            case NPCState.Idle:
                ExecuteIdle();
                break;
            case NPCState.Patrol:
                ExecutePatrol();
                break;
            case NPCState.Follow:
                ExecuteFollow();
                break;
            case NPCState.Combat:
                ExecuteCombat();
                break;
            case NPCState.Flee:
                ExecuteFlee();
                break;
        }
    }

    private void ExecuteIdle()
    {
        _stateTimer += Time.deltaTime;

        // たまに周囲を見回す
        if (_stateTimer > 3f)
        {
            LookAround();
            _stateTimer = 0f;
        }
    }

    private void ExecutePatrol()
    {
        if (patrolPoints.Length == 0) return;

        var target = patrolPoints[_currentPatrolIndex];

        if (!character.Navigator.IsMoving)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist < 0.5f)
            {
                // 到着、次のポイントへ
                _stateTimer += Time.deltaTime;
                if (_stateTimer > patrolWaitTime)
                {
                    _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
                    _stateTimer = 0f;
                }
            }
            else
            {
                character.Navigator.MoveTo(target.position);
            }
        }
    }

    private void ExecuteFollow()
    {
        if (followTarget == null) return;

        float dist = Vector3.Distance(transform.position, followTarget.position);
        if (dist > followDistance)
        {
            character.Navigator.MoveTo(followTarget.position);
        }
    }

    private void ExecuteCombat()
    {
        var enemies = DetectEnemies();
        if (enemies.Count == 0)
        {
            TransitionTo(defaultState);
            return;
        }

        var target = enemies[0];
        float dist = Vector3.Distance(transform.position, target.transform.position);

        if (dist < 2f)
        {
            // 近接攻撃
            var executor = GetComponent<ActionExecutor>();
            executor?.TryExecuteAction("Attack", new ActionContext
            {
                Actor = character,
                Target = target,
                Distance = dist
            });
        }
        else if (dist < combatRange)
        {
            // 魔法攻撃
            var executor = GetComponent<ActionExecutor>();
            executor?.TryExecuteAction("Magic", new ActionContext
            {
                Actor = character,
                Target = target,
                TargetPosition = target.transform.position,
                Distance = dist
            });
        }
        else
        {
            // 接近
            character.Navigator.MoveTo(target.transform.position);
        }
    }

    private void ExecuteFlee()
    {
        var enemies = DetectEnemies();
        if (enemies.Count > 0)
        {
            // 敵の反対方向へ逃げる
            Vector3 fleeDir = transform.position - enemies[0].transform.position;
            Vector3 fleePos = transform.position + fleeDir.normalized * 10f;
            character.Navigator.MoveTo(fleePos);
        }
    }

    private void TransitionTo(NPCState newState)
    {
        CurrentState = newState;
        _stateTimer = 0f;
    }

    private List<GameObject> DetectEnemies()
    {
        var enemies = new List<GameObject>();
        var colliders = Physics.OverlapSphere(transform.position, detectionRange);

        foreach (var col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                enemies.Add(col.gameObject);
            }
        }

        return enemies;
    }

    private void LookAround()
    {
        // ランダムな方向を見る
        var eyeGaze = character.GetComponent<VRMEyeGazeController>();
        // 視線制御
    }

    /// <summary>
    /// 追従ターゲットを設定
    /// </summary>
    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
        TransitionTo(NPCState.Follow);
    }

    /// <summary>
    /// パトロールを開始
    /// </summary>
    public void StartPatrol()
    {
        TransitionTo(NPCState.Patrol);
    }
}
```

---

## 6. テストケース

| # | テスト | 期待結果 |
|---|-------|---------|
| 1 | キャラクター生成 | 全コンポーネント正常初期化 |
| 2 | HP減少（70%以下） | 表情が辛そうに変化 |
| 3 | HP減少（40%以下） | さらに苦しそうな表情 |
| 4 | HP減少（20%以下） | 瀕死表情 |
| 5 | 感情設定（Joy） | happy表情に遷移 |
| 6 | 感情設定（Anger） | angry表情に遷移 |
| 7 | ダメージ受け | リアクション + 表情変化 |
| 8 | NPCパトロール | ポイント間を巡回 |
| 9 | NPC敵検出 | 戦闘状態に遷移 |
| 10 | NPC HP低下 | 逃走状態に遷移 |

