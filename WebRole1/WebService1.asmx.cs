using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;

namespace WebRole1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {

        public static DictionaryTree trie;
        public static List<string> stats;
        private PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available MBytes");

        [WebMethod]
        public void getTits()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                 ConfigurationManager.AppSettings["data"]);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("wikidump");
            CloudBlockBlob block = container.GetBlockBlobReference("dictionary.txt");
            var fileloca = System.IO.Path.GetTempPath() + "\\wikidump.txt";
            using (var fileStream = System.IO.File.OpenWrite(fileloca))
            {
                block.DownloadToStream(fileStream);
            }
        }

        [WebMethod]
        public List<string> BuildTrie()
        {
            trie = new DictionaryTree();
            stats = new List<string>();
            var fileloca = System.IO.Path.GetTempPath() + "\\wikidump.txt";
            using (StreamReader sr = new StreamReader(fileloca))
            {
                int count = 0;
                string word = "";
                while (sr.EndOfStream == false)
                {
                    if (count % 1000 == 0 && GetAvaliableMBytes() < 50)
                    {
                        break;
                    }
                    string line = sr.ReadLine();
                    trie.add(line);
                    word = line;
                    count++;
                }
                stats.Add(word);
                stats.Add(count.ToString());
            }
            return stats;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<string> Search(string input)
        {
            return trie.suggestions(input); ;
        }

        [WebMethod]
        public List<string> getStats()
        {
            return stats;
        }

        private float GetAvaliableMBytes()
        {
            if (memProcess.NextValue() > 50)
            {
                return  memProcess.NextValue();
            }

            return 0;
        }
    }
}
