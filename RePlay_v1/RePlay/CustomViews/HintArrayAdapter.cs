using Android.Content;
using Android.Widget;
using System.Collections.Generic;

namespace RePlay.CustomViews
{
    public class HintArrayAdapter : ArrayAdapter<string>
    {
        public HintArrayAdapter(Context context, int layout, List<string> items) : base(context, layout, items)
        {
            // empty
        }

        public override int Count => (base.Count > 0) ? base.Count - 1 : base.Count;
    }
}