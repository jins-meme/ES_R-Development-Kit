# CLAUDE.md

このリポジトリは Windows / Mac / Android 向け JINS MEME ES_R 開発キットを含むモノレポです。
**このリポジトリは public です。ローカルパス・署名鍵・資格情報などの機微情報はこのファイルに書かないでください。**
マシン固有の情報(配布物の出力先パスなど)は `CLAUDE.local.md`(gitignore対象、各マシンに個別配置)に書きます。`CLAUDE.local.md.example` にテンプレートがあります。

## リポジトリ構成

- `windows/` : Windows版(.NET)。フル機能版 `ES_R_DevKit_Windows` / 簡易版 `ES_R_DevKit_Windows_Simple`
- `Mac/` : Mac版(Xcode)。フル機能版 `ES_R_DevKit_Mac` / 簡易版 `ES_R_DevKit_Mac_Simple`
- `android/` : Android版。新Kotlin実装 `ES_R_DevKit_Android2` / 旧Java実装 `ES_R_DevKit_Android`

各プラットフォームは独立してバージョニングされています。

## GitHub Releases 運用方針

**フル機能版のみをバイナリ配布します。** Simple版・旧Android(Java)版はソース公開のみとし、GitHub Releasesの対象外とします。

| プラットフォーム | 対象プロジェクト | バージョン参照元 |
|---|---|---|
| Windows | `windows/ES_R_DevKit_Windows/MEME_Academic_Sample` | `MEME_Academic_Sample.csproj` の `<Version>` |
| Mac | `Mac/ES_R_DevKit_Mac` | `MEME_Academic.xcodeproj/project.pbxproj` の `MARKETING_VERSION`(表示バージョン)/ `CURRENT_PROJECT_VERSION`(ビルド番号) |
| Android | `android/ES_R_DevKit_Android2` | `app/build.gradle.kts` の `versionName` |

### タグ命名規則: `<os>-v<version>`

- Windows: `windows-v2.1.0`
- Mac: `mac-v1.3.3`
- Android: `android2-v3.0.2`

### 添付アセット(中間生成物を含めない)

- Windows: `scripts/package-windows-release.ps1 -SourceDir <配布用フォルダ> -OutputZip <出力zip>` でzip化する(`obj/`, `bin/Debug`, `.vs`, `*.pdb`, `*.user` を除外)
- Mac: `scripts/package-mac-release.sh <YourApp.app> <出力zip>` でzip化する(`ditto` でリソースフォークを保ちつつ `.DS_Store` 等を含めない)
- Android: Gradleの `assembleRelease` / `bundleRelease` が出力する署名済み apk/aab をそのまま使う(追加のパッケージングは不要)

配布物そのもの(署名済みインストーラー等)をどこから取得するかはマシンごとに異なるため、`CLAUDE.local.md` に書かれたパスを参照する。記載が無い場合はユーザーに確認する。

## 「GHでリリース作成して」と言われたときの手順

1. **対象プラットフォームの判定**: 直近のコミット内容・変更ディレクトリから判定する。複数にまたがる、または判断できない場合はユーザーに確認する。
2. **バージョン確認**: 上表の参照元から現在のバージョン番号を読み取り、タグ名(`<os>-v<version>`)を組み立てる。
3. **重複チェック**: `git tag -l "<tag>"` と `git ls-remote --tags origin` で同名タグが無いことを確認する。あればユーザーに確認する。
4. **作業ツリー確認**: `git status` でクリーンであることを確認する。未コミットの変更があればユーザーに確認してから進める。
5. **配布用アセットの準備**: 上記スクリプトを使ってzip化するか、Android の場合は署名済み apk/aab をそのまま使う。ソースは `CLAUDE.local.md` に書かれたパス、無ければユーザーに確認する。
6. **タグ作成とpush**: `git tag <tag>` → `git push origin <tag>`
7. **リリース作成**: `gh release create <tag> <asset...> --title "<Platform> v<version>" --notes "<変更点サマリ>"`(変更点サマリは対象プラットフォームの直近コミットメッセージから要約する)
8. 作成したReleaseのURLをユーザーに報告する。

## 前提条件

- `gh` CLI がインストール・認証済みであること(`gh auth status` で確認。未認証なら `gh auth login` を促す)
- push権限を持つGitHubアカウントで認証されていること

## 注意事項

- アップロード前に、添付ファイルへ秘密情報(署名鍵・内部限定の設定・デバッグ用の資格情報等)が混入していないか確認する
- リリース対象外(Simple版・旧Android版)のバイナリを誤って添付しない
- タグ削除・リリース削除など破壊的操作は必ずユーザーに確認してから行う
