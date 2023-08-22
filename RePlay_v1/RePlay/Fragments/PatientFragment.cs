using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using RePlay.Entity;
using RePlay.Manager;

namespace RePlay.Fragments
{
    // Fragment to update patient details and patient photo
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0672 // Member overrides obsolete member
    public class PatientFragment : DialogFragment
    {
        #region Private data members

        private Participant patient;
        private GoogleConnectionManager google_connection_manager = null;

        #endregion

        #region Properties


        public static event EventHandler<Participant> DialogClosed;
        public bool SaveInfo = false;

        

        #endregion

        #region Constructor

        public PatientFragment(Participant p, GoogleConnectionManager g)
        {
            google_connection_manager = g;
            patient = p;
        }

        #endregion

        #region OnCreate
        // Default OnCreate
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }


        // Inflates the PatientFragment View, instantiates the EditText fields for updating the names,
        // and instantiates a cancel and save button
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            Dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            Dialog.SetCanceledOnTouchOutside(false);
            View rootView = inflater.Inflate(Resource.Layout.PatientFragment, container, false);
            EditText Last = rootView.FindViewById<EditText>(Resource.Id.patient_subjectid);
            Last.Text = patient.SubjectID;

            Button Cancel = rootView.FindViewById<Button>(Resource.Id.patient_cancel);
            Button Save = rootView.FindViewById<Button>(Resource.Id.patient_save);
            
            Cancel.Click += (sender, args) =>
            {
                Dismiss();
            };

            Save.Click += (sender, args) =>
            {
                //First do a simple check to make sure the participant ID entered is not a 
                //forbidden ID
                string result = Last.Text.Trim();
                if (Participant.ParticipantID_IsOK(result))
                {
                    result = Participant.CleanParticipantID(result);
                    if (result.StartsWith("TEST", StringComparison.InvariantCultureIgnoreCase))
                    {
                        string msg = "Participant IDs that begin with the prefix of TEST are reserved for TxBDC internal testing. Please confirm the participant ID:\n\n" + result;
                        int start = msg.Length - result.Length;
                        int end = msg.Length;
                        SpannableString s = new SpannableString(msg);
                        s.SetSpan(new AlignmentSpanStandard(Layout.Alignment.AlignCenter), start, end, SpanTypes.ExclusiveInclusive);
                        s.SetSpan(new RelativeSizeSpan(2.0f), start, end, SpanTypes.ExclusiveInclusive);

                        AlertDialog.Builder alert = new AlertDialog.Builder(this.Activity);
                        alert.SetTitle("Verify action");
                        alert.SetMessage(s);
                        alert.SetNegativeButton("GO BACK", (senderAlert, args2) =>
                        {
                            //empty
                        });
                        alert.SetPositiveButton("PROCEED", (senderAlert, args2) =>
                        {
                            SavePatientResult(result);
                        });

                        Dialog dialog = alert.Create();
                        dialog.Show();
                    }
                    else
                    {
                        SavePatientResult(result);
                    }
                }
                else
                {
                    AlertDialog.Builder alert = new AlertDialog.Builder(this.Activity);
                    alert.SetTitle("Invalid Participant ID");
                    alert.SetMessage("The participant ID you entered is invalid. Please make sure that participant ID contains only letters, numbers, and underscores.");
                    alert.SetNeutralButton("OK", (senderAlert, args2) =>
                    {
                        //empty
                    });

                    Dialog dialog = alert.Create();
                    dialog.Show();
                }
            };

            return rootView;
        }

        #endregion

        #region Private methods

        private void SavePatientResult (string pid)
        {
            patient.SubjectID = pid;
            SaveInfo = true;
            Dismiss();
        }

        #endregion

        #region EventHandlers

        //This will be called after taking an image with the camera
        public override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            patient.Photo = (Bitmap)data.Extras.Get("data");
        }

        // Dismisses this fragment and invokes the DialogClosed event
        // with the Patient (fname, lname, photo) passed as a parameter.
        // SettingsActivity is responsible for attaching an actual event
        // handler to the DialogClosed event.
        // That method sets the text of SettingsActivity's PatientName variable 
        // as the `name` passed into the delegate. It also sets the Photo.
        public override void OnDismiss(IDialogInterface dialog)
        {
            base.OnDismiss(dialog);
            DialogClosed?.Invoke(this, patient);
        }

        #endregion
    }
#pragma warning restore CS0672 // Member overrides obsolete member
#pragma warning restore CS0618 // Type or member is obsolete
}
