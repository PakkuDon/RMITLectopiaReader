﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMITLectopiaReader
{
    class CourseInstance
    {
        // Properties
        public int ID { get; set; }
        public String Name { get; set; }
        public List<String> PageLinks { get; set; }
        public DateTime? LastUpdated { get; set; }

        // Constructor
        public CourseInstance(int ID, String Name, DateTime? lastUpdated)
        {
            this.ID = ID;
            this.Name = Name;
            this.PageLinks = new List<String>();
            this.LastUpdated = lastUpdated;
        }
    }
}
