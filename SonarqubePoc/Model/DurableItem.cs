using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace Poc.AzureDevOps
{
    public class DurableItem
    {
        public string SerialNumber { get; set; }
        public Entity DurableEntity { get; set; }

    }
}
