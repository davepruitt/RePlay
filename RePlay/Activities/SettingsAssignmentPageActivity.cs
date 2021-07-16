using System;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Views;
using Android.Widget;
using RePlay.CustomViews;
using RePlay.Entity;
using RePlay.Fragments;
using RePlay.Manager;

namespace RePlay.Activities
{
    [Activity(Label = "Settings", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    // This page is intended for use by the patient's physical therapist
    // so he or she can add or delete game prescriptions for the patient.
    public class SettingsAssignmentPageActivity : Activity
    {
        #region Properties

        private RecyclerView AssignedView, SavedView;
        private ImageButton Add;
        private ImageView PatientPicture;
        private Button SavePrescripton;
        private Button ClearPrescriptionButton;
        private TextView PatientName, EmptyAssigned, EmptySaved;
        private Participant patient;

        private const int ASSIGNED_CARD_SPACING = 50;
        private const int SAVED_CARD_SPACING = 45;

        // Multiple launch click prevention
        private bool PatientFragmentLaunched { get; set; } = false;
        private bool SavePrescriptionLaunched { get; set; } = false;
        public bool AddPrescriptionItemFragmentLaunched { get; set; } = false;

        private GoogleConnectionManager google_connection_manager;

        #endregion

        #region OnCreate

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //Set the UI for this activity
            SetContentView(Resource.Layout.Settings);

            //Grab the input parameters for this activity
            google_connection_manager = StupidityManager.GiveMeThat("google") as GoogleConnectionManager;
            StupidityManager.CleanTheSlatePlease();

            SettingsLoginActivity.SettingsActivityLaunched = false;
            InitializeViews();
            InitPatient();
        }

        // Activity is no longer in view
        protected override void OnStop()
        {
            base.OnStop();
            if (!PatientFragmentLaunched)
            {
                Finish();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            //Pass the google connection manager to the navigation fragment as well
            var navigation_fragment = FragmentManager.FindFragmentById<NavigationFragment>(Resource.Id.navigation_fragment);
            navigation_fragment.google_connection_manager = google_connection_manager;
        }

        #endregion

        #region Private Methods

        // Initialize the views
        private void InitializeViews()
        {   
            // Initialize both views
            AssignedView = FindViewById<RecyclerView>(Resource.Id.assigned_prescriptions);
            SavedView = FindViewById<RecyclerView>(Resource.Id.saved_prescriptions);

            AssignedView.HasFixedSize = true;
            AssignedView.SetLayoutManager(new LinearLayoutManager(this, LinearLayoutManager.Horizontal, false));

            var prescriptionAdapter = new PrescriptionItemViewAdapter(this, PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems);
            AssignedView.SetAdapter(prescriptionAdapter);
            AssignedView.AddItemDecoration(new HorizontalSpaceDecoration(ASSIGNED_CARD_SPACING));
            ItemTouchHelper.Callback callback = new CardTouchHelperCallback(prescriptionAdapter);
            ItemTouchHelper touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(AssignedView);

            SavedView.HasFixedSize = true;
            SavedView.SetLayoutManager(new LinearLayoutManager(this, LinearLayoutManager.Horizontal, false));
            SavedView.SetAdapter(new SavedPrescriptionViewAdapter(this, SavedPrescriptionManager.Instance.SavedPrescriptions.OrderByDescending(x => x.Date).ToList()));
            SavedView.AddItemDecoration(new HorizontalSpaceDecoration(SAVED_CARD_SPACING));

            EmptyAssigned = FindViewById<TextView>(Resource.Id.empty_assigned_prescriptions);
            EmptySaved = FindViewById<TextView>(Resource.Id.empty_saved_prescriptions);

            // Initialize add button and save
            SavePrescripton = FindViewById<Button>(Resource.Id.addToSavedPrescriptions);
            SavePrescripton.Click += SavePrescripton_Click;
            Add = FindViewById<ImageButton>(Resource.Id.add_prescription_card);
            Add.Touch += (s, e) => ButtonTouched(s, e, e.Event.Action);
            ClearPrescriptionButton = FindViewById<Button>(Resource.Id.clearCurrentPrescriptionButton);
            ClearPrescriptionButton.Click += ClearPrescription_Click;

            // Add the build information to the view
            var build_information_view = FindViewById<TextView>(Resource.Id.build_information_text_view);
            build_information_view.Text = "RePlay Version: " + BuildInformationManager.GetVersionName(this) + "(Build date: " + BuildInformationManager.RetrieveBuildDate(this).ToString() + ")";

            RefreshViews();
        }

        private void ClearPrescription_Click(object sender, EventArgs e)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);

            alert.SetMessage("This will clear all items in the current assignment. Are you sure you want to proceed?");
            alert.SetTitle("Confirm Action");
            alert.SetPositiveButton("Yes", (c, ev) =>
            {
                ClearPrescription();
            });
            alert.SetNegativeButton("No", (c, ev) =>
            {
                //empty
            });

            Dialog dialog = alert.Create();
            dialog.Show();
        }

        // Handle a save prescription event
        private void SavePrescripton_Click(object sender, EventArgs e)
        {
            if (!SavePrescriptionLaunched)
            {
                SavePrescriptionLaunched = true;
                View dialoglayout = LayoutInflater.Inflate(Resource.Layout.AddFromSaved, null);

                AlertDialog.Builder dialog = new AlertDialog.Builder(this);
                dialog.SetView(dialoglayout);
                var alert = dialog.Create();

                EditText name = dialoglayout.FindViewById<EditText>(Resource.Id.save_prescription_name_edit_text);
                Button add = dialoglayout.FindViewById<Button>(Resource.Id.addButton);
                add.Click += (s, ev) =>
                {
                    if (string.IsNullOrWhiteSpace(name.Text)) return;
                    if(SavedPrescriptionManager.Instance.SavedPrescriptions.Select(x => x.Name).Contains(name.Text.Trim()))
                    {
                        Toast.MakeText(this, "This name has already been used!", ToastLength.Short).Show();
                        return;
                    }

                    var copyOfPrescriptionManagerInstance = PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems.Select(pi => pi.Clone() as PrescriptionItem).ToList();
                    Prescription savePrescription = new Prescription(copyOfPrescriptionManagerInstance, name.Text);
                    SavedPrescriptionManager.Instance.SavedPrescriptions.Add(savePrescription);
                    SavedPrescriptionManager.Instance.SavePrescriptions();
                    RefreshSavedAdapter();
                    RefreshViews();
                    alert.Dismiss();
                    SavePrescriptionLaunched = false;
                };

                alert.CancelEvent += (s, ev) => { SavePrescriptionLaunched = false; };
                alert.Show();
            }
        }

        // Initialize the patient picture view and handler
        private void InitPatient()
        {
            PatientPicture = FindViewById<ImageView>(Resource.Id.settings_picture);
            PatientName = FindViewById<TextView>(Resource.Id.therapist_name);
            patient = PatientLoader.Load(Assets);
            PatientName.Text = patient.SubjectID;
            PatientPicture.SetImageBitmap(patient.Photo);
        }

        // Animations for button touches
        private void ButtonTouched(object sender, View.TouchEventArgs e, MotionEventActions action)
        {
            ImageButton button = (ImageButton)sender;
            switch (action)
            {
                case MotionEventActions.Down:
                    button.Animate().ScaleX(NavigationFragment.BUTTON_SCALE).SetDuration(NavigationFragment.BUTTON_DURATION).Start();
                    button.Animate().ScaleY(NavigationFragment.BUTTON_SCALE).SetDuration(NavigationFragment.BUTTON_DURATION).Start();
                    break;
                case MotionEventActions.Up:
                    button.Animate().Cancel();
                    button.Animate().ScaleX(1f).SetDuration(NavigationFragment.BUTTON_DURATION).Start();
                    button.Animate().ScaleY(1f).SetDuration(NavigationFragment.BUTTON_DURATION).Start();
                    if (button.Id.Equals(Add.Id)) Add_PrescriptionItem_Click(sender, e);
                    break;
            }
        }

        // Handle the event that the add button is clicked
        private void Add_PrescriptionItem_Click(object sender, EventArgs e)
        {
            if(!AddPrescriptionItemFragmentLaunched)
            {
                // Get a new instance of the AddPrescriptionFragment
                AddPrescriptionItemFragmentLaunched = true;
                FragmentTransaction fm = FragmentManager.BeginTransaction();
                AddNewPrescriptionItemFragment dialog = new AddNewPrescriptionItemFragment(this, 
                    AddNewPrescriptionItemFragment.AddPrescriptionItemMode.AddNewItemToPrescription);
                dialog.NewPrescriptionItemConfirmed += HandleNewPrescriptionItemConfirmed;
                dialog.DialogPausedOrCancelled += HandleNewPrescriptionItemCancelled;
                dialog.Show(fm, "dialog fragment");
            }
        }

        private void HandleNewPrescriptionItemCancelled(object sender, EventArgs e)
        {
            AddPrescriptionItemFragmentLaunched = false;
        }

        // Update patient name after the user enters a new name
        private void OnDialogClosed(object sender, Participant p)
        {
            PatientFragmentLaunched = false;
            var frag = sender as PatientFragment;
            if (frag.SaveInfo)
            {
                PatientName.Text = p.SubjectID;
                PatientPicture.SetImageBitmap(p.Photo);
                patient = p;
                PatientLoader.Save(patient);

                //Save the data for the purposes of Google communication
                try
                {
                    google_connection_manager.AddNewParticipant(p.SubjectID);
                }
                catch (Exception e)
                {
                    //Unable to add participant to the google drive folder
                }
            }
            RefreshAssignedAdapter();
            RefreshSavedAdapter();
            RefreshViews();
        }

        // Check if recycler views are empty
        private void RefreshViews()
        {
            EmptyAssigned.Visibility = (PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems.Count == 0) ? ViewStates.Visible : ViewStates.Invisible;
            EmptySaved.Visibility = (SavedPrescriptionManager.Instance.SavedPrescriptions.Count == 0) ? ViewStates.Visible : ViewStates.Invisible;

            if (PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems.Count == 0)
            {
                SavePrescripton.Visibility = ViewStates.Invisible;
            }
            else
            {
                SavePrescripton.Visibility = ViewStates.Visible;
                SavePrescripton.Enabled = true;
                SavePrescripton.Text = "SAVE ASSIGNMENT AS...";
                SavePrescripton.SetBackgroundResource(Resource.Drawable.txbdc_button_background_selector);
            }
        }

        private void RefreshAssignedAdapter()
        {
            var prescriptionAdapter = new PrescriptionItemViewAdapter(this, PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems);
            AssignedView.SetAdapter(prescriptionAdapter);
            ItemTouchHelper.Callback callback = new CardTouchHelperCallback(prescriptionAdapter);
            ItemTouchHelper touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(AssignedView);
        }

        private void RefreshSavedAdapter()
        {
            SavedView.SetAdapter(new SavedPrescriptionViewAdapter(this, SavedPrescriptionManager.Instance.SavedPrescriptions.OrderByDescending(x => x.Date).ToList()));
        }

        #endregion

        #region Public Methods

        private void ClearPrescription ()
        {
            if (PrescriptionManager.Instance != null &&
                PrescriptionManager.Instance.CurrentPrescription != null)
            {
                //Clear the list of items currently in the assignment
                if (PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems != null)
                {
                    PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems.Clear();
                }
                else
                {
                    PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems = 
                        new System.Collections.Generic.List<PrescriptionItem>();
                }

                //Save the assignment
                PrescriptionManager.Instance.SaveCurrentPrescription();

                //Refresh the GUI
                RefreshAssignedAdapter();
                RefreshViews();
                AssignedView.ScrollToPosition(AssignedView.GetAdapter().ItemCount - 1);
            }
            
        }

        private void HandleNewPrescriptionItemConfirmed(object sender, EventArgs e)
        {
            var event_args = e as AddNewPrescriptionItemFragment.PrescriptionItemEditEventArgs;
            if (event_args != null)
            {
                var prescription_item = event_args.NewOrEdited_PrescriptionItem;
                if (prescription_item != null)
                {
                    //Add the new prescription item to the current prescription
                    PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems.Add(prescription_item);

                    //Save the current prescription
                    PrescriptionManager.Instance.SaveCurrentPrescription();

                    //Refresh the GUI
                    RefreshAssignedAdapter();
                    RefreshViews();
                    AssignedView.ScrollToPosition(AssignedView.GetAdapter().ItemCount - 1);

                    //Reset the flag
                    AddPrescriptionItemFragmentLaunched = false;
                }
            }
        }

        // Handle an assigned prescription being deleted
        public void AssignedPrescriptionItemDeleted(int pos)
        {
            if (pos >= 0 && pos < PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems.Count)
            {
                PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems.RemoveAt(pos);
                PrescriptionManager.Instance.SaveCurrentPrescription();
                RefreshAssignedAdapter();
                RefreshViews();
            }
        }

        // Handle a saved prescription being assigned
        public void UsedSavedPrescription(Prescription p)
        {
            PrescriptionManager.Instance.SwitchPrescription(p);
            RefreshAssignedAdapter();
            RefreshViews();
        }

        // Handle an assigned prescription being deleted
        public void SavedPrescriptionDeleted(string prescription_name)
        {
            int pos = SavedPrescriptionManager.Instance.SavedPrescriptions.FindIndex(x => x.Name.Equals(prescription_name));
            if (pos >= 0 && pos < SavedPrescriptionManager.Instance.SavedPrescriptions.Count)
            {
                SavedPrescriptionManager.Instance.SavedPrescriptions.RemoveAt(pos);
                SavedPrescriptionManager.Instance.SavePrescriptions();
                RefreshSavedAdapter();
                RefreshViews();
            }
        }

        // Handle Prescription edited
        public void EditedPrescriptionItem(AddNewPrescriptionItemFragment.PrescriptionItemEditEventArgs e)
        {
            //Set the prescription item
            PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems[e.Position] = e.NewOrEdited_PrescriptionItem.DeepCopy();

            //Save the prescription
            PrescriptionManager.Instance.SaveCurrentPrescription();

            //Refresh the UI
            RefreshAssignedAdapter();
            RefreshViews();
            AssignedView.ScrollToPosition(e.Position);

            //Reset the flag
            AddPrescriptionItemFragmentLaunched = false;
        }

        #endregion
    }
}