# ES_R DevKit Android 2

## Graph update rate

`MainViewModel.handleAcademia` only emits a `GraphEvent` when
`totalCount % graphSkipCount == 0`. `graphSkipCount` is chosen in
`startMeasurement` based on the transmission rate:

- **100 Hz mode**: `graphSkipCount = 4` → 100 ÷ 4 = **25 Hz (~40 ms period)**
- **50 Hz mode**: `graphSkipCount = 2` → 50 ÷ 2 = **25 Hz (~40 ms period)**

`MainScreen` increments `bumper` on every event to trigger recomposition, so
the on-screen redraw rate is capped at the same 25 Hz.

`GRAPH_LEN = 150` is derived from this fixed plot rate: 25 Hz × 6 s = 150
samples. Because both modes produce the same plot rate, the horizontal scale
stays identical regardless of the transmission speed.
