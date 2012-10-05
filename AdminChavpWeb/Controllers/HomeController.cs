using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MongoDB.Driver;
using AdminChavpWeb.Models;
using MongoDB.Driver.Builders;
using MongoDB.Bson;

namespace AdminChavpWeb.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "The CarPass Devices Services Monitoring";

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your quintessential app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your quintessential contact page.";

            return View();
        }

        public ActionResult Services()
        {
            Dictionary<string, MachineModel> dicOfIList = new Dictionary<string, MachineModel>();
            Dictionary<string, SeviceModel> dicOfSeviceModelIList = new Dictionary<string, SeviceModel>();

            MongoServer server = MongoServer.Create("mongodb://appsit01");
            var database = server.GetDatabase("logs");
            using (server.RequestStart(database))
            {
                var servicePerformances = database.GetCollection("service_performance");

                var map = new BsonJavaScript(@"
                function() {                                                                  
                    var key = {};
                    key.class = this.class;
                    key.method = this.method;
                    key.machineName = this.machineName;
                    emit(key, { count: 1 });                                                            
                }");
                var reduce = new BsonJavaScript(@"
                function(key, values) {
                    var result = { count: 0 };
                    values.forEach(function(value){               
                        result.count += value.count;
                    });
                    return result
                }");

                var results = servicePerformances.MapReduce(map, reduce);
                foreach (var result in results.GetResults())
                {
                    var doc = result.ToBsonDocument();
                    var id = doc["_id"].AsBsonDocument;
                    var value = doc["value"].AsBsonDocument;

                    string className = id["class"].AsString;
                    string method = id["method"].AsString;
                    string machineName = id["machineName"].AsString;

                    int totalCal = Convert.ToInt32(value["count"].AsDouble);

                    if (!dicOfIList.ContainsKey(machineName))
                    {
                        dicOfIList.Add(machineName, new MachineModel
                        {
                            Name = machineName,
                        });

                    }

                    if (!dicOfSeviceModelIList.ContainsKey(machineName + className))
                    {
                        var seviceModel = new SeviceModel
                        {
                            Name = className,
                        };

                        dicOfSeviceModelIList.Add(machineName + className, seviceModel);
                        dicOfIList[machineName].Services.Add(seviceModel);
                    }

                    dicOfSeviceModelIList[machineName + className].Methods.Add(new MethodModel
                    {
                        Name = method,
                        TotalUsage = totalCal,
                    });
                }
            }

            return View(dicOfIList.Values.ToList());
        }

        public ActionResult MethodInfo(string machineName, string serviceName, string methodName)
        {
            ViewBag.MachineName = machineName;
            ViewBag.ServiceName = serviceName;
            ViewBag.MethodName = methodName;

            return View();
        }
    }
}
