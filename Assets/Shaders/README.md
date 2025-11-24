# 両面光沢布シェーダー (DoubleSidedGlossyCloth)

## 概要
このシェーダーは、布の表と裏で異なる色を表示し、サテンやエナメルのような光沢のある質感を表現します。

## 特徴
- ✅ **両面レンダリング** - 表と裏で別々の色を指定可能
- ✅ **高光沢表面** - Smoothness/Metallicパラメータで調整可能
- ✅ **PBRライティング** - URPの物理ベースレンダリング対応
- ✅ **シャドウ対応** - 影の投影・受影に対応
- ✅ **ノーマルマップ対応** - 表面の凹凸表現

## パラメータ

### Front Side（表面）
- **Front Color** - 表面の色（RGB + Alpha）

### Back Side（裏面）
- **Back Color** - 裏面の色（RGB + Alpha）

### Surface Properties（表面特性）
- **Smoothness** (0-1) - 表面の滑らかさ
  - 0 = マット（艶消し）
  - 1 = 完全な鏡面（エナメル/サテン風）
  - 推奨値: 0.9-0.95

- **Metallic** (0-1) - 金属性
  - 0 = 非金属（布）
  - 1 = 完全な金属
  - 推奨値: 0.05-0.15（布の場合）

### Normal Map（法線マップ）
- **Normal Map** - 表面の凹凸を表現するテクスチャ（オプション）
- **Normal Scale** - 法線の強さ（デフォルト: 1.0）

## 使用方法

### 1. マテリアルの作成
Project ウィンドウで右クリック → Create → Material

### 2. シェーダーの割り当て
作成したマテリアルのInspectorで：
- Shader ドロップダウンをクリック
- `Custom/DoubleSidedGlossyCloth` を選択

### 3. 色の設定
- **Front Color**: 表面の色を設定（例: 赤）
- **Back Color**: 裏面の色を設定（例: 青）

### 4. 光沢の調整
- **Smoothness**: 0.9-0.95 に設定（サテン/エナメル風）
- **Metallic**: 0.05-0.15 に設定（布の質感）

### 5. オブジェクトへの適用
マテリアルをメッシュレンダラーの Materials スロットにドラッグ&ドロップ

## サンプルマテリアル

プロジェクトに以下のサンプルマテリアルが含まれています：

### GlossyCloth_RedBlue
- 表: 赤 (R:1, G:0.2, B:0.2)
- 裏: 青 (R:0.2, G:0.3, B:1)
- Smoothness: 0.95
- Metallic: 0.1

### GlossyCloth_PinkPurple
- 表: ピンク (R:1, G:0.4, B:0.7)
- 裏: 紫 (R:0.5, G:0.1, B:0.6)
- Smoothness: 0.92
- Metallic: 0.05

## cape2への適用例

```
1. Hierarchy で cape2/Plane を選択
2. Inspector で Mesh Renderer → Materials を確認
3. サンプルマテリアル（GlossyCloth_RedBlue）をドラッグ
4. Play モードで表裏の色が違うことを確認
```

## 技術的詳細

### 実装方法
- **SV_IsFrontFace** セマンティクスで表裏判定
- URPの `UniversalFragmentPBR` 関数で物理ベースライティング
- `Cull Off` で両面レンダリング有効化
- 裏面では法線を反転させて正しいライティング

### 対応パイプライン
- Universal Render Pipeline (URP)
- Unity 6000.0.x 以降

### パフォーマンス
- 両面レンダリングのため、通常のシェーダーの約2倍のフラグメント処理
- モバイルデバイスでは頂点数を抑えることを推奨

## トラブルシューティング

### 色が表示されない
- ライトが正しく配置されているか確認
- Smoothness/Metallicの値を調整

### 影が正しくない
- ShadowCasterパスが正しく動作していることを確認
- プロジェクト設定でシャドウが有効か確認

### 裏面の法線が反転していない
- シェーダーコードの `if (!isFrontFace)` 部分を確認

## カスタマイズ

### 色以外のプロパティを追加する場合
1. Properties セクションに新しいプロパティを追加
2. CBUFFER_START に変数を追加
3. fragment シェーダーで使用

例：エミッション（発光）を追加
```hlsl
// Properties
_EmissionColor ("Emission Color", Color) = (0, 0, 0, 0)

// CBUFFER
float4 _EmissionColor;

// Fragment
surfaceData.emission = _EmissionColor.rgb;
```

## ライセンス
このシェーダーはプロジェクト内で自由に使用・改変可能です。

## 作成日
2025-11-24
