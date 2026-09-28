#!/usr/bin/env python3
"""WebView の中身を zip にする(アプリが設定の「zip を選ぶ」で読み込む形)。

    python3 webview/tools/make_zip.py [--page standard] [--out <出力先>] [--install]

zip の中の並び(アプリは展開して、仮想ホストの根に置く):
    manifest.json   {"name", "title", "version", "bridgeApi", "entry"}
    index.html      入口
    common/         共通部分(開発用の dev.js は入れない)

--install は、できた standard.zip を 3 アプリの同梱場所へも写す
(Mac: Mac/ES_R_DevKit_Mac/MEME_Academic/App/WebContent/standard.zip ほか。アプリに同梱する標準版)。
高機能版(advanced.zip)は python-processing-core で作る(ここの common/ を取り込む)。
"""
import argparse
import json
import os
import zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
REPO = os.path.dirname(ROOT)
INSTALL = {
    "standard": [
        os.path.join(REPO, "Mac", "ES_R_DevKit_Mac", "MEME_Academic", "App", "WebContent", "standard.zip"),
    ],
}
SKIP = {"dev.js", ".DS_Store"}


def build(page, out):
    src = os.path.join(ROOT, page)
    manifest = json.load(open(os.path.join(src, "manifest.json"), encoding="utf-8"))
    os.makedirs(os.path.dirname(out) or ".", exist_ok=True)
    n = 0
    # 中身が同じなら zip も同じバイト列になるように、時刻を固定して名前順に詰める
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
        def add(path, arc):
            nonlocal n
            info = zipfile.ZipInfo(arc, date_time=(2026, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o644 << 16
            with open(path, "rb") as f:
                z.writestr(info, f.read())
            n += 1
        for name in sorted(os.listdir(src)):
            if name not in SKIP and os.path.isfile(os.path.join(src, name)):
                add(os.path.join(src, name), name)
        common = os.path.join(ROOT, "common")
        for dirpath, dirnames, filenames in os.walk(common):
            dirnames.sort()
            for name in sorted(filenames):
                if name in SKIP:
                    continue
                full = os.path.join(dirpath, name)
                add(full, "common/" + os.path.relpath(full, common).replace(os.sep, "/"))
    print(f"{out}: {n} files, {os.path.getsize(out) / 1024:.0f} KB, {manifest['name']} {manifest['version']} (bridgeApi {manifest['bridgeApi']})")
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--page", default="standard")
    ap.add_argument("--out", default=None)
    ap.add_argument("--install", action="store_true")
    a = ap.parse_args()
    out = a.out or os.path.join(ROOT, "dist", f"{a.page}.zip")
    build(a.page, out)
    if a.install:
        for dest in INSTALL.get(a.page, []):
            os.makedirs(os.path.dirname(dest), exist_ok=True)
            with open(out, "rb") as f, open(dest, "wb") as g:
                g.write(f.read())
            print("installed:", os.path.relpath(dest, REPO))


if __name__ == "__main__":
    main()
