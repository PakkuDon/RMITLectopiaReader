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
        private IProgress<Double> progressCallback;

        // Constructor
        public Program()
        {
            reader = new LectopiaReader();
            model = new LectopiaModel();
            menu = new Menu();
            var lockObj = new Object();
            progressCallback = new Progress<Double>(progress =>
            {
                lock (lockObj)
                {
                    Console.Write("\r");
                    Console.Write("{0}% completed.", progress.ToString("#.00"));
                }
            });
        }

        public void Run()
        {
            // Print heading
            menu.DisplayHeader();
            Console.WriteLine("Started at {0}", DateTime.Now.ToString());

            int selectedOption;

            do
            {
                menu.DisplayOptions();
                Console.WriteLine();

                // Prompt user for input
                selectedOption = menu.GetIntegerInput("Please select an option: ");
                Console.WriteLine();

                // If valid option selected, process selected option
                // Else, print error
                if (menu.Options.ContainsKey(selectedOption))
                {
                    Menu.MenuOption option = menu.Options[selectedOption];
                    // If non-exit option selected, call relevant method
                    // Else, end loop
                    if (option.Method != null)
                    {
                        option.Method();
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid option. Please try again.");
                }

                Console.WriteLine();
            } while (true);

            Console.WriteLine("Program terminating. Press ENTER to exit.");
            Console.ReadLine();
        }

        void ReadListings()
        {
            int startID;
            int endID;
            Boolean validInput = false;
            int initialFailCount = reader.TimedOutIDs.Count();

            Console.WriteLine("Read listings");
            Console.WriteLine("---------------");

            // Ask user for start and end points of read operation
            // If user enters empty line, return to menu
            do
            {
                startID = menu.GetIntegerInput("Enter a starting ID: ");
                if (startID == Menu.DEFAULT_OPTION)
                {
                    return;
                }

                endID = menu.GetIntegerInput("Enter an ending ID: ");
                if (endID == Menu.DEFAULT_OPTION)
                {
                    return;
                }

                // If input start and end IDs are invalid, print error
                // Else set flag to exit loop
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
            Console.WriteLine("{0} connections timed out.", reader.TimedOutIDs.Count() - initialFailCount);
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
            var matchingCourses = from c in model.CourseInstances.Values
                                  where c.Name.ToLower().Contains(searchTerm.ToLower())
                                  orderby c.Name
                                  select c;

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
            Console.WriteLine("{0} URLs retained for later re-attempt", reader.TimedOutIDs.Count());
        }

        void DisplayRecordings()
        {
            // Print heading
            Console.WriteLine("Display recordings");
            Console.WriteLine("--------------------");

            // Prompt user to enter a course ID
            int id = menu.GetIntegerInput("Please enter a course ID: ");

            // If user enters empty line, cancel operation
            if (id == Menu.DEFAULT_OPTION)
            {
                return;
            }

            // If reader has a course with the matching ID, display recordings
            if (!model.CourseInstances.ContainsKey(id))
            {
                Console.WriteLine("Failed to find course instance with matching ID.");
            }
            else
            {
                var course = model.CourseInstances[id];
                var recordings = from r in course.Recordings
                                 orderby r.DateRecorded
                                 select r;
                Console.WriteLine("Displaying recordings for {0}", course.Name);
                Console.WriteLine("{0, -10} | {1, -8} | {2, -10}", "Date", "Time", "Duration");

                foreach (var recording in recordings)
                {
                    Console.WriteLine("{0, -10} | {1, -8} | {2, -10}",
                        recording.DateRecorded.ToShortDateString(),
                        recording.DateRecorded.ToShortTimeString(),
                        recording.Duration);
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

        void RetryFailedReads()
        {
            // If no failed URLs to read, print error and return
            if (reader.TimedOutIDs.Count() == 0)
            {
                Console.WriteLine("No URLs to reattempt.");
            }
            else
            {
                // Print heading
                Console.WriteLine("Retry failed reads");
                Console.WriteLine("---------------------");

                // Perform read operation
                var courses = reader.ReadFailedURLs(progressCallback);

                // Store read data in model
                foreach (var course in courses)
                {
                    model.CourseInstances[course.ID] = course;
                }
            }
        }

        static void Main(string[] args)
        {
            var program = new Program();

            // Add options to menu;
            program.menu.AddOption("Read listings", program.ReadListings);
            program.menu.AddOption("Search courses", program.SearchCourses);
            program.menu.AddOption("Display recordings", program.DisplayRecordings);
            program.menu.AddOption("Export to JSON", program.ExportToJson);
            program.menu.AddOption("Program statistics", program.ProgramStatistics);
            program.menu.AddOption("Retry failed reads", program.RetryFailedReads);
            program.menu.AddOption("Exit");

            // Initiate main loop
            program.Run();
        }
    }
}
