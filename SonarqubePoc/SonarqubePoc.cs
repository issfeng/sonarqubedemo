using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Newtonsoft.Json;
using System.Configuration;

namespace Poc.AzureDevOps
{
    public class SonarqubePoc
    {
        #region Class Level Members
        static OrganizationServiceProxy _serviceProxy;
        private static List<AdhocFileValidationRecord> validationErrors = new List<AdhocFileValidationRecord>();
        private static readonly SupportedFieldValidator supportedFieldValidator = new SupportedFieldValidator();
        private static List<AdhocImportRecord> records = new List<AdhocImportRecord>();
        private static List<SkuEntityItem> tempSkus = new List<SkuEntityItem>();
        private static List<BusinessUnitEntityItem> tempBusinessUnits = new List<BusinessUnitEntityItem>();
        public static int RecordInGroup = 200;
        public static string csvInputFile;
        public static string csvOutputFile;
        public static string runLogFileName;
        public static string runLogSerialNumber;
        #endregion Class Level Members

        public static void Main(string[] args)
        {
            try
            {                
               if(args.Length == 0 || string.IsNullOrEmpty(args[0]))
                {
                    throw new ArgumentNullException(@"Should run with CSV Input File, e.g. C:\foo\foo.csv");
                }
                csvInputFile = args[0];
                csvOutputFile = csvInputFile.Replace(".csv", "");
                runLogFileName = csvOutputFile + "_RunLog.txt";

                RecordInGroup = string.IsNullOrEmpty(ConfigurationManager.AppSettings["RecordInGroup"]) ? 200 : int.Parse(ConfigurationManager.AppSettings["RecordInGroup"]);

                WriteRunLog($"Job start at: {DateTime.Now}. Connecting to CRM ...");

                _serviceProxy = CreateDurablesServiceClient();

                WriteRunLog($"CRM is connected at: {DateTime.Now}.");

                ProcessRecords();
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                WriteRunLog($"The application terminated with a FaultException while processing serial number: {runLogSerialNumber}." +
                    $"Fault Exception: {ex.StackTrace}");
            }
            catch (TimeoutException ex)
            {
                WriteRunLog($"The application terminated with a TimeoutException while processing serial number: {runLogSerialNumber}." +
                            $"Timeout Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                WriteRunLog($"The application terminated with an error while processing serial number: {runLogSerialNumber}." +
                            $"message: {ex.Message}, Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                if (_serviceProxy != null)
                {
                    _serviceProxy.Dispose();
                }
                WriteRunLog($"Job end at: {DateTime.Now}.");
                //Console.ReadLine();
            }
        }

        public static void ProcessRecords()
        {
            try
            {
                ParseInputFile();

                tempSkus = FetchSKU(records.Select(x => x.ProductSku).ToList());

                tempBusinessUnits = FetchBusinessUnit(records.Select(x => x.CountryCode).ToList());                               

                var recordCount = records.Count();
                var totalGroup = Math.Ceiling(Convert.ToDouble(recordCount) / Convert.ToDouble(RecordInGroup));

                WriteRunLog($"Found {recordCount} record(s) and devided into {totalGroup} group(s) * {RecordInGroup} to process.");

                int groupCount = 1;

                List<AdhocImportRecord> checkRecords = new List<AdhocImportRecord>();

                for (int recordIndex = 0; recordIndex < records.Count(); recordIndex++)
                {
                    checkRecords.Add(records[recordIndex]);
                    runLogSerialNumber = records[recordIndex].UnitSerialNumber;

                    if (recordIndex == (groupCount * RecordInGroup - 1) ||
                        recordIndex == records.Count() - 1)
                    {
                        WriteRunLog($"Start from time: {DateTime.Now}; Process group {groupCount} of {totalGroup}.");

                        var existingDurables = FetchDurable(checkRecords.Select(x => x.UnitSerialNumber).ToList());

                        checkRecords.RemoveAll(x => existingDurables.Exists(y => y.SerialNumber.Equals(x.UnitSerialNumber)));

                        if (checkRecords.Count() > 0)
                        {
                            WriteRecordsToCrm(checkRecords);
                        }

                        groupCount++;
                        checkRecords.Clear();
                    }
                }
            }
            catch (FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>)
            {
                throw;
            }
        }
        public static OrganizationServiceProxy CreateDurablesServiceClient()
        {
            var credentials = new ClientCredentials();
            credentials.UserName.UserName = ConfigurationManager.AppSettings["Dynamics:Username"];
            credentials.UserName.Password = ConfigurationManager.AppSettings["Dynamics:Password"];

            var url = ConfigurationManager.AppSettings["Dynamics:BaseUrl"];

            var proxy = new OrganizationServiceProxy(new Uri(url), null, credentials, null);
            proxy.EnableProxyTypes();
            return proxy;
        }
        public static List<AdhocImportField> ParseFields(string line)
        {
            List<AdhocImportField> fields = new List<AdhocImportField>();
            string[] fieldHeaders = line.Split(',');
            var recordIndex = 0;
            for (var i = 0; i < fieldHeaders.Length; i++)
            {
                var field = new AdhocImportField()
                {
                    FieldIndex = i,
                    FieldName = fieldHeaders[i].Replace(" ", "") //remove all white space if the headings contain any - our expected field names contain none
                };
                if (supportedFieldValidator.IsFieldValid(field.FieldName))
                {
                    fields.Add(field);
                }
                else
                {
                    validationErrors.Add(new AdhocFileValidationRecord
                    {
                        ErrorLevel = AdhocFileValidationLevel.Warning,
                        FileLineId = 1,
                        RecordIndexId = ++recordIndex,
                        ErrorMessage = $"Found a field name ({field.FieldName} in the import file that is not supported.  The data it contains will not be included in the import"
                    });
                }
            }

            return fields;
        }
        public static AdhocImportRecord ParseRecord(string line, List<AdhocImportField> fields)
        {
            string[] values = line.Split(',').Select(x => x.Trim()).ToArray();
            AdhocImportRecord record = new AdhocImportRecord()
            {
                RecordType = MapFieldValue("RecordType", fields, values),
                UnitSerialNumber = MapFieldValue("UnitSerialNumber", fields, values),
                PartSerialNumber = MapFieldValue("PartSerialNumber", fields, values),
                PartNumber = MapFieldValue("PartNumber", fields, values),
                PartDescription = MapFieldValue("PartDescription", fields, values),
                ProductSku = MapFieldValue("ProductSku", fields, values),
                ShipDate = MapDateTimeFieldValue("ShipDate", fields, values),
                CountryCode = MapFieldValue("CountryCode", fields, values),
                ProductName = MapFieldValue("ProductName", fields, values),
                OrderNumber = MapFieldValue("OrderNumber", fields, values),
                InvoiceNumber = MapFieldValue("InvoiceNumber", fields, values),
                ReturnDate = MapDateTimeFieldValue("ReturnDate", fields, values),
                RepairDateTime = MapDateTimeFieldValue("RepairDate", fields, values),
                OrderDate = MapDateTimeFieldValue("OrderDate", fields, values),
                OrderLineNumber = MapFieldValue("OrderLineNumber", fields, values),
                ReplacementPartName = MapFieldValue("ReplacementPartName", fields, values),
                RegistrationDate = MapDateTimeFieldValue("RegistrationDate", fields, values),
                RegistrantFirstName = MapFieldValue("RegistrantFirstName", fields, values),
                RegistrantLastName = MapFieldValue("RegistrantLastName", fields, values),
                RegistrantEmail = MapFieldValue("RegistrantEmail", fields, values),
                RegistrationAboNumber = MapFieldValue("RegistrationAboNumber", fields, values),
                RegistrationSource = MapFieldValue("RegistrationSource", fields, values),
                IsLegacyWarranty = MapBooleanFieldValue("IsLegacyWarranty", fields, values),
                WarrantyStartDate = MapDateTimeFieldValue("WarrantyStartDate", fields, values),
                WarrantyEndDate = MapDateTimeFieldValue("WarrantyEndDate", fields, values)
            };

            return record;
        }
        private static string MapFieldValue(string fieldName, List<AdhocImportField> fields, string[] values)
        {
            var field = GetField(fieldName, fields);

            if (field != null)
            {
                return values[field.FieldIndex];
            }

            return string.Empty;
        }
        private static AdhocImportField GetField(string fieldName, List<AdhocImportField> fields)
        {
            return fields.Where(x => String.Equals(fieldName, x.FieldName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }
        private static DateTime? MapDateTimeFieldValue(string fieldName, List<AdhocImportField> fields, string[] values)
        {
            var field = GetField(fieldName, fields);

            if (field == null || String.IsNullOrEmpty(values[field.FieldIndex])) return null;

            DateTime value;

            if (DateTime.TryParse(values[field.FieldIndex], out value))
            {
                return (DateTime?)value;
            }

            return null;
        }
        private static bool MapBooleanFieldValue(string fieldName, List<AdhocImportField> fields, string[] values)
        {
            var field = GetField(fieldName, fields);

            if (field == null || String.IsNullOrEmpty(values[field.FieldIndex])) return false;

            bool value;

            if (bool.TryParse(values[field.FieldIndex], out value))
            {
                return value;
            }

            return false;
        }
        public static ExecuteTransactionRequest ProcessRecord(AdhocImportRecord record, Entity sku, Entity businessUnit)
        {
            if (record == null)
            {
                return null;
            }
            
            var reqExecTransaction = new ExecuteTransactionRequest()
            {
                Requests = new OrganizationRequestCollection()
            };

            Entity durable = new Entity("dsr_durable", "dsr_serialnumber", record.UnitSerialNumber);

            var skuDsrName = sku.Contains("dsr_name") ? sku.GetAttributeValue<string>("dsr_name") : string.Empty;
            var skuDsrDisplayname = sku.Contains("dsr_displayname") ? sku.GetAttributeValue<string>("dsr_displayname") : string.Empty;

            durable["dsr_name"] = $"{skuDsrName} - {record.UnitSerialNumber}";
            durable["dsr_displayname"] = skuDsrDisplayname;

            durable["dsr_islegacywarranty"] = record.IsLegacyWarranty;

            if (!String.IsNullOrWhiteSpace(record.ProductSku))
            {
                durable["new_skunumber"] = record.ProductSku;
                durable["dsr_skuid"] = new EntityReference(sku.LogicalName, sku.Id);
            }
            durable["dsr_relatedbusinessunit"] = businessUnit == null ? null : new EntityReference(businessUnit.LogicalName, businessUnit.Id);
            durable["dsr_shipdate"] = record.ShipDate;

            if (!String.IsNullOrWhiteSpace(record.OrderNumber))
            {
                durable["dsr_ordernumber"] = record.OrderNumber;
            }

            if (!String.IsNullOrWhiteSpace(record.InvoiceNumber))
            {
                durable["dsr_invoicenumber"] = record.InvoiceNumber;
            }

            durable["dsr_registrationdate"] = record.RegistrationDate;

            UpsertRequest upsertDurableRequest = new UpsertRequest();
            upsertDurableRequest.Target = durable;
            reqExecTransaction.Requests.Add(upsertDurableRequest);


            if (businessUnit != null && record.RegistrationDate != null)
            {
                Entity contact = new Entity("contact", Guid.NewGuid());
                contact["firstname"] = record.RegistrantFirstName;
                contact["lastname"] = record.RegistrantLastName;
                contact["emailaddress1"] = record.RegistrantEmail;
                contact["dsr_abonumber"] = record.RegistrationAboNumber;

                CreateRequest createContactRequest = new CreateRequest() { Target = contact };
                reqExecTransaction.Requests.Add(createContactRequest);

                Entity registration = new Entity("dsr_registration", Guid.NewGuid());
                registration["dsr_name"] = $"Registration: {record.RegistrantEmail} Serial: {record.UnitSerialNumber}";
                registration["dsr_businessunitid"] = new EntityReference("businessunit", businessUnit.Id);
                registration["dsr_productid"] = new EntityReference("dsr_durable", "dsr_serialnumber", record.UnitSerialNumber);
                registration["dsr_customerid"] = new EntityReference("contact", contact.Id);
                registration["dsr_registrationdate"] = record.RegistrationDate;
                registration["new_registrationsource"] = string.IsNullOrWhiteSpace(record.RegistrationSource) ? "adhoc -file-import" : record.RegistrationSource;

                CreateRequest createRegistrationRequest = new CreateRequest() { Target = registration };
                reqExecTransaction.Requests.Add(createRegistrationRequest);


                Entity upsertDurable = new Entity("dsr_durable", "dsr_serialnumber", record.UnitSerialNumber);
                upsertDurable["dsr_relatedregistration"] = new EntityReference(registration.LogicalName, registration.Id);
                UpsertRequest upsertDurableReq = new UpsertRequest() { Target = upsertDurable };
                reqExecTransaction.Requests.Add(upsertDurableReq);

            }



            if (record.IsLegacyWarranty)
            {
                TimeSpan duration = record.WarrantyEndDate.Value - record.WarrantyStartDate.Value;
                Entity serviceContract = new Entity("dsr_servicecontract", Guid.NewGuid());
                serviceContract["dsr_name"] = $"Legacy Service Contract - {record.UnitSerialNumber}";
                serviceContract["dsr_startdate"] = record.WarrantyStartDate;
                serviceContract["dsr_scduration"] = duration.Days;
                serviceContract["dsr_productid"] = new EntityReference("dsr_durable", "dsr_serialnumber", record.UnitSerialNumber);

                CreateRequest createServiceContractRequest = new CreateRequest() { Target = serviceContract };
                reqExecTransaction.Requests.Add(createServiceContractRequest);
            }


            return reqExecTransaction;
        }
        public static void WriteListFile(List<AdhocImportRecord> sourceList, string filedName)
        {

            
            StreamWriter resultfile = new StreamWriter(filedName, true);
            foreach(AdhocImportRecord record in sourceList)
            {
                string json = JsonConvert.SerializeObject(record);
                resultfile.Write(json);
                resultfile.Write(Environment.NewLine);
            }

            resultfile.Close();
        }
        public static void WriteRunLog(string runLog)
        {
            Console.WriteLine(runLog);
            if(!string.IsNullOrEmpty(runLogFileName))
            {
                StreamWriter resultfile = new StreamWriter(runLogFileName, true);
                resultfile.Write(runLog);
                resultfile.Write(Environment.NewLine);
                resultfile.Close();
            }
        }
        private static String GetFaultInfo(OrganizationRequest organizationRequest, int count,
        OrganizationServiceFault organizationServiceFault)
        {
            return $"A fault occurred when processing {organizationRequest.RequestName} request, " + 
                $"at index {count + 1} in the request collection with a fault message: {organizationServiceFault.Message}\r\n";
        }
        public static void ParseInputFile()
        {
            if (string.IsNullOrEmpty(csvInputFile) || string.IsNullOrEmpty(csvOutputFile))
            {
                throw new ArgumentException("Parameter csvInputFile cannot be null");
            }                         
            List<AdhocImportField> fields = new List<AdhocImportField>();
            var index = 0;
            using (var x = File.OpenText(csvInputFile))
            {
                var line = string.Empty;
                while (!x.EndOfStream)
                {
                    line = x.ReadLine();
                    string[] cells = line.Split(',');
                    if (index == 0)
                    {
                        fields = ParseFields(line);
                    }
                    else
                    {
                        records.Add(ParseRecord(line, fields));
                        records.Last().LineNumber = index + 1;
                    }

                    index++;
                }
            }
        }
        public static List<DurableItem> FetchDurable(List<string> serialNumbers)
        {
            if(serialNumbers == null || serialNumbers.Count == 0)
            {
                return null;
            }
            QueryExpression durableQuery = new QueryExpression()
            {
                Distinct = false,
                EntityName = "dsr_durable",
                ColumnSet = new ColumnSet(true),
                Criteria =
                        {
                            Filters =
                            {
                                new FilterExpression
                                {
                                    FilterOperator = LogicalOperator.And,
                                    Conditions =
                                    {
                                        new ConditionExpression("dsr_serialnumber", ConditionOperator.In, serialNumbers.ToArray())
                                    },
                                }
                            }
                        }
            };

            var results = _serviceProxy.RetrieveMultiple(durableQuery).Entities;
            return results.AsQueryable().Select(x => new DurableItem
            {
                SerialNumber = x.GetAttributeValue<string>("dsr_serialnumber"),
                DurableEntity = x
            }).ToList();
        }
        public static List<SkuEntityItem> FetchSKU(List<string> skuValues)
        {
            if (skuValues == null || skuValues.Count == 0)
            {
                return null;
            }
            QueryExpression skuQuery = new QueryExpression()
            {
                Distinct = true,
                EntityName = "dsr_sku",
                ColumnSet = new ColumnSet(true),
                Criteria =
                        {
                            Filters =
                            {
                                new FilterExpression
                                {
                                    FilterOperator = LogicalOperator.And,
                                    Conditions =
                                    {
                                        new ConditionExpression("dsr_skuvalue", ConditionOperator.In, skuValues.Distinct().ToArray())
                                    },
                                }
                            }
                        }
            };

            var results = _serviceProxy.RetrieveMultiple(skuQuery).Entities;
            return results.AsQueryable().Select(x => new SkuEntityItem
            {
                SkuValue = x.GetAttributeValue<string>("dsr_skuvalue"),
                SkuEntity = x
            }).ToList();
        }
        public static List<BusinessUnitEntityItem> FetchBusinessUnit(List<string> countryCodes)
        {
            if (countryCodes == null || countryCodes.Count == 0)
            {
                return null;
            }
            QueryExpression buQuery = new QueryExpression()
            {
                Distinct = false,
                EntityName = "businessunit",
                ColumnSet = new ColumnSet(true),
                Criteria =
                        {
                            Filters =
                            {
                                new FilterExpression
                                {
                                    FilterOperator = LogicalOperator.And,
                                    Conditions =
                                    {
                                        new ConditionExpression("dsr_countrycode", ConditionOperator.In, countryCodes.Distinct().ToArray())
                                    },
                                }
                            }
                        }
            };

            var results = _serviceProxy.RetrieveMultiple(buQuery).Entities;
            return results.AsQueryable().Select(x => new BusinessUnitEntityItem
            {
                CountryCode = x.GetAttributeValue<string>("dsr_countrycode"),
                BusinessUnitEntity = x
            }).ToList();
        }
        public static void WriteRecordsToCrm(List<AdhocImportRecord> newRecords)
        {
            List<AdhocImportRecord> redoRecords = new List<AdhocImportRecord>();

            ExecuteMultipleRequest executeMultipeRequest = new ExecuteMultipleRequest()
            {
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = true,
                    ReturnResponses = true
                },
                Requests = new OrganizationRequestCollection()
            };

            SkuEntityItem skuItem = null;
            BusinessUnitEntityItem businessUnitItem = null;

            foreach (AdhocImportRecord itemRecord in newRecords)
            {
                skuItem = tempSkus.Where(x => x.SkuValue == itemRecord.ProductSku).FirstOrDefault();
                businessUnitItem = tempBusinessUnits.Where(x => x.CountryCode == itemRecord.CountryCode).FirstOrDefault();

                if (skuItem != null && businessUnitItem != null)
                {
                    itemRecord.ProcessType += "New create;";
                    redoRecords.Add(itemRecord);
                    ExecuteTransactionRequest newRequest = ProcessRecord(itemRecord, skuItem.SkuEntity, businessUnitItem.BusinessUnitEntity);
                    executeMultipeRequest.Requests.Add(newRequest);
                }
                else
                {
                    itemRecord.ProcessType += "Can't create due to empty SKU or BusinessUnit;";
                    redoRecords.Add(itemRecord);
                }
            }


            string redoRecordsFile = csvOutputFile + "_redoRecords.txt";
            WriteListFile(redoRecords, redoRecordsFile);

            string executeMultipeRequestFile = csvOutputFile + "_executeMultipeResult.txt";
            StreamWriter redoRecordsResultfile = new StreamWriter(executeMultipeRequestFile, true);


            #region Execute Multiple Request within Transation
            if (executeMultipeRequest.Requests.Count() > 0)
            {
                try
                {
                    // Execute Multiple Request
                    ExecuteMultipleResponse response = (ExecuteMultipleResponse)_serviceProxy.Execute(executeMultipeRequest);

                    String faultMesg = String.Empty;

                    foreach (ExecuteMultipleResponseItem responseItem in response.Responses)
                    {
                        if (responseItem.Fault != null)
                        {
                            faultMesg += GetFaultInfo(executeMultipeRequest.Requests[responseItem.RequestIndex],
                                                        responseItem.RequestIndex, responseItem.Fault);
                        }
                    }
                    if (faultMesg != String.Empty)
                    {
                        redoRecordsResultfile.Write(faultMesg);
                    }
                }
                catch (FaultException<OrganizationServiceFault> mex)
                {
                    redoRecordsResultfile.Write($"Fail to fix import data due to {mex.Message}.");
                }
            }
            redoRecordsResultfile.Close();
            #endregion Execute Multiple Request within Transation
        }
    }
}