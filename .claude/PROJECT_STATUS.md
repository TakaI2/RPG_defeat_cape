# プロジェクトステータス

最終更新: 2025-11-25 (v0.4.2)

---

## プロジェクト概要

| 項目 | 内容 |
|------|------|
| プロジェクト名 | RPG_defete |
| 会社名 | DefaultCompany |
| Unity バージョン | 6000.0.46f1 |
| レンダーパイプライン | Universal Render Pipeline (URP) v17.0.4 |
| プロジェクトタイプ | RPG with Advanced Cloth Physics |
| 現在のバージョン | v0.4.2 |
| 開発段階 | Cloth Physics Prototyping |

---

## 現在の開発フォーカス

### MagicaCloth2 布物理システム

本プロジェクトは、MagicaCloth2を使用した「つかめる布」のテストと改造を中心に進行中です。

#### 主要機能

1. **マルチポイント頂点グラビングシステム** (`ClothVertexGrabber.cs`)
   - 複数のグラブポイントによる同時制御
   - 各グラブポイントに個別の頂点制約設定
   - Fixed頂点の除外による振動防止
   - OnPreSimulation、OnPostSimulation、Camera.onPreRenderによるスムーズなグラビング

2. **コライダーベースのグラビングシステム** (`ClothGrabber.cs`)
   - 2つの球体コライダーで布を挟んでグラブ
   - 動的なコライダー生成・削除

3. **グラブポイント移動制御** (`GrabPointMover.cs`)
   - グラブポイントの位置制御

---

## 📊 開発進捗

### ✅ 完了した実装

#### v0.1.0 (初期実装)
- [x] Unity MRプロジェクトセットアップ
- [x] MagicaCloth2統合
- [x] 基本的な頂点グラビングシステム

#### v0.2.0 (振動修正)
- [x] Camera.onPreRenderを使用した振動修正
- [x] ダイレクトメッシュコントロールの実装

#### v0.3.0 (マテリアル改善)
- [x] 両面光沢布シェーダー追加
- [x] マテリアル設定の最適化

#### v0.4.0 (マルチポイントシステム)
- [x] マルチポイント頂点グラビングシステム実装
- [x] 個別頂点制約機能
- [x] 複数グラブポイントの同時制御
- [x] Fixed属性による振動防止の強化

#### v0.4.1 (座標系修正)
- [x] cape2移動時の座標系問題を修正
- [x] 初期ClothTransform座標系を記録・使用
- [x] `WorldToInitialClothLocal()` / `InitialClothLocalToWorld()` ヘルパーメソッド追加
- [x] RandomGrabPointMoverスクリプト作成（テスト用）

#### v0.4.2 (視覚的頂点選択ツール) - **最新**
- [x] ClothVertexGrabberEditor カスタムエディタ実装
- [x] Scene viewでの頂点可視化（色分け表示）
- [x] マウスクリックによる頂点選択機能
- [x] GrabPoint別の色分け（赤/緑/青/黄/マゼンタ）
- [x] Inspector UIでの選択モード切り替え
- [x] Shift/Ctrl修飾キーによる追加/削除

### 🚧 現在の課題

- パフォーマンス最適化（多数の頂点処理）
- VRコントローラー統合（現在はキーボード入力）
- RPGゲームプレイへの統合

### ✅ 解決済みの課題

- ~~cape2移動後に頂点がgrabpointからずれる問題~~ → v0.4.1で解決

### ⏳ 今後の予定

- [ ] VRコントローラー対応
- [ ] RPGキャラクターシステムとの統合
- [ ] 布グラビングを活用したゲームプレイ要素
- [ ] マルチプレイヤー対応の検討

---

## プロジェクト構造

```
RPG_defete/
├── Assets/
│   ├── Scenes/
│   │   ├── SampleScene.unity          # メインシーン
│   │   ├── game1.unity                # ゲームシーン1
│   │   ├── Big_Scene.unity            # 大規模シーン
│   │   └── cloth_test.unity           # 布物理テストシーン
│   ├── Scripts/
│   │   ├── Cloth Physics/
│   │   │   ├── ClothVertexGrabber.cs  # マルチポイント頂点グラビング
│   │   │   ├── ClothGrabber.cs        # コライダーベースグラビング
│   │   │   ├── GrabPointMover.cs      # グラブポイント制御
│   │   │   └── RandomGrabPointMover.cs # グラブポイントランダム移動（テスト用）
│   │   ├── RPG Core/
│   │   │   ├── PlayerController.cs    # プレイヤー制御
│   │   │   ├── PlayerAttack.cs        # プレイヤー攻撃
│   │   │   ├── EnemyController.cs     # 敵AI
│   │   │   ├── EnemySpawner.cs        # 敵スポーン
│   │   │   ├── Projectile.cs          # 発射物
│   │   │   ├── GameManager.cs         # ゲーム管理
│   │   │   ├── UIManager.cs           # UI管理
│   │   │   └── ObjectPool.cs          # オブジェクトプール
│   │   └── Testing/
│   │       ├── TestScript1.cs
│   │       └── TestScript2.cs
│   ├── Editor/
│   │   └── ClothVertexGrabberEditor.cs # 頂点選択ツール（CustomEditor）
│   ├── Materials/                     # マテリアル（両面光沢布など）
│   ├── Prefabs/                       # プレハブ
│   └── Settings/                      # URP設定
├── Packages/                          # Unityパッケージ
│   └── manifest.json
├── .claude/                           # Claude Code設定
│   ├── CLAUDE.md
│   ├── PROJECT_STATUS.md              # このファイル
│   └── tasks.json                     # タスク管理
└── .vscode/                           # VS Code設定
```

---

## 技術スタック

### レンダーパイプライン
- **Universal Render Pipeline (URP) v17.0.4**

### 布物理
- **MagicaCloth2** - 高度な布物理シミュレーション
  - VertexAttribute制御
  - MagicaManagerイベントシステム
  - ダイレクトメッシュコントロール

### インストール済みパッケージ

#### コア機能
- `com.unity.render-pipelines.universal` v17.0.4 - URP
- `com.unity.inputsystem` v1.14.2 - 新入力システム
- `com.unity.timeline` v1.8.9 - タイムライン
- `com.unity.visualscripting` v1.9.7 - ビジュアルスクリプティング
- `com.unity.ugui` v2.0.0 - UI システム

#### AI/ナビゲーション
- `com.unity.ai.navigation` v2.0.9 - AI ナビゲーション

#### 開発ツール
- `com.unity.test-framework` v1.6.0 - テストフレームワーク
- `com.unity.package-validation-suite` v0.22.0-preview - パッケージ検証

#### IDE統合
- `com.unity.ide.rider` v3.0.38 - JetBrains Rider
- `com.unity.ide.visualstudio` v2.0.25 - Visual Studio

#### カスタムパッケージ
- `com.coplaydev.unity-mcp` - Unity MCP (Model Context Protocol)
  - GitHub: https://github.com/CoplayDev/unity-mcp.git

---

## 主要スクリプト概要

### ClothVertexGrabber.cs (v0.4.0)

**マルチポイント頂点グラビングシステム**

#### 主要機能
- 複数のグラブポイントを同時に制御
- 各グラブポイントに個別の頂点インデックス制約
- Fixed属性設定による振動防止
- 3つのイベントフックによるスムーズな制御：
  - `OnPreSimulation`: グラブした頂点の位置更新
  - `OnPostSimulation`: 表示位置の強制更新
  - `Camera.onPreRender`: レンダリング直前のメッシュ制御

#### GrabPointInfo クラス
```csharp
- name: グラブポイント名
- transform: グラブポイントのTransform
- keyCode: グラブ操作のキー
- allowedVertexIndices: グラブ可能な頂点インデックスのリスト（空の場合は全頂点）
- maxGrabbedVertices: 最大グラブ頂点数
```

#### 使用方法
1. MagicaClothコンポーネントをアサイン
2. グラブポイント（Transform）を設定
3. 各グラブポイントにキーを割り当て
4. 必要に応じて`allowedVertexIndices`で制約を設定
5. プレイモードでキーを押してグラブ

---

## MagicaCloth2 統合のベストプラクティス

### 初期化
- `MagicaCloth.IsValid()`でクロスの準備完了を確認
- `TeamId`の取得と保持

### 頂点制御
```csharp
// 頂点をグラブ時: Fixed属性に変更
attributes[vertexIndex] = VertexAttribute.Fixed;

// リリース時: 元の属性に復元
attributes[vertexIndex] = originalAttribute;
```

### イベントフック
```csharp
MagicaManager.OnPreSimulation += UpdateGrabbedVertices;
MagicaManager.OnPostSimulation += ForceUpdateDisplayPositions;
Camera.onPreRender += OnCameraPreRender;
```

### 振動防止
- Fixed属性の頂点は制約計算から除外される
- `Camera.onPreRender`でダイレクトメッシュ制御を行い、レンダリング前に位置を確定

---

## 開発ガイドライン

### コーディング規則
- Unity の `[SerializeField]` でインスペクター公開
- MagicaManagerのイベントシステムを活用
- `null`チェックを確実に実行
- XML ドキュメントコメントを記載

### MagicaCloth2使用時の注意
- クロスの初期化を待ってからプロパティにアクセス
- Fixed属性に変更した頂点は、リリース時に必ず元の属性に復元
- `Camera.onPreRender`は登録/解除を確実に行う（メモリリーク防止）

---

## Git履歴

最近のコミット:
```
[latest] - Fix coordinate system issue when cape2 moves (v0.4.1)
30ff029 - Add multi-point vertex grabbing system with individual constraints
fe09cb0 - Fix cloth vertex vibration using Camera.onPreRender
dedc590 - Add double-sided glossy cloth shader and materials
5ce21e1 - Initial commit: Unity MR project with MagicaCloth2 vertex grabbing
```

---

## 参考リンク

- [MagicaCloth2 Documentation](https://magicasoft.jp/magica-cloth-2/)
- [Unity MCP GitHub](https://github.com/CoplayDev/unity-mcp)
- [Unity URP Documentation](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest)

---

## 更新履歴

| 日付 | 内容 |
|------|------|
| 2025-11-25 | プロジェクトステータス初期作成（実態に合わせて更新） |
| 2025-11-25 | v0.4.1リリース（cape2移動時の座標系問題修正、RandomGrabPointMover追加） |
| 2025-11-25 | v0.4.2リリース（視覚的頂点選択ツール実装、ClothVertexGrabberEditor追加） |
