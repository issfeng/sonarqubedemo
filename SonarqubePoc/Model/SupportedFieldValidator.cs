using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poc.AzureDevOps
{
    public class SupportedFieldValidator
    {
        private List<string> supportedFieldNames = new List<string>()
        {
            "RecordType",
            "UnitSerialNumber",
            "PartSerialNumber",
            "PartNumber",
            "PartDescription",
            "ProductSku",
            "ShipDate",
            "CountryCode",
            "ProductName",
            "OrderNumber",
            "InvoiceNumber",
            "ReturnDate",
            "RepairDate",
            "OrderDate",
            "OrderLineNumber",
            "ReplacementPartName",
            "RegistrationDate",
            "RegistrantFirstName",
            "RegistrantLastName",
            "RegistrantEmail",
            "RegistrationAboNumber",
            "RegistrationSource",
            "IsLegacyWarranty",
            "WarrantyStartDate",
            "WarrantyEndDate",
            "ProcessType" //(pass,update,insert)
        };

        public bool IsFieldValid(string fieldName)
        {
            return supportedFieldNames.Contains(fieldName, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
