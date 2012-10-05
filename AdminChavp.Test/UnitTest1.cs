using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Newtonsoft.Json;

namespace AdminChavp.Test
{
    [TestClass]
    public class UnitTest1
    {
        MongoServer mServer = null;
        MongoDatabase mLogs = null;

        [TestInitialize]
        public void SetUp()
        {
            mServer = MongoServer.Create("mongodb://appsit01");
            mLogs = mServer.GetDatabase("logs");
        }

        [TestMethod]
        public void GetPerformance()
        {
            using (mServer.RequestStart(mLogs))
            {
                var servicePerformances = mLogs.GetCollection("service_performance");

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

                var finalize = new BsonJavaScript(@"
                function(key, value){
                      return value;
                }");

                var options = new MapReduceOptionsBuilder();
                options.SetFinalize(finalize);
                options.SetOutput(MapReduceOutput.Inline);

                var results = servicePerformances.MapReduce(map, reduce, options);

                int totalCal = 0;
                foreach (var result in results.GetResults())
                {
                    var doc = result.ToBsonDocument();
                    var id = doc["_id"].AsBsonDocument;
                    var value = doc["value"].AsBsonDocument;
                    totalCal += Convert.ToInt32(value["count"].AsDouble);

                    string className = id["class"].AsString;
                    string method = id["method"].AsString;
                    string machineName = id["machineName"].AsString;
                }

                var all = servicePerformances.FindAll().ToList().Count;

                Assert.AreEqual(all, totalCal);
            }
        }

        [TestMethod]
        public void GetDetailOfMethod()
        {
            using (mServer.RequestStart(mLogs))
            {
                var servicePerformances = mLogs.GetCollection("service_performance");

                var map = new BsonJavaScript(@"
                function() { 
                    var me = this;                                                                 
                    var key = {};
                    key.day = me.createdDate.getDay();
                    key.month = me.createdDate.getMonth();
                    key.year = me.createdDate.getFullYear();

                    if(me.machineName === 'APPSIT01'){
                        if(me.class === 'DeviceService'){
                            if(me.method === 'GetDeployedGeoFenceConf'){
                                emit(key, { count: 1, totalElapsedMilliseconds: me.elapsedMilliseconds });
                            }
                        }
                    }                                                            
                }");
                var reduce = new BsonJavaScript(@"
                function(key, values) {
                    var result = { count: 0, totalElapsedMilliseconds: 0.0 };
                    values.forEach(function(value){               
                        result.count += value.count;
                        result.totalElapsedMilliseconds += value.totalElapsedMilliseconds;
                    });
                    return result;
                }");

                var finalize = new BsonJavaScript(@"
                function(key, value){
                      value.averageElapsedMilliseconds = value.totalElapsedMilliseconds / value.count;
                      return value;
                }");

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

                    string toDay = string.Format("{0}/{1}/{2}", day, month, year);
                    Console.WriteLine(
                        string.Format("{0} - {1} - {2} - {3}",
                        toDay, count, totalElapsedMilliseconds, averageElapsedMilliseconds));
                }

                var query = Query.And(
                        Query.EQ("machineName", "APPSIT01"),
                        Query.EQ("class", "DeviceService"),
                        Query.EQ("method", "GetDeviceByImei")
                    );

                var all = servicePerformances.Find(query).AsEnumerable();
                int totalEMs = 0;

                foreach (var result in all)
                {
                    var doc = result.ToBsonDocument();
                    
                    var elapsedMilliseconds = Convert.ToInt32(doc[4].RawValue);
                    totalEMs += elapsedMilliseconds;
                }

                Console.WriteLine(totalEMs);
            }
        }

        [TestCleanup]
        public void TearDown()
        {
            mServer.Disconnect();
        }
    }
}
