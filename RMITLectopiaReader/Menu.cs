using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMITLectopiaReader
{
    class Menu
    {
        public class MenuOption
        {
            // Properties
            public String Text { get; private set; }
            public Action Method { get; private set; }

            // Constructor
            public MenuOption(String text, Action method = null)
            {
                this.Text = text;
                this.Method = method;
            }
        }

        // Constants for status/error codes
        public const int INVALID_INPUT = -1;
        public const int DEFAULT_OPTION = -2;

        // Instance variables
        public Dictionary<int, MenuOption> Options { get; private set; }

        // Constructor
        public Menu()
        {
            Options = new Dictionary<int, MenuOption>();
        }

        public void DisplayHeader()
        {
            Console.WriteLine("RMIT Lectopia Reader");
            Console.WriteLine("------------------------");
        }

        public void DisplayOptions()
        {
            foreach (var key in Options.Keys)
            {
                Console.WriteLine("{0}: {1}", key, Options[key].Text);
            }
        }

        public void AddOption(String text, Action method = null)
        {
            Options[Options.Count() + 1] = new MenuOption(text, method);
        }

        public int GetIntegerInput(String prompt)
        {
            int value = INVALID_INPUT;
            do
            {
                Console.Write(prompt);
                try
                {
                    String input = Console.ReadLine().Trim();
                    // If empty line entered, return 'cancel' status
                    if (input.Length == 0)
                    {
                        return DEFAULT_OPTION;
                    }
                    value = Convert.ToInt32(input);
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
