using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsrsLetterLocator
{
    public class Shelf
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public bool Status { get; set; }
        public int SX { get; set; }
        public int SY { get; set; }
        public int SZ { get; set; }
    }
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }
        public int ShelfId { get; set; }
    }
    public enum GetProductRequestProcess
    {
        Initialize = 0,
        CheckCraneIsNotBusy,
        SendRequest,
        WaitForRequestDone,
        Done
    }
    public enum DropProductRequestProcess
    {
        Initialize = 0,
        CheckCraneIsNotBusy,
        SendRequest,
        WaitForRequestDone,
        Done
    }
}

