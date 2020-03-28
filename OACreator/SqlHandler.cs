using System;
using System.Data;
using System.Data.SqlClient;


namespace OACreator
{
    class SqlHandler
    {
        private SqlConnection cnn;
        public Logger logger;

        private readonly Boolean debug = Boolean.Parse(Extensions.Debug);

        public SqlHandler(string servername, string database, string username, string password, Boolean NT)
        {
            if (debug)
                return;

            if (!NT)
                cnn = new SqlConnection("Data Source = " + servername + "; Initial Catalog = " + database + "; User ID = " + username + "; Password = " + password);
            else
                cnn = new SqlConnection("Data Source = " + servername + "; Initial Catalog = " + database + "; Integrated Security=true;");
        }

        public Boolean Connect()
        {
            if (debug)
                return true;

            try
            {
                if ((int)cnn.State == 0)
                {
                    cnn.Open();
                    return true;
                }
                return false;
            }
            catch (SqlException e)
            {
                throw new Exception("Nie można nawiązać połączenia z SQL: nie można odnaleźć serwera lub jest on niedostępny." + e.Message);
            }

        }

        public void Disconnect()
        {
            if (debug)
                return;

            try
            {
                if ((int)cnn.State == 1)
                {
                    cnn.Close();
                }
            }
            catch (SqlException)
            {
                throw new Exception("Wystąpił błąd podczas zamykania połączenia z serwerem SQL");
            }
        }

        public Tuple<int, string> GetSymbolSchematu(int DeNId)
        {
            if (debug)
                return Tuple.Create(21, "PRO_ZAKUP");

            try
            {
                using (SqlCommand cmd = new SqlCommand("CDN.PROGetSymbolSchematu", cnn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@DeNId", SqlDbType.Int).Value = DeNId;
                    cmd.Parameters.Add("@SchematTyp", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@SchematSymbol", SqlDbType.VarChar, 20).Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();

                    if (cmd.Parameters["@SchematTyp"].Value == DBNull.Value || cmd.Parameters["@SchematSymbol"].Value == DBNull.Value)
                        throw new Exception("Nie znaleziono schematu");

                    return Tuple.Create(Int32.Parse(cmd.Parameters["@SchematTyp"].Value.ToString()), cmd.Parameters["@SchematSymbol"].Value.ToString());
                }
            }
            catch (SqlException e)
            {
                Exception ex = new Exception("Bład podczas szukania Symbolu Schematu: " + e.Message);
                ex.Data["Severity"] = e.Class;

                throw ex;
            }
            catch (Exception e)
            {
                throw new Exception("Bład podczas szukania Symbolu Schematu: " + e.Message);
            }
        }

        public string GetIdKsięgowy(int DeNId)
        {
            if (debug)
                return "TestIdKsiegowy";

            try
            {
                using (SqlCommand cmd = new SqlCommand("CDN.PROGetIdKsiegowy", cnn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@DeNId", SqlDbType.Int).Value = DeNId;
                    cmd.Parameters.Add("@IdKsiegowy", SqlDbType.VarChar, 50).Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();

                    return cmd.Parameters["@IdKsiegowy"].Value.ToString();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Bład podczas szukania Id Księgowego dla DeNId = [" + DeNId + "]: " + e.Message);
            }
        }

        public bool IsNiewypełniony(int DeNId)
        {
            if (debug)
                return true;

            try
            {
                using (SqlCommand cmd = new SqlCommand("CDN.PROIsOANiewypelniony", cnn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@DeNId", SqlDbType.Int).Value = DeNId;
                    cmd.Parameters.Add("@Empty", SqlDbType.Bit).Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();

                    return (bool)cmd.Parameters["@Empty"].Value;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Bład podczas sprawdzania czy OA jest wypełniony: " + e.Message);
            }
        }

        public void OAStatusUpdate(int DeNId, StatusOA status)
        {
            if (debug)
                return;

            try
            {
                using (SqlCommand cmd = new SqlCommand("CDN.PRODekretyNagExtUpdate", cnn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@DeNId", SqlDbType.Int).Value = DeNId;
                    cmd.Parameters.Add("@StatusOA", SqlDbType.TinyInt).Value = status;

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Bład podczas aktualizacji statusów OA: " + e.Message);
            }
        }

        public void OAStatusAdd(int DeNId, StatusOA status)
        {
            if (debug)
                return;

            try
            {
                using (SqlCommand cmd = new SqlCommand("CDN.PRODekretyNagExtAdd", cnn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@DeNId", SqlDbType.Int).Value = DeNId;
                    cmd.Parameters.Add("@StatusOA", SqlDbType.TinyInt).Value = status;

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Bład podczas uzupełniania statusu OA: " + e.Message);
            }
        }


        public Tuple<DataTable, DataTable> GetDaneOA(int DeNId, Tuple<int, string> symbol)
        {
            if (debug)
                return Tuple.Create(GetRS1Mock(), GetRS2OAMock());

            try
            {
                string cmdStr;
                switch (symbol.Item1)
                {
                    case 21:
                        cmdStr = "CDN.PRODaneOA_" + symbol.Item2;
                        break;
                    case 22:
                        cmdStr = "CDN.PRODaneOA_SO_" + symbol.Item2;
                        break;
                    default:
                        throw new Exception("Nieznany typ schematu [" + symbol.Item1 + "]");

                }

                using (SqlCommand cmd = new SqlCommand(cmdStr, cnn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@DeNId", SqlDbType.Int).Value = DeNId;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        DataTable result1 = new DataTable();
                        result1.Load(reader);

                        DataTable result2 = new DataTable();
                        result2.Load(reader);

                        return Tuple.Create(result1, result2);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Bład podczas pobierania danych OA: " + e.Message);
            }
        }

        public Tuple<DataTable, DataTable> GetDaneDekret(int DeNId)
        {
            if (debug)
                return Tuple.Create(GetRS1Mock(), GetRS2DekMock());

            try
            {
                using (SqlCommand cmd = new SqlCommand("CDN.PRODaneDekret45", cnn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@DeNId", SqlDbType.Int).Value = DeNId;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        DataTable result1 = new DataTable();
                        result1.Load(reader);

                        DataTable result2 = new DataTable();
                        result2.Load(reader);

                        return Tuple.Create(result1, result2);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Bład podczas pobierania danych dekretu: " + e.Message);
            }
        }

        public DataTable GetDeNIdWithNullStatusList()
        {
            if (debug)
                return GetDeNIdWithNullStatusListMock();

            try
            {
                using (SqlCommand cmd = new SqlCommand("CDN.PROGetNullStatusDekretList", cnn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        DataTable result = new DataTable();
                        result.Load(reader);

                        return result;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Bład podczas pobierania listy DenId: " + e.Message);
            }
        }

        public void CreateOA(int DeNId, string GID, int LP, int AccIdLP, int Strona, int AccIdWym, double Kwota, double KwotaOA, double Procent)
        {
            if (debug)
            {
                logger.Write(DeNId + " : " + GID + " : " + LP + " : " + AccIdLP + " : " + Strona + " : " + AccIdWym + " : " + Kwota + " : " + KwotaOA + " : " + Procent);
                return;
            }

            try
            {
                using (SqlCommand cmd = new SqlCommand("CDN.PROAddPozycjaOA", cnn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@DeNId", SqlDbType.Int).Value = DeNId;
                    cmd.Parameters.Add("@GID", SqlDbType.NVarChar).Value = GID;
                    cmd.Parameters.Add("@LP", SqlDbType.Int).Value = LP;
                    cmd.Parameters.Add("@AccIdLP", SqlDbType.Int).Value = AccIdLP;
                    cmd.Parameters.Add("@Strona", SqlDbType.TinyInt).Value = Strona;
                    cmd.Parameters.Add("@AccIdWym", SqlDbType.Int).Value = AccIdWym;
                    cmd.Parameters.Add("@Kwota", SqlDbType.Decimal).Value = Kwota;
                    cmd.Parameters.Add("@KwotaOA", SqlDbType.Decimal).Value = KwotaOA;
                    cmd.Parameters.Add("@Procent", SqlDbType.Decimal).Value = Procent;

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Bład podczas podczas wstawiania pozycji OA do dekretu: " + e.Message);
            }
        }

        private DataTable GetDeNIdWithNullStatusListMock()
        {
            int i = 0;
            DataTable dt = new DataTable();
            dt.Columns.Add("DNE_DeNId", i.GetType());
            dt.Rows.Add(new Object[] { 11647 });

            return dt;
        }

        private DataTable GetRS1Mock()
        {
            int i = 0;
            double d = 0.00;
            DataTable dt = new DataTable();
            dt.Columns.Add("Strona", i.GetType());
            dt.Columns.Add("AccId", i.GetType());
            dt.Columns.Add("AccNumer");
            dt.Columns.Add("Kwota", d.GetType());

            dt.Rows.Add(new Object[] { 1, 895, "402-10-03-07", 23.53 });
            dt.Rows.Add(new Object[] { 1, 897, "402-10-03-09", 100.00 });
            dt.Rows.Add(new Object[] { 1, 1030, "528-21S", 18.73 });
            dt.Rows.Add(new Object[] { 1, 1033, "528-23P", 24.80 });
            dt.Rows.Add(new Object[] { 1, 1037, "528-26P", 50.00 });
            dt.Rows.Add(new Object[] { 1, 1038, "528-26S", 30.00 });

            return dt;
        }

        private DataTable GetRS2OAMock()
        {
            int i = 0;
            double d = 0.00;
            DataTable dt = new DataTable();
            dt.Columns.Add("Strona", i.GetType());
            dt.Columns.Add("Znak", i.GetType());
            dt.Columns.Add("Acc4Id", i.GetType());
            dt.Columns.Add("Acc5Id", i.GetType());
            dt.Columns.Add("Kwota", d.GetType());

            dt.Rows.Add(new Object[] { 1, 1, 895, 1030, 18.73 });
            dt.Rows.Add(new Object[] { 1, 1, 895, 1033, 4.80 });
            dt.Rows.Add(new Object[] { 1, 1, 897, 1033, 20.00 });
            dt.Rows.Add(new Object[] { 1, 1, 897, 1037, 50.00 });
            dt.Rows.Add(new Object[] { 1, 1, 897, 1038, 30.00 });

            return dt;
        }

        private DataTable GetRS2DekMock()
        {
            int i = 0;
            double d = 0.00;
            DataTable dt = new DataTable();
            dt.Columns.Add("LP", i.GetType());
            dt.Columns.Add("Strona", i.GetType());
            dt.Columns.Add("Znak", i.GetType());
            dt.Columns.Add("AccId", i.GetType());
            dt.Columns.Add("AccNumer");
            dt.Columns.Add("Kwota", d.GetType());

            dt.Rows.Add(new Object[] { 3, 1, 1, 895, "402-10-03-07", 23.53 });
            dt.Rows.Add(new Object[] { 2, 1, 1, 897, "402-10-03-09", 100.00 });

            return dt;
        }
    }
}
