using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMITLectopiaReader.Model
{
    class LectopiaModel
    {
        public LectopiaModel()
        {
            CourseInstances = new Dictionary<int, CourseInstance>();
        }

        // Properties
        public Dictionary<int, CourseInstance> CourseInstances { get; private set; }
    }
}
