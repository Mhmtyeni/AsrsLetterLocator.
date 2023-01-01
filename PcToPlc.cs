using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsrsLetterLocator
{
    public class PcToPlc
    {
        public bool bPcActive { get; set; }
        public bool bTaskRequest { get; set; }
        public bool bGetProduct { get; set; }
        public bool bDropProduct { get; set; }
        public bool bTaskDelete { get; set; }
        public bool Reserve { get; set; }
        public bool Reserve1 { get; set; }
        public bool Reserve2 { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        public int Y { get; set; }
    }
}
