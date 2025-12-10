# アクションRPG 技モーションシステム ロードマップ

## 概要

VRoidキャラクター + Mixamoアニメーションを使用したアクションRPGの戦闘システム構築計画。

| 項目 | 内容 |
|------|------|
| キャラクター | VRoid Studio で作成 (VRM形式) |
| アニメーション | Mixamo から取得 |
| 表情 | VRM BlendShape を活用 |
| 対象Unity | 6000.0.46f1 (URP) |

---

## Phase 1: 基盤構築 (VRoid + Mixamo準備)

### 1.1 VRoidキャラクターのセットアップ
**目的**: VRMキャラクターをUnityで使用可能にする

- [ ] UniVRM パッケージのインストール
  - VRM 1.0 対応: `com.vrmc.vrm` または UniVRM
  - https://github.com/vrm-c/UniVRM
- [ ] VRoid Studio でキャラクター作成・エクスポート
- [ ] UnityにVRMファイルをインポート
- [ ] Prefab化して `Assets/Characters/` に保存

### 1.2 Mixamoアニメーションの取得と設定
**目的**: Humanoid互換アニメーションを準備する

必要なアニメーション一覧:
| カテゴリ | アニメーション例 |
|---------|-----------------|
| 基本 | Idle, Walk, Run, Jump |
| 攻撃 | Punch, Kick, Sword Slash, Combo |
| スキル | Magic Cast, Special Attack |
| リアクション | Hit Reaction, Knockback, Death |

- [ ] Mixamo (https://www.mixamo.com/) でアニメーション選択
- [ ] FBX (Without Skin) でダウンロード
- [ ] Unity Import Settings:
  - Animation Type: Humanoid
  - Avatar Definition: Copy From Other Avatar (VRMのAvatar)
- [ ] `Assets/Animations/Combat/` に整理

### 1.3 Animator Controller 作成
**目的**: 戦闘用のアニメーター構築

```
States:
├── Locomotion (BlendTree)
│   ├── Idle
│   ├── Walk
│   └── Run
├── Combat Layer (Override)
│   ├── Attack_Light (Combo対応)
│   ├── Attack_Heavy
│   ├── Skill_01
│   ├── Skill_02
│   └── Skill_Ultimate
└── Reaction Layer (Additive)
    ├── Hit
    ├── Knockback
    └── Death
```

- [ ] Base Layer: 移動系
- [ ] Combat Layer: 攻撃・スキル (Override)
- [ ] Reaction Layer: 被ダメージ (Additive)

---

## Phase 2: アクション/スキルシステム

### 2.1 スキルデータ構造 (ScriptableObject)
**目的**: 技をデータとして定義し、拡張性を確保

```csharp
[CreateAssetMenu(fileName = "Skill", menuName = "RPG/Skill")]
public class SkillData : ScriptableObject
{
    public string skillName;
    public SkillType skillType;      // Light, Heavy, Skill, Ultimate
    public AnimationClip animation;
    public float damage;
    public float cooldown;
    public float animationSpeed;

    // 表情
    public string expressionName;     // VRM BlendShape名
    public float expressionWeight;

    // エフェクト
    public GameObject effectPrefab;
    public float effectTiming;        // アニメーション中のタイミング(0-1)
    public Vector3 effectOffset;

    // カメラ演出
    public bool useCameraCutIn;
    public CameraCutInData cutInData;

    // SE
    public AudioClip soundEffect;
}
```

- [ ] SkillData.cs 作成
- [ ] SkillType enum 定義
- [ ] サンプルスキル 3-5個 作成

### 2.2 スキル実行システム
**目的**: スキルを発動・管理するコンポーネント

```csharp
public class SkillExecutor : MonoBehaviour
{
    - スキル発動
    - クールダウン管理
    - アニメーション再生
    - エフェクト生成
    - 表情変更
    - カメラ演出トリガー
}
```

- [ ] SkillExecutor.cs 作成
- [ ] 入力バインディング (技1: J, 技2: K, 必殺: L など)
- [ ] コンボシステム (連続入力で派生)

### 2.3 ヒットボックス/ダメージシステム
**目的**: 攻撃判定とダメージ処理

- [ ] HitBox.cs (攻撃判定)
- [ ] HurtBox.cs (被ダメージ判定)
- [ ] DamageInfo.cs (ダメージ情報)
- [ ] アニメーションイベントでHitBox有効化

---

## Phase 3: 表情システム (VRM BlendShape)

### 3.1 VRM表情コントローラー
**目的**: 戦闘中の表情変化を制御

VRM標準表情:
| 表情名 | 用途 |
|--------|------|
| Neutral | 通常時 |
| Angry | 攻撃時 |
| Fun | スキル発動時 |
| Sad | 被ダメージ時 |
| Surprised | クリティカル時 |

- [ ] ExpressionController.cs 作成
  - SetExpression(string name, float weight, float duration)
  - FadeToNeutral(float duration)
- [ ] スキルデータと連携

### 3.2 リップシンク/自動まばたき
**目的**: 自然な表情演出

- [ ] AutoBlink.cs (自動まばたき)
- [ ] 戦闘中のまばたき頻度調整

---

## Phase 4: カメラ演出

### 4.1 カメラワークシステム
**目的**: 必殺技時のダイナミックなカメラ演出

演出パターン:
| パターン | 説明 |
|----------|------|
| ZoomIn | キャラクターにズーム |
| CutIn | カットインイラスト表示 |
| SlowMotion | スローモーション |
| Shake | 画面揺れ |
| Orbit | キャラ周囲を回る |

- [ ] CombatCameraController.cs
- [ ] CameraCutInData.cs (カットイン設定)
- [ ] Cinemachine との連携 (Virtual Camera切り替え)

### 4.2 Timeline演出 (必殺技用)
**目的**: 複雑な演出をTimeline で管理

- [ ] 必殺技用 Timeline テンプレート
- [ ] カスタムTrack (Expression Track, Effect Track)

---

## Phase 5: エフェクトシステム

### 5.1 エフェクト管理
**目的**: VFXの生成・制御

- [ ] EffectManager.cs (Singleton)
- [ ] EffectPool.cs (オブジェクトプール)
- [ ] アニメーションイベント連携

### 5.2 基本エフェクト作成
**目的**: 最低限のエフェクト素材

- [ ] 斬撃エフェクト (VFX Graph or Particle)
- [ ] ヒットエフェクト
- [ ] スキル発動エフェクト (オーラ等)

---

## Phase 6: 統合・テスト

### 6.1 テストシーン構築
- [ ] 敵キャラクター配置 (サンドバッグ)
- [ ] UI (HP, スキルクールダウン表示)
- [ ] デバッグ表示

### 6.2 調整・バランシング
- [ ] アニメーション速度調整
- [ ] ダメージ数値調整
- [ ] エフェクトタイミング調整

---

## 実装順序 (推奨)

```
Phase 1.1 (VRM) ──┐
                  ├──> Phase 2 (スキルシステム)
Phase 1.2 (Mixamo)┘         │
                            ├──> Phase 4 (カメラ)
Phase 1.3 (Animator)────────┤
                            ├──> Phase 5 (エフェクト)
Phase 3 (表情) ─────────────┘
                            │
                            v
                    Phase 6 (統合テスト)
```

---

## 必要なパッケージ/アセット

| パッケージ | 用途 | 必須度 |
|-----------|------|--------|
| UniVRM | VRMインポート | 必須 |
| Cinemachine | カメラ制御 | 推奨 |
| Timeline | 演出管理 | 推奨 |
| VFX Graph / Particle | エフェクト | 推奨 |
| DOTween | アニメーション補間 | 任意 |

---

## 見積もり時間

| Phase | 内容 | 推定時間 |
|-------|------|---------|
| Phase 1 | 基盤構築 | 4-6時間 |
| Phase 2 | スキルシステム | 6-8時間 |
| Phase 3 | 表情システム | 2-3時間 |
| Phase 4 | カメラ演出 | 4-6時間 |
| Phase 5 | エフェクト | 4-6時間 |
| Phase 6 | 統合テスト | 2-4時間 |
| **合計** | | **22-33時間** |

---

## 次のステップ

1. UniVRM をインストール
2. VRoid でテストキャラクター作成
3. Mixamo で基本アニメーションセット取得
4. Phase 2 のスキルデータ構造から実装開始

---

## 参考リソース

- UniVRM: https://github.com/vrm-c/UniVRM
- Mixamo: https://www.mixamo.com/
- VRoid Studio: https://vroid.com/studio
- VRM仕様: https://vrm.dev/
