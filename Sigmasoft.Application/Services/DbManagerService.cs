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
        private DbConfig _source;
        private DbConfig _destination;
        private SheduleConfig _shedule;
        //Provider connection
        private MySqlConnectionStringBuilder connSrc;
        private MySqlConnectionStringBuilder connDest;
        //Check DB
        private MySqlConnectionStringBuilder connCreateDb;
        // file
        private string backupFile;

        public DbManagerService(DbConfig src, DbConfig dest, SheduleConfig shedule)
        {
            this._source = src;
            this._destination = dest;
            this._shedule = shedule;

            connSrc = new MySqlConnectionStringBuilder
            {
                Server = this._source.Source,
                Port = this._source.Port,
                UserID = this._source.User,
                Password = this._source.Password,
                Database = this._source.Database,
                ConnectionTimeout = 400000,
                ConvertZeroDateTime = true
            };

            connDest = new MySqlConnectionStringBuilder
            {
                Server = this._destination.Source,
                Port = this._destination.Port,
                UserID = this._destination.User,
                Password = this._destination.Password,
                Database = this._destination.Database,
                ConnectionTimeout = 400000,
                ConvertZeroDateTime = true
            };

            connCreateDb = new MySqlConnectionStringBuilder
            {
                Server = this._destination.Source,
                UserID = this._destination.User,
                Password = this._destination.Password,
                ConnectionTimeout = 400000,
                ConvertZeroDateTime = true,
            };
        }

        public bool Backup()
        {
            try
            {
                this.backupFile = $"{this._shedule.FilePath}\\backup_{DateTime.Now.ToShortDateString().Replace('/', '_')}_{DateTime.Now.ToShortTimeString().Replace(':', '_')}_{DateTime.Now.Millisecond}.sql";

                using (var conn = new MySqlConnection(connSrc.ConnectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {

                        using (MySqlBackup mb = new MySqlBackup(cmd))
                        {
                            conn.Open();

                            this.SetTimeOut(99999, conn);

                            cmd.Connection = conn;
                            
                            var fs = new FileStream(this.backupFile, FileMode.Create);

                            mb.ExportToStream(fs);

                            fs.Close();

                            conn.Close();

                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteToFile(e.Message);

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
                        using (MySqlBackup mb = new MySqlBackup(cmd))
                        {
                            cmd.Connection = conn;
                            conn.Open();
                            mb.ImportFromFile(this.backupFile);
                            conn.Close();
                        }
                    }
                }
            }
            catch (Exception e)
            {
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
