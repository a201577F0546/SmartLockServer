using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;//泛型命名空间
using System.Net;//使用网络编程接口的命名空间
using System.Threading;//多线程所需要的命名空间
using System.Net.Sockets;//使用套接字所需要的命名空间
using System.Data.SqlClient;//连接数据库

namespace smartlockserver
{
    public partial class SLserver : Form
    {
        public SLserver()
        {
            InitializeComponent();
        }
        private static byte[] result = new byte[1024];
        private static int myPort = 8885;//？？？什么意思，我猜是该程序的端口
        static Socket serverSocket;//该程序自身的套接字
        static Socket ServerFileSocket;//接受
        static Socket ServerFileReSocket;//发送
        string RemoteEndPoint; //当前连接的网络节点
        //string ReEndFilePoint;//当前文件监听节点    
        Dictionary<string, Socket> dic = new Dictionary<string, Socket> { };//储存客户端 Dictionary<>为一个键值集合
        Dictionary<string, Socket> FileDic = new Dictionary<string, Socket> { };//储存文件客户端
        byte[] sendMsg = new byte[1024 * 1024];//字节数组，信息传输的必要格式               
        List<string> Savesocket = new List<string>();//字符串列表
        private void SLserver_Load(object sender, EventArgs e)
        {
            
                
            
          
            

        }

        private void button1_Click(object sender, EventArgs e)//开始监听
        {
            this.button1.Enabled = false;

            IPAddress ips = new IPAddress(Dns.GetHostByName(Dns.GetHostName()).AddressList[0].Address);//过时就过时吧
            textBox1.Text = ips.ToString();
            IPAddress ip = IPAddress.Parse("127.0.0.1");//服务器的公网IP 120.27.49.70
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(ip, myPort));
            serverSocket.Listen(10);
            label1.Text = "监听已启动 等待客户端接入...";

            Control.CheckForIllegalCrossThreadCalls = false;//????书上说遇到线程交叉调用控制问题，要在调用进程之前加上该语句
            Thread myThread = new Thread(ListenClientConnect);//创建一个线程来监听客户端连接
            myThread.Start();
        }
        private void ListenClientConnect()//客户端连接监听方法
        {
            Socket newclientSocket = null;//为什么要置空？ 
            while (true)
            {
                try
                {
                    newclientSocket = serverSocket.Accept();//执行到此处时候程序处于阻塞状态，一旦有客户请求，accept会产生一个新的套接字newclientSocket用于跟客户端进行真正的数据传输
                    IPAddress clientIp = (newclientSocket.RemoteEndPoint as IPEndPoint).Address;/*这一步用来获取连接到服务器客户端的IP和端口然后强制转换为IPEnd对象*/
                    //获取客户端IP as 操作是用来强制转换，如果转换失败则返回为空
                    int clientPort = (newclientSocket.RemoteEndPoint as IPEndPoint).Port;//获取客户端端口
                    string sendmsg = "连接成功！\r\n" + "本地IP:" + clientIp + ",本地端口" + clientPort.ToString();
                    RemoteEndPoint = newclientSocket.RemoteEndPoint.ToString();//
                    textBox1.AppendText("成功与" + RemoteEndPoint + "客户端建立连接！\t\n");
                    label6.Text = "客户端接入成功";
                    dic.Add(RemoteEndPoint, newclientSocket);//在存储Dictionary目录中添加socket与当前连接的网络结点
                    Onlist(RemoteEndPoint);

                    Savesocket.Add(RemoteEndPoint);//这是将服务器自身的网络信息 存进列表里  SaveFileSok.Add(RemoteEndPoint + "*" + ReEndFilePoint);

                    byte[] SendMsg = Encoding.UTF8.GetBytes(sendmsg);

                    newclientSocket.Send(SendMsg);
                    foreach (var i in Savesocket)
                    {
                        textBox1.AppendText(i + "\r\n");
                    }
                    foreach (var item in dic)//注：原来这里是 item in dic
                    {
                        sendMsg = Encoding.UTF8.GetBytes("!!!???" + "    服务器//" + gavelist());//发送过来的数据进行编码才能正常显示
                        item.Value.Send(sendMsg);//将数据发送到连接的socket
                    }

                    //显示连接信息         
                    textBox1.AppendText("当前客户端" + newclientSocket.RemoteEndPoint.ToString() + "\r\n");
                    Thread receiveThread = new Thread(ReceiveMessage);//创建一个线程来跑ReceiveMessage（）
                    receiveThread.IsBackground = true;//将该线程设为后台线程
                    receiveThread.Start(newclientSocket);//开启线程
                                                         //每循环一次线程就重新开始一次？？？？
                }

                catch (Exception)
                {

                    textBox1.AppendText("监听过程有异常！");

                }

            }//while()结束
        }//客户端监听方法结束

         /// <summary>
         /// 在listbox表中添加info
         /// </summary>
         /// <param name="info"></param>
        void Onlist(string info)
        {
            listBox1.Items.Add(info);
        }
        string gavelist()
        {
            string list = "";

            for (int i = 0; i < Savesocket.Count; i++)
            {
                list += Savesocket[i] + "//";
            }

            return list;
        }
        private void ReceiveMessage(object clientSocket)//接受信息并转发，clientSocket为服务器或将转发socket
        {
            Socket myClientSocket = (Socket)clientSocket;

            while (true)
            {
                byte[] arrserverMsg = new byte[1024 * 1024];
                byte[] senmsg = new byte[1024 * 1024];//1M的缓冲

                try
                {

                    int receiveNumber = myClientSocket.Receive(arrserverMsg);
                    string strRecMsg = Encoding.UTF8.GetString(arrserverMsg, 0, receiveNumber);

                    if (strRecMsg.Length > 6 && strRecMsg.Substring(0, 6) == "!!!???")//服务器转发 原理是添加特定前缀来区分是否需要转发
                    {
                        string address = strRecMsg.Substring(6, strRecMsg.IndexOf("()(())()") - 6);

                        int site = strRecMsg.IndexOf("()(())()") + 8;
                        string st = strRecMsg.Substring(site);
                        senmsg = Encoding.UTF8.GetBytes(st);
                        dic[address].Send(senmsg);
                    }
                    else//发送到服务器端
                    {
                        textBox1.AppendText("客户端:" + myClientSocket.RemoteEndPoint + ",time:" + GetCurrentTime() + "\r\n" + strRecMsg + "\r\n\n");
                    }

                }
                catch (Exception ex)
                {

                    textBox1.AppendText("客户端" + myClientSocket.RemoteEndPoint + "已中断连接" + "\r\n");
                    listBox1.Items.Remove(myClientSocket.RemoteEndPoint.ToString());
                    string socketip = myClientSocket.RemoteEndPoint.ToString();
                    myClientSocket.Close();
                    foreach (var item in dic)
                    {

                        if (item.Key != socketip)
                        {
                            sendMsg = Encoding.UTF8.GetBytes(gavelist());
                            try
                            {
                                item.Value.Send(sendMsg);
                            }
                            catch (Exception)
                            {

                                throw;
                            }
                            
                        }


                    }
                    break;
                }

            }
        }//消息接收方法结束
        private DateTime GetCurrentTime()//获取当前时间
        {
            DateTime currentTime = new DateTime();
            currentTime = DateTime.Now;
            return currentTime;
        }//获取当前时间结束

        private void SLserver_FormClosing(object sender, FormClosingEventArgs e)//关闭后清理线程
        {
            
            MessageBox.Show("关闭");

        }

        private void label7_Click(object sender, EventArgs e)
        {
        }

        private void button2_Click(object sender, EventArgs e)
        {

            SqlConnection conn = DB.Camcon();//生成一个连接
            try
            {
                conn.Open();//打开连接
                label4.Text = "已连接";
            }
            catch (SqlException)
            {
                label4.Text = "数据库连接失败！请检查连接字符串";
                // throw;
            }
        }
        //private void sendtoClient()//向请求的客户端发送反馈
        //{
        //    string sendMsg = textBox3.Text.Trim();
        //    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(sendMsg);//返回一个字节数组
        //    if (listBox1.SelectedIndex == -1)
        //    {
        //        MessageBox.Show("请选择要发送的客户端！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Stop);

        //    }
        //    else
        //    {
        //        string selestClient = listBox1.Text;//从客户端列表读取客户端，Text是获取当前选定项文本（是客户端的IP地址与端口信息）
        //        dic[selestClient].Send(bytes);
        //        textBox3.Clear();
        //        textBox2.AppendText("服务器发送" + selestClient + "消息" + GetCurrentTime() + "\r\n" + sendMsg + "\r\n");
        //    }
        //}
    }
}
