using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;

//connectionString = @"data source=VM-FORREST;initial catalog=EXOOAKeys2020;persist security info=True;user id=BUBBASQL;password=12345678;MultipleActiveResultSets=True";
//connectionString = @"data source=DESKTOP;initial catalog=EXOOAKeys2020; integrated security=True; MultipleActiveResultSets=True";

namespace EXOKeyCheck
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                args[1] = args[1].ToUpper();
                args[2] = args[2].ToUpper();
                args[4] = args[5].ToUpper();

                switch (args[0])
                {
                    case "/?":
                        ShowHelp();
                        break;
                    case "/sk":
                        SaveKeyToDatabase(args);
                        break;
                    case "/hk":
                        SetBoundState(args);
                        break;
                    default:
                        Console.WriteLine("Error: Opción inválida." + Environment.NewLine + "Utilice '/?' para ver la ayuda.");
                        Environment.Exit(-1);
                        break;
                }
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("Error: No se permiten parámetros núlos o cadena de espacios vacías.");
                Environment.Exit(-1);
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("Error: la cantidad de parámetros es inválida." + Environment.NewLine + "Utilice '/?' para ver la ayuda.");
                Environment.Exit(-1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(-1);
            }
        }

        private static void SaveKeyToDatabase(string[] args)
        {
            if (IsArgsValid(args))
            {
                ExecuteSaveQuery(args);
            }
        }

        private static void SetBoundState(string[] args)
        {
            if (IsArgsValid(args))
                throw new NotImplementedException();
        }

        private static void ExecuteSaveQuery(string[] args)
        {
            var serialNumber = args[1];
            var productKey = args[2];
            var productKeyID = args[3];
            var productKeyState = args[4];
            var productKeyPartNumber = args[5];

            var connectionString = @"data source=BUBBA;initial catalog=EXOOAKeys2020;persist security info=True;user id=BUBBASQL;password=12345678;MultipleActiveResultSets=True;";
            var sqlConnection = new SqlConnection(connectionString);

            using (sqlConnection)
            {
                try
                {
                    sqlConnection.Open();
                    var sqlCommand = new SqlCommand("SaveProductKey", sqlConnection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    sqlCommand.Parameters.AddWithValue("@SerialNumber", serialNumber);
                    sqlCommand.Parameters.AddWithValue("@ProductKey", productKey);
                    sqlCommand.Parameters.AddWithValue("@ProductKeyID", productKeyID);
                    sqlCommand.Parameters.AddWithValue("@ProductKeyState", productKeyState);
                    sqlCommand.Parameters.AddWithValue("@ProductKeyPartNumber", productKeyPartNumber);
                    sqlCommand.Parameters.Add("@Output", SqlDbType.Char, 500);
                    sqlCommand.Parameters["@Output"].Direction = ParameterDirection.Output;
                    sqlCommand.ExecuteNonQuery();

                    var sqloutput = Convert.ToInt32(sqlCommand.Parameters["@Output"].Value);
                    //if (sqloutput != 0)
                    //{
                    //    var report = ExecuteSelectByProductKeyQuery(productKey, "SelectByProductKey");

                    //    //para el test de eze
                    //    if (report.State == "Consumed")
                    //        File.Create(AppDomain.CurrentDomain.BaseDirectory + "Estado.Consumed");

                    //    if (report.State == "Bound")
                    //        File.Create(AppDomain.CurrentDomain.BaseDirectory + "Estado.Bound");
                    //}
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

        private static bool IsArgsValid(string[] args)
        {
            foreach (var arg in args)
                if (string.IsNullOrWhiteSpace(arg))
                    throw new ArgumentNullException();

            Regex regexForProductKey = new Regex(@"^([A-Za-z0-9]{5}-){4}[A-Za-z0-9]{5}$");
            if (!regexForProductKey.IsMatch(args[1]))
                throw new ArgumentException("Error: El parámetro (1) ProductKey es inválido.");


            Regex regexForSerialNumber = new Regex(@"^\d{7}[a-gA-G]\d{5}$");
            if (!regexForSerialNumber.IsMatch(args[2]))
                throw new ArgumentException("Error: El parámetro (2) SerialNumber es inválido.");


            Regex regexForProductKeyID = new Regex(@"^[0-9]+$");
            if (!regexForProductKeyID.IsMatch(args[3]))
                throw new ArgumentException("Error: El parámetro (3) ProductKeyID es inválido.");


            Regex regexForProductKeyPartNumber = new Regex(@"^([A-Za-z0-9]{3}-)[0-9]{5}$");
            if (!regexForProductKeyPartNumber.IsMatch(args[4]))
                throw new ArgumentException("Error: El parámetro (4) ProductKeyPartNumber es inválido.");

            return true;
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



        private static void ShowHelp()
        {
            Console.WriteLine("");
            Console.WriteLine(" Opciones:");
            Console.WriteLine("");
            //Console.WriteLine("      /sk { SerialNumber ProductKey ProductKeyID ProductKeyState ProductKeyPartNumber }");
            //Console.WriteLine("      Crea un nuevo registro en la base de datos con estado 'Consumed'");
            //Console.WriteLine("");
            //Console.WriteLine("      /hk { SerialNumber ProductKey }");
            //Console.WriteLine("      Cambia el estado de un registro de 'Consumed' a 'Bound'");
            //Console.WriteLine("");
            Console.WriteLine("      /SaveAssembledFile {Ruta al archivo XML} [Origen]");
            Console.WriteLine("      Crea un nuevo registro en la base de datos con estado 'Consumed' a partir del archivo OA3.xml dado.");
            Console.WriteLine("      El parámetro 'Origen' es opcional. Si no se especifíca se asigna el valor 'N/A'. Máximo 10 caracteres.");
            Console.WriteLine("");
            Console.WriteLine("      /SetAsBound { SerialNumber ProductKey }");
            Console.WriteLine("      Cambia el estado de un registro de 'Consumed' a 'Bound' si ambos valores estan asociados en la base de datos.");
            Console.WriteLine("");
            Console.WriteLine("      /SaveReportedFile { SerialNumber ProductKey }");
            Console.WriteLine("      Crea un nuevo registro en la base de datos del archivo OA3.xml reportado luego de crear el Hardware Hash.");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(" Ejemplos:");
            Console.WriteLine(@"      EXOKeyCheck /SaveAssembledFile C:\OA3\OA3.xml Microsoft");
            Console.WriteLine(@"      EXOKeyCheck /SetAsBound C:\OA3\OA3.xml");
            Console.WriteLine(@"      EXOKeyCheck /SaveReportedFile C:\OA3\OA3.xml");
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
            Console.WriteLine("     -5  = SerialNumber y ProductKey ya existen en base de datos.");
            Console.WriteLine("");
            Console.WriteLine(" Códigos de salida para el párametro '/hk':");
            Console.WriteLine("");
            Console.WriteLine("     -4  = SerialNumber y ProductKey existen en la base de datos, pero no estan asociados.");
            Console.WriteLine("     -6  = No se encontró ProductKey en la base de datos.");
            Console.WriteLine("     -7  = No se encontró SerialNumber en la base de datos.");
            Console.WriteLine("     -9  = El el estado del registro en la base de datos es distinto de 'Consumed'.");
            Console.WriteLine("     -13 = No se encontró ProductKey ni SerialNumber en la base de datos.");
            Console.WriteLine("");

            Environment.Exit(0);
        }
    }
}
