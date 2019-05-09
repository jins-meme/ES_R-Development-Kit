* ## Development Kit for JINS MEME ES_R (JINS MEME Academic Pack).
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
3. Go to "Serch Solution Explore", right click on "References", and then go to "Manage NuGet Packages".
4. Search JINS MEME in the NuGet tab and install "JINSMEME_ES_R by JINS Inc".
5. Finally, build the project.

When, successfully build, sample UI shows up.
<a name="fig.1"></a>
<div align="center">
<img src="https://cloud.githubusercontent.com/assets/18042520/26829252/c2fb3e70-4aff-11e7-9435-e5be7f7929a1.png" alt="属性" title="Figure. 1" width="400">
</div>
<div style="text-align: center;">Figure. 1</div>
　　　

### STEP2 Connect JINS MEME ES_R
* Insert the USB dongle of JINS MEME ES_R
* Press "Scan port". After COM Number(COM4 in the image) shows press "Open".
<a name="fig.2"></a>  
　　　![screen shot 2017-06-06 at 21 39 25](https://cloud.githubusercontent.com/assets/18042520/26829445/a7f079be-4b00-11e7-9b92-f8079f7a000e.png)  
Figure. 2

* Turn JINS MEME ES_R on and then press "Scan MEME"
* When device NO(28A183055C47 in the image) shows, press "Connect" button.
<a name="fig.3"></a>  
　　　![screen shot 2017-06-06 at 21 43 47](https://cloud.githubusercontent.com/assets/18042520/26829646/451a4224-4b01-11e7-96bb-53b6ed8a72b2.png)  
Figure. 3

* When your JINS MEME ES_R is connected, "Start Measurement" button shows up.
<a name="fig.4"></a>  
　　　![screen shot 2017-06-06 at 21 46 42](https://cloud.githubusercontent.com/assets/18042520/26829782/bfadbd7c-4b01-11e7-84d8-fccf398119d4.png)  
Figure. 4

* Press "Start Measurement" button to start working JINS MEME 
<a name="fig.5"></a>   
![screen shot 2017-06-06 at 21 48 40](https://cloud.githubusercontent.com/assets/18042520/26829860/f1ce1d6a-4b01-11e7-91d1-cf6afc65c1f1.png)  
Figure. 5

### STEP 3 Prepare for Edit code: Overview 
Figure below shows the connection and data flow.
<a name="fig.6"></a>  
　　　<img src="https://user-images.githubusercontent.com/18042520/57433993-d1043f00-7274-11e9-9460-d873d87fedc4.png" width="550">  
　　Figure. 6

* PC gets data from JINS MEME ES_R via USB dongle.
* After connected, you can call "startDataReport" method in "MemeLib" to get ready to receive data.

### STEP 4 Get sensor data
* call "memeAcademicFullDataReceived" method to receive sensor data. The method is in delegate part in "MEME_Academic_Sample.cs" file.
* Project calls this method several times.
* Instance of "AcademicFullData" class stores sensor values.

### Class variables of AcademicFullData class
| variables | meaning |
----|---- 
| Cnt | data count |
| BattLv | battery level |
| AccX | Acceleration of X-axis |
| AccY | Acceleration of Y-axis |
| AccZ | Acceleration of Z-axis |
| GyroX | Gyroscope data of X-axis | 
| GyroY | Gyroscope data of Y-axis |
| GyroZ | Gyroscope data of Z-axis |
| EogL | Eog voltage value between the bridge(reference) and left nosepad electrodes | 
| EogR | Eog voltage value between the bridge(reference) and right nosepad electrodes |
| EogH | Horizontal eye movement |
| EogV | Vertical eye movement |

# 　　
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
1. Githubからプロジェクトをクローンまたはダウンロードします.
2. 「MEME_Academic_Sample.sln」をVisualStudio 2019(または2017)で開きます．
3. 「Serch Solution Explore」の中にある「References」を右クリックし，「Manage NuGet Packages」を選択します．
4. 検索窓で「JINS MEME」を検索し，検索結果から「JINSMEME_ES_R by JINS Inc.」を選択しインストールします．
5. ビルドを実行します．
    *  ビルドが問題なく行われた場合，データ取得のためUIサンプルが表示されます．（上述の[Figure. 1](#fig.1))

## STEP 2: JINS MEME ES_Rとの接続
1. PCにUSBドングルを接続してソフトウェアを起動したら、Scan PortボタンをタップしてOpenボタンを押します．
（上述の[Figure. 2](#fig.2))
2. JINS MEME ES_Rの電源ボタンを2秒長押ししてペアリングモードにします．
（上述の[Figure. 3](#fig.3))
3. ソフトウェアのScan MEMEボタンを押してJINS MEME ES_RのIDが表示されたらConnectボタンを押します．
（上述の[Figure. 4](#fig.4))
4. Start Measuermentボタンを押してセンサーの値を取得します．（上述の[Figure. 5](#fig.5))

## STEP3 :Codeを編集する準備：接続およびデータの流れを知る．
上述の[Figure. 6](#fig.6)の通りです.
* PCはUSBドングル経由でJINS MEME ES_Rと接続を行います.
* 接続後にMemeLibのstartDataReportメソッドを呼び出すとデータの取得が可能になります.

### センサー値の取得方法
MEME_Academic_Sample.cs ファイルの中のデリゲートを実装している部分，memeAcademicFullDataReceivedメソッドでセンサー値を受け取ることができます．
* このメソッドはJINS MEME ES_Rの動作クロックに合わせて複数回呼び出されます．
* 実際のセンサー値はAcademicFullDataクラスのインスタンスに格納されています．

### AcademicFullDataクラスの変数
| 変数名 | 意味 |
----|---- 
| Cnt | データのカウント値 |
| BattLv | バッテリーレベル(5段階) |
| AccX | X軸加速度 |
| AccY | Y軸加速度 |
| AccZ | Z軸加速度 |
| GyroX | X軸まわりの角速度 | 
| GyroY | Y軸まわりの角速度 |
| GyroZ | Z軸まわりの角速度 |
| EogL | ブリッジ（レファレンス電極）と左鼻パッド電極の電位差 | 
| EogR | ブリッジ（レファレンス電極）と右鼻パッド電極の電位差 |
| EogH | 横（左右）の視線移動 |
| EogV | 縦（上下）の視線移動 |
