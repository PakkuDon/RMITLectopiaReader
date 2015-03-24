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
            // Initialize necessary components
            var reader = new LectopiaReader();
            var menu = new Menu();

            // Print heading
            menu.DisplayHeader();
            Console.WriteLine("Started at {0}", DateTime.Now.ToString());

            MenuOption selectedOption;

            do
            {
                menu.DisplayOptions();
                Console.WriteLine();
                // TODO: Validation
                selectedOption = (MenuOption)menu.GetIntegerInput("Please select an option: ");

                // Process selected option
                switch (selectedOption)
                {
                    case MenuOption.READ_LISTINGS:
                        ReadListings(menu, reader);
                        break;
                    case MenuOption.FIND_COURSE:
                        Console.WriteLine("Find course");
                        break;
                    case MenuOption.EXPORT_JSON:
                        Console.WriteLine("Export JSON");
                        break;
                    case MenuOption.EXIT:
                        Console.WriteLine("Exiting");
                        break;
                }
                Console.WriteLine();
            } while (selectedOption != MenuOption.EXIT);

            Console.ReadLine();
        }

        static void ReadListings(Menu menu, LectopiaReader reader)
        {
            Console.WriteLine("Read listings");
            Console.WriteLine("---------------");
            // Create callback object
            IProgress<Double> progressCallback = new Progress<Double>(progress =>
            {
                Console.Write("\r");
                Console.Write("{0}% completed.", progress.ToString("#.00"));
            });

            // Ask user for start and end points of read operation
            int start;
            int end;

            Boolean validInput = false;
            do
            {
                start = menu.GetIntegerInput("Enter a starting ID: ");
                end = menu.GetIntegerInput("Enter an ending ID: ");

                if (start > end)
                {
                    Console.WriteLine("Start ID must be less than end ID. Please try again.");
                }
                else
                {
                    validInput = true;
                }
            } while (!validInput);

            // Perform lengthy read operation
            Console.WriteLine("Fetching data...");
            progressCallback.Report(0);
            reader.ReadCourseData(start, end, progressCallback);
            progressCallback.Report(100);

            // Print statistics
            Console.WriteLine("Successfully read {0} out of {1} pages.", reader.CourseInstances.Count(), end);
            foreach (var course in reader.CourseInstances)
            {
                Console.WriteLine(course.Name);
            }
        }
    }
}
