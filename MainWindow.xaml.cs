using System.Text;
using System.Windows;
using System;
using System.IO;
using System.IO.Ports;
using IOPath = System.IO.Path;
using System.Windows.Threading;

namespace ComApp
{
    public partial class MainWindow : Window    
    {
        public static MainWindow Instance { get; private set; } = new MainWindow();
        private static bool isRunning = true;

        private static readonly int DATA_LENTH = 77;
        private readonly DispatcherTimer sendTimer;
        private static Thread? processingThread;
        public static Queue<byte[]> dataQueue = new(); // Hàng đợi chứa dòng data
        public static object queueLock = new(); // Khóa hàng đợi

        private List<byte> receiveBuffer = new List<byte>(); // Data buffer
        


        public MainWindow()
        {
            InitializeComponent();
            Instance = this;        //Get all components of mainWindow to Instance

            Common.CreatLogFolder();
            ComPort.LoadPort();

            // Init timer for sending data cyclic
            sendTimer = new DispatcherTimer();
            sendTimer.Tick += SendTimer_Tick;
            Task.Run(() => ProcessQueueAsync());
        }
        private static async Task ProcessQueueAsync()      // Get data from theard then process data
        {
            while (isRunning)
            {
                byte[]? dataToProcess = null;

                lock (queueLock)
                {
                    if (dataQueue.Count > 0)
                    {
                        dataToProcess = dataQueue.Dequeue();
                    }
                }

                if (dataToProcess != null)
                {
                    // Convert byte to hex string to display
                    string hexString = BitConverter.ToString(dataToProcess).Replace("-", " ");

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MainWindow.Instance.tbDataReceiver.AppendText(hexString + Environment.NewLine);
                        MainWindow.Instance.tbDataReceiver.ScrollToEnd();
                    });

                    Common.SaveDataToCSV(dataToProcess);
                }

                await Task.Delay(1);
            }
        }
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)      // Get data from Com port then save to Queue
        {
            try
            {
                int bytesToRead = ComPort.serialPort.BytesToRead;
                byte[] buffer = new byte[bytesToRead];
                ComPort.serialPort.Read(buffer, 0, bytesToRead);

                lock (queueLock)
                {
                    receiveBuffer.AddRange(buffer); // Add data to Buffer
                }
                while (receiveBuffer.Count >= DATA_LENTH)
                {
                    byte[] dataChunk;
                    lock (queueLock)
                    {
                        dataChunk = receiveBuffer.GetRange(0, DATA_LENTH).ToArray(); // Get bytes from buffer then put to queue
                        receiveBuffer.RemoveRange(0, DATA_LENTH); // Clear queue
                    }

                    lock (queueLock)
                    {
                        dataQueue.Enqueue(dataChunk); // Put data to queue as byte[]
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Can't get data: " + ex.Message));
            }
        }
        

        private void Window_Closed(object sender, EventArgs e)          // Close window event
        {
            isRunning = false;      // Stop geting  data from Com port
            processingThread?.Join();
            ComPort.serialPort?.Close();
        }
        
        private void BtConnect_Click(object sender, RoutedEventArgs e)      // Button Connect is click
        {
            if (!ComPort.serialPort.IsOpen)
            {
                try
                {
                    ComPort.serialPort.PortName = cbPorts.SelectedItem.ToString();
                    if (int.TryParse(cbBaudRate.SelectedItem.ToString(), out int baudRate)) ComPort.serialPort.BaudRate = baudRate;
                    ComPort.serialPort.DataReceived += SerialPort_DataReceived;
                    ComPort.serialPort.Open();
                    btConnect.Content = "DisConnect";

                    // Get timeStamd
                    Common.timeStamd = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Can't connect to comport: " + ex.Message);
                }
            }
            else
            {
                ComPort.serialPort.Close();
                ComPort.serialPort.DataReceived -= SerialPort_DataReceived;
                btConnect.Content = "Connect";
            }
        }

        private void BtSend_Click(object sender, RoutedEventArgs e)             // Button Send is click
        {
            if (ComPort.serialPort.IsOpen)
            {
                ComPort.serialPort.WriteLine(tbData.Text);
            }
            else
            {
                MessageBox.Show("Can't send data due to Com port isn't opened.");
            }
        }
        private void BtnClearData_Click(object sender, RoutedEventArgs e)       // Button Clear data is click
        {
            tbDataReceiver.Clear();
        }

        private void CbAutoSend_Checked(object sender, RoutedEventArgs e)       // Auto send is update
        {
            if (int.TryParse(tbCycleTime.Text, out int interval) && interval > 0)
            {
                sendTimer.Interval = TimeSpan.FromMilliseconds(interval);
                sendTimer.Start();  // Start timer to send cyclic data
            }
            else
            {
                MessageBox.Show("Cyclic must be interger");
                cbAutoSend.IsChecked = false;
            }
        }
        private void CbAutoSend_Unchecked(object sender, RoutedEventArgs e)
        {
            sendTimer.Stop();
        }

        private void SendTimer_Tick(object? sender, EventArgs e)        // Cyclic send data to com port
        {
            if (ComPort.serialPort.IsOpen)
            {
                string dataToSend = tbData.Text;

                if (!string.IsNullOrEmpty(dataToSend))
                {
                    ComPort.serialPort.WriteLine(dataToSend);
                }
            }
            else
            {
                MessageBox.Show("Com port isn't open");
            }
        }

        private void TbCycleTime_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (cbAutoSend != null && cbAutoSend.IsChecked == true)
            {
                if (int.TryParse(tbCycleTime.Text, out int interval) && interval > 0)
                {
                    sendTimer.Interval = TimeSpan.FromMilliseconds(interval);
                }
            }
        }
    }
}
