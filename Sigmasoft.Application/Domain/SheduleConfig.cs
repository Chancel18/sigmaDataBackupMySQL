using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sigmasoft.Application.Domain
{
    public class SheduleConfig
    {
        public string FilePath { get; set; }
        public DateTime IntervaleTime { get; set; }
        public bool IsEnableSSH { get; set; }
    }
}
