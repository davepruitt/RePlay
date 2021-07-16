using System;
using System.Collections.Generic;
using System.Linq;
using Microcharts;
using SkiaSharp;

namespace RePlay_Activity_RepetitionsMode
{
    public class TxBDC_BarChart : BarChart
    {
        #region Constructor

        public TxBDC_BarChart(bool flag)
            : base()
        {
            SinglePolarity = flag;
        }

        #endregion

        public double Baseline { get; set; } = 0;

        #region New stuff for the TxBDC version of the bar chart

        public SKColor SpecialBackgroundColor { get; set; } = SKColor.Empty;

        public bool UseSpecialBackgroundColor { get; set; } = false;

        public bool SinglePolarity { get; set; } = false;

        public List<TxBDC_BarChart_HorizontalLineAnnotation> LineAnnotations { get; set; } = new List<TxBDC_BarChart_HorizontalLineAnnotation>();

        public TxBDC_BarChart_HorizontalLineAnnotation ReturnThreshold { get; set; }

        public TxBDC_BarChart_HorizontalLineAnnotation HitThreshold { get; set; }

        public TxBDC_BarChart_HorizontalLineAnnotation NegReturnThreshold { get; set; }

        public TxBDC_BarChart_HorizontalLineAnnotation NegHitThreshold { get; set; }

        public virtual void DrawLineAnnotations(SKCanvas canvas, float width, float height, float footerHeight, float headerHeight)
        {
            float value_range = MaxValue - MinValue;
            float itemSizeHeight = height - Margin - footerHeight - headerHeight;
            
            for (int i = 0; i < LineAnnotations.Count; i++)
            {
                var this_line_annotation = LineAnnotations[i];

                using (var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = this_line_annotation.LineColor,
                    StrokeWidth = this_line_annotation.LineThickness,
                    IsAntialias = true
                })
                {
                    var x1 = Margin;
                    var x2 = width - Margin;
                    var y = headerHeight + (((MaxValue - this_line_annotation.Y_Value) / value_range) * itemSizeHeight);

                    var path = new SKPath();
                    path.MoveTo(new SKPoint(x1, y));
                    path.LineTo(new SKPoint(x2, y));
                    canvas.DrawPath(path, paint);
                }
            }
        }

        protected void TxBDC_DrawOtherAnnotations(SKCanvas canvas, float width, float height, float footerHeight, float headerHeight)
        {
            float value_range = MaxValue - MinValue;
            float itemSizeHeight = height - Margin - footerHeight - headerHeight;

            using (var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = HitThreshold.LineColor,
                StrokeWidth = HitThreshold.LineThickness,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new[] { 5f, 5f }, 0)
            })
            {
                var x1 = Margin;
                var x2 = width - Margin;
                var y = headerHeight + (((MaxValue - HitThreshold.Y_Value) / value_range) * itemSizeHeight);

                var path = new SKPath();
                path.MoveTo(new SKPoint(x1, y));
                path.LineTo(new SKPoint(x2, y));
                canvas.DrawPath(path, paint);
            }

            using (var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = ReturnThreshold.LineColor,
                StrokeWidth = ReturnThreshold.LineThickness,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new[] { 5f, 5f }, 0)
            })
            {
                var x1 = Margin;
                var x2 = width - Margin;
                var y = headerHeight + (((MaxValue - ReturnThreshold.Y_Value) / value_range) * itemSizeHeight);

                var path = new SKPath();
                path.MoveTo(new SKPoint(x1, y));
                path.LineTo(new SKPoint(x2, y));
                canvas.DrawPath(path, paint);
            }
        }

        protected void TxBDC_DrawNegativeAnnotations(SKCanvas canvas, float width, float height, float footerHeight, float headerHeight)
        {
            float value_range = MaxValue - MinValue;
            float itemSizeHeight = height - Margin - footerHeight - headerHeight;

            using (var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = HitThreshold.LineColor,
                StrokeWidth = HitThreshold.LineThickness,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new[] { 5f, 5f }, 0)
            })
            {
                var x1 = Margin;
                var x2 = width - Margin;
                var y = headerHeight + (((MaxValue - NegHitThreshold.Y_Value) / value_range) * itemSizeHeight);

                var path = new SKPath();
                path.MoveTo(new SKPoint(x1, y));
                path.LineTo(new SKPoint(x2, y));
                canvas.DrawPath(path, paint);
            }

            using (var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = ReturnThreshold.LineColor,
                StrokeWidth = ReturnThreshold.LineThickness,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new[] { 5f, 5f }, 0)
            })
            {
                var x1 = Margin;
                var x2 = width - Margin;
                var y = headerHeight + (((MaxValue - NegReturnThreshold.Y_Value) / value_range) * itemSizeHeight);

                var path = new SKPath();
                path.MoveTo(new SKPoint(x1, y));
                path.LineTo(new SKPoint(x2, y));
                canvas.DrawPath(path, paint);
            }
        }

        protected void TxBDC_DrawBarAreas(SKCanvas canvas, SKPoint[] points, SKSize itemSize, float headerHeight)
        {
            if (points.Length > 0 && PointAreaAlpha > 0)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    var entry = Entries.ElementAt(i);
                    var point = points[i];

                    using (var paint = new SKPaint
                    {
                        Style = SKPaintStyle.Fill,
                        Color = (UseSpecialBackgroundColor) ? SpecialBackgroundColor.WithAlpha(BarAreaAlpha) : entry.Color.WithAlpha(BarAreaAlpha),
                    })
                    {
                        var max = entry.Value > 0 ? headerHeight : headerHeight + itemSize.Height;
                        var height = Math.Abs(max - point.Y);
                        var y = Math.Min(max, point.Y);
                        canvas.DrawRect(SKRect.Create(point.X - (itemSize.Width / 2), headerHeight, itemSize.Width, itemSize.Height), paint);
                    }
                }
            }
        }

        protected virtual void TxBDC_DrawAxisLimitLabels(SKCanvas canvas, float width, float height, float footerHeight, float headerHeight)
        {
            float value_range = MaxValue - MinValue;
            float itemSizeHeight = height - Margin - footerHeight - headerHeight;

            using (var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Gray,
                TextSize = LabelTextSize,
                IsAntialias = true,
                IsStroke = false
            })
            {
                var top_of_bar_chart = headerHeight;
                var bottom_of_bar_chart = headerHeight + itemSizeHeight + LabelTextSize;

                canvas.DrawText(Convert.ToInt32(MaxValue).ToString(), 0, top_of_bar_chart, paint);
                canvas.DrawText(Convert.ToInt32(MinValue).ToString(), 0, bottom_of_bar_chart, paint);
            }
        }

        public override void DrawContent(SKCanvas canvas, int width, int height)
        {
            //All of this is just copied from the base method
            var valueLabelSizes = MeasureValueLabels();
            var footerHeight = CalculateFooterHeight(valueLabelSizes);
            var headerHeight = CalculateHeaderHeight(valueLabelSizes);
            var itemSize = CalculateItemSize(width, height, footerHeight, headerHeight);
            var origin = CalculateYOrigin(itemSize.Height, headerHeight);
            var points = CalculatePoints(itemSize, origin, headerHeight);

            TxBDC_DrawBarAreas(canvas, points, itemSize, headerHeight);
            DrawBars(canvas, points, itemSize, origin, headerHeight);
            DrawPoints(canvas, points);
            DrawFooter(canvas, points, itemSize, height, footerHeight);

            //This is new
            DrawLineAnnotations(canvas, width, height, footerHeight, headerHeight);
            TxBDC_DrawOtherAnnotations(canvas, width, height, footerHeight, headerHeight);
            if (!SinglePolarity) TxBDC_DrawNegativeAnnotations(canvas, width, height, footerHeight, headerHeight);
            // Drawing bar chart values
            //TxBDC_DrawAxisLimitLabels(canvas, width, height, footerHeight, headerHeight);
        }

        #endregion
    }
}