using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Scarlet.Utilities
{
    public static class UtilMain
    {

        /// <summary> Returns subarray of given array. </summary>
        /// <typeparam name="T"> Datatype of array </typeparam>
        /// <param name="Data"> Array to manipulate </param>
        /// <param name="Index"> Starting index of subarray. </param>
        /// <param name="Length"> Length of wanted subarray. </param>
        /// <returns> Sub array of data[index:index+length-1] (inclusive) </returns>
        public static T[] SubArray<T>(T[] Data, int Index, int Length)
        {
            T[] Result = new T[Length];
            Array.Copy(Data, Index, Result, 0, Length);
            return Result;
        }

        /// <summary> Gives a user-readable representation of a byte array. </summary>
        /// <param name="Data"> The array to format. </param>
        /// <param name="Spaces"> Whether to add spaces between every byte in the output </param>
        /// <returns> A string formatted as such: "4D 3A 20 8C", or "4D3A208C", depending on the Spaces parameter. </returns>
        public static string BytesToNiceString(byte[] Data, bool Spaces)
        {
            if (Data == null || Data.Length == 0) { return string.Empty; }
            StringBuilder Output = new StringBuilder();
            for (int i = 0; i < Data.Length; i++)
            {
                Output.Append(Data[i].ToString("X2"));
                if (Spaces) { Output.Append(' '); }
            }
            if (Spaces) { Output.Remove(Output.Length - 1, 1); }
            return Output.ToString();
        }

        /// <summary> Takes in a string, converts it into its byte representation. </summary>
        /// <param name="Data"> String to convert into bytes </param>
        /// <returns> Byte array that represents the given string. </returns>
        public static byte[] StringToBytes(string Data)
        {
            List<byte> Output = new List<byte>();
            Data = Data.Replace(" ", "");
            for (int Chunk = 0; Chunk < Math.Ceiling(Data.Length / 2.000); Chunk++)
            {
                int Start = Data.Length - ((Chunk + 1) * 2);
                string Section;
                if (Start >= 0) { Section = Data.Substring(Start, 2); }
                else { Section = Data.Substring(0, 1); }
                Output.Add(Convert.ToByte(Section, 16));
            }
            return Output.ToArray();
        }

        /// <summary> Creates a string bby repeating a sequence seperated by something else, with fenceposting. </summary>
        /// <param name="ToRepeat"> The string to repeat [Times] times. </param>
        /// <param name="Seperator"> The string to place in between each occurence of [ToRepeat]. </param>
        public static string RepeatWithSeperator(string ToRepeat, string Seperator, int Times)
        {
            if (Times <= 0) { throw new InvalidOperationException("Cannot repeat string by a negative number."); }
            StringBuilder Output = new StringBuilder();
            for (int i = 0; i < Times; i++)
            {
                Output.Append(ToRepeat);
                if (i + 1 < Times) { Output.Append(Seperator); }
            }
            return Output.ToString();
        }
 
    }

}
