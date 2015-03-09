using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.Xml;
using System.Web;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Configuration;
using System.IO;
using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage.Table;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");
      
            try
            {
                while (true)
                {
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["data"]);
                    CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                    CloudQueue commandqueue = queueClient.GetQueueReference("com");
                    commandqueue.CreateIfNotExists();


                    CloudQueueMessage retrievedMessage = commandqueue.GetMessage();

                    CloudTable urltable = getTableref();
                    urltable.CreateIfNotExists();

                    stats stat = new stats("Idle", "0", "0", "0", "","0");
                    TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(stat);
                    urltable.Execute(insertOrReplaceOperation);

                    if (retrievedMessage == null)
                    {
                        Thread.Sleep(5000);
                    }
                    else
                    {
                        string command = retrievedMessage.AsString;
                        commandqueue.DeleteMessage(retrievedMessage);
                        while (command.Equals("start"))
                        {
                            bug ladybug = new bug();
                            ladybug.sortbleach();
                            ladybug.sortcnn();
                            ladybug.crawlSiteMap();
                            ladybug.crawlHTML();
                            break;
                        }
                    }
                }
                    this.RunAsync(this.cancellationTokenSource.Token).Wait();
                }
                finally
                {
                    this.runCompleteEvent.Set();
                }
            
        }

        private static CloudTable getTableref()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["data"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable urltable = tableClient.GetTableReference("urls");

            return urltable;
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}