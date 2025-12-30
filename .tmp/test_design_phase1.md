# Phase 1 テスト設計: VRM表情コントローラ

## テスト対象

1. **VRMExpressionController** - VRM表情制御コンポーネント
2. **ExpressionCommand** - ストーリーコマンド
3. **StoryContext** - キャラクター管理機能

---

## テストケース

### TC-001: 表情の即時設定

| 項目 | 内容 |
|------|------|
| 前提条件 | VRMモデルにVRMExpressionControllerがアタッチされている |
| 操作 | `SetExpression("happy", 1.0f)` を呼び出す |
| 期待結果 | 表情が即座にhappyに変わる |
| 確認方法 | `GetExpressionWeight("happy")` が 1.0f を返す |

### TC-002: 表情の遷移設定

| 項目 | 内容 |
|------|------|
| 前提条件 | VRMモデルにVRMExpressionControllerがアタッチされている |
| 操作 | `SetExpressionWithTransition("angry", 1.0f, 0.5f)` を呼び出す |
| 期待結果 | 0.5秒かけて表情がangryに変わる |
| 確認方法 | 遷移中 `IsTransitioning == true`、完了後 `GetExpressionWeight("angry")` が 1.0f |

### TC-003: 表情のリセット

| 項目 | 内容 |
|------|------|
| 前提条件 | 何らかの表情が設定されている |
| 操作 | `ResetExpression()` を呼び出す |
| 期待結果 | 全ての表情が0になり、neutralが1.0fになる |
| 確認方法 | `GetExpressionWeight("neutral")` が 1.0f を返す |

### TC-004: 無効な表情名

| 項目 | 内容 |
|------|------|
| 前提条件 | VRMモデルにVRMExpressionControllerがアタッチされている |
| 操作 | `SetExpression("invalid_expression", 1.0f)` を呼び出す |
| 期待結果 | エラーなく処理される（カスタム表情として扱われる） |
| 確認方法 | Warningログが出力されない |

### TC-005: キャラクター登録と取得

| 項目 | 内容 |
|------|------|
| 前提条件 | StoryPlayerが存在する |
| 操作 | `RegisterCharacter("hero", controller)` を呼び出す |
| 期待結果 | キャラクターが登録される |
| 確認方法 | `TryGetCharacter("hero", out var c)` が true を返す |

### TC-006: ExpressionCommandの実行

| 項目 | 内容 |
|------|------|
| 前提条件 | キャラクター "hero" が登録されている |
| 操作 | `{"op":"expression", "targetCharacter":"hero", "expressionName":"happy"}` を実行 |
| 期待結果 | heroキャラクターの表情がhappyに変わる |
| 確認方法 | 表情が視覚的に変化する |

### TC-007: 未登録キャラクターへのExpressionCommand

| 項目 | 内容 |
|------|------|
| 前提条件 | キャラクター "unknown" は登録されていない |
| 操作 | `{"op":"expression", "targetCharacter":"unknown", "expressionName":"happy"}` を実行 |
| 期待結果 | Warningログが出力され、処理が継続する |
| 確認方法 | コンソールにWarningが表示される |

### TC-008: 複数表情の同時設定

| 項目 | 内容 |
|------|------|
| 前提条件 | VRMモデルにVRMExpressionControllerがアタッチされている |
| 操作 | `SetExpression("happy", 0.5f)` → `SetExpression("surprised", 0.5f)` |
| 期待結果 | 両方の表情がブレンドされる |
| 確認方法 | 両方の表情Weightが設定値を返す |

---

## 手動テスト手順

### 準備
1. VRMモデルをシーンに配置
2. VRMモデルに `VRMExpressionController` コンポーネントを追加
3. `characterName` を "hero" に設定
4. `ExpressionTester` コンポーネントを追加

### テスト実行
1. Playモードに入る
2. Inspectorまたはキーボードで各テストを実行
3. 表情の変化を視覚的に確認
4. コンソールログを確認

---

## 自動テスト（Edit Mode）

Unity Test Frameworkを使用した自動テストも作成可能：
- `StoryContext` のキャラクター登録/取得テスト
- `StoryCommandData` のCloneテスト

---

## 期待されるログ出力

### 正常系
```
[VRMExpressionController] Initialized for hero
[VRMExpressionController] hero: Set happy = 1
[VRMExpressionController] hero: Transitioning angry from 0 to 1 over 0.5s
[VRMExpressionController] hero: Transition complete for angry
```

### 異常系
```
[ExpressionCommand] Character not found: unknown
[VRMExpressionController] Expression not available for hero
```
