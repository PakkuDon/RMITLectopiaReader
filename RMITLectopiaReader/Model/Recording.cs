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
        public String Duration { get; set; }
        public List<Format> Formats { get; set; }

        // Constructor
        public Recording(int ID, DateTime dateRecorded, String duration)
        {
            this.ID = ID;
            this.DateRecorded = dateRecorded;
            this.Duration = duration;
            Formats = new List<Format>();
        }
    }
}
