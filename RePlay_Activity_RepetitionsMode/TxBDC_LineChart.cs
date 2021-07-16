using Microcharts;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace RePlay_Activity_RepetitionsMode
{
    public class TxBDC_LineChart : LineChart
    {
        #region Properties

        public List<TxBDC_BarChart_HorizontalLineAnnotation> HorizontalLineAnnotations { get; set; } = new List<TxBDC_BarChart_HorizontalLineAnnotation>();

        public List<TxBDC_VerticalLineAnnotation> VerticalLineAnnotations { get; set; } = new List<TxBDC_VerticalLineAnnotation>();

        #endregion

        #region Constructor

        public TxBDC_LineChart ()
            : base()
        {
            //empty
        }

        #endregion

        #region Overrides

        public override void DrawContent(SKCanvas canvas, int width, int height)
        {
            var valueLabelSizes = MeasureValueLabels();
            var footerHeight = CalculateFooterHeight(valueLabelSizes);
            var headerHeight = CalculateHeaderHeight(valueLabelSizes);
            var itemSize = CalculateItemSize(width, height, footerHeight, headerHeight);
            var origin = CalculateYOrigin(itemSize.Height, headerHeight);
            var points = CalculatePoints(itemSize, origin, headerHeight);

            DrawArea(canvas, points, itemSize, origin);
            TxBDC_DrawLine(canvas, points, itemSize);

            DrawFooter(canvas, points, itemSize, height, footerHeight);
            DrawLineAnnotations(canvas, width, height, footerHeight, headerHeight, itemSize);
        }

        public virtual void DrawLineAnnotations(SKCanvas canvas, float width, float height, float footerHeight, float headerHeight, SKSize itemSize)
        {
            float value_range = MaxValue - MinValue;
            float itemSizeHeight = height - Margin - footerHeight - headerHeight;

            for (int i = 0; i < HorizontalLineAnnotations.Count; i++)
            {
                var this_line_annotation = HorizontalLineAnnotations[i];

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

            for (int i = 0; i < VerticalLineAnnotations.Count; i++)
            {
                var this_line_annotation = VerticalLineAnnotations[i];

                using (var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = this_line_annotation.LineColor,
                    StrokeWidth = this_line_annotation.LineThickness,
                    IsAntialias = true,
                    PathEffect = SKPathEffect.CreateDash(new[] { 5f, 5f }, 0)
                })
                {
                    var x = Margin + (itemSize.Width / 2) + (this_line_annotation.X_Value * (itemSize.Width + Margin));
                    var y0 = headerHeight + (((MaxValue - 0) / value_range) * itemSizeHeight);
                    var y1 = headerHeight;

                    var path = new SKPath();
                    path.MoveTo(new SKPoint(x, y0));
                    path.LineTo(new SKPoint(x, y1));
                    canvas.DrawPath(path, paint);
                }
            }
        }

        protected void TxBDC_DrawLine(SKCanvas canvas, SKPoint[] points, SKSize itemSize)
        {
            if (points.Length > 1 && this.LineMode != LineMode.None)
            {
                using (var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.White,
                    StrokeWidth = this.LineSize,
                    IsAntialias = true,
                })
                {
                    using (var shader = this.CreateGradient(points))
                    {
                        paint.Shader = shader;

                        foreach (var point in points)
                        {
                            canvas.DrawPoint(point, SKColors.ForestGreen);
                            //canvas.DrawPoint(point, SKColors.ForestGreen, 2f, PointMode.Circle);
                        }
                    }
                }
            }
        }

        private SKShader CreateGradient(SKPoint[] points, byte alpha = 255)
        {
            var startX = points.First().X;
            var endX = points.Last().X;
            var rangeX = endX - startX;

            return SKShader.CreateLinearGradient(
                new SKPoint(startX, 0),
                new SKPoint(endX, 0),
                this.Entries.Select(x => x.Color.WithAlpha(alpha)).ToArray(),
                null,
                SKShaderTileMode.Clamp);
        }

        #endregion
    }
}
