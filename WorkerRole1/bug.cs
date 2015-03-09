using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace WorkerRole1
{

    class bug
    {
        public static List<string> cnndis;
        public static List<string> bleachdis;
        public static List<string> dupes;
        public static List<string> sitemap;
        public static List<string> LastTen;
        public static List<string> LastTenError;
        public static int count;
        public static int crawled;
        public static PerformanceCounter theCPUCounter;
        public static PerformanceCounter theMemCounter;
        public bug()
        {
            dupes = new List<string>();
            bleachdis = new List<string>();
            cnndis = new List<string>();
            sitemap = new List<string>();
            LastTen = new List<string>();
            LastTenError = new List<string>();
            count = 0;
            crawled = 0;
            theCPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            theMemCounter = new PerformanceCounter("Memory", "Available MBytes");
        }

        public void crawlHTML()
        {
            CloudQueue urlqueue = getQueueref();
            urlqueue.CreateIfNotExists();

            CloudTable urltable = getTableref();
            urltable.CreateIfNotExists();

            //go deeper......
            urlqueue.FetchAttributes();
            while (urlqueue.ApproximateMessageCount.Value > 0)
            {
                CloudQueue commandq = getCommandref();
                commandq.CreateIfNotExists();

                CloudQueueMessage commandMessage = commandq.GetMessage();

                if (commandMessage != null)
                {
                    string command = commandMessage.AsString;
                    commandq.DeleteMessage(commandMessage);
                    break;
                }
                else
                {

                    CloudQueueMessage retrievedMessage = urlqueue.GetMessage();
                    string url = retrievedMessage.AsString;
                    
                    if (url.Contains("cnn.com/") || url.Contains("bleacherreport.com/"))
                    {
                        try
                        {
                            dupes.Add(url);
                            var page = new HtmlWeb().Load(url);
                            string date = "No Date Found";
                            if (url.Contains("cnn.com/"))
                            {
                                date = page.DocumentNode.SelectSingleNode("//meta[@itemprop='dateCreated']").GetAttributeValue("content", "No Date Found");
                            }
                            string title = page.DocumentNode.SelectSingleNode("//head/title").InnerHtml;
                            string[] words = title.Split(' ');
                            foreach (string s in words)
                            {
                                if (!s.Equals("|"))
                                {
                                    string cleantitle = RemoveSpecialCharacters(s);
                                    data data = new data(cleantitle, EncodeUrlInKey(url), title, date);
                                    TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(data);
                                    urltable.ExecuteAsync(insertOrReplaceOperation);
                                }
                            }
                            count++;
                            crawled++;
                            AddStats(url);


                            string href = "";
                            foreach (HtmlNode node in page.DocumentNode.SelectNodes("//a[@href]"))
                            {
                                href = node.Attributes["href"].Value.ToString();
                                if (href.StartsWith("//"))
                                {
                                    href = "https:" + href;
                                }
                                else if (href.StartsWith("/"))
                                {
                                    if (url.Contains("cnn.com/"))
                                    {
                                        href = "https://cnn.com" + href;
                                    }
                                    else
                                    {
                                        href = "https://bleacherreport.com" + href;
                                    }
                                }
                                if ((href.Contains("cnn.com/") || href.Contains("bleacherreport.com/")) && check(href))
                                {
                                    CloudQueueMessage msg = new CloudQueueMessage(href);
                                    urlqueue.AddMessageAsync(msg);
                                    dupes.Add(href);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            count++;
                            addError(url, e.Message.ToString());
                        }
                        urlqueue.DeleteMessage(retrievedMessage);
                    }
                }
            }
                    
        }

        public static string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().ToLower();
        }

        public void addError(string nurl, string message)
        {
            if (LastTenError.Count < 10)
            {
                LastTenError.Add("Error Message: " + message + " URl: " + nurl);
            }
            else
            {
                LastTenError.RemoveAt(0);
                LastTenError.Add("Error Message: " + message + " URl: " + nurl);
            }
            CloudTable urltable = getTableref();
            urltable.CreateIfNotExists();

            data error = new data("Error", "Error", string.Join(",", LastTenError.ToArray()), ""); 
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(error);
            urltable.Execute(insertOrReplaceOperation);
        }

        public void AddStats(string nurl)
        {
            if (LastTen.Count < 10)
            {
                LastTen.Add(nurl);
            }
            else
            {
                LastTen.RemoveAt(0);
                LastTen.Add(nurl);
            }
            CloudTable urltable = getTableref();
            urltable.CreateIfNotExists();

            stats stat = new stats("Crawling", theCPUCounter.NextValue().ToString(), theMemCounter.NextValue().ToString(), count.ToString(), string.Join(",", LastTen.ToArray()), crawled.ToString());
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(stat);
            urltable.Execute(insertOrReplaceOperation);
            
        }

        public void crawlSiteMap()
        {
            CloudTable urltable = getTableref();
            urltable.CreateIfNotExists();

            stats stat = new stats("Loading", theCPUCounter.NextValue().ToString(), theMemCounter.NextValue().ToString(), "0", "", "0");
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(stat);
            urltable.Execute(insertOrReplaceOperation);

            CloudQueue urlqueue = getQueueref();
            urlqueue.CreateIfNotExists();

            //Parse through xml save url links
            foreach (string map in sitemap)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(map);
                XmlNode root = doc.DocumentElement;
                if (root.Name.Equals("urlset"))
                {
                    
                    XmlNodeList list = doc.GetElementsByTagName("loc");
                    foreach (XmlNode node in list)
                    {
                        if (check(node.InnerText))
                        {
                            CloudQueueMessage msg = new CloudQueueMessage(node.InnerText);
                            urlqueue.AddMessage(msg);
                            dupes.Add(node.InnerText);
                        }
                    }
                }
                else if (root.Name.Equals("sitemapindex"))
                {
                    XmlNodeList list = doc.GetElementsByTagName("loc");
                    foreach (XmlNode node in list)
                    {
                        if (node.InnerText.Contains("2015") || !node.InnerText.Contains("-20"))
                        {
                            parsexml(node.InnerText);
                        }

                    }
                }
            }

                
        }

        private static void parsexml(string map)
        {
            CloudQueue urlqueue = getQueueref();
            urlqueue.CreateIfNotExists();

            XmlDocument doc = new XmlDocument();
            doc.Load(map);
            XmlNode root = doc.DocumentElement;
            if (root.Name.Equals("urlset") && root.HasChildNodes)
            {
                XmlNodeList list = doc.GetElementsByTagName("loc");
                foreach (XmlNode node in list)
                {
                    if (check(node.InnerText))
                    {
                        CloudQueueMessage msg = new CloudQueueMessage(node.InnerText);
                        urlqueue.AddMessageAsync(msg);
                        dupes.Add(node.InnerText);
                    }
                }
            }
            if (root.Name.Equals("sitemapindex"))
            {
                XmlNodeList list = doc.GetElementsByTagName("loc");
                foreach (XmlNode node in list)
                {
                    if (node.InnerText.Contains("2015") || !node.InnerText.Contains("-20"))
                    {
                        parsexml(node.InnerText);
                    }
                }
            }
        }

        private static Boolean check(string url)
        {
            Boolean add = true;
            if (url.Contains("bleacherreport"))
            {
                foreach (string nope in bleachdis)
                {
                    if (url.Contains(nope))
                    {
                        add = false;
                    }
                }
            }
            else if(url.Contains("cnn"))
            {
                foreach (string nope2 in cnndis)
                {
                    if (url.Contains(nope2))
                    {
                        add = false;
                    }
                }
            }
            foreach(string nope3 in dupes) {
                if(url.Equals(nope3)) {
                    add = false;
                }
            }
            return add;
        }

        public void sortbleach()
        {
            CloudTable urltable = getTableref();
            urltable.CreateIfNotExists();

            stats stat = new stats("Loading", theCPUCounter.NextValue().ToString(), theMemCounter.NextValue().ToString(), "0", "","0");
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(stat);
            urltable.Execute(insertOrReplaceOperation);

            HttpWebRequest request = (HttpWebRequest) WebRequest.Create("http://bleacherreport.com/robots.txt");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream resStream = response.GetResponseStream();
            StreamReader sr = new StreamReader(resStream);
            String line = sr.ReadLine();
            while (line != null)
            {
                string url = line.Substring(line.IndexOf(" ") + 1);
                if (line.Contains("Disallow"))
                {
                    bleachdis.Add(url + "/");
                }
                else if (line.Contains("Sitemap"))
                {
                    if (url.Contains("http://bleacherreport.com/sitemap/nba.xml"))
                    {
                        dupes.Add(url);
                        sitemap.Add(url);
                    }
                }
                line = sr.ReadLine();
            }
        }
        public void sortcnn()
        {
            HttpWebRequest request = (HttpWebRequest)
            WebRequest.Create("http://www.cnn.com/robots.txt");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream resStream = response.GetResponseStream();
            StreamReader sr = new StreamReader(resStream);
            String line = sr.ReadLine();
            while (line != null)
            {
                string url = line.Substring(line.IndexOf(" ") + 1);
                if (line.Contains("Disallow"))
                {
                    cnndis.Add(url + "/");
                }
                else if (line.Contains("Sitemap"))
                {
                    sitemap.Add(url);
                }
                line = sr.ReadLine();
            }
        }

        private static  CloudQueue getQueueref()
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

        public int getqueuesize()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["data"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue urlqueue = queueClient.GetQueueReference("urls");
            urlqueue.CreateIfNotExists();
            urlqueue.FetchAttributes();
            return urlqueue.ApproximateMessageCount.Value;
        }

        private static CloudQueue getCommandref()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["data"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue urlqueue = queueClient.GetQueueReference("com");
            return urlqueue;
        }

        private static string EncodeUrlInKey(string url)
        {
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(url);
            var base64 = System.Convert.ToBase64String(keyBytes);
            return base64.Replace('/', '_');
        }
    }
}
