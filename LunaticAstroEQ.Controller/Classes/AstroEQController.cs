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

namespace ASCOM.LunaticAstroEQ.Controller
{
   /// <summary>
   /// Skeleton of a hardware class, all this does is hold a count of the connections,
   /// in reality extra code will be needed to handle the hardware in some way
   /// </summary>
   [ComVisible(false)]
   public partial class AstroEQController
   {
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
               _SettingsManager = new SettingsProvider();
            }
            return _SettingsManager;
         }
      }

      private ControllerSettings Settings
      {
         get
         {
            return SettingsManager.Settings;
         }
      }
      #endregion

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

      private AstroEQController()
      {
         ConnectionString = string.Empty;
         EndPoint = null;

         MCVersion = 0;

         Positions[0] = 0;
         Positions[1] = 0;
         TargetPositions[0] = 0;
         TargetPositions[1] = 0;
         SlewingSpeed[0] = 0;
         SlewingSpeed[1] = 0;
         AxesStatus[0] = new AXISSTATUS { FullStop = false, NotInitialized = true, HighSpeed = false, Slewing = false, SlewingForward = false, SlewingTo = false };
         AxesStatus[1] = new AXISSTATUS { FullStop = false, NotInitialized = true, HighSpeed = false, Slewing = false, SlewingForward = false, SlewingTo = false };

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
      // protected static SerialConnection mConnection = null;    // Now using serialPort
      protected long MCVersion = 0;   // 馬達控制器的版本號

      /// ************ Motion control related **********************
      /// They are variables represent the mount's status, but not grantee always updated.        
      /// 1) The Positions are updated with MCGetAxisPosition and MCSetAxisPosition
      /// 2) The TargetPositions are updated with MCAxisSlewTo        
      /// 3) The SlewingSpeed are updated with MCAxisSlew
      /// 4) The AxesStatus are updated updated with MCGetAxisStatus, MCAxisSlewTo, MCAxisSlew
      /// Notes:
      /// 1. Positions may not represent the mount's position while it is slewing, or user manually update by hand
      public double[] Positions = new double[2] { 0, 0 };          // 托架的軸坐標位置，以弧度爲單位
      public double[] TargetPositions = new double[2] { 0, 0 };   // 目標位置，以弧度爲單位
      public double[] SlewingSpeed = new double[2] { 0, 0 };      // 以弧度/秒為單位的運行速度                
      public AXISSTATUS[] AxesStatus = new AXISSTATUS[2];             // 托架的兩軸狀態，應通過AxesStatus[AXIS1]和AxesStatus[AXIS2]引用

      // special charactor for communication.
      const char cStartChar_Out = ':';    // Leading charactor of a command 
      const char cStartChar_In = '=';        // Leading charactor of a NORMAL response.
      const char cErrChar = '!';              // Leading charactor of an ABNORMAL response.
      const char cEndChar = (char)13;         // Tailing charactor of command and response.
      const double MAX_SPEED = 500;           //?
      const double LOW_SPEED_MARGIN = (128.0 * Constants.SIDEREALRATE);

      private char dir = '0'; // direction
                              // Mount code: 0x00=EQ6, 0x01=HEQ5, 0x02=EQ5, 0x03=EQ3
                              //             0x80=GT,  0x81=MF,   0x82=114GT
                              //             0x90=DOB
      private long MountCode;
      private long[] StepTimerFreq = new long[2];        // Frequency of stepping timer.
      private long[] PESteps = new long[2];
      private long[] HighSpeedRatio = new long[2];
      //private long[] StepPosition = new long[2];          // Never Used
      private long[] BreakSteps = new long[2];           // Break steps from slewing to stop.
      private long[] LowSpeedGotoMargin = new long[2];      // If slewing steps exceeds this LowSpeedGotoMargin, 
                                                            // GOTO is in high speed slewing.

      private bool InstantStop;              // Use InstantStop command for MCAxisStop

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

      /// <summary>
      /// One communication between mount and client
      /// </summary>
      /// <param name="Axis">The target of command</param>
      /// <param name="Command">The comamnd char set</param>
      /// <param name="cmdDataStr">The data need to send</param>
      /// <returns>The response string from mount</returns>
      private String TalkWithAxis(AXISID axis, char cmd, string cmdDataStr)
      {
         System.Diagnostics.Debug.Write(String.Format("TalkWithAxis({0}, {1}, {2})", axis, cmd, cmdDataStr));
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
         //string.Format("{0}{1}{2}{3}{4}",
         //cStartChar_Out,
         //command,
         //(int)axis,
         //(cmdDataStr ?? "."),
         //cEndChar);

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
         System.Diagnostics.Debug.WriteLine(string.Format(" -> Response: {0} (0x{0:X})", response));
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
                     InquireMotorBoardVersion(AXISID.AXIS1);
                  }
                  catch
                  {
                     // try again
                     System.Threading.Thread.Sleep(200);
                     InquireMotorBoardVersion(AXISID.AXIS1);
                  }

                  MountCode = MCVersion & 0xFF;

                  //// NOTE: Simulator settings, Mount dependent Settings

                  // Inquire Gear Rate
                  InquireGridPerRevolution(AXISID.AXIS1);
                  InquireGridPerRevolution(AXISID.AXIS2);

                  // Inquire motor timer interrup frequency
                  InquireTimerInterruptFreq(AXISID.AXIS1);
                  InquireTimerInterruptFreq(AXISID.AXIS2);

                  // Inquire motor high speed ratio
                  InquireHighSpeedRatio(AXISID.AXIS1);
                  InquireHighSpeedRatio(AXISID.AXIS2);

                  // Inquire PEC period
                  InquirePECPeriod(AXISID.AXIS1);
                  InquirePECPeriod(AXISID.AXIS2);

                  // Inquire Axis Position
                  Positions[(int)AXISID.AXIS1] = MCGetAxisPosition(AXISID.AXIS1);
                  Positions[(int)AXISID.AXIS2] = MCGetAxisPosition(AXISID.AXIS2);

                  InitializeMC();

                  // These two LowSpeedGotoMargin are calculate from slewing for 5 seconds in 128x sidereal rate
                  LowSpeedGotoMargin[(int)AXISID.AXIS1] = (long)(640 * Constants.SIDEREALRATE * FactorRadToStep[(int)AXISID.AXIS1]);
                  LowSpeedGotoMargin[(int)AXISID.AXIS2] = (long)(640 * Constants.SIDEREALRATE * FactorRadToStep[(int)AXISID.AXIS2]);

                  // Default break steps
                  BreakSteps[(int)AXISID.AXIS1] = 3500;
                  BreakSteps[(int)AXISID.AXIS2] = 3500;

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

      public void MCAxisSlew(AXISID Axis, double Speed)
      {
         // Limit maximum speed
         if (Speed > MAX_SPEED)                  // 3.4 degrees/sec, 800X sidereal rate, is the highest speed.
            Speed = MAX_SPEED;
         else if (Speed < -MAX_SPEED)
            Speed = -MAX_SPEED;

         double InternalSpeed = Speed;
         bool forward = false, highspeed = false;

         // InternalSpeed lower than 1/1000 of sidereal rate?
         if (Math.Abs(InternalSpeed) <= Constants.SIDEREALRATE / 1000.0)
         {
            MCAxisStop(Axis);
            return;
         }

         // Stop motor and set motion mode if necessary.
         PrepareForSlewing(Axis, InternalSpeed);

         if (InternalSpeed > 0.0)
            forward = true;
         else
         {
            InternalSpeed = -InternalSpeed;
            forward = false;
         }

         // TODO: ask the details

         // Calculate and set step period. 
         if (InternalSpeed > LOW_SPEED_MARGIN)
         {                 // High speed adjustment
            InternalSpeed = InternalSpeed / (double)HighSpeedRatio[(int)Axis];
            highspeed = true;
         }
         InternalSpeed = 1 / InternalSpeed;                    // For using function RadSpeedToInt(), change to unit Senonds/Rad.
         long SpeedInt = RadSpeedToInt(Axis, InternalSpeed);
         if ((MCVersion == 0x010600) || (MCVersion == 0x010601))  // For special MC version.
            SpeedInt -= 3;
         if (SpeedInt < 6) SpeedInt = 6;
         SetStepPeriod(Axis, SpeedInt);

         // Start motion
         // if (AxesStatus[Axis] & AXIS_FULL_STOPPED)				// It must be remove for the latest DC motor board.
         StartMotion(Axis);

         AxesStatus[(int)Axis].SetSlewing(forward, highspeed);
         SlewingSpeed[(int)Axis] = Speed;
      }
      public void MCAxisSlewTo(AXISID Axis, double TargetPosition)
      {
         // Get current position of the axis.
         var CurPosition = MCGetAxisPosition(Axis);

         // Calculate slewing distance.
         // Note: For EQ mount, Positions[AXIS1] is offset( -PI/2 ) adjusted in UpdateAxisPosition().
         var MovingAngle = TargetPosition - CurPosition;

         // Convert distance in radian into steps.
         var MovingSteps = AngleToStep(Axis, MovingAngle);

         bool forward = false, highspeed = false;

         // If there is no increment, return directly.
         if (MovingSteps == 0)
         {
            return;
         }

         // Set moving direction
         if (MovingSteps > 0)
         {
            dir = '0';
            forward = true;
         }
         else
         {
            dir = '1';
            MovingSteps = -MovingSteps;
            forward = false;
         }

         // Might need to check whether motor has stopped.

         // Check if the distance is long enough to trigger a high speed GOTO.
         if (MovingSteps > LowSpeedGotoMargin[(int)Axis])
         {
            SetMotionMode(Axis, '0', dir);      // high speed GOTO slewing 
            highspeed = true;
         }
         else
         {
            SetMotionMode(Axis, '2', dir);      // low speed GOTO slewing
            highspeed = false;
         }

         SetGotoTargetIncrement(Axis, MovingSteps);
         SetBreakPointIncrement(Axis, BreakSteps[(int)Axis]);
         StartMotion(Axis);

         TargetPositions[(int)Axis] = TargetPosition;
         AxesStatus[(int)Axis].SetSlewingTo(forward, highspeed);
      }
      public void MCAxisStop(AXISID Axis)
      {
         if (InstantStop)
            TalkWithAxis(Axis, 'L', null);
         else
            TalkWithAxis(Axis, 'K', null);

         AxesStatus[(int)Axis].SetFullStop();
      }
      public void MCSetAxisPosition(AXISID Axis, double NewValue)
      {
         long NewStepIndex = AngleToStep(Axis, NewValue);
         NewStepIndex += 0x800000;

         string szCmd = longTo6BitHEX(NewStepIndex);
         TalkWithAxis(Axis, 'E', szCmd);

         Positions[(int)Axis] = NewValue;
      }
      public double MCGetAxisPosition(AXISID Axis)
      {
         string response = TalkWithAxis(Axis, 'j', null);

         long iPosition = BCDstr2long(response);
         iPosition -= 0x00800000;
         Positions[(int)Axis] = StepToAngle(Axis, iPosition);

         return Positions[(int)Axis];
      }
      public AXISSTATUS MCGetAxisStatus(AXISID Axis)
      {

         var response = TalkWithAxis(Axis, 'f', null);

         if ((response[2] & 0x01) != 0)
         {
            // Axis is running
            if ((response[1] & 0x01) != 0)
               AxesStatus[(int)Axis].Slewing = true;     // Axis in slewing(AstroMisc speed) mode.
            else
               AxesStatus[(int)Axis].SlewingTo = true;      // Axis in SlewingTo mode.
         }
         else
         {
            AxesStatus[(int)Axis].FullStop = true; // FullStop = 1;	// Axis is fully stop.
         }

         if ((response[1] & 0x02) == 0)
            AxesStatus[(int)Axis].SlewingForward = true; // Angle increase = 1;
         else
            AxesStatus[(int)Axis].SlewingForward = false;

         if ((response[1] & 0x04) != 0)
            AxesStatus[(int)Axis].HighSpeed = true; // HighSpeed running mode = 1;
         else
            AxesStatus[(int)Axis].HighSpeed = false;

         if ((response[3] & 1) == 0)
            AxesStatus[(int)Axis].NotInitialized = true; // MC is not initialized.
         else
            AxesStatus[(int)Axis].NotInitialized = false;


         return AxesStatus[(int)Axis];
      }

      public void MCSetSwitch(bool OnOff)
      {
         if (OnOff)
            TalkWithAxis(AXISID.AXIS1, 'O', "1");
         else
            TalkWithAxis(AXISID.AXIS1, 'O', "0");
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
      protected string longTo6BitHEX(long number)
      {
         // 31 -> 0F0000
         String A = ((int)number & 0xFF).ToString("X").ToUpper();
         String B = (((int)number & 0xFF00) / 256).ToString("X").ToUpper();
         String C = (((int)number & 0xFF0000) / 256 / 256).ToString("X").ToUpper();

         if (A.Length == 1)
            A = "0" + A;
         if (B.Length == 1)
            B = "0" + B;
         if (C.Length == 1)
            C = "0" + C;

         // if (D)
         // Log.d(TAG, "longTo6BitHex " + number + "," + A + "," + B + "," + C);

         return A + B + C;
      }

      private void PrepareForSlewing(AXISID Axis, double speed)
      {
         char cDirection;

         var axesstatus = MCGetAxisStatus(Axis);
         if (!axesstatus.FullStop)
         {
            if ((axesstatus.SlewingTo) ||                               // GOTO in action
                 (axesstatus.HighSpeed) ||                              // Currently high speed slewing
                 (Math.Abs(speed) >= LOW_SPEED_MARGIN) ||                                    // Will be high speed slewing
                 ((axesstatus.SlewingForward) && (speed < 0)) ||              // Different direction
                 (!(axesstatus.SlewingForward) && (speed > 0))                // Different direction
                )
            {
               // We need to stop the motor first to change Motion Mode, etc.
               MCAxisStop(Axis);
            }
            else
               // Other situatuion, there is no need to set motion mode.
               return;



            // Wait until the axis stop
            while (true)
            {
               // Update Mount status, the status of both axes are also updated because _GetMountStatus() includes such operations.
               axesstatus = MCGetAxisStatus(Axis);

               // Return if the axis has stopped.
               if (axesstatus.FullStop)
                  break;

               Thread.Sleep(100);

               // If the axis is asked to stop.
               // if ( (!AxesAskedToRun[Axis] && !(MountStatus & MOUNT_TRACKING_ON)) )		// If AXIS1 or AXIS2 is asked to stop or 
               //	return ERR_USER_INTERRUPT;

            }

         }
         if (speed > 0.0)
         {
            cDirection = '0';
         }
         else
         {
            cDirection = '1';
            speed = -speed;                     // Get absolute value of Speed.
         }

         if (speed > LOW_SPEED_MARGIN)
         {
            SetMotionMode(Axis, '3', cDirection);              // Set HIGH speed slewing mode.
         }
         else
            SetMotionMode(Axis, '1', cDirection);              // Set LOW speed slewing mode.

      }

      // Convert the arc angle to "step"
      protected double[] FactorRadToStep = new double[] { 0, 0 };             // Multiply the value of the radians by the factor to get the position value of the motor board (24 digits will discard the highest byte)
      protected long AngleToStep(AXISID Axis, double AngleInRad)
      {
         return (long)(AngleInRad * FactorRadToStep[(int)Axis]);
      }

      // Convert "step" to a radian angle
      protected double[] FactorStepToRad = new double[] { 0, 0 };                 // Multiply the position value of the motor board (after the symbol problem needs to be processed) by the coefficient to get the radians value.
      protected double StepToAngle(AXISID Axis, long Steps)
      {
         return Steps * FactorStepToRad[(int)Axis];
      }

      // Converts the radians/second speed to the integer used to set the speed
      protected double[] FactorRadRateToInt = new double[] { 0, 0 };           // Multiply the value of radians/second by this factor to get a 32-bit integer for the set speed used by the motor board.
      protected long RadSpeedToInt(AXISID Axis, double RateInRad)
      {
         return (long)(RateInRad * FactorRadRateToInt[(int)Axis]);
      }


      /************************ MOTOR COMMAND SET ***************************/
      // Inquire Motor Board Version ":e(*1)", where *1: '1'= CH1, '2'= CH2, '3'= Both.
      protected void InquireMotorBoardVersion(AXISID Axis)
      {
         string response = TalkWithAxis(Axis, 'e', null);

         long tmpMCVersion = BCDstr2long(response);

         MCVersion = ((tmpMCVersion & 0xFF) << 16) | ((tmpMCVersion & 0xFF00)) | ((tmpMCVersion & 0xFF0000) >> 16);

      }
      // Inquire Grid Per Revolution ":a(*2)", where *2: '1'= CH1, '2' = CH2.
      protected void InquireGridPerRevolution(AXISID Axis)
      {
         string response = TalkWithAxis(Axis, 'a', null);

         long GearRatio = BCDstr2long(response);

         // There is a bug in the earlier version firmware(Before 2.00) of motor controller MC001.
         // Overwrite the GearRatio reported by the MC for 80GT mount and 114GT mount.
         if ((MCVersion & 0x0000FF) == 0x80)
         {
            GearRatio = 0x162B97;      // for 80GT mount
         }
         if ((MCVersion & 0x0000FF) == 0x82)
         {
            GearRatio = 0x205318;      // for 114GT mount
         }

         FactorRadToStep[(int)Axis] = GearRatio / (2 * Math.PI);
         FactorStepToRad[(int)Axis] = 2 * Math.PI / GearRatio;
      }
      // Inquire Timer Interrupt Freq ":b1".
      protected void InquireTimerInterruptFreq(AXISID Axis)
      {
         string response = TalkWithAxis(Axis, 'b', null);

         long TimeFreq = BCDstr2long(response);
         StepTimerFreq[(int)Axis] = TimeFreq;

         FactorRadRateToInt[(int)Axis] = (double)(StepTimerFreq[(int)Axis]) / FactorRadToStep[(int)Axis];
      }
      // Inquire high speed ratio ":g(*2)", where *2: '1'= CH1, '2' = CH2.
      protected void InquireHighSpeedRatio(AXISID Axis)
      {
         string response = TalkWithAxis(Axis, 'g', null);

         long highSpeedRatio = BCDstr2long(response);
         HighSpeedRatio[(int)Axis] = highSpeedRatio;
      }
      // Inquire PEC Period ":s(*1)", where *1: '1'= CH1, '2'= CH2, '3'= Both.
      protected void InquirePECPeriod(AXISID Axis)
      {
         string response = TalkWithAxis(Axis, 's', null);

         long PECPeriod = BCDstr2long(response);
         PESteps[(int)Axis] = PECPeriod;
      }
      // Set initialization done ":F3", where '3'= Both CH1 and CH2.
      protected virtual void InitializeMC()
      {
         TalkWithAxis(AXISID.AXIS1, 'F', null);
         TalkWithAxis(AXISID.AXIS2, 'F', null);
      }
      protected void SetMotionMode(AXISID Axis, char func, char direction)
      {
         string szCmd = "" + func + direction;
         TalkWithAxis(Axis, 'G', szCmd);
      }
      protected void SetGotoTargetIncrement(AXISID Axis, long StepsCount)
      {
         string cmd = longTo6BitHEX(StepsCount);

         TalkWithAxis(Axis, 'H', cmd);
      }
      protected void SetBreakPointIncrement(AXISID Axis, long StepsCount)
      {
         string szCmd = longTo6BitHEX(StepsCount);

         TalkWithAxis(Axis, 'M', szCmd);
      }
      protected void SetBreakSteps(AXISID Axis, long NewBrakeSteps)
      {
         string szCmd = longTo6BitHEX(NewBrakeSteps);
         TalkWithAxis(Axis, 'U', szCmd);
      }
      protected void SetStepPeriod(AXISID Axis, long StepsCount)
      {
         string szCmd = longTo6BitHEX(StepsCount);
         TalkWithAxis(Axis, 'I', szCmd);
      }
      protected void StartMotion(AXISID Axis)
      {
         TalkWithAxis(Axis, 'J', null);
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
