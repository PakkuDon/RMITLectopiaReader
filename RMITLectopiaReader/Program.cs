using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RMITLectopiaReader
{
    class Program
    {
        static void Main(string[] args)
        {
            // Print heading
            Console.WriteLine("RMIT Lectopia Reader");
            Console.WriteLine("------------------------");
            Console.WriteLine();
            Console.WriteLine("Started at {0}", DateTime.Now.ToString());

            // Initialize necessary components
            var reader = new LectopiaReader();
            IProgress<Double> progressCallback = new Progress<Double>(progress =>
            {
                Console.Write("\r");
                Console.Write("{0}% completed.", progress.ToString("#.##"));
            });

            // Perform lengthy reading operation
            Console.WriteLine("Reading pages");
            reader.ReadCourseData(start: 10, end: 20, callback: progressCallback);

            // Print statistics
            var list = reader.CourseInstances;
            Console.WriteLine();
            Console.WriteLine("Finished at {0}", DateTime.Now.ToString());
            Console.WriteLine("{0} successful reads.", reader.CourseInstances.Count());

            Console.ReadLine();
        }
    }
}
