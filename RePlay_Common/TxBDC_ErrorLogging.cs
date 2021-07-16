using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RePlay_Common
{
    public static class TxBDC_ErrorLogging
    {
        #region Private data members

        private static string error_log_path = string.Empty;
        private static string error_log_file = string.Empty;
        private static object error_log_lock = null;

        #endregion

        #region Public methods

        /// <summary>
        /// Calling this function initializes the path and file name variables
        /// </summary>
        public static void InitializeErrorLogging (string path)
        {
            error_log_path = path;
            error_log_file = "errors.txt";
            error_log_lock = new object();
        }
        
        /// <summary>
        /// Logs an exception to the error log file
        /// </summary>
        public static void LogException (Exception e)
        {
            lock (error_log_lock)
            {
                try
                {
                    //Create the fully qualified file name
                    string fully_qualified_file = Path.Combine(error_log_path, error_log_file);
                    
                    //Create the folder if necessary
                    new FileInfo(fully_qualified_file).Directory.Create();

                    //Open a handle to be able to write to the file
                    StreamWriter writer = new StreamWriter(fully_qualified_file, true);
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd:HH:mm:ss");
                    string stacktrace = e.StackTrace;
                    string outermost_exception = e.Message;

                    string innermost_exception = string.Empty;
                    var base_except = e.GetBaseException();
                    if (base_except != null)
                    {
                        innermost_exception = base_except.Message;
                    }

                    string function = e.TargetSite.Name;

                    writer.WriteLine("NEW ERROR DETECTED");
                    writer.WriteLine(timestamp);
                    writer.WriteLine("Stack trace: " + stacktrace);
                    writer.WriteLine("Outermost exception message: " + outermost_exception);
                    writer.WriteLine("Innermost exception message: " + innermost_exception);
                    writer.WriteLine("Function name: " + function);
                    writer.WriteLine("END OF NEW ERROR");
                    writer.WriteLine();

                    writer.Close();
                }
                catch
                {
                    //do nothing
                }
            }
        }

        /// <summary>
        /// Logs a string to the error log file.
        /// </summary>
        public static void LogString (string msg, bool include_timestamp = true)
        {
            lock (error_log_lock)
            {
                try
                {
                    //Create the fully qualified file name
                    string fully_qualified_file = Path.Combine(error_log_path, error_log_file);

                    //Create the folder if necessary
                    new FileInfo(fully_qualified_file).Directory.Create();

                    //Open a handle to be able to write to the file
                    StreamWriter writer = new StreamWriter(fully_qualified_file, true);

                    if (include_timestamp)
                    {
                        var current_ts = DateTime.Now;
                        var current_ts_date = current_ts.ToShortDateString();
                        var current_ts_time = current_ts.ToLongTimeString();
                        writer.WriteLine(current_ts_date + ", " + current_ts_time + ", " + msg);
                    }
                    else
                    {
                        writer.WriteLine(msg);
                    }

                    writer.Close();
                }
                catch
                {
                    //do nothing
                }
            }
        }

        #endregion
    }
}
