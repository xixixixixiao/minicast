using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Minicast.App.MiniCap
{
    /// <summary>
    /// 设备屏幕流.
    /// </summary>
    public class MiniCapStream
    {
        /// <summary>
        /// socket 连接.
        /// </summary>
        private readonly Socket _socket;

        /// <summary>
        /// 数据块.
        /// </summary>
        private readonly byte[] _chunk;

        /// <summary>
        /// 设备 IP.
        /// </summary>
        public string IP { get; set; }

        /// <summary>
        /// 转发端口.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 更新事件.
        /// </summary>
        public event Action Update;

        public MiniCapStream(int port)
        {
            this.Port           = port;
            this._chunk         = new byte[4096];
            this.IP             = "127.0.0.1";
            this.ImageByteQueue = new ConcurrentQueue<byte[]>();
            this.Banner         = new MiniCapBanner();

            this._socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this._socket.Connect(new IPEndPoint(IPAddress.Parse(this.IP), this.Port));
        }

        /// <summary>
        /// 用于提取byte数组
        /// </summary>
        /// <param name="bytes">源数组</param>
        /// <param name="start">起始位置</param>
        /// <param name="end">结束位置</param>
        /// <returns>提取后的数组</returns>
        private static byte[] SubByteArray(byte[] bytes, int start, int end)
        {
            var len       = end - start;
            var tempBytes = new byte[len];
            Buffer.BlockCopy(bytes, start, tempBytes, 0, len);

            return tempBytes;
        }

        public ConcurrentQueue<byte[]> ImageByteQueue { get; }

        public MiniCapBanner Banner { get; }

        public void ReadImageStream(CancellationToken token)
        {
            var readBannerBytes = 0;
            var bannerLength    = 2;
            var readFrameBytes  = 0;
            var frameBodyLength = 0;
            var frameBody       = new byte[0];

            int realLength;

            while ((realLength = _socket.Receive(_chunk)) != 0 && !token.IsCancellationRequested)
            {
                for (int cursor = 0, len = realLength; cursor < len;)
                {
                    //读取banner信息
                    if (readBannerBytes < bannerLength)
                    {
                        switch (readBannerBytes)
                        {
                            case 0:
                                Banner.Version = _chunk[cursor];

                                break;
                            case 1:
                                Banner.Length = bannerLength = _chunk[cursor];

                                break;
                            case 2:
                            case 3:
                            case 4:
                            case 5:
                                Banner.Pid += _chunk[cursor] << ((cursor - 2) * 8);

                                break;
                            case 6:
                            case 7:
                            case 8:
                            case 9:
                                Banner.RealWidth += _chunk[cursor] << ((cursor - 6) * 8);

                                break;
                            case 10:
                            case 11:
                            case 12:
                            case 13:
                                Banner.RealHeight += _chunk[cursor] << ((cursor - 10) * 8);

                                break;
                            case 14:
                            case 15:
                            case 16:
                            case 17:
                                Banner.VirtualWidth += _chunk[cursor] << ((cursor - 14) * 8);

                                break;
                            case 18:
                            case 19:
                            case 20:
                            case 21:
                                Banner.VirtualHeight += _chunk[cursor] << ((cursor - 2) * 8);

                                break;
                            case 22:
                                Banner.Orientation += _chunk[cursor] * 90;

                                break;
                            case 23:
                                Banner.Quirks = _chunk[cursor];

                                break;
                        }

                        cursor          += 1;
                        readBannerBytes += 1;
                    }
                    //读取每张图片的头4个字节(图片大小)
                    else if (readFrameBytes < 4)
                    {
                        frameBodyLength += (_chunk[cursor] << (readFrameBytes * 8));
                        cursor          += 1;
                        readFrameBytes  += 1;
                    }
                    else
                    {
                        //读取图片内容
                        if (len - cursor >= frameBodyLength)
                        {
                            frameBody = frameBody.Concat(SubByteArray(_chunk, cursor, cursor + frameBodyLength))
                                                 .ToArray();
                            AddStream(frameBody);
                            cursor          += frameBodyLength;
                            frameBodyLength =  readFrameBytes = 0;
                            frameBody       =  new byte[0];
                        }
                        else
                        {
                            frameBody       =  frameBody.Concat(SubByteArray(_chunk, cursor, len)).ToArray();
                            frameBodyLength -= len - cursor;
                            readFrameBytes  += len - cursor;
                            cursor          =  len;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 写入图片流到队列，并通知监听器更新对象
        /// </summary>
        /// <param name="frameBody">帧体</param>
        public void AddStream(byte[] frameBody)
        {
            ImageByteQueue.Enqueue(frameBody);

            // 使用事件来通知给订阅者
            Update?.Invoke();
        }

        /// <summary>
        /// 关闭 minicap.
        /// </summary>
        public void Close()
        {
            _socket.Disconnect(false);
            _socket.Close();
            _socket.Dispose();
        }
    }
}