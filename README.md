# SWB BAAT CRT PRO (cAlgo Port)

This repository contains a cTrader/cAlgo version of the MT5 panel-style indicator concept.

Overview:
- 5 timeframe groups: 15M, 30M, 1H, 4H, D1
- 3 candles per group
- SMASH toggle: candlesticks vs OHLC bar mode
- PO3 open line
- draggable panel and resize handle
- centered placement by default

Files:
- `SWBBAATCRTPro.cs` — cAlgo indicator source

How to use:
1. Open cTrader and create a new cAlgo indicator.
2. Paste the contents of `SWBBAATCRTPro.cs` into the editor.
3. Compile the indicator.
4. Attach it to a chart.

Notes:
- This is a cTrader port of the MT5 logic and intentionally stays close to the original structure.
- cTrader object capabilities differ from MT5, so some interactions are approximated using drawn objects and text overlays.
- If you want a deeper 1:1 cTrader implementation with mouse drag/resize events, a second pass can be built to make interaction more faithful to the original intent.
