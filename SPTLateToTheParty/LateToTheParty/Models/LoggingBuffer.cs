using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LateToTheParty.Controllers;

namespace LateToTheParty.Models
{
    internal class LoggingBuffer
    {
        public int MessageLength { get; private set; } = 0;
        public string Path { get; private set; } = "";
        public string FilePrefix { get; private set; } = "";

        private string[] messages = new string[0];
        private int lastIndex = 0;

        public LoggingBuffer()
        {
            
        }

        public LoggingBuffer(int messageLength) : this()
        {
            MessageLength = messageLength;
            messages = new string[MessageLength];
        }

        public LoggingBuffer(int messageLength, string path, string filePrefix) : this(messageLength)
        {
            Path = path;
            FilePrefix = filePrefix;
        }

        public void AddMessage(string message)
        {
            if (MessageLength == 0)
            {
                return;
            }

            if (lastIndex < messages.Length)
            {
                messages[lastIndex++] = message;
                return;
            }

            string[] messagesNew = new string[MessageLength];
            Array.Copy(messages, 1, messagesNew, 0, messages.Length - 1);
            messages = messagesNew;
            messages[messages.Length - 1] = message;
        }

        public string GetAllMessages()
        {
            if ((messages[0] == null) || (messages[0].Length == 0))
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(messages[0]);
            for (int i = 1; i < messages.Length; i++)
            {
                if ((messages[i] == null) || (messages[i].Length == 0))
                {
                    continue;
                }

                sb.Append(Environment.NewLine + messages[i]);
            }

            return sb.ToString();
        }

        public void WriteMessagesToLogFile()
        {
            string allMessages = GetAllMessages();
            if (allMessages.Length == 0)
            {
                LoggingController.LogWarning("There were no messages to write to a log file.");
                return;
            }

            string filename = Path + FilePrefix + "_" + DateTime.Now.ToFileTimeUtc() + ".log";

            try
            {
                if (!Directory.Exists(Path))
                {
                    Directory.CreateDirectory(Path);
                }

                File.WriteAllText(filename, allMessages);

                LoggingController.LogInfo("Recent logging messages written to file");
            }
            catch (Exception e)
            {
                e.Data.Add("Filename", filename);
                e.Data.Add("Messages", MessageLength);
                LoggingController.LogError("Could not write recent logging messages to file.");
                LoggingController.LogError(e.ToString());
            }
        }
    }
}
