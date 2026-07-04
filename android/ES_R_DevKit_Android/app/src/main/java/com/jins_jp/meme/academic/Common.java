
package com.jins_jp.meme.academic;

import android.app.Activity;
import android.content.ContentResolver;
import android.content.ContentValues;
import android.content.Context;
import android.content.pm.ActivityInfo;
import android.content.res.Configuration;
import android.net.Uri;
import android.os.Build;
import android.os.Environment;
import android.os.Handler;
import android.os.Vibrator;
import android.provider.MediaStore;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.ProgressBar;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

import com.jins.meme.academic.util.LogCat;

import org.apache.commons.csv.CSVFormat;
import org.apache.commons.csv.CSVPrinter;

import java.io.BufferedWriter;
import java.io.IOException;
import java.io.OutputStream;
import java.io.OutputStreamWriter;
import java.io.Writer;
import java.math.BigDecimal;
import java.text.NumberFormat;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.Locale;
import java.util.TimeZone;

public class Common extends Object {
    private final String TAG = getClass().getSimpleName();

    Context context;
    Activity activity;
    Handler handler;

    public final int mode_usb = 1;
    public final int mode_ble = 2;
    int mode_setting = mode_usb;

    public static final String CSV_FOLDER_NAME = "ESR Logger";

    public String headerString = null;
    public  ArrayList<String> msgs = new ArrayList<>();
    public long csvCount = 0;
    private Uri csvUri = null;

    public Common(Context context, Activity activity, Handler handler) {
        this.context = context;
        this.activity = activity;
        this.handler = handler;
    }

    public void setOrientation() {
        //Configuration configuration = context.getResources().getConfiguration();
        //if (Build.VERSION.SDK_INT < Build.VERSION_CODES.HONEYCOMB_MR2) {
        //    if ((configuration.screenLayout & Configuration.SCREENLAYOUT_SIZE_MASK) < Configuration.SCREENLAYOUT_SIZE_LARGE) {
        //        activity.setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_PORTRAIT);
        //    } else {
        //        activity.setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_LANDSCAPE);
        //    }
        //} else {
        //    if (configuration.smallestScreenWidthDp < 600) {
        //        activity.setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_PORTRAIT);
        //    } else {
        //        activity.setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_LANDSCAPE);
        //    }
        //}
        //configuration = null;
        activity.setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_PORTRAIT);
    }

    public void setViewConnect(final boolean isConnect, final String mMemeVersion) {
        handler.post(new Runnable() {
            @Override
            public void run() {
                activity.findViewById(R.id.button_scan).setEnabled(!isConnect);
                activity.findViewById(R.id.spinner_device).setEnabled(!isConnect);
                activity.findViewById(R.id.spinner_select_mode).setEnabled(isConnect);
                activity.findViewById(R.id.spinner_select_quality).setEnabled(isConnect);
                activity.findViewById(R.id.spinner_set_acceleration).setEnabled(isConnect);
                activity.findViewById(R.id.spinner_set_gyroscope).setEnabled(isConnect);
                activity.findViewById(R.id.button_initialize).setEnabled(isConnect);
                activity.findViewById(R.id.button_measurement).setEnabled(isConnect);
                activity.findViewById(R.id.button_marking).setEnabled(false);

                Button button = null;
                TextView textView = null;
                ImageView imageView = null;

                button = (Button) activity.findViewById(R.id.button_connect_ble);
                button.setEnabled(true);
                button.setText(isConnect ?
                        R.string.button_disconnect : R.string.button_connect);
                textView = (TextView) activity.findViewById(R.id.connect_state_ble);
//                textView.setText(isConnect ?
//                        R.string.text_state_connected : R.string.text_state_disconnect);
                textView.setText(isConnect ?
                        (mMemeVersion == null ? "Connected" : "Connected" + "(ver." + mMemeVersion + ")") : "Disconnect");
                imageView = (ImageView) activity.findViewById(R.id.image_battery);
                imageView.setImageResource(R.drawable.ic_battery_unknown_grey600_18dp);

                button = (Button) activity.findViewById(R.id.button_measurement);
                button.setText(R.string.button_measurement_start);

                textView = (TextView) activity.findViewById(R.id.text_success_rate);
                textView.setText("");

                vibrate();
            }
        });
    }

    public void setViewStatus(final boolean isConnect, final String mMemeVersion) {
        handler.post(new Runnable() {
            @Override
            public void run() {
                TextView textView = null;
                textView = (TextView) activity.findViewById(R.id.connect_state_ble);
                textView.setText(isConnect ?
                        (mMemeVersion == null ? "Connected" : "Connected" + "(ver." + mMemeVersion + ")") : "Disconnect");
            }
        });
    }

    public void setViewNotice(final long count) {
        handler.post(new Runnable() {
            @Override
            public void run() {
                TextView textView = null;
                textView = (TextView) activity.findViewById(R.id.text_notice);
                textView.setText("Recording("+count+")...");
            }
        });
    }

    public void setViewInitialize(final boolean isInitial) {
        handler.post(new Runnable() {
            @Override
            public void run() {
                activity.findViewById(R.id.button_initialize).setEnabled(!isInitial);
                vibrate();
            }
        });
    }

    public void setViewBattery(final int level) {
        handler.post(new Runnable() {
            @Override
            public void run() {
                ImageView view = (ImageView) activity.findViewById(R.id.image_battery);
                switch (level) {
                    case 0:
                        view.setImageResource(R.drawable.ic_battery_0_grey600_18dp);
                        break;
                    case 1:
                        view.setImageResource(R.drawable.ic_battery_1_grey600_18dp);
                        break;
                    case 2:
                        view.setImageResource(R.drawable.ic_battery_2_grey600_18dp);
                        break;
                    case 3:
                        view.setImageResource(R.drawable.ic_battery_3_grey600_18dp);
                        break;
                    case 4:
                        view.setImageResource(R.drawable.ic_battery_4_grey600_18dp);
                        break;
                    case 5:
                        view.setImageResource(R.drawable.ic_battery_5_grey600_18dp);
                        break;
                    default:
                        view.setImageResource(R.drawable.ic_battery_unknown_grey600_18dp);
                        break;
                }
            }
        });
    }

    public void setViewError(final long total, final long error) {
        handler.post(new Runnable() {
            @Override
            public void run() {
                final BigDecimal num_total = new BigDecimal(total);
                final BigDecimal num_error = new BigDecimal(error);

                double ratio = 1 - num_error.divide(num_total, 5,
                        BigDecimal.ROUND_HALF_UP).doubleValue();
                TextView textView = null;
                textView = (TextView) activity.findViewById(R.id.text_success_rate);
                NumberFormat numberFormat = NumberFormat
                        .getPercentInstance();
                numberFormat.setMaximumFractionDigits(2);
                numberFormat.setMinimumFractionDigits(2);
                textView.setText("SUCCESS RATE: "
                        + numberFormat.format(ratio));
                ProgressBar progressBar = (ProgressBar) activity
                        .findViewById(R.id.progress_success_rate);
                progressBar.setProgress((int) Math.round(ratio * 10000));
            }
        });
    }

    public void setViewComm(final long ratio) {
        handler.post(new Runnable() {
            @Override
            public void run() {
                TextView textView = (TextView) activity.findViewById(R.id.text_cominucation_rate);
                NumberFormat numberFormat = NumberFormat.getPercentInstance();
                numberFormat.setMaximumFractionDigits(2);
                numberFormat.setMinimumFractionDigits(2);
                textView.setText("COMM RATE: " + numberFormat.format(ratio/100.0));
//                textView.setText("COMM RATE: " + ratio);
            }
        });
    }

    public void setViewMarking(final boolean isMarking) {
        handler.post(new Runnable() {
            @Override
            public void run() {
                activity.findViewById(R.id.button_marking).setEnabled(!isMarking);

                vibrate();
            }
        });
    }

    public void setItemMode(final byte item_mode, final byte item_quality) {
        handler.post(new Runnable() {
            @Override
            public void run() {
                Spinner spinner = null;
                spinner = (Spinner) activity.findViewById(R.id.spinner_select_mode);
                spinner.setSelection((byte) ((item_mode - 0x01) & 0xFF));
                spinner = (Spinner) activity.findViewById(R.id.spinner_select_quality);
                spinner.setSelection((byte) ((item_quality - 0x01) & 0xFF));

                vibrate();
            }
        });
    }

    public void setItemParams(final byte item_acc, final byte item_ang) {
        handler.post(new Runnable() {
            @Override
            public void run() {
                Spinner spinner = null;
                spinner = (Spinner) activity.findViewById(R.id.spinner_set_acceleration);
                spinner.setSelection(item_acc);
                spinner = (Spinner) activity.findViewById(R.id.spinner_set_gyroscope);
                spinner.setSelection(item_ang);

                vibrate();
            }
        });
    }

    public void showToast(final String msg) {
        handler.post(new Runnable() {
            @Override
            public void run() {
                Toast toast = Toast.makeText(context, msg, Toast.LENGTH_SHORT);
//                View v = toast.getView();
//                v.setBackgroundColor(context.getResources()
//                        .getColor(R.color.colorPrimary));
                toast.show();
                toast = null;

                vibrate();
            }
        });
    }

    public void vibrate() {
        Vibrator vibrator = (Vibrator) context.getSystemService(Context.VIBRATOR_SERVICE);
        vibrator.vibrate(320L);
    }

    public byte[] getItemMode() {
        byte[] bytes = new byte[2];
        Spinner spinner = null;
        spinner = (Spinner) activity.findViewById(R.id.spinner_select_mode);
        bytes[0] = (byte) (spinner.getSelectedItemPosition() & 0xFF);
        spinner = (Spinner) activity.findViewById(R.id.spinner_select_quality);
        bytes[1] = (byte) (spinner.getSelectedItemPosition() & 0xFF);
        return bytes;
    }

    public byte[] getItemParams() {
        byte[] bytes = new byte[2];
        Spinner spinner = null;
        spinner = (Spinner) activity.findViewById(R.id.spinner_set_acceleration);
        bytes[0] = (byte) (spinner.getSelectedItemPosition() & 0xFF);
        spinner = (Spinner) activity.findViewById(R.id.spinner_set_gyroscope);
        bytes[1] = (byte) (spinner.getSelectedItemPosition() & 0xFF);
        return bytes;
    }

    public String createFileNew(String address) {
        Log.d("logd","createFileNew");

        // create file name
        SimpleDateFormat simpleDateFormat =
                new SimpleDateFormat("yyyyMMddHHmmss", Locale.getDefault());
        simpleDateFormat.setTimeZone(TimeZone.getTimeZone("GMT"));
        String address2 = address.replace(":", "");
        String name = address2 + "_"
                + simpleDateFormat.format(new Date(System.currentTimeMillis()))
                + ".csv";
        // get header item
        byte[] items = new byte[2];
        items = getItemMode();
        String item_mode = String.format("%s",
                items[0] == 0 ? "Standard" :
                        items[0] == 1 ? "Full" :
                                items[0] == 2 ? "Quaternion" : "nuknown");
        String item_quality = String.format("%s",
                items[1] == 0 ? "100Hz" :
                        items[1] == 1 ? "50Hz" : "nuknown");
        items = getItemParams();
        String item_acc = String.format("%sg", 2 * (int) Math.pow(2, items[0]));
        String item_ang = String.format("%sdps",
                250 * (int) Math.pow(2, (byte) ("Quaternion".equals(item_mode) ? 0x03 : items[1])));
        String item_use = String.format("%s", mode_setting == mode_usb ? "USB Serial" : "Built-in BLE");
        // create file header
        StringBuffer stringBuffer = new StringBuffer();
        stringBuffer.append(String.format(
                "// Data mode  : %s", item_mode));
        stringBuffer.append("\r\n");
        stringBuffer.append(String.format(
                "// Transmission speed  : %s", item_quality));
        stringBuffer.append("\r\n");
        stringBuffer.append(String.format(
                "// Acceleration sensor's range  : %s", item_acc));
        stringBuffer.append("\r\n");
        stringBuffer.append(String.format(
                "// Gyroscope sensor's range  : %s", item_ang));
        stringBuffer.append("\r\n");
//        stringBuffer.append(String.format(
//                "// Use Device  : %s", item_use));
//        stringBuffer.append("\r\n");
        switch (item_mode) {
            case "Standard":
                stringBuffer
                        .append("//"
                                        + "\r\n"
                                        + "//ARTIFACT,"
                                        + "NUM,DATE,"
                                        + "ACC_X,ACC_Y,ACC_Z,"
                                        + "EOG_L1,EOG_R1,"
                                        + "EOG_L2,EOG_R2,"
                                        + "EOG_H1,EOG_H2,"
                                        + "EOG_V1,EOG_V2"

                        );
                break;
            case "Full":
                stringBuffer
                        .append("//"
                                        + "\r\n"
                                        + "//ARTIFACT,"
                                        + "NUM,DATE,"
                                        + "ACC_X,ACC_Y,ACC_Z,"
                                        + "GYRO_X,GYRO_Y,GYRO_Z,"
                                        + "EOG_L,EOG_R,EOG_H,EOG_V"
                        );
                break;
            case "Quaternion":
                stringBuffer
                        .append("//"
                                        + "\r\n"
                                        + "//ARTIFACT,"
                                        + "NUM,DATE,"
                                        + "QUATERNION_W,QUATERNION_X,QUATERNION_Y,QUATERNION_Z"
                        );
                break;
        }

        msgs.clear();
        csvCount = 0;
        setViewNotice(csvCount);

        // create the MediaStore entry under Downloads/ESR Logger
        ContentValues values = new ContentValues();
        values.put(MediaStore.Downloads.DISPLAY_NAME, name);
        values.put(MediaStore.Downloads.MIME_TYPE, "text/csv");
        values.put(MediaStore.Downloads.RELATIVE_PATH,
                Environment.DIRECTORY_DOWNLOADS + "/" + CSV_FOLDER_NAME);
        ContentResolver resolver = context.getContentResolver();
        csvUri = resolver.insert(MediaStore.Downloads.EXTERNAL_CONTENT_URI, values);
        if (csvUri == null) {
            Log.d("logd", "failed to create MediaStore entry");
        }

        headerString = stringBuffer.toString();
        LogCat.d(TAG, "created new file");

        return name;
    }

    public void writeFile(String name, String msg) {
        BufferedWriter bw = null;

        if (name == null || msg == null || csvUri == null) {
            return;
        }

        msgs.add(msg);
        if (msgs.size() < 100) {
            return;
        }
        csvCount = csvCount + msgs.size();
        setViewNotice(csvCount);

        if (headerString != null) {
            msgs.add(0,headerString);
            headerString = null;
        }

        // write file (append to the MediaStore entry)
        try {
            OutputStream os = context.getContentResolver().openOutputStream(csvUri, "wa");
            bw = new BufferedWriter(new OutputStreamWriter(os, "utf-8"));
            for (String m: msgs){
                bw.write(m + "\r\n");
            }
            msgs.clear();

            bw.flush();
        } catch (IOException e) {
            e.printStackTrace();
            return;
        } finally {
            try {
                if (bw != null) {
                    bw.close();
                }
            } catch (Exception e) {
                e.printStackTrace();
            }
        }
        bw = null;
        LogCat.d(TAG, "wrote the data to a file");
    }

    public String IntToHex2(int i) {
        char hex_2[] = {
                Character.forDigit((i >> 4) & 0x0f, 16),
                Character.forDigit(i & 0x0f, 16)
        };
        String hex_2_str = new String(hex_2);
        return hex_2_str.toUpperCase(Locale.getDefault());
    }

    public void SetMode(int mode) {
        mode_setting = mode;
    }
}
