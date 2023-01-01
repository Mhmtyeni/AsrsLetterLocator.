using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsrsLetterLocator
{
    public class PlcToPc
    {
        public bool bPlcActive { get; set; }
        public bool bTaskExist { get; set; }
        public bool bTaskTaken { get; set; }
        public bool bTaskDone { get; set; }
        public bool bTaskDeleted { get; set; }
        public bool bAutomaticMode { get; set; }
        public bool bError { get; set; }
        public bool bConveyorProduct { get; set; }
        public bool bCraneProduct { get; set; }
        public bool Reserve1 { get; set; }
        public bool Reserve2 { get; set; }
        public bool Reserve3 { get; set; }
        public int ActX { get; set; }
        public int ActZ { get; set; }
        public int ActY { get; set; }
    }
}
