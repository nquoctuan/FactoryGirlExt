using System;
using System.Data.SQLite;
using FactoryGirlExt;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FactoryGirlExtTest
{
    [TestClass]
    public class FactoryGirlExtTest
    {
        private const string ConnectionString = "Data Source=MyDatabase.sqlite;Version=3;";

        public TestContext TestContext { get; set; }

        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            SQLiteConnection.CreateFile("TestDb");

            var dbConnection = new SQLiteConnection("Data Source=MyDatabase.sqlite;Version=3;");

            dbConnection.Open();

            var sql = "CREATE OR ALTER TABLE Employee (id INTEGER PRIMARY KEY, name VARCHAR(100), dob DateTime)";

            SQLiteCommand command = new SQLiteCommand(sql, dbConnection);

            command.ExecuteNonQuery();
        }

        [TestInitialize]
        public void FactoryGirlExtInitilize()
        {
            FactoryGirl.Define(() => new Employee {Name = "Stephan", Dob = DateTime.Now});
        }

        [TestCleanup]
        public void Cleanup()
        {
            FactoryGirl.ClearFactoryDefinitions();

        }

        [TestMethod]
        public void Build_Should_Return_An_Entity_Only()
        {

            var employee = FactoryGirl.Build<Employee>();

            Assert.IsNotNull(employee);
        }

        [TestMethod]
        public void Create_Should_Return_An_Entity_On_Memory_And_DB()
        {
            Employee employee = FactoryGirl.Build<Employee>();

            employee = FactoryGirl.CreateGeneric(ConnectionString , employee);

            Assert.IsNotNull(employee);
            Assert.IsTrue(employee.id != 0);
        }
    }

    public class Employee
    {
        public int id { get; set; }
        public DateTime Dob { get; set; }
        public string Name { get; set; }
    }
}