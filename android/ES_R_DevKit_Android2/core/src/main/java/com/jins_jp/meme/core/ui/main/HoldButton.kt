package com.jins_jp.meme.core.ui.main

import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.tween
import androidx.compose.foundation.gestures.awaitEachGesture
import androidx.compose.foundation.gestures.awaitFirstDown
import androidx.compose.foundation.gestures.waitForUpOrCancellation
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.IntRect
import androidx.compose.ui.unit.IntSize
import androidx.compose.ui.unit.LayoutDirection
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Popup
import androidx.compose.ui.window.PopupPositionProvider
import androidx.compose.ui.window.PopupProperties
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.delay
import kotlin.time.Duration.Companion.seconds

@Composable
internal fun HoldButton(
    text: String,
    hintText: String,
    enabled: Boolean,
    onConfirmed: () -> Unit,
    modifier: Modifier = Modifier,
    holdMs: Int = 700,
) {
    val progress = remember { Animatable(0f) }
    var holding by remember { mutableStateOf(false) }
    var hintTrigger by remember { mutableIntStateOf(0) }
    var showHint by remember { mutableStateOf(false) }

    LaunchedEffect(holding, enabled) {
        if (holding && enabled) {
            progress.snapTo(0f)
            try {
                progress.animateTo(
                    targetValue = 1f,
                    animationSpec = tween(durationMillis = holdMs, easing = LinearEasing),
                )
                onConfirmed()
            } catch (_: CancellationException) {
                // released early; the snap-back is handled in the else branch.
            }
        } else {
            progress.animateTo(0f, tween(durationMillis = 150))
        }
    }

    LaunchedEffect(hintTrigger) {
        if (hintTrigger > 0) {
            showHint = true
            delay(2.seconds)
            showHint = false
        }
    }

    val overlayColor = Color.Black.copy(alpha = 0.25f)
    val positionProvider = remember {
        object : PopupPositionProvider {
            override fun calculatePosition(
                anchorBounds: IntRect,
                windowSize: IntSize,
                layoutDirection: LayoutDirection,
                popupContentSize: IntSize,
            ): IntOffset {
                val x = anchorBounds.left +
                        (anchorBounds.width - popupContentSize.width) / 2
                val y = anchorBounds.top - popupContentSize.height - 16
                return IntOffset(x, y)
            }
        }
    }

    Box(modifier = modifier) {
        Button(
            onClick = {},
            enabled = enabled,
            contentPadding = PaddingValues(0.dp),
            modifier = Modifier
                .fillMaxWidth()
                .height(40.dp)
                .pointerInput(enabled) {
                    if (!enabled) return@pointerInput
                    awaitEachGesture {
                        awaitFirstDown(requireUnconsumed = false)
                        val downTime = System.currentTimeMillis()
                        holding = true
                        try {
                            waitForUpOrCancellation()
                        } finally {
                            val shortTap = System.currentTimeMillis() - downTime < holdMs
                            // Reset even when pointerInput is cancelled mid-press
                            // (e.g. `enabled` flips false while isStarting is true)
                            // so that a stale `holding = true` doesn't immediately
                            // re-arm the timer once `enabled` returns to true.
                            holding = false
                            if (shortTap) hintTrigger++
                        }
                    }
                },
        ) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .drawBehind {
                        if (progress.value > 0f) {
                            drawRect(
                                color = overlayColor,
                                topLeft = Offset.Zero,
                                size = Size(size.width * progress.value, size.height),
                            )
                        }
                    },
                contentAlignment = Alignment.Center,
            ) {
                Text(text, modifier = Modifier.padding(horizontal = 16.dp))
            }
        }
        if (showHint) {
            Popup(
                popupPositionProvider = positionProvider,
                properties = PopupProperties(focusable = false),
            ) {
                Surface(
                    color = Color(0xFF323232),
                    shape = RoundedCornerShape(6.dp),
                    shadowElevation = 4.dp,
                ) {
                    Text(
                        text = hintText,
                        color = Color.White,
                        style = MaterialTheme.typography.bodySmall,
                        modifier = Modifier.padding(horizontal = 12.dp, vertical = 8.dp),
                    )
                }
            }
        }
    }
}
