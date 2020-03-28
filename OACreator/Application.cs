using System;
using System.Linq;
using System.Data;
using Newtonsoft.Json;

namespace OACreator
{
    public class Application
    {
        public void Generate(int DenId)
        {

            Logger logger = new Logger(Extensions.LogFilePath);
            try
            {
                DataModule dataModule = new DataModule(logger);
                try
                {
                    string idKsiegowy = dataModule.GetIdKsięgowy(DenId);
                    try
                    {
                        if (!dataModule.IsNiewypełniony(DenId))
                            throw new Exception("Opis analityczny jest już wypełniony!");

                        Tuple<int, string> symbol = dataModule.GetSymbolSchematu(DenId);
                        Tuple<DataTable, DataTable> daneOA = dataModule.GetDaneOA(DenId, symbol);
                        Tuple<DataTable, DataTable> daneDekret = dataModule.GetDaneDekret(DenId);

                        if (Extensions.DataTablesDifferencesExists(daneOA.Item1, daneDekret.Item1))
                            throw new Exception("Istnieją różnice w danych!");

                        dataModule.GenerateOA(DenId, daneDekret.Item2, daneOA.Item2, idKsiegowy);
                    }
                    catch(Exception e)
                    {
                        throw new Exception("Wystąpił błąd dla dekretu o Id Księgowym = [" + idKsiegowy + "]: " + e.Message);
                    }
                }
                finally
                {
                    dataModule.Dispose();
                }
            }
            catch (Exception e)
            {
                logger.Write(e.Message);
                throw new Exception(e.Message);
            }
        }

        public DataTable GetDeNIdWithNullStatusList()
        {
            Logger logger = new Logger(Extensions.LogFilePath);
            try
            {
                DataModule dataModule = new DataModule(logger);
                try
                {
                    return dataModule.GetDeNIdWithNullStatusList();
                }
                finally
                {
                    dataModule.Dispose();
                }
            }
            catch (Exception e)
            {
                logger.Write(e.Message);
                throw new Exception(e.Message);
            }

        }
    }
}
