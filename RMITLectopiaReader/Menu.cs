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
        FIND_COURSE,
        EXPORT_JSON,
        EXIT
    }

    class Menu
    {
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
            Console.WriteLine("2) Find course");
            Console.WriteLine("3) Export to .json file");
            Console.WriteLine("4) Exit");
        }
    }
}
