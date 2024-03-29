﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MySqlConnector;
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

        private string connectionDestString;
        private string connectionCreateDbString;

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
                //ConnectionTimeout = 999999,
                //ConvertZeroDateTime = true
            };

            this.connectionDestString = $"Server={this.Destination.Source};" +
                $"Port={this.Destination.Port};" +
                $"User ID={Destination.User};" +
                $"Password={this.Destination.Password};" +
                $"Database={this.Destination.Database}";

            this.connectionCreateDbString = $"Server={this.Destination.Source};" +
                $"Port={this.Destination.Port};" +
                $"User ID={Destination.User};" +
                $"Password={this.Destination.Password}";

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

         /// <summary>
         /// Cette méthode permet de vérifié si la table journal_restauration existe,
         /// si cette table n'éxiste pas alors elle la crée puis fait une insertion dessus
         /// qui indique la date de la dérnière réstauration
         /// </summary>
         /// <param name="connection"></param>
        private void CreateOrUpdate(MySqlConnection connection)
        {
            connection.Open();

            string commandText = "";
            var command = new MySqlCommand();
            command.Connection = connection;

            commandText = "SHOW CREATE TABLE `journal_restauration`;";
            command.CommandText = commandText;
            var read = command.ExecuteReader();
            bool hasRow = read.HasRows;
            
            if(hasRow == true)
            {
                connection.Close();

                connection.Open();
                commandText = "SELECT COUNT(*) FROM `journal_restauration`";
                command.Connection = connection;
                command.CommandText = commandText;

                read = command.ExecuteReader();

                var result = read.HasRows;
                
                if (result == false)
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

                    commandText = "SELECT MAX(j0.Id) Id FROM `journal_restauration` j0";
                    command.Connection = connection;
                    command.CommandText = commandText;
                    var reader = command.ExecuteReader();

                    int journal_id = 0;

                    while (reader.Read())
                    {
                        journal_id = reader.GetInt32(0);
                    }

                    connection.Close();

                    connection.Open();
                    commandText = $"UPDATE `journal_restauration` SET `libelle`='{DateTime.Now}' WHERE  `Id`={journal_id}";
                    command.Connection = connection;
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            else
            {
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

        /// <summary>
        /// Cette méthode persmet de faire un backup sur la BD source choisi par l'utilisateur
        /// </summary>
        /// <returns>bool</returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="conn"></param>
        private void SetTimeOut(int time, MySqlConnection conn)
        {
             using (var cmd = new MySqlCommand())
             {

                 cmd.Connection = conn;

                 cmd.CommandText = $"set net_write_timeout={time}; set net_read_timeout={time}";

                 cmd.ExecuteNonQuery();
             }
        }

        /// <summary>
        /// Cette méthode permet de faire une réstauration de la BD de déstination choisi par l'utilisateur
        /// </summary>
        public void Restore()
        {

            try
            {
                using (var conn = new MySqlConnection(connectionDestString))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        using (DbBackup = new MySqlBackup(cmd))
                        {
                            cmd.Connection = conn;

                            conn.Open();

                            fs = new FileStream(this.backupFile, FileMode.Open);

                            DbBackup.ImportFromStream(fs);

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

        /// <summary>
        /// cette méthode permet de créer une pase de donnée
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns>bool</returns>
        public bool CreateDatabase(string databaseName)
        {
            try
            {
                var cmdText = $"CREATE DATABASE `{databaseName}`";

                using (var sqlConnection = new MySqlConnection(connectionCreateDbString))
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
        /// 
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns>bool</returns>
        public bool RemoveDatabase(string databaseName)
        {
            try
            {
                var cmdText = $"DROP DATABASE `{databaseName}`";

                using (var sqlConnection = new  MySqlConnection(connectionCreateDbString))
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

                using (var sqlConnection = new MySqlConnection(connectionCreateDbString))
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
