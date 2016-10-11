
package com.jins_jp.meme.academic.usb;

import java.io.IOException;
import java.util.Arrays;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbDeviceConnection;
import android.hardware.usb.UsbManager;
import android.os.Binder;
import android.os.IBinder;

import com.jins.android.usbserial.SerialIOManager;
import com.jins.android.usbserial.UsbSerialPort;
import com.jins.meme.academic.util.DataEncryption;
import com.jins.meme.academic.util.HexDump;
import com.jins.meme.academic.util.LogCat;

public class UsbHostService extends Service {
    private final String TAG = getClass().getSimpleName();

    // management and control of USB device
    private UsbManager mUsbManager;
    private UsbSerialPort mPort;
    private SerialIOManager mSerialIoManager;

    private int mPortState = STATE_CLOSE;
    private static final int STATE_CLOSE = 0;
    private static final int STATE_OPEN = 1;

    public final static String ACTION_PORT_OPEN = "ACTION_PORT_OPEN";
    public final static String ACTION_PORT_CLOSE = "ACTION_PORT_CLOSE";
    public final static String ACTION_DATA_AVAILABLE = "ACTION_DATA_AVAILABLE";
    public final static String EXTRA_DATA = "EXTRA_DATA";

    private final SerialIOManager.Listener mListener = new SerialIOManager.Listener() {

        @Override
        public void onRunError(Exception e) {
            // no use
        }

        @Override
        public void onNewData(byte[] arr) {
            LogCat.d(TAG, "reception date: " + HexDump.toHexString(arr));
            if (arr.length % 20 == 0) {
                int num = arr.length / 20;
                for (int n = 0; n < num; n++) {
                    // create data
                    byte[] data = Arrays.copyOfRange(arr, n * 20, (n + 1) * 20);
                    String intentAction = ACTION_DATA_AVAILABLE;
                    broadcastUpdate(intentAction, DataEncryption.decode(data));

                    LogCat.d(TAG,
                            "target data: " + HexDump.toHexString(DataEncryption.decode(data)));
                }
            } else {
                if (arr.length < 3 || 20 < arr.length) {
                    return;
                }
                String intentAction = ACTION_DATA_AVAILABLE;
                broadcastUpdate(intentAction, arr);

                LogCat.d(TAG, "target data: " + HexDump.toHexString(arr));
            }
        }
    };

    private void broadcastUpdate(final String action) {
        final Intent intent = new Intent(action);
        sendBroadcast(intent);
    }

    private void broadcastUpdate(final String action, final byte[] data) {
        final Intent intent = new Intent(action);
        if (data != null && data.length > 0) {
            intent.putExtra(EXTRA_DATA, data);
        }
        sendBroadcast(intent);
    }

    public class LocalBinder extends Binder {
        public UsbHostService getService() {
            return UsbHostService.this;
        }
    }

    @Override
    public IBinder onBind(Intent intent) {
        return mBinder;
    }

    @Override
    public boolean onUnbind(Intent intent) {
        return super.onUnbind(intent);
    }

    private final IBinder mBinder = new LocalBinder();

    public boolean initialize() {
        if (mUsbManager == null) {
            mUsbManager = (UsbManager) getSystemService(Context.USB_SERVICE);
            if (mUsbManager == null) {

                LogCat.e(TAG, "Unable to initialize UsbManager.");
                return false;
            }
        }

        return true;
    }

    public boolean openPort(UsbDevice device) {
        // get a connection to the first available driver
        UsbDeviceConnection connection = mUsbManager.openDevice(device);
        if (connection == null) {
            LogCat.d(TAG, "USB device connection null...");
            return false;
        } else {
            // Read some data! Most have just one port (port 0).
            mPort = new UsbSerialPort(device, 0);
            LogCat.d(TAG, "Serial device: " + mPort.getClass().getSimpleName());
            // open a connection to the first available driver
            try {
                mPort.open(connection);
                mPort.setParams(115200, 8, UsbSerialPort.STOPBITS_1, UsbSerialPort.PARITY_NONE);
            } catch (IOException e) {
                LogCat.e(TAG, "Error setting up device: " + e.getMessage(), e);
                try {
                    mPort.close();
                } catch (IOException e2) {
                    // Ignore.
                }
                mPort = null;
                return false;
            }
            // start IO Manager
            stopIoManager();
            startIoManager();
            // change state
            mPortState = STATE_OPEN;
            String intentAction = ACTION_PORT_OPEN;
            broadcastUpdate(intentAction);

            return true;
        }
    }

    public void closePort() {
        if (mPort != null) {
            // [USB] stop IO Manager
            stopIoManager();
            // [USB] close a connection to the first available driver
            try {
                mPort.close();
            } catch (IOException e) {
                // Ignore.
            }
            mPort = null;
            // change state
            mPortState = STATE_CLOSE;
            String intentAction = ACTION_PORT_CLOSE;
            broadcastUpdate(intentAction);
        }
    }

    public String getStatus() {
        String status = "";
        switch (mPortState) {
            case STATE_CLOSE:
                status = "STATE_CLOSE";
                break;
            case STATE_OPEN:
                status = "STATE_OPEN";
                break;
        }
        return status;
    }

    private void startIoManager() {
        if (mPort != null) {
            LogCat.i(TAG, "Starting io manager ..");
            mSerialIoManager = new SerialIOManager(mPort, mListener);
            ExecutorService executor = Executors.newSingleThreadExecutor();
            executor.submit(mSerialIoManager);
        }
    }

    private void stopIoManager() {
        if (mSerialIoManager != null) {
            LogCat.i(TAG, "Stopping io manager ..");
            mSerialIoManager.stop();
            mSerialIoManager = null;
        }
    }

    public void write(byte[] data) {
        if (mSerialIoManager != null) {
            mSerialIoManager.writeAsync(data);
        }
    }

}
