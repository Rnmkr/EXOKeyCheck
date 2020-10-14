using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;

namespace EXOKeyCheck
{
    /// <summary>
    //<Key>
    //  <ProductKey>NFX7C-HRFRM-J6VJK-FDHG3-VXMMP</ProductKey>
    //  <ProductKeyID>3273088776052</ProductKeyID>
    //  <ProductKeyState>2</ProductKeyState>
    //  <ProductKeyPartNumber>KU9-00001</ProductKeyPartNumber>
    //</Key>
    //var productKey = args[1].ToUpper();
    //var serialNumber = args[2].ToUpper();
    //var productKeyID = args[3];
    //var productKeyPartNumber = args[4].ToUpper();
    /// </summary>

    //TODO: purgar, pasar a ProductKey y usar el nuevo branch
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                switch (args[0])
                {
                    case "/?":
                        ShowHelp();
                        break;
                    case "/sk":
                        if (ArgsValidation(args))
                        {
                            SaveKeyToDatabase(args[1], args[2], args[3], args[4]);
                        }
                        break;
                    case "/hk":
                        if (ArgsValidation(args))
                        {
                            SetBoundState(args[1], args[2]);
                        }
                        break;
                    default:
                        Environment.Exit(-1);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(-1);
            }
        }

        private static bool ArgsValidation(string[] args)
        {
            if (args.Length < 4) return false;

            foreach (var arg in args)
            {
                if (string.IsNullOrWhiteSpace(arg)) return false;
            }

            var productKey = args[1].ToUpper();
            var serialNumber = args[2].ToUpper();
            var productKeyID = args[3];
            var productKeyPartNumber = args[4].ToUpper();

            Regex regexForProductKey = new Regex(@"^([A-Za-z0-9]{5}-){4}[A-Za-z0-9]{5}$");
            if (!regexForProductKey.IsMatch(productKey))
            {
                throw new ArgumentException("Error: ProductKey inválido [1]");
            }

            Regex regexForSerialNumber = new Regex(@"^\d{7}[a-gA-G]\d{5}$");
            if (!regexForSerialNumber.IsMatch(serialNumber))
            {
                throw new ArgumentException("Error: SerialNumber inválido [2]");
            }

            Regex regexForProductKeyID = new Regex(@"^[0-9]+$"); ///
            if (!regexForProductKeyID.IsMatch(productKeyID))
            {
                throw new ArgumentException("Error: ProductKeyID inválido [3]");
            }

            Regex regexForProductKeyPartNumber = new Regex(@"^([A-Za-z0-9]{3}-)[0-9]{5}$"); ///
            if (!regexForProductKeyPartNumber.IsMatch(productKeyPartNumber))
            {
                throw new ArgumentException("Error: ProductKeyPartNumber inválido [4]");
            }

            return true;
        }

        private static void SetBoundState(string v1, string v2)
        {
            throw new NotImplementedException();
        }

        private static void SaveKeyToDatabase(string productKey, string serialNumber, string productKeyID, string productKeyPartNumber)
        {
            throw new NotImplementedException();
        }

        private static void SelectQuery(string option, string key, string serialNumber)
        {
            Regex regexKey = new Regex(@"^([A-Za-z0-9]{5}-){4}[A-Za-z0-9]{5}$");
            if (!regexKey.IsMatch(key))
            {
                throw new ArgumentException("Formato inválido de OA Key");
            }

            Regex regexSerialNumber = new Regex(@"^\d{7}[a-gA-G]\d{5}$");
            if (!regexSerialNumber.IsMatch(serialNumber))
            {
                throw new ArgumentException("Formato inválido de Serial Number");
            }

            switch (option)
            {
                case "/sk":
                    TrySaveKey(key, serialNumber);
                    break;
                case "/hk":
                    TryHashKey(key, serialNumber);
                    break;
                default:
                    throw new ArgumentException("Opcion inválida");
            }
        }

        private static void TrySaveKey(string key, string serialNumber)
        {
            ExecuteSaveQuery(key, serialNumber);
        }

        private static void TryHashKey(string key, string serialNumber)
        {
            var report = ExecuteSelectFromKeyQuery(key, "GetRecordFromKey");

            if (string.IsNullOrWhiteSpace(report.OAKey))
            {
                //Console.WriteLine("No se encontró la clave en la base de datos.");
                Environment.Exit(-6);
            }

            if (report.State == "Bound")
            {
                //Console.WriteLine("La clave ya está hasheada con el serial: " + report.SerialNumber);
                Environment.Exit(-9);
            }

            if (report.SerialNumber == serialNumber)
            {
                ExecuteHashQuery(report.ReportID);
            }
            else
            {
                //Console.WriteLine("La clave no coincide con el numero de serie en la base de datos.");
                Environment.Exit(-4);
            }


        }

        private static void ExecuteHashQuery(int reportID)
        {
            var connectionString = @"data source=BUBBA;initial catalog=EXOOAKeys2020;persist security info=True;user id=BUBBASQL;password=12345678;MultipleActiveResultSets=True;";
            //var connectionString = @"data source=VM-FORREST;initial catalog=EXOOAKeys2020;persist security info=True;user id=BUBBASQL;password=12345678;MultipleActiveResultSets=True";
            //var connectionString = @"data source=DESKTOP;initial catalog=EXOOAKeys2020; integrated security=True; MultipleActiveResultSets=True";
            var sqlConnection = new SqlConnection(connectionString);

            using (sqlConnection)
            {
                try
                {
                    sqlConnection.Open();
                    var sqlCommand = new SqlCommand("HashOAKey", sqlConnection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlCommand.Parameters.AddWithValue("@ReportID", reportID);
                    sqlCommand.Parameters.Add("@Output", SqlDbType.Char, 500);
                    sqlCommand.Parameters["@Output"].Direction = ParameterDirection.Output;
                    sqlCommand.ExecuteNonQuery();

                    var sqloutput = Convert.ToInt32(sqlCommand.Parameters["@Output"].Value);
                    Environment.Exit(sqloutput);
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("network-related"))
                    {
                        //Console.WriteLine("Error en la red intentando guardar registro." + Environment.NewLine + e.Message);
                        Environment.Exit(-8);
                    }

                    throw new Exception("Error intentando guardar registro." + Environment.NewLine + e.Message);
                }
                finally
                {
                    sqlConnection.Close();
                }
            }

        }

        private static OAKeyReport ExecuteSelectFromKeyQuery(string key, string storedprocedure)
        {
            var connectionString = @"data source=BUBBA;initial catalog=EXOOAKeys2020;persist security info=True;user id=BUBBASQL;password=12345678;MultipleActiveResultSets=True;";
            //var connectionString = @"data source=VM-FORREST;initial catalog=EXOOAKeys2020;persist security info=True;user id=BUBBASQL;password=12345678;MultipleActiveResultSets=True";
            //var connectionString = @"data source=DESKTOP;initial catalog=EXOOAKeys2020; integrated security=True; MultipleActiveResultSets=True";
            var sqlConnection = new SqlConnection(connectionString);
            using (sqlConnection)
            {
                IDataReader reader = null;
                var report = new OAKeyReport();
                try
                {
                    sqlConnection.Open();
                    var sqlCommand = new SqlCommand(storedprocedure, sqlConnection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlCommand.Parameters.AddWithValue("@Keyword", key);
                    reader = sqlCommand.ExecuteReader();

                    while (reader.Read())
                    {
                        var reportEntity = new OAKeyReport()
                        {
                            ReportID = (int)reader["ReportID"],
                            OAKey = (string)reader["OAKey"],
                            SerialNumber = (string)reader["SerialNumber"],
                            State = (string)reader["State"],
                            DateConsumed = (DateTime)reader["DateConsumed"],
                        };

                        if ((reader["DateBound"]) != DBNull.Value)
                            reportEntity.DateBound = (DateTime)reader["DateBound"];

                        report = reportEntity;
                    }
                }
                catch (Exception e)
                {
                    //Console.WriteLine("Error intentando obtener registro." + e.Message);
                }
                finally
                {
                    if (reader != null) reader.Close();
                    sqlConnection.Close();
                }

                return report;
            }

        }

        private static void ExecuteSaveQuery(string key, string serialNumber)
        {
            var connectionString = @"data source=BUBBA;initial catalog=EXOOAKeys2020;persist security info=True;user id=BUBBASQL;password=12345678;MultipleActiveResultSets=True;";
            //var connectionString = @"data source=VM-FORREST;initial catalog=EXOOAKeys2020;persist security info=True;user id=BUBBASQL;password=12345678;MultipleActiveResultSets=True";
            //var connectionString = @"data source=DESKTOP;initial catalog=EXOOAKeys2020; integrated security=True; MultipleActiveResultSets=True";
            var sqlConnection = new SqlConnection(connectionString);
            using (sqlConnection)
            {
                try
                {
                    sqlConnection.Open();
                    var sqlCommand = new SqlCommand("SaveOAKey", sqlConnection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    //sqlCommand.Parameters.AddWithValue("@ProductKeyCode", productkeycode);
                    sqlCommand.Parameters.AddWithValue("@OAKey", key);
                    sqlCommand.Parameters.AddWithValue("@SerialNumber", serialNumber);
                    sqlCommand.Parameters.Add("@Output", SqlDbType.Char, 500);
                    sqlCommand.Parameters["@Output"].Direction = ParameterDirection.Output;
                    sqlCommand.ExecuteNonQuery();

                    var sqloutput = Convert.ToInt32(sqlCommand.Parameters["@Output"].Value);
                    if (sqloutput != 0)
                    {
                        var report = ExecuteSelectFromKeyQuery(key, "GetRecordFromKey");
                        if (report.State == "Consumed")
                        {
                            File.Create(AppDomain.CurrentDomain.BaseDirectory + "Estado.Consumed");
                        }
                        if (report.State == "Bound")
                        {
                            File.Create(AppDomain.CurrentDomain.BaseDirectory + "Estado.Bound");
                        }
                    }
                    Environment.Exit(sqloutput);
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("network-related"))
                    {
                        //Console.WriteLine("Error en la red intentando guardar registro." + Environment.NewLine + e.Message);
                        Environment.Exit(-8);
                    }
                    //Console.WriteLine("Error intentando guardar registro." + Environment.NewLine + e.Message);
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Opciones de parámetros:");
            Console.WriteLine("");
            Console.WriteLine("      /sk { ProductKey SerialNumber [ProductKeyID ProductKeyPartNumber] }");
            Console.WriteLine("      Crea un nuevo registro en la base de datos con estado 'Consumed'");
            Console.WriteLine("");
            Console.WriteLine("      /hk { ProductKey SerialNumber }");
            Console.WriteLine("      Cambia el estado 'Consumed' de un registro a 'Bound'");
            Console.WriteLine("");
            Console.WriteLine(" Códigos de salida generales:");
            Console.WriteLine("");
            Console.WriteLine("      0  = Operación realizada sin errores.");
            Console.WriteLine("     -1  = Error de ejecución de la aplicación.");
            Console.WriteLine("     -8  = Error de conexión con la base de datos.");
            Console.WriteLine("");
            Console.WriteLine(" Códigos de salida para el parámetro '/sk':");
            Console.WriteLine("");
            Console.WriteLine("     -2  = ProductKey ya existe en base de datos.");
            Console.WriteLine("     -3  = SerialNumber ya existe en base de datos.");
            Console.WriteLine("     -5  = ProductKey y SerialNumber ya existen en base de datos.");
            Console.WriteLine("");
            Console.WriteLine(" Códigos de salida para el párametro '/hk':");
            Console.WriteLine("");
            Console.WriteLine("     -4  = ProductKey y SerialNumber existen en la base de datos, pero no estan asociados.");
            Console.WriteLine("     -6  = No se encontró ProductKey en la base de datos.");
            Console.WriteLine("     -7  = No se encontró SerialNumber en la base de datos.");
            Console.WriteLine("     -9  = El el estado del registro en la base de datos es distinto de 'Consumed'.");
            Console.WriteLine("     -13 = No se encontró ProductKey ni SerialNumber en la base de datos.");
            Console.WriteLine("");

            Environment.Exit(0);
        }
    }
}
