using System;
using System.Collections.Generic;
using System.Data;

namespace OACreator
{
    public class DataModule
    {
        private readonly Logger logger;
        private SqlHandler sql;
        public DataModule(Logger _logger)
        {
            sql = new SqlHandler(Extensions.SQLServerName, Extensions.SQLDatabase, Extensions.SQLUsername, Extensions.SQLPassword, Boolean.Parse(Extensions.SQLNT));
            sql.Connect();

            sql.logger = _logger;
            logger = _logger;
        }

        public void Dispose()
        {
            sql.Disconnect();
        }

        public Tuple<int, string> GetSymbolSchematu(int DeNId)
        {
            try
            {
                return sql.GetSymbolSchematu(DeNId);
            }
            catch (Exception e)
            {
                if (e.Data["Severity"] != null && (byte)e.Data["Severity"] == 14)
                    sql.OAStatusAdd(DeNId, StatusOA.DoUzupełnieniaManualnie);

                throw e;
            }
        }

        public string GetIdKsięgowy(int DeNId)
        {
            return sql.GetIdKsięgowy(DeNId);
        }

        public Boolean IsNiewypełniony(int DeNId)
        {
            return sql.IsNiewypełniony(DeNId);
        }

        public Tuple<DataTable, DataTable> GetDaneOA(int DenId, Tuple<int, string> symbol)
        {
            try
            {
                return sql.GetDaneOA(DenId, symbol);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Could not find stored procedure"))
                    sql.OAStatusAdd(DenId, StatusOA.BrakFunkcji);

                throw e;
            }
        }

        public Tuple<DataTable, DataTable> GetDaneDekret(int DenId)
        {
            return sql.GetDaneDekret(DenId);
        }

        public DataTable GetDeNIdWithNullStatusList()
        {
            return sql.GetDeNIdWithNullStatusList();
        }

        public void GenerateOA(int DeNId, DataTable datDek, DataTable datOA, string idKsiegowy)
        {
            if (datDek.Rows.Count == 0 && datOA.Rows.Count == 0)
            {
                logger.Write("Pusty OA dla dekretu o DeNId = [" + DeNId + "], Id ksiegowy = [" + idKsiegowy + "]");
                sql.OAStatusAdd(DeNId, StatusOA.PustyAutomatycznie);
                return;
            }

            List<OAData> oaDataList = new List<OAData>();
            double sumProcent = 0.0000;
            double procent;

            int idxDek = 0, idxOA = 0;
            if (!(idxDek < datDek.Rows.Count && idxOA < datOA.Rows.Count))
                throw new Exception("Błąd danych!");

            double DekretKwota = Double.Parse(datDek.Rows[idxDek]["Kwota"].ToString());
            double OAKwota = Double.Parse(datOA.Rows[idxOA]["Kwota"].ToString());
            double PozostalaKwota = 0.00;
            int Status = 0;

            while (true)
            {
                if (Int32.Parse(datDek.Rows[idxDek]["AccId"].ToString()) != Int32.Parse(datOA.Rows[idxOA]["Acc4Id"].ToString()) ||
                    Int32.Parse(datDek.Rows[idxDek]["Strona"].ToString()) != Int32.Parse(datOA.Rows[idxOA]["Strona"].ToString()) ||
                    Int32.Parse(datDek.Rows[idxDek]["Znak"].ToString()) != Int32.Parse(datOA.Rows[idxOA]["Znak"].ToString()))
                    throw new Exception("Błąd danych!");

                switch (Status)
                {
                    case -1:
                        if (DekretKwota == PozostalaKwota)
                        {
                            OAKwota = PozostalaKwota;
                            PozostalaKwota = 0.00;
                            Status = 0;
                        }
                        else
                            if (DekretKwota > PozostalaKwota)
                        {
                            OAKwota = PozostalaKwota;
                            PozostalaKwota = Math.Round(DekretKwota - PozostalaKwota, 2);
                            Status = 1;
                        }
                        else
                        {
                            PozostalaKwota = Math.Round(PozostalaKwota - DekretKwota, 2);
                            OAKwota = DekretKwota;
                        }
                        break;
                    case 0:
                        if (DekretKwota == OAKwota)
                        {
                        }
                        else
                            if (DekretKwota > OAKwota)
                        {
                            PozostalaKwota = Math.Round(DekretKwota - OAKwota, 2);
                            Status = 1;
                        }
                        else
                        {
                            PozostalaKwota = Math.Round(OAKwota - DekretKwota, 2);
                            OAKwota = DekretKwota;
                            Status = -1;
                        }
                        break;
                    case 1:
                        if (OAKwota == PozostalaKwota)
                        {
                            PozostalaKwota = 0.00;
                            Status = 0;
                        }
                        else
                            if (OAKwota > PozostalaKwota)
                        {
                            OAKwota = PozostalaKwota;
                            PozostalaKwota = Math.Round(Double.Parse(datOA.Rows[idxOA]["Kwota"].ToString()) - PozostalaKwota, 2);
                            Status = -1;
                        }
                        else
                        {
                            PozostalaKwota = Math.Round(PozostalaKwota - OAKwota, 2);
                        }
                        break;
                }

                if (Status == 1)
                {
                    procent = Math.Round(100 * OAKwota / DekretKwota, 2, MidpointRounding.AwayFromZero);
                    sumProcent += procent;
                }
                else
                {
                    procent = 100 - sumProcent;
                    sumProcent = 0.00;
                }

                /*
                sql.CreateOA(DeNId,
                        Guid.NewGuid().ToString(),
                        Int32.Parse(datDek.Rows[idxDek]["LP"].ToString()),
                        Int32.Parse(datOA.Rows[idxOA]["Acc4Id"].ToString()),
                        Int32.Parse(datDek.Rows[idxDek]["Strona"].ToString()),
                        Int32.Parse(datOA.Rows[idxOA]["Acc5Id"].ToString()),
                        DekretKwota * Int32.Parse(datDek.Rows[idxDek]["Znak"].ToString()),
                        OAKwota * Int32.Parse(datDek.Rows[idxDek]["Znak"].ToString()),
                        procent);
                */
                oaDataList.Add(new OAData(DeNId,
                        Guid.NewGuid().ToString(),
                        Int32.Parse(datDek.Rows[idxDek]["LP"].ToString()),
                        Int32.Parse(datOA.Rows[idxOA]["Acc4Id"].ToString()),
                        Int32.Parse(datDek.Rows[idxDek]["Strona"].ToString()),
                        Int32.Parse(datOA.Rows[idxOA]["Acc5Id"].ToString()),
                        DekretKwota * Int32.Parse(datDek.Rows[idxDek]["Znak"].ToString()),
                        OAKwota * Int32.Parse(datDek.Rows[idxDek]["Znak"].ToString()),
                        procent));

                if (Status != 1)
                {
                    idxDek += 1;
                    if (idxDek >= datDek.Rows.Count)
                        break;
                    DekretKwota = double.Parse(datDek.Rows[idxDek]["Kwota"].ToString());
                }

                if (Status != -1)
                {
                    idxOA += 1;
                    if (idxOA >= datOA.Rows.Count)
                        break;
                    OAKwota = double.Parse(datOA.Rows[idxOA]["Kwota"].ToString());
                }
            }
            if (PozostalaKwota != 0.00)
                throw new Exception("Błąd danych!");

            foreach(OAData oaData in oaDataList)
            {
                sql.CreateOA(oaData.DeNId, oaData.GID, oaData.LP, oaData.AccIdLP, oaData.Strona, oaData.AccIdWym, oaData.Kwota, oaData.KwotaOA, oaData.Procent);
            }

            logger.Write("Pomyślnie dodano pozycje do dekretu o DeNId = [" + DeNId + "], Id ksiegowy = [" + idKsiegowy + "]");
            sql.OAStatusAdd(DeNId, StatusOA.WypełnionyAutomatycznie);
        }


        public void GenerateOA_OLD(int DeNId, DataTable datDek, DataTable datOA)
        {
            double sumKwota = 0.00, sumProcent = 0.0000;
            double kwotaAbsolute, kwotaDek, kwotaOA, procent;
            double[] deficency = new double[2] { 0.00, 0.00 };
            int idxDek = 0, idxOA = 0;
            while (idxDek < datDek.Rows.Count && idxOA < datOA.Rows.Count)
            {
                if (Int32.Parse(datDek.Rows[idxDek]["AccId"].ToString()) != Int32.Parse(datOA.Rows[idxOA]["Acc4Id"].ToString()) ||
                    Int32.Parse(datDek.Rows[idxDek]["Strona"].ToString()) != Int32.Parse(datOA.Rows[idxOA]["Strona"].ToString()) ||
                    Int32.Parse(datDek.Rows[idxDek]["Znak"].ToString()) != Int32.Parse(datOA.Rows[idxOA]["Znak"].ToString()))
                    throw new Exception("Błąd danych!");

                kwotaAbsolute = double.Parse(datDek.Rows[idxDek]["Kwota"].ToString());
                kwotaDek = deficency[0] == 0.00 ? double.Parse(datDek.Rows[idxDek]["Kwota"].ToString()) : deficency[0];
                kwotaOA = deficency[1] == 0.00 ? double.Parse(datOA.Rows[idxOA]["Kwota"].ToString()) : deficency[1];
                procent = Math.Round(100.00 * kwotaOA / kwotaAbsolute, 2, MidpointRounding.AwayFromZero);

                sumKwota += double.Parse(datOA.Rows[idxOA]["Kwota"].ToString());

                sql.CreateOA(DeNId,
                        Guid.NewGuid().ToString(),
                        Int32.Parse(datDek.Rows[idxDek]["LP"].ToString()),
                        Int32.Parse(datOA.Rows[idxOA]["Acc4Id"].ToString()),
                        Int32.Parse(datDek.Rows[idxDek]["Strona"].ToString()),
                        Int32.Parse(datOA.Rows[idxOA]["Acc5Id"].ToString()),
                        kwotaAbsolute * Int32.Parse(datDek.Rows[idxDek]["Znak"].ToString()),
                        kwotaOA * Int32.Parse(datDek.Rows[idxDek]["Znak"].ToString()),
                        sumKwota == kwotaAbsolute ? Math.Round(100.00 - sumProcent, 2, MidpointRounding.AwayFromZero) : procent);

                if (kwotaOA == kwotaDek)
                {
                    deficency[0] = 0.00;
                    deficency[1] = 0.00;
                    idxDek += 1;
                    idxOA += 1;
                    sumKwota = 0.00;
                    sumProcent = 0.0000;
                }
                else if (kwotaOA < kwotaDek)
                {
                    deficency[0] = Math.Round(kwotaDek - kwotaOA, 2);
                    deficency[1] = 0.00;
                    idxOA += 1;
                    sumProcent += procent;

                }
                else if (kwotaOA > kwotaDek)
                {
                    deficency[0] = 0.00;
                    deficency[1] = Math.Round(kwotaOA - kwotaDek, 2);
                    idxDek += 1;
                    sumProcent += procent;
                }
            }

            logger.Write("Pomyślnie dodano pozycje do dekretu o DeNId = [" + DeNId + "]");
        }
    }
}
