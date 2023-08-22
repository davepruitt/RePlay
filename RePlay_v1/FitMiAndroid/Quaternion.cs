using System;
using System.Collections.Generic;
using System.Linq;

namespace FitMiAndroid
{
    public static class Quaternion
    {
        public static List<double> q_normalize(List<double> v, double tolerance = 0.00001)
        {
            var mag2 = v.Select(x => x * x).Sum();
            if (Math.Abs(mag2 - 1.0) > tolerance)
            {
                var mag = Math.Sqrt(mag2);
                v = v.Select(x => x / mag).ToList();
            }

            return v;
        }

        public static List<double> q_mult(List<double> q1, List<double> q2)
        {
            var w1 = q1[0];
            var x1 = q1[1];
            var y1 = q1[2];
            var z1 = q1[3];

            var w2 = q2[0];
            var x2 = q2[1];
            var y2 = q2[2];
            var z2 = q2[3];

            var w = w1 * w2 - x1 * x2 - y1 * y2 - z1 * z2;
            var x = w1 * x2 + x1 * w2 + y1 * z2 - z1 * y2;
            var y = w1 * y2 + y1 * w2 + z1 * x2 - x1 * z2;
            var z = w1 * z2 + z1 * w2 + x1 * y2 - y1 * x2;

            return new List<double>() { w, x, y, z };
        }

        public static List<double> q_conjugate(List<double> q)
        {
            var w = q[0];
            var x = q[1];
            var y = q[2];
            var z = q[3];

            return new List<double>() { w, -x, -y, -z };
        }

        public static List<double> qv_mult(List<double> q1, List<double> v1)
        {
            var q2 = v1.ToList();
            q2.Insert(0, 0);

            var result = Quaternion.q_mult(Quaternion.q_mult(q1, q2), Quaternion.q_conjugate(q1));
            result.RemoveRange(0, 1);

            return result;
        }

        public static double quaternion_distance(List<double> q1, List<double> q2)
        {
            double q_inner_mult = q1[0] * q2[0] +
                                  q1[1] * q2[1] +
                                  q1[2] * q2[2] +
                                  q1[3] * q2[3];
            return (1.0 - Math.Pow(q_inner_mult, 2));
        }

        public static double quaternion_dot_product_no_w(List<double> q1, List<double> q2)
        {
            double result = q1[1] * q2[1] +
                            q1[2] * q2[2] +
                            q1[3] * q2[3];
            return result;
        }
    }
}
