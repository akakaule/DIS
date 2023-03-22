using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BH.DIS.MessageStore.States
{
    public class BlobsEventPaging
    {
        public IEnumerable<BlobItem> Blobs { get; set; }
        public string ContinuationToken { get; set; }
    }
}
