using SkiaSharp;

namespace RePlay_Activity_RepetitionsMode
{
    public class TxBDC_BarChart_HorizontalLineAnnotation
    {
        #region Line style enumeration

        public enum TxBDC_LineStyle
        {
            Solid,
            Dashed
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public TxBDC_BarChart_HorizontalLineAnnotation()
        {
            //empty
        }

        #endregion

        #region Public properties

        public float Y_Value { get; set; } = 0;
        public float LineThickness { get; set; } = 2;
        public SKColor LineColor { get; set; } = SKColors.Black;
        public TxBDC_LineStyle LineStyle { get; set; } = TxBDC_LineStyle.Solid;

        #endregion
    }
}