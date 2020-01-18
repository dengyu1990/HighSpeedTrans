using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace msgTrans
{
    public partial class transServer : Form
    {
        public transServer()
        {
            InitializeComponent();
        }

        List<Socket> ClientProxSocketList = new List<Socket>();

        private void btnStart_Click(object sender, EventArgs e)
        {
            //创建Socket对象
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //绑定IP和端口
            socket.Bind(new IPEndPoint(IPAddress.Parse(txtIP.Text), int.Parse(txtPort.Text)));
            //开启侦听
            socket.Listen(10); //连接等待队列（同时来了100个连接，只能处理一个，队列里10个等待，其余返回错误）
            //开始接受客户端的连接
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.AcceptClientConnect), socket);

        }
        //接受消息
        public void AcceptClientConnect(object socket)
        {
            var serverSocket = socket as Socket;
            this.AppendToLOG("服务端开始接受客户端的连接");
            while (true)
            {
                var proxSocket = serverSocket.Accept();
                this.AppendToLOG(string.Format("客户端：{0}已连接上", proxSocket.RemoteEndPoint.ToString()));
                ClientProxSocketList.Add(proxSocket);
                //不停地接受当前连接的客户端发送来的消息
                ThreadPool.QueueUserWorkItem(new WaitCallback(ReceiveData), proxSocket);
            }
        }

        #region 发送字符串类型
        //发送消息
        private void btnSend_Click(object sender, EventArgs e)
        {
            foreach (var proxSocket in ClientProxSocketList)
            {
                if (proxSocket.Connected)
                {
                    byte[] data = Encoding.Default.GetBytes(txtMsg.Text);
                    proxSocket.Send(data, 0, data.Length, SocketFlags.None);
                }
            }
        }
        #endregion

        public void ReceiveData(object socket)
        {
            var proxSocket = socket as Socket;
            byte[] data = new byte[1024 * 1024];
            while (true)
            {
                int len = 0;
                try
                {
                    len = proxSocket.Receive(data, 0, data.Length, SocketFlags.None);
                }
                catch (Exception ex)
                {
                    //异常退出
                    AppendToLOG(string.Format("客户端：{0}异常退出,{1}", proxSocket.RemoteEndPoint.ToString(),ex.ToString()));
                    ClientProxSocketList.Remove(proxSocket);
                    StopConnect(proxSocket);
                    return; 

                }
                if (len <= 0)
                {
                    //客户端正常退出
                    AppendToLOG(string.Format("客户端：{0}正常退出", proxSocket.RemoteEndPoint.ToString()));
                    ClientProxSocketList.Remove(proxSocket);
                    StopConnect(proxSocket);
                    return; //让方法结束，终结当前接受客户端数据的异步线程
                }
                //把接受到的数据放到文本框输出上
                string str = Encoding.Default.GetString(data, 0, len);
                AppendToLOG(string.Format("接受到客户端：{0}的消息是：{1}", proxSocket.RemoteEndPoint.ToString(), str));
            }
        }

        private void StopConnect(Socket proxSocket)
        {
            try
            {
                if (proxSocket.Connected)
                {
                    proxSocket.Shutdown(SocketShutdown.Both);
                    proxSocket.Close(100);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("停止连接失败，{0}", ex.ToString());
            }
        }

        //往日志输出框里追加数据的
        public void AppendToLOG(string str)
        {
            if (rtxtLog.InvokeRequired)  //
            {
                rtxtLog.Invoke(new Action<string>(s =>   //去找文本框所在线程去执行相应方法
                {
                    this.rtxtLog.Text = string.Format("{0}\r\n{1}", s, rtxtLog.Text);
                }), str);
            }
            else {
                this.rtxtLog.Text = string.Format("{0}\r\n{1}", str, rtxtLog.Text);
            }
            
        }
    }
}
