using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Minicast.App.MiniCap
{
    /// <summary>
    /// stream of device's screeen.
    /// </summary>
    public class MiniCapStream
    {
        /// <summary>
        /// Socket connection.
        /// </summary>
        private readonly Socket _socket;

        /// <summary>
        /// Data chunk.
        /// </summary>
        private readonly byte[] _chunk;

        /// <summary>
        /// Device IP.
        /// </summary>
        public string IP { get; set; }

        /// <summary>
        /// Forward port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Update event.
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
        /// Extract sub-array of the byte array.
        /// </summary>
        /// <param name="bytes">Original array.</param>
        /// <param name="start">Start position.</param>
        /// <param name="end">End position.</param>
        /// <returns>the sub-array of the byte array.</returns>
        private static byte[] SubByteArray(byte[] bytes, int start, int end)
        {
            var len       = end - start;
            var tempBytes = new byte[len];
            Buffer.BlockCopy(bytes, start, tempBytes, 0, len);

            return tempBytes;
        }

        public ConcurrentQueue<byte[]> ImageByteQueue { get; }

        public MiniCapBanner Banner { get; }

        /// <summary>
        /// Read image from stream.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
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
                    // Read the banner info.
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
                    // Read the length of the image. The first four bytes is the length.
                    else if (readFrameBytes < 4)
                    {
                        frameBodyLength += (_chunk[cursor] << (readFrameBytes * 8));
                        cursor          += 1;
                        readFrameBytes  += 1;
                    }
                    else
                    {
                        // Read the image content.
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
        /// Writhe the image stream to the queue and notify the listener to update.
        /// </summary>
        /// <param name="frameBody">The frame body.</param>
        public void AddStream(byte[] frameBody)
        {
            ImageByteQueue.Enqueue(frameBody);

            // Notify subscribers.
            Update?.Invoke();
        }

        /// <summary>
        /// Close minicap.
        /// </summary>
        public void Close()
        {
            _socket.Disconnect(false);
            _socket.Close();
            _socket.Dispose();
        }
    }
}