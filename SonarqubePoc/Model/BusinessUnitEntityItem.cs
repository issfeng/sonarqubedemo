using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace Poc.AzureDevOps
{
    public class BusinessUnitEntityItem
    {
        public string CountryCode { get; set; }
        public Entity BusinessUnitEntity { get; set; }

    }
}