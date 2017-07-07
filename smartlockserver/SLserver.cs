using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;//使用网络编程接口的命名空间
using System.Threading;//多线程所需要的命名空间
using System.Net.Sockets;//使用套接字所需要的命名空间
using System.Data.SqlClient;//连接数据库
using System.Diagnostics;
using System.Configuration;
using System.Timers;
namespace smartlockserver
{
    public partial class SLserver : Form
    {
        public SLserver()
        {
            InitializeComponent();
        }
        private static byte[] result = new byte[1024];
        private static int myPort = 8885;//服务器
        static Socket serverSocket;//该程序自身的套接字
        string RemoteEndPoint; //当前连接的网络节点
        public Dictionary<string, Socket> dic = new Dictionary<string, Socket> { };//储存客户端 Dictionary<>为一个键值集合
        public Dictionary<string, Socket> Lockdic = new Dictionary<string, Socket> { };//存储智能锁的字典 
        //public Dictionary<string, Socket> FileDic = new Dictionary<string, Socket> { };//储存文件客户端
        byte[] sendMsg = new byte[1024 * 1024];//字节数组，信息传输的必要格式               
        List<string> Savesocket = new List<string>();//字符串列表 并没有制定列表的大小
        Thread myThread;
        Thread heartbeat;//用来进行心跳
        Thread heartbeat2;
        int sum=0;//记录客户端的个数
        string fenlei;
        Socket newscoket = null;
        private void SLserver_Load(object sender, EventArgs e)
        {
            textBox1.Text = "****连接字符串****:\n";
            textBox1.AppendText("~~~~" + ConfigurationManager.AppSettings["constring"].ToString()+"~~~~"+ "\n");
            int length;
            length = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Length;//获取长度
            for (int i = 0; i < length; i++)
            {
                if (Dns.GetHostEntry(Dns.GetHostName()).AddressList[i].AddressFamily.ToString()=="InterNetwork")
                {
                    IPAddress ips = new IPAddress(Dns.GetHostEntry(Dns.GetHostName()).AddressList[i].GetAddressBytes());
                    textBox1.AppendText("****本地IP：");
                    textBox1.AppendText(ips.ToString() + "****\n");
                    IPAddress ip = IPAddress.Parse(ips.ToString());//服务器的局域网IP
                    serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    serverSocket.Bind(new IPEndPoint(ip, myPort));//对于服务器程序，套接字必须绑定到本地IP地址和端口上，明确本地半相关
                    break;
                }
              
            }

            serverSocket.Listen(10);//参数backlog指出系统等待用户程序排队的连接数
            label1.Text = "监听已启动 等待客户端接入...";
            
            Control.CheckForIllegalCrossThreadCalls = false;//????书上说遇到线程交叉调用控制问题，要在调用进程之前加上该语句
             myThread = new Thread(ListenClientConnect);//创建一个线程来监听客户端连接
             heartbeat = new Thread(heartbeating);
            // heartbeat2 = new Thread(heartbeating2ing);
             
             myThread.Start();
            heartbeat.Start();//心跳线程
           // heartbeat2.Start();//监视锁端列表
        }
        private void heartbeating()//心跳包监视用户列表
        {
            while(true)
            {
                        
             //   textBox2.AppendText("H");
                #region 监视用户端列表
                if (dic.Count != 0)
                {
                    string[] sKeys = new string[dic.Count];//声明一个数组来存放字典里的可以
                    dic.Keys.CopyTo(sKeys, 0);//将所有字典中所有的键复制到数组里面
                    for (int i = 0; i < dic.Count; i++)
                    {
                        try
                        {
                            dic[sKeys[i]].Send(Encoding.UTF8.GetBytes("areYouAlive?\n"));//之所以加try是因为在遍历字典进行发送的时候如果客户端已经断开就会检测到异常然后就会提示已经断
                        }
                        catch (Exception)
                        {
                            listBox1.Items.Remove(dic[sKeys[i]].RemoteEndPoint.ToString());
                            string socketip = dic[sKeys[i]].RemoteEndPoint.ToString();
                            Savesocket.Remove(dic[sKeys[i]].RemoteEndPoint.ToString());//从socket列表里面移除断开的客户端；
                            dic.Remove(dic[sKeys[i]].RemoteEndPoint.ToString());//从列表里面移除该项 疑问：如果移除了某一项，那么整个列表的索引会不会从0开始重新排布呢？
                                                                                //如果没有重新排布，那么使用索引访问某一项的话会不会有异常？
                            this.label6.Text = Savesocket.Count + "";
                            try
                            {
                                dic[sKeys[i]].Close();//至此完成一个客户端的断开连接
                            }
                            catch (Exception)
                            {

                                textBox1.AppendText("字典里空了？？\n");
                            }


                        }

                        //  j = i;

                    }//for循环结束
                    textBox2.AppendText("有连接，休眠五秒中\n");
                    Thread.Sleep(5000);
                }
                else
                {
                    textBox2.AppendText("当前没有连接\n");
                    textBox2.AppendText("无连接，休眠五秒中\n");
                    Thread.Sleep(5000);


                }
                #endregion
               // 监视锁端列表
               




            }//while 循环结束

        }
        //private void heartbeating2ing()//监视锁列表
        //{
        //    while (true)
        //    {

        //        textBox2.AppendText("H");
        //        if (Lockdic.Count != 0)
        //    {
        //        string[] sKeys = new string[Lockdic.Count];//声明一个数组来存放字典里的可以
        //        Lockdic.Keys.CopyTo(sKeys, 0);//将所有字典中所有的键复制到数组里面
        //        for (int i = 0; i < Lockdic.Count; i++)
        //        {
        //            try
        //            {
        //                Lockdic[sKeys[i]].Send(Encoding.UTF8.GetBytes("areYouAlive?"));//之所以加try是因为在遍历字典进行发送的时候如果客户端已经断开就会检测到异常然后就会提示已经断
        //            }
        //            catch (Exception)
        //            {
        //                listBox2.Items.Remove(Lockdic[sKeys[i]].RemoteEndPoint.ToString());
        //                string socketip = Lockdic[sKeys[i]].RemoteEndPoint.ToString();
        //                Savesocket.Remove(Lockdic[sKeys[i]].RemoteEndPoint.ToString());//从socket列表里面移除断开的客户端；
        //                Lockdic.Remove(Lockdic[sKeys[i]].RemoteEndPoint.ToString());//从列表里面移除该项 疑问：如果移除了某一项，那么整个列表的索引会不会从0开始重新排布呢？
        //                                                                            //如果没有重新排布，那么使用索引访问某一项的话会不会有异常？
        //                this.label6.Text = Savesocket.Count + "";
        //                try
        //                {
        //                    Lockdic[sKeys[i]].Close();//至此完成一个客户端的断开连接
        //                }
        //                catch (Exception)
        //                {

        //                    textBox1.AppendText("字典里空了？？");
        //                }


        //            }

        //            //  j = i;

        //        }//for循环结束
        //        textBox2.AppendText("有连接，休眠五秒中");
        //        Thread.Sleep(5000);

           
        //    }
        //    else
        //    {
        //        textBox2.AppendText("当前没有连接");
        //        textBox2.AppendText("无连接，休眠五秒中");
        //        Thread.Sleep(5000);


        //    }
        //        #endregion
        //    }//while 循环结束
        //}
        private void button1_Click(object sender, EventArgs e)//开始监听
        {
           
        }
        private void ListenClientConnect()//客户端连接监听方法
        {
            Socket newclientSocket = null;//为什么要置空？ 
           
            while (true)
            {
                try
                {
                    newclientSocket = serverSocket.Accept();//执行到此处时候线程处于阻塞状态，一旦有客户请求，accept会产生一个新的套接字newclientSocket用于跟客户端进行真正的数据传输
                    newscoket = newclientSocket;
                    IPAddress clientIp = (newclientSocket.RemoteEndPoint as IPEndPoint).Address;/*这一步用来获取连接到服务器客户端的IP和端口然后强制转换为IPEnd对象*/
                    //获取客户端IP as 操作是用来强制转换，如果转换失败则返回为空

                    int clientPort = (newclientSocket.RemoteEndPoint as IPEndPoint).Port;//获取客户端端口
                    string sendmsg = "YC连接成功！\r\n" + "本地IP:" + clientIp + ",本地端口" + clientPort.ToString();
                    RemoteEndPoint = newclientSocket.RemoteEndPoint.ToString();//获取接入客户端的网络结点
                    textBox1.AppendText("成功与" + RemoteEndPoint + "客户端建立连接！\t\n");
                    label1.Text="接入成功";
                   

                    //
                    
                    Savesocket.Add(RemoteEndPoint);//这是将服务器自身的网络信息 存进列表里  SaveFileSok.Add(RemoteEndPoint + "*" + ReEndFilePoint);
                    this.label6.Text = Savesocket.Count + "";
                    byte[] SendMsg = Encoding.UTF8.GetBytes(sendmsg);

                  //  newclientSocket.Send(SendMsg);

                    //显示连接信息         
                    textBox1.AppendText("当前客户端" + newclientSocket.RemoteEndPoint.ToString() + "\r\n");
                    Thread receiveThread = new Thread(ReceiveMessage);//创建一个线程来跑ReceiveMessage（）
                    //receiveThread.IsBackground = true;//将该线程设为后台线程
                    receiveThread.Start(newclientSocket);//开启线程
                                                         //每循环一次线程就重新开始一次？？？？
                }//try 结束

                catch (Exception)
                {

                    textBox1.AppendText("监听结束");
                    label1.Text="监听结束";
                    break;

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
           /// <summary>
           /// 
           /// </summary>
           /// <param name="info"></param>
        void OnLocklist(string info)
        {
            listBox2.Items.Add(info);
        }
        private void ReceiveMessage(object clientSocket)//接受信息并进行处理反馈，clientSocket为服务器或将转发socket
        {

            textBox1.AppendText("receiveMessage线程启动");
            Socket myClientSocket = (Socket)clientSocket;
            int i = 0;//这个i是用来标记是不是第一次发送信息
            while (true)
            {
                try
                {
                    if (myClientSocket != null && myClientSocket.Connected)
                    {


                        byte[] arrserverMsg = new byte[1024 * 1024];//缓冲区
                        byte[] senmsg = new byte[1024 * 1024];//1M的缓冲
                        try
                        {
                            int receiveNumber = myClientSocket.Receive(arrserverMsg);//将从客户端接收到的数据输入缓冲区 这是阻塞式读取操作：的如果没有可接受的的数据进程执行到此处就会挂起
                            string strRecMsg = Encoding.UTF8.GetString(arrserverMsg, 0, receiveNumber);
                            #region 客户端分类
                            textBox6.AppendText("所有消息："+strRecMsg+"\n");
                            //加进列表之前需要检测是不是有重复的
                            if (strRecMsg.Length>=5&& strRecMsg.Substring(0, 5) == "ilock"&& Lockdic.ContainsKey(RemoteEndPoint)==false)//将锁添加进列表 执行添加之前检测是不是有重复的
                            { 
                                if(Lockdic.ContainsKey(RemoteEndPoint))
                                fenlei = "ilock";
                                Lockdic.Add(RemoteEndPoint, newscoket);//执行添加之前检测是不是有重复的
                                OnLocklist(RemoteEndPoint);
                                
                            }
                            if(strRecMsg.Length >= 5 && strRecMsg.Substring(0, 5) == "ilock")
                            {
                                string[] sKeys = new string[Lockdic.Count];//声明一个数组来存放字典里的可以
                                Lockdic.Keys.CopyTo(sKeys, 0);//将所有字典中所有的键复制到数组里面
                                sendMsg = Encoding.UTF8.GetBytes("ServerCopy");
                                for (int j = 0; j < Lockdic.Count; j++)//向智能锁列表里所有连接发送copy
                                {
                                    Lockdic[sKeys[j]].Send(sendMsg);
                                }
                            }
                            if (strRecMsg.Length==5&& strRecMsg.Substring(0, 5) == "phone"&&dic.ContainsKey(RemoteEndPoint)==false)//将手机添加进用户列表
                            {
                                dic.Add(RemoteEndPoint, newscoket);
                                Onlist(RemoteEndPoint);
                                i++;
                                textBox4.AppendText("用户添加成功！\n");
                            }
                            //if (strRecMsg.Length>=6&&strRecMsg.Substring(0, 6) == "unlock" && i == 0)
                            //{
                            //    dic.Add(RemoteEndPoint, newscoket);
                            //    Onlist(RemoteEndPoint);
                            //    i++;
                            //}

                            if (strRecMsg.Length >= 12 && strRecMsg.Substring(0,12)=="phone@unlock"&&i!=0)
                            {
                                string[] sKeys = new string[Lockdic.Count];//声明一个数组来存放字典里的可以
                                Lockdic.Keys.CopyTo(sKeys, 0);//将所有字典中所有的键复制到数组里面
                                sendMsg = Encoding.UTF8.GetBytes("unlock");
                                for (int j = 0; j < Lockdic.Count; j++)
                                {
                                    Lockdic[sKeys[j]].Send(sendMsg);
                                }
                                textBox4.AppendText("手机消息："+strRecMsg+"\n");
                            }

                            
                            #endregion
                            #region 用户名重名检测请求
                            if (strRecMsg.Length > 9 && strRecMsg.Substring(0, 9) == "namecheck")
                            {
                                string namecheck = strRecMsg.Substring(9, strRecMsg.IndexOf("!") - 9);
                                textBox1.AppendText(namecheck);
                                DB db = new DB();
                                SqlDataReader dr = db.reDr("select nickname from users where nickname='" + namecheck + "'");
                                dr.Read();
                                if (dr.HasRows)
                                {
                                    senmsg = Encoding.UTF8.GetBytes("namecheck_1\n");
                                    myClientSocket.Send(senmsg);
                                    textBox1.AppendText("用户名已经存在\n");
                                }

                                else
                                {
                                    senmsg = Encoding.UTF8.GetBytes("namecheck_0\n");
                                    myClientSocket.Send(senmsg);
                                    textBox1.AppendText("用户可以注册\n");
                                }
                            }
                            #endregion
                            #region  登录请求 原理是添加特定前缀来区分是否需要转发
                            if (strRecMsg.Length > 6 && strRecMsg.Substring(0, 5) == "login")//登录请求 原理是添加特定前缀来区分是否需要转发
                            { //在我的架构中，服务器不需要转发消息，它只需要区分不同种类的请求即可
                                Dohash MD5 = new Dohash();
                                DB db = new DB();
                                // textBox1.AppendText("!!!" + strRecMsg);//输出到日志查看一下内容
                                string user = strRecMsg.Substring(5, strRecMsg.IndexOf("@") - 5);
                                string password = MD5.GetMD5(strRecMsg.Substring(strRecMsg.IndexOf("@") + 1, strRecMsg.IndexOf("!") - strRecMsg.IndexOf("@") - 1));
                                textBox1.AppendText(user);
                                textBox1.AppendText("MD5");
                                textBox1.AppendText(password);
                                //接下来把用户名以及密码嵌入查询字符串进行验证
                                SqlDataReader dr = db.reDr("select nickname,password,Role from users where password='" + password + "' and nickname='" + user + "'");
                                dr.Read();
                                if (dr.HasRows)//通过dr中是否包含行判断用户是否通过身份验证
                                {
                                    //如果通过了验证，那么就向客户端发送 1
                                    sendMsg = Encoding.UTF8.GetBytes("login_1\n");
                                    myClientSocket.Send(sendMsg);
                                    textBox1.AppendText("登录成功！\n");

                                }
                                else
                                {
                                    //如果没有那就发送0
                                    sendMsg = Encoding.UTF8.GetBytes("login_0\n");
                                    myClientSocket.Send(sendMsg);
                                    textBox1.AppendText("登录失败！\n");

                                }
                                dr.Close();

                            }
                            #endregion

                            #region 注册请求
                            if (strRecMsg.Length > 6 && strRecMsg.Substring(0, 6) == "regist")
                            {
                                Dohash MD5 = new Dohash();
                                DB db = new DB();
                                string user = strRecMsg.Substring(6, strRecMsg.IndexOf("@") - 6);
                                string password = MD5.GetMD5(strRecMsg.Substring(strRecMsg.IndexOf("@") + 1, strRecMsg.IndexOf("!") - strRecMsg.IndexOf("@") - 1));
                                string cmdstr = "insert into users(nickname,password) values('" + user + "','" + password +"')";
                                SqlDataReader regi = db.reDr(cmdstr);
                                try
                                {
                                    int reValue = db.sqlEx(cmdstr);
                                    textBox1.AppendText("返回值为："+reValue);
                                    if (reValue == 1)
                                    {
                                        senmsg = Encoding.UTF8.GetBytes("regist_1\n");
                                        myClientSocket.Send(senmsg);
                                        textBox1.AppendText("注册成功！\n");

                                    }
                                    else if (reValue == 0)
                                    {
                                        senmsg = Encoding.UTF8.GetBytes("regist_0\n");
                                        myClientSocket.Send(senmsg);
                                        textBox1.AppendText("注册失败！\n");
                                    }
                                }
                                catch (Exception e)
                                {
                                    MessageBox.Show("存在异常");

                                }
                            }
                            #endregion

                          #region 开锁请求
                            //if (strRecMsg.Length > 0 && strRecMsg.Substring(0, 6) == "unlock")
                            //{
                            //    Dohash MD5 = new Dohash();
                            //    DB db = new DB();
                            //    string cmdstr = "update smartlock set Lockstate='" + "unlock" + "'where LockMAC=B8-86-87-52-9D-B0";
                            //    SqlDataReader regi = db.reDr(cmdstr);
                            //    try
                            //    {
                            //        int reValue = db.sqlEx(cmdstr);
                            //        if (reValue == 1)
                            //        {
                            //            senmsg = Encoding.UTF8.GetBytes("r1");
                            //            myClientSocket.Send(sendMsg);
                            //             cmdstr = "update smartlock set Lockstate='" + "Locking" + "'where LockMAC=B8-86-87-52-9D-B0";
                            //             regi = db.reDr(cmdstr);
                            //        }
                            //        else if (reValue == 0)
                            //        {
                            //            senmsg = Encoding.UTF8.GetBytes("r0");
                            //            myClientSocket.Send(sendMsg);
                            //        }
                            //        textBox4.AppendText(strRecMsg);
                            //    }
                            //    catch (Exception e)
                            //    {
                            //        MessageBox.Show("存在异常");

                            //    }
                            //}
                            #endregion

                            #region 解除绑定



                            #endregion

                            #region 绑定新锁请求
                            if (strRecMsg.Length >=7 && strRecMsg.Substring(0, 7) == "bindmac")
                            {

                                Dohash MD5 = new Dohash();
                                DB db = new DB();
                                string lockmac = strRecMsg.Substring(7, strRecMsg.IndexOf("@") - 7);
                                string password = MD5.GetMD5(strRecMsg.Substring(strRecMsg.IndexOf("@") + 1, strRecMsg.IndexOf("!") - strRecMsg.IndexOf("@") - 1));
                                string LockMAC = "4A-4B-4C-4D-4E-4F";
                                string cmdstr = "insert into users(nickname,password,LockMAC) values('" + password + "','" + LockMAC + "')";//用来插入lockmac记录
                                SqlDataReader regi = db.reDr(cmdstr);
                                try
                                {
                                    int reValue = db.sqlEx(cmdstr);
                                    if (reValue == 1)
                                    {
                                        senmsg = Encoding.UTF8.GetBytes("regist_1\n");
                                        myClientSocket.Send(senmsg);
                                        textBox1.AppendText("绑定成功！\n");

                                    }
                                    else if (reValue == 0)
                                    {
                                        senmsg = Encoding.UTF8.GetBytes("regist_0\n");
                                        myClientSocket.Send(senmsg);
                                        textBox1.AppendText("绑定成功！\n");
                                    }
                                }
                                catch (Exception e)
                                {
                                    MessageBox.Show("存在异常");

                                }

                            }

                            #endregion

                            #region 获取设备列表请求
                            if (strRecMsg.Length >=7 && strRecMsg.Substring(0, 7) == "getlock")
                            {
                                string username = strRecMsg.Substring(7, strRecMsg.IndexOf("!") - 7);

                                DB db = new DB();
                                SqlDataReader dr = db.reDr("select LockMAC from users where nickname='" + username + "'");
                                string lockname = "";
                                while (dr.Read())
                                {
                                    lockname = dr.GetString(dr.GetOrdinal("LockMAC")) + lockname;
                                }

                                textBox1.AppendText(lockname);

                            }
                            #endregion

                            #region 处理智能锁消息

                            if (strRecMsg.Length>=3&&strRecMsg.Substring(0,3)=="ULN")
                                textBox3.AppendText("锁具成功打开并返回："+ strRecMsg.Substring(0, 3)+"\n");
                            if (strRecMsg.Length >= 3 && strRecMsg.Substring(0, 3) == "ULS")
                                textBox3.AppendText("智能锁返回："+strRecMsg+"\n");
                            #endregion

                        }//try结束
                        catch (SocketException e)//当一个客户端关闭的时候
                        {
                            textBox1.AppendText("客户端" + myClientSocket.RemoteEndPoint + "已中断连接" + "\r\n");

                            listBox1.Items.Remove(myClientSocket.RemoteEndPoint.ToString());

                            string socketip = myClientSocket.RemoteEndPoint.ToString();
                            Savesocket.Remove(myClientSocket.RemoteEndPoint.ToString());//从socket列表里面移除断开的客户端；
                            dic.Remove(myClientSocket.RemoteEndPoint.ToString());//从列表里面移除该项 疑问：如果移除了某一项，那么整个列表的索引会不会从0开始重新排布呢？
                            //如果没有重新排布，那么使用索引访问某一项的话会不会有异常？
                            this.label6.Text = Savesocket.Count + "";
                            myClientSocket.Close();//至此完成一个客户端的断开连接
                            break;

                        }//catch结束
                    }//if结束
                    else
                    {
                        break;//如果没有新的连接那么一定要break出来，不然CPU会因为死循环的原因疯跑
                    }
                }
                catch (Exception)
                {
                    //抓到一个异常：System.ArgumentException”类型的未经处理的异常在 smartlockserver.exe 中发生 

                   // 其他信息: 已添加了具有相同键的项。
                  // “System.ArgumentOutOfRangeException”类型的未经处理的异常在 smartlockserver.exe 中发生

                //其他信息: 索引和长度必须引用该字符串内的位置。
                    throw;
                   // break;
                }
               
            }//while循环结束            
        }//消息接收方法结束
        string gavelist()
        {
            string list = "";

            for (int i = 0; i < Savesocket.Count; i++)
            {
                list += Savesocket[i] + "//";//将每个socket信息都加了“//”后缀存到字符串list里
            }

            return list;
        }
        private DateTime GetCurrentTime()//获取当前时间
        {
            DateTime currentTime = new DateTime();
            currentTime = DateTime.Now;
            return currentTime;
        }//获取当前时间结束

        private void SLserver_FormClosing(object sender, FormClosingEventArgs e)//关闭后清理线程
        {

            Process p = Process.GetCurrentProcess();
            if (p != null)
            {
                //MessageBox.Show("存在驻留线程");
                p.Kill();

            }

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
           
            
       
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (serverSocket.Connected)
            {
                serverSocket.Shutdown(SocketShutdown.Both);
                serverSocket.Close();

                foreach (Socket i in dic.Values)
                {
                    if (i.Connected)
                    {
                        i.Close();

                    }
                }
              //  this.button1.Enabled = true;

            }
            else
            {
                serverSocket.Close();
                this.textBox1.AppendText("当前没有连接\n");
                //button1.Enabled = true;
               // button2.Enabled = false;
            }
        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string[] sKeys = new string[Lockdic.Count];//声明一个数组来存放字典里的可以
            Lockdic.Keys.CopyTo(sKeys, 0);//将所有字典中所有的键复制到数组里面
            sendMsg = Encoding.UTF8.GetBytes(textBox5.Text);
            for (int j = 0; j < Lockdic.Count; j++)
            {
                Lockdic[sKeys[j]].Send(sendMsg);
            }
            textBox4.AppendText("服务器消息测试:" + textBox5.Text + "\n");
        }

      
    }
}
//#endregion