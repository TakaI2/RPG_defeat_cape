# MagicaCloth2 頂点グラブ機能実装プラン

## プロジェクト目標

Move属性に設定された特定の頂点（1-2個程度）をランタイムで特定のTransform（grabpoint）に追従させる機能を実装する。

---

## 調査結果サマリー

### 実装可能性: ✅ **可能**

MagicaCloth2の内部構造を解析した結果、**dispPosArray（表示位置配列）を`OnPostSimulation`イベントで直接更新**することで、頂点を確実に特定位置に追従させることが可能と判明。

### 最重要発見 🔑

**dispPosArrayが最終的な表示位置を制御**:
- `basePosArray`: アニメーション姿勢（シミュレーションの基準）
- `nextPosArray`: シミュレーション結果（制約計算後）
- **`dispPosArray`: 実際のメッシュ表示に使用される最終座標** ⭐

`basePosArray`や`nextPosArray`を更新しても、シミュレーション処理で`dispPosArray`に正しく反映されなければ意味がない。**`OnPostSimulation`イベントで`dispPosArray`を直接更新することで、シミュレーション後の表示位置を確実にコントロール可能**。

---

## 技術的詳細

### MagicaCloth2のアーキテクチャ

#### 1. データ構造

**VertexAttribute** (`Assets/MagicaCloth2/Scripts/Core/VirtualMesh/VertexAttribute.cs`)
- `byte`型のビットフラグ
- `Flag_Fixed = 0x01`: 固定頂点
- `Flag_Move = 0x02`: 移動頂点
- `IsMove()`: Move属性判定メソッド

**VirtualMesh** (`Assets/MagicaCloth2/Scripts/Core/VirtualMesh/VirtualMesh.cs`)
```csharp
public ExSimpleNativeArray<VertexAttribute> attributes;  // 頂点属性
public ExSimpleNativeArray<float3> localPositions;       // ローカル座標
public NativeArray<float> vertexDepths;                  // 頂点深さ(0-1)
```

**SimulationManager** (`Assets/MagicaCloth2/Scripts/Core/Manager/Simulation/SimulationManager.cs`)
```csharp
public ExNativeArray<float3> basePosArray;     // アニメーション姿勢座標（基準位置）
public ExNativeArray<float3> nextPosArray;     // 現在のシミュレーション座標
public ExNativeArray<float3> dispPosArray;     // 表示座標（最終的なレンダリング用）⭐
public ExNativeArray<float3> velocityArray;    // 速度
```

#### 2. シミュレーションフロー（重要！詳細は「技術的注意事項」セクション参照）

1. **OnPreSimulation イベント** ← ユーザーコード介入ポイント1
2. **basePosArray設定**: VirtualMeshからスキニングでアニメーション姿勢を計算
3. **制約計算**: Distance, Bending, Motion, Collision等の制約がnextPosArrayを更新
4. **dispPosArray計算**: nextPosArrayから表示用座標を生成
5. **OnPostSimulation イベント** ← ユーザーコード介入ポイント2 ⭐推奨
6. **メッシュ書き込み**: dispPosArrayをレンダラーメッシュに反映

**重要な発見**:
- `OnPreSimulation`で`basePosArray`を更新しても、シミュレーション処理で上書きされる
- **`OnPostSimulation`で`dispPosArray`を直接更新すれば、確実に表示位置をコントロール可能**
- Fixed属性の頂点はシミュレーション処理から除外される（ただし、`dispPosArray`更新が必要）

#### 3. 既存API

**公開されているAPI** (`MagicaClothAPI.cs`)
- `ResetCloth(bool keepPose)`
- `AddForce(Vector3, float, ClothForceMode)`
- `SetTimeScale(float)`
- **パーティクル位置を直接操作するAPIは存在しない**

**利用可能なイベント**
- `MagicaManager.OnPreSimulation`: シミュレーション前に毎フレーム実行

---

## 推奨実装アプローチ

### 方式: basePosArrayの上書き

**難易度**: ⭐⭐⭐☆☆ (中程度)

**メリット**:
- ✅ 既存のイベントシステム（OnPreSimulation）を活用
- ✅ シミュレーションパイプラインを破壊しない
- ✅ Distance/Bending制約との協調が可能
- ✅ 実装が比較的シンプル

**デメリット**:
- ⚠️ 非公開APIへのアクセスが必要
- ⚠️ MagicaCloth2のアップデートで動作変更の可能性
- ⚠️ 毎フレーム実行によるパフォーマンス影響

---

## 実装プラン

### Phase 1: 基本プロトタイプ (目標: 全Move頂点の追従)

**実装内容**:
1. `ClothVertexGrabber`クラスを作成
2. `MagicaManager.OnPreSimulation`イベントに登録
3. Move属性の全頂点を検出
4. grabpointの位置をbasePosArrayに書き込み

**コード例**:
```csharp
public class ClothVertexGrabber : MonoBehaviour
{
    [SerializeField] private MagicaCloth magicaCloth;
    [SerializeField] private Transform grabPoint;
    [SerializeField] private bool isGrabbing = false;

    void OnEnable()
    {
        MagicaManager.OnPreSimulation += UpdateGrabbedVertex;
    }

    void OnDisable()
    {
        MagicaManager.OnPreSimulation -= UpdateGrabbedVertex;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            isGrabbing = true;
        if (Input.GetKeyUp(KeyCode.Space))
            isGrabbing = false;
    }

    void UpdateGrabbedVertex()
    {
        if (!isGrabbing || !magicaCloth.IsValid()) return;

        var process = magicaCloth.Process;
        int teamId = process.TeamId;
        ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);

        // パーティクル位置配列を取得
        var basePosArray = MagicaManager.Simulation.basePosArray;

        // 頂点属性を取得
        var proxyMesh = process.ProxyMeshContainer.shareVirtualMesh;
        var attributes = proxyMesh.attributes.GetNativeArray();

        int pIndex = tdata.particleChunk.startIndex;
        for (int i = 0; i < tdata.particleChunk.dataLength; i++, pIndex++)
        {
            var attr = attributes[i];
            if (attr.IsMove())
            {
                // grabpointの位置をクロスのローカル空間に変換
                Vector3 localPos = magicaCloth.ClothTransform.InverseTransformPoint(
                    grabPoint.position
                );
                basePosArray[pIndex] = localPos;
            }
        }
    }
}
```

**検証項目**:
- [ ] スペースキー押下で全Move頂点がgrabpointに追従するか
- [ ] スペースキー離すと元の動きに戻るか
- [ ] 他の制約（Distance, Bending）が正常に動作するか
- [ ] パフォーマンスへの影響を確認

---

### Phase 2: 特定頂点の選択 (目標: 1-2個の頂点のみ掴む)

**実装内容**:
1. 末端頂点（vertexDepth ≈ 1.0）を自動検出
2. または、エディタで手動指定したインデックスを使用
3. grabpoint最寄りの頂点を選択

**頂点選択ロジック案**:

#### 案A: 末端頂点の自動検出
```csharp
// vertexDepthsを使用して末端頂点を検出
var vertexDepths = proxyMesh.vertexDepths;
for (int i = 0; i < attributes.Length; i++)
{
    var attr = attributes[i];
    if (attr.IsMove() && vertexDepths[i] > 0.95f) // 末端付近のみ
    {
        // この頂点を操作対象にする
    }
}
```

#### 案B: grabpoint最寄りの頂点を選択
```csharp
// 現在のbasePosArrayからgrabpointに最も近い頂点を探す
float closestDistance = float.MaxValue;
int closestIndex = -1;

for (int i = 0; i < attributes.Length; i++)
{
    var attr = attributes[i];
    if (attr.IsMove())
    {
        Vector3 worldPos = magicaCloth.ClothTransform.TransformPoint(
            basePosArray[pIndex]
        );
        float distance = Vector3.Distance(worldPos, grabPoint.position);

        if (distance < closestDistance)
        {
            closestDistance = distance;
            closestIndex = i;
        }
    }
}

// closestIndexの頂点のみを操作
```

#### 案C: 手動インデックス指定
```csharp
[SerializeField] private int[] targetVertexIndices = new int[] { 0, 1 };

// 指定されたインデックスの頂点のみ操作
foreach (int vertexIndex in targetVertexIndices)
{
    if (vertexIndex < attributes.Length && attributes[vertexIndex].IsMove())
    {
        int pIndex = tdata.particleChunk.startIndex + vertexIndex;
        basePosArray[pIndex] = localGrabPos;
    }
}
```

**推奨**: 案B（最寄り頂点選択）+ 頂点数制限（最大2個）

**検証項目**:
- [ ] 特定の1-2個の頂点のみが掴まれるか
- [ ] 他のMove頂点は通常通りシミュレーションされるか
- [ ] 掴んだ頂点と他の頂点間のDistance制約が機能するか

---

### Phase 3: 改良と最適化

**実装内容**:
1. スムーズな遷移（掴む瞬間/離す瞬間の補間）
2. 複数grabpointのサポート
3. デバッグ用Gizmo表示（掴まれている頂点を視覚化）
4. パフォーマンス最適化

**改良案**:

#### スムーズな遷移
```csharp
private float grabStrength = 0f; // 0.0 ~ 1.0

void Update()
{
    if (Input.GetKey(KeyCode.Space))
        grabStrength = Mathf.MoveTowards(grabStrength, 1f, Time.deltaTime * 5f);
    else
        grabStrength = Mathf.MoveTowards(grabStrength, 0f, Time.deltaTime * 5f);
}

void UpdateGrabbedVertex()
{
    if (grabStrength < 0.01f) return; // 掴んでいない

    // basePosArrayを補間
    Vector3 targetPos = grabPoint.position;
    Vector3 originalPos = basePosArray[pIndex];
    basePosArray[pIndex] = Vector3.Lerp(originalPos, targetPos, grabStrength);
}
```

#### デバッグGizmo
```csharp
void OnDrawGizmos()
{
    if (!Application.isPlaying || !magicaCloth.IsValid()) return;

    // 掴まれている頂点を表示
    foreach (int grabbedIndex in currentlyGrabbedIndices)
    {
        Vector3 worldPos = GetVertexWorldPosition(grabbedIndex);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(worldPos, 0.05f);
    }

    // grabpointとの接続線
    Gizmos.color = Color.yellow;
    Gizmos.DrawLine(worldPos, grabPoint.position);
}
```

---

## 技術的注意事項

### 1. MagicaCloth2シミュレーションパイプライン（重要！）

#### Unityフレームの実行順序
```
1. EarlyUpdate
2. FixedUpdate
3. PreUpdate
4. Update
5. PreLateUpdate
6. LateUpdate
7. OnAfterLateUpdate ← ClothUpdateがここで実行される
8. Rendering
```

#### MagicaCloth2のフレーム内処理順序
```
フレーム開始
  ↓
1. Animation Update (Unity標準)
   → Animator/Transform更新
  ↓
2. BoneManager.ReadTransform
   → TransformからCloth用の姿勢データを読み取り
  ↓
3. OnPreSimulation イベント発火 ← ★ユーザーコードの介入ポイント1
   → basePosArray/nextPosArrayを更新可能
  ↓
4. Pre-Simulation Jobs
   → SkinningJob: VirtualMeshをスキニング
   → basePosArrayの計算（アニメーション姿勢）
  ↓
5. Simulation Step Loop (maxUpdateCount回反復)
   ├─ 速度更新、外力適用
   ├─ Tether Constraint
   ├─ Distance Constraint
   ├─ Angle Constraint
   ├─ Triangle Bending Constraint
   ├─ Collider Collision
   ├─ Self Collision
   └─ Motion Constraint
   → nextPosArrayが制約計算で更新される
  ↓
6. Post-Simulation Jobs
   → dispPosArrayの計算（表示座標）
   → ProxyMeshへの反映
  ↓
7. OnPostSimulation イベント発火 ← ★ユーザーコードの介入ポイント2
   → dispPosArrayを更新可能（この時点ではシミュレーション完了済み）
  ↓
8. ClothUpdate (OnAfterLateUpdateで実行)
   → dispPosArrayをメッシュに書き込み
  ↓
9. Rendering
   → 更新されたメッシュが表示される
```

#### 重要な配列の役割

| 配列名 | 役割 | 更新タイミング | 用途 |
|--------|------|----------------|------|
| **basePosArray** | アニメーション姿勢座標 | Pre-Simulation Jobsで毎フレーム再計算 | シミュレーションの基準姿勢 |
| **nextPosArray** | シミュレーション座標 | Simulation Step Loopで制約計算により更新 | 制約計算の結果 |
| **oldPosArray** | 1つ前のシミュレーション座標 | 毎フレーム保存 | 速度計算用 |
| **velocityArray** | 速度 | 毎フレーム計算 | 慣性・外力計算 |
| **dispPosArray** | **表示座標（最重要）** | **Post-Simulation Jobsで計算** | **実際のレンダリングに使用** |

**重要**: `dispPosArray`が実際のメッシュ表示に使われる最終的な座標です。`basePosArray`や`nextPosArray`を更新しても、シミュレーション処理でdispPosArrayに反映されなければ意味がありません。

#### イベントでの介入戦略

**OnPreSimulation**:
- basePosArray/nextPosArrayを更新可能
- ただし、その後のシミュレーション処理で上書きされる可能性が高い
- Fixed属性の頂点はシミュレーションから除外されるため、basePosArrayの値がそのまま使われる

**OnPostSimulation** ⭐推奨:
- dispPosArrayを直接更新可能
- シミュレーション完了後なので、上書きされる心配がない
- 表示位置を確実にコントロールできる

#### 頂点属性による挙動の違い

```csharp
// Move属性（通常のシミュレーション頂点）
// - basePosArrayは基準として使用される
// - nextPosArrayは制約計算で更新される
// - dispPosArrayは補間計算で生成される

// Fixed属性（固定頂点）
// - シミュレーションから除外される
// - basePosArrayの値がそのまま使われる
// - dispPosArrayはbasePosArrayと同じになる
```

### 2. 座標系の変換

MagicaCloth2は3つの座標系を使用:
- **ワールド空間**: Unity標準
- **クロスローカル空間**: ClothTransformのローカル空間
- **VirtualMeshローカル空間**: ProxyMeshのローカル空間

basePosArray/nextPosArray/dispPosArrayは**クロスローカル空間**で管理されているため:
```csharp
Vector3 localPos = magicaCloth.ClothTransform.InverseTransformPoint(grabPoint.position);
```

### 3. パーティクルインデックスの対応

VirtualMesh頂点インデックスとパーティクルインデックスの変換:
```csharp
// VirtualMesh index → Particle index
int particleIndex = tdata.particleChunk.startIndex + virtualMeshIndex;

// Particle index → VirtualMesh index
int virtualMeshIndex = particleIndex - tdata.particleChunk.startIndex;
```

### 4. スレッドセーフティ

**メインスレッド実行**:
- `OnPreSimulation`: メインスレッドで実行（Simulation Jobs実行前）
- `OnPostSimulation`: メインスレッドで実行（Simulation Jobs完了後）
- 両イベントでのNativeArray操作は安全

**注意事項**:
- Simulation Jobs実行中（OnPreSimulationとOnPostSimulationの間）はアクセス禁止
- JobHandle.Completeを手動で呼ぶと、他のシステムに影響が出る可能性があるため避ける

### 5. パフォーマンス考慮

全頂点を毎フレームループするのは非効率。最適化案:
- 掴んでいない時はイベント登録を解除
- 操作対象の頂点インデックスをキャッシュ
- 不要な座標変換を削減

### 6. 重要なソースコード参照箇所

今回の調査で判明した、MagicaCloth2の重要なコード箇所:

**SimulationManager.cs**:
- Line 64: `dispPosArray` - 表示座標の定義
- Line 38: `basePosArray` - アニメーション姿勢座標の定義
- Line 22: `nextPosArray` - シミュレーション座標の定義

**SimulationManagerNormal.cs**:
- Line 906-971: `dispPosArray`の計算処理
- 補間計算により最終的な表示座標を生成

**ClothManager.cs**:
- Line 217前後: `MagicaManager.OnPreSimulation?.Invoke()` - イベント発火箇所
- OnAfterLateUpdateコールバックで`ClothUpdate()`を実行

**MagicaManagerAPI.cs**:
- Line 18: `public static Action OnPreSimulation` - Pre-simulationイベント定義
- Line 24: `public static Action OnPostSimulation` - Post-simulationイベント定義

**ClothSerializeData.cs**:
- Line 248: `MotionConstraint.SerializeData` - Motion Constraint設定
- Line 223: `TetherConstraint.SerializeData` - Tether Constraint設定
- Line 228: `DistanceConstraint.SerializeData` - Distance Constraint設定

これらのファイルを参照すれば、シミュレーションの詳細な挙動を理解できます。

---

## 実装スケジュール

### セッション1（次回）: Phase 1実装
- [ ] `ClothVertexGrabber.cs`を作成
- [ ] 基本プロトタイプを実装
- [ ] cape2で動作確認
- [ ] 全Move頂点が追従することを確認

**所要時間見積**: 30-45分

### セッション2: Phase 2実装
- [ ] 頂点選択ロジックを実装（案B推奨）
- [ ] 1-2個の頂点のみ掴むように改良
- [ ] エッジケースのテスト

**所要時間見積**: 45-60分

### セッション3: Phase 3実装（オプショナル）
- [ ] スムーズな遷移を追加
- [ ] デバッグGizmoを実装
- [ ] パフォーマンス最適化
- [ ] 最終調整

**所要時間見積**: 30-45分

---

## 既知の制約・リスク

### 制約
1. **非公開API使用**: MagicaCloth2の内部実装に依存
2. **更新リスク**: アセットストアのアップデートで動作変更の可能性
3. **パフォーマンス**: 大量の頂点を操作すると負荷が高い

### リスク軽減策
1. バージョン管理でMagicaCloth2を固定
2. 操作対象の頂点数を最小限に抑える
3. 掴んでいない時は処理をスキップ

---

## 参考資料

### 重要なファイルパス

**コアシステム**:
- `Assets/MagicaCloth2/Scripts/Core/Manager/MagicaManager.cs`
- `Assets/MagicaCloth2/Scripts/Core/Manager/Simulation/SimulationManager.cs`
- `Assets/MagicaCloth2/Scripts/Core/Manager/Team/TeamManager.cs`

**クロスシステム**:
- `Assets/MagicaCloth2/Scripts/Core/Cloth/MagicaCloth.cs`
- `Assets/MagicaCloth2/Scripts/Core/Cloth/MagicaClothAPI.cs`
- `Assets/MagicaCloth2/Scripts/Core/Cloth/ClothProcess.cs`

**データ構造**:
- `Assets/MagicaCloth2/Scripts/Core/VirtualMesh/VirtualMesh.cs`
- `Assets/MagicaCloth2/Scripts/Core/VirtualMesh/VertexAttribute.cs`
- `Assets/MagicaCloth2/Scripts/Core/Cloth/SelectionData.cs`

**制約システム**:
- `Assets/MagicaCloth2/Scripts/Core/Cloth/Constraints/MotionConstraint.cs`
- `Assets/MagicaCloth2/Scripts/Core/Cloth/Constraints/DistanceConstraint.cs`

---

## 作成日
2025-11-24

## 最終更新日
2025-11-24 (Phase 1実装中)

## ステータス
Phase 0: 調査完了 ✅
Phase 1: 実装中 🔄
Phase 2: 未着手
Phase 3: 未着手

---

## Phase 1 実装進捗（2025-11-24）

### 実装完了項目

#### 1. ClothVertexGrabber.cs 基本実装 ✅
**ファイル**: `Assets/Scripts/ClothVertexGrabber.cs`

**実装機能**:
- OnPreSimulationイベントへの登録/解除
- スペースキーによるグラブ制御
- grabpoint・cape2の自動検出
- TeamIDの初期化

**主要メソッド**:
```csharp
void OnEnable() // OnPreSimulationイベント登録
void OnDisable() // イベント解除
void Update() // スペースキー入力検出
void StartGrabbing() // グラブ開始処理
void StopGrabbing() // グラブ解放処理
void UpdateGrabbedVertex() // 毎フレーム頂点位置更新
void OnDrawGizmos() // デバッグ可視化
```

#### 2. 特定頂点選択機能 ✅
**変更内容**:
- 全Move頂点を操作する方式から、最寄りの1-2個の頂点のみを選択する方式に変更
- grabpointからの距離でソート、最も近い頂点を選択
- `maxGrabbedVertices`パラメータで頂点数を制限（デフォルト2）

**頂点選択ロジック**:
```csharp
void StartGrabbing()
{
    // 全Move頂点とgrabpointの距離を計算
    var candidates = new List<(int index, float distance)>();

    // 距離でソート
    candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

    // 最も近い頂点を選択
    int numToGrab = Mathf.Min(maxGrabbedVertices, candidates.Count);
    grabbedVertexIndices = new int[numToGrab];
}
```

#### 3. basePosArray・nextPosArray・velocityArray 操作 ✅
**実装内容**:
```csharp
void UpdateGrabbedVertex()
{
    var basePosArray = MagicaManager.Simulation.basePosArray;
    var nextPosArray = MagicaManager.Simulation.nextPosArray;
    var velocityArray = MagicaManager.Simulation.velocityArray;

    foreach (int vertexIndex in grabbedVertexIndices)
    {
        int particleIndex = tdata.particleChunk.startIndex + vertexIndex;

        basePosArray[particleIndex] = grabPointLocalPos;
        nextPosArray[particleIndex] = grabPointLocalPos;
        velocityArray[particleIndex] = Vector3.zero;
    }
}
```

#### 4. デバッグGizmo実装 ✅
**可視化内容**:
- grabpoint位置をワイヤーフレーム球で表示（掴み中は赤、通常は黄色）
- 掴まれている頂点をシアン色の球で表示
- 頂点とgrabpointを線で接続

#### 5. Motion Constraint無効化 ✅
**問題**: 頂点位置を更新してもMotion Constraintが元の位置に引き戻す
**対策**: Start()でMotion Constraintを無効化
```csharp
var sdata = magicaCloth.SerializeData;
sdata.motionConstraint.mode = MotionConstraint.Mode.None;
magicaCloth.SetParameterChange();
```

---

### 現在直面している問題 ⚠️

#### 問題1: 頂点が動かない（解決済み） ✅
**症状**:
- ログは正常に出力される（「2 vertices selected」「Updating 2 vertices...」）
- Gizmoでシアン色の球が表示される（頂点は選択されている）
- しかし実際の布メッシュがgrabpointに追従しない
- シアンの球が布の頂点とgrabpointを高速で往復している

**根本原因の発見**:
MagicaCloth2のシミュレーションパイプラインの理解不足が原因でした:

1. **basePosArray**: アニメーション姿勢座標（毎フレーム再計算される）
2. **nextPosArray**: シミュレーション座標（制約計算で更新される）
3. **dispPosArray**: 表示座標（実際のレンダリングに使用される）

```
フレームの流れ:
1. Animation Update → basePosArray更新
2. OnPreSimulation イベント ← 我々の更新(basePosArray/nextPosArray)
3. Simulation Jobs → 制約計算 → nextPosArray更新（我々の更新を上書き）
4. Post-Simulation → dispPosArray計算（nextPosArrayから）
5. OnPostSimulation イベント
6. Rendering → dispPosArrayを使用
```

我々がOnPreSimulationでbasePosArray/nextPosArrayを更新しても、その後のシミュレーション処理で上書きされていました。

**解決策: dispPosArray直接更新アプローチ** ✅

Fixed属性 + dispPosArray直接更新の組み合わせ:

```csharp
// OnEnable
MagicaManager.OnPreSimulation += UpdateGrabbedVertex;
MagicaManager.OnPostSimulation += ForceUpdateDisplayPosition; // 追加

// OnPreSimulation: Fixed属性の頂点として設定
void UpdateGrabbedVertex()
{
    // Fixed属性の頂点用にbasePosArrayを更新
    basePosArray[particleIndex] = grabPointLocalPos;
    nextPosArray[particleIndex] = grabPointLocalPos;
}

// OnPostSimulation: 表示位置を強制的に上書き
void ForceUpdateDisplayPosition()
{
    // シミュレーション完了後、dispPosArrayを直接更新
    var dispPosArray = MagicaManager.Simulation.dispPosArray;
    foreach (int vertexIndex in grabbedVertexIndices)
    {
        int particleIndex = tdata.particleChunk.startIndex + vertexIndex;
        dispPosArray[particleIndex] = grabPointLocalPos;
    }
}
```

**実装した対策**:
- ✅ Fixed属性に変更してシミュレーションから除外
- ✅ OnPostSimulationイベントでdispPosArrayを直接更新
- ✅ OnDrawGizmosもdispPosArrayを使用するように変更
- ✅ Motion/Tether/Distance Constraintを無効化

**次のステップ**:
1. ✅ 実機テストでdispPosArray更新アプローチを検証 → 成功
2. ✅ Fixed属性をやめてMove属性維持に変更 → 布全体が引っ張られるように
3. ✅ 振動対策を実装 → 次回テストで確認

---

## 実装完了（2025-01-XX セッション）

### 成果まとめ

**実装した機能** ✅:
1. 特定の2頂点をgrabpointに追従させる
2. 布全体が自然に引っ張られる
3. 振動を防ぐ安定化処理

**技術的ブレークスルー**:

#### 1. dispPosArray直接更新アプローチ
OnPostSimulationイベントで`dispPosArray`を直接更新することで、シミュレーション後の表示位置を確実にコントロール。

#### 2. Move属性維持による制約活用
Fixed属性ではなくMove属性のまま維持することで、Distance Constraintが働き、周囲の頂点が引っ張られる。

```csharp
// Fixed属性: シミュレーション除外 → 周囲が引っ張られない
// Move属性: シミュレーション対象 → 周囲が引っ張られる ⭐採用
```

#### 3. 振動防止の4点セット
```csharp
basePosArray[particleIndex] = grabPointLocalPos;   // 基準位置
nextPosArray[particleIndex] = grabPointLocalPos;   // 現在位置
oldPosArray[particleIndex] = grabPointLocalPos;    // 前フレーム位置
velocityArray[particleIndex] = Vector3.zero;       // 速度ゼロ
```

すべての位置配列を統一し、速度をゼロにすることで振動を防ぐ。

### 最終実装コード

**イベント登録**:
```csharp
void OnEnable()
{
    MagicaManager.OnPreSimulation += UpdateGrabbedVertex;
    MagicaManager.OnPostSimulation += ForceUpdateDisplayPosition;
}

void OnDisable()
{
    MagicaManager.OnPreSimulation -= UpdateGrabbedVertex;
    MagicaManager.OnPostSimulation -= ForceUpdateDisplayPosition;
}
```

**OnPreSimulation: 位置と速度の更新**:
```csharp
void UpdateGrabbedVertex()
{
    var basePosArray = MagicaManager.Simulation.basePosArray;
    var nextPosArray = MagicaManager.Simulation.nextPosArray;
    var oldPosArray = MagicaManager.Simulation.oldPosArray;
    var velocityArray = MagicaManager.Simulation.velocityArray;

    Vector3 grabPointLocalPos = magicaCloth.ClothTransform.InverseTransformPoint(grabPoint.position);

    foreach (int vertexIndex in grabbedVertexIndices)
    {
        int particleIndex = tdata.particleChunk.startIndex + vertexIndex;

        // すべての位置配列を統一
        basePosArray[particleIndex] = grabPointLocalPos;
        nextPosArray[particleIndex] = grabPointLocalPos;
        oldPosArray[particleIndex] = grabPointLocalPos;

        // 速度をゼロに
        velocityArray[particleIndex] = Vector3.zero;
    }
}
```

**OnPostSimulation: 表示位置の強制更新**:
```csharp
void ForceUpdateDisplayPosition()
{
    var dispPosArray = MagicaManager.Simulation.dispPosArray;
    Vector3 grabPointLocalPos = magicaCloth.ClothTransform.InverseTransformPoint(grabPoint.position);

    foreach (int vertexIndex in grabbedVertexIndices)
    {
        int particleIndex = tdata.particleChunk.startIndex + vertexIndex;
        dispPosArray[particleIndex] = grabPointLocalPos;
    }
}
```

**制約設定**:
```csharp
void Start()
{
    var sdata = magicaCloth.SerializeData;

    // Motion Constraint: 無効化（grabpointまで移動可能に）
    sdata.motionConstraint.useMaxDistance = false;
    sdata.motionConstraint.useBackstop = false;

    // Tether Constraint: 無効化（初期位置への引き戻しを防ぐ）
    sdata.tetherConstraint.distanceCompression = 0.0f;

    // Distance Constraint: 有効のまま（周囲の頂点が引っ張られる）
    // stiffnessは変更しない

    magicaCloth.SetParameterChange();
}
```

### 動作状況

**確認済み** ✅:
- ✅ 掴んだ2頂点がgrabpointに追従
- ✅ 布全体が引っ張られる（Distance Constraintが機能）
- ✅ シアン色のギズモが正しい位置に表示

**次回確認事項**:
- ⏳ 掴んだ頂点の振動が解消されているか
- ⏳ grabpointを動かしたときの追従性

### 残課題

なし（振動対策が効けば完成）

---

### 技術的発見事項

#### 座標変換の重要性
grabpointのワールド座標をClothTransformのローカル空間に変換する必要がある:
```csharp
Vector3 grabPointLocalPos = magicaCloth.ClothTransform.InverseTransformPoint(grabPoint.position);
```

#### TeamIDの初期化タイミング
- `magicaCloth.Process.TeamId`は、MagicaClothが`IsValid()`になった後でないと取得できない
- `StartGrabbing()`と`UpdateGrabbedVertex()`の両方で初期化チェックが必要

#### MagicaCloth2の制約システム
MagicaCloth2は以下の順序でシミュレーションを実行:
1. **OnPreSimulation** ← 現在の介入ポイント
2. basePosArrayから初期姿勢を設定
3. 制約計算（Distance, Bending, Motion, Tether等）がnextPosArrayを更新
4. 結果をレンダラーに反映

→ OnPreSimulationでbasePosArrayを変更しても、制約計算で上書きされる可能性がある

---

### 次回セッションで試すこと

#### 優先度1: Tether Constraintも無効化
```csharp
sdata.tetherConstraint.mode = TetherConstraint.Mode.None;
```

#### 優先度2: Distance Constraintの調整
```csharp
// ストレッチ許容度を上げる
sdata.distanceConstraint.stiffness.SetValue(0.1f); // デフォルトは1.0
```

#### 優先度3: 制約計算の後に上書き
OnPreSimulationではなく、制約計算後に介入する方法を調査:
- `MagicaManager.OnPostSimulation`イベントがあるか確認
- または、独自のLateUpdateで`nextPosArray`を直接上書き

#### 優先度4: Fixed属性に変更
Move属性の頂点ではなく、一時的にFixed属性に変更してgrabpointに固定:
```csharp
// 頂点属性をFixedに変更し、位置を固定
attributes[vertexIndex] = VertexAttribute.Fixed;
```

---

### パラメータ設定

**現在の設定**:
```csharp
[SerializeField] private int maxGrabbedVertices = 2;
[SerializeField] private float grabSpeed = 10f; // 現在未使用（直接設定に変更）
```

**grabSpeed削除の理由**:
- Lerpによる緩やかな移動では制約に負ける可能性があるため
- 直接位置設定に変更して制約との競合を確認中

---

### コード構造の改善点（将来）

#### 1. 初期化処理の統合
現在、`StartGrabbing()`と`UpdateGrabbedVertex()`の両方で初期化チェックを実施。
→ 統一した初期化メソッドを作成すべき

#### 2. エラーハンドリング
`GetTeamDataRef()`がinvalidなteamIdで呼ばれるとクラッシュする可能性。
→ try-catchまたは事前チェックを追加

#### 3. パフォーマンス最適化
毎フレーム`GetNativeArray()`を呼んでいる。
→ 初回にキャッシュして再利用

---

## 参考ログ出力

**正常動作時のログ**:
```
[ClothVertexGrabber] Initialized - MagicaCloth: Magica Cloth, GrabPoint: grabpoint
[ClothVertexGrabber] Registered to OnPreSimulation event
[ClothVertexGrabber] Initialized with TeamId: 1
[ClothVertexGrabber] Grabbing started - 2 vertices selected
[ClothVertexGrabber] Updating 2 vertices, lerpFactor: 0.xxx, target: (0.00, 2.00, 0.00)
```

**Gizmo描画**:
- 黄色ワイヤー球: grabpoint（非掴み中）
- 赤色ワイヤー球 + 赤色球: grabpoint（掴み中）
- シアン色球: 掴まれている頂点の位置
- シアン色線: 頂点とgrabpointの接続

---

## 検証項目チェックリスト

### Phase 1 基本プロトタイプ
- [x] ClothVertexGrabber.cs作成
- [x] OnPreSimulationイベント登録
- [x] スペースキーでグラブ開始/解放
- [x] cape2とgrabpointの自動検出
- [x] TeamID初期化
- [x] 特定頂点の選択（距離ベース）
- [x] basePosArray/nextPosArray/velocityArray更新
- [x] デバッグGizmo表示
- [ ] **頂点が実際にgrabpointに追従する** ← 未達成
- [ ] 他の制約（Distance, Bending）が正常に動作
- [ ] パフォーマンスへの影響確認

### 残タスク
- [ ] Motion Constraint無効化のテスト結果確認
- [ ] Tether Constraint無効化
- [ ] OnPostSimulation等の代替手段調査
- [ ] 制約パラメータの調整
