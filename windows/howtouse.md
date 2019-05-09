## このプロジェクトの使い方

このプロジェクトはJINS MEME ES_R (JINS MEME Academic Pack)の動作デモプロジェクトです。
ライブラリを利用してJINS MEME ES_Rのデータを取得するところを実際に試すことができます。

## 最初に
Nugetからライブラリを取得して登録します。
ライブラリーにアップデートがあった場合も更新を行います。
ライブラリはNugetに登録されています。


## 全体フロー
図1の通りです。
PCはUSBドングル経由でJINS MEME ES_Rと接続を行います。
接続後にMemeLibのstartDataReportメソッドを呼び出すとデータの取得が可能になります。

## センサー値の取得方法
MEME_Academic_Sample.cs ファイルの中のデリゲートを実装している部分、memeAcademicFullDataReceivedメソッドでセンサー値を受け取ることができます。
このメソッドはJINS MEME ES_Rの動作クロックに合わせて複数回呼び出されます。
実際のセンサー値はAcademicFullDataクラスのインスタンスに格納されています。

## AcademicFullDataクラスの変数
| 変数名 | 意味 |
----|---- 
| Cnt | データのカウント値 |
| BattLv | バッテリーレベル(5段階) |
| AccX | 加速度X軸 |
| AccY | 加速度Y軸 |
| AccZ | 加速度Z軸 |
| GyroX | 角速度X軸 | 
| GyroY | 角速度Y軸 |
| GyroZ | 角速度Z軸 |
| EogL | 視線左 | 
| EogR | 視線右 |
| EogH | 視線上 |
| EogV | 視線下 |
