using System;
using System.Collections.Generic;
using cAlgo.API;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class SWBBAATCRTPro : Indicator
    {
        private const int GrpCount = 5;
        private const int LiveMinMs = 900;

        private const int ZPanel = 100;
        private const int ZWick = 200;
        private const int ZOpen = 250;
        private const int ZBody = 300;
        private const int ZLabel = 400;

        private const int ResizeSquareSize = 12;
        private const double ResizeMin = 0.5;
        private const double ResizeMax = 4.0;

        private readonly TimeFrame[] _tf =
        {
            TimeFrame.Minute15,
            TimeFrame.Minute30,
            TimeFrame.Hour1,
            TimeFrame.Hour4,
            TimeFrame.Day1
        };

        private readonly string[] _tfName = { "15M", "30M", "1H", "4H", "D1" };
        private readonly bool[] _enabled = new bool[GrpCount];
        private readonly bool[] _hasObj = new bool[GrpCount];
        private readonly DateTime[] _lastStart = new DateTime[GrpCount];
        private readonly Color[] _lastBody = new Color[GrpCount];
        private readonly double[] _hiEx = new double[GrpCount];
        private readonly double[] _scaleY = new double[GrpCount];
        private readonly double[] _minY = new double[GrpCount];
        private readonly double[] _maxY = new double[GrpCount];
        private readonly double[] _groupX = new double[GrpCount];
        private readonly List<string> _objNames = new List<string>();

        private bool _init;
        private bool _smash;
        private string _settings = "";
        private double _baseX = 10;
        private double _baseY = 10;
        private int _frameW;
        private int _frameH;
        private int _grpPad;
        private int _groupW;
        private int _groupH;
        private double _labelH;
        private int _candles = 3;
        private int _lastCorner = 4;
        private double _point = 0.00001;
        private long _lastLive = 0;
        private double _scaleX = 1.0;
        private double _scaleYScale = 1.0;

        [Parameter("Corner", DefaultValue = 4, MinValue = 0, MaxValue = 4)]
        public int InpCorner { get; set; }

        [Parameter("Margin X", DefaultValue = 20, MinValue = 0)]
        public int InpMarginX { get; set; }

        [Parameter("Margin Y", DefaultValue = 20, MinValue = 0)]
        public int InpMarginY { get; set; }

        [Parameter("Panel Background", DefaultValue = false)]
        public bool InpPanelBack { get; set; }

        [Parameter("Panel Color", DefaultValue = "White")]
        public Color InpPanelColor { get; set; }

        [Parameter("TF 15M", DefaultValue = true)]
        public bool InpTF15M { get; set; }

        [Parameter("TF 30M", DefaultValue = true)]
        public bool InpTF30M { get; set; }

        [Parameter("TF 1H", DefaultValue = true)]
        public bool InpTFH1 { get; set; }

        [Parameter("TF 4H", DefaultValue = true)]
        public bool InpTFH4 { get; set; }

        [Parameter("TF D1", DefaultValue = true)]
        public bool InpTFD1 { get; set; }

        [Parameter("Candles", DefaultValue = 3, MinValue = 1, MaxValue = 10)]
        public int InpCandles { get; set; }

        [Parameter("Group Height", DefaultValue = 120, MinValue = 20)]
        public int InpGroupHeight { get; set; }

        [Parameter("Candle Width", DefaultValue = 18, MinValue = 6)]
        public int InpCandleWidth { get; set; }

        [Parameter("Body Width", DefaultValue = 9, MinValue = 3)]
        public int InpBodyWidth { get; set; }

        [Parameter("Group Gap", DefaultValue = 8, MinValue = 2)]
        public int InpGroupGap { get; set; }

        [Parameter("Bull Color", DefaultValue = "DarkGreen")]
        public Color InpBullColor { get; set; }

        [Parameter("Bear Color", DefaultValue = "Red")]
        public Color InpBearColor { get; set; }

        [Parameter("Open Line", DefaultValue = true)]
        public bool InpShowOpenLine { get; set; }

        [Parameter("Open Color", DefaultValue = "DarkBlue")]
        public Color InpOpenColor { get; set; }

        [Parameter("Show TF Text", DefaultValue = true)]
        public bool InpShowTFText { get; set; }

        [Parameter("Show Time Text", DefaultValue = true)]
        public bool InpShowTimeText { get; set; }

        [Parameter("Time Offset Hours", DefaultValue = 0)]
        public int InpTimeOffset { get; set; }

        [Parameter("Text Size", DefaultValue = 8, MinValue = 6, MaxValue = 16)]
        public int InpTextSize { get; set; }

        [Parameter("Text Color", DefaultValue = "DarkGray")]
        public Color InpTextColor { get; set; }

        [Parameter("Border Color", DefaultValue = "Gray")]
        public Color InpBorderColor { get; set; }

        [Parameter("Show Resize", DefaultValue = true)]
        public bool InpShowResize { get; set; }

        protected override void Initialize()
        {
            for (int i = 0; i < GrpCount; i++)
            {
                _enabled[i] = i == 0 ? InpTF15M : i == 1 ? InpTF30M : i == 2 ? InpTFH1 : i == 3 ? InpTFH4 : InpTFD1;
                _hasObj[i] = false;
                _lastStart[i] = DateTime.MinValue;
                _lastBody[i] = Colors.Transparent;
            }

            _point = Symbol.PipSize <= 0 ? 0.00001 : Symbol.PipSize;
            _smash = false;
            _settings = string.Empty;
            _init = false;
            Refresh();
        }

        public override void Calculate(int index)
        {
            Refresh();
        }

        private string FrameName() => "SWB_PRO_CRT_frm";
        private string ObjName(int g, int i, string suffix) => "SWB_PRO_CRT_g" + g + "c" + i + suffix;
        private string OpenName(int g) => "SWB_PRO_CRT_g" + g + "opn";
        private string TFNameObj(int g) => "SWB_PRO_CRT_g" + g + "tf";
        private string TimeNameObj(int g) => "SWB_PRO_CRT_g" + g + "tm";
        private string ResizeName() => "SWB_PRO_CRT_rsz";
        private string SmashName() => "SWB_PRO_CRT_smash";

        private int GrpH() => Math.Max(30, (int)Math.Round(InpGroupHeight * _scaleYScale));
        private int CndW() => Math.Max(6, (int)Math.Round(InpCandleWidth * _scaleX));
        private int BdyW() => Math.Max(3, (int)Math.Round(InpBodyWidth * _scaleX));
        private int GrpGap() => Math.Max(2, (int)Math.Round(InpGroupGap * _scaleX));
        private int SmashW() => Math.Max(44, (int)Math.Round(58 * _scaleX));
        private int SmashH() => Math.Max(15, (int)Math.Round(17 * _scaleYScale));
        private int SmashZone() => SmashW() + 6;

        private void AddObj(string nm)
        {
            if (!_objNames.Contains(nm))
                _objNames.Add(nm);
        }

        private void DeleteAllObjects()
        {
            foreach (var nm in _objNames)
            {
                if (Chart.Objects.Exists(nm))
                    Chart.Objects.Remove(nm);
            }
            _objNames.Clear();
        }

        private string SettingsKey()
        {
            return string.Format(
                "{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}|{13}|{14}|{15}|{16}|{17}|{18}|{19}|{20}|{21}|{22}|{23}|{24}|{25}",
                InpCorner,
                InpMarginX,
                InpMarginY,
                InpCandles,
                InpGroupHeight,
                InpCandleWidth,
                InpBodyWidth,
                InpGroupGap,
                InpTextSize,
                InpTimeOffset,
                InpTF15M ? 1 : 0,
                InpTF30M ? 1 : 0,
                InpTFH1 ? 1 : 0,
                InpTFH4 ? 1 : 0,
                InpTFD1 ? 1 : 0,
                InpShowOpenLine ? 1 : 0,
                InpShowTFText ? 1 : 0,
                InpShowTimeText ? 1 : 0,
                InpPanelBack ? 1 : 0,
                InpBullColor.ToArgb(),
                InpBearColor.ToArgb(),
                InpOpenColor.ToArgb(),
                InpTextColor.ToArgb(),
                InpBorderColor.ToArgb(),
                InpPanelColor.ToArgb(),
                InpShowResize ? 1 : 0
            );
        }

        private double YOfPx(int g, double price)
        {
            var v = _minY[g] + (_hiEx[g] - price) * _scaleY[g];
            v = Math.Max(v, _minY[g]);
            v = Math.Min(v, _maxY[g]);
            return v;
        }

        private void DrawFrame()
        {
            var fname = FrameName();
            if (Chart.Objects.Exists(fname))
                Chart.Objects.Remove(fname);

            var frame = Chart.DrawRectangle(fname, _baseX, _baseY, _frameW, _frameH, InpPanelBack ? InpPanelColor : Colors.White, InpBorderColor, 1);
            frame.ZOrder = ZPanel;
            AddObj(fname);
        }

        private void DrawResizeHandle()
        {
            if (!InpShowResize)
                return;

            var name = ResizeName();
            if (Chart.Objects.Exists(name))
                Chart.Objects.Remove(name);

            var handle = Chart.DrawRectangle(name, _baseX + _frameW - ResizeSquareSize - 2, _baseY + _frameH - ResizeSquareSize - 2, ResizeSquareSize, ResizeSquareSize, Colors.Silver, Colors.DarkGray, 1);
            handle.ZOrder = ZPanel + 10;
            AddObj(name);
        }

        private void PaintSmash()
        {
            var name = SmashName();
            if (Chart.Objects.Exists(name))
                Chart.Objects.Remove(name);

            var text = _smash ? "SMASH: ON" : "SMASH: OFF";
            var x = _baseX + _frameW - SmashW() - 2;
            var y = _baseY;
            var obj = Chart.DrawText(name, text, x, y, Math.Max(7, InpTextSize + 1), Colors.Black);
            obj.ZOrder = ZLabel;
            AddObj(name);
        }

        private void PaintCandle(int group, int index, double cx, double yH, double yL, double yO, double yC, Color col)
        {
            if (_smash)
            {
                var wickName = ObjName(group, index, "wi");
                if (Chart.Objects.Exists(wickName))
                    Chart.Objects.Remove(wickName);
                var wick = Chart.DrawRectangle(wickName, cx, Math.Min(yH, yL), 1, Math.Max(1, Math.Abs(yH - yL)), col, col, 1);
                wick.ZOrder = ZWick;
                AddObj(wickName);

                var tk = Math.Max(2, (int)Math.Round(3 * _scaleX));
                var opName = ObjName(group, index, "op");
                if (Chart.Objects.Exists(opName))
                    Chart.Objects.Remove(opName);
                var openTick = Chart.DrawRectangle(opName, cx - tk, yO, tk, 1, col, col, 1);
                openTick.ZOrder = ZBody;
                AddObj(opName);

                var clName = ObjName(group, index, "cl");
                if (Chart.Objects.Exists(clName))
                    Chart.Objects.Remove(clName);
                var closeTick = Chart.DrawRectangle(clName, cx, yC, tk, 1, col, col, 1);
                closeTick.ZOrder = ZBody;
                AddObj(clName);
                return;
            }

            var wickNameBody = ObjName(group, index, "wi");
            if (Chart.Objects.Exists(wickNameBody))
                Chart.Objects.Remove(wickNameBody);
            var wick = Chart.DrawRectangle(wickNameBody, cx, Math.Min(yH, yL), 1, Math.Max(1, Math.Abs(yH - yL)), col, col, 1);
            wick.ZOrder = ZWick;
            AddObj(wickNameBody);

            var bodyName = ObjName(group, index, "bd");
            if (Chart.Objects.Exists(bodyName))
                Chart.Objects.Remove(bodyName);
            var bodyY = Math.Min(yO, yC);
            var bodyHeight = Math.Max(2, Math.Abs(yC - yO));
            var body = Chart.DrawRectangle(bodyName, cx - BdyW() / 2.0, bodyY, BdyW(), bodyHeight, col, col, 1);
            body.ZOrder = ZBody;
            AddObj(bodyName);
        }

        private void DrawGroup(int g, bool create = true)
        {
            var series = MarketData.GetSeries(_tf[g]);
            var count = Math.Min(_candles, series.Count);

            if (count < 1)
            {
                if (InpShowTFText)
                {
                    var name = TFNameObj(g);
                    if (Chart.Objects.Exists(name))
                        Chart.Objects.Remove(name);
                    var label = Chart.DrawText(name, _tfName[g], _baseX + _groupX[g], _baseY, InpTextSize, _enabled[g] ? InpOpenColor : Colors.DarkGray);
                    label.ZOrder = ZLabel;
                    AddObj(name);
                }
                return;
            }

            var low = double.MaxValue;
            var high = double.MinValue;

            for (int i = 0; i < count; i++)
            {
                var b = series.Last(i);
                low = Math.Min(low, b.Low);
                high = Math.Max(high, b.High);
            }

            var range = high - low;
            var floorR = _point * 4.0;
            if (range < floorR) range = floorR;

            _hiEx[g] = high;
            _scaleY[g] = (double)_groupH / range;
            _minY[g] = _labelH;
            _maxY[g] = _labelH + _groupH;

            for (int i = 0; i < count; i++)
            {
                var b = series.Last(i);
                double cx0 = _baseX + _groupX[g] + _grpPad + BdyW() / 2.0 + i * CndW();
                double yH = YOfPx(g, b.High);
                double yL = YOfPx(g, b.Low);
                double yO = YOfPx(g, b.Open);
                double yC = YOfPx(g, b.Close);
                var col = b.Close >= b.Open ? InpBullColor : InpBearColor;
                if (i == count - 1) _lastBody[g] = col;
                PaintCandle(g, i, cx0, yH, yL, yO, yC, col);
            }

            if (InpShowOpenLine && !_smash)
            {
                var lastBar = series.Last(0);
                var yOpen = YOfPx(g, lastBar.Open);
                var name = OpenName(g);
                if (Chart.Objects.Exists(name))
                    Chart.Objects.Remove(name);
                var line = Chart.DrawRectangle(name, _baseX + _groupX[g], yOpen, _groupW, 1, InpOpenColor, InpOpenColor, 1);
                line.ZOrder = ZOpen;
                AddObj(name);
            }

            if (InpShowTFText)
            {
                var name = TFNameObj(g);
                if (Chart.Objects.Exists(name))
                    Chart.Objects.Remove(name);
                var label = Chart.DrawText(name, _tfName[g], _baseX + _groupX[g], _baseY, InpTextSize, _enabled[g] ? InpOpenColor : Colors.DarkGray);
                label.ZOrder = ZLabel;
                AddObj(name);
            }

            if (InpShowTimeText)
            {
                var name = TimeNameObj(g);
                if (Chart.Objects.Exists(name))
                    Chart.Objects.Remove(name);
                var lastBar = series.Last(0);
                var timeText = lastBar.Time.AddHours(InpTimeOffset).ToString("HH:mm");
                var label = Chart.DrawText(name, timeText, _baseX + _groupX[g], _baseY + _maxY[g] + 2, InpTextSize, InpTextColor);
                label.ZOrder = ZLabel;
                AddObj(name);
            }
        }

        private void UpdateLive(int g)
        {
            var series = MarketData.GetSeries(_tf[g]);
            if (series.Count < 1)
                return;

            var lastBar = series.Last(0);
            var cx0 = _baseX + _groupX[g] + _grpPad + BdyW() / 2.0 + 0 * CndW();
            var yH = YOfPx(g, lastBar.High);
            var yL = YOfPx(g, lastBar.Low);
            var yO = YOfPx(g, lastBar.Open);
            var yC = YOfPx(g, lastBar.Close);
            var col = lastBar.Close >= lastBar.Open ? InpBullColor : InpBearColor;

            if (_lastBody[g] != col)
                _lastBody[g] = col;

            PaintCandle(g, 0, cx0, yH, yL, yO, yC, col);
        }

        private void CalcDims()
        {
            _grpPad = Math.Max(2, (CndW() - BdyW()) / 2);
            _groupW = _grpPad + (_candles - 1) * CndW() + BdyW();
            _groupH = GrpH();
            _labelH = Math.Max(InpShowTFText ? 20 : 0, SmashH() + 2);

            double cur = 0;
            for (int g = 0; g < GrpCount; g++)
            {
                if (!_enabled[g]) continue;
                _groupX[g] = cur;
                cur += _groupW + GrpGap();
            }

            if (cur > 0)
                cur -= GrpGap();

            _frameW = (int)(cur + 14 + SmashZone());
            _frameH = (int)(_labelH + _groupH + (InpShowTimeText ? (InpTextSize + 2) : 0) + 8);
        }

        private void RebuildAll()
        {
            DeleteAllObjects();

            for (int g = 0; g < GrpCount; g++)
            {
                _hasObj[g] = false;
                _lastStart[g] = DateTime.MinValue;
                _lastBody[g] = Colors.Transparent;
            }

            _candles = Math.Max(1, Math.Min(10, InpCandles));
            CalcDims();

            var corner = PanelCorner();
            var chartW = Chart.Width;
            var chartH = Chart.Height;

            double nbX = InpMarginX;
            double nbY = InpMarginY;

            if (corner == 1)
            {
                nbX = chartW - _frameW - InpMarginX;
            }
            else if (corner == 2)
            {
                nbY = chartH - _frameH - InpMarginY;
            }
            else if (corner == 3)
            {
                nbX = chartW - _frameW - InpMarginX;
                nbY = chartH - _frameH - InpMarginY;
            }
            else if (corner == 4)
            {
                nbX = (chartW - _frameW) / 2.0;
                nbY = (chartH - _frameH) / 2.0;
            }

            if (chartW >= _frameW + 6)
                nbX = Math.Min(nbX, chartW - _frameW - 6);
            if (chartH >= _frameH + 6)
                nbY = Math.Min(nbY, chartH - _frameH - 6);

            nbX = Math.Max(nbX, 2);
            nbY = Math.Max(nbY, 2);

            _baseX = nbX;
            _baseY = nbY;
            _lastCorner = corner;

            DrawFrame();
            DrawResizeHandle();
            PaintSmash();

            for (int g = 0; g < GrpCount; g++)
            {
                if (_enabled[g])
                {
                    DrawGroup(g);
                    _hasObj[g] = true;
                }
            }

            _init = true;
            Chart.Redraw();
        }

        private int PanelCorner()
        {
            var c = InpCorner;
            if (c < 0 || c > 4) c = 4;
            return c;
        }

        private bool GroupObjectsExist(int g)
        {
            if (!Chart.Objects.Exists(FrameName()))
                return false;

            for (int i = 0; i < _candles; i++)
            {
                if (!Chart.Objects.Exists(ObjName(g, i, "wi")))
                    return false;

                if (_smash)
                {
                    if (!Chart.Objects.Exists(ObjName(g, i, "op")))
                        return false;
                    if (!Chart.Objects.Exists(ObjName(g, i, "cl")))
                        return false;
                }
                else if (!Chart.Objects.Exists(ObjName(g, i, "bd")))
                    return false;
            }

            return true;
        }

        private void Refresh()
        {
            var key = SettingsKey();
            if (key != _settings)
            {
                _settings = key;
                RebuildAll();
                return;
            }

            if (!_init)
            {
                RebuildAll();
                return;
            }

            var changed = false;
            for (int g = 0; g < GrpCount; g++)
            {
                if (!_enabled[g]) continue;
                var series = MarketData.GetSeries(_tf[g]);
                if (series.Count < 1) continue;
                var timeNew = series.Last(0).Time;
                if (timeNew != _lastStart[g] || !_hasObj[g] || !GroupObjectsExist(g))
                {
                    DrawGroup(g);
                    _lastStart[g] = timeNew;
                    changed = true;
                }
            }

            if (changed)
            {
                Chart.Redraw();
                return;
            }

            var nowMs = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            if (nowMs - _lastLive >= LiveMinMs)
            {
                _lastLive = nowMs;
                for (int g = 0; g < GrpCount; g++)
                {
                    if (_enabled[g])
                        UpdateLive(g);
                }
                Chart.Redraw();
            }
        }
    }
}

