﻿using CoreGraphics;
using ObjCRuntime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CGStreamDataProvider
{
    /// <summary>
    /// Creates <see cref="CGDataProvider"/> from <see cref="Stream"/>.
    /// </summary>
    public class CGStreamDataProvider
    {
        /// <summary>
        /// Creates <see cref="CGDataProvider"/> from <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">Original data stream.</param>
        /// <param name="ownStream"><c>true</c> to instruct <see cref="CGDataProvider"/> object to close the stream on its cleanup.</param>
        /// <param name="bufferingSize">Buffering size.</param>
        /// <returns></returns>
        public static CGDataProvider CreateWithStream(Stream stream, bool ownStream, int bufferingSize = 1024 * 512)
        {
            if (stream == null)
                throw new ArgumentNullException("stream must not be null.", "stream");
            if (!stream.CanSeek || !stream.CanRead)
                throw new ArgumentException("stream is not seekable/readable.", "stream");
            if (bufferingSize <= 0)
                throw new ArgumentException("bufferingSize should be positive at least.", "bufferingSize");

            var callbacks = new CGDataProviderCallbacks
            {
                getBytes = (info, buffer, count) =>
                {
                    var buf = new byte[bufferingSize];
                    long bytesRead = 0;
                    long bytesToRead = count.ToInt64();
                    while (bytesToRead > 0)
                    {
                        var ret = stream.Read(buf, 0, (int)Math.Min(bytesToRead, buf.Length));
                        if (ret == 0)
                            break;

                        Marshal.Copy(buf, 0, buffer, ret);
                        buffer += ret;
                        bytesRead += ret;
                        bytesToRead -= ret;
                    }

                    return new IntPtr(bytesRead);
                },
                skipBytes = (info, count) =>
                {
                    stream.Position += count.ToInt64();
                },
                rewind = info =>
                {
                    stream.Position = 0;
                },
                releaseProvider = info =>
                {
                    if (ownStream)
                        stream.Dispose();
                }
            };
            return new CoreGraphics.CGDataProvider(CGDataProviderCreateSequential(IntPtr.Zero, callbacks));
        }

        /// <summary>
        /// Creates <see cref="CGDataProvider"/> from <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">Original data stream.</param>
        /// <param name="streamLength">Data stream length in bytes.</param>
        /// <param name="ownStream"><c>true</c> to instruct <see cref="CGDataProvider"/> object to close the stream on its cleanup.</param>
        /// <param name="bufferingSize">Buffering size.</param>
        /// <returns></returns>
        public static CGDataProvider CreateWithStreamWithLength(Stream stream, long streamLength, bool ownStream, int bufferingSize = 1024 * 512)
        {
            if (stream == null)
                throw new ArgumentNullException("stream must not be null.", "stream");
            if (!stream.CanSeek || !stream.CanRead)
                throw new ArgumentException("stream is not seekable/readable.", "stream");

            var callbacks = new CGDataProviderDirectCallbacks
            {
                version = 0,
                getBytePointer = null,
                releaseBytePointer = null,
                getBytesAtPosition = (info, buffer, position, count) =>
                {
                    stream.Position = position.ToInt64();

                    var buf = new byte[bufferingSize];
                    long bytesRead = 0;
                    long bytesToRead = count.ToInt64();
                    while (bytesToRead > 0)
                    {
                        var ret = stream.Read(buf, 0, (int)Math.Min(bytesToRead, buf.Length));
                        if (ret == 0)
                            break;

                        Marshal.Copy(buf, 0, buffer, ret);
                        buffer += ret;
                        bytesRead += ret;
                        bytesToRead -= ret;
                    }

                    return new IntPtr(bytesRead);
                },
                releaseInfo = info =>
                {
                    if (ownStream)
                        stream.Dispose();
                }
            };
            return new CoreGraphics.CGDataProvider(CGDataProviderCreateDirect(IntPtr.Zero, new IntPtr(streamLength), callbacks));
        }

        [DllImport(Constants.CoreGraphicsLibrary)]
        static extern IntPtr CGDataProviderCreateSequential(IntPtr info, CGDataProviderCallbacks callbacks);

        [DllImport(Constants.CoreGraphicsLibrary)]
        static extern IntPtr CGDataProviderCreateDirect(IntPtr info, IntPtr size, CGDataProviderDirectCallbacks callbacks);

        struct CGDataProviderCallbacks
        {
            public CGDataProviderGetBytesCallback getBytes;
            public CGDataProviderSkipBytesCallback skipBytes;
            public CGDataProviderRewindCallback rewind;
            public CGDataProviderReleaseInfoCallback releaseProvider;
        }

        struct CGDataProviderDirectCallbacks
        {
            public int version;
            public CGDataProviderGetBytePointerCallback getBytePointer;
            public CGDataProviderReleaseBytePointerCallback releaseBytePointer;
            public CGDataProviderGetBytesAtPositionCallback getBytesAtPosition;
            public CGDataProviderReleaseInfoCallback releaseInfo;
        };

        delegate IntPtr CGDataProviderGetBytesCallback(IntPtr info, IntPtr buffer, IntPtr count);
        delegate void CGDataProviderSkipBytesCallback(IntPtr info, IntPtr count);
        delegate void CGDataProviderRewindCallback(IntPtr info);
        delegate void CGDataProviderReleaseInfoCallback(IntPtr info);

        delegate IntPtr CGDataProviderGetBytePointerCallback(IntPtr info);
        delegate void CGDataProviderReleaseBytePointerCallback(IntPtr info, IntPtr pointer);
        delegate IntPtr CGDataProviderGetBytesAtPositionCallback(IntPtr info, IntPtr buffer, IntPtr position, IntPtr count);
    }
}
