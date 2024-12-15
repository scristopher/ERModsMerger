using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Org.BouncyCastle.Crypto.Prng;

namespace ERModsMerger.Core
{
    public class LOG
    {
        DateTime Date;
        private string _message = "";
        public string Message {
            get {  return _message; } 
            set 
            {
                _message = value;
                OnPropertyChanged(this);
            } 
        }

        private LOGTYPE _type = LOGTYPE.INFO;
        public LOGTYPE Type
        {
            get { return _type; }
            set
            {
                _type = value;
                OnPropertyChanged(this);
            }
        }

        private double _progress = 0;
        public double Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                OnPropertyChanged(this);
            }
        }


        private List<LOG> SubLogs { get; set; }

        public LOG(string logMessage, LOGTYPE logType = LOGTYPE.INFO, bool isSubLog = false)
        {
            if (ConsoleOutput)
            {
               
                if (!isSubLog)
                    Console.Write("\n\n");
                else
                    Console.Write("\n");
            }

            Date = DateTime.Now;    
            
            Type = logType;
            Progress = 0;
            Message = logMessage.Replace("\r", "");
            SubLogs = new List<LOG>();

           

            if(!isSubLog)
                OnNewLog(this);
        }

        public LOG AddSubLog(string message, LOGTYPE type = LOGTYPE.INFO)
        {
            var sublog = new LOG(message, type, true);
            SubLogs.Add(sublog);
            OnSubLogAdded(sublog);
            return sublog;
        }

        public delegate void SubLogAddedEventHandler(LOG log);
        public event SubLogAddedEventHandler? SubLogAdded;

        protected void OnSubLogAdded(LOG log)
        {
            if (SubLogAdded != null)
            {
                SubLogAdded(log);
            }
        }

        public delegate void LogPropertyChangedEventHandler(LOG log);
        public event LogPropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(LOG log)
        {
            if(ConsoleOutput)
            {
                //clear the current console line
                int currentLineCursor = Console.CursorTop;
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, currentLineCursor);

                string logMessage = log.Message;

                switch (log.Type)
                {
                    case LOGTYPE.INFO: logMessage = "🛈  " + logMessage; break;
                    case LOGTYPE.ERROR: logMessage = "🔴  " + logMessage; break;
                    case LOGTYPE.WARNING: logMessage = "⚠  " + logMessage; break;
                    case LOGTYPE.SUCCESS: logMessage = "✅  " + logMessage; break;
                    case LOGTYPE.QUERY_USER__YES_NO: logMessage = "❔  " + logMessage; break;
                }


               
                Console.Write("\r" + logMessage);
            }




            if (PropertyChanged != null)
            {
                PropertyChanged(log);
            }
        }


        public delegate void NewLogEventHandler(
        NewLogEventArgs args);
        public static event NewLogEventHandler? NewLog;

        protected static void OnNewLog(LOG log)
        {
            if (NewLog != null)
            {
                NewLog(new NewLogEventArgs(log));
            }
        }

        private static bool _consoleOutput = false;
        public static bool ConsoleOutput {  get { return _consoleOutput; } set { _consoleOutput = value; } }

        private static bool _wpfOutput = false;
        public static bool WpfOutput { get { return _wpfOutput; } set { _wpfOutput = value; } }

        public static List<LOG> lOGs = new List<LOG>();
        
        public static LOG Log(string logMessage, LOGTYPE logType = LOGTYPE.INFO)
        {
            var log = new LOG(logMessage, logType);
            lOGs.Add(log);
            return log;
        }


        public static bool QueryUserYesNoQuestion(string queryMessage)
        {
            bool result = false;
            if(ConsoleOutput)
            {
                Console.WriteLine("❔  " + queryMessage);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n\n<<YES (Press 'Y')>>\t\t<<NO (Press any other key)>>\n\n");

                Console.ResetColor();

                char keyPressed = Console.ReadKey(true).KeyChar;
                if (char.ToUpper(keyPressed) == 'Y')
                    result = true;
                else
                    result = false;

                Log(queryMessage + "  -" + result, LOGTYPE.QUERY_USER__YES_NO);
            }
            else if(WpfOutput)
            {
                var Result = MessageBox.Show(queryMessage, "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (Result == MessageBoxResult.Yes)
                    result = true;


                Log(queryMessage + " - " + result, LOGTYPE.QUERY_USER__YES_NO);
            }

            return result;
        }

    }

    public enum LOGTYPE
    {
        ERROR = 0,
        INFO = 1,
        WARNING = 2,
        SUCCESS = 3,
        QUERY_USER__YES_NO = 4
    }

    public class NewLogEventArgs : EventArgs
    {
        private readonly LOG log;

        public NewLogEventArgs(LOG log)
        {
            this.log = log;
        }

        public LOG Log
        {
            get { return this.log; }
        }
    }

}
