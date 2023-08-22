using System;
using System.Collections.Generic;
using System.Linq;

namespace FitMiAndroid
{
    public static class LinearAlgebra
    {
        public static double Norm(List<double> v, double p = 2)
        {
            //(sum(abs(this[i]) ^ p)) ^ (1 / p)
            return Math.Pow(v.Select(x => Math.Pow(Math.Abs(x), p)).Sum(), 1.0 / p);
        }
    }
}