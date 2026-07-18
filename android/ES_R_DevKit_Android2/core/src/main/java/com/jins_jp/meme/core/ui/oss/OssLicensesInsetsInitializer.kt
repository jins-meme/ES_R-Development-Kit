package com.jins_jp.meme.core.ui.oss

import android.app.Activity
import android.app.Application
import android.content.ContentProvider
import android.content.ContentValues
import android.database.Cursor
import android.net.Uri
import android.os.Bundle
import android.util.TypedValue
import android.view.View
import androidx.core.view.ViewCompat
import androidx.core.view.WindowInsetsCompat

/**
 * play-services-oss-licenses の OssLicensesMenuActivity / OssLicensesActivity は
 * WindowInsets 非対応の旧レイアウトのため、エッジトゥエッジが強制される端末
 * (Android 15+ で windowOptOutEdgeToEdgeEnforcement が無視される OS 世代)では
 * ライセンス一覧/本文がコンテンツルートいっぱいに描画され、ActionBar や
 * ステータス/ナビゲーションバーに重なってしまう。
 *
 * ライブラリのレイアウトは編集できず、アプリの Application も触りたくないので、
 * ContentProvider の onCreate で ActivityLifecycleCallbacks を自動登録し、
 * 該当 Activity のコンテンツルート(android.R.id.content)へシステムバー +
 * ActionBar 分の padding を当てて重なりを解消する。
 *
 * エッジトゥエッジでない端末(= top インセットがコンテンツまで伝播しない)では
 * padding が 0 になり従来表示を壊さない。
 */
class OssLicensesInsetsInitializer : ContentProvider() {

    override fun onCreate(): Boolean {
        val app = context?.applicationContext as? Application ?: return false
        app.registerActivityLifecycleCallbacks(Callbacks)
        return true
    }

    private object Callbacks : Application.ActivityLifecycleCallbacks {
        override fun onActivityCreated(activity: Activity, savedInstanceState: Bundle?) {
            if (!activity.javaClass.name.startsWith("com.google.android.gms.oss.licenses.")) return
            val content = activity.findViewById<View>(android.R.id.content) ?: return

            val actionBarSize = TypedValue().let { tv ->
                if (activity.theme.resolveAttribute(android.R.attr.actionBarSize, tv, true)) {
                    TypedValue.complexToDimensionPixelSize(tv.data, activity.resources.displayMetrics)
                } else {
                    0
                }
            }

            ViewCompat.setOnApplyWindowInsetsListener(content) { v, insets ->
                val bars = insets.getInsets(WindowInsetsCompat.Type.systemBars())
                // top インセットが伝播している = エッジトゥエッジでコンテンツが
                // ActionBar 背面まで伸びている状態。ActionBar の高さも足して下げる。
                val extraTop = if (bars.top > 0) actionBarSize else 0
                v.setPadding(bars.left, bars.top + extraTop, bars.right, bars.bottom)
                insets
            }
            ViewCompat.requestApplyInsets(content)
        }

        override fun onActivityStarted(activity: Activity) {}
        override fun onActivityResumed(activity: Activity) {}
        override fun onActivityPaused(activity: Activity) {}
        override fun onActivityStopped(activity: Activity) {}
        override fun onActivitySaveInstanceState(activity: Activity, outState: Bundle) {}
        override fun onActivityDestroyed(activity: Activity) {}
    }

    override fun query(
        uri: Uri,
        projection: Array<out String>?,
        selection: String?,
        selectionArgs: Array<out String>?,
        sortOrder: String?,
    ): Cursor? = null

    override fun getType(uri: Uri): String? = null

    override fun insert(uri: Uri, values: ContentValues?): Uri? = null

    override fun delete(uri: Uri, selection: String?, selectionArgs: Array<out String>?): Int = 0

    override fun update(
        uri: Uri,
        values: ContentValues?,
        selection: String?,
        selectionArgs: Array<out String>?,
    ): Int = 0
}
