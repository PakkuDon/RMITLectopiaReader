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
                    Console.Write("{0}% completed.", progress.ToString("0.00"));
                }
            });
        }

        /// <summary>
        /// Performs the main program loop.
        /// </summary>
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
                    // If non-exit option selected, print heading and 
                    // call relevant method
                    if (option.Method != null)
                    {
                        Console.WriteLine(option.GetBannerText());
                        option.Method();
                    }
                    // Else, end program
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

            // Print exit message
            Console.WriteLine("Program terminating. Press ENTER to exit.");
            Console.ReadLine();
        }

        /// <summary>
        /// Loads recording data for course instancess with IDs between those 
        /// specified by the user. Extracted data is added to model.
        /// </summary>
        void ReadListings()
        {
            int startID;
            int count;
            Boolean validInput = false;
            int initialFailCount = reader.FailedReads.Count();

            // Ask user for start and end points of read operation
            // If user enters empty line, return to menu
            do
            {
                startID = menu.GetIntegerInput("Enter a starting ID: ");
                if (startID == Menu.DEFAULT_OPTION)
                {
                    return;
                }

                count = menu.GetIntegerInput("Enter number of pages to read: ");
                if (count == Menu.DEFAULT_OPTION)
                {
                    return;
                }

                // If input start and end IDs are invalid, print error
                // Else set flag to exit loop
                if (startID < 0)
                {
                    Console.WriteLine("Start ID must be greater than 0.");
                }
                else if (count < 0)
                {
                    Console.WriteLine("Count must be greater than 0.");
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
            List<CourseInstance> courses = reader.ReadListingsInRange(startID, startID + count, progressCallback);
            DateTime endTime = DateTime.Now;

            // Add course information to model
            courses.ForEach(c => model.CourseInstances[c.ID] = c);

            // Print statistics
            Console.WriteLine("Successfully read {0} out of {1} pages.", courses.Count(), count);
            Console.WriteLine("{0} connections timed out.", reader.FailedReads.Count() - initialFailCount);
            Console.WriteLine("Operation took {0}", endTime - startTime);
        }

        /// <summary>
        /// Displays a list of courses containing the search term entered by the user.
        /// </summary>
        void SearchCourses()
        {
            // Prompt user for a search term
            Console.Write("Search term: ");
            String searchTerm = Console.ReadLine();

            // Retrieve list of courses containing the given substring
            var matchingCourses = from c in model.CourseInstances.Values
                                  where c.Name.ToLower().Contains(searchTerm.ToLower())
                                  orderby c.Name
                                  select c;

            // Convert results to formatted strings for paging
            String headerText = String.Format("{0} results found.", matchingCourses.Count());
            List<String> courseResultsText = new List<String>();

            if (matchingCourses.Count() > 0)
            {
                headerText += String.Format("\n{0, -10} | {1, -20}", "ID", "Name");
                foreach (var course in matchingCourses)
                {
                    courseResultsText.Add(String.Format("{0, -10} | {1, -20}", course.ID, course.Name));
                }
            }

            // Display search results
            PageOutput(headerText, courseResultsText);
        }

        /// <summary>
        /// Displays some trivial numbers about the program's current session.
        /// </summary>
        void ProgramStatistics()
        {
            // Display general information
            Console.WriteLine("{0} listings stored in data", model.CourseInstances.Count());
            Console.WriteLine("{0} URLs retained for later re-attempt", reader.FailedReads.Count());
        }

        /// <summary>
        /// Lists details for recordings owned by a given course instance.
        /// </summary>
        void DisplayRecordings()
        {
            // Prompt user to enter a course ID
            int id = menu.GetIntegerInput("Please enter a course ID: ");

            // If user enters empty line, cancel operation
            if (id == Menu.DEFAULT_OPTION)
            {
                return;
            }

            // If reader has a course with the matching ID, 
            // retrieve and display recordings
            if (model.CourseInstances.ContainsKey(id))
            {
                var course = model.CourseInstances[id];
                var recordings = from r in reader.GetRecordings(course, progressCallback)
                                 orderby r.DateRecorded
                                 select r;
                String headerText = "Displaying recordings for " + course.Name + "\n"
                    + String.Format("{0, -10} | {1, -8} | {2, -10} | {3, -10}", 
                    "Date", "Time", "Duration", "Formats available");
                List<String> recordingsText = new List<String>();
                foreach (var recording in recordings)
                {
                    recordingsText.Add(String.Format("{0, -10} | {1, -8} | {2, -10} | {3, -10}",
                        recording.DateRecorded.ToShortDateString(),
                        recording.DateRecorded.ToShortTimeString(),
                        recording.Duration,
                        recording.Formats.Count()));
                }

                PageOutput(headerText, recordingsText);
            }
            // Else, print error message
            else
            {
                Console.WriteLine("Failed to find course instance with matching ID.");
            }
        }

        /// <summary>
        /// Constructs a JSON string based on the contents of the model. JSON string is
        /// then written to some file.
        /// </summary>
        void ExportToJson()
        {
            Console.WriteLine("Writing to data.json...");

            // Prepare output data
            var outputData = new { 
                Courses = model.CourseInstances,
                DateGenerated = DateTime.Now 
            };

            // Write data out to file
            // TODO: Prompt user for filepath to save at
            using (StreamWriter sw = new StreamWriter("data.json"))
            {
                var jsonString = JsonConvert.SerializeObject(outputData);
                sw.Write(jsonString);
            }

            // Print completion message
            Console.WriteLine("File saved.");
        }

        /// <summary>
        /// Attempts to retrieve recording data from previously timed-out 
        /// read attempts.
        /// </summary>
        void RetryFailedReads()
        {
            // If no failed URLs to read, print error and return
            if (reader.FailedReads.Count() == 0)
            {
                Console.WriteLine("No URLs to reattempt.");
            }
            else
            {
                // Perform read operation
                var courses = reader.ReadFailedURLs(progressCallback);

                // Store read data in model
                foreach (var course in courses)
                {
                    model.CourseInstances[course.ID] = course;
                }
            }
        }

        /// <summary>
        /// Display output over a series of 'pages' which the user can browse.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="rows"></param>
        private void PageOutput(String header, List<String> rows)
        {
            var pageSize = 15;
            var pageCount = (int)(Math.Ceiling((decimal)rows.Count() / pageSize));
            var pageNum = 1;
            ConsoleKey key;

            // Pad out row list with empty strings
            while (rows.Count() % pageSize != 0)
            {
                rows.Add(String.Empty);
            }

            // Print rows a page at a time
            // Keep displaying pages until user enters 'Q'
            do
            {
                // Display current page content
                Console.Clear();
                Console.WriteLine(header);
                var currentPage = rows.GetRange((pageNum - 1) * pageSize, pageSize);
                for (var i = 0; i < currentPage.Count(); i++)
                {
                    Console.WriteLine(Truncate(currentPage[i], Console.WindowWidth));
                }

                // Print page number and navigation instructions
                Console.WriteLine("{0} / {1} Up/Down keys to switch pages. Q to exit", pageNum, pageCount);

                // Get next page to switch to
                // Set page number if next number is within bounds
                key = Console.ReadKey().Key;
                if (key == ConsoleKey.UpArrow && pageNum != 1)
                {
                    pageNum--;
                }
                else if (key == ConsoleKey.DownArrow && pageNum < pageCount)
                {
                    pageNum++;
                }
            } while (key != ConsoleKey.Q);

            Console.Clear();
        }

        /// <summary>
        /// Helper function for trimming long Strings.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="length"></param>
        /// <param name="trail"></param>
        /// <returns></returns>
        public String Truncate(String value, int length, String trail = "...")
        {
            if (value.Length >= length)
            {
                value = value.Substring(0, length - trail.Length - 1) + trail;
            }
            return value;
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
