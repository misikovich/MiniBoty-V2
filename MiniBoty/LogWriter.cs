using System;
using System.IO;
using System.Reflection;

namespace MiniBoty
{
    class LogWriter
    {
        private string m_exePath = string.Empty;
        private string _filename;
        public LogWriter(string filename)
        {
            _filename = filename;
        }
        public void LogWrite(object[] logMessage)
        {
            m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                using (StreamWriter w = File.AppendText(m_exePath + "\\" + _filename))
                {
                    Log(logMessage, w);
                }
            }
            catch (Exception)
            {
            }
        }
        public void Log(object[] buffer, TextWriter txtWriter)
        {
            if ((bool)buffer[0])
            {
                try
                {
                    txtWriter.Write("\r\nLog Entry : ");
                    txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                        DateTime.Now.ToLongDateString());
                    txtWriter.WriteLine("Punish: " + buffer[6] + " " + buffer[7]);
                    txtWriter.WriteLine("Username: " + buffer[1]);
                    txtWriter.WriteLine("Message: " + buffer[2]);
                    txtWriter.WriteLine("Reason: " + buffer[3]);
                    txtWriter.WriteLine("Detailed: " + buffer[4]);
                    txtWriter.WriteLine("-------------------------------");
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
