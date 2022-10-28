
package com.jins_jp.meme.academic;

import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.ServiceConnection;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbManager;
import android.os.Bundle;
import android.os.IBinder;
import android.support.v7.widget.Toolbar;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.RatingBar;
import android.widget.Spinner;
import android.widget.TextView;

import com.jins.meme.academic.util.DataEncryption;
import com.jins.meme.academic.util.HexDump;
import com.jins.meme.academic.util.LogCat;
import com.jins_jp.meme.academic.usb.UsbHostService;

import java.io.UnsupportedEncodingException;
import java.math.BigDecimal;
import java.util.HashMap;
import java.util.Iterator;
import java.util.Timer;
import java.util.TimerTask;

public class MainUsbActivity extends MainActivity {

    private UsbHostService mUsbHostService;
    private UsbManager mUsbManager;
    private UsbDevice mUsbDevice;
    // management and control of USB device
    private final int ID_VENDOR = 4292;
    private final int ID_PRODUCT = 60000;
    // for custom broadcast receiver
    private static final String ACTION_USB_PERMISSION = "com.jins_jp.meme.academic.android.USB_PERMISSION";
    private PendingIntent mPermissionIntent;
    public static boolean isRequest = true;
    public static boolean isOpening = false;
    private String mUsbDeviceName = null;
    private String mUsbDeviceVersion = null;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        // set view to this activity
        setContentView(R.layout.activity_main_usb);

        // Graph
        graphEOG.makeChart();
        graphAcc.makeChart();
        graphGyro.makeChart();

        // set the tool-bar to this activity
//        Toolbar mToolbar = (Toolbar) findViewById(R.id.header);
//        mToolbar.setTitle(R.string.app_title);
//        setSupportActionBar(mToolbar);

        // set view
        setViewDefault();

        common.SetMode(common.mode_usb);
    }

    @Override
    protected void onStart() {
        super.onStart();
        // get USB Manager
        mUsbManager = (UsbManager) getSystemService(Context.USB_SERVICE);
        // set pending intent
        mPermissionIntent = PendingIntent.getBroadcast(this, 0, new Intent(ACTION_USB_PERMISSION),
                PendingIntent.FLAG_UPDATE_CURRENT);
        // bind service
        Intent intent = new Intent(getApplicationContext(), UsbHostService.class);
        bindService(intent, mConnection, Context.BIND_AUTO_CREATE);
        LogCat.d(TAG, "bind Service");
    }

    @Override
    protected void onStop() {
        super.onStop();
        // unbind service
        unbindService(mConnection);
        LogCat.d(TAG, "unbind Service");
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        Intent intent = new Intent();
        intent.setClass(getApplicationContext(), UsbHostService.class);
        if (isOpening) {
            stopService(intent);
            mUsbHostService.closePort();
        }
        // release pending intent
        if (mPermissionIntent != null) {
            mPermissionIntent.cancel();
        }
    }

    private UsbDevice getDevice() {
        HashMap<String, UsbDevice> deviceList = mUsbManager.getDeviceList();
        Iterator<UsbDevice> deviceIterator = deviceList.values().iterator();
        while (deviceIterator.hasNext()) {
            UsbDevice device = deviceIterator.next();
            if (device != null) {
                if (device.getVendorId() == ID_VENDOR && device.getProductId() == ID_PRODUCT) {
                    return device;
                } else {
                    return null;
                }
            }
        }
        return null;
    }

    @Override
    protected ServiceConnection getConnection() {
        mConnection = new ServiceConnection() {

            @Override
            public void onServiceConnected(ComponentName componentName, IBinder service) {
                mUsbHostService = ((UsbHostService.LocalBinder) service).getService();
                if (!mUsbHostService.initialize()) {
                    LogCat.e(TAG, "Unable to initialize Bluetooth");
                    finish();
                }
                LogCat.i(TAG, "initialized USB");
            }

            @Override
            public void onServiceDisconnected(ComponentName componentName) {
                mUsbHostService = null;
            }

        };
        return mConnection;
    }

    @Override
    protected BroadcastReceiver getReceiver() {
        mReceiver = new BroadcastReceiver() {

            public void onReceive(Context context, Intent intent) {
                // get action from intent
                String action = intent.getAction();
                LogCat.d(TAG, "received " + action);
                // get device from intent
                UsbDevice device = (UsbDevice) intent.getParcelableExtra(UsbManager.EXTRA_DEVICE);
                if (UsbManager.ACTION_USB_DEVICE_ATTACHED.equals(action)) {
                    LogCat.d(TAG, "attacehed " + device);

                    common.showToast("ACTION_USB_DEVICE_ATTACHED");
                    mUsbDevice = getDevice();
                    if (mUsbDevice != null && device.equals(mUsbDevice)) {
                        if (!mUsbManager.hasPermission(mUsbDevice)) {
                            handler.postDelayed(new Runnable() {
                                @Override
                                public void run() {
                                    if (mUsbManager == null) {
                                        return;
                                    }
                                    mUsbManager.requestPermission(mUsbDevice, mPermissionIntent);
                                }
                            }, 300L);
                        }
                    }

                } else if (UsbManager.ACTION_USB_DEVICE_DETACHED.equals(action)) {
                    LogCat.d(TAG, "detacehed " + device);

                    common.showToast("ACTION_USB_DEVICE_DETACHED");
                    mUsbDevice = null;
                    finish();

                } else if (ACTION_USB_PERMISSION.equals(action)) {
                    synchronized (this) {
                        if (intent.getBooleanExtra(UsbManager.EXTRA_PERMISSION_GRANTED, false)) {
                            LogCat.d(TAG, "permission agreed for " + device);
                            handler.post(new Runnable() {
                                @Override
                                public void run() {
                                    Button button = (Button) findViewById(R.id.button_open_usb);
                                    button.setEnabled(true);
                                }
                            });
                        } else {
                            LogCat.d(TAG, "permission denied for " + device);
                            mUsbDevice = null;
                            finish();
                        }
                    }

                } else if (UsbHostService.ACTION_PORT_OPEN.equals(action)) {
                    LogCat.d(TAG, "ACTION_PORT_OPEN");

                    handler.postDelayed(new Runnable() {
                        @Override
                        public void run() {
                            checkUsbState();
                        }
                    }, 100L);
                    handler.postDelayed(new Runnable() {
                        @Override
                        public void run() {
                            getUsbName();
                        }
                    }, 700L);
                    handler.postDelayed(new Runnable() {
                        @Override
                        public void run() {
                            getUsbVersion();
                        }
                    }, 1400L);
                    handler.postDelayed(new Runnable() {
                        @Override
                        public void run() {
                            startService(new Intent(getApplicationContext(), UsbHostService.class));
                            isOpening = true;
                            setViewOpen();
                            common.setViewConnect(isConnect,mMemeVersion);
                        }
                    }, 2000L);

                } else if (UsbHostService.ACTION_PORT_CLOSE.equals(action)) {
                    LogCat.d(TAG, "ACTION_PORT_CLOSE");

                    stopService(new Intent(getApplicationContext(), UsbHostService.class));
                    isOpening = false;
                    setViewDefault();

                } else if (UsbHostService.ACTION_DATA_AVAILABLE.equals(action)) {
                    LogCat.d(TAG, "ACTION_DATA_AVAILABLE");

                    byte[] data = intent.getByteArrayExtra(UsbHostService.EXTRA_DATA);
                    LogCat.d(TAG, "decode data: " + HexDump.toHexString(data));

                    analysisData(data);
                }
            }

        };
        return mReceiver;
    }

    @Override
    protected IntentFilter makeIntentFilter() {
        // set intent filter for custom　broadcast receiver
        IntentFilter intentFilter = new IntentFilter();
        intentFilter.addAction(ACTION_USB_PERMISSION);
        intentFilter.addAction(UsbManager.ACTION_USB_DEVICE_ATTACHED);
        intentFilter.addAction(UsbManager.ACTION_USB_DEVICE_DETACHED);
        intentFilter.addAction(UsbHostService.ACTION_PORT_OPEN);
        intentFilter.addAction(UsbHostService.ACTION_PORT_CLOSE);
        intentFilter.addAction(UsbHostService.ACTION_DATA_AVAILABLE);
        return intentFilter;
    }

    public void open(View v) {
        if (isOpening) {
            mUsbHostService.closePort();
        } else {
            mUsbHostService.openPort(mUsbDevice);
        }
        findViewById(R.id.button_open_usb).setEnabled(false);

    }

    @Override
    protected void scanLeDevice(final boolean enable) {
        LogCat.d(TAG, "scan the device");

        isScannin = true;
        handler.postDelayed(new Runnable() {
            @Override
            public void run() {
                // create data
                byte[] data = new byte[3];
                data[0] = (byte) (0x03 & 0xFF);
                data[1] = (byte) (0x4e & 0xFF);
                data[2] = (byte) (0x30 & 0xFF);
                LogCat.d(TAG, "send data: " + HexDump.toHexString(data));
                // send data
                sendData(data);

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
                        if (!isOpening) {
                            isScannin = false;
                            // cancel the timer
                            timer.cancel();
                            timer.purge();
                        }
                        // check a time-out
                        if (time > timeout) {
                            isScannin = false;
                            setViewScan();
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
                        switch (mReadByte[0]) {
                            case (byte) (0x03 & 0xFF):
                                if (mReadByte[1] != (byte) (0x4E & 0xFF)
                                        || mReadByte[2] != (byte) (0x32 & 0xFF)) {
                                    return;
                                }
                                // if finished device scan,
                                LogCat.d(TAG, "found device list size: " + mDeviceSet.size());

                                isScannin = false;
                                setViewScan();
                                // cancel the timer
                                timer.cancel();
                                timer.purge();
                                break;

                            case (byte) (0x0F & 0xFF):
                                if (mReadByte[1] != (byte) (0x4E & 0xFF)
                                        || mReadByte[2] != (byte) (0x31 & 0xFF)) {
                                    return;
                                }
                                if (mReadByte.length == 15) {
                                    // add device address
                                    byte[] item = new byte[12];
                                    for (int i = 0; i < 12; i++) {
                                        try {
                                            item[i] = mReadByte[i + 3];
                                        } catch (ArrayIndexOutOfBoundsException e) {
                                            // ignore
                                        }
                                    }
                                    StringBuffer address = new StringBuffer();
                                    try {
                                        address.append(new String(item, "UTF-8"));
                                    } catch (UnsupportedEncodingException e) {
                                        e.printStackTrace();
                                    }
                                    address.insert(10, ":");
                                    address.insert(8, ":");
                                    address.insert(6, ":");
                                    address.insert(4, ":");
                                    address.insert(2, ":");

                                    mDeviceSet.add(address.toString());
                                    LogCat.d(TAG, "addappendress of the found BLE device: "
                                            + address.toString());
                                }
                                break;
                        }
                        mReadByte = null;
                    }
                }, delay, interval);
            }
        }, 500L);
        setViewScan();
    }

    @Override
    protected void connectLeDevice() {
        findViewById(R.id.button_connect_ble).setEnabled(false);
        if (isConnect) {
            mMemeAddress = null;
            mMemeVersion = null;
            // send "disconnect BLE" command
            disconnect();
        } else {
            Spinner spinner = (Spinner) findViewById(R.id.spinner_device);
            String address = (String) spinner.getSelectedItem();
            mMemeAddress = address;
            DataEncryption.setKey(address);
            // send "connect BLE" command
            connect(address.replace(":", ""));
        }
    }

    private void connect(String address) {
        LogCat.i(TAG, "connect the BLE device " + address);

        byte[] item = new byte[12];
        try {
            item = address.getBytes("US-ASCII");
        } catch (UnsupportedEncodingException e) {
            e.printStackTrace();
        }
        // create data
        byte[] data = new byte[15];
        data[0] = (byte) (0x0F & 0xFF);
        data[1] = (byte) (0x4E & 0xFF);
        data[2] = (byte) (0x40 & 0xFF);
        for (int i = 0; i < 12; i++) {
            data[i + 3] = item[i];
        }
        LogCat.d(TAG, "send command: " + HexDump.toHexString(data));
        // send data
        sendData(data);

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
                if (!isOpening) {
                    // cancel the timer
                    timer.cancel();
                    timer.purge();
                }
                // check a time-out
                if (time > timeout) {
                    common.showToast(getString(R.string.msg_failed_connection));
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
                if (mReadByte[0] == (byte) (0x03 & 0xFF)
                        && mReadByte[1] == (byte) (0x4E & 0xFF)) {
                    switch (mReadByte[2]) {
                        case (byte) (0x41 & 0xFF):
                            LogCat.d(TAG, "success to connect");
                            // change a flag
                            isConnect = true;
                            common.setViewConnect(isConnect,mMemeVersion);
                            // get device information
                            handler.postDelayed(new Runnable() {
                                @Override
                                public void run() {
                                    getBleVersion();
                                }
                            }, 0L);
                            // get mode
                            handler.postDelayed(new Runnable() {
                                @Override
                                public void run() {
                                    getMode();
                                }
                            }, 300L);
                            // get parameters
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

                        case (byte) (0x42 & 0xFF):
                            LogCat.d(TAG, "failed to connect");
                            mMemeAddress = null;
                            // cancel the timer
                            timer.cancel();
                            timer.purge();
                            break;
                    }
                }
            }
        }, delay, interval);
    }

    private void disconnect() {
        LogCat.i(TAG, "disconnct the device");
        // create data
        byte[] data = new byte[3];
        data[0] = (byte) (0x03 & 0xFF);
        data[1] = (byte) (0x4e & 0xFF);
        data[2] = (byte) (0x50 & 0xFF);
        LogCat.d(TAG, "disconnect command: " + HexDump.toHexString(data));
        // send data
        sendData(data);

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
                    common.showToast(getString(R.string.msg_failed_disconnection));
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
                if (mReadByte[0] == (byte) (0x03 & 0xFF)
                        && mReadByte[1] == (byte) (0x4E & 0xFF)) {
                    switch (mReadByte[2]) {
                        case (byte) (0x51 & 0xFF):
                            LogCat.d(TAG, "success to disconnect");
                            // change a flag
                            isConnect = false;
                            common.setViewConnect(isConnect,mMemeVersion);
                            // cancel the timer
                            timer.cancel();
                            timer.purge();
                            break;

                        case (byte) (0x52 & 0xFF):
                            LogCat.d(TAG, "failed to disconnect");
                            // cancel the timer
                            timer.cancel();
                            timer.purge();
                            break;
                    }
                }
            }
        }, delay, interval);
    }

    private void getUsbName() {
        LogCat.i(TAG, "check name of the USB device");
        // create data
        byte[] data = new byte[3];
        data[0] = (byte) (0x03 & 0xFF);
        data[1] = (byte) (0x4e & 0xFF);
        data[2] = (byte) (0x10 & 0xFF);
        LogCat.d(TAG, "send data: " + HexDump.toHexString(data));
        // send data
        sendData(data);

        // set timer
        final Timer timer;
        final long delay = 0L;
        final long interval = 1L;
        final long timeout = TIMEOUT * 100L;
        timer = new Timer(true);
        timer.scheduleAtFixedRate(new TimerTask() {
            long time = 0L;

            @Override
            public void run() {
                if (!isOpening) {
                    // cancel the timer
                    timer.cancel();
                    timer.purge();
                }
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
                if (mReadByte[0] != (byte) (0x0A & 0xFF)
                        || mReadByte[1] != (byte) (0x4E & 0xFF)
                        || mReadByte[2] != (byte) (0x11 & 0xFF)) {
                    return;
                }
                byte[] info = new byte[7];
                for (int i = 0; i < 7; i++) {
                    info[i] = mReadByte[i + 3];
                }
                String name = new String(info);
                LogCat.d(TAG, "the USB device name is " + name);

                mUsbDeviceName = name;
                // cancel the timer
                timer.cancel();
                timer.purge();
            }
        }, delay, interval);
    }

    private void getUsbVersion() {
        LogCat.i(TAG, "check version of the USB device");
        // create data
        byte[] data = new byte[3];
        data[0] = (byte) (0x03 & 0xFF);
        data[1] = (byte) (0x4e & 0xFF);
        data[2] = (byte) (0x20 & 0xFF);
        LogCat.d(TAG, "send data: " + HexDump.toHexString(data));
        // send data
        sendData(data);

        // set timer
        final Timer timer;
        final long delay = 0L;
        final long interval = 1L;
        final long timeout = TIMEOUT * 100L;
        timer = new Timer(true);
        timer.scheduleAtFixedRate(new TimerTask() {
            long time = 0L;

            @Override
            public void run() {
                if (!isOpening) {
                    // cancel the timer
                    timer.cancel();
                    timer.purge();
                }
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
                if (mReadByte[0] != (byte) (0x0B & 0xFF)
                        || mReadByte[1] != (byte) (0x4E & 0xFF)
                        || mReadByte[2] != (byte) (0x21 & 0xFF)) {
                    return;
                }
                byte[] info = new byte[8];
                for (int i = 0; i < 8; i++) {
                    info[i] = mReadByte[i + 3];
                }
                String version = new String(info);
                LogCat.d(TAG, "the USB device version is " + version);

                mUsbDeviceVersion = version;
                // cancel the timer
                timer.cancel();
                timer.purge();
            }
        }, delay, interval);
    }

    private void checkUsbState() {
        LogCat.i(TAG, "check state of the USB device");
        // send "disconnect BLE" command
        byte[] data = new byte[3];
        data[0] = (byte) (0x03 & 0xFF);
        data[1] = (byte) (0x4e & 0xFF);
        data[2] = (byte) (0x50 & 0xFF);
        LogCat.d(TAG, "send data: " + HexDump.toHexString(data));
        // send data
        sendData(data);

        // set timer
        final Timer timer;
        final long delay = 0L;
        final long interval = 1L;
        final long timeout = TIMEOUT * 100L;
        timer = new Timer(true);
        timer.scheduleAtFixedRate(new TimerTask() {
            long time = 0L;

            @Override
            public void run() {
                if (!isOpening) {
                    // cancel the timer
                    timer.cancel();
                    timer.purge();
                }
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
                if (mReadByte[0] != (byte) (0x03 & 0xFF)
                        || mReadByte[1] != (byte) (0x4E & 0xFF)) {
                    return;
                }
                switch (mReadByte[2]) {
                    case (byte) (0x51 & 0xFF):
                        LogCat.d(TAG, "success to check disconnect state");
                        // cancel the timer
                        timer.cancel();
                        timer.purge();
                        break;
                    case (byte) (0x52 & 0xFF):
                        LogCat.d(TAG, "failed to check disconnect state");
                        // cancel the timer
                        timer.cancel();
                        timer.purge();
                        mUsbHostService.closePort();
                        break;
                }
            }
        }, delay, interval);
    }

    @Override
    protected void setViewDefault() {
        isRequest = true;
        isOpening = false;
        isConnect = false;
        isMeasure = false;

        handler.post(new Runnable() {
            @Override
            public void run() {
                // set view enable
                int[] resource = {//
                        R.id.button_open_usb,
                        R.id.button_scan,
                        R.id.spinner_device,
                        R.id.button_connect_ble,
                        R.id.spinner_select_mode,
                        R.id.spinner_select_quality,
                        R.id.spinner_set_acceleration,
                        R.id.spinner_set_gyroscope,
                        R.id.button_initialize,
                        R.id.button_measurement,
                        R.id.button_marking,
                        R.id.button_eog_graph,
                        R.id.graph_VvVh,
                        R.id.button_acc_graph,
                        R.id.graph_acc,
                        R.id.button_gyro_graph,
                        R.id.graph_gyro,
                };
                for (int i = 0; i < resource.length; i++) {
                    findViewById(resource[i]).setEnabled(false);
                }
                // set textview
                TextView textView;
                textView = (TextView) findViewById(R.id.connect_state_usb);
                textView.setText(R.string.text_state_close);
                textView = (TextView) findViewById(R.id.connect_state_ble);
                textView.setText(R.string.text_state_disconnect);
                textView = null;
                // set spinner
                Spinner spinner = (Spinner) findViewById(R.id.spinner_device);
                ArrayAdapter<String> adapter = new ArrayAdapter<String>(getApplicationContext(),
                        R.layout.list_item_1, new String[]{});
                adapter.setDropDownViewResource(R.layout.spinner_dropdown_item);
                spinner.setAdapter(adapter);
                adapter = null;
                spinner = null;
                Spinner[] spinners = new Spinner[4];
                spinners[0] = (Spinner) findViewById(R.id.spinner_select_mode);
                spinners[1] = (Spinner) findViewById(R.id.spinner_set_acceleration);
                spinners[2] = (Spinner) findViewById(R.id.spinner_set_gyroscope);
                for (int i = 0; i < 3; i++) {
                    spinners[i].setSelection(0);
                }
                spinners = null;
                // set progress-bar
                findViewById(R.id.progress_ble).setVisibility(View.INVISIBLE);
                // set button
                Button button;
                button = (Button) findViewById(R.id.button_open_usb);
                button.setText(R.string.button_open);
                button = (Button) findViewById(R.id.button_connect_ble);
                button.setText(R.string.button_connect);
                button = (Button) findViewById(R.id.button_measurement);
                button.setText(R.string.button_measurement_start);
                button = null;
                // set imageview
                ImageView view = (ImageView) findViewById(R.id.image_battery);
                view.setImageResource(R.drawable.ic_battery_unknown_grey600_18dp);
                // set layout(error)
                LinearLayout layout = (LinearLayout) findViewById(R.id.layout_error);
                layout.setVisibility(View.GONE);
                layout = null;

                invalidateOptionsMenu();
                common.vibrate();
            }
        });
    }

    private void setViewOpen() {
        handler.post(new Runnable() {
            @Override
            public void run() {
                Button button = (Button) findViewById(R.id.button_open_usb);
                button.setEnabled(true);
                button.setText(isOpening ? R.string.button_close : R.string.button_open);

                TextView textView = (TextView) findViewById(R.id.connect_state_usb);
                textView.setText(isOpening ? R.string.text_state_open : R.string.text_state_close);

                common.vibrate();
                invalidateOptionsMenu();
            }
        });
    }

    @Override
    protected void setViewScan() {
        handler.post(new Runnable() {
            @Override
            public void run() {
                findViewById(R.id.button_scan).setEnabled(!isScannin);
                findViewById(R.id.button_connect_ble).setEnabled(!isScannin);
                findViewById(R.id.progress_ble).setVisibility(
                        isScannin ? View.VISIBLE : View.INVISIBLE);

                if (mDeviceAdapter == null || mDeviceSet == null) {
                    return;
                }
                // clear list
                if (!mDeviceAdapter.isEmpty()) {
                    mDeviceSet.clear();
                    mDeviceAdapter.clear();
                }
                // update list
                if (0 != mDeviceSet.size()) {
                    mDeviceAdapter.addAll(mDeviceSet);
                    findViewById(R.id.button_connect_ble).setEnabled(true);
                    findViewById(R.id.spinner_device).setEnabled(true);

                    common.vibrate();
                } else {
                    findViewById(R.id.button_connect_ble).setEnabled(false);
                    findViewById(R.id.spinner_device).setEnabled(false);
                }
                mDeviceAdapter.notifyDataSetChanged();
                Spinner spinner = (Spinner) findViewById(R.id.spinner_device);
                spinner.setAdapter(mDeviceAdapter);
            }
        });
    }

    @Override
    protected void setViewMeasurement() {
        handler.post(new Runnable() {
            @Override
            public void run() {
                Button button = (Button) findViewById(R.id.button_measurement);
                button.setText(isMeasure ?
                        R.string.button_measurement_stop : R.string.button_measurement_start);
                findViewById(R.id.button_connect_ble).setEnabled(!isMeasure);
                findViewById(R.id.button_initialize).setEnabled(!isMeasure);
                findViewById(R.id.button_marking).setEnabled(isMeasure);
                findViewById(R.id.text_notice)
                        .setVisibility(isMeasure ? View.VISIBLE : View.INVISIBLE);
                findViewById(R.id.layout_error)
                        .setVisibility(isMeasure ? View.VISIBLE : View.INVISIBLE);
                findViewById(R.id.spinner_select_mode).setEnabled(!isMeasure);
                findViewById(R.id.spinner_select_quality).setEnabled(!isMeasure);
                findViewById(R.id.spinner_set_acceleration).setEnabled(!isMeasure);
                findViewById(R.id.spinner_set_gyroscope).setEnabled(!isMeasure);

                findViewById(R.id.button_eog_graph).setEnabled(isMeasure);
                findViewById(R.id.graph_VvVh).setEnabled(isMeasure);
                findViewById(R.id.button_acc_graph).setEnabled(isMeasure);
                findViewById(R.id.graph_acc).setEnabled(isMeasure);
                findViewById(R.id.button_gyro_graph).setEnabled(isMeasure);
                findViewById(R.id.graph_gyro).setEnabled(isMeasure);

                if (isMeasure) {
                    timerError = new Timer(true);
                    timerError.schedule(new TimerTask() {
                        @Override
                        public void run() {

                            Spinner spinner = (Spinner) findViewById(R.id.spinner_select_quality);
                            final long priod = 400 / (((spinner
                                    .getSelectedItemPosition() + 0x01) & 0xFF) * 10);
                            final long count = mNumTotalLast - mNumTotalPrev;
                            final BigDecimal num_count = new BigDecimal(
                                    count);
                            final BigDecimal num_priod = new BigDecimal(
                                    priod);
                            final long ratio_last = (int) (num_count
                                    .divide(num_priod, 5,
                                            BigDecimal.ROUND_HALF_UP)
                                    .doubleValue() * 100);

                            long ratio = (ratio_last + mRatioPrev) / 2;
//                            RatingBar rateBar = (RatingBar) findViewById(R.id.rate_cominucation_state);
//                            if (ratio == 0) {
//                                rateBar.setRating(0);
//                            } else if (0 < ratio && ratio <= 70) {
//                                rateBar.setRating(1);
//                            } else if (70 < ratio && ratio <= 90) {
//                                rateBar.setRating(2);
//                            } else if (90 < ratio) {
//                                rateBar.setRating(3);
//                            }
                            common.setViewComm(ratio);

                            mNumTotalPrev += count;
                            mRatioPrev = ratio_last;

                        }
                    }, 200L, 400L);
                } else {
                    timerError.cancel();
                    timerError.purge();

                    handler.postDelayed(new Runnable() {
                        @Override
                        public void run() {
                            ImageView imageView = (ImageView) findViewById(R.id.image_battery);
                            imageView.setImageResource(R.drawable.ic_battery_unknown_grey600_18dp);
                        }
                    }, 500L);
                }
                // initialize parameter
                mPrevCount = -1;
                mPrevTime = 0;
                mTotalCount = 0;
                mErrorCount = 0;

                mNumTotalPrev = 0;
                mNumTotalLast = 0;
                mRatioPrev = 100;

                common.vibrate();
            }
        });
    }

    @Override
    protected void sendData(byte[] data) {
        mUsbHostService.write(data);
    }
}
