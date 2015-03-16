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

            // Initialize necessary components
            var reader = new LectopiaReader();
            IProgress<Double> progressCallback = new Progress<Double>(progress =>
            {
                Console.Write("\r");
                Console.Write("{0}% completed.", progress.ToString("#.##"));
            });

            // Perform lengthy reading operation
            Console.WriteLine("Reading pages");
            reader.ReadCourseData(end: 400, callback: progressCallback);

            // Print statistics
            var list = reader.RecordingListURLs;
            Console.WriteLine();
            Console.WriteLine("{0} successful reads.", reader.RecordingListURLs.Count());

            Console.ReadLine();
        }
    }
}
