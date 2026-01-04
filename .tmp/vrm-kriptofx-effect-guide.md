# VRMキャラクターにKriptoFXエフェクトを適用する手順

## 概要

VRoid Studio等で作成したVRMキャラクターに、KriptoFX Realistic Effects Pack v4のエフェクトを適用する方法を説明します。

## 前提条件

- Unity 6000.0.46f1 以上
- VRM 1.0 パッケージ (UniVRM)
- KriptoFX Realistic Effects Pack v4

## 仕組み

KriptoFXのエフェクトシステムは以下の構成で動作します：

```
キャラクター (Animator + RFX4_EffectEvent)
├── SkinnedMeshRenderer (キャラクターモデル)
└── ボーン階層
    └── RightHand ← エフェクトがここにアタッチされる
        └── Effect_Hand(Clone) ← 自動生成
```

### 主要コンポーネント

| コンポーネント | 役割 |
|--------------|------|
| **Animator** | キャラクターアニメーション制御 |
| **RFX4_EffectEvent** | エフェクト発動のタイミングと場所を制御 |

## 手順

### Step 1: VRMキャラクターをシーンに配置

1. VRMファイル（例：`megu.vrm`）をシーンにドラッグ&ドロップ
2. 位置を調整

### Step 2: Animatorコントローラーを設定

VRMキャラクターには既にAnimatorコンポーネントがありますが、アニメーションコントローラーを設定する必要があります。

**設定方法：**
1. VRMキャラクターを選択
2. Inspectorで**Animator**コンポーネントを確認
3. **Controller**フィールドに以下を設定：
   - `Assets/KriptoFX/Realistic Effects Pack v4/Effects/Models/Character/Animators/Anim1.controller`
   - または自作のアニメーションコントローラー

### Step 3: RFX4_EffectEventコンポーネントを追加

1. VRMキャラクターを選択
2. **Add Component** → `RFX4_EffectEvent` を検索して追加

### Step 4: RFX4_EffectEventを設定

| プロパティ | 設定値 | 説明 |
|-----------|--------|------|
| **Character Effect** | `Effect1_Hand.prefab` | 手に表示されるエフェクト |
| **Character Attach Point** | `J_Bip_R_Hand` | 右手ボーン |
| **Character Effect Destroy Time** | 10 | エフェクトの生存時間 |
| **Character Effect 2** | (任意) | 両手使用時 |
| **Character Attach Point 2** | `J_Bip_L_Hand` | 左手ボーン |
| **Main Effect** | `Effect1.prefab` | 発射されるエフェクト |
| **Attach Point** | `J_Bip_R_Hand` | 発射位置 |
| **Override Attach Point To Target** | `Target` | エフェクトの目標 |
| **Effect Destroy Time** | 3 | メインエフェクトの生存時間 |

### Step 5: ターゲットオブジェクトを作成（オプション）

エフェクトが向かう先のターゲットが必要な場合：

1. 空のGameObjectを作成（名前：`EffectTarget`）
2. 任意の位置に配置
3. **Override Attach Point To Target**に設定

## VRMボーン対応表

VRMのボーン名は標準的なHumanoidボーンと異なります。

| 部位 | VRMボーン名 | 標準Humanoid名 |
|------|------------|---------------|
| 右手 | `J_Bip_R_Hand` | `RightHand` |
| 左手 | `J_Bip_L_Hand` | `LeftHand` |
| 頭 | `J_Bip_C_Head` | `Head` |
| 腰 | `J_Bip_C_Hips` | `Hips` |
| 右肩 | `J_Bip_R_Shoulder` | `RightShoulder` |
| 左肩 | `J_Bip_L_Shoulder` | `LeftShoulder` |

## VRMボーン階層

```
VRMキャラクター
└── Root
    └── J_Bip_C_Hips
        ├── J_Bip_C_Spine
        │   └── J_Bip_C_Chest
        │       └── J_Bip_C_UpperChest
        │           ├── J_Bip_C_Neck
        │           │   └── J_Bip_C_Head
        │           ├── J_Bip_R_Shoulder
        │           │   └── J_Bip_R_UpperArm
        │           │       └── J_Bip_R_LowerArm
        │           │           └── J_Bip_R_Hand
        │           │               └── (指ボーン...)
        │           └── J_Bip_L_Shoulder
        │               └── (左腕...)
        ├── J_Bip_R_UpperLeg
        │   └── (右足...)
        └── J_Bip_L_UpperLeg
            └── (左足...)
```

## 使用可能なエフェクト一覧

KriptoFX Realistic Effects Pack v4には27種類のエフェクトが含まれています：

- `Effect1.prefab` ～ `Effect27.prefab`
- 対応するハンドエフェクト: `Effect1_Hand.prefab` など

---

## Mixamoアニメーションとエフェクトの連携

### 仕組み

KriptoFXは**Animation Events**を使ってエフェクトをトリガーしています。

```
アニメーション再生
    ↓
Animation Event発火 (特定フレームで)
    ↓
RFX4_EffectEventのメソッド呼び出し
    ↓
エフェクト生成
```

### 利用可能なトリガーメソッド

`RFX4_EffectEvent`コンポーネントには以下のpublicメソッドがあり、Animation Eventから呼び出せます：

| メソッド名 | 用途 | タイミング例 |
|-----------|------|-------------|
| `ActivateCharacterEffect()` | 手のエフェクト（溜め） | 詠唱開始時 |
| `ActivateCharacterEffect2()` | 左手エフェクト | 両手魔法時 |
| `ActivateEffect()` | メインエフェクト発射 | 攻撃モーションのピーク |
| `ActivateAdditionalEffect()` | 追加エフェクト | 着弾時など |

### Mixamoアニメーションの準備

#### Step 1: Mixamoからダウンロード

1. [Mixamo](https://www.mixamo.com/) にアクセス
2. アニメーションを選択（例：Standing Melee Attack, Magic Attack など）
3. ダウンロード設定：
   - **Format**: FBX for Unity (.fbx)
   - **Skin**: Without Skin（アニメーションのみの場合）
   - **Frames per Second**: 30
   - **Keyframe Reduction**: none

#### Step 2: Unityにインポート

1. ダウンロードしたFBXファイルを `Assets/Animations/Mixamo/` などにドラッグ&ドロップ

#### Step 3: FBXのRig設定

1. FBXファイルを選択
2. Inspectorで**Rig**タブを開く
3. 以下を設定：
   - **Animation Type**: Humanoid
   - **Avatar Definition**: Create From This Model
4. **Apply**をクリック

#### Step 4: Animation Eventsを追加

1. Inspectorで**Animation**タブを開く
2. クリップ名を選択（例：`mixamo.com`）
3. プレビュー下の**Events**セクションを展開
4. タイムラインをスクラブしてエフェクト発動フレームを決定
5. **+** ボタンでイベントを追加
6. **Function**フィールドにメソッド名を入力：
   - `ActivateCharacterEffect` （溜め開始）
   - `ActivateEffect` （発射）
7. **Apply**をクリック

### Animation Events設定の図解

```
┌─────────────────────────────────────────────────────────────┐
│ Inspector > Animation Tab                                   │
├─────────────────────────────────────────────────────────────┤
│ Clips: [mixamo.com ▼]                                       │
│                                                             │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ Preview Timeline                                        │ │
│ │ ├──●────────────●──────────────────────●───────────────│ │
│ │ │  ↑            ↑                      ↑               │ │
│ │ │  0:00         0:15                   0:45            │ │
│ │ └─────────────────────────────────────────────────────┘ │
│                                                             │
│ Events:                                                     │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ [+] [-]                                                 │ │
│ │                                                         │ │
│ │ Event 0:                                                │ │
│ │   Time: 0.15                                            │ │
│ │   Function: ActivateCharacterEffect                     │ │
│ │                                                         │ │
│ │ Event 1:                                                │ │
│ │   Time: 0.45                                            │ │
│ │   Function: ActivateEffect                              │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                             │
│ [Apply] [Revert]                                            │
└─────────────────────────────────────────────────────────────┘
```

### Step 5: Animator Controllerに追加

1. 使用するAnimator Controller（または新規作成）を開く
2. Animator Windowで右クリック → **Create State** → **Empty**
3. 新しいステートを選択し、**Motion**にMixamoのクリップを設定
4. トランジションを設定（例：Idle → Attack → Idle）

### 実践例：魔法攻撃アニメーション

#### シナリオ
Mixamoの「Standing Magic Attack」に炎エフェクトを追加

#### イベント配置

| フレーム | 時間(30fps) | イベント | 説明 |
|---------|------------|----------|------|
| 5 | 0.17s | `ActivateCharacterEffect` | 手に炎を溜める |
| 25 | 0.83s | `ActivateEffect` | 炎の弾を発射 |

#### RFX4_EffectEvent設定

| プロパティ | 設定値 |
|-----------|--------|
| Character Effect | `Effect7_Hand.prefab`（炎） |
| Character Attach Point | `J_Bip_R_Hand` |
| Main Effect | `Effect7.prefab` |
| Attach Point | `J_Bip_R_Hand` |

### よくあるアニメーションパターン

#### 近接攻撃（剣を振る）
```
0:00 ─────── 0:15 ─────── 0:30 ─────── 0:45
構え         振りかぶり    ヒット        戻り
                          ↑
                    ActivateEffect
                    (斬撃エフェクト)
```

#### 魔法詠唱
```
0:00 ─────── 0:30 ─────── 1:00 ─────── 1:30
詠唱開始     溜め中        発射          終了
↑                         ↑
ActivateCharacterEffect   ActivateEffect
(手のオーラ)              (魔法弾)
```

#### 両手魔法
```
0:00 ─────── 0:20 ─────── 0:40 ─────── 1:00
両手広げる   溜め          合わせる      発射
↑           ↑                          ↑
Activate    Activate                   ActivateEffect
Character   Character2
Effect      (左手)
(右手)
```

### Tips

1. **タイミング調整**: Animation Eventの位置はドラッグで微調整可能
2. **複数エフェクト**: 同じフレームに複数のイベントを追加可能
3. **プレビュー確認**: Animationウィンドウでプレビュー再生しながら調整
4. **ループアニメーション**: ループする場合はエフェクトの生存時間に注意

## トラブルシューティング

### エフェクトが表示されない

1. **Character Attach Point**が正しく設定されているか確認
2. Animatorが再生状態か確認
3. エフェクトプリファブのパスが正しいか確認

### アニメーションがおかしい

1. VRMのAvatarが正しく設定されているか確認
2. AnimatorのApply Root Motionを確認

### エフェクトの位置がずれる

1. ボーンの向きを確認
2. エフェクトプリファブ内のオフセットを調整

## 参考

- KriptoFXデモシーン: `Assets/KriptoFX/Realistic Effects Pack v4/Mobile_Demo(LegacyRendering).unity`
- 元のキャラクター構成: `Effect1(Clone)` オブジェクトを参照

---

**作成日**: 2026-01-03
**動作確認環境**: Unity 6000.0.46f1 + VRM 1.0 + KriptoFX Realistic Effects Pack v4
