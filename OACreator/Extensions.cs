using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Configuration;

namespace OACreator
{
    public static class Extensions
    {
        public static string SQLServerName => GetAppSetting("SQLServerName");
        public static string SQLDatabase => GetAppSetting("SQLDatabase");
        public static string SQLUsername => GetAppSetting("SQLUsername");
        public static string SQLPassword => GetAppSetting("SQLPassword");
        public static string SQLNT => GetAppSetting("SQLNT");
        public static string LogFilePath => GetAppSetting("LogFilePath");
        public static string Debug => GetAppSetting("Debug");

        private static string GetAppSetting(string key)
        {
            try
            {
                var asmPath = Assembly.GetExecutingAssembly().Location;
                var config = ConfigurationManager.OpenExeConfiguration(asmPath);
                var setting = config.AppSettings.Settings[key];
                return setting.Value;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Error reading configuration setting", e);
            }
        }

        public static bool DataTablesDifferencesExists(DataTable dt1, DataTable dt2)
        {
            if (dt1.Rows.Count != dt2.Rows.Count)
                return true;

            var differences = dt1.AsEnumerable().Except(dt2.AsEnumerable(), DataRowComparer.Default);

            return differences.Any() ? true : false;
        }
    }

    enum StatusOA
    {
        WypełnionyAutomatycznie = 1,
        PustyAutomatycznie = 2,
        UzupełnionyManualnie = 3,
        DoUzupełnieniaManualnie = 4,
        BrakFunkcji = 5
    }
}
