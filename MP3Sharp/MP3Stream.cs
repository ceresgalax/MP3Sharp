// /***************************************************************************
//  * MP3Stream.cs
//  * Copyright (c) 2015 the authors.
//  *
//  * All rights reserved. This program and the accompanying materials
//  * are made available under the terms of the GNU Lesser General Public License
//  * (LGPL) version 3 which accompanies this distribution, and is available at
//  * https://www.gnu.org/licenses/lgpl-3.0.en.html
//  *
//  * This library is distributed in the hope that it will be useful,
//  * but WITHOUT ANY WARRANTY; without even the implied warranty of
//  * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  * Lesser General Public License for more details.
//  *
//  ***************************************************************************/

using System;
using System.IO;
using MP3Sharp.Decoding;

namespace MP3Sharp
{
    /// <summary>
    ///     Provides a view of the sequence of bytes that are produced during the conversion of an MP3 stream
    ///     into a 16-bit PCM-encoded ("WAV" format) stream.
    /// </summary>
    public class MP3Stream : Stream
    {
        // Used to interface with JavaZoom code.
        private readonly Bitstream m_BitStream;
        private readonly Decoder m_Decoder = new Decoder(Decoder.DefaultParams);
        // local variables.
        private readonly IStreamBuffer m_Buffer;
        private readonly Stream m_SourceStream;
        private readonly int m_BackStreamByteCountRep = 0;
        private short m_ChannelCountRep = -1;
        protected SoundFormat FormatRep;
        private int m_FrequencyRep = -1;

        public bool IsEOF
        {
            get;
            protected set;
		}

        /// <summary>
        ///     Creates a new stream instance using the provided filename, and the default chunk size of 4096 bytes.
        /// </summary>
        public MP3Stream(string fileName)
            : this(new FileStream(fileName, FileMode.Open))
        {
        }

        /// <summary>
        ///     Creates a new stream instance using the provided filename and chunk size.
        /// </summary>
        public MP3Stream(string fileName, int chunkSize)
            : this(new FileStream(fileName, FileMode.Open), chunkSize)
        {
        }

        /// <summary>
        ///     Creates a new stream instance using the provided stream as a source, and the default chunk size of 4096 bytes.
        /// </summary>
        public MP3Stream(Stream sourceStream)
            : this(sourceStream, 4096)
        {
        }

        /// <summary>
        ///     Creates a new stream instance using the provided stream as a source.
        ///     Will also read the first frame of the MP3 into the internal buffer.
        ///     TODO: allow selecting stereo or mono in the constructor (note that this also requires "implementing" the stereo format).
        /// </summary>
        public MP3Stream(Stream sourceStream, int chunkSize)
        {
            IsEOF = false;
            FormatRep = SoundFormat.Pcm16BitStereo;
            m_SourceStream = sourceStream;
            m_BitStream = new Bitstream(new PushbackStream(m_SourceStream, chunkSize));
            
            // Read a frame from the bitstream.
            Header header = m_BitStream.readFrame();
            if (header == null)
            {
                IsEOF = true;
            } 
            else
            {
                // Set the channel count and frequency values for the stream.
                if (header.mode() == Header.SINGLE_CHANNEL) 
                {
                    m_ChannelCountRep = 1;
                    m_Buffer = new Buffer16BitMono();
                } 
                else 
                {
                    m_ChannelCountRep = 2;
                    m_Buffer = new Buffer16BitStereo();
                }

                m_FrequencyRep = header.frequency();
                
                // Rewind so that the first frame can be read with the decoder. 
                m_BitStream.unreadFrame();
            }
            
            m_Decoder.OutputBuffer = m_Buffer;
        }

        /// <summary>
        ///     Gets the chunk size.
        /// </summary>
        public int ChunkSize => m_BackStreamByteCountRep;

        /// <summary>
        ///     Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead => m_SourceStream.CanRead;

        /// <summary>
        ///     Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek => m_SourceStream.CanSeek;

        /// <summary>
        ///     Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite => m_SourceStream.CanWrite;

        /// <summary>
        ///     Gets the length in bytes of the stream.
        /// </summary>
        public override long Length => m_SourceStream.Length;

        /// <summary>
        ///     Gets or sets the position of the source stream.  This is relative to the number of bytes in the MP3 file, rather
        ///     than the total number of PCM bytes (typically signicantly greater) contained in the Mp3Stream's output.
        /// </summary>
        public override long Position
        {
            get => m_SourceStream.Position;
            set => m_SourceStream.Position = value;
        }

        /// <summary>
        ///     Gets the frequency of the audio being decoded. Updated every call to Read() or DecodeFrames(),
        ///     to reflect the most recent header information from the MP3 Stream.
        /// </summary>
        public int Frequency => m_FrequencyRep;

        /// <summary>
        ///     Gets the number of channels available in the audio being decoded. Updated every call to Read() or DecodeFrames(),
        ///     to reflect the most recent header information from the MP3 Stream.
        /// </summary>
        public short ChannelCount => m_ChannelCountRep;

        /// <summary>
        ///     Gets the PCM output format of this stream.
        /// </summary>
        public SoundFormat Format => FormatRep;

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            m_SourceStream.Flush();
        }

        /// <summary>
        ///     Sets the position of the source stream.
        /// </summary>
        public override long Seek(long pos, SeekOrigin origin)
        {
            return m_SourceStream.Seek(pos, origin);
        }

        /// <summary>
        ///     This method is not valid for an Mp3Stream.
        /// </summary>
        public override void SetLength(long len)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        ///     This method is not valid for an Mp3Stream.
        /// </summary>
        public override void Write(byte[] buf, int ofs, int count)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        ///     Decodes the requested number of frames from the MP3 stream and caches their PCM-encoded bytes.
        ///     These can subsequently be obtained using the Read method.
        ///     Returns the number of frames that were successfully decoded.
        /// </summary>
        public int DecodeFrames(int frameCount)
        {
            int framesDecoded = 0;
            bool aFrameWasRead = true;
            while (framesDecoded < frameCount && aFrameWasRead)
            {
                aFrameWasRead = ReadFrame();
                if (aFrameWasRead) framesDecoded++;
            }
            return framesDecoded;
        }

        /// <summary>
        ///     Reads the MP3 stream as PCM-encoded bytes.  Decodes a portion of the stream if necessary.
        ///     Returns the number of bytes read.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // Copy from queue buffers, reading new ones as necessary,
            // until we can't read more or we have read "count" bytes
            if (IsEOF)
                return 0;

            int bytesRead = 0;
            while (true)
            {
                if (m_Buffer.BytesLeft <= 0)
                {
                    if (!ReadFrame()) // out of frames or end of stream?
                    {
                        IsEOF = true;
                        break;
                    }
                }

                // Copy as much as we can from the current buffer:
                bytesRead += m_Buffer.Read(buffer,
                    offset + bytesRead,
                    count - bytesRead);

                if (bytesRead >= count)
                    break;
            }
            return bytesRead;
        }

        /// <summary>
        ///     Closes the source stream and releases any associated resources.
        ///     If you don't call this, you may be leaking file descriptors.
        /// </summary>
        public override void Close()
        {
            m_BitStream.close(); // This should close SourceStream as well.
        }

        /// <summary>
        ///     Reads a frame from the MP3 stream.  Returns whether the operation was successful.  If it wasn't,
        ///     the source stream is probably at its end.
        /// </summary>
        private bool ReadFrame()
        {
            // Read a frame from the bitstream.
            Header header = m_BitStream.readFrame();
            if (header == null)
                return false;

            try
            {
                // Decode the frame.
                ABuffer decoderOutput = m_Decoder.DecodeFrame(header, m_BitStream);

                // Apparently, the way JavaZoom sets the output buffer
                // on the decoder is a bit dodgy. Even though
                // this exception should never happen, we test to be sure.
                if (decoderOutput != m_Buffer)
                    throw new ApplicationException("Output buffers are different.");

                // And we're done.
            }
            finally
            {
                // No resource leaks please!
                m_BitStream.CloseFrame();
            }
            return true;
        }
    }
}