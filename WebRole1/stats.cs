using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1
{
    class stats : TableEntity
    {
        public stats(string status, string cpu, string memory, string size, string lastten, string urlcrawl)
        {
            this.PartitionKey = "Stats";
            this.RowKey = "Stats";

            this.Status = status;
            this.CpuCounter = cpu;
            this.Memorycounter = memory;
            this.Tablesize = size;
            this.LastTen = lastten;
            this.Crawled = urlcrawl;
        }

        public stats() { }

        public string Crawled { get; set; }
        public string Status { get; set; }
        public string LastTen { get; set; }
        public string CpuCounter { get; set; }
        public string Memorycounter { get; set; }
        public string Tablesize { get; set; }

    }
}
