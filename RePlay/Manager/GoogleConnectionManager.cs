using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using RePlay_GoogleCommunications;

namespace RePlay.Manager
{
    public class GoogleConnectionManager
    {
        public GoogleConnectionManager()
        {
            //empty
        }

        public void InitializeGoogleDriveConnection(Activity current_activity)
        {
            BackgroundWorker bg = new BackgroundWorker();
            bg.DoWork += (s, e) =>
            {
                var input = current_activity.Assets.Open("Replay-5b4318531d17.json");
                RePlay_GoogleCommunications.RePlay_Google.InitializeGoogleDrive(input);
            };

            bg.RunWorkerAsync();
        }

        public void SetProjectForGoogleCommunication(string project_name)
        {
            RePlay_GoogleCommunications.RePlay_Google.SetCurrentProject(project_name);
        }

        public void SetSiteForGoogleCommunication(string site_name)
        {
            RePlay_GoogleCommunications.RePlay_Google.SetCurrentSite(site_name);
        }

        public async void AddNewParticipant(string participant_id)
        {
            await Task.Run(() =>
            {
                try
                {
                    //Next, add the participant to the various project spreadsheets in the cloud...

                    //Create a spreadsheet for this participant...
                    RePlay_GoogleCommunications.RePlay_Google.CreateSubjectFile(participant_id);

                    //Add a row for this participant in the project spreadsheet
                    RePlay_GoogleCommunications.RePlay_Google.AddParticipantToProjectFile(participant_id);
                }
                catch (Exception)
                {
                    //empty
                }
            });
        }

        public async void AddRowToParticipantSheet(string participant_id, DateTime current_date, string tablet_id, 
            string game_name, string task_name, string difficulty, TimeSpan duration, string total_repetitions)
        {
            await Task.Run(() =>
            {
                try
                {
                    //If necessary, create a file for this subject, and a row in the project sheet.
                    RePlay_GoogleCommunications.RePlay_Google.CreateSubjectFile(participant_id);
                    RePlay_GoogleCommunications.RePlay_Google.AddParticipantToProjectFile(participant_id);

                    //Add the data for this session to the subject's file
                    RePlay_GoogleCommunications.RePlay_Google.UpdateSubjectFile(participant_id, current_date, tablet_id,
                    game_name, task_name, difficulty, duration, total_repetitions);
                    RePlay_GoogleCommunications.RePlay_Google.UpdateProjectFile(participant_id, string.Empty);
                }
                catch (Exception)
                {
                    //empty
                }
            });
        }
    }
}
