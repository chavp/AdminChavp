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

                    int totalCal = Convert.ToInt32(value["count"]);

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

            var v = dicOfIList.Values.OrderBy(item => item.Name).ToList();
            return View(v);
        }

        public ActionResult MethodInfo(string machineName, string serviceName, string methodName)
        {
            IList<MethodDetailModel> methodDetailModelList = new List<MethodDetailModel>();
            ViewBag.MachineName = machineName;
            ViewBag.ServiceName = serviceName;
            ViewBag.MethodName = methodName;

            MongoServer server = MongoServer.Create("mongodb://appsit01");
            var database = server.GetDatabase("logs");
            using (server.RequestStart(database))
            {
                var servicePerformances = database.GetCollection("service_performance");

                var map = @"
                function() { 
                    var me = this;                                                                 
                    var key = {};
                    key.day = me.createdDate.getDay();
                    key.month = me.createdDate.getMonth();
                    key.year = me.createdDate.getFullYear();

                    if(me.machineName === ':machineName'){
                        if(me.class === ':serviceName'){
                            if(me.method === ':methodName'){
                                emit(key, { count: 1, totalElapsedMilliseconds: me.elapsedMilliseconds });
                            }
                        }
                    }                                                            
                }";
                map = map.Replace(":machineName", machineName);
                map = map.Replace(":serviceName", serviceName);
                map = map.Replace(":methodName", methodName);

                var reduce = @"
                function(key, values) {
                    var result = { count: 0, totalElapsedMilliseconds: 0 };
                    values.forEach(function(value){               
                        result.count += value.count;
                        result.totalElapsedMilliseconds += value.totalElapsedMilliseconds;
                    });
                    return result;
                }";

                var finalize = @"
                function(key, value){
                      value.averageElapsedMilliseconds = value.totalElapsedMilliseconds / value.count;
                      return value;
                }";

                var options = new MapReduceOptionsBuilder();
                options.SetFinalize(finalize);
                options.SetOutput(MapReduceOutput.Inline);

                var results = servicePerformances.MapReduce(map, reduce, options);
                foreach (var result in results.GetResults())
                {
                    var doc = result.ToBsonDocument();
                    var id = doc["_id"].AsBsonDocument;
                    var value = doc["value"].AsBsonDocument;
                    var count = Convert.ToInt32(value["count"]);
                    var averageElapsedMilliseconds = Convert.ToDouble(value["averageElapsedMilliseconds"]);
                    var totalElapsedMilliseconds = Convert.ToInt32(value["totalElapsedMilliseconds"]);

                    var day = Convert.ToInt32(id["day"]);
                    var month = Convert.ToInt32(id["month"]);
                    var year = Convert.ToInt32(id["year"]);

                    string toDay = string.Format("{0}/{1}/{2}", month, day, year);

                    MethodDetailModel methodDetailModel = new MethodDetailModel
                    {
                        Date = new DateTime(year, month, day),
                        MachineName = machineName,
                        ServiceName = serviceName,
                        MethodName = methodName,
                        TotalUsage = count,
                        TotalElapsedMilliseconds = totalElapsedMilliseconds,
                        AverageElapsedMilliseconds = averageElapsedMilliseconds,
                    };

                    methodDetailModelList.Add(methodDetailModel);

                }
            }

            methodDetailModelList = methodDetailModelList.OrderByDescending(m => m.Date).ToList();
            return View(methodDetailModelList);
        }
    }
}
