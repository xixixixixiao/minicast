using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Minicast.App.MiniTouch
{
    /// <summary>
    /// 设备操作流.
    /// </summary>
    public class MiniTouchStream
    {
        /// <summary>
        /// socket 连接.
        /// </summary>
        private readonly Socket _socket;

        /// <summary>
        /// 传输数据块.
        /// </summary>
        private byte[] _chunk;

        /// <summary>
        /// 设备 x 坐标.
        /// </summary>
        private int _deviceX;

        /// <summary>
        /// 设备 y 坐标.
        /// </summary>
        private int _deviceY;

        /// <summary>
        /// 设备 IP
        /// </summary>
        public string IP { get; set; }

        /// <summary>
        /// 设备投影流端口
        /// </summary>
        public int Port { get; set; }

        private readonly MiniTouchBanner _banner;

        /// <summary>
        /// 投影客户端宽度.
        /// </summary>
        public int ClientWidth { get; set; }

        /// <summary>
        /// 投影客户端高度.
        /// </summary>
        public int ClientHeight { get; set; }

        /// <summary>
        /// 点击 x 轴.
        /// </summary>
        public int PointX { get; set; }

        /// <summary>
        /// 点击 y 轴.
        /// </summary>
        public int PointY { get; set; }

        public MiniTouchStream(int port)
        {
            this.IP   = "127.0.0.1";
            this.Port = port;

            this._socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this._socket.Connect(new IPEndPoint(IPAddress.Parse(IP), Port));
            _banner = new MiniTouchBanner();
            this.ParseTouchBanner(this._socket);
        }

        /// <summary>
        /// 解析 TouchBanner 信息.
        /// </summary>
        /// <param name="socket">数据流.</param>
        private void ParseTouchBanner(Socket socket)
        {
            this._chunk = new byte[64];
            socket.Receive(_chunk);

            var result = Encoding.Default.GetString(_chunk)
                                 .Split('\n', ' ')
                                 .ToArray();

            try
            {
                /**
                 * 读取banner数据.
                 */
                this._banner.Version     = Convert.ToInt32(result[1]);
                this._banner.MaxContacts = Convert.ToInt32(result[3]);
                this._banner.MaxX        = Convert.ToInt32(result[4]);
                this._banner.MaxY        = Convert.ToInt32(result[5]);
                this._banner.MaxPressure = Convert.ToInt32(result[6]);
                this._banner.Pid         = Convert.ToInt32(result[8]);
            }
            catch (IndexOutOfRangeException e)
            {
                throw new IndexOutOfRangeException($@"读取 Banner 数据出错, 原因: {e.Message}");
            }
        }

        /// <summary>
        /// 执行按下操作
        /// </summary>
        public void TapDown()
        {
            //转换为设备的真实坐标
            ScalePoint();

            //通过minitouch命令执行点击;传递的文本'd'为点击命令，0为触摸点索引，X Y 为具体的坐标值，50为压力值，注意必须以\n结尾，否则无法触发动作
            Execute($"d 0 {this._deviceX.ToString()} {this._deviceY.ToString()} 50\n");
        }

        /// <summary>
        /// 执行抬起操作
        /// </summary>
        public void TapUp()
        {
            Execute($"u 0\n");
        }

        /// <summary>
        /// 滑动操作
        /// </summary>
        public void Swipe()
        {
            //转换为设备的真实坐标
            ScalePoint();

            //通过minitouch命令执行划动;传递的文本'd'为划动命令，0为触摸点索引，X Y 为要滑动到的坐标值，50为压力值，注意必须以\n结尾，否则无法触发动作
            Execute($"m 0 {this._deviceX.ToString()} {this._deviceY.ToString()} 50\n");
        }

        /// <summary>
        /// 坐标缩放转换
        /// </summary>
        /// <returns></returns>
        private void ScalePoint()
        {
            this._deviceX = (int) ((double) this.PointX / this.ClientWidth  * this._banner.MaxX);
            this._deviceY = (int) ((double) this.PointY / this.ClientHeight * this._banner.MaxY);
        }

        /// <summary>
        /// 执行模拟动作
        /// </summary>
        /// <param name="command">动作命令</param>
        public void Execute(string command)
        {
            //将动作数据转换为socket要提交的byte数据
            var inputBuffer = Encoding.ASCII.GetBytes(command);

            //发送socket数据
            this._socket.Send(inputBuffer);

            //提交触摸动作的命令
            const string submit = "c\n";
            inputBuffer = Encoding.ASCII.GetBytes(submit);

            //发送socket数据确认触摸动作的执行
            this._socket.Send(inputBuffer);
        }

        /// <summary>
        /// 关闭 minitouch.
        /// </summary>
        public void Close()
        {
            _socket.Disconnect(false);
            _socket.Close();
            _socket.Dispose();
        }
    }
}