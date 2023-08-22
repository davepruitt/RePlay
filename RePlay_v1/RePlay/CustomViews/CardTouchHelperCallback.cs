using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using static RePlay.CustomViews.PrescriptionItemViewAdapter;

namespace RePlay.CustomViews
{
    public class CardTouchHelperCallback : ItemTouchHelper.Callback
    {
        private CardTouchHelperAdapter ItemAdapter;

        public override bool IsLongPressDragEnabled => true;
        public override bool IsItemViewSwipeEnabled => false;

        public CardTouchHelperCallback(CardTouchHelperAdapter adapter)
        {
            ItemAdapter = adapter;
        }

        public override int GetMovementFlags(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            int dragFlags = ItemTouchHelper.Left | ItemTouchHelper.Right;
            int swipeFlags = ItemTouchHelper.Up | ItemTouchHelper.Down;

            return MakeMovementFlags(dragFlags, swipeFlags);
        }

        public override bool OnMove(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder, RecyclerView.ViewHolder target)
        {
            ItemAdapter.OnItemMove(viewHolder.AdapterPosition, target.AdapterPosition);

            return true;
        }

        public override void OnSwiped(RecyclerView.ViewHolder viewHolder, int direction)
        {
            ItemAdapter.OnItemDismiss(viewHolder.AdapterPosition);
        }

        public override void OnSelectedChanged(RecyclerView.ViewHolder viewHolder, int actionState)
        {
            if (actionState != ItemTouchHelper.ActionStateIdle)
            {
                if (viewHolder.GetType() == typeof(PrescriptionItemViewHolder))
                {
                    var holder = (PrescriptionItemViewHolder)viewHolder;
                    ItemAdapter.OnItemSelected(holder);
                }
            }
            base.OnSelectedChanged(viewHolder, actionState);
        }

        public override void ClearView(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            if (viewHolder.GetType() == typeof(PrescriptionItemViewHolder))
            {
                var holder = (PrescriptionItemViewHolder)viewHolder;
                ItemAdapter.OnItemDropped(holder);
            }
            base.ClearView(recyclerView, viewHolder);
        }
    }
}