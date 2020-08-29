using Microsoft.VisualStudio.TestTools.UnitTesting;
using Poc.AzureDevOps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poc.AzureDevOps.Tests
{
    [TestClass()]
    public class SonarqubePocTests
    {
        [Ignore]
        [TestMethod()]
        public void MainTest()
        {
            string[] testArgs = { @"" };

            SonarqubePoc.Main(testArgs);
            
            Assert.Fail();
        }

        [Ignore]
        [TestMethod()]
        public void RunTest()
        {
            Assert.Fail();
        }

        [Ignore]
        [TestMethod()]
        public void CreateDurablesServiceClientTest()
        {
            Assert.Fail();
        }

        [Ignore]
        [TestMethod()]
        public void ParseFieldsTest()
        {
            Assert.Fail();
        }

        [Ignore]
        [TestMethod()]
        public void ParseRecordTest()
        {
            Assert.Fail();
        }

        [Ignore]
        [TestMethod()]
        public void ProcessRecordTest()
        {
            Assert.Fail();
        }

        [Ignore]
        [TestMethod()]
        public void WriteListFileTest()
        {
            Assert.Fail();
        }

        [Ignore]
        [TestMethod()]
        public void WriteRunLogTest()
        {
            Assert.Fail();
        }
    }
}