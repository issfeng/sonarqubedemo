using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poc.AzureDevOps
{
    public class AdhocImportRecord
    {
        //Record Type 
        public string RecordType { get; set; } //(Durable, Component, Replacement)
        public int LineNumber { get; set; }

        //Add Manufacture Data
        public string UnitSerialNumber { get; set; }
        public string PartSerialNumber { get; set; }
        public string PartNumber { get; set; }
        public string PartDescription { get; set; }
        public string ProductSku { get; set; }

        //Add ship event fields
        public DateTime? ShipDate { get; set; }
        public string CountryCode { get; set; }
        public string ProductName { get; set; }
        public string OrderNumber { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime? ReturnDate { get; set; }

        //Add Replacement Part Fields 
        public DateTime? RepairDateTime { get; set; }
        public DateTime? OrderDate { get; set; }
        public string OrderLineNumber { get; set; }
        public string ReplacementPartName { get; set; }

        //Add Registration Basics
        public DateTime? RegistrationDate { get; set; }
        public string RegistrantFirstName { get; set; }
        public string RegistrantLastName { get; set; }
        public string RegistrantEmail { get; set; }
        public string RegistrationAboNumber { get; set; }
        public string RegistrationSource { get; set; }

        //Add historical record information
        public bool IsLegacyWarranty { get; set; }
        public DateTime? WarrantyStartDate { get; set; }
        public DateTime? WarrantyEndDate { get; set; }
        public string ProcessType { get; set; }
    }
}
