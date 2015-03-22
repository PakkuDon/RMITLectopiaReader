using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMITLectopiaReader
{
    class Format
    {
        // Properties
        public int ID { get; set; }
        public String Name { get; set; }

        // Constructor
        public Format(int id, String name)
        {
            this.ID = id;
            this.Name = name;
        }
    }
}
