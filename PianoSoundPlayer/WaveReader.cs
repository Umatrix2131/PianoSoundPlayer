using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PianoSoundPlayer
{
    public class WaveReader
    {
     
        private static (byte[] Bytes, WaveFormat waveFormat) WaveReadFormFile(string FilePath)
        {
            AudioFileReader audioWriter = new AudioFileReader(FilePath);
            var bytes = new byte[audioWriter.Length];
            audioWriter.Read(bytes, 0, (int)audioWriter.Length);
            audioWriter.Close();
            return (bytes, audioWriter.WaveFormat);
        }
        unsafe private static T[] ToData<T>(byte[] bytes)
        {
            T[] Arrays = new T[bytes.Length / sizeof(T)];
            Buffer.BlockCopy(bytes, 0, Arrays, 0, bytes.Length);
            return Arrays;
        }
        private static (double[] L, double[] R) ConvertDataTypesToDouble(object DataSrc)
        {
            if (DataSrc is byte[] A1)
            {
                var evenArray = A1.Where((value, index) => index % 2 == 0).Select(value => (double)value).ToArray();
                var oddArray = A1.Where((value, index) => index % 2 != 0).Select(value => (double)value).ToArray();
                return (evenArray, oddArray);
            }
            if (DataSrc is short[] A2)
            {
                var evenArray = A2.Where((value, index) => index % 2 == 0).Select(value => (double)value).ToArray();
                var oddArray = A2.Where((value, index) => index % 2 != 0).Select(value => (double)value).ToArray();
                return (evenArray, oddArray);
            }
            if (DataSrc is int[] A3)
            {
                var evenArray = A3.Where((value, index) => index % 2 == 0).Select(value => (double)value).ToArray();
                var oddArray = A3.Where((value, index) => index % 2 != 0).Select(value => (double)value).ToArray();
                return (evenArray, oddArray);
            }
            if (DataSrc is float[] A4)
            {
                var evenArray = A4.Where((value, index) => index % 2 == 0).Select(value => (double)value).ToArray();
                var oddArray = A4.Where((value, index) => index % 2 != 0).Select(value => (double)value).ToArray();
                return (evenArray, oddArray);
            }
            if (DataSrc is double[] A5)
            {
                var evenArray = A5.Where((value, index) => index % 2 == 0).Select(value => (double)value).ToArray();
                var oddArray = A5.Where((value, index) => index % 2 != 0).Select(value => (double)value).ToArray();
                return (evenArray, oddArray);
            }

            return (null, null);
        }
        public static byte[] ToBytes<T>(T[] arrays)
        {
            if (typeof(T).Name == typeof(short).Name)
            {
                int byteLength = arrays.Length * 2;
                byte[] byteArray = new byte[byteLength];
                Buffer.BlockCopy(arrays, 0, byteArray, 0, byteLength);
                return byteArray;
            }
            if (typeof(T).Name == typeof(int).Name)
            {
                int byteLength = arrays.Length * 4;
                byte[] byteArray = new byte[byteLength];
                Buffer.BlockCopy(arrays, 0, byteArray, 0, byteLength);
                return byteArray;
            }
            if (typeof(T).Name == typeof(uint).Name)
            {
                int byteLength = arrays.Length * 4;
                byte[] byteArray = new byte[byteLength];
                Buffer.BlockCopy(arrays, 0, byteArray, 0, byteLength);
                return byteArray;
            }
            if (typeof(T).Name == typeof(float).Name)
            {
                int byteLength = arrays.Length * 4;
                byte[] byteArray = new byte[byteLength];
                Buffer.BlockCopy(arrays, 0, byteArray, 0, byteLength);
                return byteArray;
            }
            if (typeof(T).Name == typeof(double).Name)
            {
                int byteLength = arrays.Length * 8;
                byte[] byteArray = new byte[byteLength];
                Buffer.BlockCopy(arrays, 0, byteArray, 0, byteLength);
                return byteArray;
            }
            return null;
        }
        public static T[] Combind<T>(T[] data1, T[] data2)
        {
            return data1.Zip(data2, (a, b) => new T[] { a, b }).SelectMany(pair => pair).ToArray();
        } 
        public static int[] DoubleToInt(double[] Datas)
        {
            return Datas.Select(s => (int)(s * int.MaxValue)).ToArray();
        }

        public static AudioDataSet ReadWaveFile(string FilePath)
        {
            var GetDatas = WaveReadFormFile(FilePath);

            object LRo = null;
            if (GetDatas.waveFormat.BitsPerSample == 8) LRo = ToData<byte>(GetDatas.Bytes);
            if (GetDatas.waveFormat.BitsPerSample == 16) LRo = ToData<short>(GetDatas.Bytes);
            if (GetDatas.waveFormat.BitsPerSample == 32) LRo = ToData<float>(GetDatas.Bytes);

            var GetData = ConvertDataTypesToDouble(LRo); 
            int[] L = DoubleToInt(GetData.L);
            int[] R = DoubleToInt(GetData.R);
            var FinalData = WaveReader.Combind(L, R);

            AudioDataSet Temp = new AudioDataSet();
            Temp.LR = FinalData.ToList();
            Temp.waveFormat = new WaveFormat(GetDatas.waveFormat.SampleRate, 32, GetDatas.waveFormat.Channels);

            return Temp;
        }
    }
}
