# Unity初心者向けチュートリアル：Vampire Survivors風ゲームを作ろう

このチュートリアルでは、Unityを使ってシンプルな2D/3Dアクションゲームのプロトタイプを作成します。プレイヤーがマウスに向かって移動し、自動で弾を発射して迫りくる敵を倒すゲームです。

## 前提条件
- Unity HubとUnityエディタ（2021.3以降推奨）がインストールされていること
- 基本的なC#の知識があるとなお良い（なくてもコードをコピー＆ペーストで進められます）

## ステップ 1: プロジェクトのセットアップ
1.  **Unity Hubを開く**: 「新しいプロジェクト」をクリック。
2.  **テンプレート選択**: 「3D (Core)」を選択（今回は3Dオブジェクトを使いますが、カメラを上から見下ろす形にします）。
3.  **プロジェクト名**: `RPG_defete` など好きな名前を入力して作成。
4.  **フォルダ作成**: `Assets` フォルダ内に `Scripts`, `Prefabs`, `Materials` フォルダを作成して整理しやすくします。

## ステップ 2: プレイヤーを作ろう（移動と回転）
プレイヤーキャラクターを作成し、マウスカーソルの方向に移動・回転させます。

1.  **プレイヤー作成**: Hierarchyで右クリック > 3D Object > Capsule。「Player」にリネーム。
2.  **地面作成**: Hierarchyで右クリック > 3D Object > Plane。「Ground」にリネーム。Scaleを (5, 1, 5) に設定。
3.  **スクリプト作成**: `Scripts` フォルダに `PlayerController.cs` を作成。
4.  **コード記述**:
    - `Rigidbody` コンポーネントを取得。
    - `Camera.main.ScreenPointToRay` でマウス位置へのレイを飛ばす。
    - `Physics.Raycast` で地面との交点を取得し、その方向へ `transform.LookAt` と `Rigidbody.MovePosition` で移動。
5.  **設定**: Playerに `Rigidbody` を追加（Use Gravity: On, Constraints: Freeze Rotation X/Z, Freeze Position Y）。`PlayerController` をアタッチ。

## ステップ 3: 攻撃システム（弾の発射）
自動で弾を発射する仕組みを作ります。

1.  **弾の作成**: Sphereを作成し、小さくする（Scale 0.3）。「Projectile」にリネームし、Prefab化（HierarchyからProjectウィンドウへドラッグ）。シーンからは削除。
2.  **スクリプト作成**: `Projectile.cs` と `PlayerAttack.cs` を作成。
3.  **Projectile.cs**:
    - `Update` で `transform.Translate(Vector3.forward * speed)` で前進。
    - `OnTriggerEnter` で敵（タグ "Enemy"）に当たったら敵を倒して自分も消える処理。
4.  **PlayerAttack.cs**:
    - 一定時間ごとに `Instantiate` で弾を生成。
5.  **設定**: Playerに `PlayerAttack` をアタッチ。Projectile Prefabを割り当て。Projectile Prefabに `Projectile` スクリプトをアタッチし、Is Triggerをオンにする。

## ステップ 4: 敵キャラとAI（追跡）
プレイヤーを追いかける敵を作ります。

1.  **敵作成**: Cubeを作成。「Enemy」にリネーム。赤色などのマテリアルを適用して区別。タグ「Enemy」を追加して設定。
2.  **スクリプト作成**: `EnemyController.cs` を作成。
3.  **コード記述**:
    - ターゲット（プレイヤー）の方向を計算。
    - `transform.position` を更新して移動。
    - `transform.LookAt` でプレイヤーを向く。
4.  **Prefab化**: EnemyをPrefab化してシーンから削除。

## ステップ 5: スポーンシステム（オブジェクトプール）
敵を効率よく無限に湧かせるために「オブジェクトプール」を使います。

1.  **スクリプト作成**: `ObjectPool.cs`（汎用プール）と `EnemySpawner.cs` を作成。
2.  **ObjectPool.cs**: `Queue` を使ってオブジェクトを再利用する仕組みを実装。
3.  **EnemySpawner.cs**:
    - `ObjectPool` を初期化。
    - 一定時間ごとにプレイヤーの周囲（ランダムな位置）に敵を出現させる。
4.  **設定**: 空のGameObject「GameManager」を作成し、`EnemySpawner` をアタッチ。Enemy PrefabとPlayerを割り当て。

## ステップ 6: ゲーム管理（ゲームオーバー）
ゲームの状態（プレイ中、ゲームオーバー）を管理します。

1.  **スクリプト作成**: `GameManager.cs` を作成（シングルトンパターン）。
2.  **コード記述**:
    - `GameState` (Playing, GameOver) を定義。
    - プレイヤーが敵に当たったら `EndGame()` を呼び出し、状態をGameOverにする。
    - 生存時間 (`SurvivalTime`) を計測。
3.  **PlayerController修正**: `OnCollisionEnter` で敵に当たったら `GameManager.Instance.EndGame()` を呼ぶように追加。

## ステップ 7: UI（ゲームオーバー画面）
ゲームオーバー時に生存時間を表示します。

1.  **UI作成**: Canvasを作成。Text (TMP) を配置して「Game Over」と生存時間を表示するようにレイアウト。最初は非表示にしておく。
2.  **スクリプト作成**: `UIManager.cs` を作成。
3.  **コード記述**: `ShowGameOver(float time)` メソッドでUIを表示し、テキストを更新。
4.  **連携**: `GameManager` から `UIManager` を呼び出す。

---
**完成！**
これで、迫りくる敵を倒しながら生き残る基本的なゲームループが完成しました。ここから、HP制にしたり、レベルアップ機能を追加したりして、自分だけのゲームに進化させていきましょう！
