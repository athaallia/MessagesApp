using System;
using System.Windows.Forms;

namespace ChatClient
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new ChatForm());
        }
    }
}
