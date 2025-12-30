# Phase 2 テスト設計: アニメーション & 移動コントローラ

## テスト対象

1. **VRMAnimationController** - アニメーション制御コンポーネント
2. **CharacterNavigator** - NavMesh移動コンポーネント
3. **PoseCommand** - アニメーションストーリーコマンド
4. **MoveCommand** - 移動ストーリーコマンド

---

## テストケース

### TC-201: Triggerアニメーション再生

| 項目 | 内容 |
|------|------|
| 前提条件 | VRMAnimationControllerがアタッチされ、Animatorが設定されている |
| 操作 | `PlayAnimation("Jump")` を呼び出す |
| 期待結果 | Jumpトリガーが発火し、アニメーションが再生される |
| 確認方法 | Animatorの状態遷移を視覚的に確認 |

### TC-202: CrossFadeアニメーション再生

| 項目 | 内容 |
|------|------|
| 前提条件 | VRMAnimationControllerがアタッチされ、Animatorが設定されている |
| 操作 | `CrossFade("Walk", 0.3f)` を呼び出す |
| 期待結果 | 0.3秒かけてWalkステートにクロスフェードする |
| 確認方法 | アニメーションが滑らかに遷移することを視覚的に確認 |

### TC-203: アニメーション完了待機

| 項目 | 内容 |
|------|------|
| 前提条件 | アニメーション再生中 |
| 操作 | `WaitForAnimationComplete()` コルーチンを実行 |
| 期待結果 | アニメーションが完了するまで待機する |
| 確認方法 | 待機後にログ出力されることを確認 |

### TC-204: NavMesh座標移動

| 項目 | 内容 |
|------|------|
| 前提条件 | CharacterNavigatorがアタッチされ、NavMeshがベイクされている |
| 操作 | `MoveTo(new Vector3(5, 0, 5))` を呼び出す |
| 期待結果 | キャラクターが目的地へ移動する |
| 確認方法 | `IsMoving == true` → 移動完了後 `IsMoving == false` |

### TC-205: 名前付きポイント移動

| 項目 | 内容 |
|------|------|
| 前提条件 | 移動ポイント "stage_center" が登録されている |
| 操作 | `MoveToPoint("stage_center")` を呼び出す |
| 期待結果 | stage_centerの位置へ移動する |
| 確認方法 | キャラクターがポイント位置に到着する |

### TC-206: 移動停止

| 項目 | 内容 |
|------|------|
| 前提条件 | キャラクターが移動中 |
| 操作 | `Stop()` を呼び出す |
| 期待結果 | 移動が即座に停止する |
| 確認方法 | `IsMoving == false` |

### TC-207: PoseCommandの実行

| 項目 | 内容 |
|------|------|
| 前提条件 | キャラクター "hero" のAnimationControllerが登録されている |
| 操作 | `{"op":"pose", "targetCharacter":"hero", "animationTrigger":"Wave"}` を実行 |
| 期待結果 | heroキャラクターがWaveアニメーションを再生 |
| 確認方法 | アニメーションが再生されることを視覚的に確認 |

### TC-208: MoveCommandの実行

| 項目 | 内容 |
|------|------|
| 前提条件 | キャラクター "hero" のNavigatorが登録されている |
| 操作 | `{"op":"move", "targetCharacter":"hero", "moveTargetPoint":"stage_center"}` を実行 |
| 期待結果 | heroキャラクターがstage_centerへ移動 |
| 確認方法 | キャラクターが目的地に到着することを確認 |

### TC-209: 移動中のアニメーション連携

| 項目 | 内容 |
|------|------|
| 前提条件 | CharacterNavigatorとVRMAnimationControllerが両方設定されている |
| 操作 | 移動を開始する |
| 期待結果 | IsMovingパラメータがtrueになり、歩行アニメーションが再生される |
| 確認方法 | AnimatorのIsMovingパラメータを確認 |

### TC-210: 未登録コントローラへのコマンド

| 項目 | 内容 |
|------|------|
| 前提条件 | キャラクター "unknown" は登録されていない |
| 操作 | `{"op":"pose", "targetCharacter":"unknown", "animationTrigger":"Wave"}` を実行 |
| 期待結果 | Warningログが出力され、処理が継続する |
| 確認方法 | コンソールにWarningが表示される |

---

## 手動テスト手順

### 準備
1. VRMモデルをシーンに配置
2. VRMモデルに以下のコンポーネントを追加:
   - `VRMAnimationController`
   - `CharacterNavigator` (NavMeshAgentが自動追加)
   - `AnimationMoveTester`
3. シーンにNavMesh Surfaceを配置してベイク
4. テスト用の移動ポイント(EmptyObject)を配置

### テスト実行
1. Playモードに入る
2. キーボードで各テストを実行:
   - `1`: Trigger テスト
   - `2`: CrossFade テスト
   - `3`: 移動テスト
   - `S`: 停止
3. アニメーションと移動を視覚的に確認
4. コンソールログを確認

---

## 期待されるログ出力

### 正常系
```
[VRMAnimationController] Initialized for hero
[VRMAnimationController] hero: PlayAnimation trigger=Wave
[VRMAnimationController] hero: CrossFade state=Walk, fadeTime=0.3
[CharacterNavigator] Initialized for hero
[CharacterNavigator] hero: MoveTo position=(5, 0, 5), speed=3.5
[CharacterNavigator] hero: Arrived at destination
```

### 異常系
```
[PoseCommand] Animation controller not found for character: unknown
[MoveCommand] Navigator not found for character: unknown
[MoveCommand] Move point not found: invalid_point
```

---

## 統合テスト用ストーリーJSON

`Assets/Resources/Story/Data/animation_move_test.json` を使用してpose/move/expressionコマンドの組み合わせテストを実行可能。
