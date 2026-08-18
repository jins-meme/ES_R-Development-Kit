#!/usr/bin/env python3
"""ES_R CSV の記録時刻をローカルタイムから UTC へ変換する一括コンバータ。

Mac版 DevKit は以前、CSV の DATE 列とファイル名のタイムスタンプを
ローカルタイム（JST 等）で書き出していた。Android版 DevKit と同じく UTC で
記録するよう修正したため、それ以前に記録した CSV を後から揃えるためのツール。

変換対象は 1ファイルにつき 2箇所:

  * DATE 列（3カラム目）  "2026/07/30 16:13:33.65" -> "2026/07/30 07:13:33.65"
  * ファイル名の日時      "<MAC>_20260730161334.csv" -> "<MAC>_20260730071334.csv"

DATE 列以外（ARTIFACT 列・NUM・センサ値・ヘッダのコメント行・改行コード）は
1バイトも変更しない。

既定は dry-run。実際に書き換えるには --apply を付ける。
元ファイルは既定でバックアップディレクトリへ退避する（--no-backup で無効）。

使い方:
    python3 convert_csv_local_to_utc.py ~/Documents/JINS/sensorData            # 確認だけ
    python3 convert_csv_local_to_utc.py ~/Documents/JINS/sensorData --apply    # 実行
"""

from __future__ import annotations

import argparse
import os
import re
import shutil
import sys
from datetime import datetime, timezone
from pathlib import Path
from zoneinfo import ZoneInfo

# DATE 列: "yyyy/MM/dd HH:mm:ss" + 小数秒（Mac版は .SS、Android版は .SSS）
DATE_RE = re.compile(r"^(\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2})(\.\d+)?$")
DATE_FMT = "%Y/%m/%d %H:%M:%S"

# ファイル名: "<MACアドレス>_yyyyMMddHHmmss.csv"
NAME_RE = re.compile(r"^(?P<head>.+_)(?P<stamp>\d{14})(?P<tail>\.csv)$", re.IGNORECASE)
NAME_FMT = "%Y%m%d%H%M%S"

HEADER_MARK = "//ARTIFACT"
DATE_COLUMN = 2  # ARTIFACT, NUM, DATE, ...

# ファイル更新時刻との突き合わせで「既にUTC」を判定する際の許容差（秒）。
# 記録開始 <= mtime <= 記録開始 + 1日 に収まっていれば整合しているとみなす。
# タイムゾーン差より広いので、両方の解釈が成立することがある（その場合は近い方を採る）。
MTIME_SLACK_SEC = 86_400


class SkipFile(Exception):
    """このファイルは変換対象外（理由付き）。"""


def convert_stamp(text: str, src_tz: ZoneInfo, fmt: str) -> str:
    """fmt 形式の日時文字列を src_tz のローカル時刻とみなし、UTC の同形式へ変換する。"""
    naive = datetime.strptime(text, fmt)
    return naive.replace(tzinfo=src_tz).astimezone(timezone.utc).strftime(fmt)


def convert_date_field(field: str, src_tz: ZoneInfo, cache: dict[str, str]) -> str:
    """DATE 列 1個を変換する。小数秒は桁数ごとそのまま引き継ぐ（オフセットは秒単位なので不変）。"""
    m = DATE_RE.match(field)
    if not m:
        raise SkipFile(f"DATE 列の書式が想定外: {field!r}")
    head, fraction = m.group(1), m.group(2) or ""
    # 100Hz なら同じ「秒」が100行続くので、秒単位でキャッシュすると変換回数が1/100になる。
    converted = cache.get(head)
    if converted is None:
        converted = convert_stamp(head, src_tz, DATE_FMT)
        cache[head] = converted
    return converted + fraction


def convert_text(text: str, src_tz: ZoneInfo) -> tuple[str, int, str, str]:
    """CSV 全体を変換し、(新しい本文, 変換行数, 先頭行のDATE, 変換後の先頭行DATE) を返す。"""
    lines = text.split("\n")
    try:
        header_index = next(i for i, line in enumerate(lines) if line.startswith(HEADER_MARK))
    except StopIteration:
        raise SkipFile("ES_R形式のヘッダ行（//ARTIFACT...）が無い")

    converted_rows = 0
    first_before = first_after = ""
    cache: dict[str, str] = {}

    for i in range(header_index + 1, len(lines)):
        line = lines[i]
        if not line.strip():
            continue  # 末尾の空行など。行そのものは保持する。
        fields = line.split(",")
        if len(fields) <= DATE_COLUMN:
            raise SkipFile(f"{i + 1}行目のカラム数が不足: {line!r}")
        before = fields[DATE_COLUMN]
        after = convert_date_field(before, src_tz, cache)
        if not converted_rows:
            first_before, first_after = before, after
        fields[DATE_COLUMN] = after
        lines[i] = ",".join(fields)
        converted_rows += 1

    if not converted_rows:
        raise SkipFile("データ行が無い")
    return "\n".join(lines), converted_rows, first_before, first_after


def convert_name(path: Path, src_tz: ZoneInfo) -> str:
    """ファイル名の日時部分を変換した新しいファイル名を返す。"""
    m = NAME_RE.match(path.name)
    if not m:
        raise SkipFile(f"ファイル名に yyyyMMddHHmmss が見つからない: {path.name}")
    stamp = convert_stamp(m.group("stamp"), src_tz, NAME_FMT)
    return f"{m.group('head')}{stamp}{m.group('tail')}"


def classify_timezone(first_date: str, mtime: float, src_tz: ZoneInfo) -> str:
    """DATE 列が既に UTC か、ローカルタイムかを、ファイル更新時刻との整合で判定する。

    mtime は記録終了時刻（絶対時刻）なので、記録開始時刻（＝先頭行のDATE）は
    mtime の少し前にあるはず。DATE をローカル解釈した場合と UTC 解釈した場合で、
    そこに収まる方が本来のタイムゾーン。JST のファイルなら UTC 解釈は約9時間ずれる。

    戻り値は "utc" / "local" / "unknown"。mtime が記録後に触られている
    （cp でコピーした等）と判定材料が無くなるため "unknown" を返す。
    変換をやめるのは "utc" と確証が持てた場合だけにし、
    判定できないものは指示どおり変換する（警告は出す）。
    """
    head = DATE_RE.match(first_date).group(1)
    naive = datetime.strptime(head, DATE_FMT)
    as_local = naive.replace(tzinfo=src_tz).timestamp()
    as_utc = naive.replace(tzinfo=timezone.utc).timestamp()
    # 記録開始は mtime より前で、そう遠くない過去にあるはず。
    plausible = {kind: mtime - start
                 for kind, start in (("local", as_local), ("utc", as_utc))
                 if 0 <= mtime - start <= MTIME_SLACK_SEC}
    if not plausible:
        return "unknown"
    # 両方あり得る場合（タイムゾーン差より許容幅が広いとき）は mtime に近い方を採る。
    return min(plausible, key=plausible.get)


def process(path: Path, src_tz: ZoneInfo, args: argparse.Namespace) -> str:
    """1ファイルを処理し、結果を表す1文字（. 変換 / s スキップ / ! エラー）を返す。"""
    try:
        text = path.read_text(encoding="utf-8")
        stat = path.stat()
        new_text, rows, before, after = convert_text(text, src_tz)
        new_name = convert_name(path, src_tz)

        kind = classify_timezone(before, stat.st_mtime, src_tz)
        if kind == "utc" and not args.force:
            raise SkipFile("DATE がファイル更新時刻と照らして既に UTC（--force で強制変換）")

        new_path = path.with_name(new_name)
        if new_path != path and new_path.exists():
            raise SkipFile(f"変換後の名前が既に存在する: {new_name}")

        print(f"  {path.name}")
        print(f"    -> {new_name}   ({rows}行)")
        print(f"    DATE {before}  ->  {after}")
        if kind == "unknown":
            print("    [warn] ファイル更新時刻が記録時刻と合わず、変換要否を裏取りできませんでした"
                  "（コピーされたファイル等）。既に UTC のファイルでないか確認してください。")

        if not args.apply:
            return "."

        if args.backup:
            backup_dir = Path(args.backup_dir) if args.backup_dir else path.parent / "_local_backup"
            backup_dir.mkdir(parents=True, exist_ok=True)
            backup_path = backup_dir / path.name
            if backup_path.exists():
                raise SkipFile(f"バックアップ先が既に存在する: {backup_path}")
            shutil.copy2(path, backup_path)

        # 一時ファイルへ書いてから置き換える（途中で落ちても元ファイルを壊さない）。
        tmp_path = path.with_name(path.name + ".utc.tmp")
        tmp_path.write_text(new_text, encoding="utf-8")
        # 記録の絶対時刻を表す更新時刻は変えない（並び順とUTC判定を保つため）。
        os.utime(tmp_path, (stat.st_atime, stat.st_mtime))
        os.replace(tmp_path, new_path)
        if new_path != path:
            path.unlink()

        # 書き出した内容を読み直して、先頭行が期待どおり変換されているか確認する。
        check = new_path.read_text(encoding="utf-8").split("\n")
        header_index = next(i for i, l in enumerate(check) if l.startswith(HEADER_MARK))
        actual = check[header_index + 1].split(",")[DATE_COLUMN]
        if actual != after:
            print(f"    !! 検証失敗: 先頭行が {actual!r}（期待 {after!r}）", file=sys.stderr)
            return "!"
        return "."

    except SkipFile as e:
        print(f"  [skip] {path.name}: {e}")
        return "s"
    except Exception as e:  # noqa: BLE001 - 1ファイルの失敗で全体を止めない
        print(f"  [ERROR] {path.name}: {e}", file=sys.stderr)
        return "!"


def collect(targets: list[str]) -> list[Path]:
    files: list[Path] = []
    for t in targets:
        p = Path(t).expanduser()
        if p.is_dir():
            files += sorted(p.glob("*.csv"))
        elif p.is_file():
            files.append(p)
        else:
            print(f"  [ERROR] 見つからない: {p}", file=sys.stderr)
    return files


def main() -> int:
    parser = argparse.ArgumentParser(
        description="ES_R CSV の DATE 列とファイル名をローカルタイムから UTC へ変換する。",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    parser.add_argument("paths", nargs="+", metavar="PATH",
                        help="CSVファイル、またはCSVを含むディレクトリ")
    parser.add_argument("--from-tz", default="Asia/Tokyo",
                        help="記録時のタイムゾーン（既定: Asia/Tokyo）")
    parser.add_argument("--apply", action="store_true",
                        help="実際に書き換える（付けない場合は内容を表示するだけ）")
    parser.add_argument("--backup-dir", default=None,
                        help="元ファイルの退避先（既定: 各CSVと同じ階層の _local_backup/）")
    parser.add_argument("--no-backup", dest="backup", action="store_false",
                        help="元ファイルを退避しない")
    parser.add_argument("--force", action="store_true",
                        help="既に UTC に見えるファイルも変換する")
    args = parser.parse_args()

    try:
        src_tz = ZoneInfo(args.from_tz)
    except Exception:
        print(f"不明なタイムゾーン: {args.from_tz}", file=sys.stderr)
        return 2

    files = collect(args.paths)
    if not files:
        print("対象のCSVがありません。")
        return 1

    mode = "変換します" if args.apply else "確認のみ（--apply で実行）"
    print(f"{len(files)} 件 / {args.from_tz} -> UTC / {mode}\n")

    results = [process(p, src_tz, args) for p in files]
    done, skipped, failed = (results.count(c) for c in ".s!")
    print(f"\n変換 {done} 件 / スキップ {skipped} 件 / 失敗 {failed} 件")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
