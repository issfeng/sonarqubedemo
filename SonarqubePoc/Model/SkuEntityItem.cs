using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace Poc.AzureDevOps
{
    public class SkuEntityItem
    {
        public string SkuValue { get; set; }
        public Entity SkuEntity { get; set; }

    }
}