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

using System.Security;
using System.Security.Cryptography;

using System.IO;
using System.Net;
using System.Net.Sockets;


using System.Threading;
using System.Media;

using System.ComponentModel;
using System.Text.RegularExpressions;

namespace ClientProject1
{
    public partial class MainWindow : Window
    {

        TcpClient clSocket = new TcpClient() ;
        NetworkStream netStream = default(NetworkStream); 
         
        String str;

        string readData;

        private Thread ctThread;

        private bool connected = false; 

        private string ip = "127.0.0.1" ;
        
        private int  port = 999  ;

        bool stillChat = true;

        List<Thread> listThread = new List<Thread>();

        TextBlock chatHistory = new TextBlock(); 

        public MainWindow()
        {
            InitializeComponent();
            this.Closing += new CancelEventHandler(OnWindowClosing);

            viewerHistory.Content = chatHistory; 
           
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            stopChat(); 
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!connected)
            {
                var username = userNameBox.Text;
                if (string.IsNullOrEmpty(username))
                {
                    username = "mohammad"; 
                }
                conServer(ip, username ) ; 
                connected = true; 
            }
        }

        private void conServer(string ipserver, string nama)
        {
            readData = "Connecting to Chat Server...";
            
            printChatHistory("Connecting to Chat Server..." ); 

            try
            {
                clSocket.Connect( ipserver,  999 );
                netStream = clSocket.GetStream();

                byte[] outStream = System.Text.Encoding.ASCII.GetBytes(nama);
                netStream.Write(outStream, 0, outStream.Length);
                netStream.Flush();

                ctThread = new Thread(getMessage);
                ctThread.Start();
            }
            catch (SocketException se)
            {
                Console.WriteLine("failed to connect to server");  
            }
        }

        public void stopChat()
        {
            stillChat = false; 
        }


        public delegate void UpdateHistory(string data); 

        public void printChatHistory(string data) 
        {
            chatHistory.Text += data + Environment.NewLine ; 
        }

        Regex rgx = new Regex("[^a-zA-Z0-9 -/+=>]");
        private void getMessage()
        {
            try
            {
                while (stillChat)
                {
                    Stream st = clSocket.GetStream();
                    byte[] inStream = new byte[200];
                    st.Read(inStream, 0, 200 );
                    string returnData;
                    if (stillChat)
                    {
                        returnData = System.Text.Encoding.UTF8.GetString(inStream);
                        returnData = rgx.Replace(returnData, ""); 
                        this.Dispatcher.Invoke(new UpdateHistory(this.printChatHistory), new object[] { returnData}); 
                    }
                }
            }catch(Exception ex){
                Console.WriteLine("client error receiving broadcast message: " + ex.ToString()); 
            }
        }

        private void buttonEncrypt_Click(object sender, RoutedEventArgs e)
        {
            var text = inputChat.Text;
            var result = EncryptStringAES(text , "abcdefg" );
            encrypTedBlock.Text = result; 
        }


        #region encryption 
        private static byte[] _salt = Encoding.ASCII.GetBytes("o6806642kbM7c5"); 
        public static string EncryptStringAES(string plainText, string sharedSecret)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException("plainText");
            if (string.IsNullOrEmpty(sharedSecret))
                throw new ArgumentNullException("sharedSecret");

            string outStr = null;                       // Encrypted string to return
            RijndaelManaged aesAlg = null;              // RijndaelManaged object used to encrypt the data.

            try
            {
                // generate the key from the shared secret and the salt
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

                // Create a RijndaelManaged object
                aesAlg = new RijndaelManaged();
                aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    // prepend the IV
                    msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
                    msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                    }
                    outStr = Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                //Label l1 = new Label();
                //l1.ForeColor = Color.Red;
                //l1.Text = "Enter Proper Key value.";
                //l1.Show();
                //ForClient f = new ForClient();
                //f.Controls.Add(l1);
            }
            finally
            {
                // Clear the RijndaelManaged object.
                if (aesAlg != null)
                    aesAlg.Clear();
            }

            // Return the encrypted bytes from the memory stream.
            return outStr;
        }
        #endregion 

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (clSocket.Connected)
                {
                    Stream st = clSocket.GetStream();
                    byte[] outstream = System.Text.Encoding.UTF8.GetBytes(encrypTedBlock.Text);
                    //byte[] outStream = System.Text.Encoding.UTF8.GetBytes(inputChat.Text);
                    st.Write(outstream, 0, outstream.Length);
                    
                    st.Flush();
                    //printChatHistory(inputChat.Text);
                    //printChatHistory(DecryptStringAES(encrypTedBlock.Text, "abcdefg" ));
                    //printChatHistory(inputChat.Text);  
                    inputChat.Text = ""; 
                }
                else
                {
                    Console.WriteLine("socket connected"); 
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("failed to write data: " +  ex.ToString());  
                //MessageBox.Show(se.ToString());
            }
        }

    }
}
