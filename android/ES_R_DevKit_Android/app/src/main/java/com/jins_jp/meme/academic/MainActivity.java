
package com.jins_jp.meme.academic;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.BroadcastReceiver;
import android.content.DialogInterface;
import android.content.IntentFilter;
import android.content.ServiceConnection;
import android.content.SharedPreferences;
import android.media.MediaScannerConnection;
import android.media.MediaScannerConnection.OnScanCompletedListener;
import android.net.Uri;
import android.os.Bundle;
import android.os.Handler;
import android.preference.PreferenceManager;
import android.support.v7.app.AppCompatActivity;
import android.view.KeyEvent;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.view.WindowManager;
import android.widget.ArrayAdapter;
import android.widget.Button;

import com.jins.meme.academic.util.DataEncryption;
import com.jins.meme.academic.util.HexDump;
import com.jins.meme.academic.util.LogCat;
import com.jins_jp.meme.academic.graph.GraphAcc;
import com.jins_jp.meme.academic.graph.GraphEOG;
import com.jins_jp.meme.academic.graph.GraphGyro;

import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.text.SimpleDateFormat;
import java.util.Collections;
import java.util.Date;
import java.util.Locale;
import java.util.Set;
import java.util.TimeZone;
import java.util.Timer;
import java.util.TimerTask;
import java.util.TreeSet;

abstract class MainActivity extends AppCompatActivity {
    protected final String TAG = getClass().getSimpleName();

    protected Common common = null;
    protected Activity activity = this;

    protected Handler handler = new Handler();

    protected GraphEOG graphEOG = null;
    protected GraphAcc graphAcc = null;
    protected GraphGyro graphGyro = null;

    protected boolean isScannin = false;
    protected boolean isConnect = false;
    protected boolean isMeasure = false;
    protected boolean isMarking = false;
    protected boolean isInitial = false;
    protected final int TIMEOUT = 6;

    protected Timer timerError;
    protected int mQuality = 1;
    protected long mPrevTime = 0;
    protected long mPrevCount = -1;
    protected long mTotalCount = 0;
    protected long mErrorCount = 0;

    protected long mRatioPrev = 100;
    protected long mNumTotalLast = 0;
    protected long mNumTotalPrev = 0;

    protected Set<String> mDeviceSet;
    protected ArrayAdapter<String> mDeviceAdapter;

    protected byte[] mReadByte = new byte[20];
    protected final static String PATH_LOCAL = "/JINS/MEME academic/";
    protected String mMemeAddress = null;
    protected String mMemeVersion = null;
    protected String mFileName = null;

    protected final static byte DATA_LENGTH = (byte) (0x14 & 0xFF);
    protected final static byte ADN_START_STOP_SEND = (byte) (0xA0 & 0xFF);
    protected final static byte ADN_GET_DEV_INFO = (byte) (0xA1 & 0xFF);
    protected final static byte ADN_GET_MODE = (byte) (0xA3 & 0xFF);
    protected final static byte ADN_SET_MODE = (byte) (0xA4 & 0xFF);
    protected final static byte ADN_CLR_PARAMS = (byte) (0xA8 & 0xFF);
    protected final static byte ADN_GET_6AXIS_PARAMS = (byte) (0xA9 & 0xFF);
    protected final static byte ADN_SET_6AXIS_PARAMS = (byte) (0xAA & 0xFF);
    protected final static byte AUP_REPORT_DEV_INFO = (byte) (0x81 & 0xFF);
    protected final static byte AUP_REPORT_MODE = (byte) (0x83 & 0xFF);
    protected final static byte AUP_REPORT_6AXIS_PARAMS = (byte) (0x89 & 0xFF);
    protected final static byte AUP_REPORT_RESP = (byte) (0x8F & 0xFF);
    protected final static byte AUP_REPORT_ACADEMIA1 = (byte) (0x98 & 0xFF);
    protected final static byte AUP_REPORT_ACADEMIA2 = (byte) (0x99 & 0xFF);
    protected final static byte AUP_REPORT_ACADEMIA3 = (byte) (0x9A & 0xFF);

    protected final static int GrasphSkipCount = 5;
    private boolean EogGraphEnableFlag = false;
    private boolean AccGraphEnableFlag = false;
    private boolean GyroGraphEnableFlag = false;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        // get common tool
        common = new Common(this, activity, handler);
        // get graph tool
        graphEOG = new GraphEOG(this, activity, handler);
        graphAcc = new GraphAcc(this, activity, handler);
        graphGyro = new GraphGyro(this, activity, handler);
        // fix orientation
        common.setOrientation();
        // check storage state
        common.makeDirectory(PATH_LOCAL);
        // set view
        mDeviceSet = Collections.synchronizedSortedSet(new TreeSet<String>());
        mDeviceAdapter = new ArrayAdapter<String>(getApplicationContext(), R.layout.list_item_1);
        mDeviceAdapter.setDropDownViewResource(R.layout.spinner_dropdown_item);
        // register BroadcastReceiver
        registerReceiver(mReceiver, makeIntentFilter());
    }

    @Override
    protected void onResume() {
        super.onResume();
        // set to keep the screen on
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
    }

    @Override
    protected void onPause() {
        super.onPause();
        // set to keep the screen on
        getWindow().clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        // clear collection
        mDeviceSet.clear();
        mDeviceAdapter.clear();
        // unregister BroadcastReceiver
        unregisterReceiver(mReceiver);
        // run gc
        System.gc();
    }

    @Override
    public boolean dispatchKeyEvent(KeyEvent event) {
        if (event.getAction() == KeyEvent.ACTION_DOWN) {
            switch (event.getKeyCode()) {
                case KeyEvent.KEYCODE_BACK:
                    // show finish dialog
                    AlertDialog.Builder builder = new AlertDialog.Builder(activity);
                    builder.setMessage(getString(R.string.msg_finish_app));
                    builder.setPositiveButton(R.string.button_dialog_ok,
                            new DialogInterface.OnClickListener() {
                                @Override
                                public void onClick(DialogInterface dialog, int which) {
                                    finish();
                                }
                            });
                    builder.setNegativeButton(R.string.button_dailog_no, null);
                    builder.setCancelable(false);
                    builder.show();
                    builder = null;
                    return true;
                default:
                    break;
            }
        }
        return super.dispatchKeyEvent(event);
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        getMenuInflater().inflate(R.menu.main, menu);
        if (isConnect) {
            menu.findItem(R.id.action_information_ble)
                    .setVisible(true)
                    .setTitle(getString(R.string.text_label_version_ble, mMemeVersion));
        } else {
            menu.findItem(R.id.action_information_ble).setVisible(false);
        }
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        int id = item.getItemId();
        switch (id) {
            case R.id.action_information_ble:
                return true;
            default:
                break;
        }
        return super.onOptionsItemSelected(item);
    }

    protected ServiceConnection mConnection = getConnection();

    protected BroadcastReceiver mReceiver = getReceiver();

    protected abstract ServiceConnection getConnection();

    protected abstract BroadcastReceiver getReceiver();

    protected abstract IntentFilter makeIntentFilter();

    public void scan(View v) {
        scanLeDevice(true);
    }

    protected abstract void scanLeDevice(final boolean enable);

    public void connect(View v) {
        connectLeDevice();
    }

    protected abstract void connectLeDevice();

    public void initialize(View v) {
        initialize();
    }

    private void initialize() {
        isInitial = true;
        // create data
        byte[] data = new byte[20];
        data[0] = DATA_LENGTH;
        data[1] = ADN_CLR_PARAMS;
        data[2] = (byte) (0xFF & 0xFF);
        for (int i = 3; i < 20; i++) {
            data[i] = (byte) (0x00 & 0xFF);
        }
        LogCat.d(TAG, "send data: " + HexDump.toHexString(data));
        // send data
        sendData(DataEncryption.encode(data));

        // set timer
        final Timer timer;
        final long delay = 0L;
        final long interval = 1L;
        final long timeout = TIMEOUT * 1000L;
        timer = new Timer(true);
        timer.scheduleAtFixedRate(new TimerTask() {
            long time = 0L;

            @Override
            public void run() {
                // check a time-out
                if (time > timeout) {
                    isInitial = false;
                    common.setViewInitialize(isInitial);
                    // show toast
                    common.showToast(getString(R.string.msg_failed_initialize));
                    // cancel the timer
                    timer.cancel();
                    timer.purge();
                    return;
                } else {
                    // count up the time
                    time = time + interval;
                }
                // analysis a read data
                if (mReadByte == null) {
                    return;
                }
                if (mReadByte[0] == DATA_LENGTH
                        && mReadByte[1] == AUP_REPORT_RESP) {
                    switch (mReadByte[2]) {
                        case (byte) (0x00 & 0xFF): // ACK
                            LogCat.d(TAG, "success to initialize");
                            isInitial = false;
                            common.setViewInitialize(isInitial);
                            // show toast
                            common.showToast(getString(R.string.msg_success_initialize));
                            // get mode
                            handler.postDelayed(new Runnable() {
                                @Override
                                public void run() {
                                    getMode();
                                }
                            }, 300L);
                            handler.postDelayed(new Runnable() {
                                @Override
                                public void run() {
                                    getParams();
                                }
                            }, 600L);
                            // cancel the timer
                            timer.cancel();
                            timer.purge();
                            break;

                        case (byte) (0xFF & 0xFF): // NACK
                            LogCat.d(TAG, "failed to initialize");
                            isInitial = false;
                            common.setViewInitialize(isInitial);
                            // show toast
                            common.showToast(getString(R.string.msg_failed_initialize));
                            // cancel the timer
                            timer.cancel();
                            timer.purge();
                            break;
                    }
                }
            }
        }, delay, interval);
        common.setViewInitialize(isInitial);
    }

    public void measurement(View v) {
        AlertDialog.Builder builder = new AlertDialog.Builder(activity);
        builder.setMessage(getString(isMeasure ?
                R.string.msg_stop_measurement : R.string.msg_start_measurement));
        builder.setPositiveButton(R.string.button_dialog_ok,
                new DialogInterface.OnClickListener() {
                    @Override
                    public void onClick(DialogInterface dialog, int which) {
                        if (isMeasure) {
                            stop();
                        } else {
                            start();
                        }
                    }
                });
        builder.setNegativeButton(R.string.button_dailog_no, null);
        builder.setCancelable(false);
        builder.show();
        builder = null;
    }

    private void start() {
        LogCat.i(TAG, "start measurement");
        handler.postDelayed(new Runnable() {
            @Override
            public void run() {
                setMode();
            }
        }, 0L);
        handler.postDelayed(new Runnable() {
            @Override
            public void run() {
                setParams();
            }
        }, 500L);
        handler.postDelayed(new Runnable() {
            @Override
            public void run() {
                mFileName = common.createFileNew(mMemeAddress);
        }
        }, 1000L);
        handler.postDelayed(new Runnable() {
            @Override
            public void run() {
                // create data
                byte[] data = new byte[20];
                data[0] = DATA_LENGTH;
                data[1] = ADN_START_STOP_SEND;
                data[2] = (byte) (0x01 & 0xFF);
                for (int i = 3; i < 20; i++) {
                    data[i] = (byte) 0x00;
                }
                LogCat.d(TAG, "start command: " + HexDump.toHexString(data));
                // send data
                sendData(DataEncryption.encode(data));

                // set timer
                final Timer timer;
                final long delay = 0L;
                final long interval = 1L;
                final long timeout = TIMEOUT * 1000L;
                timer = new Timer(true);
                timer.scheduleAtFixedRate(new TimerTask() {
                    long time = 0L;

                    @Override
                    public void run() {
                        // check a time-out
                        if (time > timeout) {
                            common.showToast(getString(R.string.msg_failed_initialize));
                            // cancel the timer
                            timer.cancel();
                            timer.purge();
                            return;
                        } else {
                            // count up the time
                            time = time + interval;
                        }
                        // analysis a read data
                        if (mReadByte == null) {
                            return;
                        }
                        if (mReadByte[0] == DATA_LENGTH
                                && mReadByte[1] == AUP_REPORT_RESP) {
                            switch (mReadByte[2]) {
                                case (byte) (0x00 & 0xFF): // ACK
                                    LogCat.d(TAG, "success to start measurement");
                                    // change a flag
                                    isMeasure = true;
                                    // show some of views
                                    setViewMeasurement();
                                    // cancel the timer
                                    timer.cancel();
                                    timer.purge();
                                    break;

                                case (byte) (0xFF & 0xFF): // NACK
                                    LogCat.d(TAG, "failed to start measurement");
                                    // cancel the timer
                                    timer.cancel();
                                    timer.purge();
                                    break;
                            }
                        }
                    }
                }, delay, interval);
            }
        }, 1500L);
    }

    private void stop() {
        LogCat.i(TAG, "stop measurement");
        handler.postDelayed(new Runnable() {
            @Override
            public void run() {
                // create data
                byte[] data = new byte[20];
                data[0] = DATA_LENGTH;
                data[1] = ADN_START_STOP_SEND;
                data[2] = (byte) (0x00 & 0xFF);
                for (int i = 3; i < 20; i++) {
                    data[i] = (byte) 0x00;
                }
                LogCat.d(TAG, "stop command: " + HexDump.toHexString(data));
                // send data
                sendData(DataEncryption.encode(data));

                // set timer
                final Timer timer;
                final long delay = 0L;
                final long interval = 1L;
                final long timeout = TIMEOUT * 1000L;
                timer = new Timer(true);
                timer.scheduleAtFixedRate(new TimerTask() {
                    long time = 0L;

                    @Override
                    public void run() {
                        // check a time-out
                        if (time > timeout) {
                            common.showToast(getString(R.string.msg_failed_initialize));
                            // cancel the timer
                            timer.cancel();
                            timer.purge();
                            return;
                        } else {
                            // count up the time
                            time = time + interval;
                            // analysis a read data
                            if (mReadByte == null) {
                                return;
                            }
                            if (mReadByte[0] == DATA_LENGTH
                                    && mReadByte[1] == AUP_REPORT_RESP) {
                                switch (mReadByte[2]) {
                                    case (byte) (0x00 & 0xFF): // ACK
                                        LogCat.d(TAG, "success to stop measurement");
                                        // get file path
                                        SharedPreferences prefs = PreferenceManager
                                                .getDefaultSharedPreferences(getApplicationContext());
                                        String path = prefs.getString(
                                                getString(R.string.key_pref_path), "");
                                        // scan file
                                        String[] paths = {
                                                path + mFileName
                                        };
                                        String[] mimeTypes = {
                                                "text/csv"
                                        };
                                        try {
                                            MediaScannerConnection.scanFile(
                                                    getApplicationContext(), paths,
                                                    mimeTypes,
                                                    new OnScanCompletedListener() {
                                                        @Override
                                                        public void onScanCompleted(
                                                                String path, Uri uri) {
                                                        }
                                                    });
                                        } catch (Exception e) {
                                            // ignore
                                        }
                                        // change a flag
                                        isMeasure = false;
                                        // release file name
                                        mFileName = null;
                                        // show some of views
                                        setViewMeasurement();
                                        // cancel the timer
                                        timer.cancel();
                                        timer.purge();
                                        break;

                                    case (byte) (0xFF & 0xFF): // NACK
                                        LogCat.d(TAG, "failed to stop measurement");
                                        // cancel the timer
                                        timer.cancel();
                                        timer.purge();
                                        break;
                                }
                            }
                        }
                    }
                }, delay, interval);
            }
        }, 0L);
    }

    public void marking(View v) {
        marking();
    }

    private void marking() {
        isMarking = true;
        common.setViewMarking(isMarking);

        // set timer
        final Timer timer;
        final long delay = 0L;
        final long interval = 1L;
        final long timeout = 10L;
        timer = new Timer(true);
        timer.scheduleAtFixedRate(new TimerTask() {
            long time = 0L;

            @Override
            public void run() {
                // check a time-out
                if (time > timeout) {
                    isMarking = false;
                    common.setViewMarking(isMarking);
                    // cancel the timer
                    timer.cancel();
                    timer.purge();
                    return;
                } else {
                    // count up the time
                    time = time + interval;
                }
            }
        }, delay, interval);
    }

    protected void getBleVersion() {
        LogCat.i(TAG, "check version of the BLE device");
        // create data
        byte[] data = new byte[20];
        data[0] = DATA_LENGTH;
        data[1] = ADN_GET_DEV_INFO;
        data[2] = (byte) (0x00 & 0xFF);
        for (int i = 3; i < 20; i++) {
            data[i] = (byte) 0x00;
        }
        LogCat.d(TAG, "send data: " + HexDump.toHexString(data));
        // send data
        sendData(DataEncryption.encode(data));

        // set timer
        final Timer timer;
        final long delay = 0L;
        final long interval = 1L;
        final long timeout = TIMEOUT * 1000L;
        timer = new Timer(true);
        timer.scheduleAtFixedRate(new TimerTask() {
            long time = 0L;

            @Override
            public void run() {
                // check a time-out
                if (time > timeout) {
                    common.showToast(getString(R.string.msg_failed_get_information));
                    // cancel the timer
                    timer.cancel();
                    timer.purge();
                    return;
                } else {
                    // count up the time
                    time = time + interval;
                }
                // analysis a read data
                if (mReadByte == null) {
                    return;
                }
                if (mReadByte[0] != DATA_LENGTH
                        || mReadByte[1] != AUP_REPORT_DEV_INFO) {
                    return;
                }
                byte[] info = new byte[5];
                for (int i = 0; i < 5; i++) {
                    info[i] = mReadByte[i + 2];
                }

                StringBuffer stringBuffer = new StringBuffer();
                stringBuffer.append(String.format("%X", ByteBuffer.wrap(new byte[]{
                        info[0], info[1]
                }).order(ByteOrder.LITTLE_ENDIAN).getShort()));
                stringBuffer.append("-" + info[4]);
                stringBuffer.append("." + info[3]);
                stringBuffer.append("." + info[2]);
                String deviceVersion = stringBuffer.toString();
                LogCat.d(TAG, "the BLE device version is " + deviceVersion);

                mMemeVersion = deviceVersion;
                invalidateOptionsMenu();
                // cancel the timer
                timer.cancel();
                timer.purge();
            }
        }, delay, interval);
    }

    protected void getMode() {
        LogCat.i(TAG, "get measurement mode");
        // create data
        byte[] data = new byte[20];
        data[0] = DATA_LENGTH;
        data[1] = ADN_GET_MODE;
        data[2] = (byte) (0x00 & 0xFF);
        for (int i = 3; i < 20; i++) {
            data[i] = (byte) 0x00;
        }
        LogCat.d(TAG, "send data: " + HexDump.toHexString(data));
        // send data
        sendData(DataEncryption.encode(data));

        // set timer
        final Timer timer;
        final long delay = 0L;
        final long interval = 1L;
        final long timeout = TIMEOUT * 1000L;
        timer = new Timer(true);
        timer.scheduleAtFixedRate(new TimerTask() {
            long time = 0L;

            @Override
            public void run() {
                // check a time-out
                if (time > timeout) {
                    common.showToast(getString(R.string.msg_failed_get_mode));
                    // cancel the timer
                    timer.cancel();
                    timer.purge();
                    return;
                } else {
                    // count up the time
                    time = time + interval;
                }
                // analysis a read data
                if (mReadByte == null) {
                    return;
                }
                if (mReadByte[0] != DATA_LENGTH
                        || mReadByte[1] != AUP_REPORT_MODE) {
                    return;
                }
                final byte item_mode = (byte) (mReadByte[4]);
                final byte item_quality = (byte) (mReadByte[5]);
                LogCat.d(TAG, "mode position: " + item_mode);
                LogCat.d(TAG, "quality position: " + item_quality);
                common.setItemMode(item_mode, item_quality);
                // cancel the timer
                timer.cancel();
                timer.purge();
            }
        }, delay, interval);
    }

    protected void setMode() {
        LogCat.i(TAG, "set measurement mode");
        // create data
        byte[] bytes = common.getItemMode();
        byte item_mode = (byte) ((bytes[0] + 0x01) & 0xFF);
        byte item_quality = (byte) ((bytes[1] + 0x01) & 0xFF);
        mQuality = item_quality;
        byte[] data = new byte[20];
        data[0] = DATA_LENGTH;
        data[1] = ADN_SET_MODE;
        for (int i = 2; i < 20; i++) {
            data[i] = (byte) 0x00;
        }
        data[4] = (byte) (item_mode & 0xFF);
        data[5] = (byte) (item_quality & 0xFF);
        LogCat.d(TAG, "send data: " + HexDump.toHexString(data));
        // send data
        sendData(DataEncryption.encode(data));

        // set timer
        final Timer timer;
        final long delay = 0L;
        final long interval = 1L;
        final long timeout = TIMEOUT * 1000L;
        timer = new Timer(true);
        timer.scheduleAtFixedRate(new TimerTask() {
            long time = 0L;

            @Override
            public void run() {
                // check a time-out
                if (time > timeout) {
                    common.showToast(getString(R.string.msg_failed_set_mode));
                    // cancel the timer
                    timer.cancel();
                    timer.purge();
                    return;
                } else {
                    // count up the time
                    time = time + interval;
                }
                // analysis a read data
                if (mReadByte == null) {
                    return;
                }
                if (mReadByte[0] != DATA_LENGTH
                        || mReadByte[1] != AUP_REPORT_RESP) {
                    return;
                }
                switch (mReadByte[2]) {
                    case (byte) (0x00 & 0xFF): // ACK
                        LogCat.d(TAG, "success to set mode");
                        break;

                    case (byte) (0xFF & 0xFF): // NACK
                        LogCat.d(TAG, "failed to set mode");
                        // show some of views
                        // setViewStateDevice(true);
                        // setViewStateMeasurement(false);
                        // show toast
                        common.showToast(getString(R.string.msg_failed_set_mode));
                        break;
                }
                // cancel the timer
                timer.cancel();
                timer.purge();
            }
        }, delay, interval);

    }

    protected void getParams() {
        LogCat.i(TAG, "get range parameters of the sensor");
        // create data
        byte[] data = new byte[20];
        data[0] = DATA_LENGTH;
        data[1] = ADN_GET_6AXIS_PARAMS;
        data[2] = (byte) (0x00 & 0xFF);
        for (int i = 3; i < 20; i++) {
            data[i] = (byte) 0x00;
        }
        LogCat.d(TAG, "send data: " + HexDump.toHexString(data));
        // send data
        sendData(DataEncryption.encode(data));

        // set timer
        final Timer timer;
        final long delay = 0L;
        final long interval = 1L;
        final long timeout = TIMEOUT * 1000L;
        timer = new Timer(true);
        timer.scheduleAtFixedRate(new TimerTask() {
            long time = 0L;

            @Override
            public void run() {
                // check a time-out
                if (time > timeout) {
                    common.showToast(getString(R.string.msg_failed_get_params));
                    // cancel the timer
                    timer.cancel();
                    timer.purge();
                    return;
                } else {
                    // count up the time
                    time = time + interval;
                }
                // analysis a read data
                if (mReadByte == null) {
                    return;
                }
                if (mReadByte[0] != DATA_LENGTH
                        || mReadByte[1] != AUP_REPORT_6AXIS_PARAMS) {
                    return;
                }
                final byte item_acc = mReadByte[2];
                final byte item_ang = mReadByte[3];
                LogCat.d(TAG, "acc range parameter position: " + item_acc);
                LogCat.d(TAG, "gyro range parameter position: " + item_ang);
                common.setItemParams(item_acc, item_ang);
                // cancel the timer
                timer.cancel();
                timer.purge();
            }
        }, delay, interval);
    }

    protected void setParams() {
        LogCat.i(TAG, "set range parameters of the sensor");
        // create data
        byte[] bytes = null;
        bytes = common.getItemParams();
        byte item_acc = (byte) (bytes[0] & 0xFF);
        byte item_ang = (byte) (bytes[1] & 0xFF);
        // send "set measurement mode" command
        bytes = common.getItemMode();
        byte item_mode = (byte) ((bytes[0] + 0x01) & 0xFF);
        // byte item_quality = (byte) ((bytes[1] + 0x01) & 0xFF);
        if (item_mode == 3) {
            item_ang = (byte) (0x03 & 0xFF);
            common.setItemParams(item_acc, item_ang);
        }

        byte[] data = new byte[20];
        data[0] = DATA_LENGTH;
        data[1] = ADN_SET_6AXIS_PARAMS;
        for (int i = 2; i < 20; i++) {
            data[i] = (byte) 0x00;
        }
        data[2] = (byte) (item_acc & 0xFF);
        data[3] = (byte) (item_ang & 0xFF);
        LogCat.d(TAG, "send data: " + HexDump.toHexString(data));
        // send data
        sendData(DataEncryption.encode(data));

        // set timer
        final Timer timer;
        final long delay = 0L;
        final long interval = 1L;
        final long timeout = TIMEOUT * 1000L;
        timer = new Timer(true);
        timer.scheduleAtFixedRate(new TimerTask() {
            long time = 0L;

            @Override
            public void run() {
                // check a time-out
                if (time > timeout) {
                    common.showToast(getString(R.string.msg_failed_set_mode));
                    // cancel the timer
                    timer.cancel();
                    timer.purge();
                    return;
                } else {
                    // count up the time
                    time = time + interval;
                }
                // analysis a read data
                if (mReadByte == null) {
                    return;
                }
                if (mReadByte[0] != DATA_LENGTH
                        || mReadByte[1] != AUP_REPORT_RESP) {
                    return;
                }
                switch (mReadByte[2]) {
                    case (byte) (0x00 & 0xFF): // ACK
                        LogCat.d(TAG, "success to set range parameters");
                        break;

                    case (byte) (0xFF & 0xFF): // NACK
                        LogCat.d(TAG, "failed to set range parameters");
                        // show some of views
                        // setViewStateDevice(true);
                        // setViewStateMeasurement(false);
                        // show toast
                        common.showToast(getString(R.string.msg_failed_set_params));
                        break;
                }
                // cancel the timer
                timer.cancel();
                timer.purge();
            }
        }, delay, interval);
    }

    protected abstract void setViewDefault();

    protected abstract void setViewScan();

    protected abstract void setViewMeasurement();

    protected abstract void sendData(byte[] data);

    protected void analysisData(byte[] data) {
        // analysis the reception data
        short head = 0;
        short count = 0;
        short level = 0;
        int[] values = null;
        try {
            switch (data[1]) {
                case AUP_REPORT_ACADEMIA1:
                    // head data
                    head = ByteBuffer.wrap(new byte[]{
                            data[2], data[3]
                    })
                            .order(ByteOrder.LITTLE_ENDIAN).getShort();
                    count = (short) (head & 0x0FFF);
                    level = (short) ((head & 0xF000) >> 12);
                    // body data
                    values = new int[11];
                    for (int i = 4; i < 18; i++) {
                        if (i % 2 == 0) {
                            byte[] bytes = {
                                    data[i], data[i + 1]
                            };
                            values[i / 2 - 2] = ByteBuffer.wrap(bytes)
                                    .order(ByteOrder.LITTLE_ENDIAN).getShort();
                        }
                    }
                    values[7] = (short) (values[3] - values[4]);
                    values[8] = (short) (values[5] - values[6]);
                    values[9] = (short) (0 - (values[3] + values[4]) / 2);
                    values[10] = (short) (0 - (values[5] + values[6]) / 2);

                    if ((count % GrasphSkipCount) == 0) {
                        if (EogGraphEnableFlag == true) {
                            graphEOG.setGraphEOG((count/GrasphSkipCount), (short) values[7], (short) values[9]);
                        }
                        if (AccGraphEnableFlag == true) {
                            graphAcc.setGraphAcc((count / GrasphSkipCount), (short) values[0], (short) values[1], (short) values[2]);
                        }
                    }

                    break;

                case AUP_REPORT_ACADEMIA2:
                    // head data
                    head = ByteBuffer.wrap(new byte[]{
                            data[2], data[3]
                    })
                            .order(ByteOrder.LITTLE_ENDIAN).getShort();
                    count = (short) (head & 0x0FFF);
                    level = (short) ((head & 0xF000) >> 12);
                    // body data
                    values = new int[10];
                    for (int i = 4; i < 20; i++) {
                        if (i % 2 == 0) {
                            byte[] bytes = {
                                    data[i], data[i + 1]
                            };
                            values[i / 2 - 2] = ByteBuffer.wrap(bytes)
                                    .order(ByteOrder.LITTLE_ENDIAN).getShort();
                        }
                    }
                    values[8] = (short) (values[6] - values[7]);
                    values[9] = (short) (0 - (values[6] + values[7]) / 2);

                    if ((count % GrasphSkipCount) == 0) {

                        if (EogGraphEnableFlag == true) {
                            graphEOG.setGraphEOG((count / GrasphSkipCount), (short) values[8], (short) values[9]);
                        }

                        if (AccGraphEnableFlag == true) {
                            graphAcc.setGraphAcc((count / GrasphSkipCount), (short) values[0], (short) values[1], (short) values[2]);
                        }

                        if (GyroGraphEnableFlag == true) {
                            graphGyro.setGraphGyro((count / GrasphSkipCount), (short) values[3], (short) values[4], (short) values[5]);
                        }
                    }
                    break;

                case AUP_REPORT_ACADEMIA3:
                    // head data
                    head = ByteBuffer.wrap(new byte[]{
                            data[2], data[3]
                    })
                            .order(ByteOrder.LITTLE_ENDIAN).getShort();
                    count = (short) (head & 0x0FFF);
                    level = (short) ((head & 0xF000) >> 12);
                    // body data
                    values = new int[4];
                    for (int i = 4; i < 20; i++) {
                        if (i % 4 == 0) {
                            byte[] bytes = {
                                    data[i], data[i + 1], data[i + 2],
                                    data[i + 3]
                            };
                            values[i / 4 - 1] = ByteBuffer.wrap(bytes)
                                    .order(ByteOrder.LITTLE_ENDIAN).getInt();
                        }
                    }

                    break;

                default:
                    mReadByte = data;
                    break;
            }
        } catch (Exception e) {
            stop();
        }

        if (values == null)
            return;

        // create the recording data
        long deff = 0;
        if (mPrevCount < 0) {
            deff = 0;
            mPrevTime = System.currentTimeMillis();
        } else {
            if (mPrevCount < (long) count) {
                deff = (long) count - mPrevCount;
            } else if (mPrevCount > (long) count) {
                deff = 0x1000 - mPrevCount + (long) count;
            }
        }
        mPrevCount = count;
        mPrevTime += deff * 10 * mQuality;

        if (deff == 0) {
            mTotalCount += deff + 1;
            mErrorCount += deff;
        } else {
            mTotalCount += deff;
            if (deff - 1 > 0) {
                mErrorCount += deff - 1;
                LogCat.e(TAG, "increment error count !");
            }
        }

        StringBuffer stringBuffer = new StringBuffer();

        stringBuffer.append(isMarking ? "X" : "");
        stringBuffer.append(",");

        stringBuffer.append(String.format("%d", mTotalCount));

        SimpleDateFormat simpleDateFormat =
                new SimpleDateFormat("yyyy/MM/dd HH:mm:ss.SS", Locale.getDefault());
        simpleDateFormat.setTimeZone(TimeZone.getTimeZone("GMT"));
        stringBuffer.append(",");

        stringBuffer.append(simpleDateFormat.format(new Date(mPrevTime)));
        for (int i = 0; i < values.length; i++) {
            stringBuffer.append(",");
            stringBuffer.append(String.format("%d", values[i]));
        }
        values = null;

        // write the recording data
        SharedPreferences prefs = PreferenceManager
                .getDefaultSharedPreferences(getApplicationContext());
        String path = prefs.getString(getString(R.string.key_pref_path), "");
        common.writeFile(path, mFileName, stringBuffer.toString());
        stringBuffer = null;
        // update view
        common.setViewBattery(level);

        common.setViewError(mTotalCount, mErrorCount);
        mNumTotalLast = mTotalCount;
        LogCat.d(TAG,
                "total:" + mTotalCount + "(" + mPrevCount + ")"
                        + " date:" + simpleDateFormat.format(new Date(mPrevTime)));

        data = null;
    }

    public void EogGraphEnabled(View v) {
        if (EogGraphEnableFlag == true) {
            EogGraphEnableFlag = false;
            // set button
            Button button;
            button = (Button) findViewById(R.id.button_eog_graph);
            button.setText(R.string.eog_graph_disable);
        }
        else {
            EogGraphEnableFlag = true;
            // set button
            Button button;
            button = (Button) findViewById(R.id.button_eog_graph);
            button.setText(R.string.eog_graph_enable);
        }
    }

    public void AccGraphEnabled(View v) {
        if (AccGraphEnableFlag == true) {
            AccGraphEnableFlag = false;
            // set button
            Button button;
            button = (Button) findViewById(R.id.button_acc_graph);
            button.setText(R.string.acc_graph_disable);
        }
        else {
            AccGraphEnableFlag = true;
            // set button
            Button button;
            button = (Button) findViewById(R.id.button_acc_graph);
            button.setText(R.string.acc_graph_enable);
        }
    }

    public void GyroGraphEnabled(View v) {
        if (GyroGraphEnableFlag == true) {
            GyroGraphEnableFlag = false;
            // set button
            Button button;
            button = (Button) findViewById(R.id.button_gyro_graph);
            button.setText(R.string.gyro_graph_disable);
        }
        else {
            GyroGraphEnableFlag = true;
            // set button
            Button button;
            button = (Button) findViewById(R.id.button_gyro_graph);
            button.setText(R.string.gyro_graph_enable);
        }
    }
}
