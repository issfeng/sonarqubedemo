using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poc.AzureDevOps
{
    public class RecordTypeValidator
    {
        private List<string> validRecordTypes = new List<string>()
        {
            "durable",
            "component",
            "replacement"
        };

        public bool IsValid(string recordType)
        {
            return validRecordTypes.Contains(recordType, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
