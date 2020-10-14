﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;

namespace EXOKeyCheck
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var option = args[0];
                if (option == "/?") { ShowHelp(); }
                //var productkeycode = args[1].ToUpper();
                var key = args[1].ToUpper();
                var serialNumber = args[2].ToUpper();
                SelectQuery(option, key, serialNumber);
            }
            catch(IndexOutOfRangeException)
            {
                //Console.Writeline("Argumento(s) invalido(s)");
            }
            catch (Exception e)
            {
                //Console.Writeline(e.Message);
            }
            finally
            {
                Environment.Exit(-1);
            }
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
                //Console.Writeline("No se encontró la clave en la base de datos.");
                Environment.Exit(-6);
            }

            if (report.State == "Bound")
            {
                //Console.Writeline("La clave ya está hasheada con el serial: " + report.SerialNumber);
                Environment.Exit(-9);
            }

            if (report.SerialNumber == serialNumber)
            {
                ExecuteHashQuery(report.ReportID);
            }
            else
            {
                //Console.Writeline("La clave no coincide con el numero de serie en la base de datos.");
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
                        //Console.Writeline("Error en la red intentando guardar registro." + Environment.NewLine + e.Message);
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
                catch (Exception e )
                {
                    //Console.Writeline("Error intentando obtener registro." + e.Message);
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
                        //Console.Writeline("Error en la red intentando guardar registro." + Environment.NewLine + e.Message);
                        Environment.Exit(-8);
                    }
                    //Console.Writeline("Error intentando guardar registro." + Environment.NewLine + e.Message);
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        private static void ShowHelp()
        {
            //Console.Writeline("");
            //Console.Writeline(" EXOOACheck /[ sk | hk ] {OAKey SerialNumber}");
            //Console.Writeline("");
            //Console.Writeline("     /sk {OAKey SerialNumber}: Crea un nuevo registro en la base de datos.");
            //Console.Writeline("     /hk {OAKey SerialNumber}: Coloca a la clave como 'Hasheada'.");
            //Console.Writeline("");
            //Console.Writeline(" Códigos de salida:");
            //Console.Writeline("");
            //Console.Writeline("      0 = OK");
            //Console.Writeline("     -1 = ERROR en aplicación.");
            //Console.Writeline("     -2 = OA KEY existente en base de datos.");
            //Console.Writeline("     -3 = SERIAL NUMBER existente en base de datos.");
            //Console.Writeline("     -4 = OA KEY no coincide con SERIAL NUMBER en base de datos.");
            //Console.Writeline("     -5 = OA KEY y SERIAL NUMBER existentes en base de datos.");
            //Console.Writeline("     -6 = OA KEY inexistente en base de datos");
            //Console.Writeline("     -7 = SERIAL NUMBER inexistente en base de datos");
            //Console.Writeline("     -8 = ERROR DE RED intentando conectarse a la base de datos");
            //Console.Writeline("     -9 = OA KEY ya se encuentra hasheado con anterioridad");
            //Console.Writeline("     -13 = OA KEY y SERIAL NUMBER inexistentes en base de datos");

            Environment.Exit(0);
        }
    }
}
