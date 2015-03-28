using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RMITLectopiaReader.Model;
using Newtonsoft.Json;
using System.IO;

namespace RMITLectopiaReader
{
    class Program
    {
        private LectopiaReader reader;
        private LectopiaModel model;
        private Menu menu;

        // Constructor
        public Program()
        {
            reader = new LectopiaReader();
            model = new LectopiaModel();
            menu = new Menu();
        }

        public void Run()
        {
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
                        ReadListings();
                        break;
                    case MenuOption.SEARCH_COURSES:
                        SearchCourses();
                        break;
                    case MenuOption.DISPLAY_RECORDINGS:
                        DisplayRecordings();
                        break;
                    case MenuOption.EXPORT_JSON:
                        ExportToJson();
                        break;
                    case MenuOption.PROGRAM_STATISTICS:
                        ProgramStatistics();
                        break;
                    case MenuOption.EXIT:
                        Console.WriteLine("Exiting");
                        break;
                }
                Console.WriteLine();
            } while (selectedOption != MenuOption.EXIT);

            Console.ReadLine();
        }

        void ReadListings()
        {
            int startID;
            int endID;
            Boolean validInput = false;
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
            List<CourseInstance> courses = reader.ReadListingsInRange(startID, endID, progressCallback);
            progressCallback.Report(100);
            DateTime endTime = DateTime.Now;

            // Add course information to model
            courses.ForEach(c => model.CourseInstances[c.ID] = c);

            // Print statistics
            Console.WriteLine("Successfully read {0} out of {1} pages.", courses.Count(), endID - startID);
            Console.WriteLine("{0} connections timed out.", reader.UnsuccessfulURLs.Count() - initialFailCount);
            Console.WriteLine("Operation took {0}", endTime - startTime);
        }

        void SearchCourses()
        {
            // Print heading
            Console.WriteLine("Search courses");
            Console.WriteLine("----------------");

            // Prompt user for a search term
            Console.Write("Search term: ");
            String searchTerm = Console.ReadLine();

            // Retrieve list of courses containing the given substring
            var matchingCourses = model.CourseInstances.Values.Where(
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

        void ProgramStatistics()
        {
            // Print heading
            Console.WriteLine("Program statistics");
            Console.WriteLine("--------------------");

            // Display general information
            Console.WriteLine("{0} listings stored in data", model.CourseInstances.Count());
            Console.WriteLine("{0} URLs retained for later re-attempt", reader.UnsuccessfulURLs.Count());
        }

        void DisplayRecordings()
        {
            // Print heading
            Console.WriteLine("Display recordings");
            Console.WriteLine("--------------------");

            // Prompt user to enter a course ID
            int id = menu.GetIntegerInput("Please enter a course ID: ");

            // If reader has a course with the matching ID, display recordings
            if (!model.CourseInstances.ContainsKey(id))
            {
                Console.WriteLine("Failed to find course instance with matching ID.");
            }
            else
            {
                var course = model.CourseInstances[id];
                var recordings = course.Recordings;
                Console.WriteLine("Displaying recordings for {0}", course.Name);
                Console.WriteLine("-----------------------------");

                foreach (var recording in recordings)
                {
                    Console.WriteLine("{0, -15} | {1, -10}", recording.DateRecorded, recording.Duration);
                }
            }
        }

        void ExportToJson()
        {
            // Print heading
            Console.WriteLine("Export to JSON");
            Console.WriteLine("----------------");

            Console.WriteLine("Writing to data.json...");
            // Write data out to file
            // TODO: Prompt user for filepath to save at
            using (StreamWriter sw = new StreamWriter("data.json"))
            {
                var jsonString = JsonConvert.SerializeObject(model.CourseInstances);
                sw.Write(jsonString);
            }

            // Print completion message
            Console.WriteLine("File saved.");
        }

        static void Main(string[] args)
        {
            new Program().Run();
        }
    }
}
