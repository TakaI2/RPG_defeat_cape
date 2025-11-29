---
marp: true
theme: default
paginate: true
backgroundColor: #fff
---

# Unityで作る！
## ヴァンパイアサバイバー風ゲーム
### 初心者向けチュートリアル

---

## このチュートリアルのゴール

**自分だけの2D/3Dアクションゲームを作ろう！**

- プレイヤーの移動と回転
- 自動攻撃システム
- 敵の追跡AI
- 無限に湧く敵（オブジェクトプール）
- ゲームオーバーの仕組み

---

## 前提条件

- **Unity Hub** と **Unity エディタ** (2021.3以降推奨)
- やる気！
- (あれば良い) C#の基礎知識
  - なくてもコピペでOK！

---

## Step 1: プロジェクトの準備

1. **Unity Hub** を起動
2. **「新しいプロジェクト」** を作成
3. テンプレート: **「3D (Core)」**
4. プロジェクト名: `RPG_defete` (任意)

> **Point**: フォルダ構成を整理しよう
> `Assets` の中に `Scripts`, `Prefabs`, `Materials` フォルダを作成

---

## Step 2: プレイヤーを作る (1/2)

**目標**: マウスカーソルの方向に移動・回転させる

1. **プレイヤー作成**: 
   - Hierarchy > 3D Object > Capsule
   - 名前を `Player` に変更
2. **地面作成**:
   - Hierarchy > 3D Object > Plane
   - 名前を `Ground` に変更
   - Scale: (5, 1, 5)

---

## Step 2: プレイヤーを作る (2/2)

**スクリプト**: `PlayerController.cs`

- `Rigidbody` で物理挙動を制御
- `Camera.main.ScreenPointToRay` でマウス位置を取得
- マウスの方向を向く (`LookAt`)
- その方向へ移動 (`MovePosition`)

> **設定**: PlayerのRigidbody設定
> Use Gravity: On
> Constraints: Freeze Rotation X/Z, Freeze Position Y

---

## Step 3: 攻撃システム (1/2)

**目標**: 自動で弾を発射する

1. **弾 (Projectile) 作成**:
   - Sphereを作成 (Scale 0.3)
   - 名前を `Projectile` に
   - **Prefab化** してシーンから削除

---

## Step 3: 攻撃システム (2/2)

**スクリプト**:

- **`Projectile.cs`**:
  - 前方に移動
  - 敵に当たったら消滅
- **`PlayerAttack.cs`**:
  - 一定時間ごとに弾を生成 (`Instantiate`)

> **設定**: Playerに `PlayerAttack` をアタッチ

---

## Step 4: 敵キャラとAI

**目標**: プレイヤーを追いかける敵

1. **敵 (Enemy) 作成**:
   - Cubeを作成 (赤色推奨)
   - 名前を `Enemy`
   - Tagに `Enemy` を設定
2. **スクリプト**: `EnemyController.cs`
   - プレイヤーの位置へ移動
   - プレイヤーの方を向く

---

## Step 5: スポーンシステム

**目標**: 敵を効率よく無限に湧かせる

**「オブジェクトプール」** という技術を使います！
- 生成と削除を繰り返すと重くなる
- 使い回すことで軽量化

**スクリプト**:
- `ObjectPool.cs`: 再利用の仕組み
- `EnemySpawner.cs`: 定期的に敵を出現させる

---

## Step 6: ゲーム管理

**目標**: ゲームオーバーを作る

**スクリプト**: `GameManager.cs`
- ゲームの状態管理 (Playing, GameOver)
- 生存時間の計測
- プレイヤーが敵に当たったらゲームオーバー

---

## Step 7: UI (仕上げ)

**目標**: ゲームオーバー画面を表示

1. **Canvas** を作成
2. **Text (TMP)** で「Game Over」と生存時間を表示
3. `UIManager.cs` で表示を制御

---

# 完成！

**おめでとうございます！**
基本的なゲームループが完成しました。

**次のステップ案**:
- 敵の種類を増やす
- レベルアップ機能
- 派手なエフェクト
- 音をつける

自分だけのゲームに進化させましょう！
