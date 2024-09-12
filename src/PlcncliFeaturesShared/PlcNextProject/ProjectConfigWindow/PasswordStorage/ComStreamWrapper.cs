#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.IO;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;

namespace PlcncliFeatures.PlcNextProject.ProjectConfigWindow
{
    internal class ComStreamWrapper : Stream
    {
        private readonly IStream originalStream;

        public ComStreamWrapper(IStream originalStream)
        {
            this.originalStream = originalStream;
        }

        public override bool CanSeek => false;

        public override bool CanRead => true;

        public override bool CanWrite => true;

        public override bool CanTimeout => false;

        public override long Length => throw new NotImplementedException();

        public override long Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            byte[] internalBuffer = new byte[count];
            this.originalStream.Read(internalBuffer, (uint)count, out uint read);

            Array.Copy(internalBuffer, 0, buffer, offset, read);

            return (int)read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            int remaining = count;
            int currentOffset = offset;

            do
            {
                byte[] internalBuffer = new byte[remaining];
                Array.Copy(buffer, currentOffset, internalBuffer, 0, count);

                this.originalStream.Write(internalBuffer, (uint)remaining, out uint written);

                remaining -= (int)written;
                currentOffset += (int)written;
            }
            while (remaining > 0);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Flush()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.originalStream.Commit(0);
        }
    }
}
