# Cloth Grab API Usage Guide

## 概要

v0.4.4では、`ClothVertexGrabber`に外部制御用の公開APIを追加しました。これにより、アニメーション、タイムライン、他のスクリプトから布のグラブを制御できるようになりました。

## 新しい公開API

### ClothVertexGrabber.cs

#### インデックスによる制御

```csharp
// グラブポイントのインデックス（0-based）でグラブを開始
public void StartGrabbingAtPoint(int grabPointIndex)

// グラブポイントのインデックス（0-based）でグラブを停止
public void StopGrabbingAtPoint(int grabPointIndex)

// グラブポイントのインデックス（0-based）でグラブをトグル
public void ToggleGrabbingAtPoint(int grabPointIndex)

// グラブ中かどうかを確認
public bool IsGrabbingAtPoint(int grabPointIndex)
```

#### 名前による制御

```csharp
// グラブポイントの名前でグラブを開始
public void StartGrabbingAtPoint(string grabPointName)

// グラブポイントの名前でグラブを停止
public void StopGrabbingAtPoint(string grabPointName)

// グラブポイントの名前でグラブをトグル
public void ToggleGrabbingAtPoint(string grabPointName)

// グラブ中かどうかを確認
public bool IsGrabbingAtPoint(string grabPointName)
```

## 使用方法

### 1. アニメーションイベントから制御

#### 手順

1. **ClothGrabController スクリプトをキャラクターに追加**
   - Unity-chanなどのキャラクターGameObjectに`ClothGrabController`をアタッチ
   - Inspectorで`Cloth Grabber`フィールドに`ClothVertexGrabber`をアサイン

2. **アニメーションウィンドウでイベントを追加**
   - Window > Animation > Animationを開く
   - アニメーションクリップを選択
   - グラブを発動したいフレームにイベントマーカーを追加
   - イベント関数を選択（例：`OnGrabCapeWithRightHand`）

3. **利用可能なイベント関数**
   - `OnGrabCapeWithRightHand()` - 右手でケープをつかむ
   - `OnReleaseCapeFromRightHand()` - 右手からケープを離す
   - `OnGrabCapeWithLeftHand()` - 左手でケープをつかむ
   - `OnReleaseCapeFromLeftHand()` - 左手からケープを離す
   - `OnToggleRightHandGrab()` - 右手のグラブをトグル
   - `OnGrabByName()` - カスタム名でグラブ
   - `OnReleaseByName()` - カスタム名でリリース

#### 設定例

```
Animation: "Walk"
Frame 10: OnGrabCapeWithRightHand
Frame 50: OnReleaseCapeFromRightHand
```

### 2. Timelineから制御

1. **Animation TrackまたはSignal Trackを使用**
   - Window > Sequencing > Timeline
   - Animation TrackにAnimation Event Markerを追加
   - または、Signal Trackを使用してカスタムシグナルを発火

2. **ClothGrabControllerのメソッドを呼び出し**

### 3. 他のスクリプトから直接制御

#### 方法A: ClothGrabController経由（推奨）

```csharp
using UnityEngine;

public class MyCharacterScript : MonoBehaviour
{
    private ClothGrabController grabController;

    void Start()
    {
        grabController = GetComponent<ClothGrabController>();
    }

    void Update()
    {
        // 例：Gキーで右手でつかむ
        if (Input.GetKeyDown(KeyCode.G))
        {
            grabController.GrabCapeWithRightHand();
        }

        // 例：Gキーを離すと放す
        if (Input.GetKeyUp(KeyCode.G))
        {
            grabController.ReleaseCapeFromRightHand();
        }

        // 例：特定のグラブポイントをインデックスで制御
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            grabController.GrabAtPoint(0);
        }

        // 例：全てのグラブポイントを解放
        if (Input.GetKeyDown(KeyCode.R))
        {
            grabController.ReleaseAll();
        }
    }
}
```

#### 方法B: ClothVertexGrabber直接制御

```csharp
using UnityEngine;

public class MyCharacterScript : MonoBehaviour
{
    private ClothVertexGrabber clothGrabber;

    void Start()
    {
        clothGrabber = GetComponent<ClothVertexGrabber>();
    }

    void Update()
    {
        // インデックスでグラブ開始
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            clothGrabber.StartGrabbingAtPoint(0);
        }

        // 名前でグラブ停止
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            clothGrabber.StopGrabbingAtPoint("grabpoint1");
        }

        // トグル機能
        if (Input.GetKeyDown(KeyCode.T))
        {
            clothGrabber.ToggleGrabbingAtPoint(0);
        }

        // グラブ状態の確認
        if (clothGrabber.IsGrabbingAtPoint(0))
        {
            Debug.Log("Currently grabbing at point 0");
        }
    }
}
```

### 4. ゲームプレイでの活用例

#### 例1: 攻撃アニメーションでマントをつかむ

```
Animation: "Attack_Combo1"
Frame 5: OnGrabCapeWithRightHand  // 攻撃の準備でマントをつかむ
Frame 30: OnReleaseCapeFromRightHand  // 攻撃後にマントを離す
```

#### 例2: ダッシュ時にマントを保持

```csharp
public class PlayerDashController : MonoBehaviour
{
    private ClothGrabController grabController;

    void Start()
    {
        grabController = GetComponent<ClothGrabController>();
    }

    void StartDash()
    {
        // ダッシュ開始時にマントをつかんで固定
        grabController.GrabAtPoint(0);
        grabController.GrabAtPoint(1);
    }

    void EndDash()
    {
        // ダッシュ終了時に解放
        grabController.ReleaseAll();
    }
}
```

#### 例3: 特定のアクションで特定の頂点をつかむ

```csharp
public class SpecialMoveController : MonoBehaviour
{
    private ClothVertexGrabber clothGrabber;

    void ExecuteSpecialMove()
    {
        // 右肩の頂点グループをつかむ（grabpoint1）
        clothGrabber.StartGrabbingAtPoint("grabpoint1");

        // 1秒後に離す
        Invoke("ReleaseGrab", 1f);
    }

    void ReleaseGrab()
    {
        clothGrabber.StopGrabbingAtPoint("grabpoint1");
    }
}
```

## グラブポイントの設定

### Inspector設定

1. **ClothVertexGrabber**
   - `Grab Points`配列でグラブポイントを設定
   - 各グラブポイントに以下を設定：
     - Name: グラブポイントの名前（例："RightHand", "LeftHand"）
     - Transform: グラブポイントのTransform
     - Key Code: キーボード入力（オプション、外部制御する場合は不要）
     - Allowed Vertex Indices: このグラブポイントがつかめる頂点のインデックスリスト

2. **ClothGrabController**（オプション）
   - `Cloth Grabber`: ClothVertexGrabberへの参照
   - `Right Hand Grab Point Index`: 右手用のグラブポイントインデックス（デフォルト: 0）
   - `Left Hand Grab Point Index`: 左手用のグラブポイントインデックス（デフォルト: 1）
   - `Custom Grab Point Name`: カスタム名前指定用（オプション）

## 頂点の割り当て

### エディタツールで視覚的に割り当て

1. **Scene ViewでVertex Selection Modeを有効化**
   - `ClothVertexGrabber`をInspectorで選択
   - "Enable Vertex Selection Mode"をチェック
   - Scene Viewで頂点が可視化される

2. **頂点をクリックして割り当て**
   - "Selected Grab Point"でグラブポイントを選択
   - Scene Viewで頂点をクリックして割り当て
   - Shift+クリック: 追加
   - Ctrl+クリック: 削除

3. **永続化**
   - Play中に割り当てた頂点は自動保存される
   - または"Save Assignments"ボタンで手動保存

## トラブルシューティング

### グラブが動作しない場合

1. **MagicaClothの初期化を確認**
   - `magicaCloth.IsValid()`がtrueであることを確認
   - コンソールログで初期化メッセージを確認

2. **グラブポイントのインデックスを確認**
   - グラブポイント配列のインデックスは0から始まる
   - 配列の範囲外を指定していないか確認

3. **頂点が割り当てられているか確認**
   - `Allowed Vertex Indices`が空の場合、全頂点がグラブ可能
   - リストに頂点インデックスが入っている場合、その頂点のみグラブ可能

4. **コンソールログを確認**
   - 各メソッドは警告/エラーメッセージを出力
   - `[ClothVertexGrabber]`のプレフィックスで検索

### 警告メッセージ

- `Invalid grab point index: X` - インデックスが範囲外
- `Grab point 'X' not found` - 名前が一致するグラブポイントが見つからない
- `X is already grabbing` - すでにグラブ中
- `X is not currently grabbing` - グラブしていない状態で停止を試みた

## ベストプラクティス

1. **グラブポイントの命名規則**
   - 分かりやすい名前を使用（例："RightHand", "LeftShoulder", "BackCenter"）
   - 名前による制御を使用する場合、typoに注意

2. **パフォーマンス**
   - グラブする頂点数を最小限に（`maxGrabbedVertices`を調整）
   - 不要なグラブポイントは無効化

3. **アニメーション統合**
   - グラブ開始と停止のタイミングを適切に設定
   - アニメーションの自然な流れに合わせる

4. **デバッグ**
   - Gizmosでグラブ状態を視覚的に確認
   - コンソールログで動作を追跡

## APIリファレンス

### ClothVertexGrabber

| メソッド | 説明 | パラメータ | 戻り値 |
|---------|------|-----------|--------|
| `StartGrabbingAtPoint(int)` | インデックスでグラブ開始 | grabPointIndex (0-based) | void |
| `StopGrabbingAtPoint(int)` | インデックスでグラブ停止 | grabPointIndex (0-based) | void |
| `StartGrabbingAtPoint(string)` | 名前でグラブ開始 | grabPointName | void |
| `StopGrabbingAtPoint(string)` | 名前でグラブ停止 | grabPointName | void |
| `ToggleGrabbingAtPoint(int)` | インデックスでグラブトグル | grabPointIndex (0-based) | void |
| `ToggleGrabbingAtPoint(string)` | 名前でグラブトグル | grabPointName | void |
| `IsGrabbingAtPoint(int)` | グラブ中か確認（インデックス） | grabPointIndex (0-based) | bool |
| `IsGrabbingAtPoint(string)` | グラブ中か確認（名前） | grabPointName | bool |
| `GetGrabPoints()` | グラブポイント配列を取得 | なし | GrabPointInfo[] |

### ClothGrabController

| メソッド | 説明 | パラメータ | 戻り値 |
|---------|------|-----------|--------|
| `GrabCapeWithRightHand()` | 右手でケープをつかむ | なし | void |
| `ReleaseCapeFromRightHand()` | 右手からケープを離す | なし | void |
| `GrabCapeWithLeftHand()` | 左手でケープをつかむ | なし | void |
| `ReleaseCapeFromLeftHand()` | 左手からケープを離す | なし | void |
| `GrabAtPoint(int)` | インデックスでグラブ | grabPointIndex | void |
| `ReleaseAtPoint(int)` | インデックスで解放 | grabPointIndex | void |
| `GrabAtPoint(string)` | 名前でグラブ | grabPointName | void |
| `ReleaseAtPoint(string)` | 名前で解放 | grabPointName | void |
| `IsGrabbing(int)` | グラブ中か確認 | grabPointIndex | bool |
| `ReleaseAll()` | 全グラブポイント解放 | なし | void |

## 更新履歴

- v0.4.4 (2025-11-25): 外部制御用公開API追加、ClothGrabControllerサンプルスクリプト作成
