using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ComponentModel; 

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

using System.Threading;

using System.Collections;

using System.IO; 



namespace ClientServerApp
{
    public partial class ServerGUI : Window
    {
        private readonly BackgroundWorker worker ;
        public static Dictionary<string, TcpClient> dicClient = new Dictionary<string, TcpClient>();
        public TcpListener serSocket ;
        public TcpClient clSocket ;
        private Socket mainSocket;

        public int counter = 0;
        public static string readData;
        public string readout;
        static bool dataBru = false;

        bool socketRunning = false;

        List<ClientThread > listThread = new List<ClientThread>();
        List<TcpClient> listClient = new List<TcpClient>(); 

        TextBlock chatBlock = new TextBlock(); 

        public ServerGUI()
        {
            worker = new BackgroundWorker(); 

            IPAddress ipAd = IPAddress.Parse("127.0.0.1");
            clSocket = default(TcpClient); 

            serSocket = new TcpListener(ipAd, 999); 
            InitializeComponent();
            worker.DoWork += backgroundWorker_DoWork;
            this.Closing += new CancelEventHandler(OnWindowClosing);

            chatScroll.Content = chatBlock; 
        }


        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            foreach (ClientThread cl in listThread)
            {
                cl.stopChat(); 
            }
        }

        private void startServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (! socketRunning)
            {
                worker.RunWorkerAsync();
                socketRunning = true; 
            }
        }

        public TextBlock getChatBlock()
        {
            return chatBlock; 
        }

        public  void cetakHistory(string data)
        {
            chatBlock.Text += data;
        }

        public void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("start server"); 
            serSocket.Start(); 
            while (true) 
            {
                try
                {
                    clSocket = serSocket.AcceptTcpClient();
                    counter += 1;
                    byte[] bytesFrom = new byte[100] ;
                    string dataFromClient = null;

                    Stream ns = clSocket.GetStream();
                    ns.Read(bytesFrom, 0, 100 );
                    dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);

                    dicClient.Add(dataFromClient, clSocket);
                    listClient.Add(clSocket); 

                    ClientThread cl = new ClientThread(this, listClient);  
                    cl.startClientThread(clSocket, dicClient, dataFromClient);

                    listThread.Add(cl); 
                    if (dataFromClient != null)
                    {
                        Console.WriteLine("client accepted: " + dataFromClient);
                    }
                    else
                    {
                        Console.WriteLine("mmmmm"); 
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("eror background worker: "  + ex.ToString()); 

                }
            }
        }


        public  static void broadCast(string message, string user) 
        {
            Console.WriteLine("broadcasting data: " + message + ">> " + user);
            try
            {
                //foreach (var item in dicClient)
                //{
                //    TcpClient client = item.Value;
                //    Stream broadcastStream = client.GetStream();
                //    Byte[] broacastBytes = null;
                //    broacastBytes = Encoding.UTF8.GetBytes(user + " says : " + message);
                //    broadcastStream.Write(broacastBytes, 0, broacastBytes.Length);
                //    broadcastStream.Flush();
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine("eror when broadcasting data: " + ex.ToString() ); 
            }

        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
