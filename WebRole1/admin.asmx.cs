using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Services;
using System.Xml;
using WorkerRole1;

namespace WebRole1
{
    /// <summary>
    /// Summary description for admin
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class admin : System.Web.Services.WebService
    {
        public static Dictionary<string, List<string>> cache = new Dictionary<string,List<string>>();

        [WebMethod]
        public String StartCrawl()
        {
            //get reerence to queue
            CloudQueue commandqueue = getCommandref();
            commandqueue.CreateIfNotExists();
            CloudQueueMessage msg = new CloudQueueMessage("start");
            commandqueue.AddMessage(msg);

            return "Lady Bug Deployed";
	    }

        [WebMethod]
        public string StopCrawl()
        {
            //Send Stop Command
            CloudQueue commandqueue = getCommandref();
            commandqueue.CreateIfNotExists();
            CloudQueueMessage msg = new CloudQueueMessage("stop");
            commandqueue.AddMessage(msg);


            return "Lady Bug Grounded!";
        }

        [WebMethod]
        public string ClearAll()
        {
            //Delete Table
            CloudTable urltable = getTableref();
            urltable.DeleteIfExists();

            //Clear Queue
            CloudQueue urlqueue = getQueueref();

            urlqueue.Clear();
            return "All Data Destroyed";
        }

         [WebMethod]
         public int getQueueSize()
         {
             CloudQueue urlqueue = getQueueref();
             urlqueue.CreateIfNotExists();
             urlqueue.FetchAttributes();

             return urlqueue.ApproximateMessageCount.Value;
         }

        [WebMethod]
        public string GetPageTitle(string input)
        {
            input = EncodeUrlInKey(input);
           CloudTable urltable = getTableref();
           TableOperation retrieve = TableOperation.Retrieve<data>("Visited", input);
           TableResult result = urltable.Execute(retrieve);
           if (result.Result != null)
           {
               return ((data)result.Result).Title;
           }
           return "No title Found";
        }

        [WebMethod]
        public string Stats()
        {
            CloudTable urltable = getTableref();
            TableOperation retrieve = TableOperation.Retrieve<stats>("Stats", "Stats");
            TableResult result = urltable.Execute(retrieve);
            List<string> list = new List<string>();
            list.Add(((stats)result.Result).Status);
            list.Add(((stats)result.Result).CpuCounter);
            list.Add(((stats)result.Result).Memorycounter);
            list.Add(((stats)result.Result).Tablesize);
            list.Add(((stats)result.Result).LastTen);
            list.Add(((stats)result.Result).Crawled);

            return new JavaScriptSerializer().Serialize(list);
        }

        [WebMethod]
        public string Error()
        {
            CloudTable urltable = getTableref();
            TableQuery<data> query = new TableQuery<data>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Error"));
            List<string> list = new List<string>();
            foreach (data entity in urltable.ExecuteQuery(query))
            {
                list.Add(entity.Title);
            }

            return new JavaScriptSerializer().Serialize(list);
        }

        private static CloudQueue getCommandref()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["data"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue urlqueue = queueClient.GetQueueReference("com");
            return urlqueue;
        }
        private static CloudQueue getQueueref()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["data"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue urlqueue = queueClient.GetQueueReference("urls");
            return urlqueue;
        }

        private static CloudTable getTableref()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["data"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable urltable = tableClient.GetTableReference("urls");

            return urltable;
        }

        private static string EncodeUrlInKey(string url)
        {
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(url);
            var base64 = System.Convert.ToBase64String(keyBytes);
            return base64.Replace('/', '_');
        }

        private static String DecodeUrlInKey(String encodedKey)
        {
            var base64 = encodedKey.Replace('_', '/');
            byte[] bytes = System.Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        [WebMethod]
        public void clearCache()
        {
            cache.Clear();
        }

        [WebMethod]
        public List<string> getPopular(string input)
        {
            if (cache.ContainsKey(input))
            {
                return cache[input];
            }
            else
            {
                if (cache.Count() == 100)
                {
                    cache.Clear();
                }
                List<data> entities = new List<data>();
                string[] words = input.ToLower().Split(' ');
                CloudTable urltable = getTableref();
                foreach (string s in words)
                {
                    // Construct the query operation for all customer entities where PartitionKey=input.
                    TableQuery<data> query = new TableQuery<data>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, s));
                    if (urltable.ExecuteQuery(query) == null)
                    {
                        return new List<string> { "No Article Titles Match Your Search.", "No Url's Found" };
                    }
                    else
                    {
                        foreach (data entity in urltable.ExecuteQuery(query))
                        {
                            entities.Add(entity);
                        }
                    }
                }

                var result = entities.Select(x => new Tuple<string, string, string>(x.RowKey, x.Title, x.Date))
                    .GroupBy(x => x.Item1)
                    .Select(x => new Tuple<string, string, string, int>(x.Key, x.ToList().First().Item2, x.ToList().First().Item3, x.Count()))
                    .OrderByDescending(x => x.Item4)
                    .ThenByDescending(x => x.Item3)
                    .Take(10);

                List<string> final = new List<string>();
                foreach (var t in result)
                {
                    List<string> list = new List<string>();
                    list.Add(t.Item2);
                    list.Add(DecodeUrlInKey(t.Item1));
                    list.Add(t.Item3);
                    list.Add(t.Item4.ToString());

                    var serialized = new JavaScriptSerializer().Serialize(list);
                    final.Add(serialized);
                }
                cache.Add(input, final);

                return final;
            }
        }
    }
}