using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using Sigmasoft.Application.Domain;
using Sigmasoft.Application.Helper;

namespace Sigmasoft.Application.Services
{
    public class SSHManagerService
    {
        private SSHConfig _sshConfig;
        private SSHLocalServer _sshLocalServer;
        private SSHLocalServer _sshLocalServerBounding;
        private ConnectionInfo connInfo;

        private SshClient clientSsh;

        public SSHManagerService(SSHConfig sshConfig, SSHLocalServer sshLocalServer, SSHLocalServer sshLocalServerBounding)
        {
            this._sshConfig = sshConfig;
            this._sshLocalServer = sshLocalServer;
            this._sshLocalServerBounding = sshLocalServerBounding; 

            connInfo = new ConnectionInfo(
                this._sshConfig.Host,
                this._sshConfig.Port,
                this._sshConfig.UserName,
                new PasswordAuthenticationMethod(this._sshConfig.UserName, this._sshConfig.Pwd)
                );

            this.clientSsh = new SshClient(connInfo);
        }

        public bool IsConnected() => this.clientSsh.IsConnected;

        public void Start()
        {
            try
            {
                clientSsh.Connect();

                ForwardedPortLocal portFwld = new ForwardedPortLocal(
                    this._sshLocalServerBounding.Hote, 
                    Convert.ToUInt32(this._sshLocalServerBounding.Port), 
                    this._sshLocalServer.Hote, 
                    Convert.ToUInt32(this._sshLocalServer.Port));

                clientSsh.AddForwardedPort(portFwld);

                if (clientSsh.IsConnected == true)
                {
                    portFwld.Start();
                }
            }
            catch (Exception e)
            {
                Log.WriteToFile(e.Message);
            }
        }

        public void Disconnect()
        {
            this.clientSsh.Disconnect();
        }


    }
}
