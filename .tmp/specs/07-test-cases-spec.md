# 仕様書: テストケース

## 概要

アクションゲームの全機能を検証するための包括的なテストケース集。
各フェーズの機能を段階的にテストし、最終的に統合テストを実施する。

---

## 1. Phase 1: 基盤システムテスト

### 1.1 マウス操作テスト

| ID | テスト名 | 手順 | 期待結果 |
|----|---------|------|---------|
| I-01 | 地面右クリック移動 | 1. 地面を右クリック | キャラが指定位置へNavMesh移動 |
| I-02 | 移動中の方向変更 | 1. 地面を右クリック<br>2. 移動中に別位置を右クリック | 新しい目的地に変更 |
| I-03 | 移動不可領域クリック | 1. NavMesh外の地面を右クリック | 最寄りの移動可能点へ移動 |
| I-04 | ホバー検出（Enemy） | 1. マウスをEnemyに重ねる | 赤ハイライト + HP表示 |
| I-05 | ホバー検出（Object） | 1. マウスをInteractableに重ねる | アイコン表示 |
| I-06 | ホバー検出（NPC） | 1. マウスをNPCに重ねる | 名前表示 |

### 1.2 カメラテスト

| ID | テスト名 | 手順 | 期待結果 |
|----|---------|------|---------|
| C-01 | 水平回転 | 1. 右ボタンを押しながら左右ドラッグ | カメラがターゲットを中心に水平回転 |
| C-02 | 垂直回転 | 1. 右ボタンを押しながら上下ドラッグ | カメラが垂直方向に回転（制限内） |
| C-03 | ズームイン | 1. マウスホイールを前に回す | カメラがターゲットに近づく |
| C-04 | ズームアウト | 1. マウスホイールを後ろに回す | カメラがターゲットから離れる |
| C-05 | 障害物回避 | 1. キャラを壁際に移動させる | カメラが壁を貫通せず手前に配置 |
| C-06 | フォーカス切替 | 1. FocusOn()を呼び出す | 指定オブジェクトにスムーズ遷移 |

### 1.3 ターゲッティングテスト

| ID | テスト名 | 手順 | 期待結果 |
|----|---------|------|---------|
| T-01 | Enemy近距離判定 | 1. Enemyに近づく（2m以内）<br>2. 左クリック | Attack判定 |
| T-02 | Enemy遠距離判定 | 1. Enemyから離れる（2m以上）<br>2. 左クリック | Magic判定 |
| T-03 | Object属性判定 | 1. grabbableオブジェクトを左クリック | Grab判定 |
| T-04 | NPC判定 | 1. NPCを左クリック | Talk判定 |
| T-05 | 空白地面判定 | 1. 何もない地面を左クリック | Magic（その位置へ）判定 |

---

## 2. Phase 2: インタラクションテスト

### 2.1 インタラクションポイントテスト

| ID | テスト名 | 手順 | 期待結果 |
|----|---------|------|---------|
| IP-01 | キャラポイント配置 | 1. キャラPrefabをシーンに配置 | 全インタラクションポイントが正しい位置に配置 |
| IP-02 | eye_point位置 | 1. キャラのeye_pointを確認 | Headの目の前に配置 |
| IP-03 | mouth_point位置 | 1. キャラのmouth_pointを確認 | Headの口元に配置 |
| IP-04 | オブジェクトgrab_point | 1. Interactableのgrab_pointを確認 | 取っ手/側面に配置 |
| IP-05 | 椅子sit_point | 1. 椅子のsit_pointを確認 | 座面中央に配置 |

### 2.2 Interactableテスト

| ID | テスト名 | 手順 | 期待結果 |
|----|---------|------|---------|
| IA-01 | サイズ自動判定 | 1. autoDetectSize=trueのオブジェクト配置<br>2. Playモード開始 | Boundsからサイズカテゴリ自動設定 |
| IA-02 | Tiny判定 | 1. 0.1m以下のオブジェクト | SizeCategory.Tiny |
| IA-03 | Small判定 | 1. 0.1-0.3mのオブジェクト | SizeCategory.Small |
| IA-04 | Medium判定 | 1. 0.3-0.8mのオブジェクト | SizeCategory.Medium |
| IA-05 | Large判定 | 1. 0.8m以上のオブジェクト | SizeCategory.Large |
| IA-06 | 属性判定 | 1. grabbable属性オブジェクトでHasAttribute確認 | true返却 |

---

## 3. Phase 3: 行動システムテスト

### 3.1 攻撃行動テスト

| ID | テスト名 | 手順 | 期待結果 |
|----|---------|------|---------|
| A-01 | 近接Attack実行 | 1. Enemyに近接（2m以内）<br>2. 左クリック | 攻撃アニメ + 表情(angry) + ダメージ |
| A-02 | Magic詠唱 | 1. 遠距離でEnemyを左クリック | 詠唱アニメ + 手元エフェクト |
| A-03 | Magic発射 | 1. 詠唱完了後 | メインエフェクト発射 |
| A-04 | Magic着弾 | 1. 魔法がターゲットに到達 | ダメージ + 着弾エフェクト |
| A-05 | 地面Magic | 1. 何もない地面を左クリック | その位置に向けてMagic発射 |

### 3.2 日常行動テスト

| ID | テスト名 | 手順 | 期待結果 |
|----|---------|------|---------|
| D-01 | Tiny Grab | 1. Tinyオブジェクトをクリック | 指で摘むIK + 右手で保持 |
| D-02 | Small Grab | 1. Smallオブジェクトをクリック | 片手で掴むIK |
| D-03 | Medium Grab | 1. Mediumオブジェクトをクリック | 両手で掴むIK |
| D-04 | Large Grab | 1. Largeオブジェクトをクリック | 両手 + 体全体で押す/引く |
| D-05 | Touch実行 | 1. touchableオブジェクトをクリック | 手をtouch_pointへIK移動 |
| D-06 | Sit実行 | 1. 椅子（sittable）をクリック | sit_pointへ移動 + 座るアニメ |
| D-07 | Stomp実行 | 1. stompableオブジェクトをクリック | 足をstomp_pointへIK移動 |

### 3.3 Eat行動テスト

| ID | テスト名 | 手順 | 期待結果 |
|----|---------|------|---------|
| E-01 | Small食べ物をEat | 1. Small食べ物をgrab<br>2. Eat実行 | 手を口へ運ぶ + 口開け + 幸せ表情 |
| E-02 | Large食べ物をEat | 1. Large食べ物に近づく<br>2. Eat実行 | 顔を近づける + かぶりつき |
| E-03 | 口パク（aa） | 1. Eat中 | 口が開く（aa表情） |
| E-04 | 幸せ表情 | 1. Eat完了後 | happy表情に遷移 |

### 3.4 感情行動テスト

| ID | テスト名 | 手順 | 期待結果 |
|----|---------|------|---------|
| EM-01 | Talk開始 | 1. NPCをクリック | 相手のeye_pointにLookAt |
| EM-02 | Talk口パク | 1. Talk中にセリフ表示 | 母音に応じた口パク |
| EM-03 | Kiss実行 | 1. Kissコマンド実行 | 相手に近づく + 肩に手 + 目を細める |
| EM-04 | Hug実行 | 1. Hugコマンド実行 | 両腕を肩/腰へIK + 感情に応じた表情 |

---

## 4. Phase 4: キャラクターテスト

### 4.1 HP/表情連動テスト

| ID | テスト名 | 手順 | 期待結果 |
|----|---------|------|---------|
| HP-01 | HP100-70% | 1. HPを100-70%に設定 | 通常表情 |
| HP-02 | HP70-40% | 1. HPを70-40%に設定 | やや辛そう（sad:0.2） |
| HP-03 | HP40-20% | 1. HPを40-20%に設定 | 苦しそう（sad:0.4 + angry:0.2） |
| HP-04 | HP20-0% | 1. HPを20-0%に設定 | 瀕死（sad:0.6） |
| HP-05 | ダメージ表情 | 1. ダメージを受ける | 一瞬shocked + 元に戻る |

### 4.2 感情システムテスト

| ID | テスト名 | 手順 | 期待結果 |
|----|---------|------|---------|
| EM-01 | Joy設定 | 1. SetEmotion(Joy)呼び出し | happy表情に遷移 |
| EM-02 | Anger設定 | 1. SetEmotion(Anger)呼び出し | angry表情に遷移 |
| EM-03 | 感情減衰 | 1. Joy設定<br>2. 時間経過 | 徐々にNeutralへ |
| EM-04 | 複合感情 | 1. 複数感情を追加 | 最も強い感情が優先 |

### 4.3 NPCテスト

| ID | テスト名 | 手順 | 期待結果 |
|----|---------|------|---------|
| NPC-01 | Idle状態 | 1. NPCを配置（敵なし） | 周囲を見回す |
| NPC-02 | Patrol状態 | 1. パトロールポイント設定<br>2. StartPatrol() | ポイント間を巡回 |
| NPC-03 | Follow状態 | 1. SetFollowTarget(player) | プレイヤーに追従 |
| NPC-04 | Combat遷移 | 1. NPC近くに敵配置 | Combat状態に遷移 |
| NPC-05 | Flee遷移 | 1. Combat中にHP20%以下 | Flee状態に遷移 |

---

## 5. Phase 5: 戦闘システムテスト

### 5.1 魔法システムテスト

| ID | テスト名 | 手順 | 期待結果 |
|----|---------|------|---------|
| M-01 | 詠唱エフェクト | 1. Magic発動 | 手元にChargeEffect |
| M-02 | メインエフェクト | 1. 詠唱完了 | MainEffect発射 |
| M-03 | 投射物移動 | 1. 投射物確認 | 指定速度で移動 |
| M-04 | ホーミング | 1. ホーミング魔法発射 | ターゲットに追尾 |
| M-05 | 範囲ダメージ | 1. 範囲魔法使用 | 範囲内全敵にダメージ |

### 5.2 敵システムテスト

| ID | テスト名 | 手順 | 期待結果 |
|----|---------|------|---------|
| EN-01 | 敵検出 | 1. プレイヤーが範囲内に入る | Chase状態に遷移 |
| EN-02 | 敵攻撃 | 1. プレイヤーが攻撃範囲内 | Attack実行 |
| EN-03 | ヒットリアクション | 1. 敵にダメージ | Stagger + アニメ |
| EN-04 | 部分ラグドール | 1. 敵HPを30%以下に | 部分的にラグドール化 |
| EN-05 | 敵死亡 | 1. 敵HPを0に | 完全ラグドール + エフェクト |
| EN-06 | drag_point有効化 | 1. 敵死亡後 | drag_pointがアクティブに |

### 5.3 引きずりテスト

| ID | テスト名 | 手順 | 期待結果 |
|----|---------|------|---------|
| DR-01 | 死体掴み | 1. 死亡した敵のdrag_pointをクリック | 引きずり開始 |
| DR-02 | 死体移動 | 1. 引きずり中に移動 | 死体が追従 |
| DR-03 | 死体解放 | 1. 引きずり中に再クリック | 引きずり終了 |

---

## 6. 統合テストシナリオ

### シナリオ1: 物を拾って置く

```
1. プレイヤー操作開始
2. 地面を右クリックして棚の近くへ移動
3. 棚の上のコップ（Small）を左クリック
   → 期待: 片手で掴むIK + 視線がコップに
4. テーブルの上を右クリック
   → 期待: テーブルに移動
5. テーブルを左クリック（または専用コマンド）
   → 期待: コップを置く
```

### シナリオ2: 触りながら話す

```
1. ボタン（touchable）を左クリック
   → 期待: 手がボタンに伸びる
2. 話すコマンドを実行
   → 期待: 口パク開始 + セリフ表示
3. 感情（Joy）を設定
   → 期待: 話しながら笑顔に
```

### シナリオ3: 魔法で敵を倒して引きずる

```
1. 敵を発見（遠距離）
2. 敵を左クリック
   → 期待: Magic詠唱 + 発射
3. 敵HPが減少
   → 期待: ダメージ数値表示
4. 複数回攻撃して敵HP=0に
   → 期待: ラグドール化 + 死亡エフェクト
5. 敵のdrag_pointを左クリック
   → 期待: 引きずり開始
6. 指定場所まで移動
   → 期待: 死体が追従
```

### シナリオ4: 椅子に座って食事

```
1. 椅子（sittable）を左クリック
   → 期待: 椅子に移動 + 座るアニメ
2. テーブル上のリンゴ（Small, eatable）を左クリック
   → 期待: 座ったまま手を伸ばして掴む
3. Eatコマンド実行
   → 期待: 口に運ぶ + 口開け + 噛むアニメ
4. 完了
   → 期待: happy表情 + セリフ「美味しい！」
```

### シナリオ5: NPCと会話

```
1. NPCを発見
2. NPCを左クリック
   → 期待: NPCに近づく + Talk開始
3. 会話システム起動
   → 期待: 互いのeye_pointにLookAt
4. セリフ表示
   → 期待: 母音に応じた口パク
5. 感情変化
   → 期待: セリフ内容に応じた表情遷移
```

---

## 7. 自動テスト用コード

```csharp
[TestFixture]
public class ActionGameTests
{
    private GameCharacter _player;
    private EnemyController _enemy;
    private Interactable _testObject;

    [SetUp]
    public void Setup()
    {
        // テスト用オブジェクト生成
        _player = CreateTestPlayer();
        _enemy = CreateTestEnemy();
        _testObject = CreateTestInteractable();
    }

    [Test]
    public void Test_GrabSmallObject_UsesOneHand()
    {
        // Arrange
        _testObject.sizeCategory = SizeCategory.Small;
        var executor = _player.GetComponent<ActionExecutor>();
        var context = new ActionContext
        {
            Actor = _player,
            Target = _testObject.gameObject,
            TargetInteractable = _testObject,
            TargetSize = SizeCategory.Small
        };

        // Act
        bool result = executor.TryExecuteAction("Grab", context);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(_testObject, _player.CurrentHeldObject);
    }

    [Test]
    public void Test_EnemyTakeDamage_HPDecreases()
    {
        // Arrange
        float initialHP = _enemy.HP;
        float damage = 20f;

        // Act
        _enemy.TakeDamage(damage);

        // Assert
        Assert.AreEqual(initialHP - damage, _enemy.HP);
    }

    [Test]
    public void Test_EnemyDeath_EnablesRagdoll()
    {
        // Arrange
        _enemy.TakeDamage(_enemy.HP);

        // Assert
        Assert.IsTrue(_enemy.IsDead);
        Assert.IsTrue(_enemy.IsDraggable);
    }

    [Test]
    public void Test_HPLow_ChangesExpression()
    {
        // Arrange
        var expression = _player.GetComponent<VRMExpressionController>();

        // Act
        _player.TakeDamage(_player.HP * 0.5f); // HP 50%

        // Assert
        // 表情がsad寄りになっていることを確認
        // (実際の検証方法は実装に依存)
    }

    // ヘルパーメソッド
    private GameCharacter CreateTestPlayer() { /* ... */ }
    private EnemyController CreateTestEnemy() { /* ... */ }
    private Interactable CreateTestInteractable() { /* ... */ }
}
```

---

## 8. テスト実施チェックリスト

### Phase 1 完了条件
- [ ] 全マウス操作テスト合格
- [ ] 全カメラテスト合格
- [ ] 全ターゲッティングテスト合格

### Phase 2 完了条件
- [ ] 全インタラクションポイントテスト合格
- [ ] 全Interactableテスト合格

### Phase 3 完了条件
- [ ] 全攻撃行動テスト合格
- [ ] 全日常行動テスト合格
- [ ] 全感情行動テスト合格

### Phase 4 完了条件
- [ ] 全HP/表情連動テスト合格
- [ ] 全感情システムテスト合格
- [ ] 全NPCテスト合格

### Phase 5 完了条件
- [ ] 全魔法システムテスト合格
- [ ] 全敵システムテスト合格
- [ ] 全引きずりテスト合格

### 統合テスト完了条件
- [ ] シナリオ1～5 全て合格
- [ ] パフォーマンス基準達成（60fps維持）
- [ ] メモリリーク無し

