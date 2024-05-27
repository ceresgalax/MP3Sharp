using MP3Sharp.Decoding;

namespace MP3Sharp
{
    /// <summary>
    /// An <see cref="ABuffer"/> suitable for being used by an <see cref="MP3Stream"/> for buffering output from its decoder.
    /// </summary>
    internal interface IStreamBuffer : ABuffer
    {
        int BytesLeft { get; }

        /// <summary>
        ///     Reads a sequence of bytes from the buffer and advances the position of the 
        ///     buffer by the number of bytes read.
        /// </summary>
        /// <returns>
        ///     The total number of bytes read in to the buffer. This can be less than the
        ///     number of bytes requested if that many bytes are not currently available, or
        ///     zero if th eend of the buffer has been reached.
        /// </returns>
        int Read(byte[] bufferOut, int offset, int count);
    }
}