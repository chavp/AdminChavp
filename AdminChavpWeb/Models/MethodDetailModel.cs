using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdminChavpWeb.Models
{
    public class MethodDetailModel
    {
        public DateTime Date { get; set; }

        public string MachineName { get; set; }
        public string ServiceName { get; set; }
        public string MethodName { get; set; }

        public int TotalUsage { get; set; }

        public double TotalElapsedMilliseconds { get; set; }
        public double AverageElapsedMilliseconds { get; set; }

    }
}