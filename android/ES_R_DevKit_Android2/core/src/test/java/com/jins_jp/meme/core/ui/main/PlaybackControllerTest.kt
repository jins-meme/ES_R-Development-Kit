package com.jins_jp.meme.core.ui.main

import android.net.Uri
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.test.TestScope
import kotlinx.coroutines.test.advanceUntilIdle
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.mockito.Mockito

/**
 * [PlaybackController] の検証: 再生はグラフ画面のページが受け持つので、コントローラは
 * 「ページに CSV を読ませる」「終わりで再生元へ書き戻す」だけをする。
 */
@OptIn(ExperimentalCoroutinesApi::class)
class PlaybackControllerTest {

    private val repo = FakeMemeBleClient()
    private val ui = MutableStateFlow(MainUiState())
    private var stopMeasurementCount = 0
    private val opened = mutableListOf<Pair<Uri, String>>()
    private var closed = 0
    private val merged = mutableListOf<Uri>()

    // メソッドは呼ばれない（ラムダへ素通しされるだけ）ので mock で足りる。
    private val uri: Uri = Mockito.mock(Uri::class.java)
    private val uri2: Uri = Mockito.mock(Uri::class.java)

    private fun TestScope.newController(): PlaybackController {
        val reconnect = ReconnectController(
            scope = this,
            repo = repo,
            ui = ui,
            onSuppressAutoConnect = {},
            restartMeasurement = {},
            stopMeasurementService = {},
        )
        return PlaybackController(
            scope = this,
            ui = ui,
            reconnect = reconnect,
            stopMeasurement = { stopMeasurementCount++ },
            displayName = { if (it === uri) "a.csv.gz" else "b.csv" },
            openReplay = { u, name -> opened += u to name },
            closeReplay = { closed++ },
            mergeLabels = { merged += it },
        )
    }

    @Test
    fun nullUriMeansCancelledDialogAndDoesNothing() = runTest {
        newController().start(null)
        advanceUntilIdle()
        assertTrue(opened.isEmpty())
        assertFalse(ui.value.isReplaying)
    }

    @Test
    fun startOpensTheFileInThePage() = runTest {
        val c = newController()
        c.start(uri)
        advanceUntilIdle()
        assertEquals(listOf(uri to "a.csv.gz"), opened)
        assertTrue(ui.value.isReplaying)
        assertEquals("a.csv.gz", ui.value.replayName)
        assertEquals(uri, c.sourceUri)
    }

    @Test
    fun startStopsARunningMeasurementFirst() = runTest {
        ui.update { it.copy(isMeasuring = true) }
        newController().start(uri)
        advanceUntilIdle()
        assertEquals(1, stopMeasurementCount)
    }

    @Test
    fun exitClosesTheReplayAndWritesBackToTheSource() = runTest {
        val c = newController()
        c.start(uri)
        c.exit()
        advanceUntilIdle()
        assertEquals(1, closed)
        assertEquals(listOf(uri), merged)
        assertFalse(ui.value.isReplaying)
        assertNull(ui.value.replayName)
        assertNull(c.sourceUri)
    }

    @Test
    fun exitWithoutReplayDoesNothing() = runTest {
        newController().exit()
        advanceUntilIdle()
        assertEquals(0, closed)
        assertTrue(merged.isEmpty())
    }

    @Test
    fun choosingAnotherFileWritesBackThePreviousOne() = runTest {
        val c = newController()
        c.start(uri)
        c.start(uri2)
        advanceUntilIdle()
        assertEquals(listOf(uri), merged)
        assertEquals(listOf(uri to "a.csv.gz", uri2 to "b.csv"), opened)
        assertEquals(uri2, c.sourceUri)
        assertTrue(ui.value.isReplaying)
    }
}
