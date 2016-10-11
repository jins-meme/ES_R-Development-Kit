
package com.jins_jp.meme.academic;

import android.animation.Animator;
import android.animation.AnimatorSet;
import android.animation.ObjectAnimator;
import android.annotation.SuppressLint;
import android.content.Intent;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.content.pm.PackageManager.NameNotFoundException;
import android.os.Bundle;
import android.os.Handler;
import android.support.v7.app.AppCompatActivity;
import android.view.KeyEvent;
import android.view.View;
import android.view.View.OnClickListener;
import android.view.View.OnLongClickListener;
import android.widget.CompoundButton;
import android.widget.CompoundButton.OnCheckedChangeListener;
import android.widget.RelativeLayout;
import android.widget.Switch;
import android.widget.TextView;

import java.util.ArrayList;
import java.util.List;
import java.util.Timer;
import java.util.TimerTask;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;

public class SplashActivity extends AppCompatActivity implements OnClickListener {

    Handler handler = new Handler();
    //
    private RelativeLayout mViewMask;
    private RelativeLayout mViewMain;
    private Switch switchModeView;

    boolean isUse = true;
    boolean isBle = false;

    @SuppressLint("NewApi")
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        // set view
        getWindow().getDecorView().setSystemUiVisibility(View.SYSTEM_UI_FLAG_LOW_PROFILE);
        setContentView(R.layout.activity_splash);
        // set view
        setViewVersion();
        // set view
        mViewMask = (RelativeLayout) findViewById(R.id.view_mask);
        mViewMain = (RelativeLayout) findViewById(R.id.view_main);
        //mViewMain.setOnClickListener(new OnClickListener() {
        //    @Override
        //    public void onClick(View v) {
                // start activity
        //        Class<?> cls = isUse ? MainUsbActivity.class : MainBleActivity.class;
        //        Intent intent = new Intent(getApplicationContext(), cls);
        //        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TOP);
        //        startActivity(intent);
        //        intent = null;
        //        finish();
        //    }
        //});
        switchModeView = (Switch) findViewById(R.id.switch_use);
        switchModeView.setChecked(isUse);
        switchModeView.setOnCheckedChangeListener(new OnCheckedChangeListener() {
            @Override
            public void onCheckedChanged(CompoundButton buttonView, boolean isChecked) {
                isUse = isChecked;
            }
        });
        switchModeView.setVisibility(View.INVISIBLE);

        TextView textView = (TextView) findViewById(R.id.text_app_version);
        textView.setOnLongClickListener(new OnLongClickListener() {
            @Override
            public boolean onLongClick(View v) {
                if (isBle)
                    switchModeView.setVisibility(View.VISIBLE);
                Timer timer = new Timer(true);
                timer.schedule(new TimerTask() {
                    @Override
                    public void run() {
                        handler.post(new Runnable() {
                            @Override
                            public void run() {
                                switchModeView.setVisibility(View.INVISIBLE);
                            }
                        });
                    }
                }, 2500L);
                return true;
            }
        });

        findViewById(R.id.button_usb).setOnClickListener(this);
        findViewById(R.id.button_ble).setOnClickListener(this);
    }

    @Override
    protected void onStart() {
        super.onStart();
        // invisible mask view
        mViewMask.setVisibility(View.INVISIBLE);
    }

    @Override
    protected void onResume() {
        super.onResume();
        // enable clickable
        mViewMain.setClickable(true);
        // start flash
        setViewFlash(true);
    }

    @Override
    protected void onPause() {
        super.onPause();
        // disable clickable
        mViewMain.setClickable(false);
        // stop flash
        setViewFlash(false);
    }

    @Override
    protected void onStop() {
        super.onStop();
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        // release object
        mViewMain = null;
        mViewMask = null;
        // run gc
        System.gc();
    }

    @Override
    public boolean dispatchKeyEvent(KeyEvent event) {
        if (event.getAction() == KeyEvent.ACTION_DOWN) {
            switch (event.getKeyCode()) {
                case KeyEvent.KEYCODE_BACK:
                    return true;
                default:
                    break;
            }
        }
        return super.dispatchKeyEvent(event);
    }

    public void setViewFlash(final boolean enabled) {
        ScheduledExecutorService mExecutorService = Executors.newScheduledThreadPool(1);

        if (enabled) {
            final TextView msg = (TextView) findViewById(R.id.text_flash);
            mExecutorService.scheduleWithFixedDelay(new Runnable() {
                @Override
                public void run() {
                    Handler handler = new Handler();
                    handler.post(new Runnable() {
                        @Override
                        public void run() {
                            msg.setVisibility(View.VISIBLE);
                            animateAlpha();
                        }
                    });
                }

                private void animateAlpha() {
                    // list of Animator to run
                    List<Animator> animatorList = new ArrayList<Animator>();
                    // the alpha value from 0.2 to 1 over 800 msec to change
                    ObjectAnimator animeFadeIn = ObjectAnimator.ofFloat(msg, "alpha", 0.2f, 1f);
                    animeFadeIn.setDuration(800);
                    // the alpha value from 1 to 0.2 over 800 msec to change
                    ObjectAnimator animeFadeOut = ObjectAnimator.ofFloat(msg, "alpha", 1f, 0.2f);
                    animeFadeOut.setDuration(800);
                    // add to executed Animator list
                    animatorList.add(animeFadeIn);
                    animatorList.add(animeFadeOut);
                    final AnimatorSet animatorSet = new AnimatorSet();
                    // run to the order of the list
                    animatorSet.playSequentially(animatorList);
                    animatorSet.start();
                }

            }, 0, 1700, TimeUnit.MILLISECONDS);
        } else {
            if (mExecutorService != null && !mExecutorService.isShutdown()) {
                mExecutorService.shutdown();
            }
        }
    }

    private void setViewVersion() {
        // Set textView
        TextView textView = (TextView) findViewById(R.id.text_app_version);
        String versionName = "";
        try {
            PackageInfo packageInfo = getPackageManager().getPackageInfo(
                    getPackageName(), PackageManager.GET_META_DATA);
            versionName = getString(R.string.label_version, packageInfo.versionName);
            packageInfo = null;
        } catch (NameNotFoundException e) {
            e.printStackTrace();
        }
        textView.setText(versionName);
        versionName = null;
    }

    @Override
    public void onClick(View v) {

        Class<?> cls = MainUsbActivity.class;

        if (v.getId() == R.id.button_usb) {
            cls = MainUsbActivity.class;
        }
        else if (v.getId() == R.id.button_ble) {
            cls = MainBleActivity.class;
        }
        else {
            cls = MainUsbActivity.class;
        }
        Intent intent = new Intent(getApplicationContext(), cls);
        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TOP);
        startActivity(intent);
        intent = null;
        finish();
    }
}
