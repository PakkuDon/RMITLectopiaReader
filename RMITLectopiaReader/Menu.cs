using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMITLectopiaReader
{
    enum MenuOption
    {
        READ_LISTINGS = 1,
        SEARCH_COURSES,
        DISPLAY_RECORDINGS,
        EXPORT_JSON,
        PROGRAM_STATISTICS,
        EXIT
    }

    class Menu
    {
        // Constants for status/error codes
        private const int INVALID_INPUT = -1;

        // Constructor
        public Menu()
        {
        }

        public void DisplayHeader()
        {
            Console.WriteLine("RMIT Lectopia Reader");
            Console.WriteLine("------------------------");
        }

        public void DisplayOptions()
        {
            Console.WriteLine("1) Read listings");
            Console.WriteLine("2) Search courses");
            Console.WriteLine("3) Display recordings");
            Console.WriteLine("4) Export to .json file");
            Console.WriteLine("5) Display program statistics");
            Console.WriteLine("6) Exit");
        }

        public int GetIntegerInput(String prompt)
        {
            int value = INVALID_INPUT;
            do
            {
                Console.Write(prompt);
                try
                {
                    value = Convert.ToInt32(Console.ReadLine());
                }
                catch (System.FormatException)
                {
                    Console.WriteLine("Input must be numeric. Please try again.");
                }
            } while (value == INVALID_INPUT);

            return value;
        }
    }
}
