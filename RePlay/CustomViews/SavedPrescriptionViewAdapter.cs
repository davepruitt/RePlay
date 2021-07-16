using Android.App;
using Android.Content;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using RePlay.Activities;
using RePlay.Entity;
using RePlay.Helpers;
using RePlay.Manager;
using System.Collections.Generic;
using System.Linq;

namespace RePlay.CustomViews
{
    public class SavedPrescriptionViewAdapter : RecyclerView.Adapter
    {
        private List<Prescription> PrescriptionsList;
        private readonly Context Context;
        private readonly SettingsAssignmentPageActivity Settings;

        public static bool AddFromSavedLaunched { get; set; } = false;
        public static bool DeleteSavedPrescriptionLaunched { get; set; } = false;

        public SavedPrescriptionViewAdapter(Context m, List<Prescription> prescriptions)
        {
            Settings = (SettingsAssignmentPageActivity)m;
            Context = m;
            PrescriptionsList = prescriptions;
        }

        public override int ItemCount
        {
            get { return PrescriptionsList.Count; }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            //Do a simple check before continuing
            if (PrescriptionsList == null || position >= PrescriptionsList.Count)
            {
                return;
            }

            //Get the prescription that is at the indicated position
            Prescription currentPrescription = PrescriptionsList[position];

            //Get the view holder
            SavedPrescriptionViewHolder view = holder as SavedPrescriptionViewHolder;
            if (view == null)
            {
                return;
            }

            // Set some information that will be displayed in the cardview
            if (currentPrescription != null)
            {
                view.PrescriptionNameText.Text = StringHelper.TruncateStringForUI(currentPrescription.Name);
                view.PrescriptionDateText.Text = "Date: " + currentPrescription.Date.ToShortDateString();
                view.PrescriptionCountText.Text = currentPrescription.PrescriptionItems.Count.ToString() + " items";

                if (currentPrescription.PrescriptionItems != null &&
                    currentPrescription.PrescriptionItems.Count > 0)
                {
                    var first_prescription_item = currentPrescription.PrescriptionItems.FirstOrDefault();
                    var exercise_type = first_prescription_item.Exercise;
                    view.Image.SetImageResource(ExerciseManager.Instance.MapNameToPic(exercise_type, Settings));
                }
            }
            
            // Event handlers for buttons
            view.Delete.Click += (sender, args) =>
            {
                if (!DeleteSavedPrescriptionLaunched)
                {
                    int pos = view.AdapterPosition;
                    string prescription_name = string.Empty;
                    if ((PrescriptionsList != null) &&
                        (PrescriptionsList.Count > 0) && 
                        (pos >= 0) &&
                        (pos < PrescriptionsList.Count))
                    {
                        prescription_name = PrescriptionsList[pos].Name;
                    }

                    DeleteSavedPrescriptionLaunched = true;
                    AlertDialog.Builder dialog = new AlertDialog.Builder(Settings);
                    AlertDialog alert = dialog.Create();
                    alert.SetTitle("Confirm");
                    alert.SetCanceledOnTouchOutside(false);
                    alert.SetMessage("Are you sure you want to delete this prescription?");
                    alert.SetButton("YES", (c, ev) =>
                    {
                        Settings.SavedPrescriptionDeleted(prescription_name);
                        DeleteSavedPrescriptionLaunched = false;
                    });
                    alert.SetButton2("NO", (c, ev) => {
                        alert.Dismiss();
                        DeleteSavedPrescriptionLaunched = false;
                    });
                    alert.Show();
                }
            };

            view.Assign.Click += (sender, e) =>
            {
                // assign prescription
                if (!AddFromSavedLaunched)
                {
                    int pos = view.AdapterPosition;
                    Prescription curr = PrescriptionsList[pos];
                    AddFromSavedLaunched = true;
                    AlertDialog.Builder dialog = new AlertDialog.Builder(Settings);
                    AlertDialog alert = dialog.Create();
                    alert.SetTitle("Confirm");
                    alert.SetCanceledOnTouchOutside(false);
                    alert.SetMessage("Are you sure you want to replace the current prescription?");
                    alert.SetButton("YES", (c, ev) =>
                    {
                        Settings.UsedSavedPrescription(curr);
                        AddFromSavedLaunched = false;
                    });
                    alert.SetButton2("NO", (c, ev) => {
                        alert.Dismiss();
                        AddFromSavedLaunched = false;
                    });
                    alert.Show();
                }
            };
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            LayoutInflater inflater = LayoutInflater.From(parent.Context);
            View item = inflater.Inflate(Resource.Layout.SavedPrescriptionCard, parent, false);

            return new SavedPrescriptionViewHolder(item);
        }

        public class SavedPrescriptionViewHolder : RecyclerView.ViewHolder
        {
            public TextView PrescriptionNameText { get; set; }
            public TextView PrescriptionDateText { get; set; }
            public TextView PrescriptionCountText { get; set; }
            public ImageButton Delete { get; set; }
            public Button Assign { get; set; }
            public ImageView Image { get; set; }

            public SavedPrescriptionViewHolder(View item) : base(item)
            {
                PrescriptionNameText = item.FindViewById<TextView>(Resource.Id.prescription_name);
                PrescriptionDateText = item.FindViewById<TextView>(Resource.Id.date_text);
                PrescriptionCountText = item.FindViewById<TextView>(Resource.Id.number_of_items);
                Delete = item.FindViewById<ImageButton>(Resource.Id.delete_image);
                Assign = item.FindViewById<Button>(Resource.Id.add_saved_prescription);
                Image = item.FindViewById<ImageView>(Resource.Id.exercise_image);
            }
        }
    }
}