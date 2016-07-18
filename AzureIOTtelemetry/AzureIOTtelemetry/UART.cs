using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;
using System.Threading;
using Windows.Storage.Streams;

namespace AzureIOTtelemetry
{
    class UART
    {
        private SerialDevice uart = null;
        private CancellationTokenSource ReadCancellationTokenSource;
        private bool portOpen = false;
        DataReader dataReaderObject = null;
        private UARTstatus portStatus = UARTstatus.Closed;
        public string portMessage;
        private string message;

        public UART()
        {

        }

        public int Connected
        {
            get
            {
                throw new System.NotImplementedException();
            }

            set
            {
            }
        }

        public async void Connect()
        {
            try
            {
                string availableSerialDevices = SerialDevice.GetDeviceSelector();
                var asyncDevices = await DeviceInformation.FindAllAsync(availableSerialDevices);
                DeviceInformation entry = asyncDevices[0];

                uart = await SerialDevice.FromIdAsync(entry.Id);
                uart.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                uart.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                uart.BaudRate = 57600; // Need to parametise
                uart.Parity = SerialParity.None;
                uart.StopBits = SerialStopBitCount.One;
                uart.DataBits = 8;
                uart.Handshake = SerialHandshake.None;
                portOpen = true;
                portStatus = UARTstatus.Open;
            }
            catch (Exception ex)
            {
                portMessage = ex.Message;
            }
        }
        public async void Listen()
        {
            try
            {
                if(uart != null)
                {
                    dataReaderObject = new DataReader(uart.InputStream);
                    while (true)
                    {
                        await ReadAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                if(ex.GetType().Name == "TaskCanceledException")
                {
                    CloseDevice();
                }
                else
                {
                    portMessage = ex.Message;
                }
            }
            finally
            {
                if(dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }

        private void CloseDevice()
        {
            if(uart != null)
            {
                uart.Dispose();
            }
            uart = null;
            portStatus = UARTstatus.Closed;
        }

        private async Task ReadAsync(CancellationToken token)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 1024;

            // If task cancellation was requested, comply
            token.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

            // Create a task object to wait for data on the serialPort.InputStream
            loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(token);

            // Launch the task and wait
            UInt32 bytesRead = await loadAsyncTask;
            if (bytesRead > 0)
            {
                message = dataReaderObject.ReadString(bytesRead);
                portStatus = UARTstatus.Read;
            }
        }

        public void SetBaud()
        {
            throw new System.NotImplementedException();
        }

        public void SetPort()
        {
            throw new System.NotImplementedException();
        }
    }

    public enum UARTstatus
    {
        Closed,
        Open,
        Error,
        Read
    }
}
