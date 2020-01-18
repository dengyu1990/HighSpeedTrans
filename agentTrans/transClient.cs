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

namespace agentTrans
{
    public partial class transClient : Form
    {
        public transClient()
        {
            InitializeComponent();
        }

        public Socket ClientSocket { get; set; }

        private void btnConn_Click(object sender, EventArgs e)
        {
            //客户端连接服务器端,创建Socket对象
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ClientSocket = socket;
            //连接服务端即可
            try
            {
                socket.Connect(IPAddress.Parse(txtIP.Text), int.Parse(txtPort.Text));
            }
            catch (Exception ex)
            {
                //Thread.Sleep(500);
                //btnConn_Click(this, e);
                MessageBox.Show("服务器传输通道验证失败!" + ex.ToString());
                return;
            }
            //发送与接受消息
            Thread thread = new Thread(new ParameterizedThreadStart(ReceiveData));
            thread.IsBackground = true;
            thread.Start(ClientSocket);
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (ClientSocket.Connected)
            {
                byte[] data = Encoding.Default.GetBytes(txtMsg.Text);
                ClientSocket.Send(data, 0, data.Length, SocketFlags.None);
            }
        }

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
                    AppendToLOG(string.Format("服务端：{0}异常退出,{1}", proxSocket.RemoteEndPoint.ToString(),ex.ToString()));
                    StopConnect();
                    return;
                }
                if (len <= 0)
                {
                    //客户端正常退出
                    AppendToLOG(string.Format("服务端：{0}正常退出", proxSocket.RemoteEndPoint.ToString()));
                    StopConnect();
                    return; //让方法结束，终结当前接受客户端数据的异步线程
                }
                //把接受到的数据放到文本框输出上
                string str = Encoding.Default.GetString(data, 0, len);
                AppendToLOG(string.Format("接受到服务端：{0}的消息是：{1}", proxSocket.RemoteEndPoint.ToString(), str));
            }
        }

        private void StopConnect()
        {
            try
            {
                if (ClientSocket.Connected)
                {
                    ClientSocket.Shutdown(SocketShutdown.Both);
                    ClientSocket.Close(100); //如果100秒还没关闭就强制关闭
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
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
            else
            {
                this.rtxtLog.Text = string.Format("{0}\r\n{1}", str, rtxtLog.Text);
            }
        }

        private void transClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            //判断是否已连接，如果连接就关闭
            StopConnect();
        }
    }
}
