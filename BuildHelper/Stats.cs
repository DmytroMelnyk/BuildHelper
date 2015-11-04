using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildHelper
{
    public class Stats
    {
        public double Mu { get; private set; }
        public double Sigma { get; private set; }
        public double Dispersion
        {
            get { return Sigma * Sigma; }
            set { }
        }

        public void Calculate(List<long> values)
        {
            if (values.Count == 0)
                return;
            Mu = values.Average();
            double temp = values.Sum(arg => Math.Pow(arg - Mu, 2));
            if (values.Count > 1)
                Sigma = Math.Sqrt(temp / (values.Count - 1)); // Sqrt(dispersion)
            else
                Sigma = 0;
        }
    }
}
