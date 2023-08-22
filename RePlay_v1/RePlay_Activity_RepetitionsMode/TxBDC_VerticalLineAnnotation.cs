using SkiaSharp;

namespace RePlay_Activity_RepetitionsMode
{
    public class TxBDC_VerticalLineAnnotation
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public TxBDC_VerticalLineAnnotation()
        {
            //empty
        }

        #endregion

        #region Public properties

        public float X_Value { get; set; } = 0;
        public float LineThickness { get; set; } = 2;
        public SKColor LineColor { get; set; } = SKColors.Black;
        public TxBDC_BarChart_HorizontalLineAnnotation.TxBDC_LineStyle LineStyle { get; set; } = TxBDC_BarChart_HorizontalLineAnnotation.TxBDC_LineStyle.Solid;

        #endregion
    }
}