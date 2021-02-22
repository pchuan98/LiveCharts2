﻿// The MIT License(MIT)

// Copyright(c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using LiveChartsCore.Context;
using LiveChartsCore.Drawing;
using System;
using System.Collections.Generic;

namespace LiveChartsCore
{
    /// <summary>
    /// Defines the data to plot as columns.
    /// </summary>
    public class ColumnSeries<TModel, TVisual, TDrawingContext> : Series<TModel, TVisual, TDrawingContext>
        where TVisual : ISizedGeometry<TDrawingContext>, IHighlightableGeometry<TDrawingContext>, new()
        where TDrawingContext : DrawingContext
    {
        public ColumnSeries()
            : base (SeriesProperties.Bar | SeriesProperties.VerticalOrientation)
        {

        }

        public double Pivot { get; set; }
        public double MaxColumnWidth { get; set; } = 30;
        public bool IgnoresColumnPosition { get; set; } = false;
        public TransitionsSetterDelegate<ISizedGeometry<TDrawingContext>> TransitionsSetter { get; set; }

        public override void Measure(
            CartesianChartCore<TDrawingContext> chart, IAxis<TDrawingContext> xAxis, IAxis<TDrawingContext> yAxis)
        {
            var drawLocation = chart.DrawMaringLocation;
            var drawMarginSize = chart.DrawMarginSize;
            var xScale = new ScaleContext(drawLocation, drawMarginSize, xAxis.Orientation, xAxis.DataBounds);
            var yScale = new ScaleContext(drawLocation, drawMarginSize, yAxis.Orientation, yAxis.DataBounds);

            float uw = xScale.ScaleToUi(1f) - xScale.ScaleToUi(0f);
            float uwm = 0.5f * uw;
            float sw = Stroke?.StrokeWidth ?? 0;
            float p = yScale.ScaleToUi(unchecked((float)Pivot));
             
            var pos = chart.SeriesContext.GetColumnPostion(this);
            var count = chart.SeriesContext.GetColumnSeriesCount();
            float cp = 0f;

            if (!IgnoresColumnPosition && count > 1)
            {
                uw = uw / count;
                uwm = 0.5f * uw;
                cp = (pos - (count/2f)) * uw + uwm;
            }

            if (uw > MaxColumnWidth)
            {
                uw = unchecked((float)MaxColumnWidth);
                uwm = uw / 2f;
            }

            if (Fill != null) chart.Canvas.AddPaintTask(Fill);
            if (Stroke != null) chart.Canvas.AddPaintTask(Stroke);

            var chartAnimation = new Animation(chart.EasingFunction, chart.AnimationsSpeed);
            var ts = TransitionsSetter ?? SetDefaultTransitions;

            foreach (var point in Fetch(chart))
            {
                var x = xScale.ScaleToUi(point.X);
                var y = yScale.ScaleToUi(point.Y);
                float b = Math.Abs(y - p);

                if (point.Visual == null)
                {
                    var r = new TVisual
                    {
                        X = x - uwm + cp,
                        Y = p,
                        Width = uw,
                        Height = 0
                    };

                    ts(r, chartAnimation);

                    point.HoverArea = new HoverArea();
                    point.Visual = r;
                    if (Fill != null) Fill.AddGeometyToPaintTask(r);
                    if (Stroke != null) Stroke.AddGeometyToPaintTask(r);
                }

                var sizedGeometry = (TVisual)point.Visual;

                var cy = point.Y > Pivot ? y : y - b;

                sizedGeometry.X = x - uwm + cp;
                sizedGeometry.Y = cy;
                sizedGeometry.Width = uw;
                sizedGeometry.Height = b;

                point.HoverArea.SetDimensions(x - uwm + cp, cy, uw, b);
                OnPointMeasured(point, sizedGeometry);
                chart.MeasuredDrawables.Add(sizedGeometry);
            }

            if (HighlightFill != null) chart.Canvas.AddPaintTask(HighlightFill);
            if (HighlightStroke != null) chart.Canvas.AddPaintTask(HighlightStroke);
        }

        public override CartesianBounds GetBounds(
            CartesianChartCore<TDrawingContext> chart, IAxis<TDrawingContext> x, IAxis<TDrawingContext> y)
        {
            var baseBounds = base.GetBounds(chart, x, y);

            var tick = y.GetTick(chart.ControlSize, baseBounds.YAxisBounds);

            return new CartesianBounds
            {
                XAxisBounds = new Bounds
                {
                    Max = baseBounds.XAxisBounds.Max + 0.5,
                    Min = baseBounds.XAxisBounds.Min - 0.5
                },
                YAxisBounds = new Bounds
                {
                    Max = baseBounds.YAxisBounds.Max + tick.Value,
                    min = baseBounds.YAxisBounds.min - tick.Value
                }
            };
        }

        protected virtual void SetDefaultTransitions(ISizedGeometry<TDrawingContext> visual, Animation defaultAnimation)
        {
            var defaultProperties = new string[]
            {
                nameof(visual.X),
                nameof(visual.Width)
            };
            visual.SetPropertyTransition(defaultAnimation, defaultProperties);
            visual.CompleteTransition(defaultProperties);

            var bounceProperties = new string[]
            {
                nameof(visual.Y),
                nameof(visual.Height),
            };
            visual.SetPropertyTransition(
                new Animation(EasingFunctions.BounceOut, (long)(defaultAnimation.duration * 1.5), defaultAnimation.RepeatTimes),
                bounceProperties);
            visual.CompleteTransition(bounceProperties);
        }

        public override int GetStackGroup() => 0;
    }
}