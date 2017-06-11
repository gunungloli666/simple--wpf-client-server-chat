using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Security;
using System.Security.Cryptography;

using System.IO;
using System.Net;
using System.Net.Sockets;

using System.Threading;
using System.Media;

using System.Text.RegularExpressions;

namespace ClientServerApp
{

    class ClientThread
    {
        private bool stillChat = true;
        private TcpClient client;
        private Dictionary<string, TcpClient> dict;
        private string id;

        List<TcpClient> listTcp;

        public ClientThread(TcpClient tcp, Dictionary<string, TcpClient> dic, string id)
        {
            this.client = tcp;
            this.dict = dic;
            this.id = id;
            Thread chatThread = new Thread(doChat);
            chatThread.Start();
        }

        public ClientThread()
        {
        }
        private ServerGUI gui;
        public ClientThread(ServerGUI gui)
        {
            this.gui = gui;
        }

        public ClientThread(ServerGUI gui, List<TcpClient> tcpList)
        {
            this.gui = gui;
            this.listTcp = tcpList; 
        }

        public void startClientThread(TcpClient tcp, Dictionary<string, TcpClient> dic, string id)
        {
            this.client = tcp;
            this.dict = dic;
            this.id = id;

            Thread chatThread = new Thread(doChat);
            chatThread.Start();
        }

        public void stopChat()
        {
            this.stillChat = false;
        }

        #region decryptor
        private static byte[] _salt = Encoding.ASCII.GetBytes("o6806642kbM7c5");
        public static string DecryptStringAES(string cipherText, string sharedSecret)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException("cipherText");
            if (string.IsNullOrEmpty(sharedSecret))
                throw new ArgumentNullException("sharedSecret");

            // Declare the RijndaelManaged object
            // used to decrypt the data.
            RijndaelManaged aesAlg = null;

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            try
            {
                // generate the key from the shared secret and the salt
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

                // Create the streams used for decryption.                
                byte[] bytes = Convert.FromBase64String(cipherText);
                using (MemoryStream msDecrypt = new MemoryStream(bytes))
                {
                    // Create a RijndaelManaged object
                    // with the specified key and IV.
                    aesAlg = new RijndaelManaged();
                    aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                    // Get the initialization vector from the encrypted stream
                    aesAlg.IV = ReadByteArray(msDecrypt);
                    // Create a decrytor to perform the stream transform.
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                if (aesAlg != null)
                    aesAlg.Clear();
            }
            return plaintext;
        }

        private static byte[] ReadByteArray(Stream s)
        {
            byte[] rawLength = new byte[sizeof(int)];
            if (s.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
            {
                throw new SystemException("Stream did not contain properly formatted byte array");
            }
            byte[] buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
            if (s.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new SystemException("Did not read byte array properly");
            }
            return buffer;
        }
        #endregion

        Regex rgx = new Regex("[^a-zA-Z0-9 -/+=]");
        public delegate void UpdateTextCallback(string message, string printID);
        private void UpdateText(string message, string printID)
        {
            String hasil = rgx.Replace(message, "");
            String id_replac = rgx.Replace(id, "");
            StringBuilder build = new StringBuilder();
            if (gui != null)
            {
                if (printID.Equals("yes"))
                {
                    gui.getChatBlock().Text += id_replac + ">>>" + hasil + Environment.NewLine;
                }
                else if(printID.Equals("no"))
                {
                    gui.getChatBlock().Text +=  hasil + Environment.NewLine;
                }
            }
        }

        public delegate void Broadcasting(string message, string id);
        public void broadCasting(string message ,string id )
        {
            String hasil = rgx.Replace(message, "");
            string res=  "";
            if (! string.IsNullOrEmpty(this. id))
            {
                res = this. id; 
            }else{
                res = "rpt"; 
            }
            try
            {
                foreach (var item in this.dict)
                {
                    TcpClient client = item.Value;
                    Stream broadcastStream = client.GetStream();
                    Byte[] broacastBytes = null;
                    broacastBytes = System.Text.Encoding.UTF8.GetBytes(res + ">>>" + hasil);
                    broadcastStream.Write(broacastBytes, 0, broacastBytes.Length);
                    broadcastStream.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("exception during broadcast: " + ex.ToString()); 
            }
        }


        string key = "abcdefg";

        public void doChat()
        {
            while (stillChat == true)
            {
                try
                {
                    byte[] bytesFrom = new byte[200];
                    string dataFromClient = null;
                    Stream ns = client.GetStream();

                    ns.Read(bytesFrom, 0, 200 );
                    dataFromClient = System.Text.Encoding.UTF8.GetString(bytesFrom);
                    gui.getChatBlock().Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new Object[] { dataFromClient, "yes" });


                    string decrypted = DecryptStringAES(rgx.Replace(dataFromClient, "") , "abcdefg");
                    gui.getChatBlock().Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new Object[] { "(" + decrypted + ")" , "no" });

                    broadCasting(decrypted,  this.id ); 
                }
                catch (Exception e)
                {
                    Console.WriteLine("eror in client  thread: " + e.ToString());
                }
            }

        }
    }


}
