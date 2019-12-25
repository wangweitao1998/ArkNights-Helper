using System;
using System.IO;

namespace ArkNights {
    class Logger {
        private string logFile;
        private StreamWriter writer;
        private FileStream fileStream = null;

        public Logger(string fileName) {
            logFile = fileName;
            CreateDirectory(logFile);
        }

        public void log(string info) {
            try {
                FileInfo fileInfo = new FileInfo(logFile);
                if (!fileInfo.Exists) {
                    fileStream = fileInfo.Create();
                    writer = new StreamWriter(fileStream);
                }
                else {
                    fileStream = fileInfo.Open(FileMode.Append, FileAccess.Write);
                    writer = new StreamWriter(fileStream);
                }
                writer.WriteLine(DateTime.Now + ": " + info);

            }
            finally {
                if (writer != null) {
                    writer.Close();
                    writer.Dispose();
                    fileStream.Close();
                    fileStream.Dispose();
                }
            }
        }

        public void CreateDirectory(string infoPath) {
            DirectoryInfo directoryInfo = Directory.GetParent(infoPath);
            if (!directoryInfo.Exists) {
                directoryInfo.Create();
            }
        }
    }
}
