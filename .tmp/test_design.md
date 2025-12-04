# テスト設計書: ストーリー・メッセージウィンドウシステム

## 1. テスト方針

### 1.1 テストレベル

| レベル | 対象 | 方法 |
|--------|------|------|
| Unit Test | 個別クラス・メソッド | Unity Test Framework (EditMode) |
| Integration Test | コンポーネント間連携 | Unity Test Framework (PlayMode) |
| Manual Test | UI・視覚的動作確認 | テストシーン |

### 1.2 テスト優先度

| 優先度 | 説明 |
|--------|------|
| P0 | 必須 - リリースブロッカー |
| P1 | 高 - 主要機能 |
| P2 | 中 - 補助機能 |
| P3 | 低 - エッジケース |

---

## 2. ユニットテスト

### 2.1 StoryData テスト

| ID | テストケース | 優先度 | 期待結果 |
|----|-------------|--------|----------|
| UT-SD-001 | 空のStoryDataを作成できる | P0 | storyId=""、commands=空リスト |
| UT-SD-002 | コマンドを追加できる | P0 | commands.Count増加 |
| UT-SD-003 | コマンドを削除できる | P0 | commands.Count減少 |
| UT-SD-004 | コマンドの順序を変更できる | P1 | 指定位置に移動 |
| UT-SD-005 | 有効なJSONをインポートできる | P0 | 正しくパース |
| UT-SD-006 | 無効なJSONでエラーハンドリング | P1 | 例外またはエラーログ |
| UT-SD-007 | JSONにエクスポートできる | P0 | 有効なJSON文字列 |
| UT-SD-008 | インポート→エクスポートで同一性保持 | P1 | 同じ内容 |

```csharp
[Test]
public void ImportFromJson_ValidJson_ParsesCorrectly()
{
    var story = ScriptableObject.CreateInstance<StoryData>();
    var json = @"{""id"":""test"",""script"":[{""op"":""say"",""name"":""Test"",""lines"":[""Hello""]}]}";

    story.ImportFromJson(json);

    Assert.AreEqual("test", story.StoryId);
    Assert.AreEqual(1, story.Commands.Count);
    Assert.AreEqual("say", story.Commands[0].op);
}
```

### 2.2 StoryCommandData テスト

| ID | テストケース | 優先度 | 期待結果 |
|----|-------------|--------|----------|
| UT-CMD-001 | デフォルト値が正しい | P0 | loop=true, volume=1.0等 |
| UT-CMD-002 | PortraitPositionのデフォルトはCenter | P1 | Center |
| UT-CMD-003 | 全opタイプをシリアライズできる | P0 | 各タイプで成功 |

### 2.3 TypewriterEffect テスト

| ID | テストケース | 優先度 | 期待結果 |
|----|-------------|--------|----------|
| UT-TW-001 | 空文字列で即完了 | P1 | IsTyping=false即座 |
| UT-TW-002 | Skip()で全文即表示 | P0 | fullText表示、IsTyping=false |
| UT-TW-003 | charactersPerSecond=0でエラーなし | P2 | 例外なし |
| UT-TW-004 | OnTypingCompletedイベント発火 | P0 | イベント発火 |

### 2.4 StoryResourceLoader テスト

| ID | テストケース | 優先度 | 期待結果 |
|----|-------------|--------|----------|
| UT-RL-001 | 存在するリソースをロードできる | P0 | null以外 |
| UT-RL-002 | 存在しないリソースでnull | P1 | null返却 |
| UT-RL-003 | PreloadStoryで全リソース事前読込 | P1 | context辞書に格納 |

---

## 3. 統合テスト (PlayMode)

### 3.1 StoryPlayer テスト

| ID | テストケース | 優先度 | 期待結果 |
|----|-------------|--------|----------|
| IT-SP-001 | Play()でストーリー開始 | P0 | IsPlaying=true、OnStoryStarted発火 |
| IT-SP-002 | 全コマンド順次実行 | P0 | 各コマンドExecute呼出 |
| IT-SP-003 | Stop()で即停止 | P0 | IsPlaying=false |
| IT-SP-004 | Pause()/Resume()で一時停止/再開 | P1 | IsPaused切替 |
| IT-SP-005 | 終了時OnStoryEnded発火 | P0 | イベント発火 |
| IT-SP-006 | 未知のopをスキップ | P2 | エラーなく続行 |

```csharp
[UnityTest]
public IEnumerator Play_SimpleStory_CompletesSuccessfully()
{
    var story = CreateTestStory();
    var player = CreateStoryPlayer();
    bool ended = false;
    player.OnStoryEnded += _ => ended = true;

    player.Play(story);

    yield return new WaitUntil(() => ended || Time.time > 10f);
    Assert.IsTrue(ended);
    Assert.IsFalse(player.IsPlaying);
}
```

### 3.2 MessageWindow テスト

| ID | テストケース | 優先度 | 期待結果 |
|----|-------------|--------|----------|
| IT-MW-001 | Show()で表示 | P0 | alpha=1 |
| IT-MW-002 | Hide()で非表示 | P0 | alpha=0 |
| IT-MW-003 | ShowDialogue()でタイプライター開始 | P0 | テキスト表示開始 |
| IT-MW-004 | OnAdvanceInput()でスキップ | P0 | 即時全文表示 |
| IT-MW-005 | 複数行セリフを順次表示 | P0 | 各行表示後入力待ち |
| IT-MW-006 | 立ち絵が正しい位置に表示 | P1 | left/center/right |
| IT-MW-007 | OnDialogueCompleted発火 | P0 | 全行完了後発火 |

### 3.3 コマンド統合テスト

| ID | テストケース | 優先度 | 期待結果 |
|----|-------------|--------|----------|
| IT-CMD-001 | SayCommand: セリフ表示 | P0 | MessageWindow表示 |
| IT-CMD-002 | BgCommand: 背景変更 | P1 | Image.sprite変更 |
| IT-CMD-003 | BgCommand: フェード効果 | P1 | 徐々に切替 |
| IT-CMD-004 | BgmPlayCommand: BGM再生 | P1 | AudioSource.isPlaying=true |
| IT-CMD-005 | BgmPlayCommand: フェードイン | P2 | volume徐々に上昇 |
| IT-CMD-006 | BgmStopCommand: BGM停止 | P1 | AudioSource.isPlaying=false |
| IT-CMD-007 | SeCommand: SE再生 | P1 | PlayOneShot呼出 |
| IT-CMD-008 | WaitCommand: 指定時間待機 | P1 | 待機後次へ |
| IT-CMD-009 | EndCommand: ウィンドウ非表示 | P0 | MessageWindow.Hide()呼出 |

---

## 4. 手動テスト

### 4.1 テストシーン構成

```
TestScene_Story
├── StoryPlayer
├── MessageWindow
├── BackgroundImage
├── TestUI
│   ├── PlayButton (story_test_01)
│   ├── PlayButton (story_test_02)
│   ├── StopButton
│   └── StatusText
└── TestStoryData/
    ├── story_test_01.asset (基本テスト)
    ├── story_test_02.asset (全機能テスト)
    └── story_test_03.asset (エッジケース)
```

### 4.2 テストストーリーデータ

#### story_test_01 (基本テスト)
```json
{
  "id": "test_01",
  "script": [
    {"op": "say", "name": "テスト", "lines": ["これはテストです。", "2行目です。"]},
    {"op": "end"}
  ]
}
```

#### story_test_02 (全機能テスト)
```json
{
  "id": "test_02",
  "script": [
    {"op": "bgm.play", "name": "test_bgm", "loop": true, "volume": 0.5, "fade": 500},
    {"op": "bg", "name": "test_bg", "fade": 300},
    {"op": "say", "name": "キャラA", "portrait": "chara_a", "portraitPosition": "left", "lines": ["左に立ち絵が出ます。"]},
    {"op": "say", "name": "キャラB", "portrait": "chara_b", "portraitPosition": "right", "lines": ["右に立ち絵が出ます。"]},
    {"op": "wait", "duration": 1000},
    {"op": "se.play", "name": "test_se"},
    {"op": "bgm.stop", "fade": 500},
    {"op": "end", "returnTo": "game"}
  ]
}
```

### 4.3 手動テストチェックリスト

#### MT-001: 基本表示確認
- [ ] メッセージウィンドウが画面下部に表示される
- [ ] キャラクター名が正しく表示される
- [ ] セリフがタイプライター効果で表示される
- [ ] 進行マーク(▼)が表示される

#### MT-002: 入力操作確認
- [ ] クリックでタイプライター中→即時全文表示
- [ ] クリックで全文表示後→次のセリフへ
- [ ] Spaceキーで同様の動作
- [ ] Enterキーで同様の動作

#### MT-003: 立ち絵確認
- [ ] 左位置に立ち絵表示
- [ ] 中央位置に立ち絵表示
- [ ] 右位置に立ち絵表示
- [ ] フェードイン効果が動作
- [ ] 立ち絵なしでセリフのみ表示可能

#### MT-004: 背景確認
- [ ] 背景画像が表示される
- [ ] フェード切替が動作する

#### MT-005: 音声確認
- [ ] BGMが再生される
- [ ] BGMがループする
- [ ] BGMがフェードイン/アウトする
- [ ] SEが再生される

#### MT-006: パフォーマンス確認
- [ ] 60FPS維持（タイプライター中）
- [ ] 60FPS維持（フェード中）
- [ ] 長いセリフ(500文字)でも問題なし

#### MT-007: エディタ確認
- [ ] Story Editorウィンドウが開く
- [ ] コマンドの追加ができる
- [ ] コマンドの削除ができる
- [ ] コマンドの並び替えができる
- [ ] JSONインポートが動作する
- [ ] JSONエクスポートが動作する

---

## 5. エッジケース・異常系テスト

| ID | テストケース | 優先度 | 期待結果 |
|----|-------------|--------|----------|
| EC-001 | 空のscript配列 | P1 | 即座にOnStoryEnded |
| EC-002 | 存在しない立ち絵名 | P1 | エラーなく続行、ログ出力 |
| EC-003 | 存在しない背景名 | P1 | エラーなく続行、ログ出力 |
| EC-004 | 存在しないBGM名 | P1 | エラーなく続行、ログ出力 |
| EC-005 | 空のlines配列 | P2 | スキップして次へ |
| EC-006 | null文字列のセリフ | P2 | 空文字として扱う |
| EC-007 | 負のfade値 | P2 | 0として扱う |
| EC-008 | 再生中にPlay()再呼出 | P1 | 前のストーリー停止、新規開始 |
| EC-009 | 再生中にシーン遷移 | P1 | エラーなく停止 |
| EC-010 | 非常に長いセリフ(10000文字) | P2 | 正常に表示（スクロール不可） |

---

## 6. 回帰テスト

### 6.1 コア機能回帰

各リリース前に実施:

1. story_test_01 を最初から最後まで再生
2. story_test_02 を最初から最後まで再生
3. ストーリー中にStop()で停止
4. JSONインポート→エクスポート→再インポートで同一性確認

### 6.2 自動回帰テストスイート

```csharp
[TestFixture]
public class StorySystemRegressionTests
{
    [UnityTest]
    public IEnumerator RegressionTest_BasicStoryPlayback() { ... }

    [UnityTest]
    public IEnumerator RegressionTest_AllCommandTypes() { ... }

    [UnityTest]
    public IEnumerator RegressionTest_StopDuringPlayback() { ... }

    [Test]
    public void RegressionTest_JsonRoundTrip() { ... }
}
```

---

## 7. テストデータ準備

### 7.1 必要なテストリソース

| リソース | パス | 説明 |
|---------|------|------|
| test_portrait_a | Resources/Story/Portraits/test_portrait_a | テスト用立ち絵A |
| test_portrait_b | Resources/Story/Portraits/test_portrait_b | テスト用立ち絵B |
| test_bg | Resources/Story/Backgrounds/test_bg | テスト用背景 |
| test_bgm | Resources/Story/BGM/test_bgm | テスト用BGM |
| test_se | Resources/Story/SE/test_se | テスト用SE |

### 7.2 テストヘルパー

```csharp
public static class StoryTestHelper
{
    public static StoryData CreateSimpleStory(string id, params string[] lines)
    {
        var story = ScriptableObject.CreateInstance<StoryData>();
        // ... setup
        return story;
    }

    public static StoryPlayer CreateStoryPlayer()
    {
        var go = new GameObject("TestStoryPlayer");
        return go.AddComponent<StoryPlayer>();
    }
}
```

---

## 8. テスト実行計画

### 8.1 開発中

- ユニットテスト: コード変更ごとに実行
- 統合テスト: 機能完成ごとに実行
- 手動テスト: 主要機能完成時

### 8.2 リリース前

1. 全ユニットテスト実行 (EditMode)
2. 全統合テスト実行 (PlayMode)
3. 手動テストチェックリスト完了
4. 回帰テストスイート実行

### 8.3 合格基準

- ユニットテスト: 100% パス
- 統合テスト: 100% パス
- 手動テスト: P0/P1項目 100% パス
- パフォーマンス: 60FPS維持
