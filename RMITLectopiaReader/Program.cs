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
            var menu = new Menu();

            // Print heading
            menu.DisplayHeader();
            Console.WriteLine("Started at {0}", DateTime.Now.ToString());

            // Initialize necessary components
            var reader = new LectopiaReader();
            IProgress<Double> progressCallback = new Progress<Double>(progress =>
            {
                Console.Write("\r");
                Console.Write("{0}% completed.", progress.ToString("#.##"));
            });

            MenuOption selectedOption;

            do
            {
                menu.DisplayOptions();
                Console.WriteLine();
                Console.Write("Please select an option: ");
                // TODO: Validation
                selectedOption = (MenuOption)Convert.ToInt32(Console.ReadLine());

                // Process selected option
                switch (selectedOption)
                {
                    case MenuOption.READ_LISTINGS:
                        Console.WriteLine("Reading listings");
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
    }
}
