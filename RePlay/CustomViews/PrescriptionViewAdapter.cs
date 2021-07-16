using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using RePlay.Activities;
using RePlay.DataClasses;
using RePlay.Entity;
using RePlay.Fragments;
using RePlay.Helpers;
using RePlay.Manager;
using RePlay_Exercises;
using System.Collections.Generic;

namespace RePlay.CustomViews
{
    public class PrescriptionItemViewAdapter : RecyclerView.Adapter, CardTouchHelperAdapter
    {
        private List<PrescriptionItem> PrescriptionsList;
        private readonly Context Context;
        private readonly SettingsAssignmentPageActivity SettingsActivity;

        public static bool EditPrescriptionLaunched { get; set; } = false;
        public static bool DeletePrescriptionLaunched { get; set; } = false;

        public PrescriptionItemViewAdapter(Context m, List<PrescriptionItem> prescriptions)
        {
            SettingsActivity = (SettingsAssignmentPageActivity)m;
            Context = m;
            PrescriptionsList = prescriptions;
        }

        public override int ItemCount
        {
            get { return PrescriptionsList.Count; }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            //Get the view
            PrescriptionItemViewHolder view = holder as PrescriptionItemViewHolder;
            if (view == null)
            {
                //Return immediately if the view is null for some reason
                return;
            }

            int prescription_pos = holder.AdapterPosition;

            //Make sure the prescriptions list is okay
            if (PrescriptionsList == null ||
                prescription_pos >= PrescriptionsList.Count)
            {
                return;
            }

            PrescriptionItem currentPrescription = PrescriptionsList[prescription_pos];

            if (currentPrescription != null && currentPrescription.Game != null)
            {
                // Set cardview stuff
                string exercise_text = string.Empty;
                if (currentPrescription.Exercise != ExerciseType.Unknown)
                {
                    exercise_text = ExerciseTypeConverter.ConvertExerciseTypeToDescription(currentPrescription.Exercise);
                }
                else if (currentPrescription.Game.HasDefinedGameSpecificExercise())
                {
                    exercise_text = currentPrescription.Game.GetGameSpecificExercise();
                }

                view.ExerciseText.Text = StringHelper.TruncateStringForUI(exercise_text);
                view.DeviceText.Text = currentPrescription.Device.ToString();
                view.GameText.Text = currentPrescription.Game.ExternalName;
                if (currentPrescription.Duration == 1)
                {
                    view.DurationText.Text = (GameManager.Instance.IsRepetitionsMode(currentPrescription.Game)) ? currentPrescription.Duration + " rep" : currentPrescription.Duration + " minute";
                }
                else
                {
                    view.DurationText.Text = (GameManager.Instance.IsRepetitionsMode(currentPrescription.Game)) ? currentPrescription.Duration + " reps" : currentPrescription.Duration + " minutes";
                }

                view.Image.SetImageResource(currentPrescription.GetExerciseImageResourceID(SettingsActivity));
            }

            // Event handlers for buttons

            view.Delete.Click += (sender, args) =>
            {
                // Multiple click launch prevention
                if (!DeletePrescriptionLaunched)
                {
                    int pos = view.AdapterPosition;
                    DeletePrescriptionLaunched = true;
                    AlertDialog.Builder dialog = new AlertDialog.Builder(SettingsActivity);
                    AlertDialog alert = dialog.Create();
                    alert.SetTitle("Confirm");
                    alert.SetCanceledOnTouchOutside(false);
                    alert.SetMessage("Are you sure you want to remove this item from the prescription?");
                    alert.SetButton("YES", (c, ev) =>
                    {
                        SettingsActivity.AssignedPrescriptionItemDeleted(pos);
                    });
                    alert.SetButton2("NO", (c, ev) => {
                        alert.Dismiss();
                    });
                    alert.Show();
                    DeletePrescriptionLaunched = false;
                }
            };

            view.Edit.Click += (s, e) =>
            {
                if (!EditPrescriptionLaunched)
                {
                    EditPrescriptionLaunched = true;
                    int pos = view.AdapterPosition;
                    Edit_Prescription_Click(s, new EditPrescriptionEventArgs
                    {
                        Position = pos
                    });
                }
            };
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            LayoutInflater inflater = LayoutInflater.From(parent.Context);
            View item = inflater.Inflate(Resource.Layout.AssignedPrescriptionItemCard, parent, false);

            return new PrescriptionItemViewHolder(item);
        }

        private void Edit_Prescription_Click(object sender, EditPrescriptionEventArgs e)
        {
            // Get a new instance of the AddPrescriptionFragment
            FragmentTransaction fm = SettingsActivity.FragmentManager.BeginTransaction();
            AddNewPrescriptionItemFragment dialog = new AddNewPrescriptionItemFragment(
                SettingsActivity,
                AddNewPrescriptionItemFragment.AddPrescriptionItemMode.EditExistingItemOfPrescription,
                PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems[e.Position],
                e.Position);
            dialog.EditPrescriptionItemConfirmed += HandleEditPrescriptionItemConfirmed;
            dialog.DialogPausedOrCancelled += HandleEditPrescriptionCancelled;
            dialog.Show(fm, "dialog fragment");
        }

        private void HandleEditPrescriptionCancelled(object sender, System.EventArgs e)
        {
            SettingsActivity.AddPrescriptionItemFragmentLaunched = false;
            EditPrescriptionLaunched = false;
        }

        private void HandleEditPrescriptionItemConfirmed(object sender, System.EventArgs e)
        {
            var event_args = e as AddNewPrescriptionItemFragment.PrescriptionItemEditEventArgs;
            if (event_args != null)
            {
                SettingsActivity.EditedPrescriptionItem(event_args);
                EditPrescriptionLaunched = false;
            }
        }

        public void OnItemMove(int from, int to)
        {
            if (from < to)
            {
                for (int i = from; i < to; i++)
                {
                    SwapPrescriptionItems(i, i + 1);
                }
            }
            else
            {
                for (int i = from; i > to; i--)
                {
                    SwapPrescriptionItems(i, i - 1);
                }
            }

            NotifyItemMoved(from, to);
        }

        public void OnItemDismiss(int position)
        {
            PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems.RemoveAt(position);
            PrescriptionManager.Instance.SaveCurrentPrescription();

            NotifyItemRemoved(position);
        }

        public void SwapPrescriptionItems(int a, int b)
        {
            if (a < PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems.Count &&
                b < PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems.Count &&
                a >= 0 &&
                b >= 0)
            {
                var tmp = PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems[a];
                PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems[a] = PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems[b];
                PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems[b] = tmp;
                PrescriptionManager.Instance.SaveCurrentPrescription();
            }
        }

        public void OnItemSelected(PrescriptionItemViewHolder viewholder)
        {
            viewholder.WrapperView.SetBackgroundColor(Color.LightGreen);
        }

        public void OnItemDropped(PrescriptionItemViewHolder viewholder)
        {
            viewholder.WrapperView.SetBackgroundColor(Color.White);
            NotifyDataSetChanged();
        }

        public class PrescriptionItemViewHolder : RecyclerView.ViewHolder
        {
            public TextView ExerciseText { get; set; }
            public TextView DeviceText { get; set; }
            public TextView GameText { get; set; }
            public TextView DurationText { get; set; }
            public ImageButton Delete { get; set; }
            public Button Edit { get; set; }
            public ImageView Image { get; set; }
            public RelativeLayout WrapperView { get; set; }

            public PrescriptionItemViewHolder(View item) : base(item)
            {
                ExerciseText = item.FindViewById<TextView>(Resource.Id.exercise_name);
                DeviceText = item.FindViewById<TextView>(Resource.Id.device_name);
                GameText = item.FindViewById<TextView>(Resource.Id.game_name);
                DurationText = item.FindViewById<TextView>(Resource.Id.duration_text);
                Delete = item.FindViewById<ImageButton>(Resource.Id.delete_prescription);
                Edit = item.FindViewById<Button>(Resource.Id.edit_assigned_exercise);
                Image = item.FindViewById<ImageView>(Resource.Id.prescription_image);
                WrapperView = item.FindViewById<RelativeLayout>(Resource.Id.prescription_item_card_layout);
            }
        }
    }

    public class HorizontalSpaceDecoration : RecyclerView.ItemDecoration
    {
        private int HORIZ_SPACE;

        public HorizontalSpaceDecoration(int itemOffset)
        {
            HORIZ_SPACE = itemOffset;
        }

        public override void GetItemOffsets(Rect outRect, int itemPosition, RecyclerView parent)
        {
            base.GetItemOffsets(outRect, itemPosition, parent);

            if (itemPosition != parent.GetAdapter().ItemCount - 1)
            {
                outRect.Right = HORIZ_SPACE;
            }
        }
    }
}