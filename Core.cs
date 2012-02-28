using System;
using System.Runtime.InteropServices;
using System.IO;

namespace HIDUSBLib
{
   internal class Core
   {
      public IntPtr _hHid = (IntPtr)(INVALID_HANDLE_VALUE);		   // HID device handle
      public string _strDevicePathName = string.Empty;
      public IntPtr _hDevInfo = (IntPtr)(INVALID_HANDLE_VALUE);   // Device infoset handle

      public const UInt32 DIGCF_PRESENT = 0x00000002;
      public const UInt32 DIGCF_DEVICEINTERFACE = 0x00000010;
      public const UInt32 DIGCF_INTERFACEDEVICE = 0x00000010;
      public const UInt32 GENERIC_READ = 0x80000000;
      public const UInt32 GENERIC_WRITE = 0x40000000;
      public const UInt32 FILE_SHARE_READ = 0x00000001;
      public const UInt32 FILE_SHARE_WRITE = 0x00000002;
      public const int OPEN_EXISTING = 3;
      public const int EV_RXFLAG = 0x0002;

      public const int INVALID_HANDLE_VALUE = -1;
      public const int ERROR_INVALID_HANDLE = 6;
      public const int FILE_FLAG_OVERLAPED = 0x40000000;

      private GUID _guid = new GUID();
      public SP_DEVICE_INTERFACE_DATA _deviceInterfaceData;
      public SP_DEVICE_INTERFACE_DETAIL_DATA _deviceInterfaceDetailData;
      public HIDP_CAPS _hidpCaps;

      // GUID structure
      [StructLayout(LayoutKind.Sequential, Pack = 1)]
      internal struct GUID
      {
         public UInt32 Data1;
         public UInt16 Data2;
         public UInt16 Data3;
         [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
         public byte[] data4;
      }

      [DllImport("setupapi.dll", SetLastError = true)]
      static extern IntPtr SetupDiGetClassDevs(
         ref GUID ClassGuid,
         [MarshalAs(UnmanagedType.LPTStr)] string Enumerator,
         IntPtr hwndParent,
         UInt32 Flags
      );

      [DllImport("setupapi.dll", SetLastError = true)]
      static extern bool SetupDiEnumDeviceInterfaces(
         IntPtr DeviceInfoSet,
         IntPtr DeviceInfoData,
         ref GUID lpHidGuid,
         UInt32 MemberIndex,
         ref SP_DEVICE_INTERFACE_DATA lpDeviceInterfaceData
      );

      [DllImport("setupapi.dll", SetLastError = true)]
      static extern bool SetupDiGetDeviceInterfaceDetail(
         IntPtr DeviceInfoSet,
         ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
         ref SP_DEVICE_INTERFACE_DETAIL_DATA DeviceInterfaceDetailData,
         UInt32 DeviceInterfaceDetailDataSize,
         ref UInt32 RequiredSize,
         IntPtr DeviceInfoData
      );

      [DllImport("setupapi.dll", SetLastError = true)]
      static extern bool SetupDiGetDeviceInterfaceDetail(
         IntPtr DeviceInfoSet,
         ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
         IntPtr DeviceInterfaceDetailData,
         UInt32 DeviceInterfaceDetailDataSize,
         ref UInt32 RequiredSize,
         IntPtr DeviceInfoData);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern IntPtr CreateFile(
         string lpFileName,
         UInt32 dwDesiredAccess,
         UInt32 dwShareMode,
         IntPtr lpSecurityAttributes,
         UInt32 dwCreationDisposition,
         UInt32 dwFlagsAndAttributes,
         IntPtr hTemplateFile
      );


      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern bool ReadFile(
         IntPtr hFile,						
         [Out] byte[] lpBuffer,				   
         UInt32 nNumberOfBytesToRead,	
         out UInt32 lpNumberOfBytesRead,	
         IntPtr lpOverlapped
      );

      [DllImport("setupapi.dll", SetLastError = true)]
      static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);


      // File I/O stuff

      [DllImport("kernel32.dll")]
      static public extern int CloseHandle(int hObject);

      [DllImport("kernel32.dll")]
      static public extern IntPtr WriteFile(IntPtr hFile, byte[] lpBuffer, UInt32 nNumberOfBytesToWrite, out UInt32 uNumberOfBytesWritten, IntPtr lpOverlapped);


      // Managed Code wrappers for the DLL Calls

      public void HidD_GetHidGuid()
      {
         HidApiDecl.HidD_GetHidGuid(ref _guid);
      }

      public IntPtr SetupDiGetClassDevs()
      {
         _hDevInfo = SetupDiGetClassDevs(ref _guid, null, IntPtr.Zero, DIGCF_INTERFACEDEVICE | DIGCF_PRESENT);
         return _hDevInfo;
      }

      public bool SetupDiEnumDeviceInterfaces(UInt32 memberIndex)
      {
         _deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();
         _deviceInterfaceData.cbSize = Marshal.SizeOf(_deviceInterfaceData);
         bool bResult = SetupDiEnumDeviceInterfaces(_hDevInfo, IntPtr.Zero, ref _guid, memberIndex, ref _deviceInterfaceData);
         return bResult;
      }

      public bool SetupDiGetDeviceInterfaceDetail()
      {
         UInt32 uDeviceInterfaceDetailDataSize = 0, uRequiredSize = 0;
         _deviceInterfaceDetailData = new SP_DEVICE_INTERFACE_DETAIL_DATA();
         _deviceInterfaceDetailData.cbSize = 5;  // …
         
         SetupDiGetDeviceInterfaceDetail(
            _hDevInfo,
            ref _deviceInterfaceData,
            IntPtr.Zero,
            uDeviceInterfaceDetailDataSize,
            ref uRequiredSize,
            IntPtr.Zero);

         uDeviceInterfaceDetailDataSize = uRequiredSize;

         bool bResult = SetupDiGetDeviceInterfaceDetail(
            _hDevInfo,
            ref _deviceInterfaceData,
            ref _deviceInterfaceDetailData,
            uDeviceInterfaceDetailDataSize,
            ref uRequiredSize,
            IntPtr.Zero);

         _strDevicePathName = _deviceInterfaceDetailData.DevicePath;
         return bResult;
      }

      public int CreateFile(string DeviceName)
      {
         _hHid = CreateFile(
            DeviceName,
            GENERIC_READ | GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            0,
            IntPtr.Zero);

         if (_hHid == (IntPtr)INVALID_HANDLE_VALUE) return 0;
         else return 1;
      }

      public int CloseHandle()
      {
         _hHid = (IntPtr)INVALID_HANDLE_VALUE;
         // return CloseHandle(hObject);  // DOES NOT WORK (hangs most of the time)
         return -1;
      }

      public bool HidD_GetPreparsedData(ref IntPtr pHidpPreparsedData)
      {
         return HidApiDecl.HidD_GetPreparsedData(_hHid, ref pHidpPreparsedData);
      }

      public int HidP_GetCaps(IntPtr pHidpPreparsedData)
      {
         _hidpCaps = new HIDP_CAPS();
         return HidApiDecl.HidP_GetCaps(pHidpPreparsedData, ref _hidpCaps);
      }

      public byte[] ReadFile(UInt32 uInputReportByteLength)
      {
         UInt32 uBytesRead = 0;

         if (_hHid == (IntPtr)INVALID_HANDLE_VALUE) return null;

         byte[] buffer = new byte[uInputReportByteLength];
         if (ReadFile(_hHid, buffer, uInputReportByteLength, out uBytesRead, IntPtr.Zero)) return buffer;
         else return null;
      }

      public bool SetupDiDestroyDeviceInfoList()
      {
         bool bResult = SetupDiDestroyDeviceInfoList(_hDevInfo);
         _hDevInfo = (IntPtr)INVALID_HANDLE_VALUE;
         return bResult;
      }

      public bool HidD_FreePreparsedData(IntPtr pHidpPreparsedData)
      {
         return SetupDiDestroyDeviceInfoList(pHidpPreparsedData);
      }
   }
}
