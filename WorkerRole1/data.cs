using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public class data : TableEntity
    {
        public data(string partitle, string encourl, string title, string date)
        {
            this.PartitionKey = partitle;
            this.RowKey = encourl;

            this.Title = title;
            this.Date = date;
        }

        public data() { }

        public string Title { get; set; }

        public string Date { get; set; }

    }
}
