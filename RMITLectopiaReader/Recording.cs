using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMITLectopiaReader
{
    class Recording
    {
        // Properties
        public int ID { get; set; }
        public DateTime DateRecorded { get; set; }
        public String Speaker { get; set; }
        public int Duration { get; set; }
        public List<RecordingFormat> RecordingFormats { get; set; }

        // Constructor
        public Recording()
        {
            RecordingFormats = new List<RecordingFormat>();
        }
    }
}
