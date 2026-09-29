#!/usr/bin/env python3
"""開発用のローカル配信。zip の中と同じ並びで配る:

    /              … webview/<page>/(既定 standard。index.html と manifest.json)
    /common/       … webview/common/
    /data/         … --data で指定したフォルダ(収録 CSV を ?dev&replay=/data/<名前> で開く用)

    python3 webview/tools/serve.py [--page standard] [--port 8790] [--data <フォルダ>]
    → http://127.0.0.1:8790/?dev   (模擬の計測・CSV を開くパネルが出る)

キャッシュさせない。.mjs / .wasm などの MIME を付ける。
"""
import argparse
import http.server
import os
import posixpath
import urllib.parse

HERE = os.path.dirname(os.path.abspath(__file__))
CSP = ("default-src 'self'; script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline'; "
       "img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self' blob: data:; worker-src 'self' blob:; "
       "media-src 'self' blob: data:; frame-src 'none'; object-src 'none'; form-action 'none'; base-uri 'none'")
ROOT = os.path.dirname(HERE)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--page", default="standard")
    ap.add_argument("--port", type=int, default=8790)
    ap.add_argument("--host", default="127.0.0.1")
    ap.add_argument("--data", default=None)
    a = ap.parse_args()
    mounts = [("/common/", os.path.join(ROOT, "common"))]
    if a.data:
        mounts.append(("/data/", os.path.abspath(os.path.expanduser(a.data))))
    mounts.append(("/", os.path.join(ROOT, a.page)))

    class H(http.server.SimpleHTTPRequestHandler):
        extensions_map = {**http.server.SimpleHTTPRequestHandler.extensions_map,
                          ".js": "text/javascript", ".mjs": "text/javascript", ".wasm": "application/wasm",
                          ".json": "application/json", ".gz": "application/gzip"}

        def translate_path(self, path):
            path = urllib.parse.unquote(urllib.parse.urlsplit(path).path)
            for prefix, base in mounts:
                if path.startswith(prefix):
                    rel = posixpath.normpath(path[len(prefix):]).lstrip("/")
                    if rel.startswith(".."):
                        return os.path.join(base, "__forbidden__")
                    return os.path.join(base, rel)
            return os.path.join(ROOT, "__none__")

        def end_headers(self):
            self.send_header("Cache-Control", "no-store")
            self.send_header("Content-Security-Policy", CSP)   # アプリと同じ(BRIDGE.md の Limits)。外への通信はここでも塞がる
            super().end_headers()

    print(f"http://{a.host}:{a.port}/?dev")
    http.server.ThreadingHTTPServer((a.host, a.port), H).serve_forever()


if __name__ == "__main__":
    main()
