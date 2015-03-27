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

                selectedOption = (MenuOption)menu.GetIntegerInput("Please select an option: ");
                Console.WriteLine();

                // Process selected option
                switch (selectedOption)
                {
                    case MenuOption.READ_LISTINGS:
                        ReadListings(menu, reader);
                        break;
                    case MenuOption.SEARCH_COURSES:
                        SearchCourses(menu, reader);
                        break;
                    case MenuOption.DISPLAY_RECORDINGS:
                        DisplayRecordings(menu, reader);
                        break;
                    case MenuOption.EXPORT_JSON:
                        Console.WriteLine("Export JSON");
                        break;
                    case MenuOption.PROGRAM_STATISTICS:
                        ProgramStatistics(menu, reader);
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
            int startID;
            int endID;
            Boolean validInput = false;
            int initialCourseCount = reader.CourseInstances.Count();
            int initialFailCount = reader.UnsuccessfulURLs.Count();

            Console.WriteLine("Read listings");
            Console.WriteLine("---------------");
            // Create callback object
            IProgress<Double> progressCallback = new Progress<Double>(progress =>
            {
                Console.Write("\r");
                Console.Write("{0}% completed.", progress.ToString("#.00"));
            });

            // Ask user for start and end points of read operation

            do
            {
                startID = menu.GetIntegerInput("Enter a starting ID: ");
                endID = menu.GetIntegerInput("Enter an ending ID: ");

                if (startID > endID)
                {
                    Console.WriteLine("Start ID must be less than end ID. Please try again.");
                }
                else
                {
                    validInput = true;
                }
            } while (!validInput);

            // Perform lengthy read operation
            DateTime startTime = DateTime.Now;
            Console.WriteLine("Fetching data...");
            progressCallback.Report(0);
            reader.ReadListingsInRange(startID, endID, progressCallback);
            progressCallback.Report(100);
            DateTime endTime = DateTime.Now;

            // Print statistics
            Console.WriteLine("Successfully read {0} out of {1} pages.", reader.CourseInstances.Count() - initialCourseCount, endID - startID);
            Console.WriteLine("{0} connections timed out.", reader.UnsuccessfulURLs.Count() - initialFailCount);
            Console.WriteLine("Operation took {0}", endTime - startTime);
        }

        static void SearchCourses(Menu menu, LectopiaReader reader)
        {
            // Print heading
            Console.WriteLine("Search courses");
            Console.WriteLine("----------------");

            // Prompt user for a search term
            Console.Write("Search term: ");
            String searchTerm = Console.ReadLine();

            // Retrieve list of courses containing the given substring
            var matchingCourses = reader.CourseInstances.Values.Where(
                c => c.Name.ToLower().Contains(searchTerm.ToLower()));

            // Display search results
            Console.WriteLine("{0} results found.", matchingCourses.Count());
            if (matchingCourses.Count() > 0)
            {
                Console.WriteLine("{0, -10} | {1, -20}", "ID", "Name");
                foreach (var course in matchingCourses)
                {
                    Console.WriteLine("{0, -10} | {1, -20}", course.ID, course.Name);
                }
            }
        }

        static void ProgramStatistics(Menu menu, LectopiaReader reader)
        {
            // Print heading
            Console.WriteLine("Program statistics");
            Console.WriteLine("--------------------");

            // Display general information
            Console.WriteLine("{0} listings stored in data", reader.CourseInstances.Count());
            Console.WriteLine("{0} URLs retained for later re-attempt", reader.UnsuccessfulURLs.Count());
        }

        static void DisplayRecordings(Menu menu, LectopiaReader reader)
        {
            // Print heading
            Console.WriteLine("Display recordings");
            Console.WriteLine("--------------------");

            // Prompt user to enter a course ID
            int id = menu.GetIntegerInput("Please enter a course ID: ");

            // If reader has a course with the matching ID, display recordings
            if (!reader.CourseInstances.ContainsKey(id))
            {
                Console.WriteLine("Failed to find course instance with matching ID.");
            }
            else
            {
                var course = reader.CourseInstances[id];
                var recordings = course.Recordings;
                Console.WriteLine("Displaying recordings for {0}", course.Name);
                Console.WriteLine("-----------------------------");

                foreach (var recording in recordings)
                {
                    Console.WriteLine("{0, -15} | {1, -10}", recording.DateRecorded, recording.Duration);
                }
            }
        }
    }
}
