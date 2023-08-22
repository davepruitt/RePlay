using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RePlay_Common
{
    public class MatlabCompatibility
    {

        /// <summary>
        /// Converts a .NET DateTime object into a 64-bit floating-point value that
        /// is the same format that Matlab uses to represents dates and times.
        /// </summary>
        /// <param name="date_time">The .NET DateTime object</param>
        /// <returns>Matlab datecode</returns>
        public static double ConvertDateTimeToMatlabDatenum(DateTime date_time)
        {
            double result = double.NaN;

            //Get the ticks from the DateTime object
            long ticks = date_time.Ticks;
            long a = Convert.ToInt64(Math.Pow(10, 7)) * 60 * 60 * 24;
            long offset = -367 * Convert.ToInt64(Math.Pow(10, 7)) * 60 * 60 * 24;

            result = Convert.ToDouble(ticks - offset) / a;

            return result;
        }

        /// <summary>
        /// This function converts a Matlab datenum to a C#/.NET DateTime.
        /// </summary>
        /// <param name="datenum">Matlab datenum</param>
        /// <returns>C# DateTime</returns>
        public static DateTime ConvertMatlabDatenumToDateTime(double datenum)
        {
            DateTime result = DateTime.MinValue;

            long a = Convert.ToInt64(Math.Pow(10, 7)) * 60 * 60 * 24;
            long offset = -367 * Convert.ToInt64(Math.Pow(10, 7)) * 60 * 60 * 24;
            long ticks = Convert.ToInt64(a * datenum) + offset;

            result = new DateTime(ticks);

            return result;
        }
    }
}
