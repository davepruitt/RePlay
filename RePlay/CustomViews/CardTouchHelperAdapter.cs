using static RePlay.CustomViews.PrescriptionItemViewAdapter;

namespace RePlay.CustomViews
{
    public interface CardTouchHelperAdapter
    {
        void OnItemMove(int from, int to);

        void OnItemDismiss(int position);

        void OnItemSelected(PrescriptionItemViewHolder viewholder);

        void OnItemDropped(PrescriptionItemViewHolder viewholder);
    }
}