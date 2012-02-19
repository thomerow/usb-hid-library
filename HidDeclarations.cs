using System;
using System.Runtime.InteropServices;

namespace HIDUSBLib
{
   // API Declarations for communicating with HID-class devices.

   // From hidpi.h
   // Defines a set of integer constants for HidP_Report_Type
   internal enum HIDP_REPORT_TYPE : short
   {
      HidP_Input = 0,
      HidP_Output = 1,
      HidP_Feature = 2,
   }

   // Structures and classes for API calls

   /// <summary>
   /// The HIDD_ATTRIBUTES structure contains vendor information about a HIDClass device.
   /// </summary>
   [StructLayout(LayoutKind.Sequential, Pack = 1)]
   internal struct HIDD_ATTRIBUTES
   {
      public int Size;
      public short VendorID;
      public short ProductID;
      public short VersionNumber;
   }

   /// <summary>
   /// The HIDP_CAPS structure contains information about a top-level collection's capability.
   /// </summary>
   [StructLayout(LayoutKind.Sequential, Pack = 1)]
   internal struct HIDP_CAPS
   {
      public UInt16 Usage;
      public UInt16 UsagePage;
      public UInt16 InputReportByteLength;
      public UInt16 OutputReportByteLength;
      public UInt16 FeatureReportByteLength;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
      public UInt16[] Reserved;
      public UInt16 NumberLinkCollectionNodes;
      public UInt16 NumberInputButtonCaps;
      public UInt16 NumberInputValueCaps;
      public UInt16 NumberInputDataIndices;
      public UInt16 NumberOutputButtonCaps;
      public UInt16 NumberOutputValueCaps;
      public UInt16 NumberOutputDataIndices;
      public UInt16 NumberFeatureButtonCaps;
      public UInt16 NumberFeatureValueCaps;
      public UInt16 NumberFeatureDataIndices;
   }

   // Device interface data
   [StructLayout(LayoutKind.Sequential, Pack = 1)]
   internal struct SP_DEVICE_INTERFACE_DATA
   {
      public int cbSize;
      public Core.GUID InterfaceClassGuid;
      public int Flags;
      public int Reserved;
   }

   // Device interface detail data
   [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
   internal struct SP_DEVICE_INTERFACE_DETAIL_DATA
   {
      public UInt32 cbSize;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
      public string DevicePath;
   }

   sealed class HidApiDecl
   {
      // API functions

      [DllImport("hid.dll")]
      static public extern bool HidD_GetAttributes(int HidDeviceObject, ref HIDD_ATTRIBUTES Attributes);

      [DllImport("hid.dll")]
      static public extern void HidD_GetHidGuid(ref Core.GUID HidGuid);

      [DllImport("hid.dll")]
      static public extern bool HidD_GetPreparsedData(IntPtr HidDeviceObject, ref IntPtr pPreparsedData);

      [DllImport("hid.dll")]
      static public extern bool HidD_SetOutputReport(int HidDeviceObject, ref byte lpReportBuffer, int ReportBufferLength);

      [DllImport("hid.dll")]
      static public extern int HidP_GetCaps(IntPtr pPreparsedData, ref HIDP_CAPS Capabilities);
   }
}
