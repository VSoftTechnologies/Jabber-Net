/* --------------------------------------------------------------------------
 * Copyrights
 *
 * Portions created by or assigned to Cursive Systems, Inc. are
 * Copyright (c) 2002-2008 Cursive Systems, Inc.  All Rights Reserved.  Contact
 * information for Cursive Systems, Inc. is available at
 * http://www.cursive.net/.
 *
 * License
 *
 * Jabber-Net is licensed under the LGPL.
 * See licenses/Jabber-Net_LGPLv3.txt for details.
 * --------------------------------------------------------------------------*/

using System;
using System.Threading;
using JabberNet.bedrock.net;
using NUnit.Framework;

namespace JabberNet.Test.bedrock.net
{
    /// <summary>
    ///    Summary description for AsyncSocketTest.
    /// </summary>
    [TestFixture]
    public class AsyncSocketTest : ISocketEventListener
    {
        private static readonly System.Text.Encoding ENC = System.Text.Encoding.ASCII;

        private static readonly byte[] sbuf = ENC.GetBytes("01234567890123456789012345678901234567890123456789012345678901234567890123456789");
        private readonly object done = new object();
        private string success = null;

        private bool succeeded = true;
        private string errorMessage;

        [Test] public void Test_Write()
        {
            SocketWatcher w = new SocketWatcher(20);
            Address a = new Address("127.0.0.1", 7001);
            a.Resolve();

            AsyncSocket listen = w.CreateListenSocket(this, a);
            listen.RequestAccept();

            AsyncSocket connect;
            lock (done)
            {
                connect = w.CreateConnectSocket(this, a);
                bool NoTimeout = Monitor.Wait(done, new TimeSpan(0, 0, 30));

                Assert.IsTrue(NoTimeout, "The read command didn't complete in time.");
                Assert.IsTrue(succeeded, errorMessage);
            }

            Assert.AreEqual("5678901234", success);
            connect.Close();
            listen.Close();
        }

        [Test]
        public void Test_Ops()
        {
            using (var w = new SocketWatcher(20))
            {
                var a = new Address("127.0.0.1", 7002);
                a.Resolve();
                var one = w.CreateListenSocket(this, a);
                one.Close();
                AsyncSocket two = null;
                var three = one;
                var four = two;
                Assert.IsTrue(one == three);
                Assert.IsTrue(two == four);
                Assert.IsTrue(one >= three);
                Assert.IsTrue(two >= four);
                Assert.IsTrue(one <= three);
                Assert.IsTrue(two <= four);
                Assert.IsTrue(one != two);
                Assert.IsTrue(two != one);
                Assert.IsTrue(one > two);
                Assert.IsTrue(one >= two);
                Assert.IsTrue(two < one);
                Assert.IsTrue(two <= one);

                two = w.CreateListenSocket(this, a);
                two.Close();
                four = two;
                Assert.IsTrue(one == three);
                Assert.IsTrue(two == four);
                Assert.IsTrue(one >= three);
                Assert.IsTrue(two >= four);
                Assert.IsTrue(one <= three);
                Assert.IsTrue(two <= four);
                Assert.IsTrue(one != two);
                Assert.IsTrue(two != one);

                var c = ((IComparable)one).CompareTo(two);
                Assert.IsTrue(c != 0);
                if (c == -1)
                {
                    // one less than two
                    Assert.IsTrue(one < two);
                    Assert.IsTrue(one <= two);
                    Assert.IsTrue(two > one);
                    Assert.IsTrue(two >= one);
                }
                else if (c == 1)
                {
                    // one greater than two
                    Assert.IsTrue(one > two);
                    Assert.IsTrue(one >= two);
                    Assert.IsTrue(two < one);
                    Assert.IsTrue(two <= one);
                }
                else
                {
                    Assert.IsTrue(false);
                }
                one.Close();
                two.Close();
            }
        }

        #region Implementation of ISocketEventListener
        public bool OnAccept(BaseSocket newsocket)
        {
            newsocket.RequestRead();
            return false;
        }

        public bool OnRead(BaseSocket sock, byte[] buf, int offset, int length)
        {
            success = ENC.GetString(buf, offset, length);
            sock.Close();
            lock(done)
            {
                Monitor.Pulse(done);
            }
            return false;
        }

        public void OnWrite(BaseSocket sock, byte[] buf, int offset, int length)
        {
            System.Diagnostics.Debug.WriteLine(ENC.GetString(buf, offset, length));
            sock.Close();
        }

        public void OnError(BaseSocket sock, Exception ex)
        {
            lock (done)
            {
                succeeded = false;
                errorMessage = ex.Message;
                Monitor.Pulse(done);
            }
        }

        public void OnConnect(BaseSocket sock)
        {
            sock.Write(sbuf, 5, 10);
        }

        public void OnClose(BaseSocket sock)
        {

        }

        public void OnInit(BaseSocket new_sock)
        {

        }

        public ISocketEventListener GetListener(BaseSocket new_sock)
        {
            return this;
        }

        public bool OnInvalidCertificate(BaseSocket sock,
            System.Security.Cryptography.X509Certificates.X509Certificate certificate,
            System.Security.Cryptography.X509Certificates.X509Chain chain,
            System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return false;
        }

        #endregion
    }
}
