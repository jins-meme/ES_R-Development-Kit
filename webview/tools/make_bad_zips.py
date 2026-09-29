#!/usr/bin/env python3
"""アプリの zip の検査(webview/BRIDGE.md の Limits)を確かめるための、わざと悪い zip を作る。

    python3 webview/tools/make_bad_zips.py <出力フォルダ>

名前が good で始まるものは受け入れられ、それ以外は断られるのが正しい。Mac の自己テスト
(MEME_AUTOTEST_SUITE=zip MEME_AUTOTEST_BADZIPS=<出力フォルダ>)が 1 つずつ読み込んで確かめる。
書き込まれてはいけないファイルの名前は evil_* にしてある(見つかったら検査の抜け)。
"""
import io
import json
import os
import struct
import sys
import unicodedata
import zipfile

MANIFEST = {"name": "t", "title": "Test", "version": "1.0.0", "bridgeApi": 1, "entry": "index.html"}
INDEX = b"<!doctype html><title>t</title><p>test"


def base(z, manifest=None, index=True):
    z.writestr("manifest.json", json.dumps(manifest or MANIFEST))
    if index:
        z.writestr("index.html", INDEX)


def make(path, fn):
    buf = io.BytesIO()
    with zipfile.ZipFile(buf, "w", zipfile.ZIP_DEFLATED) as z:
        fn(z)
    data = bytearray(buf.getvalue())
    return data


def patch(data, name, *, flags=None, method=None, usize=None):
    """name のエントリの目次と中身の見出しを書き換える(zipfile では作れない形)"""
    raw = name.encode()
    p = 0
    while True:
        p = data.find(b"PK\x01\x02", p)
        if p < 0:
            break
        nlen = struct.unpack_from("<H", data, p + 28)[0]
        if bytes(data[p + 46:p + 46 + nlen]) == raw:
            local = struct.unpack_from("<I", data, p + 42)[0]
            if flags is not None:
                struct.pack_into("<H", data, p + 8, flags); struct.pack_into("<H", data, local + 6, flags)
            if method is not None:
                struct.pack_into("<H", data, p + 10, method); struct.pack_into("<H", data, local + 8, method)
            if usize is not None:
                struct.pack_into("<I", data, p + 24, usize); struct.pack_into("<I", data, local + 22, usize)
            return data
        p += 4
    raise KeyError(name)


def symlink(z, name, target):
    info = zipfile.ZipInfo(name)
    info.create_system = 3
    info.external_attr = (0o120777 << 16)
    z.writestr(info, target)


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else "bad_zips"
    os.makedirs(out, exist_ok=True)
    cases = {
        "good_minimal": make(None, lambda z: base(z)),
        "good_subfolders": make(None, lambda z: (base(z), z.writestr("a/b/c.js", "0"), z.writestr("a/", ""))),
        "slip_dotdot": make(None, lambda z: (base(z), z.writestr("../evil_slip.txt", "x"))),
        "slip_deep_dotdot": make(None, lambda z: (base(z), z.writestr("a/../../evil_slip2.txt", "x"))),
        "slip_absolute": make(None, lambda z: (base(z), z.writestr("/tmp/evil_abs.txt", "x"))),
        "slip_backslash": make(None, lambda z: (base(z), z.writestr("..\\evil_bs.txt", "x"))),
        "slip_drive": make(None, lambda z: (base(z), z.writestr("C:evil_drive.txt", "x"))),
        "symlink_dir": make(None, lambda z: (base(z), symlink(z, "link", "/tmp"), z.writestr("link/evil_link.txt", "x"))),
        "symlink_file": make(None, lambda z: (base(z), symlink(z, "evil_link2", "/etc/hosts"))),
        "bomb_ratio": make(None, lambda z: (base(z), z.writestr("big.bin", b"\0" * 40_000_000))),
        "bomb_entry": make(None, lambda z: (base(z), z.writestr("big.bin", os.urandom(1000) * 60_000))),
        "too_many_files": make(None, lambda z: (base(z), [z.writestr(f"f/{i}.txt", "x") for i in range(2100)])),
        "dup_same": make(None, lambda z: (base(z), z.writestr("a.js", "1"), z.writestr("a.js", "2"))),
        "dup_case": make(None, lambda z: (base(z), z.writestr("Index.html", "x"))),
        "dup_unicode": make(None, lambda z: (base(z), z.writestr(unicodedata.normalize("NFC", "é.js"), "1"),
                                             z.writestr(unicodedata.normalize("NFD", "é.js"), "2"))),
        "file_and_folder": make(None, lambda z: (base(z), z.writestr("a", "1"), z.writestr("a/b.js", "2"))),
        "reserved_name": make(None, lambda z: (base(z), z.writestr("con.txt", "x"))),
        "trailing_dot": make(None, lambda z: (base(z), z.writestr("a./b.js", "x"))),
        "control_char": make(None, lambda z: (base(z), z.writestr("a\x01.js", "x"))),
        "no_manifest": make(None, lambda z: z.writestr("index.html", INDEX)),
        "no_entry": make(None, lambda z: base(z, index=False)),
        "bad_bridge": make(None, lambda z: base(z, {**MANIFEST, "bridgeApi": 2})),
        "entry_outside": make(None, lambda z: base(z, {**MANIFEST, "entry": "../index.html"})),
        "long_title": make(None, lambda z: base(z, {**MANIFEST, "title": "T" * 200})),
        "not_a_zip": bytearray(b"this is not a zip file" * 10),
    }
    # zipfile では作れない形は、作ってから見出しを書き換える
    cases["bomb_lying_size"] = patch(make(None, lambda z: (base(z), z.writestr("big.bin", b"\0" * 5_000_000))), "big.bin", usize=1000)
    cases["encrypted"] = patch(make(None, lambda z: (base(z), z.writestr("x.js", "x"))), "x.js", flags=1)
    cases["method_bzip2"] = patch(make(None, lambda z: (base(z), z.writestr("x.js", "x"))), "x.js", method=12)
    for name, data in cases.items():
        with open(os.path.join(out, name + ".zip"), "wb") as f:
            f.write(data)
    print(f"{out}: {len(cases)} zips ({sum(1 for n in cases if n.startswith('good'))} good)")


if __name__ == "__main__":
    main()
