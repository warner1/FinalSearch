using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1
{
    class status :TableEntity
    {
        public status(string status)
        {
            this.PartitionKey = "Status";
            this.RowKey = "Status";

            this.Status = status;
        }

        public status() { }

        public string Status { get; set; }
    }
}
