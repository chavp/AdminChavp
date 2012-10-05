using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdminChavpWeb.Models
{
    public class MachineModel
    {
        public MachineModel()
        {
            Services = new List<SeviceModel>();
        }

        public string Name { get; set; }
        public IList<SeviceModel> Services { get; set; }
    }
}