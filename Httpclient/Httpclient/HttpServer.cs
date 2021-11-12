using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.InteropServices;
namespace Httpclient
{
    public class HttpServer : IDisposable
    {
        //public static Bitmap bm;
        private List<Socket> _Clients;
        private Thread _Thread;
        //public static double seconds;
        
        public HttpServer()
            :this(Snapshots(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height, true))//(Screen.Snapshots(816, 545, true))
        {
            
        }

        public HttpServer(IEnumerable<Image> imagesSource)
        {
            _Clients = new List<Socket>();
            _Thread = null;

            this.ImagesSource = imagesSource;
            this.Interval = 50;

        }

        public IEnumerable<Image> ImagesSource { get; set; }

  
        public int Interval { get; set; }

        
        public bool IsRunning { get { return (_Thread != null && _Thread.IsAlive); } }

       
        public void Start(int port)
        {

            lock (this)
            {
                _Thread = new Thread(new ParameterizedThreadStart(ServerThread));
                _Thread.IsBackground = true;
                _Thread.Start(port);
            }

        }
        
        private void ServerThread(object state)
        {

            try
            {
                Socket Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                Server.Bind(new IPEndPoint(IPAddress.Any, (int)state));
                Server.Listen(10);

                System.Diagnostics.Debug.WriteLine(string.Format("Server started on port {0}.", state));
                //int i = 0;
                // while(true)
                //{
                //    Socket client = Server.Accept();

                //    ThreadPool.QueueUserWorkItem(new WaitCallback(ClientThread), client);
                //}

                foreach (Socket client in Server.IncommingConnectoins())
                {//i = 0;
                    //Thread myThread = new Thread(ClientThread);
                    //myThread.Start(client);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ClientThread), client);
                    //ThreadPool.QueueUserWorkItem(new WaitCallback(ControlThread), client);
                    //((IDisposable)client).Dispose();
                }
            }
            catch { }

            //this.Stop();
        }
        void ControlThread(object client)
        {
            byte[] bytes = new byte[128];
            Socket socketc = (Socket)client;
            while (true)
            {
                lock (_Clients)
                    _Clients.Add(socketc);
                try
                {

                    System.Diagnostics.Debug.WriteLine(string.Format("New client from {0}", socketc.RemoteEndPoint.ToString()));
                    int bytesRec = socketc.Receive(bytes);
                    String aa = Encoding.Unicode.GetString(bytes, 0, bytesRec);
                    string[] config = aa.Split(',');
                    Mouse.MoveTo(config[0], config[1]);
                }
                catch
                {
                    ((IDisposable)client).Dispose();
                }
                finally 
                {
                    lock (_Clients)
                        _Clients.Remove(socketc);
                }
            }
        }
        private void ClientThread(object client)
        {
            System.Diagnostics.Stopwatch stopwatch;
            //double seconds;
            Socket socket = (Socket)client;

            System.Diagnostics.Debug.WriteLine(string.Format("New client from {0}", socket.RemoteEndPoint.ToString()));

            lock (_Clients)
                _Clients.Add(socket);

            try
            {
                using (MjpegWriter wr = new MjpegWriter(new NetworkStream(socket, true)))
                {

                    // Writes the response header to the client.
                    wr.WriteHeader();
                   
                    // Streams the images from the source to the client.
                    foreach (var imgStream in Streams(this.ImagesSource))
                    {
                        stopwatch = new System.Diagnostics.Stopwatch();
                        stopwatch.Start();
                        if (this.Interval > 0)
                            Thread.Sleep(this.Interval);

                        wr.Write(imgStream);
                        //bm = (Bitmap)Image.FromStream(imgStream);
                        
                        stopwatch.Stop();
                        TimeSpan timeSpan = stopwatch.Elapsed; //獲取總時間
                        //seconds = 1 / (timeSpan.TotalSeconds);
                        //System.Diagnostics.Debug.WriteLine(seconds.ToString("0.0") + " FPS");
                    }

                }
            }
            catch (Exception ex)
            {
                //((IDisposable)client).Dispose();
                //((IDisposable)this.ImagesSource).Dispose();
                //Dispose();
                //MessageBox.Show(ex.ToString());
            }
            finally
            {
                lock (_Clients)
                    _Clients.Remove(socket);

            }
        }

        public static IEnumerable<Image> Snapshots(int width, int height, bool showCursor)
        {
            Size size = new Size(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);

            Bitmap srcImage = new Bitmap(size.Width, size.Height);
            Graphics srcGraphics = Graphics.FromImage(srcImage);

            bool scaled = (width != size.Width || height != size.Height);

            Bitmap dstImage = srcImage;
            Graphics dstGraphics = srcGraphics;

            if (scaled)
            {
                dstImage = new Bitmap(width, height);
                dstGraphics = Graphics.FromImage(dstImage);
            }

            Rectangle src = new Rectangle(0, 0, size.Width, size.Height);
            Rectangle dst = new Rectangle(0, 0, width, height);
            Size curSize = new Size(32, 32);

            while (true)
            {

                srcGraphics.CopyFromScreen(0, 0, 0, 0, size);

                if (showCursor)
                    Cursors.Default.Draw(srcGraphics, new Rectangle(Cursor.Position, curSize));

                if (scaled)
                    dstGraphics.DrawImage(srcImage, dst, src, GraphicsUnit.Pixel);

                yield return dstImage;

            }
        }

        static IEnumerable<MemoryStream> Streams( IEnumerable<Image> source)
        {
            
            foreach (var img in source)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.SetLength(0);
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    yield return ms;
                }        
            }

            //ms.Close();
            // ms = null;

            yield break;
        }
        #region IDisposable Members

        public void Dispose()
        {
              
        }
       
        #endregion
    }

    static class SocketExtensions
    {

        public static IEnumerable<Socket> IncommingConnectoins(this Socket server)
        {
            while (true)
                yield return server.Accept();
        }

    }    
   
    static public class Mouse
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern Int32 SendInput(Int32 cInputs, ref INPUT pInputs, Int32 cbSize);

        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 28)]
        public struct INPUT
        {
            [FieldOffset(0)]
            public INPUTTYPE dwType;
            [FieldOffset(4)]
            public MOUSEINPUT mi;
            [FieldOffset(4)]
            public KEYBOARDINPUT ki;
            [FieldOffset(4)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MOUSEINPUT
        {
            public Int32 dx;
            public Int32 dy;
            public Int32 mouseData;
            public MOUSEFLAG dwFlags;
            public Int32 time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct KEYBOARDINPUT
        {
            public Int16 wVk;
            public Int16 wScan;
            public KEYBOARDFLAG dwFlags;
            public Int32 time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct HARDWAREINPUT
        {
            public Int32 uMsg;
            public Int16 wParamL;
            public Int16 wParamH;
        }

        public enum INPUTTYPE : int
        {
            Mouse = 0,
            Keyboard = 1,
            Hardware = 2
        }

        [Flags()]
        public enum MOUSEFLAG : int
        {
            MOVE = 0x1,
            LEFTDOWN = 0x2,
            LEFTUP = 0x4,
            RIGHTDOWN = 0x8,
            RIGHTUP = 0x10,
            MIDDLEDOWN = 0x20,
            MIDDLEUP = 0x40,
            XDOWN = 0x80,
            XUP = 0x100,
            VIRTUALDESK = 0x400,
            WHEEL = 0x800,
            ABSOLUTE = 0x8000
        }

        [Flags()]
        public enum KEYBOARDFLAG : int
        {
            EXTENDEDKEY = 1,
            KEYUP = 2,
            UNICODE = 4,
            SCANCODE = 8
        }

        static public void LeftDown()
        {
            INPUT leftdown = new INPUT();

            leftdown.dwType = 0;
            leftdown.mi = new MOUSEINPUT();
            leftdown.mi.dwExtraInfo = IntPtr.Zero;
            leftdown.mi.dx = 0;
            leftdown.mi.dy = 0;
            leftdown.mi.time = 0;
            leftdown.mi.mouseData = 0;
            leftdown.mi.dwFlags = MOUSEFLAG.LEFTDOWN;

            SendInput(1, ref leftdown, Marshal.SizeOf(typeof(INPUT)));
        }

        static public void LeftUp()
        {
            INPUT leftup = new INPUT();

            leftup.dwType = 0;
            leftup.mi = new MOUSEINPUT();
            leftup.mi.dwExtraInfo = IntPtr.Zero;
            leftup.mi.dx = 0;
            leftup.mi.dy = 0;
            leftup.mi.time = 0;
            leftup.mi.mouseData = 0;
            leftup.mi.dwFlags = MOUSEFLAG.LEFTUP;

            SendInput(1, ref leftup, Marshal.SizeOf(typeof(INPUT)));
        }

        static public void LeftClick()
        {
            LeftDown();
            Thread.Sleep(20);
            LeftUp();
        }

        static public void LeftDoubleClick()
        {
            LeftClick();
            Thread.Sleep(50);
            LeftClick();
        }

        static public void DragTo(string sor_X, string sor_Y, string des_X, string des_Y)
        {
            MoveTo(sor_X, sor_Y);
            LeftDown();
            Thread.Sleep(200);
            MoveTo(des_X, des_Y);
            LeftUp();
        }

        static public void MoveTo(string tx, string ty)
        {
            int x, y;
            int.TryParse(tx, out x);
            int.TryParse(ty, out y);

            Cursor.Position = new Point(x, y);
        }
    }
}
