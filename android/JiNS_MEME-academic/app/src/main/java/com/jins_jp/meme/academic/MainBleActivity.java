
package com.jins_jp.meme.academic;

import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothGattCharacteristic;
import android.bluetooth.BluetoothManager;
import android.content.BroadcastReceiver;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.ServiceConnection;
import android.os.Bundle;
import android.os.IBinder;
import android.support.v7.widget.Toolbar;
import android.view.Menu;
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
import com.jins_jp.meme.academic.ble.BluetoothLeService;

import java.math.BigDecimal;
import java.util.Timer;
import java.util.TimerTask;
import java.util.UUID;

public class MainBleActivity extends MainActivity {

    private BluetoothLeService mBluetoothLeService;
    private BluetoothAdapter mBluetoothAdapter;
    private BluetoothGattCharacteristic mBluetoothGattChar;
    private final UUID serviceUUID = BluetoothLeService.SERVICE_UUID;
    private final UUID rxcharaUUID = BluetoothLeService.RX_CHAR_UUID;
    private final UUID txcharaUUID = BluetoothLeService.TX_CHAR_UUID;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        // set view to this activity
        setContentView(R.layout.activity_main_ble);

        // Graph
        graphEOG.makeChart();
        graphAcc.makeChart();
        graphGyro.makeChart();

        // set the tool-bar to this activity
        Toolbar mToolbar = (Toolbar) findViewById(R.id.header);
        mToolbar.setTitle(R.string.app_title);
        setSupportActionBar(mToolbar);
        // set view
        setViewDefault();

        common.SetMode(common.mode_ble);
    }

    @Override
    protected void onStart() {
        super.onStart();
        // get Bluetooth adapter
        final BluetoothManager bluetoothManager = (BluetoothManager) getSystemService(Context.BLUETOOTH_SERVICE);
        mBluetoothAdapter = bluetoothManager.getAdapter();
        if (!mBluetoothAdapter.isEnabled()) {
            // show dialog
            if (!mBluetoothAdapter.isEnabled()) {
                mBluetoothAdapter.enable();
            }
        }
        // bind service
        Intent intent = new Intent(getApplicationContext(), BluetoothLeService.class);
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
        // stop device scan
        if (isScannin)
            scanLeDevice(false);
        // disconnect BLE device
        if (isConnect)
            mBluetoothLeService.disconnect();
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

    private BluetoothAdapter.LeScanCallback mLeScanCallback = new BluetoothAdapter.LeScanCallback() {

        @Override
        public void onLeScan(final BluetoothDevice device, final int rssi,
                             final byte[] scanRecord) {
            runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    if (device.getName() == null) {
                        return;
                    }
                    if (serviceUUID.equals(UUID.fromString(getUuid(scanRecord)))) {
                        // add device address
                        String address = device.getAddress();
                        if (!mDeviceSet.contains(address)) {
                            mDeviceSet.add(address);
                            LogCat.d(TAG, "address: " + address);
                        }
                        address = null;
                    }
                }

                private String getUuid(byte[] scanRecord) {
                    String uuid = null;
                    uuid = common.IntToHex2(scanRecord[20] & 0xff)
                            + common.IntToHex2(scanRecord[19] & 0xff)
                            + common.IntToHex2(scanRecord[18] & 0xff)
                            + common.IntToHex2(scanRecord[17] & 0xff)
                            + "-"
                            + common.IntToHex2(scanRecord[16] & 0xff)
                            + common.IntToHex2(scanRecord[15] & 0xff)
                            + "-"
                            + common.IntToHex2(scanRecord[14] & 0xff)
                            + common.IntToHex2(scanRecord[13] & 0xff)
                            + "-"
                            + common.IntToHex2(scanRecord[12] & 0xff)
                            + common.IntToHex2(scanRecord[11] & 0xff)
                            + "-"
                            + common.IntToHex2(scanRecord[10] & 0xff)
                            + common.IntToHex2(scanRecord[9] & 0xff)
                            + common.IntToHex2(scanRecord[8] & 0xff)
                            + common.IntToHex2(scanRecord[7] & 0xff)
                            + common.IntToHex2(scanRecord[6] & 0xff)
                            + common.IntToHex2(scanRecord[5] & 0xff);

                    return uuid;
                }

            });
        }

    };

    @Override
    protected ServiceConnection getConnection() {
        mConnection = new ServiceConnection() {

            @Override
            public void onServiceConnected(ComponentName componentName, IBinder service) {
                mBluetoothLeService = ((BluetoothLeService.LocalBinder) service).getService();
                if (!mBluetoothLeService.initialize()) {
                    LogCat.e(TAG, "Unable to initialize Bluetooth");
                    finish();
                }
                LogCat.i(TAG, "initialized Bluetooth");
            }

            @Override
            public void onServiceDisconnected(ComponentName componentName) {
                mBluetoothLeService = null;
            }

        };
        return mConnection;
    }

    @Override
    protected BroadcastReceiver getReceiver() {
        mReceiver = new BroadcastReceiver() {

            @Override
            public void onReceive(Context context, Intent intent) {
                final String action = intent.getAction();

                if (BluetoothLeService.ACTION_GATT_CONNECTED.equals(action)) {
                    LogCat.d(TAG, "ACTION_GATT_CONNECTED");

                } else if (BluetoothLeService.ACTION_GATT_DISCONNECTED.equals(action)) {
                    LogCat.d(TAG, "ACTION_GATT_DISCONNECTED");

                    isConnect = false;
                    setViewDefault();
                    // close service
                    mBluetoothLeService.close();
                    // stop service
                    stopService(new Intent(getApplicationContext(), BluetoothLeService.class));
                    common.showToast("Disconncted from the MEME");
                } else if (BluetoothLeService.ACTION_GATT_SERVICES_DISCOVERED.equals(action)) {
                    LogCat.d(TAG, "ACTION_GATT_SERVICES_DISCOVERED");

                    isConnect = true;
                    common.setViewConnect(isConnect);
                    try {
                        // notification enable
                        handler.postDelayed(new Runnable() {
                            @Override
                            public void run() {
                                mBluetoothGattChar = mBluetoothLeService
                                        .getSupportedGattService(serviceUUID)
                                        .getCharacteristic(rxcharaUUID);
                                mBluetoothLeService
                                        .setCharacteristicNotification(mBluetoothGattChar, true);
                            }
                        }, 1500L);
                    } catch (NullPointerException e) {
                        mBluetoothLeService.disconnect();
                        common.showToast("Not a connected device");
                    }
                    invalidateOptionsMenu();
                } else if (BluetoothLeService.ACTION_GATT_DESCRIPTOR_READ.equals(action)) {
                    LogCat.d(TAG, "ACTION_GATT_DESCRIPTOR_READ");

                } else if (BluetoothLeService.ACTION_GATT_CHARACTERISTIC_READ.equals(action)) {
                    LogCat.d(TAG, "ACTION_GATT_CHARACTERISTIC_READ");

                } else if (BluetoothLeService.ACTION_GATT_DESCRIPTOR_WRITE.equals(action)) {
                    LogCat.d(TAG, "ACTION_GATT_DESCRIPTOR_WRITE");
                    // start service
                    startService(new Intent(getApplicationContext(), BluetoothLeService.class));
                    common.showToast("Conncted to the MEME");
                    handler.postDelayed(new Runnable() {
                        @Override
                        public void run() {
                            getBleVersion();
                        }
                    }, 300L);
                    handler.postDelayed(new Runnable() {
                        @Override
                        public void run() {
                            getMode();
                        }
                    }, 600L);
                    handler.postDelayed(new Runnable() {
                        @Override
                        public void run() {
                            getParams();
                        }
                    }, 900L);
                } else if (BluetoothLeService.ACTION_GATT_CHARACTERISTIC_WRITE.equals(action)) {
                    LogCat.d(TAG, "ACTION_GATT_CHARACTERISTIC_WRITE");

                } else if (BluetoothLeService.ACTION_DATA_AVAILABLE.equals(action)) {
                    LogCat.d(TAG, "ACTION_DATA_AVAILABLE");

                    byte[] data = intent.getByteArrayExtra(BluetoothLeService.EXTRA_DATA);
                    LogCat.d(TAG, "decode data: " + HexDump.toHexString(data));

                    analysisData(data);
                }
            }

        };
        return mReceiver;
    }

    @Override
    protected IntentFilter makeIntentFilter() {
        final IntentFilter intentFilter = new IntentFilter();
        intentFilter.addAction(BluetoothLeService.ACTION_GATT_CONNECTED);
        intentFilter.addAction(BluetoothLeService.ACTION_GATT_DISCONNECTED);
        intentFilter.addAction(BluetoothLeService.ACTION_GATT_SERVICES_DISCOVERED);
        intentFilter.addAction(BluetoothLeService.ACTION_GATT_DESCRIPTOR_READ);
        intentFilter.addAction(BluetoothLeService.ACTION_GATT_DESCRIPTOR_WRITE);
        intentFilter.addAction(BluetoothLeService.ACTION_GATT_CHARACTERISTIC_READ);
        intentFilter.addAction(BluetoothLeService.ACTION_GATT_CHARACTERISTIC_WRITE);
        intentFilter.addAction(BluetoothLeService.ACTION_DATA_AVAILABLE);
        return intentFilter;
    }

    @SuppressWarnings("deprecation")
    @Override
    protected void scanLeDevice(final boolean enable) {
        if (!mBluetoothAdapter.isEnabled()) {
            return;
        }

        isScannin = enable;
        if (enable) {
            // start device scan
            mBluetoothAdapter.startLeScan(mLeScanCallback);
            // set timer
            handler.postDelayed(new Runnable() {
                @Override
                public void run() {
                    scanLeDevice(false);
                }
            }, TIMEOUT * 1000);
        } else {
            // stop device scan
            mBluetoothAdapter.stopLeScan(mLeScanCallback);
        }
        setViewScan();
    }

    @Override
    protected void connectLeDevice() {
        if (!mBluetoothAdapter.isEnabled()) {
            return;
        }
        findViewById(R.id.button_connect_ble).setEnabled(false);
        if (isConnect) {
            mMemeAddress = null;
            mMemeVersion = null;
            // send "disconnect BLE" command
            mBluetoothLeService.disconnect();
        } else {
            Spinner spinner = (Spinner) findViewById(R.id.spinner_device);
            String address = (String) spinner.getSelectedItem();
            mMemeAddress = address;
            DataEncryption.setKey(address);
            // send "connect BLE" command
            mBluetoothLeService.connect(address);
        }
    }

    @Override
    protected void setViewDefault() {
        isConnect = false;
        isMeasure = false;

        handler.post(new Runnable() {
            @Override
            public void run() {
                // set view enable
                findViewById(R.id.button_scan).setEnabled(true);
                int[] resource = {//
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
                            RatingBar rateBar = (RatingBar) findViewById(R.id.rate_cominucation_state);
                            if (ratio == 0) {
                                rateBar.setRating(0);
                            } else if (0 < ratio && ratio <= 70) {
                                rateBar.setRating(1);
                            } else if (70 < ratio && ratio <= 90) {
                                rateBar.setRating(2);
                            } else if (90 < ratio) {
                                rateBar.setRating(3);
                            }
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
        mBluetoothGattChar = mBluetoothLeService.getSupportedGattService(
                serviceUUID).getCharacteristic(txcharaUUID);
        mBluetoothGattChar.setValue(data);
        mBluetoothLeService.writeCharacteristic(mBluetoothGattChar);
    }

}
