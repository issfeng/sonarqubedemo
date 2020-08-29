using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poc.AzureDevOps
{
    public class AdhocFileValidationRecord
    {
        public int FileLineId { get; set; } //line number in file
        public int RecordIndexId { get; set; } //error index for the record (1-x per record) 
        public AdhocFileValidationLevel ErrorLevel { get; set; }
        public string ErrorMessage { get; set; }

    }
}
