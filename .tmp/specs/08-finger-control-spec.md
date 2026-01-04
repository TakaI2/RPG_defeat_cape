# 仕様書: 指制御・タッチ反応システム

## 概要

VRMモデルの指ボーンを制御し、ボタン押し・掴む・掻く・くすぐるなどの
細かい動作を実現する。また、タッチを受けたキャラクターの反応システムも含む。

---

## 1. VRM指ボーン構造

### 1.1 ボーン一覧

```
VRM Hand Bones
│
├── J_Bip_R_Hand (右手首)
│   ├── J_Bip_R_Thumb1 → Thumb2 → Thumb3   (親指: 3関節)
│   ├── J_Bip_R_Index1 → Index2 → Index3   (人差し指: 3関節)
│   ├── J_Bip_R_Middle1 → Middle2 → Middle3 (中指: 3関節)
│   ├── J_Bip_R_Ring1 → Ring2 → Ring3      (薬指: 3関節)
│   └── J_Bip_R_Little1 → Little2 → Little3 (小指: 3関節)
│
└── J_Bip_L_Hand (左手首)
    └── (同様の構造)

合計: 片手15ボーン × 2 = 30ボーン
```

### 1.2 関節の回転軸

```
┌─────────────────────────────────────────────────────────────┐
│                    指の回転軸                                │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  人差し指〜小指:                                            │
│  ┌────────────────────────────────────────┐                │
│  │ Z軸回転 = 曲げる/伸ばす                │                │
│  │                                        │                │
│  │    伸ばす (0°)      曲げる (90°)       │                │
│  │    ────────         ┐                  │                │
│  │                     │                  │                │
│  │                     └──                │                │
│  └────────────────────────────────────────┘                │
│                                                             │
│  親指:                                                      │
│  ┌────────────────────────────────────────┐                │
│  │ Z軸 + Y軸の複合回転                    │                │
│  │ (他の指とは異なる動き)                 │                │
│  └────────────────────────────────────────┘                │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. VRMFingerController クラス

### 2.1 基本構造

```csharp
[System.Serializable]
public class FingerPose
{
    public string poseName;

    [Header("各指の曲げ具合 (0=伸ばす, 1=完全に握る)")]
    [Range(0, 1)] public float thumb = 0f;
    [Range(0, 1)] public float index = 0f;
    [Range(0, 1)] public float middle = 0f;
    [Range(0, 1)] public float ring = 0f;
    [Range(0, 1)] public float little = 0f;

    public FingerPose() { }

    public FingerPose(string name, float th, float idx, float mid, float ring, float lit)
    {
        poseName = name;
        thumb = th;
        index = idx;
        middle = mid;
        this.ring = ring;
        little = lit;
    }

    /// <summary>
    /// 2つのポーズを補間
    /// </summary>
    public static FingerPose Lerp(FingerPose a, FingerPose b, float t)
    {
        return new FingerPose
        {
            poseName = "Interpolated",
            thumb = Mathf.Lerp(a.thumb, b.thumb, t),
            index = Mathf.Lerp(a.index, b.index, t),
            middle = Mathf.Lerp(a.middle, b.middle, t),
            ring = Mathf.Lerp(a.ring, b.ring, t),
            little = Mathf.Lerp(a.little, b.little, t)
        };
    }
}

public class VRMFingerController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Animator animator;

    [Header("設定")]
    [SerializeField] private float maxCurlAngle = 90f;
    [SerializeField] private float defaultTransitionTime = 0.2f;

    [Header("プリセットポーズ")]
    [SerializeField] private List<FingerPose> presetPoses = new List<FingerPose>();

    // 指ボーン参照
    private Transform[][] _fingerBones; // [hand][finger*3+joint]
    private FingerPose[] _currentPose;  // [hand] 現在のポーズ

    // 動的動作用
    private Coroutine[] _activeMotion;  // [hand] 実行中のモーション

    private void Awake()
    {
        _fingerBones = new Transform[2][];
        _currentPose = new FingerPose[2];
        _activeMotion = new Coroutine[2];

        CollectFingerBones();
        InitializePresets();

        _currentPose[0] = new FingerPose("Current", 0, 0, 0, 0, 0);
        _currentPose[1] = new FingerPose("Current", 0, 0, 0, 0, 0);
    }

    /// <summary>
    /// VRMボーンから指を収集
    /// </summary>
    private void CollectFingerBones()
    {
        string[] sides = { "R", "L" };
        string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Little" };

        for (int s = 0; s < 2; s++)
        {
            _fingerBones[s] = new Transform[15];

            for (int f = 0; f < 5; f++)
            {
                for (int j = 1; j <= 3; j++)
                {
                    string boneName = $"J_Bip_{sides[s]}_{fingerNames[f]}{j}";
                    _fingerBones[s][f * 3 + (j - 1)] = FindBoneRecursive(transform, boneName);
                }
            }
        }
    }

    private Transform FindBoneRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;

        foreach (Transform child in parent)
        {
            var result = FindBoneRecursive(child, name);
            if (result != null) return result;
        }

        return null;
    }

    /// <summary>
    /// プリセットポーズを初期化
    /// </summary>
    private void InitializePresets()
    {
        presetPoses = new List<FingerPose>
        {
            // 基本ポーズ
            new FingerPose("Open", 0f, 0f, 0f, 0f, 0f),           // 開いた手
            new FingerPose("Fist", 0.8f, 1f, 1f, 1f, 1f),         // 握りこぶし
            new FingerPose("Point", 0.3f, 0f, 1f, 1f, 1f),        // 人差し指で指す
            new FingerPose("Pinch", 0.5f, 0.5f, 0.8f, 0.8f, 0.8f), // つまむ
            new FingerPose("Grip", 0.6f, 0.7f, 0.7f, 0.7f, 0.7f),  // 握る

            // 感情表現
            new FingerPose("Peace", 0.5f, 0f, 0f, 1f, 1f),        // ピース
            new FingerPose("ThumbsUp", 0f, 1f, 1f, 1f, 1f),       // いいね
            new FingerPose("Wave", 0.2f, 0.2f, 0.2f, 0.2f, 0.2f), // 手を振る準備

            // タッチ動作用
            new FingerPose("Scratch", 0.3f, 0.4f, 0.4f, 0.4f, 0.4f),   // 掻く準備
            new FingerPose("Tickle", 0.2f, 0.3f, 0.3f, 0.3f, 0.3f),    // くすぐる準備
            new FingerPose("Poke", 0.4f, 0f, 0.9f, 0.9f, 0.9f),        // つつく
            new FingerPose("Caress", 0.1f, 0.1f, 0.1f, 0.1f, 0.1f),    // 撫でる
        };
    }

    /// <summary>
    /// 指ポーズを即座に適用
    /// </summary>
    public void ApplyPose(FingerPose pose, HandSide side)
    {
        int s = (int)side;
        float[] curls = { pose.thumb, pose.index, pose.middle, pose.ring, pose.little };

        for (int f = 0; f < 5; f++)
        {
            for (int j = 0; j < 3; j++)
            {
                int idx = f * 3 + j;
                if (_fingerBones[s][idx] == null) continue;

                float angle = curls[f] * maxCurlAngle;

                // 親指は回転軸が異なる
                if (f == 0)
                {
                    _fingerBones[s][idx].localRotation = Quaternion.Euler(0, angle * 0.3f, -angle * 0.5f);
                }
                else
                {
                    _fingerBones[s][idx].localRotation = Quaternion.Euler(0, 0, angle);
                }
            }
        }

        _currentPose[s] = pose;
    }

    /// <summary>
    /// 名前でポーズを適用
    /// </summary>
    public void ApplyPose(string poseName, HandSide side)
    {
        var pose = presetPoses.Find(p => p.poseName == poseName);
        if (pose != null) ApplyPose(pose, side);
    }

    /// <summary>
    /// ポーズをスムーズに遷移
    /// </summary>
    public IEnumerator TransitionToPose(FingerPose targetPose, HandSide side, float duration)
    {
        int s = (int)side;
        FingerPose startPose = _currentPose[s];

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            FingerPose interpolated = FingerPose.Lerp(startPose, targetPose, t);
            ApplyPose(interpolated, side);

            elapsed += Time.deltaTime;
            yield return null;
        }

        ApplyPose(targetPose, side);
    }

    public IEnumerator TransitionToPose(string poseName, HandSide side, float duration)
    {
        var pose = presetPoses.Find(p => p.poseName == poseName);
        if (pose != null)
            yield return TransitionToPose(pose, side, duration);
    }

    /// <summary>
    /// 特定の指だけ動かす
    /// </summary>
    public void SetFingerCurl(HandSide side, FingerType finger, float curl)
    {
        int s = (int)side;
        int f = (int)finger;
        float angle = curl * maxCurlAngle;

        for (int j = 0; j < 3; j++)
        {
            int idx = f * 3 + j;
            if (_fingerBones[s][idx] == null) continue;

            if (f == 0) // 親指
            {
                _fingerBones[s][idx].localRotation = Quaternion.Euler(0, angle * 0.3f, -angle * 0.5f);
            }
            else
            {
                _fingerBones[s][idx].localRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        // 現在のポーズを更新
        switch (finger)
        {
            case FingerType.Thumb: _currentPose[s].thumb = curl; break;
            case FingerType.Index: _currentPose[s].index = curl; break;
            case FingerType.Middle: _currentPose[s].middle = curl; break;
            case FingerType.Ring: _currentPose[s].ring = curl; break;
            case FingerType.Little: _currentPose[s].little = curl; break;
        }
    }
}

public enum HandSide { Right = 0, Left = 1 }
public enum FingerType { Thumb = 0, Index = 1, Middle = 2, Ring = 3, Little = 4 }
```

---

## 3. 動的指モーション（掻く・くすぐる）

### 3.1 FingerMotionController

```csharp
public class FingerMotionController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private VRMFingerController fingerController;

    [Header("モーション設定")]
    [SerializeField] private float scratchFrequency = 4f;    // 掻く周波数
    [SerializeField] private float tickleFrequency = 8f;     // くすぐる周波数
    [SerializeField] private float motionAmplitude = 0.3f;   // 動きの振幅

    private Coroutine _activeMotion;

    /// <summary>
    /// 掻く動作を開始
    /// </summary>
    public void StartScratch(HandSide side, float duration = -1)
    {
        StopCurrentMotion();
        _activeMotion = StartCoroutine(ScratchMotion(side, duration));
    }

    /// <summary>
    /// くすぐる動作を開始
    /// </summary>
    public void StartTickle(HandSide side, float duration = -1)
    {
        StopCurrentMotion();
        _activeMotion = StartCoroutine(TickleMotion(side, duration));
    }

    /// <summary>
    /// 撫でる動作を開始
    /// </summary>
    public void StartCaress(HandSide side, float duration = -1)
    {
        StopCurrentMotion();
        _activeMotion = StartCoroutine(CaressMotion(side, duration));
    }

    /// <summary>
    /// モーションを停止
    /// </summary>
    public void StopCurrentMotion()
    {
        if (_activeMotion != null)
        {
            StopCoroutine(_activeMotion);
            _activeMotion = null;
        }
    }

    /// <summary>
    /// 掻く動作コルーチン
    /// 指を曲げ伸ばしして掻くような動き
    /// </summary>
    private IEnumerator ScratchMotion(HandSide side, float duration)
    {
        // 基本ポーズ（Scratch）を適用
        yield return fingerController.TransitionToPose("Scratch", side, 0.2f);

        float elapsed = 0f;
        float baseTime = Time.time;

        while (duration < 0 || elapsed < duration)
        {
            float t = Time.time - baseTime;

            // 各指を位相をずらして波打たせる
            for (int f = 1; f < 5; f++) // 人差し指〜小指
            {
                float phase = f * 0.2f; // 指ごとに位相をずらす
                float curl = 0.4f + Mathf.Sin((t + phase) * scratchFrequency * Mathf.PI * 2) * motionAmplitude;
                fingerController.SetFingerCurl(side, (FingerType)f, curl);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 終了時は開いた手に戻す
        yield return fingerController.TransitionToPose("Open", side, 0.3f);
    }

    /// <summary>
    /// くすぐる動作コルーチン
    /// より速く細かい動き
    /// </summary>
    private IEnumerator TickleMotion(HandSide side, float duration)
    {
        yield return fingerController.TransitionToPose("Tickle", side, 0.2f);

        float elapsed = 0f;
        float baseTime = Time.time;

        while (duration < 0 || elapsed < duration)
        {
            float t = Time.time - baseTime;

            // 高速で細かく動かす
            for (int f = 1; f < 5; f++)
            {
                float phase = f * 0.15f;
                // Sin波 + ノイズで不規則な動き
                float noise = Mathf.PerlinNoise(t * 10f, f * 100f) * 0.1f;
                float curl = 0.3f + Mathf.Sin((t + phase) * tickleFrequency * Mathf.PI * 2) * 0.2f + noise;
                fingerController.SetFingerCurl(side, (FingerType)f, curl);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return fingerController.TransitionToPose("Open", side, 0.3f);
    }

    /// <summary>
    /// 撫でる動作コルーチン
    /// ゆっくり滑らかな動き
    /// </summary>
    private IEnumerator CaressMotion(HandSide side, float duration)
    {
        yield return fingerController.TransitionToPose("Caress", side, 0.3f);

        float elapsed = 0f;
        float baseTime = Time.time;

        while (duration < 0 || elapsed < duration)
        {
            float t = Time.time - baseTime;

            // ゆっくり波打つ動き
            for (int f = 0; f < 5; f++)
            {
                float phase = f * 0.3f;
                float curl = 0.1f + Mathf.Sin((t + phase) * 1.5f * Mathf.PI) * 0.1f;
                fingerController.SetFingerCurl(side, (FingerType)f, Mathf.Max(0, curl));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return fingerController.TransitionToPose("Open", side, 0.3f);
    }
}
```

### 3.2 指モーションの図解

```
┌─────────────────────────────────────────────────────────────┐
│                    掻く動作 (Scratch)                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  時間経過 →                                                 │
│                                                             │
│  人差し指: ▄▂▄▂▄▂▄▂  (位相 0.0)                           │
│  中指:     ▂▄▂▄▂▄▂▄  (位相 0.2)                           │
│  薬指:     ▄▂▄▂▄▂▄▂  (位相 0.4)                           │
│  小指:     ▂▄▂▄▂▄▂▄  (位相 0.6)                           │
│                                                             │
│  ▄ = 曲げる (curl: 0.6)                                    │
│  ▂ = 伸ばす (curl: 0.2)                                    │
│                                                             │
│  周波数: 4Hz (1秒に4往復)                                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                   くすぐる動作 (Tickle)                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  時間経過 →                                                 │
│                                                             │
│  人差し指: ▃▅▂▄▃▅▂▄▃▅  (+ ノイズ)                        │
│  中指:     ▅▃▄▂▅▃▄▂▅▃  (+ ノイズ)                        │
│  薬指:     ▂▄▃▅▂▄▃▅▂▄  (+ ノイズ)                        │
│  小指:     ▄▂▅▃▄▂▅▃▄▂  (+ ノイズ)                        │
│                                                             │
│  周波数: 8Hz (1秒に8往復) + Perlinノイズ                    │
│  → より細かく不規則な動き                                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    撫でる動作 (Caress)                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  時間経過 →                                                 │
│                                                             │
│  全指:  ▁▂▃▂▁▂▃▂▁▂▃  (ゆっくり)                          │
│                                                             │
│  周波数: 1.5Hz (ゆっくり波打つ)                             │
│  振幅: 小さい (0.0〜0.2)                                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 4. タッチ反応システム

### 4.1 TouchReactionController

```csharp
public enum TouchType
{
    Poke,       // つつく
    Scratch,    // 掻く
    Tickle,     // くすぐる
    Caress,     // 撫でる
    Press       // 押す
}

public enum BodyPart
{
    Head,
    Face,
    Shoulder,
    Arm,
    Hand,
    Back,
    Chest,
    Belly,
    Hip,
    Leg,
    Foot
}

[System.Serializable]
public class TouchReaction
{
    public TouchType touchType;
    public BodyPart bodyPart;

    [Header("表情反応")]
    public string expressionPreset;
    public float expressionIntensity = 1f;

    [Header("体の反応")]
    public bool enableBodyReaction = true;
    public float reactionIntensity = 1f;
    public AnimationClip reactionAnimation;

    [Header("音声反応")]
    public AudioClip[] voiceClips;
    public float voiceChance = 0.5f;
}

public class TouchReactionController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private GameCharacter character;
    [SerializeField] private VRMExpressionController expressionController;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    [Header("反応設定")]
    [SerializeField] private List<TouchReaction> reactions = new List<TouchReaction>();

    [Header("体の揺れ設定")]
    [SerializeField] private float bodySwayAmount = 0.02f;
    [SerializeField] private float bodySwaySpeed = 5f;

    // 現在のタッチ状態
    private bool _isBeingTouched;
    private TouchType _currentTouchType;
    private BodyPart _currentBodyPart;
    private Coroutine _reactionCoroutine;

    private void Awake()
    {
        InitializeDefaultReactions();
    }

    /// <summary>
    /// デフォルト反応を設定
    /// </summary>
    private void InitializeDefaultReactions()
    {
        reactions = new List<TouchReaction>
        {
            // 頭を撫でる → 嬉しい
            new TouchReaction
            {
                touchType = TouchType.Caress,
                bodyPart = BodyPart.Head,
                expressionPreset = "happy",
                expressionIntensity = 0.8f,
                enableBodyReaction = true,
                reactionIntensity = 0.3f
            },

            // 脇腹をくすぐる → 笑う + 体をくねらせる
            new TouchReaction
            {
                touchType = TouchType.Tickle,
                bodyPart = BodyPart.Belly,
                expressionPreset = "laugh",
                expressionIntensity = 1f,
                enableBodyReaction = true,
                reactionIntensity = 1f
            },

            // 肩をつつく → 驚く
            new TouchReaction
            {
                touchType = TouchType.Poke,
                bodyPart = BodyPart.Shoulder,
                expressionPreset = "surprised",
                expressionIntensity = 0.6f,
                enableBodyReaction = true,
                reactionIntensity = 0.5f
            },

            // 頬を撫でる → 照れる
            new TouchReaction
            {
                touchType = TouchType.Caress,
                bodyPart = BodyPart.Face,
                expressionPreset = "embarrassed",
                expressionIntensity = 0.7f,
                enableBodyReaction = true,
                reactionIntensity = 0.2f
            },

            // 足をくすぐる → 大笑い + 大きく動く
            new TouchReaction
            {
                touchType = TouchType.Tickle,
                bodyPart = BodyPart.Foot,
                expressionPreset = "laugh",
                expressionIntensity = 1f,
                enableBodyReaction = true,
                reactionIntensity = 1.5f
            },

            // 背中を掻く → 気持ちいい
            new TouchReaction
            {
                touchType = TouchType.Scratch,
                bodyPart = BodyPart.Back,
                expressionPreset = "content",
                expressionIntensity = 0.8f,
                enableBodyReaction = true,
                reactionIntensity = 0.4f
            },

            // 手を握る → 照れる/嬉しい
            new TouchReaction
            {
                touchType = TouchType.Press,
                bodyPart = BodyPart.Hand,
                expressionPreset = "embarrassed",
                expressionIntensity = 0.5f,
                enableBodyReaction = false
            }
        };
    }

    /// <summary>
    /// タッチ開始
    /// </summary>
    public void OnTouchStart(TouchType type, BodyPart part)
    {
        _isBeingTouched = true;
        _currentTouchType = type;
        _currentBodyPart = part;

        // 反応を検索
        var reaction = FindReaction(type, part);

        if (_reactionCoroutine != null)
            StopCoroutine(_reactionCoroutine);

        _reactionCoroutine = StartCoroutine(PlayReaction(reaction));
    }

    /// <summary>
    /// タッチ終了
    /// </summary>
    public void OnTouchEnd()
    {
        _isBeingTouched = false;

        if (_reactionCoroutine != null)
        {
            StopCoroutine(_reactionCoroutine);
            _reactionCoroutine = null;
        }

        // 表情を戻す
        StartCoroutine(ResetExpression());
    }

    /// <summary>
    /// 反応を検索
    /// </summary>
    private TouchReaction FindReaction(TouchType type, BodyPart part)
    {
        // 完全一致を優先
        var exact = reactions.Find(r => r.touchType == type && r.bodyPart == part);
        if (exact != null) return exact;

        // タイプのみ一致
        var typeMatch = reactions.Find(r => r.touchType == type);
        if (typeMatch != null) return typeMatch;

        // デフォルト反応
        return new TouchReaction
        {
            expressionPreset = "surprised",
            expressionIntensity = 0.3f,
            enableBodyReaction = true,
            reactionIntensity = 0.3f
        };
    }

    /// <summary>
    /// 反応を再生
    /// </summary>
    private IEnumerator PlayReaction(TouchReaction reaction)
    {
        // 表情変化
        if (!string.IsNullOrEmpty(reaction.expressionPreset))
        {
            expressionController?.StartPresetTransition(
                reaction.expressionPreset, 0.2f);
        }

        // 音声
        if (reaction.voiceClips?.Length > 0 && Random.value < reaction.voiceChance)
        {
            var clip = reaction.voiceClips[Random.Range(0, reaction.voiceClips.Length)];
            audioSource?.PlayOneShot(clip);
        }

        // 体の反応
        if (reaction.enableBodyReaction)
        {
            yield return BodyReaction(reaction);
        }

        // タッチが続いている間、反応を継続
        while (_isBeingTouched)
        {
            if (reaction.enableBodyReaction)
            {
                yield return ContinuousBodyReaction(reaction);
            }
            else
            {
                yield return null;
            }
        }
    }

    /// <summary>
    /// 初期体反応
    /// </summary>
    private IEnumerator BodyReaction(TouchReaction reaction)
    {
        // アニメーションがあれば再生
        if (reaction.reactionAnimation != null)
        {
            animator?.Play(reaction.reactionAnimation.name);
        }
        else
        {
            // アニメーションがなければ、体を揺らす
            yield return ShakeBody(reaction.reactionIntensity, 0.3f);
        }
    }

    /// <summary>
    /// 継続的な体反応（くすぐられ続けている時など）
    /// </summary>
    private IEnumerator ContinuousBodyReaction(TouchReaction reaction)
    {
        // くすぐりの場合は継続的に体を揺らす
        if (_currentTouchType == TouchType.Tickle)
        {
            yield return TickleBodyReaction(reaction.reactionIntensity);
        }
        else
        {
            yield return null;
        }
    }

    /// <summary>
    /// 体を揺らす
    /// </summary>
    private IEnumerator ShakeBody(float intensity, float duration)
    {
        Vector3 originalPos = transform.localPosition;
        Quaternion originalRot = transform.localRotation;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float shake = (1 - t) * intensity; // 徐々に減衰

            Vector3 offset = new Vector3(
                Mathf.Sin(Time.time * 30f) * bodySwayAmount * shake,
                Mathf.Sin(Time.time * 25f) * bodySwayAmount * shake * 0.5f,
                0
            );

            transform.localPosition = originalPos + offset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
        transform.localRotation = originalRot;
    }

    /// <summary>
    /// くすぐられている時の体反応
    /// </summary>
    private IEnumerator TickleBodyReaction(float intensity)
    {
        Vector3 originalPos = transform.localPosition;

        // 不規則に体を揺らす
        float noise1 = Mathf.PerlinNoise(Time.time * bodySwaySpeed, 0) - 0.5f;
        float noise2 = Mathf.PerlinNoise(0, Time.time * bodySwaySpeed) - 0.5f;

        Vector3 offset = new Vector3(
            noise1 * bodySwayAmount * intensity * 2f,
            Mathf.Abs(noise2) * bodySwayAmount * intensity,
            noise2 * bodySwayAmount * intensity
        );

        transform.localPosition = originalPos + offset;

        yield return null;

        transform.localPosition = originalPos;
    }

    /// <summary>
    /// 表情をリセット
    /// </summary>
    private IEnumerator ResetExpression()
    {
        yield return new WaitForSeconds(0.5f);
        expressionController?.ResetToNeutral(0.5f);
    }
}
```

### 4.2 反応パターン表

| タッチ種類 | 部位 | 表情 | 体の反応 | 強度 |
|-----------|------|------|----------|------|
| Caress | Head | happy | 小さく揺れる | 0.3 |
| Caress | Face | embarrassed | 少し揺れる | 0.2 |
| Tickle | Belly | laugh | 大きくくねる | 1.0 |
| Tickle | Foot | laugh | 激しく動く | 1.5 |
| Scratch | Back | content | 気持ちよさそう | 0.4 |
| Poke | Shoulder | surprised | ビクッとする | 0.5 |
| Press | Hand | embarrassed | なし | 0 |

---

## 5. TouchActionへの統合

### 5.1 拡張されたTouchAction

```csharp
public class TouchAction : ActionBase
{
    public override string ActionName => "Touch";

    // タッチの種類（UIや状況から決定）
    public TouchType CurrentTouchType { get; set; } = TouchType.Poke;

    public override IEnumerator Execute(ActionContext context)
    {
        var actor = context.Actor;
        var target = context.Target;
        var targetInteractable = context.TargetInteractable;

        var fingerController = actor.GetComponent<VRMFingerController>();
        var fingerMotion = actor.GetComponent<FingerMotionController>();
        var ik = context.IKController;

        // ターゲットがキャラクターの場合、反応システムを取得
        var targetReaction = target.GetComponent<TouchReactionController>();
        BodyPart targetPart = DetermineBodyPart(context);

        // 1. 視線をターゲットへ
        context.EyeGazeController?.SetLookAtTarget(target.transform);

        // 2. タッチタイプに応じた指ポーズ
        string fingerPose = GetFingerPoseForTouchType(CurrentTouchType);
        StartCoroutine(fingerController.TransitionToPose(fingerPose, HandSide.Right, 0.3f));

        // 3. 手を伸ばす
        Vector3 touchPos = GetTouchPosition(context);
        yield return ik.MoveHandSmooth(HandSide.Right, touchPos, 0.4f);

        // 4. タッチ開始を通知
        targetReaction?.OnTouchStart(CurrentTouchType, targetPart);

        // 5. タッチタイプに応じた動作
        switch (CurrentTouchType)
        {
            case TouchType.Poke:
                yield return PokeMotion(ik, touchPos);
                break;

            case TouchType.Scratch:
                fingerMotion.StartScratch(HandSide.Right);
                yield return ScratchMotion(ik, touchPos, 2f);
                fingerMotion.StopCurrentMotion();
                break;

            case TouchType.Tickle:
                fingerMotion.StartTickle(HandSide.Right);
                yield return TickleMotion(ik, touchPos, 3f);
                fingerMotion.StopCurrentMotion();
                break;

            case TouchType.Caress:
                fingerMotion.StartCaress(HandSide.Right);
                yield return CaressMotion(ik, touchPos, 2f);
                fingerMotion.StopCurrentMotion();
                break;
        }

        // 6. タッチ終了を通知
        targetReaction?.OnTouchEnd();

        // 7. 手を戻す
        StartCoroutine(fingerController.TransitionToPose("Open", HandSide.Right, 0.3f));
        yield return ik.ResetHand(HandSide.Right, 0.3f);
    }

    private string GetFingerPoseForTouchType(TouchType type)
    {
        return type switch
        {
            TouchType.Poke => "Point",
            TouchType.Scratch => "Scratch",
            TouchType.Tickle => "Tickle",
            TouchType.Caress => "Caress",
            TouchType.Press => "Open",
            _ => "Open"
        };
    }

    /// <summary>
    /// つつく動作
    /// </summary>
    private IEnumerator PokeMotion(VRMFinalIKController ik, Vector3 basePos)
    {
        // 押し込む
        Vector3 pokePos = basePos + Vector3.forward * 0.03f;
        yield return ik.MoveHandSmooth(HandSide.Right, pokePos, 0.1f);
        yield return new WaitForSeconds(0.1f);

        // 戻す
        yield return ik.MoveHandSmooth(HandSide.Right, basePos, 0.1f);
    }

    /// <summary>
    /// 掻く動作（手を小刻みに動かす）
    /// </summary>
    private IEnumerator ScratchMotion(VRMFinalIKController ik, Vector3 basePos, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 小さく上下に動かす
            float offset = Mathf.Sin(Time.time * 15f) * 0.01f;
            Vector3 pos = basePos + Vector3.up * offset;
            ik.SetHandTarget(HandSide.Right, pos);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// くすぐる動作（手を細かく動かす）
    /// </summary>
    private IEnumerator TickleMotion(VRMFinalIKController ik, Vector3 basePos, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // ランダムに小さく動かす
            Vector3 offset = new Vector3(
                Mathf.PerlinNoise(Time.time * 20f, 0) - 0.5f,
                Mathf.PerlinNoise(0, Time.time * 20f) - 0.5f,
                Mathf.PerlinNoise(Time.time * 20f, Time.time * 20f) - 0.5f
            ) * 0.02f;

            ik.SetHandTarget(HandSide.Right, basePos + offset);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// 撫でる動作（ゆっくり移動）
    /// </summary>
    private IEnumerator CaressMotion(VRMFinalIKController ik, Vector3 basePos, float duration)
    {
        float elapsed = 0f;
        Vector3 startPos = basePos - Vector3.right * 0.1f;
        Vector3 endPos = basePos + Vector3.right * 0.1f;

        while (elapsed < duration)
        {
            // 左右にゆっくり撫でる
            float t = Mathf.PingPong(elapsed * 0.5f, 1f);
            t = Mathf.SmoothStep(0, 1, t);

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            ik.SetHandTarget(HandSide.Right, pos);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// タッチ位置からBodyPartを推定
    /// </summary>
    private BodyPart DetermineBodyPart(ActionContext context)
    {
        var touchPoint = context.TargetPoint;
        if (touchPoint == null) return BodyPart.Chest;

        // ポイント名から推定
        string name = touchPoint.name.ToLower();

        if (name.Contains("head")) return BodyPart.Head;
        if (name.Contains("face") || name.Contains("cheek")) return BodyPart.Face;
        if (name.Contains("shoulder")) return BodyPart.Shoulder;
        if (name.Contains("arm")) return BodyPart.Arm;
        if (name.Contains("hand")) return BodyPart.Hand;
        if (name.Contains("back")) return BodyPart.Back;
        if (name.Contains("belly") || name.Contains("stomach")) return BodyPart.Belly;
        if (name.Contains("hip")) return BodyPart.Hip;
        if (name.Contains("leg")) return BodyPart.Leg;
        if (name.Contains("foot")) return BodyPart.Foot;

        return BodyPart.Chest;
    }
}
```

---

## 6. 全体フロー図

```
┌─────────────────────────────────────────────────────────────┐
│              タッチインタラクション全体フロー                │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  [操作者]                          [対象キャラ]             │
│                                                             │
│  タッチ開始                                                 │
│      │                                                      │
│      ▼                                                      │
│  ┌──────────────┐                                           │
│  │ 指ポーズ設定  │                                           │
│  │ (Point/Tickle│                                           │
│  │  /Scratch等) │                                           │
│  └──────┬───────┘                                           │
│         │                                                   │
│         ▼                                                   │
│  ┌──────────────┐                                           │
│  │ IKで手を伸ばす│                                           │
│  └──────┬───────┘                                           │
│         │                                                   │
│         ▼                                                   │
│  ┌──────────────┐     通知      ┌──────────────┐           │
│  │ 接触開始     │─────────────▶│ OnTouchStart │           │
│  └──────┬───────┘               └──────┬───────┘           │
│         │                              │                    │
│         │                              ▼                    │
│         │                       ┌──────────────┐           │
│         │                       │ 表情変化     │           │
│         │                       │ (laugh/happy │           │
│         │                       │  /embarrassed)│           │
│         │                       └──────┬───────┘           │
│         │                              │                    │
│         │                              ▼                    │
│         │                       ┌──────────────┐           │
│         │                       │ 体の反応     │           │
│         │                       │ (くねる/揺れる)│          │
│         │                       └──────────────┘           │
│         │                                                   │
│         ▼                                                   │
│  ┌──────────────┐                                           │
│  │ 指モーション  │  (継続中)                                 │
│  │ (波打ち動作)  │◀─────────────────────────────────────┐  │
│  └──────┬───────┘                                       │  │
│         │                                               │  │
│         │ ループ                                        │  │
│         └───────────────────────────────────────────────┘  │
│                                                             │
│  タッチ終了                                                 │
│      │                                                      │
│      ▼                                                      │
│  ┌──────────────┐     通知      ┌──────────────┐           │
│  │ 指を戻す     │─────────────▶│ OnTouchEnd   │           │
│  │ 手を戻す     │               │ → 表情リセット│           │
│  └──────────────┘               └──────────────┘           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 7. テストケース

| # | テスト | 期待結果 |
|---|-------|---------|
| 1 | Point指ポーズ適用 | 人差し指が伸び、他が曲がる |
| 2 | 指ポーズ遷移 | スムーズに補間される |
| 3 | 掻くモーション | 指が波打つように動く |
| 4 | くすぐるモーション | 指が細かく不規則に動く |
| 5 | 頭を撫でる | 対象が嬉しそうな表情に |
| 6 | 脇腹をくすぐる | 対象が笑い、体をくねらせる |
| 7 | 足をくすぐる | 対象が激しく反応 |
| 8 | タッチ終了 | 表情が徐々に戻る |

---

## 8. 今後の拡張

- 両手での同時タッチ
- NPCからプレイヤーへのタッチ
- 好感度による反応の変化
- 音声反応の追加
- VRコントローラー対応

