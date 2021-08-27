
package com.jins_jp.meme.academic.ble;

import java.util.List;
import java.util.UUID;

import android.annotation.SuppressLint;
import android.app.Service;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothGatt;
import android.bluetooth.BluetoothGattCallback;
import android.bluetooth.BluetoothGattCharacteristic;
import android.bluetooth.BluetoothGattDescriptor;
import android.bluetooth.BluetoothGattService;
import android.bluetooth.BluetoothManager;
import android.bluetooth.BluetoothProfile;
import android.content.Context;
import android.content.Intent;
import android.os.Binder;
import android.os.IBinder;
import android.util.Log;

import com.jins.meme.academic.util.DataEncryption;
import com.jins.meme.academic.util.LogCat;

@SuppressLint("NewApi")
public class BluetoothLeService extends Service {
    private final String TAG = getClass().getSimpleName();

    private BluetoothManager mBluetoothManager;
    private BluetoothAdapter mBluetoothAdapter;
    private BluetoothGatt mBluetoothGatt;

    private String mBluetoothDeviceAddress;
    private int mConnectionState = STATE_DISCONNECTED;
    private static final int STATE_DISCONNECTED = 0;
    private static final int STATE_CONNECTING = 1;
    private static final int STATE_CONNECTED = 2;
    private static final int STATE_DISCONNECTING = 3;

    public final static String ACTION_GATT_CONNECTED = "ACTION_GATT_CONNECTED";
    public final static String ACTION_GATT_CONNECTING = "ACTION_GATT_CONNECTING";
    public final static String ACTION_GATT_DISCONNECTED = "ACTION_GATT_DISCONNECTED";
    public final static String ACTION_GATT_DISCONNECTING = "ACTION_GATT_DISCONNECTING";
    public final static String ACTION_GATT_SERVICES_DISCOVERED = "ACTION_GATT_SERVICES_DISCOVERED";
    public final static String ACTION_GATT_DESCRIPTOR_READ = "ACTION_GATT_DESCRIPTOR_READ";
    public final static String ACTION_GATT_DESCRIPTOR_WRITE = "ACTION_GATT_DESCRIPTOR_WRITE";
    public final static String ACTION_GATT_CHARACTERISTIC_READ = "ACTION_GATT_CHARACTERISTIC_READ";
    public final static String ACTION_GATT_CHARACTERISTIC_WRITE = "ACTION_GATT_CHARACTERISTIC_WRITE";
    public final static String ACTION_DATA_AVAILABLE = "ACTION_DATA_AVAILABLE";
    public final static String EXTRA_DATA = "EXTRA_DATA";

    public final static UUID SERVICE_UUID = UUID.fromString("D6F25BD1-5B54-4360-96D8-7AA62E04C7EF");
    public final static UUID RX_CHAR_UUID = UUID.fromString("D6F25BD4-5B54-4360-96D8-7AA62E04C7EF");
    public final static UUID TX_CHAR_UUID = UUID.fromString("D6F25BD2-5B54-4360-96D8-7AA62E04C7EF");
    public final static UUID CLIENT_CHARACTERISTIC_CONFIG = UUID
            .fromString("00002902-0000-1000-8000-00805f9b34fb");

    private final BluetoothGattCallback mGattCallback = new BluetoothGattCallback() {

        @Override
        public void onConnectionStateChange(BluetoothGatt gatt, int status, int newState) {
            if (mBluetoothGatt != gatt) {
                close();
            }

            String intentAction;
            if (newState == BluetoothProfile.STATE_CONNECTED) {
                mConnectionState = STATE_CONNECTED;
                intentAction = ACTION_GATT_CONNECTED;
                broadcastUpdate(intentAction);
                LogCat.i(TAG, "Connected to GATT server.");

                LogCat.i(TAG, "status: " + status);
                boolean bool = gatt.discoverServices();
                LogCat.i(TAG, "Attempting to start service discovery:" + bool);

            } else if (newState == BluetoothProfile.STATE_CONNECTING) {
                mConnectionState = STATE_CONNECTING;
                intentAction = ACTION_GATT_CONNECTING;
                broadcastUpdate(intentAction);
            } else if (newState == BluetoothProfile.STATE_DISCONNECTED) {
                mConnectionState = STATE_DISCONNECTED;
                intentAction = ACTION_GATT_DISCONNECTED;
                broadcastUpdate(intentAction);

                LogCat.i(TAG, "Disconnected from GATT server.");
            } else if (newState == BluetoothProfile.STATE_DISCONNECTING) {
                mConnectionState = STATE_DISCONNECTING;
            }
        }

        @Override
        public void onServicesDiscovered(BluetoothGatt gatt, int status) {
            if (mBluetoothGatt == gatt)
                LogCat.w(TAG, "onServicesDiscovered received: " + status);

            if (status == BluetoothGatt.GATT_SUCCESS) {
                String intentAction = ACTION_GATT_SERVICES_DISCOVERED;
                broadcastUpdate(intentAction);
            }

            LogCat.w(TAG, "onServicesDiscovered received: " + status);
        }

        @Override
        public void onDescriptorRead(BluetoothGatt gatt, BluetoothGattDescriptor descriptor,
                int status) {
            if (status == BluetoothGatt.GATT_SUCCESS) {
                String intentAction = ACTION_GATT_DESCRIPTOR_READ;
                broadcastUpdate(intentAction);
            }

            LogCat.w(TAG, "onDescriptorRead received: " + status);
        };

        @Override
        public void onDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor,
                int status) {
            if (status == BluetoothGatt.GATT_SUCCESS) {
                String intentAction = ACTION_GATT_DESCRIPTOR_WRITE;
                broadcastUpdate(intentAction);
            }

            LogCat.w(TAG, "onDescriptorWrite received: " + status);
        };

        @Override
        public void onCharacteristicRead(BluetoothGatt gatt,
                BluetoothGattCharacteristic characteristic, int status) {
            if (status == BluetoothGatt.GATT_SUCCESS) {
                String intentAction = ACTION_GATT_CHARACTERISTIC_READ;
                broadcastUpdate(intentAction, characteristic);
            }

            LogCat.w(TAG, "onCharacteristicRead received: " + status);
        }

        @Override
        public void onCharacteristicWrite(BluetoothGatt gatt,
                BluetoothGattCharacteristic characteristic, int status) {
            if (status == BluetoothGatt.GATT_SUCCESS) {
                String intentAction = ACTION_GATT_CHARACTERISTIC_WRITE;
                broadcastUpdate(intentAction);
            }

            LogCat.w(TAG, "onCharacteristicWrite received: " + status);
        };

        @Override
        public void onCharacteristicChanged(BluetoothGatt gatt,
                BluetoothGattCharacteristic characteristic) {
            String intentAction = ACTION_DATA_AVAILABLE;
            broadcastUpdate(intentAction, characteristic);
        }

    };

    private void broadcastUpdate(final String action) {
        final Intent intent = new Intent(action);
        sendBroadcast(intent);
    }

    private void broadcastUpdate(final String action,
            final BluetoothGattCharacteristic characteristic) {
        final Intent intent = new Intent(action);
        final byte[] data = characteristic.getValue();
        if (data != null && data.length > 0) {
            intent.putExtra(EXTRA_DATA, DataEncryption.decode(data));
        }
        sendBroadcast(intent);
    }

    public class LocalBinder extends Binder {
        public BluetoothLeService getService() {
            return BluetoothLeService.this;
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
        if (mBluetoothManager == null) {
            mBluetoothManager = (BluetoothManager) getSystemService(Context.BLUETOOTH_SERVICE);
            if (mBluetoothManager == null) {

                LogCat.e(TAG, "Unable to initialize BluetoothManager.");
                return false;
            }
        }

        mBluetoothAdapter = mBluetoothManager.getAdapter();
        if (mBluetoothAdapter == null) {

            LogCat.e(TAG, "Unable to obtain a BluetoothAdapter.");
            return false;
        }

        return true;
    }

    public boolean connect(final String address) {
        if (mBluetoothAdapter == null || address == null) {

            LogCat.w(TAG, "BluetoothAdapter not initialized or unspecified address.");
            return false;
        }

        if (mBluetoothDeviceAddress != null && address.equals(mBluetoothDeviceAddress)
                && mBluetoothGatt != null) {

            LogCat.d(TAG, "Trying to use an existing mBluetoothGatt for connection.");
            if (mBluetoothGatt.connect()) {
                mConnectionState = STATE_CONNECTING;
                return true;
            } else {
                return false;
            }
        }

        final BluetoothDevice device = mBluetoothAdapter.getRemoteDevice(address);
        if (device == null) {

            LogCat.w(TAG, "Device not found.  Unable to connect.");
            return false;
        }
        mBluetoothGatt = device.connectGatt(getApplicationContext(), false, mGattCallback);

        LogCat.d(TAG, "Trying to create a new connection.");
        mBluetoothDeviceAddress = address;
        mConnectionState = STATE_CONNECTING;
        return true;
    }

    public void disconnect() {
        if (mBluetoothAdapter == null || mBluetoothGatt == null) {

            LogCat.w(TAG, "BluetoothAdapter not initialized");
            return;
        }
        mBluetoothGatt.disconnect();
    }

    public void close() {
        if (mBluetoothGatt == null) {
            return;
        }
        mBluetoothGatt.close();
        mBluetoothGatt = null;
        mBluetoothDeviceAddress = null;
    }

    public String getStatus() {
        String status = "";
        switch (mConnectionState) {
            case STATE_DISCONNECTED:
                status = "STATE_DISCONNECTED";
                break;
            case STATE_CONNECTING:
                status = "STATE_CONNECTING";
                break;
            case STATE_CONNECTED:
                status = "STATE_CONNECTED";
                break;
            default:
                status = "STATE_UNKNOWN";
                break;
        }
        return status;
    }

    public void readCharacteristic(BluetoothGattCharacteristic characteristic) {
        if (mBluetoothAdapter == null || mBluetoothGatt == null) {

            LogCat.w(TAG, "BluetoothAdapter not initialized");
            return;
        }
        mBluetoothGatt.readCharacteristic(characteristic);
    }

    public void writeCharacteristic(BluetoothGattCharacteristic characteristic) {
        if (mBluetoothAdapter == null || mBluetoothGatt == null) {

            LogCat.w(TAG, "BluetoothAdapter not initialized");
            return;
        }
        mBluetoothGatt.writeCharacteristic(characteristic);
    }

    public void setCharacteristicNotification(BluetoothGattCharacteristic characteristic,
            boolean enabled) {
        if (mBluetoothAdapter == null || mBluetoothGatt == null) {

            LogCat.w(TAG, "BluetoothAdapter not initialized");
            return;
        }
        if (!mBluetoothGatt.setCharacteristicNotification(characteristic, enabled)) {
            return;
        }
        try {
            BluetoothGattDescriptor descriptor = characteristic
                    .getDescriptor(CLIENT_CHARACTERISTIC_CONFIG);

            if (enabled) {
                descriptor.setValue(BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE);
            } else {
                descriptor.setValue(BluetoothGattDescriptor.DISABLE_NOTIFICATION_VALUE);
            }
            boolean bool = mBluetoothGatt.writeDescriptor(descriptor);
            LogCat.d(TAG, "bool: " + bool);
        } catch (NullPointerException e) {
            e.printStackTrace();
        }
    }

    public List<BluetoothGattService> getSupportedGattServices() {
        if (mBluetoothGatt == null)
            return null;
        return mBluetoothGatt.getServices();
    }

    public BluetoothGattService getSupportedGattService(UUID uuid) {
        if (mBluetoothGatt == null)
            return null;
        try {
            LogCat.d(TAG, "BluetoothGattService: " + mBluetoothGatt.getService(uuid).getUuid());
        } catch (NullPointerException e) {
            return null;
        }
        return mBluetoothGatt.getService(uuid);
    }
}
