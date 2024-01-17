using System;
using System.IO;

namespace BW_Cubinator
{
    public class OpenPNG
    {
        public static void SavePNGFromRawTextureData(byte[] rawTextureData, int width, int height, string outputFilePath)
        {
            // Convert ARGB32 to RGBA format
            byte[] rgbaData = ConvertARGB32ToRGBA(rawTextureData);

            // Save the PNG file
            SavePNGFromByteArray(rgbaData, width, height, outputFilePath);
        }

        // Helper method to convert ARGB32 to RGBA format
        private static byte[] ConvertARGB32ToRGBA(byte[] argb32Data)
        {
            int pixelCount = argb32Data.Length / 4;
            byte[] rgbaData = new byte[argb32Data.Length];

            for (int i = 0; i < pixelCount; i++)
            {
                // ARGB32 format: [A][R][G][B]
                // RGBA format:  [R][G][B][A]

                int srcOffset = i * 4;
                int destOffset = i * 4;

                // Copy RGB bytes (discard Alpha channel)
                rgbaData[destOffset + 0] = argb32Data[srcOffset + 1]; // R
                rgbaData[destOffset + 1] = argb32Data[srcOffset + 2]; // G
                rgbaData[destOffset + 2] = argb32Data[srcOffset + 3]; // B

                // Copy Alpha channel
                rgbaData[destOffset + 3] = argb32Data[srcOffset + 0]; // A
            }

            return rgbaData;
        }

        public static void SavePNGFromByteArray(byte[] rgbaData, int width, int height, string outputFilePath)
        {
            // PNG file header (8 bytes)
            byte[] pngHeader =
            {
                137, 80, 78, 71, 13, 10, 26, 10 // Hexadecimal: 89 50 4E 47 0D 0A 1A 0A
            };

            // Create the PNG IHDR chunk (Image Header)
            byte[] ihdrChunk = CreateIHDRChunk(width, height);

            // Create the PNG IDAT chunk (Image Data)
            byte[] idatChunk = CreateIDATChunk(rgbaData);

            // Create the PNG IEND chunk (End)
            byte[] iendChunk = { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 };

            // Combine all the chunks
            byte[] pngData = CombineChunks(pngHeader, ihdrChunk, idatChunk, iendChunk);

            // Write the PNG data to a file
            File.WriteAllBytes(outputFilePath, pngData);
        }

        // Helper method to create the PNG IHDR chunk
        private static byte[] CreateIHDRChunk(int width, int height)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)width);    // Width
                writer.Write((uint)height);   // Height
                writer.Write((byte)8);        // Bit depth (8 bits per channel for RGBA)
                writer.Write((byte)6);        // Color type (6 for RGBA)
                writer.Write((byte)0);        // Compression method (always 0 for PNG)
                writer.Write((byte)0);        // Filter method (always 0 for PNG)
                writer.Write((byte)0);        // Interlace method (0 for no interlace)

                // Create the chunk data
                byte[] chunkData = stream.ToArray();

                // Add chunk length and type information
                byte[] lengthBytes = BitConverter.GetBytes(chunkData.Length);
                byte[] typeBytes = { 73, 72, 68, 82 }; // Hexadecimal: 49 48 44 52

                byte[] ihdrChunk = CombineChunks(lengthBytes, typeBytes, chunkData);
                return ihdrChunk;
            }
        }

        // Helper method to create the PNG IDAT chunk
        private static byte[] CreateIDATChunk(byte[] rgbaData)
        {
            // Compress the RGBA data (for simplicity, not implemented here)

            // For the sake of simplicity, let's assume no compression for this example.
            // In a real-world scenario, you'd need to use a proper compression algorithm like DEFLATE.

            // In this example, we'll simply return the uncompressed RGBA data as the IDAT chunk.
            byte[] lengthBytes = BitConverter.GetBytes(rgbaData.Length);
            byte[] typeBytes = { 73, 68, 65, 84 }; // Hexadecimal: 49 44 41 54

            byte[] idatChunk = CombineChunks(lengthBytes, typeBytes, rgbaData);
            return idatChunk;
        }

        // Helper method to combine chunks (length + type + data) into a single byte array
        private static byte[] CombineChunks(params byte[][] chunks)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                foreach (byte[] chunk in chunks)
                {
                    writer.Write(chunk);
                }
                return stream.ToArray();
            }
        }
    }
}