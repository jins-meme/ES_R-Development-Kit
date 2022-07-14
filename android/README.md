## English

### Summary of ES_R-DevelopmentKit-android
* "ES_R" means JINS MEME ES_R (previouslly called JINS MEME Academic pack)
* sample code included.
* To record the data, you need to write codes as needed.

### Validation of connectivity: with/without the USB dongle
When developing your own software with ES_R-DevelopmentKit-android, you have 2 choices regarding how to connect JINS MEME ES_R with an Android device. One way is to use the official USB(BLE) dongle, and the other way is a direct connection with BLE chip embedded in the Android device. If you use the official USB dongle, please check supported(validated) android OS and devices [here](https://github.com/jins-meme/ES_R-DataLogger-for-Android).
If circumstances allow, we would recommend to use the dongle to avoid unexpected problems.

In case, you would like to connect JINS MEME ES_R with Android without the official USB dongle, please be sure that several tricky combinations between Device and OS exit. 

So far, we have velified that direct BLE connection is successful in the following combination. 

| App Version | Device Model | OS| Direct connection |
|:--:|:--:|:--:|:--:|
|2.1.0|Nexus5|7.x|-|
|2.1.0|Nexus5|6.x|-|
|2.1.0|Nexus5|4.4|✔|
|2.1.0|Nexus9|6.0.1|-|
|2.1.0|Nexus9|5.1.1|✔|
|2.1.0|HUAWEI mate pro 10|8.0.0|✔|
|2.1.1|Xperia Lite 8|10.0.0|✔|
|2.1.2|Xperia Lite 8|10.0.0|✔|
|2.1.2|Pixel6|12|✔|
|2.1.3|Nexus5|11.0.0|✔|
|2.1.3|Xperia Lite 8|10.0.0|✔|
|2.1.4|Xperia Lite 8|10.0.0|✔|

#### "✔" successfully connected. "-" not working.
