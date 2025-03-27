using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using IOPath = System.IO.Path;
using System.Threading.Tasks.Dataflow;
using System.Text;
using System.IO.Ports;

namespace ComApp
{
    public static class Common
    {
        private static string? filePath;
        public static DateTime timeStamd;
        public static void CreatLogFolder()       // Create folder to save log file                                  
        {
            string projectRoot = IOPath.GetFullPath(IOPath.Combine(AppContext.BaseDirectory, @"..\..\.."));
            string folderPath = IOPath.Combine(projectRoot, "Logs");
            filePath = IOPath.Combine(folderPath, "ReceivedData.csv");
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Can't create folder: " + ex.Message));
            }
        }

        public static void SaveDataToCSV(byte[] data) // Save data to csv file
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Can't access to log folder"));
                }
                else
                {
                    using (StreamWriter sw = new(filePath, true, Encoding.UTF8))
                    {
                        DateTime tempTime = GetTimeFromData(data);
                        string timestamp = tempTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        string hexString = BitConverter.ToString(data.Skip(4).ToArray()).Replace("-", " ");
                        sw.WriteLine($"{timestamp},{hexString}");
                    }
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Can't write to csv file: " + ex.Message));
            }
        }

        public static DateTime GetTimeFromData(byte[] data)
        {

            int milliseconds = BitConverter.ToInt32(data, 0);
            DateTime referenceTime = timeStamd;
            return referenceTime.AddMilliseconds(milliseconds);
        }
    }
    public static class ComPort
    {

        public static SerialPort serialPort = new("COM7", 9600, Parity.None, 8, StopBits.One);
        public static void LoadPort()       // Load Com port
        {
            MainWindow.Instance.cbPorts.Items.Clear();
            string[] availablePorts = SerialPort.GetPortNames();

            if (availablePorts.Length == 0)
            {
                for (int i = 1; i <= 20; i++)
                {
                    MainWindow.Instance.cbPorts.Items.Add("COM" + i);
                }
            }
            else
            {
                foreach (string port in availablePorts)
                {
                    MainWindow.Instance.cbPorts.Items.Add(port);
                }
            }
            if (MainWindow.Instance.cbPorts.Items.Count > 0) MainWindow.Instance.cbPorts.SelectedIndex = 0;

            // Load Baudrate
            int[] baudRates = { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600, 1843200 };
            foreach (int baud in baudRates)
            {
                MainWindow.Instance.cbBaudRate.Items.Add(baud);
            }
            MainWindow.Instance.cbBaudRate.SelectedIndex = 11;
        }
        

    }
}
