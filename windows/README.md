## Development Kit for JINS MEME ES_R (JINS MEME Academic Pack).
# English
([日本語補足](#japanesedoc))
## Summary of ES_R-DevelopmentKit-Windows


This project includes sample code to use JINS MEME ES_R(also called JINS MEME Academic pack). To run this project, please be sure that you have JINS MEME ES_R and BLE dongle. For general instruction, please go [here](https://jins-meme.github.io/apdoc/en/) and see manual.

## System environment
* Requirment: Microsoft .Net Framework 4.5 or Net Framework4.7
* Windows 10
* Visual Studio 2017, 2019
    * Checked runnning on Windows 10 version1803 and Visual Studio 2017
    * Checked runnning on Windows 10 version1803 and Visual Studio 2019

## STEP 1: How to build
1. Download or Clone this project
2. Open "MEME_Academic_Sample.sln" with Visual Studio 2019/2017
3. Go to "Serch Solution Expore", right click on "References", and then go to "Manage NuGet Packages".
4. Search JINS MEME in the NuGet tab and install "JINSMEME_ES_R by JINS Inc".
5. Finally, build the project.

When, successfully build, sample UI shows up.  
  　　![successbuild](https://cloud.githubusercontent.com/assets/18042520/26829252/c2fb3e70-4aff-11e7-9435-e5be7f7929a1.png)

### STEP2 Connect JINS MEME ES_R
* Insert the USB dongle of JINS MEME ES_R
* Press "Scan port". After COM Number(COM4 in the image) shows press "Open".
![screen shot 2017-06-06 at 21 39 25](https://cloud.githubusercontent.com/assets/18042520/26829445/a7f079be-4b00-11e7-9b92-f8079f7a000e.png)
* Turn JINS MEME ES_R on and then press "Scan MEME"
* When device NO(28A183055C47 in the image) shows, press "Connect" button.
![screen shot 2017-06-06 at 21 43 47](https://cloud.githubusercontent.com/assets/18042520/26829646/451a4224-4b01-11e7-96bb-53b6ed8a72b2.png)

* When your JINS MEME ES_R is connected, "Start Measurement" button shows up.
![screen shot 2017-06-06 at 21 46 42](https://cloud.githubusercontent.com/assets/18042520/26829782/bfadbd7c-4b01-11e7-84d8-fccf398119d4.png)

* Press "Start Measurement" button to start working JINS MEME  
![screen shot 2017-06-06 at 21 48 40](https://cloud.githubusercontent.com/assets/18042520/26829860/f1ce1d6a-4b01-11e7-91d1-cf6afc65c1f1.png)

### STEP3 Edit Code
### Fig.1 show the data flow.
PCはUSBドングル経由でJINS MEME ES_Rと接続を行います.
接続後にMemeLibのstartDataReportメソッドを呼び出すとデータの取得が可能になります.

### センサー値の取得方法
MEME_Academic_Sample.cs ファイルの中のデリゲートを実装している部分，memeAcademicFullDataReceivedメソッドでセンサー値を受け取ることができます．
このメソッドはJINS MEME ES_Rの動作クロックに合わせて複数回呼び出されます．
実際のセンサー値はAcademicFullDataクラスのインスタンスに格納されています．

### AcademicFullDataクラスの変数
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


<a name="japanesedoc"></a>
# 日本語
## はじめに
このプロジェクトは，JINS MEME ES_R (JINS MEME Academic Pack)の動作デモプロジェクトです．
プロジェクトを実行して動作させるためにはハードウェアである「[JINS MEME ES_R](https://jins-meme.com/ja/purchase/application/) (JINS MEME Academic Pack)」および専用「[BLEドングル](https://jins-meme.com/ja/purchase/application/)」が必要になります。

## 環境要件
* .Net Framework 4.5.2以上 まはた 4.7.2以上
* Windows10
* Visual Studio 2017, 2019
    * Windows 10 version1803 および Visual Studio 2019で動作確認済み
    * Windows 10 version1803 および Visual Studio 2017で動作確認済み

## STEP 1: ビルド
1. Githubからプロジェクトをクローンまたはダウンロードを行います。
2. Nugetから[JINSMEME_ES_R](https://www.nuget.org/packages/JINSMEME_ES_R/)と検索し必要なライブラリをプロジェクトに取り込みます．
3. プロジェクトをビルドして実行します．

## STEP 2: JINS MEME ES_Rとの接続
1. PCにUSBドングルを接続してソフトウェアを起動したら、Scan PortボタンをタップしてOpenボタンを押します．
2. JINS MEME ES_Rの電源ボタンを長押ししてペアリングモードにします．
3. ソフトウェアのScan MEMEボタンを押してJINS MEME ES_RのIDが表示されたらConnectボタンを押します．
4. Start Measuermentボタンを押してセンサーの値を取得します．  
　　

## STEP3 :Edit Code
### 全体フロー
図1の通りです.
PCはUSBドングル経由でJINS MEME ES_Rと接続を行います.
接続後にMemeLibのstartDataReportメソッドを呼び出すとデータの取得が可能になります.

### センサー値の取得方法
MEME_Academic_Sample.cs ファイルの中のデリゲートを実装している部分，memeAcademicFullDataReceivedメソッドでセンサー値を受け取ることができます．
このメソッドはJINS MEME ES_Rの動作クロックに合わせて複数回呼び出されます．
実際のセンサー値はAcademicFullDataクラスのインスタンスに格納されています．

### AcademicFullDataクラスの変数
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
