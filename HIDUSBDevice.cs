using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace HIDUSBLib
{
   public class DataReceivedEventArgs : EventArgs
   {
      public byte[] Data { get; private set; }

      public DataReceivedEventArgs(byte[] data)
      {
         Data = data;
      }
   }

   public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

   /// <summary>
   /// …
   /// </summary>
   public class HIDUSBDevice : IDisposable
   {
      private bool _bDisposed = false;

      private string _strDevicePath;      // device path
      private UInt32 _uDeviceCount;       // device count

      private int _nByteCount = 0;        // Recieved Bytes

      private Core _core = new Core();

      // Reading thread
      protected Thread _dataReadingThread;

      private const int HIDReportLength = 65;

      /// <summary>
      /// Tries to establish a connection to the device with the given name.
      /// </summary>
      /// <param name="vID">The vendor ID of the USB device.</param>
      /// <param name="pID">The product ID of the USB device.</param>
      public HIDUSBDevice(string vID, string pID)
      {
         // Set vid and pid
         VendorID = vID;
         ProductID = pID;

         // Try to establish connection
         Connect();

         // Create Read Thread
         _dataReadingThread = new Thread(new ThreadStart(ReadDataThread));
      }

      public HIDUSBDevice(string vID) : this(vID, string.Empty) { }

      public event DataReceivedEventHandler DataReceived;

      protected virtual void OnDataReceived(DataReceivedEventArgs e)
      {
         if (DataReceived == null) return;
         DataReceived(this, e);
      }

      /// <summary>
      /// Connects the device.
      /// </summary>
      /// <returns>true if connection is established</returns>
      public bool Connect()
      {
         // SearchDevice
         SearchDevice();

         // Return connection state
         return IsConnected;
      }

      /// <summary>
      /// Searches the device with soecified vendor and product id an connect to it.
      /// </summary>
      /// <returns></returns>
      internal bool SearchDevice()
      {
         //no device found yet
         bool bDeviceFound = false;
         _uDeviceCount = 0;
         _strDevicePath = string.Empty;

         bool bResult = true;
         UInt32 uDeviceCount = 0;

         // Get GUID and handle
         _core.CT_HidGuid();
         _core.SetupDiGetClassDevs();

         // Search the device
         while (bResult)
         {
            // Open device
            bResult = _core.SetupDiEnumDeviceInterfaces(uDeviceCount);

            // Get device path
            _core.SetupDiGetDeviceInterfaceDetail();

            // Check if the device path contains vid and pid
            string deviceID = VendorID + "&" + ProductID;
            if (_core._strDevicePathName.IndexOf(deviceID) > 0)
            {
               // Store device information
               _uDeviceCount = uDeviceCount;
               _strDevicePath = _core._strDevicePathName;
               bDeviceFound = true;

               // Init device
               _core.SetupDiEnumDeviceInterfaces(_uDeviceCount);

               _core.SetupDiGetDeviceInterfaceDetail();

               // Create device handle
               _core.CreateFile(this._strDevicePath);
               break;
            }
            ++uDeviceCount;
         }

         _core.SetupDiDestroyDeviceInfoList();

         IsConnected = bDeviceFound;
         return IsConnected;
      }

      public bool Write(byte[] data)
      {
         int nByteCount = data.Length;
         int nPos = 0;

         bool bSuccess = true;

         // Build 64 byte hid reports
         while (nPos < nByteCount)
         {
            byte[] chunk = new byte[HIDReportLength];
            for (int i = 0; i < HIDReportLength; i++)
            {
               if (nPos < nByteCount)
               {
                  chunk[i] = data[nPos++];
               }
               else
               {
                  chunk[i] = 0;
               }
            }

            // send the report
            bSuccess = WriteData(chunk);

            Thread.Sleep(5);
         }
         return bSuccess;
      }

      /// <summary>
      /// Writes data.
      /// </summary>
      /// <param name="data">The data to write.</param>
      /// <returns></returns>
      internal bool WriteData(byte[] data)
      {
         bool bSuccess = false;

         if (IsConnected)
         {
            // Set the size of the Output report buffer.
            byte[] outputReportBuffer = new byte[HIDReportLength];
            Array.Clear(outputReportBuffer, 0, outputReportBuffer.Length);

            // Store the report data following the report ID.
            Array.ConstrainedCopy(data, 0, outputReportBuffer, 1, outputReportBuffer.Length);

            OutputReport report = new OutputReport();
            bSuccess = report.Write(outputReportBuffer, _core._hHid);
         }
         else
         {
            bSuccess = false;
         }
         return bSuccess;
      }

      /// <summary>
      ///  ThreadMethod for reading Data
      /// </summary>
      internal void ReadDataThread()
      {
         IntPtr pPreparsedData;

         do
         {
            pPreparsedData = IntPtr.Zero;

            if (_core.HidD_GetPreparsedData(ref pPreparsedData))
            {
               int code = _core.HidP_GetCaps(pPreparsedData);
               int reportLength = _core._hidpCaps.InputReportByteLength;

               do
               {
                  // Read data
                  byte[] dataReceived = _core.ReadFile(_core._hidpCaps.InputReportByteLength);
                  if (dataReceived != null)
                  {
                     _nByteCount += dataReceived.Length;

                     // Send received data to event listeners
                     OnDataReceived(new DataReceivedEventArgs(dataReceived));
                  }
               } while (true);
            }
         } while (true);
      }

      public void StartReading()
      {
         if (_dataReadingThread.ThreadState == System.Threading.ThreadState.Unstarted)
         {
            _dataReadingThread.Start();
         }
      }

      public void StopReading()
      {
         if (_dataReadingThread.ThreadState == System.Threading.ThreadState.Running)
         {
            _dataReadingThread.Abort();
         }
      }

      /// <summary>
      /// Disconnects the device. DOES NOT WORK (hangs most of the time.)
      /// </summary>
      public void Disconnect()
      {
         if (IsConnected)
         {
            _core.CloseHandle();
            IsConnected = false;
         }
      }

      /// <summary>
      /// Vendor ID.
      /// </summary>
      public string VendorID { get; private set; }

      /// <summary>
      /// Gets the product ID.
      /// </summary>
      /// <returns>the product ID</returns>
      public string ProductID { get; private set; }

      /// <summary>
      /// State of the connection.
      /// </summary>      
      public bool IsConnected { get; set; }

      /// <summary>
      /// Gets the devices found.
      /// </summary>

      public string[] Devices
      {
         get
         {
            List<string> devices = new List<string>();

            this._uDeviceCount = 0;
            this._strDevicePath = string.Empty;

            bool bResult = true;
            UInt32 uDeviceCount = 0;
            UInt32 uNumberOfDevices = 0;

            // Get GUID and handle
            _core.CT_HidGuid();
            _core.SetupDiGetClassDevs();

            // Search the device until you have found it or no more devices in list
            while (bResult)
            {
               // Open the device
               bResult = _core.SetupDiEnumDeviceInterfaces(uDeviceCount);

               // Get device path
               _core.SetupDiGetDeviceInterfaceDetail();

               // Is this the correct device?
               string deviceID = VendorID;
               if (_core._strDevicePathName.IndexOf(deviceID) > 0)
               {
                  devices.Add(_core._strDevicePathName);
                  uNumberOfDevices++;
               }

               uDeviceCount++;
            }

            _core.SetupDiDestroyDeviceInfoList();

            return devices.ToArray();
         }
      }

      internal abstract class HostReport
      {
         protected abstract bool ProtectedWrite(IntPtr deviceHandle, byte[] reportBuffer);

         internal bool Write(byte[] reportBuffer, IntPtr deviceHandle)
         {
            bool bSuccess = false;

            try
            {
               bSuccess = ProtectedWrite(deviceHandle, reportBuffer);
            }
            catch { }

            return bSuccess;
         }
      }

      internal class OutputReport : HostReport
      {
         // For Output reports the host sends to the device.
         // Uses interrupt or control transfers depending on the device and OS.

         protected override bool ProtectedWrite(IntPtr hidHandle, byte[] outputReportBuffer)
         {
            UInt32 uNumberOfBytesWritten = 0;
            IntPtr hResult;
            bool bSuccess = false;

            try
            {
               // The host will use an interrupt transfer if the the HID has an interrupt OUT
               // endpoint (requires USB 1.1 or later) AND the OS is NOT Windows 98 Gold (original version).
               // Otherwise the the host will use a control transfer.
               // The application doesn't have to know or care which type of transfer is used.
               hResult = Core.WriteFile(hidHandle, outputReportBuffer, (UInt32) outputReportBuffer.Length, out uNumberOfBytesWritten, IntPtr.Zero);

               bSuccess = (hResult.ToInt32() == 0) ? false : true;
            }
            catch {}

            return bSuccess;
         }

      }

      #region IDisposable Members

      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected void Dispose(bool disposeManagedResources)
      {
         if (!this._bDisposed)
         {
            if (_core._hHid != (IntPtr)Core.INVALID_HANDLE_VALUE) _core.CloseHandle();
            this._bDisposed = true;
         }
      }

      #endregion
   }
}
