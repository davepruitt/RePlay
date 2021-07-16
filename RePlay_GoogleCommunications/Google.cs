using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using System.IO;
using Google.Apis.Http;

namespace RePlay_GoogleCommunications
{
    /// <summary>
    /// Functions to connect to Google Drive and upload data to Google Sheets
    /// </summary>
    public static class RePlay_Google
    {
        #region Private variables

        private static string ProjectFolderID = string.Empty;
        private static string ProjectSheetName = "Project Sheet";
        
        private static string[] Scopes = { SheetsService.Scope.Spreadsheets, DriveService.Scope.Drive };
        private static string ApplicationName = "Replay";
        private static string KeyFileName = "Replay-5b4318531d17.json"; //For github release: this file still exists, but the contents have been removed.
        private static DriveService GoogleDrive = null;
        private static SheetsService GoogleSheets = null;

        private static List<string> SharedEmails = new List<string>() { /* empty for github release */ };
        private static List<string> SharedEmailsRole = new List<string>() { "writer", "reader" };

        private static string FOLDER_MIME_TYPE = "application/vnd.google-apps.folder";
        private static string CurrentProjectName = string.Empty;
        private static string CurrentSiteName = string.Empty;

        private static List<string> SubjectSheetColumnNames = new List<string>()
        {
            "Date",
            "Time",
            "Tablet ID",
            "Game",
            "Task",
            "Difficulty",
            "Duration",
            "Total Repetitions"
        };

        #endregion

        #region Private methods

        private static bool AuthenticateServiceAccount(Stream stream)
        {
            try
            {
                GoogleCredential credential;
                credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);

                GoogleDrive = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });

                GoogleSheets = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });

                //Everything was successful
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private static bool AuthenticateServiceAccount(string key_file_path)
        {
            try
            {
                GoogleCredential credential;
                using (var stream = new FileStream(key_file_path, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
                }

                GoogleDrive = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });

                GoogleSheets = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });

                //Everything was successful
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private static IList<global::Google.Apis.Drive.v3.Data.File> GetFolderChildren (string folder_id)
        {
            FilesResource.ListRequest listRequest = GoogleDrive.Files.List();
            listRequest.Fields = "files(kind, id, name, parents, mimeType)";
            listRequest.Q = "'" + folder_id + "' in parents";

            try
            {
                IList<global::Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;
                return files;
            }
            catch (Exception e)
            {
                return new List<global::Google.Apis.Drive.v3.Data.File>();
            }
        }

        private static IList<global::Google.Apis.Drive.v3.Data.File> GetFileByName (string file_name, string parent_id = "")
        {
            FilesResource.ListRequest listRequest = GoogleDrive.Files.List();
            listRequest.Fields = "files(kind, id, name, parents, mimeType, webViewLink)";
            
            string query = "name = '" + file_name + "'";
            if (!string.IsNullOrEmpty(parent_id))
            {
                query += " and '" + parent_id + "' in parents";
            }

            listRequest.Q = query;

            try
            {
                IList<global::Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;
                return files;
            }
            catch (Exception e)
            {
                return new List<global::Google.Apis.Drive.v3.Data.File>();
            }
        }

        private static IList<global::Google.Apis.Drive.v3.Data.File> GetFolderByName(string file_name, string parent_id = "")
        {
            var results = GetFileByName(file_name, parent_id);
            var filtered_results = results.Where(x => x.MimeType.Equals(FOLDER_MIME_TYPE)).ToList();
            return filtered_results;
        }

        private static global::Google.Apis.Drive.v3.Data.File GetRootFolder ()
        {
            var root_folder = GetFolderByName("Replay").FirstOrDefault();
            return root_folder;
        }

        private static global::Google.Apis.Drive.v3.Data.File GetMainProjectsFolder ()
        {
            var root_folder = GetRootFolder();
            if (root_folder != null)
            {
                var main_projects_folder = GetFolderByName("Projects", root_folder.Id).FirstOrDefault();
                return main_projects_folder;
            }
            else
            {
                return null;
            }
        }

        public static List<string> GetListOfProjects()
        {
            var main_projects_folder = GetMainProjectsFolder();
            if (main_projects_folder != null)
            {
                var project_list = GetFolderChildren(main_projects_folder.Id).Where(x => x.MimeType.Equals(FOLDER_MIME_TYPE)).Select(x => x.Name).ToList();
                return project_list;
            }
            else
            {
                return new List<string>();
            }
        }

        private static global::Google.Apis.Drive.v3.Data.File GetProjectFolder(string project_name)
        {
            var main_projects_folder = GetMainProjectsFolder();
            if (main_projects_folder != null)
            {
                var project_folder = GetFolderByName(project_name, main_projects_folder.Id).FirstOrDefault();
                return project_folder;
            }
            else
            {
                return null;
            }
        }

        private static global::Google.Apis.Drive.v3.Data.File GetSubjectsFolder(string project_name)
        {
            var project_folder = GetProjectFolder(project_name);
            if (project_folder != null)
            {
                var subjects_folder = GetFolderByName("Participants", project_folder.Id).FirstOrDefault();
                return subjects_folder;
            }
            else
            {
                return null;
            }
        }

        private static global::Google.Apis.Drive.v3.Data.File GetFileForSubject(string subject_id)
        {
            var subjects_folder = GetSubjectsFolder(CurrentProjectName);
            if (subjects_folder != null)
            {
                return GetFileByName(subject_id, subjects_folder.Id).FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        private static global::Google.Apis.Drive.v3.Data.File GetProjectSheet ()
        {
            var project_folder = GetProjectFolder(CurrentProjectName);
            if (project_folder != null)
            {
                var project_sheet = GetFileByName("Project Sheet", project_folder.Id).FirstOrDefault();
                return project_sheet;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Initializes and authenticates connections to Google Drive and Google Sheets
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public static bool InitializeGoogleDrive()
        {
            return AuthenticateServiceAccount(KeyFileName);
        }

        /// <summary>
        /// Given an initial stream object already created, initializes Google Drive and Google Sheets
        /// </summary>
        public static bool InitializeGoogleDrive(Stream stream)
        {
            return AuthenticateServiceAccount(stream);
        }

        /// <summary>
        /// Sets the current project name, which is used to determine where to find the files on Google
        /// </summary>
        public static void SetCurrentProject (string project_name)
        {
            CurrentProjectName = project_name;
        }

        /// <summary>
        /// Sets the current site name for subjects
        /// </summary>
        public static void SetCurrentSite (string site_name)
        {
            CurrentSiteName = site_name;
        }

        /// <summary>
        /// Checks to see if a subject's spreadsheet already exists in Google Drive
        /// </summary>
        /// <param name="subject_id">The subject ID</param>
        /// <returns>True if it exists, false otherwise</returns>
        public static bool CheckIfSubjectFileExists(string subject_id)
        {
            if (GoogleDrive != null && !string.IsNullOrEmpty(CurrentProjectName))
            {
                return (GetFileForSubject(subject_id) != null);
            }

            return false;
        }

        /// <summary>
        /// Creates a new Google Spreadsheet for the specified subject. If a spreadsheet already exists for the subject, then this function does nothing.
        /// </summary>
        /// <param name="subject_id">The subject ID</param>
        /// <returns>True if a new spreadsheet was created or already exists, false if the function failed to create the new sheet</returns>
        public static bool CreateSubjectFile(string subject_id)
        {
            if (GoogleDrive != null && !string.IsNullOrEmpty(CurrentProjectName))
            {
                //Get the root folder
                var root_folder = GetRootFolder();

                if (root_folder != null)
                {
                    //Find the subject template sheet
                    var subject_template_file = GetFileByName("Replay Subject Template", root_folder.Id).FirstOrDefault();

                    //Now find the subject's file, if it already exists
                    var subjects_folder = GetSubjectsFolder(CurrentProjectName);
                    if (subjects_folder != null)
                    {
                        var subject_file = GetFileByName(subject_id, subjects_folder.Id).FirstOrDefault();
                        if (subject_file != null)
                        {
                            //If the subject file has already been created, make sure it has been given the correct permissions

                            //Grab the permissions for the file
                            var this_file_permissions_request = GoogleDrive.Permissions.List(subject_file.Id);
                            this_file_permissions_request.Fields = "permissions(emailAddress,id,kind,role,type)";
                            var this_file_permissions = this_file_permissions_request.Execute();

                            //Update the permissions if necessary
                            for (int i = 0; i < SharedEmails.Count && i < SharedEmailsRole.Count; i++)
                            {
                                var current_email = SharedEmails[i];
                                var current_role = SharedEmailsRole[i];

                                var current_permissions = this_file_permissions.Permissions.Where(
                                    x => !string.IsNullOrEmpty(x.EmailAddress) && x.EmailAddress.Equals(current_email)).FirstOrDefault();
                                if (current_permissions == null)
                                {
                                    Permission new_permission = new Permission();
                                    new_permission.Type = "user";
                                    new_permission.Role = current_role;
                                    new_permission.EmailAddress = current_email;

                                    GoogleDrive.Permissions.Create(new_permission, subject_file.Id);
                                }
                            }
                        }
                        else
                        {
                            //If the subject file does not exist already...

                            //Create the subject file from the template file.
                            var copiedFile = new global::Google.Apis.Drive.v3.Data.File();
                            copiedFile.Name = subject_id;
                            copiedFile.Parents = new List<string>() { subjects_folder.Id };
                            GoogleDrive.Files.Copy(copiedFile, subject_template_file.Id).Execute();

                            //Create permissions for the new subject file
                            for (int i = 0; i < SharedEmails.Count && i < SharedEmailsRole.Count; i++)
                            {
                                var current_email = SharedEmails[i];
                                var current_role = SharedEmailsRole[i];

                                Permission new_permission = new Permission();
                                new_permission.Type = "user";
                                new_permission.Role = current_role;
                                new_permission.EmailAddress = current_email;

                                GoogleDrive.Permissions.Create(new_permission, copiedFile.Id);
                            }
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Appends a new row of data onto the Google Sheets file for the specified subject
        /// </summary>
        /// <param name="subject_id">The subject on whom's sheet a new data row will be appended</param>
        /// <param name="session_date">The date of the session</param>
        /// <param name="task_name">The start time of the session</param>
        /// <param name="handedness">Left or right hand</param>
        /// <param name="motion_direction">Left or right motion direction</param>
        /// <param name="total_attempts">The number of attempts the subject was asked to complete</param>
        /// <param name="actual_total_attempts">The number of attempts the subject actually performed</param>
        /// <param name="median_peak_signal">The median peak signal of the data during this session</param>
        /// <param name="comments">Any comments provided by the researcher or therapist</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool UpdateSubjectFile(
            string subject_id, 
            DateTime session_date, 
            string device_id,  
            string game_name,
            string task_name, 
            string difficulty,
            TimeSpan duration,
            string total_repetitions)
        {
            if (GoogleDrive != null && GoogleSheets != null && !string.IsNullOrEmpty(CurrentProjectName))
            {
                var subj_file = GetFileForSubject(subject_id);
    
                if (subj_file != null)
                {
                    string duration_str = string.Empty;
                    try
                    {
                        duration_str = Convert.ToInt32(Math.Round(duration.TotalSeconds)).ToString();
                    }
                    catch (Exception)
                    {
                        duration_str = string.Empty;
                    }

                    //If the subject's file was found, create a new row of data to append to the sheet...
                    ValueRange request_body = new ValueRange();
                    request_body.Values = new List<IList<object>>();
                    List<object> row_vals = new List<object>()
                    {
                        session_date.ToShortDateString(),
                        session_date.ToLongTimeString(),
                        device_id,
                        game_name,
                        task_name,
                        difficulty,
                        duration_str,
                        total_repetitions
                    };
                    request_body.Values.Add(row_vals);

                    SpreadsheetsResource.ValuesResource.AppendRequest request = GoogleSheets.Spreadsheets.Values.Append(request_body, subj_file.Id, "Sheet1");
                    request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
                    request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;

                    request.Execute();

                    //The request completed successfully
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Given a subject ID, this function returns the web link to that subject's spreadsheet
        /// </summary>
        /// <returns>Web link to the spreadsheet for this subject, or an empty string upon failure</returns>
        public static string GetSubjectSheetWebLink(string subject_id)
        {
            if (GoogleDrive != null && GoogleSheets != null && !string.IsNullOrEmpty(CurrentProjectName))
            {
                var subject_file = GetFileForSubject(subject_id);
                if (subject_file != null)
                {
                    return subject_file.WebViewLink;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets a list of all dates of visits for the specified subject
        /// </summary>
        public static List<DateTime> GetListOfVisits(string subject_id)
        {
            List<DateTime> result = new List<DateTime>();

            if (GoogleDrive != null && GoogleSheets != null && !string.IsNullOrEmpty(CurrentProjectName))
            {
                //Find the subject file
                var subject_file = GetFileForSubject(subject_id);

                if (subject_file != null)
                {
                    //Get the spreadsheet data
                    string spreadsheet_id = subject_file.Id;
                    string range = "Sheet1";
                    SpreadsheetsResource.ValuesResource.GetRequest request = GoogleSheets.Spreadsheets.Values.Get(spreadsheet_id, range);

                    ValueRange response = request.Execute();
                    IList<IList<object>> values = response.Values;
                    if (values != null && values.Count > 0)
                    {
                        //Find all dates in the date column of the spreadsheet
                        var all_dates = values.Select(x => x[0]).ToList();
                        for (int i = 0; i < all_dates.Count; i++)
                        {
                            bool parse_success = DateTime.TryParse(all_dates[i] as string, out DateTime d_result);
                            if (parse_success)
                            {
                                result.Add(d_result);
                            }
                        }

                        //Get unique values in the list
                        result = result.Distinct().ToList();
                    }
                }                
            }

            return result;
        }

        /// <summary>
        /// Updates the project spreadsheet to reflect the most current information about the specified subject
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public static bool UpdateProjectFile(string subject_id, string comments)
        {
            string proj_file_name = ProjectSheetName;
            
            if (GoogleDrive != null && GoogleSheets != null && !string.IsNullOrEmpty(CurrentProjectName))
            {
                //Get the project sheet file
                var project_sheet = GetProjectSheet();

                if (project_sheet != null)
                {
                    //Now let's find the row that contains the data regarding this subject
                    string spreadsheet_id = project_sheet.Id;
                    string range = "Subjects";
                    SpreadsheetsResource.ValuesResource.GetRequest request = GoogleSheets.Spreadsheets.Values.Get(spreadsheet_id, range);

                    ValueRange response = request.Execute();
                    IList<IList<object>> values = response.Values;
                    if (values != null && values.Count > 0)
                    {
                        try
                        {
                            //Get a list of all subject ids
                            var all_subject_ids = values.Select(x => x[0]).ToList();
                            var row_idx = all_subject_ids.FindIndex(x => x.Equals(subject_id));

                            //Set default values for cell values
                            string link_to_subject_sheet = string.Empty;
                            string total_visits = string.Empty;
                            string most_recent_visit = string.Empty;
                            string first_visit = string.Empty;
                            string site_name = CurrentSiteName;

                            //Get information we need about this subject
                            var list_of_visits_for_this_subject = RePlay_Google.GetListOfVisits(subject_id);

                            //Get the visits info
                            total_visits = list_of_visits_for_this_subject.Count.ToString();
                            most_recent_visit = list_of_visits_for_this_subject.Max().ToShortDateString();
                            first_visit = list_of_visits_for_this_subject.Min().ToShortDateString();

                            //Get the link to this subject's sheet
                            link_to_subject_sheet = RePlay_Google.GetSubjectSheetWebLink(subject_id);

                            if (row_idx >= 0)
                            {
                                //Get the current information from this row
                                site_name = values[row_idx][1] as string;

                                //Update this subject's row
                                string cell_range = (row_idx + 1).ToString() + ":" + (row_idx + 1).ToString();
                                List<object> values_for_row = new List<object>() 
                                { 
                                    subject_id,  
                                    site_name, 
                                    total_visits, 
                                    most_recent_visit, 
                                    first_visit, 
                                    link_to_subject_sheet, 
                                    comments 
                                };
                                ValueRange req_body = new ValueRange();
                                req_body.Values = new List<IList<object>>() { values_for_row };

                                SpreadsheetsResource.ValuesResource.UpdateRequest update_req = GoogleSheets.Spreadsheets.Values.Update(req_body, spreadsheet_id, cell_range);
                                update_req.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                                update_req.Execute();
                            }
                            else
                            {
                                //Update this subject's row
                                List<object> values_for_row = new List<object>() 
                                { 
                                    subject_id,
                                    site_name,
                                    total_visits, 
                                    most_recent_visit, 
                                    first_visit, 
                                    link_to_subject_sheet, 
                                    comments 
                                };
                                ValueRange req_body = new ValueRange();
                                req_body.Values = new List<IList<object>>() { values_for_row };

                                SpreadsheetsResource.ValuesResource.AppendRequest append_request = GoogleSheets.Spreadsheets.Values.Append(req_body, spreadsheet_id, "Sheet1");
                                append_request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
                                append_request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;

                                append_request.Execute();
                            }

                            //Sort the spreadsheet by most recent visit
                            var sort_request = new Request()
                            {
                                SortRange = new SortRangeRequest()
                                {
                                    Range = new GridRange()
                                    {
                                        SheetId = 0,
                                        StartRowIndex = 1
                                    },

                                    SortSpecs = new List<SortSpec>()
                                    {
                                        new SortSpec()
                                        {
                                            SortOrder = "DESCENDING",
                                            DimensionIndex = 4
                                        }
                                    }
                                }
                            };

                            List<Request> requests = new List<Request>() { sort_request };

                            BatchUpdateSpreadsheetRequest requestBody = new BatchUpdateSpreadsheetRequest();
                            requestBody.Requests = requests;
                            var new_request = GoogleSheets.Spreadsheets.BatchUpdate(requestBody, spreadsheet_id);
                            new_request.Execute();

                            //Return true, indicating that everything was successful
                            return true;
                        }
                        catch (Exception e)
                        {
                            //empty
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Adds a new participant to the project file
        /// </summary>
        public static bool AddParticipantToProjectFile(string subject_id)
        {
            if (GoogleDrive != null && GoogleSheets != null && !string.IsNullOrEmpty(CurrentProjectName))
            {
                //Get the project sheet file
                var project_sheet = GetProjectSheet();

                if (project_sheet != null)
                {
                    //Now let's find the row that contains the data regarding this subject
                    string spreadsheet_id = project_sheet.Id;
                    string range = "Subjects";
                    SpreadsheetsResource.ValuesResource.GetRequest request = GoogleSheets.Spreadsheets.Values.Get(spreadsheet_id, range);

                    ValueRange response = request.Execute();
                    IList<IList<object>> values = response.Values;
                    if (values != null && values.Count > 0)
                    {
                        try
                        {
                            //Get a list of all subject ids found in the spreadsheet
                            var all_subject_ids = values.Select(x => x[0]).ToList();
                            var row_idx = all_subject_ids.FindIndex(x => x.Equals(subject_id));

                            //Set default values for cell values
                            string comments = string.Empty;
                            string implanted = string.Empty;
                            string link_to_subject_sheet = string.Empty;
                            string total_visits = "1";
                            string most_recent_visit = DateTime.Now.ToShortDateString();
                            string first_visit = DateTime.Now.ToShortDateString();
                            string site = CurrentSiteName;

                            //Get the link to this subject's individual spreadsheet
                            link_to_subject_sheet = RePlay_Google.GetSubjectSheetWebLink(subject_id);

                            //Update this subject's row in the project sheet
                            if (row_idx < 0)
                            {
                                List<object> values_for_row = new List<object>() 
                                { 
                                    subject_id, 
                                    site, 
                                    total_visits, 
                                    most_recent_visit, 
                                    first_visit, 
                                    link_to_subject_sheet, 
                                    comments 
                                };
                                ValueRange req_body = new ValueRange();
                                req_body.Values = new List<IList<object>>() { values_for_row };

                                SpreadsheetsResource.ValuesResource.AppendRequest append_request = GoogleSheets.Spreadsheets.Values.Append(req_body, spreadsheet_id, "Subjects");
                                append_request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
                                append_request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;

                                append_request.Execute();
                            }

                            //Sort the spreadsheet by most recent visit
                            var sort_request = new Request()
                            {
                                SortRange = new SortRangeRequest()
                                {
                                    Range = new GridRange()
                                    {
                                        SheetId = 0,
                                        StartRowIndex = 1
                                    },

                                    SortSpecs = new List<SortSpec>()
                                    {
                                        new SortSpec()
                                        {
                                            SortOrder = "DESCENDING",
                                            DimensionIndex = 2
                                        }
                                    }
                                }
                            };

                            List<Request> requests = new List<Request>() { sort_request };

                            BatchUpdateSpreadsheetRequest requestBody = new BatchUpdateSpreadsheetRequest();
                            requestBody.Requests = requests;
                            var new_request = GoogleSheets.Spreadsheets.BatchUpdate(requestBody, spreadsheet_id);
                            new_request.Execute();

                            //Return true, indicating that everything was successful
                            return true;
                        }
                        catch (Exception e)
                        {
                            //empty
                        }
                    }
                }   
            }

            return false;
        }

        /// <summary>
        /// Retrieves a list of all subjects in the project file
        /// </summary>
        /// <returns></returns>
        public static List<string> RetrieveListOfSubjects()
        {
            List<string> result_ids = new List<string>();
            
            string proj_file_name = ProjectSheetName;
            
            if (GoogleDrive != null && GoogleSheets != null && !string.IsNullOrEmpty(CurrentProjectName))
            {
                var project_sheet = GetProjectSheet();

                if (project_sheet != null)
                {
                    //Now let's find the row that contains the data regarding this subject

                    string spreadsheet_id = project_sheet.Id;
                    string range = "Subjects";
                    SpreadsheetsResource.ValuesResource.GetRequest request = GoogleSheets.Spreadsheets.Values.Get(spreadsheet_id, range);

                    try
                    {
                        ValueRange response = request.Execute();
                        IList<IList<object>> values = response.Values;
                        if (values != null && values.Count > 0)
                        {
                            try
                            {
                                //Get a list of all subject ids
                                result_ids = values.Select(x => x[0].ToString()).ToList();
                                
                                //Remove the header row
                                result_ids.RemoveAt(0);
                            }
                            catch (Exception e)
                            {
                                //empty
                            }
                        }
                    }
                    catch (Exception req_except)
                    {
                        //empty
                    }
                }
            }

            return result_ids;
        }

        public static List<string> RetrieveListOfSites ()
        {
            List<string> site_ids = new List<string>();

            string proj_file_name = ProjectSheetName;

            if (GoogleDrive != null && GoogleSheets != null && !string.IsNullOrEmpty(CurrentProjectName))
            {
                var project_sheet = GetProjectSheet();

                if (project_sheet != null)
                {
                    //Now let's find the row that contains the data regarding this subject

                    string spreadsheet_id = project_sheet.Id;
                    string range = "Sites";
                    SpreadsheetsResource.ValuesResource.GetRequest request = GoogleSheets.Spreadsheets.Values.Get(spreadsheet_id, range);

                    try
                    {
                        ValueRange response = request.Execute();
                        IList<IList<object>> values = response.Values;
                        if (values != null && values.Count > 0)
                        {
                            try
                            {
                                //Get a list of all subject ids
                                site_ids = values.Select(x => x[0].ToString()).ToList();

                                //Remove the header row
                                site_ids.RemoveAt(0);
                            }
                            catch (Exception e)
                            {
                                //empty
                            }
                        }
                    }
                    catch (Exception req_except)
                    {
                        //empty
                    }
                }
            }

            return site_ids;
        }

        #endregion
    }
}
