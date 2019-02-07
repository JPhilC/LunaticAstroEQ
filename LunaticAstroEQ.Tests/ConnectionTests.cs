using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.DeviceInterface;
using ASCOM.LunaticAstroEQ;
using ASCOM.LunaticAstroEQ.Controller;
using ASCOM.LunaticAstroEQ.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TA.Ascom.ReactiveCommunications;

namespace LunaticAstroEQ.Tests
{
   /*
   InquireMotorBoardVersion
   TalkWithAxis(AXIS1, e, )
    - > Command: :e1

   Raw response: =010500

    -> Response: =010500
    (0x=010500
   )
   InquireGridPerRevolution
   TalkWithAxis(AXIS1, a, )
    - > Command: :a1

   Raw response: =018025

    -> Response: =018025
    (0x=018025
   )
   TalkWithAxis(AXIS2, a, )
    - > Command: :a2

   Raw response: =018025

    -> Response: =018025
    (0x=018025
   )
   InquireTimerInterruptFreq
   TalkWithAxis(AXIS1, b, )
    - > Command: :b1

   Raw response: =8B3800

    -> Response: =8B3800
    (0x=8B3800
   )
   TalkWithAxis(AXIS2, b, )
    - > Command: :b2

   Raw response: =8B3800

    -> Response: =8B3800
    (0x=8B3800
   )
   InquireHighSpeedRatio
   TalkWithAxis(AXIS1, g, )
    - > Command: :g1

   Raw response: =08

    -> Response: =08
    (0x=08
   )
   TalkWithAxis(AXIS2, g, )
    - > Command: :g2

   Raw response: =08

    -> Response: =08
    (0x=08
   )
   InquirePECPeriod
   TalkWithAxis(AXIS1, s, )
    - > Command: :s1

   Raw response: =AB4200

    -> Response: =AB4200
    (0x=AB4200
   )
   TalkWithAxis(AXIS2, s, )
    - > Command: :s2

   Raw response: =AB4200

    -> Response: =AB4200
    (0x=AB4200
   )
   MCGetAxisPosition
   TalkWithAxis(AXIS1, j, )
    - > Command: :j1

   Raw response: =000080

    -> Response: =000080
    (0x=000080
   )
   TalkWithAxis(AXIS2, j, )
    - > Command: :j2

   Raw response: =000080

    -> Response: =000080
    (0x=000080
   )
   InitializeMC
   TalkWithAxis(AXIS1, F, )
    - > Command: :F1

   Raw response: =

    -> Response:  (0x)
   TalkWithAxis(AXIS2, F, )
    - > Command: :F2

   Raw response: =

    -> Response:  (0x)
    */
   [TestClass]
   public class ConnectionTests
   {
      enum AXISID { AXIS1 = 0, AXIS2 = 1 }; // ID unsed in ASTRO.DLL for axis 1 and axis 2 of a mount.

      int Retry = 1;
      double TimeOut = 2;

      string ConnectionString = "COM3:9600,None,8,One,NoDTR,NoRTS";
      const char cStartChar_Out = ':';    // Leading charactor of a command 
      const char cStartChar_In = '=';
      const char cErrChar = '!';              // Leading charactor of an ABNORMAL response.
      const char cEndChar = (char)13;

      [TestMethod]
      public void EQTransactionInquireMotorBoardVersionTest()
      {
         string response = TalkWithAxis(AXISID.AXIS1, 'e', null);
         Assert.AreEqual("=010500\r", response);
      }

      [TestMethod]
      public void EQTransactionInquireGridPerRevolution()
      {
         string response = TalkWithAxis(AXISID.AXIS1, 'a', null);
         Assert.AreEqual("=018025\r", response);
      }


      private string TalkWithAxis(AXISID axis, char cmd, string cmdDataStr)
      {
         string response = string.Empty;
         DeviceEndpoint endPoint = SerialDeviceEndpoint.FromConnectionString(ConnectionString);

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


         var cmdTransaction = new EQTransaction(cmdString) { Timeout = TimeSpan.FromSeconds(TimeOut) };


         using (ICommunicationChannel channel = new SerialCommunicationChannel(endPoint))
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
               throw new Exception("AstroEQ not responding", ex);
            }
            finally
            {
               // To clean up, we just need to dispose the TransactionObserver and the channel is closed automatically.
               // Not strictly necessary, but good practice.
               transactionObserver.OnCompleted(); // There will be no more transactions.
               transactionObserver = null; // not necessary, but good practice.
               endPoint = null;
            }

         }
         return response;
      }

      [TestMethod]
      public void TestSetMotionMode()
      {
         string EQresult = EQ_SendGCode(AxisId.Axis1_RA, HemisphereOption.Northern, AxisMode.Slew, AxisDirection.Forward, AxisSpeed.LowSpeed);
         string MCresult = MCSetMotionMode(AxisId.Axis1_RA, HemisphereOption.Northern, AxisMode.Slew, AxisDirection.Forward, AxisSpeed.LowSpeed);
         Assert.AreEqual(EQresult, MCresult);
      }


      #region MC code
      protected string MCSetMotionMode(AxisId axis, HemisphereOption hemisphere, AxisMode mode, AxisDirection direction, AxisSpeed speed)
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
            //goto
            if (speed == AxisSpeed.LowSpeed)
            {
               // Low speed goto = 2
               ch |= 0x20;
            }
            else
            {
               //high speed goto = 0

            }
         }
         else
         {
            // slew
            if (speed == AxisSpeed.HighSpeed)
            {
               // High speed slew= 3
               ch |= 0x30;
            }
            else
            {
               // low speed slew= 1
               ch |= 0x10;
            }
         }


         string szCmd = LongTo2BitHEX(ch);
         return GetTalkWithAxisCommand(axis, 'G', szCmd);
      }


      private string GetTalkWithAxisCommand(AxisId axis, char cmd, string cmdDataStr)
      {
         //System.Diagnostics.Debug.WriteLine(String.Format("TalkWithAxis({0}, {1}, {2})", axis, cmd, cmdDataStr));
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
         //System.Diagnostics.Debug.WriteLine($" - > Command: {cmdString}");
         //string.Format("{0}{1}{2}{3}{4}",
         //cStartChar_Out,
         //command,
         //(int)axis,
         //(cmdDataStr ?? "."),
         //cEndChar);

         return cmdString;
      }

      private string LongTo6BitHEX(long number)
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

      private string LongTo2BitHEX(long number)
      {
         // 31 -> 0F0000
         String A = ((int)number & 0xFF).ToString("X").ToUpper();

         if (A.Length == 1)
            A = "0" + A;

         // if (D)
         // Log.d(TAG, "longTo6BitHex " + number + "," + A + "," + B + "," + C);

         return A;
      }
      #endregion

      #region Old EQ code ...
      private string EQ_SendGCode(AxisId axisId, HemisphereOption hemisphere, AxisMode mode, AxisDirection direction, AxisSpeed speed)
      {
         System.Diagnostics.Debug.WriteLine(string.Format("EQ_SendGCode({0}, {1}, {2}, {3}, {4})", axisId, hemisphere, mode, direction, speed));
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
            //goto
            if (speed == AxisSpeed.LowSpeed)
            {
               // Low speed goto = 2
               ch |= 0x20;
            }
            else
            {
               //high speed goto = 0

            }
         }
         else
         {
            // slew
            if (speed == AxisSpeed.HighSpeed)
            {
               // High speed slew= 3
               ch |= 0x30;
            }
            else
            {
               // low speed slew= 1
               ch |= 0x10;
            }
         }


         // Send 'G' Command, with parameter
         return GetEQSendCommandCommand(axisId, 'G', ch, 2);


      }



      private string GetEQSendCommandCommand(AxisId motorId, char command, int parameters, short count)
      {

         System.Diagnostics.Debug.WriteLine(String.Format("EQ_SendCommand({0}, {1}, {2}, {3})", motorId, command, parameters, count));
         char[] hex_str = "0123456789ABCDEF     ".ToCharArray();   // Hexadecimal translation
         const int BufferSize = 20;
         StringBuilder sb = new StringBuilder(BufferSize);
         sb.Append(cStartChar_Out);
         sb.Append(command);
         sb.Append(((int)motorId + 1).ToString());
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
               return "Error";
         }
         sb.Append(cEndChar);
         return sb.ToString();

      }

      #endregion

   }


}
