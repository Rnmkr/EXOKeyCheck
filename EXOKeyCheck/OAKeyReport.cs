using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EXOKeyCheck
{
    internal class OAKeyReport
    {

        private int _reportID;
        private string _oAKey;
        private string _serialNumber;
        private string _state;
        private DateTime _dateConsumed;
        private DateTime? _dateBound;

        public int ReportID { get => _reportID; set => _reportID = value; }
        public string OAKey { get => _oAKey; set => _oAKey = value; }
        public string SerialNumber { get => _serialNumber; set => _serialNumber = value; }
        public string State { get => _state; set => _state = value; }
        public DateTime DateConsumed { get => _dateConsumed; set => _dateConsumed = value; }
        public DateTime? DateBound { get => _dateBound; set => _dateBound = value; }
    }
}
