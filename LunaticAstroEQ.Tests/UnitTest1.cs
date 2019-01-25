using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.DeviceInterface;
using ASCOM.LunaticAstroEQ;
using ASCOM.LunaticAstroEQ.Controller;
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

      string ConnectionString = "COM6:9600,None,8,One,NoDTR,NoRTS";
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
   }
}
