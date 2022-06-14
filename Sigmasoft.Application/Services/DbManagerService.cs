using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Sigmasoft.Application.Domain;
using Sigmasoft.Application.Helper;

namespace Sigmasoft.Application.Services
{
    public class DbManagerService
    {
        public DbConfig Source { get; set; }
        public DbConfig Destination { get; set; }
        public SheduleConfig Shedule { get; set; }
        //Provider connection
        private MySqlConnectionStringBuilder connSrc;
        private MySqlConnectionStringBuilder connDest;
        //Check DB
        private MySqlConnectionStringBuilder connCreateDb;
        //Connection DB
        private MySqlConnection _conn;
        // file
        private string backupFile;

        private FileStream fs;

        public MySqlBackup DbBackup { get; set; } = new MySqlBackup();

        public DbManagerService() {}

        public DbManagerService(MySqlBackup dbBackup)
        {
            this.DbBackup = dbBackup;
        }

        public DbManagerService(DbConfig src, DbConfig dest, SheduleConfig shedule)
        {
            this.Source = src;
            this.Destination = dest;
            this.Shedule = shedule;

            this.InitializeConnection();
        }

        public void InitializeConnection()
        {
            connSrc = new MySqlConnectionStringBuilder
            {
                Server = this.Source.Source,
                Port = this.Source.Port,
                UserID = this.Source.User,
                Password = this.Source.Password,
                Database = this.Source.Database,
                ConnectionTimeout = 999999,
                ConvertZeroDateTime = true
            };

            connDest = new MySqlConnectionStringBuilder
            {
                Server = this.Destination.Source,
                Port = this.Destination.Port,
                UserID = this.Destination.User,
                Password = this.Destination.Password,
                Database = this.Destination.Database,
                ConnectionTimeout = 999999,
                ConvertZeroDateTime = true
            };

            connCreateDb = new MySqlConnectionStringBuilder
            {
                Server = this.Destination.Source,
                UserID = this.Destination.User,
                Password = this.Destination.Password,
                ConnectionTimeout = 999999,
                ConvertZeroDateTime = true,
            };
        }

        public void CloseConnection()
        {
            if (this._conn != null)
            {
                this._conn.Close();
                this._conn.Dispose();
                this.DbBackup.StopAllProcess();
            }
        }

        private void CreateOrUpdate(MySqlConnection connection)
        {
            connection.Open();

            string commandText = "";
            var command = new MySqlCommand();
            command.Connection = connection;

            commandText = "SHOW CREATE TABLE `journal_restauration`;";
            command.CommandText = commandText;
            var hasRow = command.ExecuteReader();
            

            if(hasRow.HasRows)
            {
                connection.Close();

                connection.Open();
                commandText = "SELECT COUNT(*) FROM `journal_restauration`";
                command.Connection = connection;
                command.CommandText = commandText;

                var result = command.ExecuteReader();
                
                
                if (result.HasRows == false)
                {
                    connection.Close();

                    connection.Open();
                    commandText = $"INSERT INTO `journal_restauration` (`libelle`) VALUES ('({DateTime.Now})');";
                    command.Connection = connection;
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                    connection.Close();
                }
                else
                {
                    connection.Close();

                    connection.Open();
                    commandText = $"UPDATE `journal_restauration` SET `libelle`='{DateTime.Now}' WHERE  `Id`=1";
                    command.Connection = connection;
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            else
            {
                throw new Exception();

                string path = Path.GetFullPath("Scripts\\script.txt");

                foreach (var line in File.ReadLines(path))
                {
                    commandText += line;
                }

                connection.Open();
                command.CommandText = commandText;
                command.Connection = connection;
                command.ExecuteNonQuery();
                connection.Close();

                connection.Open();
                commandText = $"INSERT INTO `journal_restauration` (`libelle`) VALUES ('({DateTime.Now})');";
                command.Connection = connection;
                command.CommandText = commandText;
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        public bool Backup()
        {
            try
            {
                this.backupFile = $"{this.Shedule.FilePath}\\dump{DateTime.Now.Date.ToShortDateString().Replace('/', 'd')}_{DateTime.Now.ToShortTimeString().Replace(':', 'h')}m{DateTime.Now.Millisecond}.sql";

                fs = File.Create(backupFile);

                this._conn = new MySqlConnection(connSrc.ConnectionString);

                var cmd = new MySqlCommand();

                this.CreateOrUpdate(_conn);

                this._conn.Open();

                this.SetTimeOut(999999, this._conn);

                cmd.Connection = this._conn;

                cmd.CommandTimeout = 999999;

                DbBackup.Command = cmd;

                DbBackup.ExportInfo.ResetAutoIncrement = true;

                DbBackup.ExportToStream(fs);

                fs.Close();

                return true;
            }
            catch (Exception e)
            {
                Log.WriteToFile(e.Message);

                fs.Close();

                File.Delete(fs.Name);

                return false;
            }
        }

        private void SetTimeOut(int time, MySqlConnection conn)
        {
             using (var cmd = new MySqlCommand())
             {

                 cmd.Connection = conn;

                 cmd.CommandText = $"set net_write_timeout={time}; set net_read_timeout={time}";

                 cmd.ExecuteNonQuery();
             }
        }

        public void Restore()
        {

            try
            {
                using (var conn = new MySqlConnection(connDest.ConnectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        using (DbBackup = new MySqlBackup(cmd))
                        {
                            cmd.Connection = conn;

                            conn.Open();

                            fs = new FileStream(this.backupFile, FileMode.Open);

                            DbBackup.ImportFromStream(fs);

                            //var path = Path.GetFullPath("Scripts\\script.txt");

                            //string commandText = "";

                            //foreach (var line in File.ReadLines(path))
                            //{
                            //    commandText += line;
                            //}

                            //cmd.CommandText = commandText;

                            //cmd.ExecuteNonQuery();

                            //commandText = $"INSERT INTO `journal_restauration` (`libelle`) VALUES ('({DateTime.Now})');";

                            //cmd.CommandText = commandText;

                            cmd.ExecuteNonQuery();

                            fs.Close();

                            conn.Close();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                fs.Close();

                Log.WriteToFile(e.Message);

            }
        }

        public bool CreateDatabase(string databaseName)
        {
            try
            {
                var cmdText = $"CREATE DATABASE `{databaseName}`";

                using (var sqlConnection = new MySqlConnection(connCreateDb.ConnectionString))
                {
                    using (var sqlCmd = new MySqlCommand(cmdText, sqlConnection))
                    {
                        // Open the connection as late as possible
                        sqlConnection.Open();

                        // count(*) will always return an int, so it's safe to use Convert.ToInt32
                        return Convert.ToInt32(sqlCmd.ExecuteNonQuery()) == 1;
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteToFile(e.Message);
                throw;
            }

        }

        public bool RemoveDatabase(string databaseName)
        {
            try
            {
                var cmdText = $"DROP DATABASE `{databaseName}`";

                using (var sqlConnection = new  MySqlConnection(connDest.ConnectionString))
                {
                    using (var sqlCmd = new MySqlCommand(cmdText, sqlConnection))
                    {
                        // Open the connection as late as possible
                        sqlConnection.Open();

                        // count(*) will always return an int, so it's safe to use Convert.ToInt32
                        return Convert.ToInt32(sqlCmd.ExecuteNonQuery()) == 1;
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteToFile(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Cette méthode permet de vérifié si une base de donnée existe
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public bool CheckDatabase(string databaseName)
        {
            try
            {
                var cmdText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = @database";

                using (var sqlConnection = new MySqlConnection(connCreateDb.ConnectionString))
                {
                    using (var sqlCmd = new MySqlCommand(cmdText, sqlConnection))
                    {
                        // Use parameters to protect against Sql Injection
                        sqlCmd.Parameters.AddWithValue("@database", databaseName);

                        // Open the connection as late as possible
                        sqlConnection.Open();

                        // count(*) will always return an int, so it's safe to use Convert.ToInt32
                        return Convert.ToInt32(sqlCmd.ExecuteScalar()) == 1;
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteToFile(e.Message);
                throw;
            }

        }
    }
}
