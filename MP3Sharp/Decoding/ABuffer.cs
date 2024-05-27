// /***************************************************************************
//  * ABuffer.cs
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
namespace MP3Sharp.Decoding
{
    /// <summary>
    /// A buffer that a <see cref="Decoder"/> can output to.
    /// </summary>
    internal interface ABuffer
    {
        /// <summary>
        ///     Takes a 16 Bit PCM sample.
        /// </summary>
        void Append(int channel, short valueRenamed);

        /// <summary>
        ///     Accepts 32 new PCM samples.
        /// </summary>
        void AppendSamples(int channel, float[] f);
        
        /// <summary>
        ///     Write the samples to the file or directly to the audio hardware.
        /// </summary>
        void WriteBuffer(int val);

        void Close();

        /// <summary>
        ///     Clears all data in the buffer (for seeking).
        /// </summary>
        void ClearBuffer();

        /// <summary>
        ///     Notify the buffer that the user has stopped the stream.
        /// </summary>
        void SetStopFlag();
    }
    
    internal static class ABufferUtil
    {
        public const int OBUFFERSIZE = 2*1152; // max. 2 * 1152 samples per frame
        public const int MAXCHANNELS = 2; // max. number of channels
        
        /// <summary>
        ///     Accepts 32 new PCM samples.
        /// </summary>
        internal static void DefaultAppendSamples(this ABuffer buffer, int channel, float[] f)
        {
            for (int i = 0; i < 32; i++)
            {
                buffer.Append(channel, Clip((f[i])));
            }
        }
        
        /// <summary>
        ///     Clip Sample to 16 Bits
        /// </summary>
        private static short Clip(float sample)
        {
            return ((sample > 32767.0f) ? (short) 32767 : ((sample < -32768.0f) ? (short) -32768 : (short) sample));
        }
    }
}