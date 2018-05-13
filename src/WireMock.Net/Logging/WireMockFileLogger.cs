using System;
using System.IO;

namespace WireMock.Logging
{
    /// <summary>
    /// WireMockFileLogger which logs to Console
    /// </summary>
    /// <seealso cref="IWireMockLogger" />
    public class WireMockFileLogger : IWireMockLogger, IDisposable
    {
        bool _console;
        StreamWriter _streamWriter;
        private Object _lock = new Object();

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="logfile">The file to log to.</param>
        /// <param name="console">Should logs also be shown on console</param>
        public WireMockFileLogger(string logfile, bool console = false)
        {
            _streamWriter = null;
            _console = console;
            string folder = Path.GetDirectoryName(logfile);
            if (!Directory.Exists(folder))
            {
                Console.WriteLine(Format("Warn", "Log file folder '{0}' does NOT exist. Will try creating.", folder));
                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch (Exception e)
                {
                    Console.WriteLine(Format("Error", "Failed creation of log file folder '{0}' with Exception: '{1}'", folder, e.Message));
                    return;
                }
            }

            try
            {
                _streamWriter = File.AppendText(logfile);
            }
            catch(Exception e)
            {
                Console.WriteLine(Format("Error", "Failed creation of log file '{0}' with Exception: '{1}'", logfile, e.Message));
            }
        }

        /// <see cref="IWireMockLogger.Debug"/>
        public void Debug(string formatString, params object[] args)
        {   
            WriteLine("Debug", false, formatString, args);
        }

        /// <see cref="IWireMockLogger.Info"/>
        public void Info(string formatString, params object[] args)
        {
            WriteLine("Info", _console, formatString, args);
        }

        /// <see cref="IWireMockLogger.Warn"/>
        public void Warn(string formatString, params object[] args)
        {
            WriteLine("Warn", _console, formatString, args);
        }

        /// <see cref="IWireMockLogger.Error"/>
        public void Error(string formatString, params object[] args)
        {
            WriteLine("Error", _console, formatString, args);
        }

        private void WriteLine(string type, bool doConsole, string formatString, params object[] args)
        {
            if(doConsole)
            {
                Console.WriteLine(Format(type, formatString, args));
            }

            lock (_lock)
            {
                _streamWriter.WriteLine(Format(type, formatString, args));
                _streamWriter.Flush();
            }
        }

        private static string Format(string level, string formatString, params object[] args)
        {
            string message = string.Format(formatString, args);

            return $"{DateTime.UtcNow} [{level}] : {message}";
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _streamWriter.Flush();
#if !NETSTANDARD
                _streamWriter.Close();
#endif
                _streamWriter.Dispose();
            }
        }
    }
}