using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poc.AzureDevOps
{
    public enum AdhocFileValidationLevel
    {
        Warning, //note for import summary file (e.g. identified a field we don't support for import)
        Error //processing inhibiting validation issue (e.g. no record type field, incomplete information for a record)
    };
}
