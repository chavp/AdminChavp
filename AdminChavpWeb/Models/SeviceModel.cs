using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdminChavpWeb.Models
{
    public class SeviceModel
    {
        public string Name { get; set; }

        public SeviceModel()
        {
            Methods = new List<MethodModel>();
        }

        public IList<MethodModel> Methods { get; set; }
    }


}