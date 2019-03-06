/*
BSD 2-Clause License

Copyright (c) 2019, LunaticSoftware.org, Email: phil@lunaticsoftware.org
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
*/

using ASCOM;
using ASCOM.Utilities.Exceptions;
using ASCOM.LunaticAstroEQ.Controller;
using ASCOM.LunaticAstroEQ.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TA.Ascom.ReactiveCommunications;
using ASCOM.LunaticAstroEQ.Core.Geometry;
using CoreConstants = ASCOM.LunaticAstroEQ.Core.Constants;
using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using ASCOM.Utilities;

namespace ASCOM.LunaticAstroEQ.Controller
{
    internal class FFlags
    {
        internal const int Initialised = 0x001;
        internal const int Running = 0x010;
        internal const int Slewing = 0x100;
        internal const int Reversed = 0x200;
        internal const int HighSpeed = 0x400;
    }

    /// <summary>
    /// Skeleton of a hardware class, all this does is hold a count of the connections,
    /// in reality extra code will be needed to handle the hardware in some way
    /// </summary>
    [ComVisible(false)]
    public partial class AstroEQController
    {
        #region Private member variables ...
        // protected static SerialConnection mConnection = null;    // Now using serialPort
        protected long MCVersion = 0;   // Motor controller version number

        /// ************ Motion control related **********************
        /// They are variables represent the mount's status, but not grantee always updated.
        /// 1) The AxisPosition is updated with MCGetAxisPosition and MCSetAxisPosition
        /// 2) The TargetPosition is updated with MCAxisSlewTo        
        /// 3) The SlewingSpeed is updated with MCAxisSlew
        /// 4) The AxesStatus is updated updated with MCGetAxisStatus, MCAxisSlewTo, MCAxisSlew
        /// Notes:
        /// 1. Positions may not represent the mount's position while it is slewing, or user manually update by hand

        private double[] _SlewingSpeed = new double[2] { 0, 0 };        // Operating speed in radians per second                
        private AxisState[] _AxisState = new AxisState[2];           // The two-axis status of the carriage should be referenced by AxesStatus[AXIS1] and AxesStatus[AXIS2]
        private long[] GridPerRevolution = new long[2];                  // Number of steps for 360 degree

        // special charactor for communication.
        const char cStartChar_Out = ':';       // Leading charactor of a command 
        const char cStartChar_In = '=';        // Leading charactor of a NORMAL response.
        const char cErrChar = '!';             // Leading charactor of an ABNORMAL response.
        const char cEndChar = (char)13;        // Tailing charactor of command and response.

        public const double LOW_SPEED_MARGIN_RADIANS = (128.0 * CoreConstants.SIDEREAL_RATE_RADIANS);
        public const double LOW_SPEED_MARGIN_ARCSECS = (128.0 * CoreConstants.SIDEREAL_RATE_ARCSECS);

        /// <summary>
        /// Maximum error allowed when comparing Axis positions in radians (roughly 0.5 seconds)
        /// </summary>
        private const double AXIS_ERROR_TOLERANCE = 3.5E-5;


        //private char dir = '0'; // direction
        //                        // Mount code: 0x00=EQ6, 0x01=HEQ5, 0x02=EQ5, 0x03=EQ3
        //                        //             0x80=GT,  0x81=MF,   0x82=114GT
        //                        //             0x90=DOB
        private long MountCode;
        private long[] StepTimerFreq = new long[2];        // Frequency of stepping timer.
        private long[] PESteps = new long[2];
        private long[] HighSpeedRatio = new long[2];
        //private long[] StepPosition = new long[2];          // Never Used
        private long[] BreakSteps = new long[2];           // Break steps from slewing to stop.
        private long[] LowSpeedGotoMargin = new long[2];      // If slewing steps exceeds this LowSpeedGotoMargin, 
                                                              // GOTO is in high speed slewing.
        private double[] LowSpeedSlewRate = new double[2];    // Low speed slew rate (Steps/Sec)
        private double[] HighSpeedSlewRate = new double[2];   // High speed slew rate (Steps/sec)

        private System.IO.StreamWriter _dumpFile = null;

        #endregion

        #region Singleton code ...
        private static AstroEQController _Instance = null;

        public static AstroEQController Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new AstroEQController();
                }
                return _Instance;
            }
        }

        #endregion

        #region Settings related stuff ...
        private ISettingsProvider<ControllerSettings> _SettingsManager = null;

        public ISettingsProvider<ControllerSettings> SettingsManager
        {
            get
            {
                if (_SettingsManager == null)
                {
                    _SettingsManager = new SettingsManager();
                }
                return _SettingsManager;
            }
        }

        private ControllerSettings _Settings
        {
            get
            {
                return SettingsManager.Settings;
            }
        }


        private void SaveSettings()
        {
            SettingsManager.SaveSettings();
        }

        #endregion
        /// <summary>
        /// The observatory latitude
        /// </summary>
        public Angle ObservatoryLatitude
        {
            get
            {
                lock (_Settings)
                {
                    return _Settings.ObservatoryLocation.Latitude;
                }
            }
            set
            {
                lock (_Settings)
                {
                    if (value != _Settings.ObservatoryLocation.Latitude)
                    {
                        _Settings.ObservatoryLocation.Latitude = value;
                        SaveSettings();
                    }
                }
            }
        }

        /// <summary>
        /// The observatory longitude
        /// </summary>
        public Angle ObservatoryLongitude
        {
            get
            {
                lock (_Settings)
                {
                    return _Settings.ObservatoryLocation.Longitude;
                }
            }
            set
            {
                lock (_Settings)
                {
                    if (value != _Settings.ObservatoryLocation.Longitude)
                    {
                        _Settings.ObservatoryLocation.Longitude = value;
                        SaveSettings();
                    }
                }
            }
        }


        ///// <summary>
        ///// The observatory geo coordinates
        ///// </summary>
        //public LatLongCoordinate ObservatoryLocation
        //{
        //   get
        //   {
        //      lock (_Settings)
        //      {
        //         return _Settings.ObservatoryLocation;
        //      }
        //   }
        //   set
        //   {
        //      lock (_Settings)
        //      {
        //         if (value != _Settings.ObservatoryLocation)
        //         {
        //            _Settings.ObservatoryLocation = value;
        //            SaveSettings();
        //         }
        //      }
        //   }
        //}

        /// <summary>
        /// The observatory elevation (m)
        /// </summary>
        public double ObservatoryElevation
        {
            get
            {
                lock (_Settings)
                {
                    return _Settings.ObservatoryElevation;
                }
            }
            set
            {
                lock (_Settings)
                {
                    if (value != _Settings.ObservatoryElevation)
                    {
                        _Settings.ObservatoryElevation = value;
                        SaveSettings();
                    }
                }
            }
        }

        private object lockObject = new object();

        /// <summary>
        /// Connection string that is currently being used'
        /// </summary>
        private string ConnectionString;

        /// <summary>
        /// End point for connection to mount.
        /// </summary>
        private DeviceEndpoint EndPoint;

        /// <summary>
        /// Timeout in seconds.
        /// </summary>
        private double TimeOut = 2;

        private int Retry = 1;


        private int OpenConnections;

        private bool ControllerActive;

        public AstroEQController()
        {
            ConnectionString = string.Empty;
            EndPoint = null;

            MCVersion = 0;

            _AxisState[0] = new AxisState { FullStop = false, NotInitialized = true, Slewing = false, SlewingTo = false, MeshedForReverse = false, Tracking = false, TrackingRate = 0.0 };
            _AxisState[1] = new AxisState { FullStop = false, NotInitialized = true, Slewing = false, SlewingTo = false, MeshedForReverse = false, Tracking = false, TrackingRate = 0.0 };

        }

        /// <summary>
        /// Distructor as per IDisposable documentation
        /// </summary>
        //~AstroEQController()
        //{
        //   Dispose(false);
        //}

        #region Connect/Disconnect taken from ASCOM example
        public int Connect(string ComPort, int baud, int timeout, int retry)
        {
            if ((timeout == 0) || (timeout > 50000))
            {
                return Constants.MOUNT_BADPARAM;
            }

            if (retry > 100)
            {
                return Constants.MOUNT_BADPARAM;
            }

            lock (lockObject)
            {
                if (_Settings.CreateDumpFiles)
                {
                    DateTime now = DateTime.Now;
                    // Get the ASCOM trace file path
                    string logPath = $"{ System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\ASCOM\\Logs {now.ToString("yyyy-MM-dd")}";
                    System.IO.Directory.CreateDirectory(logPath);
                    string fileName = $"{logPath}\\{this.GetType().Name}.{now.ToString("HHmm.ffffff")}.dmp";
                    _dumpFile = new StreamWriter(fileName, false);
                }

                int result = MCInit(ComPort, baud, timeout, retry);
                Interlocked.Increment(ref OpenConnections);
                return result;
            }
        }

        public int Disconnect()
        {
            lock (lockObject)
            {
                int result = 0;
                Interlocked.Decrement(ref OpenConnections);
                if (OpenConnections <= 0)
                {
                    if (_dumpFile != null)
                    {
                        _dumpFile.Flush();
                        _dumpFile.Close();
                        _dumpFile = null;
                    }
                    EndPoint = null;
                    ConnectionString = string.Empty;
                    ControllerActive = false;
                }
                SettingsManager.SaveSettings();
                return result;
            }
        }

        public bool IsConnected
        {
            get
            {
                return (EndPoint != null);
            }
        }


        #endregion

        #region Skywatcher_Open code ...

        #region ASCOM serial comms stuff (hopefully can be removed once ReactiveStuff working)

        ///// <summary>
        ///// One communication between mount and client
        ///// </summary>
        ///// <param name="Axis">The target of command</param>
        ///// <param name="Command">The comamnd char set</param>
        ///// <param name="cmdDataStr">The data need to send</param>
        ///// <returns>The response string from mount</returns>
        //protected virtual String TalkWithAxis_ASCOM(AXISID Axis, char Command, string cmdDataStr)
        //{
        //   /// Lock the serial connection
        //   /// It grantee there is only one thread entering this function in one time
        //   /// ref: http://msdn.microsoft.com/en-us/library/ms173179.aspx
        //   /// TODO: handle exception
        //   lock (serialPort)
        //   {
        //      for (int i = 0; i < 2; i++)
        //      {
        //         // prepare to communicate
        //         try
        //         {
        //            serialPort.ClearBuffers();
        //            // send the request
        //            SendRequest(Axis, Command, cmdDataStr);
        //            //Trace.TraceInformation("Send command successful");
        //            // receive the response
        //            return ReceiveResponse();
        //         }
        //         catch (SerialPortInUseException e)
        //         {
        //            Trace.TraceError("Timeout, need Resend the Command");
        //         }
        //         catch (IOException e)
        //         {
        //            Trace.TraceError("Connnection Lost");
        //            throw new DriverException("AstroEQ not responding", e);
        //         }
        //      }
        //      //Trace.TraceError("Timeout, stop send");
        //      if (Axis == AXISID.AXIS1)
        //         throw new DriverException("AstroEQ axis 1 not responding");
        //      else
        //         throw new DriverException("AstroEQ axis 2 not responding");
        //   }

        //}


        //protected void SendRequest(AXISID Axis, char Command, string cmdDataStr)
        //{
        //   if (cmdDataStr == null)
        //      cmdDataStr = "";

        //   const int BufferSize = 20;
        //   StringBuilder CommandStr = new StringBuilder(BufferSize);
        //   CommandStr.Append(cStartChar_Out);                  // 0: Leading char
        //   CommandStr.Append(Command);                         // 1: Length of command( Source, distination, command char, data )

        //   // Target Device
        //   CommandStr.Append(Axis == AXISID.AXIS1 ? '1' : '2');    // 2: Target Axis
        //                                                           // Copy command data to buffer
        //   CommandStr.Append(cmdDataStr);

        //   CommandStr.Append(cEndChar);    // CR Character            

        //   serialPort.Transmit(CommandStr.ToString());
        //}

        //protected string ReceiveResponse()
        //{
        //   //string response = null;
        //   //try
        //   //{
        //   //   // Receive Response
        //   //   response = serialPort.Receive();
        //   //}
        //   //catch (TimeoutException tox)
        //   //{
        //   //   throw new ASCOM.DriverException("Receive timeout error", tox);
        //   //}
        //   //catch { throw; }

        //   //return response;

        //   // format "::e1\r=020883\r"
        //   long startticks = DateTime.Now.Ticks;

        //   StringBuilder mBuffer = new StringBuilder(15);
        //   bool StartReading = false, EndReading = false;

        //   int index = 0;
        //   long interval = 0;
        //   while (!EndReading)
        //   {
        //      index++;
        //      long curticks = DateTime.Now.Ticks;
        //      interval = curticks - startticks;

        //      if ((curticks - startticks) > 10000 * 1000)
        //      {
        //         //Trace.TraceError("Timeout {0} / {1}", mConnection.mBuffer, mBuffer);          
        //         throw new ASCOM.DriverException("Receive timeout error");
        //      }

        //      string r = serialPort.Receive();

        //      for (int i = 0; i < r.Length; i++)
        //      {
        //         // this code order is important
        //         if (r[i] == cStartChar_In || r[i] == cErrChar)
        //            StartReading = true;

        //         if (StartReading)
        //            mBuffer.Append(r[i]);

        //         if (r[i] == cEndChar)
        //         {
        //            if (StartReading)
        //            {
        //               EndReading = true;
        //               break;
        //            }
        //         }
        //      }

        //      Thread.Sleep(1);
        //   }

        //   //Trace.TraceInformation("Loop :" + index.ToString() + "Ticks :" + interval);
        //   return mBuffer.ToString();
        //}

        #endregion


        private bool echoResponse = false;
        /// <summary>
        /// One communication between mount and client
        /// </summary>
        /// <param name="Axis">The target of command</param>
        /// <param name="Command">The comamnd char set</param>
        /// <param name="cmdDataStr">The data need to send</param>
        /// <returns>The response string from mount</returns>
        public String TalkWithAxis(AxisId axis, char cmd, string cmdDataStr)
        {
            string response = string.Empty;

            const int BufferSize = 20;
            StringBuilder sb = new StringBuilder(BufferSize);
            sb.Append(cStartChar_Out);                  // 0: Leading char
            sb.Append(cmd);                         // 1: Length of command( Source, distination, command char, data )

            // Target Device
            sb.Append(((int)axis + 1).ToString());    // 2: Target Axis
                                                      // Copy command data to buffer
            sb.Append(cmdDataStr);

            sb.Append(cEndChar);    // CR Character            

            string cmdString = sb.ToString();
            // echoResponse = false;
            // send the request
            //if ((axis == 0) && ("GHMUIJ".Contains(cmd)))
            //{
            //echoResponse = true;
            //System.Diagnostics.Debug.Write(String.Format("TalkWithAxis({0}, {1}, {2})", axis, cmd, cmdDataStr));
            //}

            if (_dumpFile != null)
            {
                _dumpFile.Write(cmdString);
            }
            var cmdTransaction = new EQTransaction(cmdString) { Timeout = TimeSpan.FromSeconds(TimeOut) };


            using (ICommunicationChannel channel = new SerialCommunicationChannel(EndPoint))
            using (var processor = new ReactiveTransactionProcessor())
            {
                var transactionObserver = new TransactionObserver(channel);
                processor.SubscribeTransactionObserver(transactionObserver);
                try
                {
                    channel.Open();

                    // prepare to communicate
                    for (int i = 0; i < Retry; i++)
                    {

                        Task.Run(() => processor.CommitTransaction(cmdTransaction));
                        cmdTransaction.WaitForCompletionOrTimeout();
                        if (!cmdTransaction.Failed)
                        {
                            response = cmdTransaction.Value.ToString();
                            if (_dumpFile != null)
                            {
                                _dumpFile.WriteLine(response);
                                //System.Diagnostics.Debug.WriteLine($" -> {response}");
                                //echoResponse = false;
                            }
                            break;
                        }
                        else
                        {
                            Trace.TraceError(cmdTransaction.ErrorMessage.Single());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Connnection Lost");
                    throw new DriverException("AstroEQ not responding", ex);
                }
                finally
                {
                    // To clean up, we just need to dispose the TransactionObserver and the channel is closed automatically.
                    // Not strictly necessary, but good practice.
                    transactionObserver.OnCompleted(); // There will be no more transactions.
                    transactionObserver = null; // not necessary, but good practice.
                }

            }
            //if (string.IsNullOrWhiteSpace(response)) {
            //   if (axis == AxisId.Axis1)
            //      throw new MountControllerException(ErrorCode.ERR_NORESPONSE_AXIS1);
            //   else
            //      throw new MountControllerException(ErrorCode.ERR_NORESPONSE_AXIS2);
            //}
            //System.Diagnostics.Debug.WriteLine($" -> Response: {response} (0x{response:X})");
            return response;
        }

        /// <summary>
        /// Send the command to the correct mount
        /// </summary>
        /// <param name="motorId">motor_id (0 RA, 1 DEC)</param>
        /// <param name="command">command (ASCII command to send to mount)</param>
        /// <param name="parameters">parameter (Binary parameter or 0)</param>
        /// <param name="count">count (# parameter bytes)</param>
        /// <returns>Driver Return Value
        ///   -	EQ_OK			0x2000000 - Success with no return values
        ///   -	EQ_COMTIMEOUT	0x1000005 - COM TIMEOUT
        ///   -	EQ_INVALID		0x3000000 - Invalid Parameter</returns>
        /// <remarks></remarks>
        public int EQ_SendCommand(int motorId, char command, int parameters, short count)
        {
            if (motorId == (int)AxisId.Both_Axes)
            {
                return Constants.MOUNT_BADPARAM;
            }
            System.Diagnostics.Debug.Write(String.Format("EQ_SendCommand({0}, {1}, {2}, {3})", motorId, command, Convert.ToString(parameters, 16), count));
            int response = Constants.EQ_OK;
            char[] hex_str = "0123456789ABCDEF     ".ToCharArray();   // Hexadecimal translation
            const int BufferSize = 20;
            StringBuilder sb = new StringBuilder(BufferSize);
            sb.Append(cStartChar_Out);
            sb.Append(command);
            sb.Append((motorId + 1).ToString());
            switch (count)
            {
                case 0:
                    // Do nothing
                    break;
                case 1:
                    // nibble 1
                    sb.Append(hex_str[(parameters & 0x00000f)]);
                    break;
                case 2:
                    // Byte 1
                    sb.Append(hex_str[(parameters & 0x0000f0) >> 4]);
                    sb.Append(hex_str[(parameters & 0x00000f)]);
                    break;
                case 3:
                    // Byte 1
                    sb.Append(hex_str[(parameters & 0x0000f0) >> 4]);
                    sb.Append(hex_str[(parameters & 0x00000f)]);
                    // Nibble 3
                    sb.Append(hex_str[(parameters & 0x000f00) >> 8]);
                    break;
                case 4:
                    // Byte 1
                    sb.Append(hex_str[(parameters & 0x0000f0) >> 4]);
                    sb.Append(hex_str[(parameters & 0x00000f)]);
                    // Byte 2
                    sb.Append(hex_str[(parameters & 0x00f000) >> 12]);
                    sb.Append(hex_str[(parameters & 0x000f00) >> 8]);
                    break;
                case 5:
                    // Byte 1
                    sb.Append(hex_str[(parameters & 0x0000f0) >> 4]);
                    sb.Append(hex_str[(parameters & 0x00000f)]);
                    // Byte 2
                    sb.Append(hex_str[(parameters & 0x00f000) >> 12]);
                    sb.Append(hex_str[(parameters & 0x000f00) >> 8]);
                    // nibble
                    sb.Append(hex_str[(parameters & 0x0f0000) >> 16]);
                    break;
                case 6:
                    // Byte 1
                    sb.Append(hex_str[(parameters & 0x0000f0) >> 4]);
                    sb.Append(hex_str[(parameters & 0x00000f)]);
                    // Byte 2
                    sb.Append(hex_str[(parameters & 0x00f000) >> 12]);
                    sb.Append(hex_str[(parameters & 0x000f00) >> 8]);
                    // Byte 3
                    sb.Append(hex_str[(parameters & 0xf00000) >> 20]);
                    sb.Append(hex_str[(parameters & 0x0f0000) >> 16]);
                    break;
                default:
                    return Constants.EQ_INVALID;
            }
            sb.Append(cEndChar);
            string cmdString = sb.ToString();
            var cmdTransaction = new EQContrlTransaction(cmdString) { Timeout = TimeSpan.FromSeconds(TimeOut) };


            using (ICommunicationChannel channel = new SerialCommunicationChannel(EndPoint))
            using (var processor = new ReactiveTransactionProcessor())
            {
                var transactionObserver = new TransactionObserver(channel);
                processor.SubscribeTransactionObserver(transactionObserver);
                try
                {
                    channel.Open();

                    // prepare to communicate
                    for (int i = 0; i < Retry; i++)
                    {

                        Task.Run(() => processor.CommitTransaction(cmdTransaction));
                        cmdTransaction.WaitForCompletionOrTimeout();
                        if (!cmdTransaction.Failed)
                        {
                            response = cmdTransaction.Value;
                            break;
                        }
                        else
                        {
                            Trace.TraceError(cmdTransaction.ErrorMessage.Single());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Connnection Lost");
                    throw new Exception("Connection lost", ex);
                }
                finally
                {
                    // To clean up, we just need to dispose the TransactionObserver and the channel is closed automatically.
                    // Not strictly necessary, but good practice.
                    transactionObserver.OnCompleted(); // There will be no more transactions.
                    transactionObserver = null; // not necessary, but good practice.
                }

            }

            System.Diagnostics.Debug.WriteLine(string.Format("    -> Response: {0}", Convert.ToString(response, 16)));
            return response;
        }

        public int MCInit(string comportname, int baud, int timeout, int retry)
        {

            int result;
            if (ControllerActive)
            {
                return Constants.MOUNT_COMCONNECTED;
            }

            if ((timeout == 0) || (timeout > 50000))
            {
                return Constants.MOUNT_BADPARAM;
            }

            if (retry > 100)
            {
                return Constants.MOUNT_BADPARAM;
            }

            lock (lockObject)
            {
                try
                {
                    result = Constants.MOUNT_SUCCESS;
                    if (EndPoint == null)
                    {
                        #region Capture connection parameters ...
                        // ConnectionString = string.Format("{0}:{1},None,8,One,DTR,RTS", ComPort, baud);
                        ConnectionString = string.Format("{0}:{1},None,8,One,NoDTR,NoRTS", comportname, baud);
                        EndPoint = SerialDeviceEndpoint.FromConnectionString(ConnectionString);
                        TimeOut = timeout * 0.001;  // Convert from milliseconds to seconds.
                        Retry = retry;
                        #endregion


                        ControllerActive = false;
                        try
                        {
                            // System.Diagnostics.Debug.WriteLine("InquireMotorBoardVersion");
                            InquireMotorBoardVersion(AxisId.Axis1_RA);
                        }
                        catch
                        {
                            // try again
                            System.Threading.Thread.Sleep(200);
                            InquireMotorBoardVersion(AxisId.Axis1_RA);
                        }

                        MountCode = MCVersion & 0xFF;

                        //// NOTE: Simulator settings, Mount dependent Settings

                        // Inquire Gear Rate
                        // System.Diagnostics.Debug.WriteLine("InquireGridPerRevolution");
                        InquireGridPerRevolution(AxisId.Axis1_RA);
                        InquireGridPerRevolution(AxisId.Axis2_Dec);

                        // Inquire motor timer interrup frequency
                        // System.Diagnostics.Debug.WriteLine("InquireTimerInterruptFreq");
                        InquireTimerInterruptFreq(AxisId.Axis1_RA);
                        InquireTimerInterruptFreq(AxisId.Axis2_Dec);

                        // Inquire motor high speed ratio
                        // System.Diagnostics.Debug.WriteLine("InquireHighSpeedRatio");
                        InquireHighSpeedRatio(AxisId.Axis1_RA);
                        InquireHighSpeedRatio(AxisId.Axis2_Dec);

                        // Inquire PEC period
                        // System.Diagnostics.Debug.WriteLine("InquirePECPeriod");
                        InquirePECPeriod(AxisId.Axis1_RA);
                        InquirePECPeriod(AxisId.Axis2_Dec);
                        // System.Diagnostics.Debug.WriteLine($"Raw state 1: {MCGetRawAxisStatus(AxisId.Axis1_RA)}\n");

                        #region Mimic EQMOD ...
                        // ==== This block is here simply because it was sent by EQMOD === //
                        // Gearchange command? Picked from watching USB traffic from EQMOD
                        SetGearChangeHCDetection();   // 'q' command

                        // Set program mode switched 'O' command.
                        MCSetSwitch(AxisId.Axis1_RA, false);
                        // MCSetSwitch(AxisId.Axis2_Dec, false);

                        // ==== This block is here simply because it was sent by EQMOD === //
                        #endregion

                        //// Inquire Axis Position
                        //System.Diagnostics.Debug.WriteLine("MCGetAxisPosition");
                        //_AxisPosition[(int)AXISID.AXIS1] = MCGetAxisPosition(AXISID.AXIS1);
                        //_AxisPosition[(int)AXISID.AXIS2] = MCGetAxisPosition(AXISID.AXIS2);

                        // System.Diagnostics.Debug.WriteLine("InitializeMC");
                        InitializeMC();

                        // These two LowSpeedGotoMargin are calculate from slewing for 5 seconds in 128x sidereal rate
                        LowSpeedGotoMargin[(int)AxisId.Axis1_RA] = (long)(640 * CoreConstants.SIDEREAL_RATE_RADIANS * FactorRadToStep[(int)AxisId.Axis1_RA]);
                        LowSpeedGotoMargin[(int)AxisId.Axis2_Dec] = (long)(640 * CoreConstants.SIDEREAL_RATE_RADIANS * FactorRadToStep[(int)AxisId.Axis2_Dec]);

                        // I think the following are IRQs/Step!
                        LowSpeedSlewRate[(int)AxisId.Axis1_RA] = ((double)StepTimerFreq[(int)AxisId.Axis1_RA] / ((double)GridPerRevolution[(int)AxisId.Axis1_RA] / CoreConstants.SECONDS_PER_SIDERIAL_DAY));
                        LowSpeedSlewRate[(int)AxisId.Axis2_Dec] = ((double)StepTimerFreq[(int)AxisId.Axis2_Dec] / ((double)GridPerRevolution[(int)AxisId.Axis2_Dec] / CoreConstants.SECONDS_PER_SIDERIAL_DAY));
                        HighSpeedSlewRate[(int)AxisId.Axis1_RA] = ((double)HighSpeedRatio[(int)AxisId.Axis1_RA] * ((double)StepTimerFreq[(int)AxisId.Axis1_RA] / ((double)GridPerRevolution[(int)AxisId.Axis1_RA] / CoreConstants.SECONDS_PER_SIDERIAL_DAY)));
                        HighSpeedSlewRate[(int)AxisId.Axis2_Dec] = ((double)HighSpeedRatio[(int)AxisId.Axis2_Dec] * ((double)StepTimerFreq[(int)AxisId.Axis2_Dec] / ((double)GridPerRevolution[(int)AxisId.Axis2_Dec] / CoreConstants.SECONDS_PER_SIDERIAL_DAY)));

                        // Default break steps
                        BreakSteps[(int)AxisId.Axis1_RA] = 3500;
                        BreakSteps[(int)AxisId.Axis2_Dec] = 3500;

                        ControllerActive = true;

                        result = Constants.MOUNT_SUCCESS;
                    }
                    else
                    {
                        result = Constants.MOUNT_COMCONNECTED;
                    }
                }
                catch (Exception ex)
                {
                    result = Constants.MOUNT_COMERROR;
                }
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="speed">degrees per second</param>
        /// <param name="hemisphere"></param>
        public void MCAxisSlew(AxisId axis, double speed, HemisphereOption hemisphere)
        {
            lock (lockObject)
            {
                double maxSlewRateRadians = (_Settings.MaximumSlewRate * CoreConstants.SIDEREAL_RATE_RADIANS);          //?   Radians

                double internalSpeed = speed * CoreConstants.DEG_RAD;
                // Limit maximum speed
                if (internalSpeed > maxSlewRateRadians)                  // 3.4 degrees/sec, 800X sidereal rate, is the highest speed.
                    internalSpeed = maxSlewRateRadians;
                else if (internalSpeed < -maxSlewRateRadians)
                    internalSpeed = -maxSlewRateRadians;

                bool forward = false, highspeed = false;

                // InternalSpeed lower than 1/1000 of sidereal rate?
                if (Math.Abs(internalSpeed) <= CoreConstants.SIDEREAL_RATE_RADIANS / 1000.0)
                {
                    AxisStop(axis);
                    return;
                }

                // Stop motor and set motion mode if necessary.
                PrepareForSlewing(axis, internalSpeed, hemisphere);

                if (internalSpeed > 0.0)
                    forward = true;
                else
                {
                    internalSpeed = -internalSpeed;
                    forward = false;
                }

                // Calculate and set step period. 
                if (internalSpeed > LOW_SPEED_MARGIN_RADIANS)
                {                 // High speed adjustment
                    internalSpeed = internalSpeed / (double)HighSpeedRatio[(int)axis];
                    highspeed = true;
                }
                internalSpeed = 1 / internalSpeed;                    // For using function RadSpeedToInt(), change to unit Senonds/Rad.

                long SpeedInt = RadSpeedToInt(axis, internalSpeed);
                if ((MCVersion == 0x010600) || (MCVersion == 0x010601))  // For special MC version.
                    SpeedInt -= 3;
                if (SpeedInt < 6) SpeedInt = 6;
                SetStepPeriod(axis, SpeedInt);

                // Start motion
                StartMotion(axis);

                _AxisState[(int)axis].SetSlewing(forward, highspeed);
                _SlewingSpeed[(int)axis] = speed;
            }
        }

        public double MCGetSlewTimeEstimate(AxisPosition targetPosition, HemisphereOption hemisphere)
        {
            lock (lockObject)
            {
                double raTime = GetSlewTimeEstimate(AxisId.Axis1_RA, targetPosition.RAAxis.Radians);
                double decTime = GetSlewTimeEstimate(AxisId.Axis2_Dec, targetPosition.DecAxis.Radians);
                return Math.Max(raTime, decTime);
            }
        }

        private double GetSlewTimeEstimate(AxisId axis, double targetPosition)
        {
            int ax = (int)axis;
            var movingSteps = Math.Abs(GetMovingSteps(axis, targetPosition));

            // If there is no increment, return directly.
            if (movingSteps == 0)
            {
                return 0.0;
            }

            long highSteps = 0;
            long lowSteps = 0;
            // Check if the distance is long enough to trigger a high speed GOTO.
            if (movingSteps > LowSpeedGotoMargin[ax])
            {
                lowSteps = BreakSteps[(int)axis];
                highSteps = movingSteps - lowSteps;
            }
            else
            {
                lowSteps = movingSteps;
            }

            double lowTime = lowSteps / LowSpeedSlewRate[ax];
            double highTime = highSteps / HighSpeedSlewRate[ax];
            return highTime + lowTime;
        }


        public void MCAxisSlewTo(AxisPosition targetPosition, HemisphereOption hemisphere)
        {
            lock (lockObject)
            {
                AxisSlewTo(AxisId.Axis1_RA, targetPosition.RAAxis.Radians, hemisphere);
                AxisSlewTo(AxisId.Axis2_Dec, targetPosition.DecAxis.Radians, hemisphere);
            }
        }


        /// <summary>
        /// Slew one axis to a position given in Radians
        /// </summary>
        /// <param name="axis">Axis to slew</param>
        /// <param name="targetPosition">Target position in Radians</param>
        public void MCAxisSlewTo(AxisId axis, double targetPosition, HemisphereOption hemisphere)
        {
            lock (lockObject)
            {
                AxisSlewTo(axis, targetPosition, hemisphere);
            }
        }

        /// <summary>
        /// Slew one axis to a position given in Radians
        /// </summary>
        /// <param name="axis">Axis to slew</param>
        /// <param name="targetPosition">Target position in Radians</param>
        private void AxisSlewTo(AxisId axis, double targetPosition, HemisphereOption hemisphere)
        {
            var movingSteps = GetMovingSteps(axis, targetPosition);

            bool forward = true, highspeed = false;
            AxisDirection direction = AxisDirection.Forward;

            // If there is no increment, return directly.
            if (movingSteps == 0)
            {
                return;
            }

            // Set moving direction
            if (movingSteps < 0)
            {
                direction = AxisDirection.Reverse;
                movingSteps = -movingSteps;
                forward = false;
            }

            // Check if axis stopped
            AxisState axisState = GetAxisState(axis);
            if (!axisState.FullStop)
            {
                AxisStop(axis);
                axisState = GetAxisState(axis);
                while (!axisState.FullStop)
                {
                    Thread.Sleep(100);
                    axisState = GetAxisState(axis);
                }
            }

            // Check if the distance is long enough to trigger a high speed GOTO.
            if (movingSteps > LowSpeedGotoMargin[(int)axis])
            {
                SetMotionMode(axis, hemisphere, AxisMode.Goto, direction, AxisSpeed.HighSpeed);  // high speed GOTO slewing 
                highspeed = true;
            }
            else
            {
                SetMotionMode(axis, hemisphere, AxisMode.Goto, direction, AxisSpeed.LowSpeed);  // low speed GOTO slewing
                highspeed = false;
            }


            SetGotoTargetIncrement(axis, movingSteps);

            SetBreakPointIncrement(axis, BreakSteps[(int)axis]);
            StartMotion(axis);

            // _TargetPosition[(int)Axis] = TargetPosition;
            _AxisState[(int)axis].SetSlewingTo(forward, highspeed);
        }


        private long GetMovingSteps(AxisId axis, double targetPosition)
        {
            // Get current position of the axis.
            var CurPosition = MCGetAxisPosition(axis);
            double movingAngle;
            // If Current Position < 180 and target is < 180 simple move
            if (CurPosition < Math.PI && targetPosition > Math.PI)
            {
                movingAngle = targetPosition - CurPosition - CoreConstants.TWO_PI;
            }
            // If current position >180 and target < 180 must move through zero
            else if (CurPosition > Math.PI && targetPosition < Math.PI)
            {
                movingAngle = CoreConstants.TWO_PI - (CurPosition - targetPosition);
            }
            // If current position < 180 and target > 180 must move through zero
            else
            {
                movingAngle = targetPosition - CurPosition;
            }
            // Calculate slewing distance.
            //// Note: For EQ mount, Positions[AXIS1] is offset( -PI/2 ) adjusted in UpdateAxisPosition().
            //var MovingAngle = TargetPosition - CurPosition;
            //System.Diagnostics.Debug.WriteLine($"Current Position = {Angle.RadiansToDegrees(CurPosition)}");
            //System.Diagnostics.Debug.WriteLine($"Target Position = {Angle.RadiansToDegrees(TargetPosition)}");
            //System.Diagnostics.Debug.WriteLine($"MovingAngle = {Angle.RadiansToDegrees(MovingAngle)}");
            // Convert distance in radian into steps.
            return AngleToStep(axis, movingAngle);
        }


        public void MCAxisSlewBy(AxisId axis, double movingAngle, HemisphereOption hemisphere)
        {
            System.Diagnostics.Debug.WriteLine($"MCAxisSlewBy - {axis}, {movingAngle}");
            lock (lockObject)
            {
                //var MovingAngle = TargetPosition - CurPosition;
                // Convert distance in radian into steps.
                var movingSteps = AngleToStep(axis, movingAngle);

                AxisDirection direction = AxisDirection.Forward;
                bool forward = false, highspeed = false;

                // If there is no increment, return directly.
                if (movingSteps == 0)
                {
                    return;
                }

                // Set moving direction
                if (movingSteps < 0)
                {
                    movingSteps = -movingSteps;
                    direction = AxisDirection.Reverse;
                    forward = false;
                }

                // Check if axis stopped
                AxisState axesstate = GetAxisState(axis);
                if (!axesstate.FullStop)
                {
                    AxisStop(axis);
                    axesstate = GetAxisState(axis);
                    while (!axesstate.FullStop)
                    {
                        Thread.Sleep(100);
                        // Update Mount status, the status of both axes are also updated because _GetMountStatus() includes such operations.
                        axesstate = GetAxisState(axis);
                    }
                }

                // Check if the distance is long enough to trigger a high speed GOTO.
                if (movingSteps > LowSpeedGotoMargin[(int)axis])
                {
                    SetMotionMode(axis, hemisphere, AxisMode.Goto, direction, AxisSpeed.HighSpeed);  // high speed GOTO slewing 
                    highspeed = true;
                }
                else
                {
                    SetMotionMode(axis, hemisphere, AxisMode.Goto, direction, AxisSpeed.LowSpeed);  // low speed GOTO slewing 
                    highspeed = false;
                }

                SetGotoTargetIncrement(axis, movingSteps);
                SetBreakPointIncrement(axis, BreakSteps[(int)axis]);
                StartMotion(axis);

                // _TargetPosition[(int)Axis] = TargetPosition;
                _AxisState[(int)axis].SetSlewingTo(forward, highspeed);
            }
        }

        public void MCAxisSlewBy(Angle[] deltaAngle, HemisphereOption hemisphere)
        {
            lock (lockObject)
            {
                MCAxisSlewBy(AxisId.Axis1_RA, deltaAngle[0].Radians, hemisphere);
                MCAxisSlewBy(AxisId.Axis2_Dec, deltaAngle[1].Radians, hemisphere);
            }
        }

        /// <summary>
        /// Start the axis moving at a given rate of arcsecs/per second.
        /// </summary>
        /// <param name="trackingRate">arcsecs per second</param>
        /// <param name="hemisphere"></param>
        /// <param name="direction"></param>
        public void MCStartTrackingRate(AxisId axis, double trackingRate, HemisphereOption hemisphere, AxisDirection direction)
        {
            lock (lockObject)
            {
                int ax = (int)axis;
                if (Math.Abs(trackingRate) >= LOW_SPEED_MARGIN_ARCSECS)
                {
                    throw new ASCOM.InvalidValueException("Tracking rate is too high for low speed tracking.");
                }

                // LowSpeedSlewRate[0] is the Sidereal step rate so we need to work out the multiplier.
                double lowSpeedMultiplier = CoreConstants.SIDEREAL_RATE_ARCSECS / trackingRate;
                int stepPeriod = (int)(LowSpeedSlewRate[ax] * lowSpeedMultiplier);
                AxisState axisState = GetAxisState(axis);

                // If the axis is changing speed or direction must stop it first
                if (!axisState.FullStop)
                {
                    AxisStop(axis);
                    axisState = GetAxisState(axis);
                    // Wait until the axis stop
                    while (!axisState.FullStop)
                    {
                        Thread.Sleep(100);
                        // Update Mount status, the status of both axes are also updated because _GetMountStatus() includes such operations.
                        axisState = GetAxisState(axis);
                    }

                }



                // Set the motor hemisphere, mode, direction and speed
                SetMotionMode(axis, hemisphere, AxisMode.Slew, direction, AxisSpeed.LowSpeed);

                // Set step period
                SetStepPeriod(axis, stepPeriod);

                // Start RA Motor
                StartMotion(axis);    // 301 -> 201

                _AxisState[ax].SetTracking(true, stepPeriod);
            }
        }


        public void MCAxisStop(AxisId axis)
        {

            lock (lockObject)
            {
                if (axis == AxisId.Both_Axes)
                {
                    AxisStop(AxisId.Axis1_RA);
                    AxisStop(AxisId.Axis2_Dec);
                    return;
                }

                AxisStop(axis);
            }
        }

        private void AxisStop(AxisId axis)
        {
            TalkWithAxis(axis, 'K', null);
            _AxisState[(int)axis].SetStopped();
        }

        public void MCAxisStopAndRelease(AxisId axis)
        {

            lock (lockObject)
            {
                if (axis == AxisId.Both_Axes)
                {
                    MCAxisStopAndRelease(AxisId.Axis1_RA);
                    MCAxisStopAndRelease(AxisId.Axis2_Dec);
                    return;
                }

                TalkWithAxis(axis, 'L', null);
                _AxisState[(int)axis].SetStopped();
            }
        }

        /// <summary>
        /// Sets an axis position
        /// </summary>
        /// <param name="Axis"></param>
        /// <param name="NewValue">The current axis position in radians</param>
        public void MCSetAxisPosition(AxisPosition newPositions)
        {
            lock (lockObject)
            {
                SetAxisPosition(AxisId.Axis1_RA, newPositions.RAAxis.Radians);
                SetAxisPosition(AxisId.Axis2_Dec, newPositions.DecAxis.Radians);
            }
        }

        /// <summary>
        /// Sets an axis position
        /// </summary>
        /// <param name="Axis"></param>
        /// <param name="NewValue">The current axis position in radians</param>
        public void MCSetAxisPosition(AxisId axis, double newValue)
        {
            lock (lockObject)
            {
                SetAxisPosition(axis, newValue);
            }
        }


        private void SetAxisPosition(AxisId Axis, double NewValue)
        {
            long NewStepIndex = AngleToStep(Axis, NewValue);
            NewStepIndex += 0x800000;

            string szCmd = LongTo6BitHEX(NewStepIndex);
            TalkWithAxis(Axis, 'E', szCmd);

            // _AxisPosition[(int)Axis] = NewValue;
        }

        /// <summary>
        /// Returns the current axis position in radians
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public double MCGetAxisPosition(AxisId axis)
        {
            lock (lockObject)
            {
                return GetAxisPosition(axis);
            }
        }

        private double GetAxisPosition(AxisId axis)
        {
            string response = TalkWithAxis(axis, 'j', null);
            long iPosition = BCDstr2long(response);
            iPosition -= 0x00800000;
            return StepToAngle(axis, iPosition);
        }

        public AxisPosition MCGetAxisPositions()
        {
            lock (lockObject)
            {
                return new AxisPosition(GetAxisPosition(AxisId.Axis1_RA), GetAxisPosition(AxisId.Axis2_Dec), false, true);
            }
        }

        public AxisState[] MCGetAxesStates()
        {
            lock (lockObject)
            {
                return new AxisState[] {
            MCGetAxisState(AxisId.Axis1_RA),
            MCGetAxisState(AxisId.Axis2_Dec)
         };
            }
        }

        public AxisState MCGetAxisState(AxisId axis)
        {
            lock (lockObject)
            {
                return GetAxisState(axis);
            }
        }

        private AxisState GetAxisState(AxisId axis)
        {
            int ax = (int)axis;
            var response = TalkWithAxis(axis, 'f', null);
            int state = Convert.ToInt32(response.Substring(1, response.Length - 2), 16);

            if ((state & FFlags.Running) == FFlags.Running)
            {
                _AxisState[ax].FullStop = false;
                // Axis is running
                if ((state & FFlags.Slewing) == FFlags.Slewing)
                {
                    // SLEWing
                    _AxisState[ax].SlewingTo = false;
                    _AxisState[ax].Slewing = true;
                }
                else
                {
                    // GOTOing
                    _AxisState[ax].Slewing = false;
                    _AxisState[ax].SlewingTo = true;
                }
            }
            else
            {
                _AxisState[ax].FullStop = true; // FullStop = 1;	// Axis is fully stop.
                _AxisState[ax].Slewing = false;
                _AxisState[ax].SlewingTo = false;
            }

            if ((state & FFlags.Reversed) == FFlags.Reversed)
            {
                _AxisState[ax].MeshedForReverse = true; // Gears are meshed for reverse running
            }
            else
            {
                _AxisState[ax].MeshedForReverse = false;
            }


            if ((state & FFlags.Initialised) == FFlags.Initialised)
            {
                _AxisState[ax].NotInitialized = false;
            }
            else
            {
                _AxisState[ax].NotInitialized = true;      // MC is not initialized.
            }

            if ((state & FFlags.HighSpeed) == FFlags.HighSpeed)
            {
                _AxisState[ax].HighSpeed = true;
            }
            else
            {
                _AxisState[ax].HighSpeed = false;
            }
            return _AxisState[ax];
        }

        //public long MCGetAxisStatus(AxisId axis)
        //{
        //   long state = 0;
        //   lock (lockObject)
        //   {
        //      var response = TalkWithAxis(axis, 'f', null);
        //      state = BCDstr2long(response);

        //   }
        //   return state;
        //}

        public string MCGetRawAxisStatus(AxisId axis)
        {
            string response = "=!";
            lock (lockObject)
            {
                response = TalkWithAxis(axis, 'f', null);

            }
            return response;
        }


        /// <summary>
        /// Return the maximum slew rates in degrees per second.
        /// This should come from the mount but the highspeedratio 
        /// appears to be incorrect at the time of writing.
        /// </summary>
        /// <returns></returns>
        public double[] MCGetMaxRates()
        {
            return new double[] { _Settings.MaximumSlewRate * CoreConstants.SIDEREAL_RATE_DEGREES, _Settings.MaximumSlewRate * CoreConstants.SIDEREAL_RATE_DEGREES };

        }

        public void MCSetSwitch(AxisId axis, bool OnOff)
        {
            lock (lockObject)
            {
                if (OnOff)
                    TalkWithAxis(axis, 'O', "1");
                else
                    TalkWithAxis(axis, 'O', "0");
            }
        }

        public void MCSetPolarScopeBrightness(long brightness)
        {
            string cmd = LongTo2BitHEX(brightness);

            TalkWithAxis(AxisId.Axis2_Dec, 'V', cmd);
        }

        // Skywaterch Helper function
        protected bool IsHEXChar(char tmpChar)
        {
            return ((tmpChar >= '0') && (tmpChar <= '9')) || ((tmpChar >= 'A') && (tmpChar <= 'F'));
        }

        protected long HEX2Int(char HEX)
        {
            long tmp;
            tmp = HEX - 0x30;
            if (tmp > 9)
                tmp -= 7;
            return tmp;
        }

        protected long BCDstr2long(string str)
        {
            // =020782 => 8521474
            try
            {
                long value = 0;
                for (int i = 1; i + 1 < str.Length; i += 2)
                {
                    value += (long)(int.Parse(str.Substring(i, 2), System.Globalization.NumberStyles.AllowHexSpecifier) * Math.Pow(16, i - 1));
                }

                // if(D)
                // Log.d(TAG,"BCDstr2long " + response + ","+value);
                return value;
            }
            catch (FormatException e)
            {
                throw new ASCOM.DriverException("Parse BCD Failed");
            }
            // return Integer.parseInt(response.substring(0, 2), 16)
            // + Integer.parseInt(response.substring(2, 4), 16) * 256
            // + Integer.parseInt(response.substring(4, 6), 16) * 256 * 256;
        }

        protected string LongTo6BitHEX(long number)
        {
            String A = ((int)number & 0xFF).ToString("X").ToUpper();
            String B = (((int)number & 0xFF00) / 256).ToString("X").ToUpper();
            String C = (((int)number & 0xFF0000) / 256 / 256).ToString("X").ToUpper();

            if (A.Length == 1)
                A = "0" + A;
            if (B.Length == 1)
                B = "0" + B;
            if (C.Length == 1)
                C = "0" + C;
            return A + B + C;
        }

        protected string LongTo2BitHEX(long number)
        {
            String A = ((int)number & 0xFF).ToString("X").ToUpper();

            if (A.Length == 1)
                A = "0" + A;
            return A;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="speed">Speed in radians per second</param>
        /// <param name="hemisphere"></param>
        private void PrepareForSlewing(AxisId axis, double speed, HemisphereOption hemisphere)
        {
            AxisDirection direction = (speed > 0.0 ? AxisDirection.Forward : AxisDirection.Reverse);

            var axesstate = GetAxisState(axis);
            if (!axesstate.FullStop)
            {
                if ((axesstate.SlewingTo) ||                               // GOTO in action
                     (axesstate.HighSpeed) ||                              // Currently high speed slewing
                     (Math.Abs(speed) >= LOW_SPEED_MARGIN_RADIANS) ||               // Will be high speed slewing
                     ((axesstate.MeshedForReverse) && (speed > 0)) ||        // Different direction
                     ((!axesstate.MeshedForReverse) && (speed < 0))          // Different direction
                    )
                {
                    // We need to stop the motor first to change Motion Mode, etc.
                    AxisStop(axis);
                }
                else
                {
                    // Other situatuion, there is no need to set motion mode.
                    return;
                }
            }

            if (direction == AxisDirection.Reverse)
            {
                speed = -speed;                     // Get absolute value of Speed.
            }

            if (speed > LOW_SPEED_MARGIN_RADIANS)
            {
                SetMotionMode(axis, hemisphere, AxisMode.Slew, direction, AxisSpeed.HighSpeed);  // Set HIGH speed slewing mode.
            }
            else
            {
                SetMotionMode(axis, hemisphere, AxisMode.Slew, direction, AxisSpeed.LowSpeed);   // Set LOW speed slewing mode.
            }

        }

        // Convert the arc angle to "step"
        protected double[] FactorRadToStep = new double[] { 0, 0 };     // Multiply the value of the radians by the factor to get the position value of the motor board (24 digits will discard the highest byte)
        protected long AngleToStep(AxisId Axis, double AngleInRad)
        {
            return (long)(AngleInRad * FactorRadToStep[(int)Axis]);
        }

        // Convert "step" to a radian angle
        protected double[] FactorStepToRad = new double[] { 0, 0 };    // Multiply the position value of the motor board (after the symbol problem needs to be processed) by the coefficient to get the radians value.
        protected double StepToAngle(AxisId axis, long steps)
        {
            return steps * FactorStepToRad[(int)axis];
        }

        // Converts the radians/second speed to the integer used to set the speed
        protected double[] FactorRadRateToInt = new double[] { 0, 0 };           // Multiply the value of radians/second by this factor to get a 32-bit integer for the set speed used by the motor board.
        protected long RadSpeedToInt(AxisId Axis, double RateInRad)
        {
            return (long)(RateInRad * FactorRadRateToInt[(int)Axis]);
        }


        /************************ MOTOR COMMAND SET ***************************/
        // Inquire Motor Board Version ":e(*1)", where *1: '1'= CH1, '2'= CH2, '3'= Both.
        protected void InquireMotorBoardVersion(AxisId Axis)
        {
            string response = TalkWithAxis(Axis, 'e', null);

            long tmpMCVersion = BCDstr2long(response);

            MCVersion = ((tmpMCVersion & 0xFF) << 16) | ((tmpMCVersion & 0xFF00)) | ((tmpMCVersion & 0xFF0000) >> 16);

        }
        // Inquire Grid Per Revolution ":a(*2)", where *2: '1'= CH1, '2' = CH2.
        protected void InquireGridPerRevolution(AxisId axis)
        {
            string response = TalkWithAxis(axis, 'a', null);

            long gearRatio = BCDstr2long(response);

            // There is a bug in the earlier version firmware(Before 2.00) of motor controller MC001.
            // Overwrite the GearRatio reported by the MC for 80GT mount and 114GT mount.
            if ((MCVersion & 0x0000FF) == 0x80)
            {
                gearRatio = 0x162B97;      // for 80GT mount
            }
            if ((MCVersion & 0x0000FF) == 0x82)
            {
                gearRatio = 0x205318;      // for 114GT mount
            }
            if (gearRatio == 0)
            {
                gearRatio++;
            }
            GridPerRevolution[(int)axis] = gearRatio;
            FactorRadToStep[(int)axis] = gearRatio / (2 * Math.PI);
            FactorStepToRad[(int)axis] = 2 * Math.PI / gearRatio;
        }
        // Inquire Timer Interrupt Freq ":b1".
        protected void InquireTimerInterruptFreq(AxisId Axis)
        {
            string response = TalkWithAxis(Axis, 'b', null);

            long TimeFreq = BCDstr2long(response);
            StepTimerFreq[(int)Axis] = TimeFreq;

            FactorRadRateToInt[(int)Axis] = (double)(StepTimerFreq[(int)Axis]) / FactorRadToStep[(int)Axis];
        }
        // Inquire high speed ratio ":g(*2)", where *2: '1'= CH1, '2' = CH2.
        protected void InquireHighSpeedRatio(AxisId Axis)
        {
            string response = TalkWithAxis(Axis, 'g', null);

            long highSpeedRatio = BCDstr2long(response);
            HighSpeedRatio[(int)Axis] = highSpeedRatio;
        }
        // Inquire PEC Period ":s(*1)", where *1: '1'= CH1, '2'= CH2, '3'= Both.
        protected void InquirePECPeriod(AxisId Axis)
        {
            string response = TalkWithAxis(Axis, 's', null);

            long PECPeriod = BCDstr2long(response);
            PESteps[(int)Axis] = PECPeriod;
        }


        /// <summary>
        /// This sends a 'q' command as spotted in the EQMOD USB stream.
        /// </summary>
        private void SetGearChangeHCDetection()
        {
            TalkWithAxis(AxisId.Axis1_RA, 'q', "010000");
        }

        // Set initialization done ":F3", where '3'= Both CH1 and CH2.
        protected virtual void InitializeMC()
        {
            TalkWithAxis(AxisId.Axis1_RA, 'F', null);
            TalkWithAxis(AxisId.Axis2_Dec, 'F', null);
        }

        [Flags]
        public enum MotionMode
        {
            Reverse = 0x01,
            Southern = 0x02,
            Slew = 0x04,
            Lowspeed = 0x08,


        }

        protected void SetMotionMode(AxisId axis, HemisphereOption hemisphere, AxisMode mode, AxisDirection direction, AxisSpeed speed)
        {
            byte ch;
            ch = 0;

            // Set Direction bit	(Bit 0)
            if (direction == AxisDirection.Reverse)
            {
                ch |= 0x01;
            }

            // Set Hemisphere bit	(Bit 1)
            if (hemisphere == HemisphereOption.Southern)
            {
                ch |= 0x02;
            }

            // 0 = high speed GOTO mode
            // 1 = low speed SLEW mode
            // 2 = low speed GOTO mode
            // 3 = high speed SLEW mode 

            // Set Mode and speed bits
            if (mode == AxisMode.Goto)
            {
                // For goto the speed bit is set for low speed
                if (speed == AxisSpeed.LowSpeed)
                {
                    ch |= 0x20;
                }
            }
            else
            {
                // slew
                ch |= 0x10;
                // For slews the speed bit is set for high speed.
                if (speed == AxisSpeed.HighSpeed)
                {
                    ch |= 0x20;
                }
            }



            string szCmd = LongTo2BitHEX(ch);
            TalkWithAxis(axis, 'G', szCmd);

        }

        //protected void SetMotionMode(AxisId axis, char func, char direction)
        //{
        //   string szCmd = "" + func + direction;
        //   TalkWithAxis(axis, 'G', szCmd);
        //}



        protected void SetGotoTargetIncrement(AxisId Axis, long StepsCount)
        {
            string cmd = LongTo6BitHEX(StepsCount);

            TalkWithAxis(Axis, 'H', cmd);
        }
        protected void SetBreakPointIncrement(AxisId Axis, long StepsCount)
        {
            string szCmd = LongTo6BitHEX(StepsCount);

            TalkWithAxis(Axis, 'M', szCmd);
        }
        protected void SetBreakSteps(AxisId Axis, long NewBrakeSteps)
        {
            string szCmd = LongTo6BitHEX(NewBrakeSteps);
            TalkWithAxis(Axis, 'U', szCmd);
        }
        protected void SetStepPeriod(AxisId Axis, long StepsCount)
        {
            string szCmd = LongTo6BitHEX(StepsCount);
            TalkWithAxis(Axis, 'I', szCmd);
        }
        protected void StartMotion(AxisId axis)
        {
            TalkWithAxis(axis, 'J', null);
        }

        #endregion

        #region IDisposable
        //private bool disposed = false;

        //public void Dispose()
        //{
        //   Dispose(true);
        //   // This object will be cleaned up by the Dispose method.
        //   // Therefore, you should call GC.SupressFinalize to
        //   // take this object off the finalization queue
        //   // and prevent finalization code for this object
        //   // from executing a second time.
        //   GC.SuppressFinalize(this);
        //}

        //private void Dispose(bool disposing)
        //{
        //   // Check to see if Dispose has already been called.
        //   if (!this.disposed)
        //   {
        //      // If disposing equals true, dispose all managed
        //      // and unmanaged resources.
        //      if (disposing)
        //      {
        //         // Dispose managed resources.
        //         if (serialPort != null)
        //         {
        //            serialPort.Connected = false;
        //            serialPort.Dispose();
        //            serialPort = null;
        //         }
        //      }

        //      // Note disposing has been done.
        //      disposed = true;
        //   }
        //}


        #endregion




    }
}
