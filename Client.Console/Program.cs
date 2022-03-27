using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Sigmasoft.Application.Domain;
using Sigmasoft.Application.Helper;
using Sigmasoft.Application.Services;

namespace Client.Console
{
    class Program
    {
        //Service
        private static DbManagerService dbManager;
        //Entity
        private static DbConfig source = new DbConfig();
        private static DbConfig destination = new DbConfig();
        private static SheduleConfig shdConfig = new SheduleConfig();
        private static SSHConfig sshConfig = new SSHConfig();
        private static SSHLocalServer sshLocalServer = new SSHLocalServer();
        private static SSHLocalServer sshLocalServerBounding = new SSHLocalServer();
        //Config File
        private static string configSrc = Directory.GetCurrentDirectory() + "\\config\\configSrc.xml";
        private static string configDest = Directory.GetCurrentDirectory() + "\\config\\configDest.xml";
        private static string configSheduler = Directory.GetCurrentDirectory() + "\\config\\configTaskSheduler.xml";
        //Config File SSH
        private static string configSSH = Directory.GetCurrentDirectory() + "\\config\\configSSH.xml";
        private static string configLocalServer = Directory.GetCurrentDirectory() + "\\config\\configLocalServer.xml";
        private static string configLocalServerBouding = Directory.GetCurrentDirectory() + "\\config\\configLocalServerBounding.xml";

        public static Object ReadConfig(Object configSource, string fileConfig)
        {
            if (File.Exists(fileConfig))
            {
                XmlSerializer reader = new XmlSerializer(configSource.GetType());
                StreamReader file = new StreamReader(fileConfig);
                var entity = (Object)reader.Deserialize(file);
                file.Close();

                return entity;
            }

            return null;
        }

        static void Main(string[] args)
        {
            try
            {
                var src = (DbConfig)ReadConfig(source, configSrc);
                var dest = (DbConfig)ReadConfig(destination, configDest);
                var sheduler = (SheduleConfig)ReadConfig(shdConfig, configSheduler);

                dbManager = new DbManagerService(src, dest, sheduler);

                if (sheduler.IsEnableSSH == true)
                {
                    var sshCfg = (SSHConfig)ReadConfig(sshConfig, configSSH);
                    var sshLocal = (SSHLocalServer)ReadConfig(sshLocalServer, configLocalServer);
                    var sshBouding = (SSHLocalServer) ReadConfig(sshLocalServerBounding, configLocalServerBouding);

                    SSHManagerService sshManager = new SSHManagerService(sshCfg, sshLocal, sshBouding);

                    sshManager.Start();

                    if (sshManager.IsConnected())
                    {
                        System.Console.WriteLine("*========================================================*");
                        System.Console.WriteLine("|                                                        |");
                        System.Console.WriteLine("| CONNEXION EFFECTUER AVEC SUCCES SUR LE SERVEUR DISTANT |");
                        System.Console.WriteLine("|                                                        |");
                        System.Console.WriteLine("*========================================================*");

                        Thread.Sleep(3000);

                        bool backup = false;

                        int i = 0;

                        while (backup == false)
                        {
                            backup = dbManager.Backup();

                            if (backup == true)
                            {
                                System.Console.WriteLine("- Vérification de la base de donnée ** (local) **");

                                if (dbManager.CheckDatabase(dest.Database) == false)
                                {
                                    Thread.Sleep(3000);

                                    System.Console.WriteLine("- Création de la base de donnée ** (local) **");

                                    dbManager.CreateDatabase(dest.Database);

                                    Thread.Sleep(3000);

                                    System.Console.WriteLine("- Base de donnée ** (local) ** créer avec succès.");

                                    Thread.Sleep(3000);

                                    System.Console.WriteLine("- Réstoration de la base de donnée ** (local) **");

                                    dbManager.Restore();

                                    Thread.Sleep(3000);

                                    System.Console.WriteLine("- Réstoration de la base de donnée ** (local) ** éffectuer avec succès.");

                                }
                                else
                                {
                                    System.Console.WriteLine("- Suppression de la base de donnée ** (local) **");

                                    Thread.Sleep(3000);

                                    dbManager.RemoveDatabase(dest.Database);

                                    Thread.Sleep(3000);

                                    System.Console.WriteLine("- Suppression de la base de donnée ** (local) ** éffectuer avec succès.");

                                    Thread.Sleep(3000);

                                    System.Console.WriteLine("- Création de la base de donnée ** (local) **");

                                    dbManager.CreateDatabase(dest.Database);

                                    Thread.Sleep(3000);

                                    System.Console.WriteLine("- Création de la base de donnée ** (local) ** éffectuer avec succès.");

                                    dbManager.Restore();

                                    Thread.Sleep(3000);

                                    System.Console.WriteLine("- Réstoration de la base de donnée ** (local) ** éffectuer avec succès.");


                                }

                                if (sshManager.IsConnected() == true)
                                {
                                    sshManager.Disconnect();
                                }

                                System.Console.WriteLine("Opération términer avec succès !");

                                System.Console.ReadLine();
                            }
                            else
                            {
                                i++;

                                System.Console.WriteLine("Une erreur est survenue au moment du traitement.");

                                Thread.Sleep(3000);

                                System.Console.WriteLine($"({i}) - Tentative de recupèration des données.");

                                backup = false;
                            }
                        }
                    }
                    else
                    {
                        System.Console.WriteLine(
                            "Une erreur interne est survenue au moment de la tentadive de connexion au serveur SSH, " +
                            "si le problème persiste vérifier votre connexion internet si il est active ou veuiller " +
                            "contacter votre  (FAI)");

                        Thread.Sleep(3000);
                    }

                }
                else
                {
                    System.Console.WriteLine("*==============================================================*");
                    System.Console.WriteLine("|                                                              |");
                    System.Console.WriteLine("| CONNEXION EFFECTUER AVEC SUCCES SUR LE SERVEUR ** (LOCAL) ** |");
                    System.Console.WriteLine("|                                                              |");
                    System.Console.WriteLine("*==============================================================*");

                    if (dbManager.Backup() == true)
                    {
                        System.Console.WriteLine("- Vérification de la base de donnée ** (local) **");

                        if (dbManager.CheckDatabase(dest.Database) == false)
                        {
                            Thread.Sleep(3000);

                            System.Console.WriteLine("- Création de la base de donnée ** (local) **");

                            dbManager.CreateDatabase(dest.Database);

                            Thread.Sleep(3000);

                            System.Console.WriteLine("- Base de donnée ** (local) ** créer avec succès.");

                            dbManager.Restore();

                            Thread.Sleep(3000);

                            System.Console.WriteLine("- Réstoration de la base de donnée ** (local) ** éffectuer avec succès.");

                            System.Console.ReadLine();
                        }
                        else
                        {
                            dbManager.RemoveDatabase(dest.Database);

                            Thread.Sleep(3000);

                            System.Console.WriteLine("- Suppression de la base de donnée ** (local) ** éffectuer avec succès.");

                            Thread.Sleep(3000);

                            System.Console.WriteLine("- Création de la base de donnée ** (local) **");

                            dbManager.CreateDatabase(dest.Database);

                            Thread.Sleep(3000);

                            System.Console.WriteLine("- Base de donnée ** (local) ** créer avec succès.");

                            dbManager.Restore();

                            Thread.Sleep(3000);

                            System.Console.WriteLine("- Réstoration de la base de donnée ** (local) ** éffectuer avec succès.");

                            System.Console.ReadLine();
                        }

                        System.Console.WriteLine("Opération términer avec succès !");
                    }
                    else
                    {
                        System.Console.WriteLine("Une erreur est survenue au moment du traitement.");

                        Thread.Sleep(3000);

                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteToFile(e.Message);
            }
        }
    }
}
