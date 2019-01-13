﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using Xiropht_Connector_All.Setting;

namespace Xiropht_Connector_All.Utils
{
    /// <summary>
    ///     Class contains some method for helps development.
    /// </summary>
    public class ClassUtils
    {
        private static readonly List<string> ListOfCharacters = new List<string>
        {
            "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u",
            "v", "w", "x", "y", "z"
        };

        private static readonly List<string> ListOfNumbers = new List<string>
            {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9"};

        private static readonly List<string> ListOfSpecialCharacters =
            new List<string> {"&", "~", "#", "@", "'", "(", "|", "\\", ")", "="};

        private static readonly RNGCryptoServiceProvider Generator = new RNGCryptoServiceProvider();


        /// <summary>
        ///     Generate a dynamic certificate, include the static genesis secondary key
        /// </summary>
        /// <returns></returns>
        public static string GenerateCertificate()
        {
            var certificate = ClassConnectorSetting.NETWORK_GENESIS_SECONDARY_KEY;
            for (var i = 0;
                i < ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE;
                i++)
            {
                var randomType = GetRandomBetween(0, 100);
                if (randomType <= 15) // LowerCase character
                    certificate += ListOfCharacters[GetRandomBetween(0, ListOfCharacters.Count - 1)];
                if (randomType > 15 && randomType <= 45) // Number
                    certificate += ListOfNumbers[GetRandomBetween(0, ListOfNumbers.Count - 1)];
                if (randomType > 45 && randomType <= 65) // UpperCase character.
                    certificate += ListOfCharacters[GetRandomBetween(0, ListOfCharacters.Count - 1)].ToUpper();
                if (randomType > 65) // Special Characters
                    certificate += ListOfSpecialCharacters[GetRandomBetween(0, ListOfSpecialCharacters.Count - 1)];
            }

            return certificate;
        }

        public static string FromHex(string hex)
        {
            var ba = Encoding.UTF8.GetBytes(hex);

            return BitConverter.ToString(ba).Replace("-", "");
        }

        /// <summary>
        ///     Génère un nombre réellement aléatoire.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static int GetRandomBetween(int minimumValue, int maximumValue)
        {
            var randomNumber = new byte[1];

            Generator.GetBytes(randomNumber);

            var asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);

            var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255d - 0.00000000001d);

            var range = maximumValue - minimumValue + 1;

            var randomValueInRange = Math.Floor(multiplier * range);

            return (int) (minimumValue + randomValueInRange);
        }

        public static CultureInfo GlobalCultureInfo = new CultureInfo("fr-FR");
        /// <summary>
        ///     Translate hashrate from H/s into KH/s or other units.
        /// </summary>
        /// <param name="hashrate"></param>
        /// <returns></returns>
        public static string GetTranslateHashrate(string info, int decimalNumberLimit)
        {
            if (info != null)
            {
                info = info.Replace(".", ",");
                var hashrate = float.Parse(info, GlobalCultureInfo);
                if (hashrate >= 1024)
                    info = Math.Round(hashrate / 1024, decimalNumberLimit) + " KH/s";
                else if (hashrate >= 1024000)
                    info = Math.Round(hashrate / 1024000, decimalNumberLimit) + " MH/s";
                else if (hashrate >= 1024000000)
                    info = Math.Round(hashrate / 1024000000, decimalNumberLimit) + " GH/s";
                else if (hashrate >= 1024000000000)
                    info = Math.Round(hashrate / 1024000000000, decimalNumberLimit) + " TH/s";
                else if (hashrate >= 1024000000000000)
                    info = Math.Round(hashrate / 1024000000000000, decimalNumberLimit) + " PH/s";
                else
                    info = hashrate + " H/s";
            }

            return info;
        }

        /// <summary>
        ///     Translate big number string into readeable number string.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static string GetTranslateBigNumber(string info)
        {
            info = float.Parse(info).ToString("F8");
            return string.Format(info, GlobalCultureInfo);
        }

        /// <summary>
        ///     Can clone a list object.
        /// </summary>
        /// <returns></returns>
        public static object CloneType(object objtype)
        {
            var lstfinal = new object();

            using (var memStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.Clone));
                binaryFormatter.Serialize(memStream, objtype);
                memStream.Seek(0, SeekOrigin.Begin);
                lstfinal = binaryFormatter.Deserialize(memStream);
            }

            return lstfinal;
        }

        public static byte[] StrToByteArray(string str)
        {
            var hexindex = new Dictionary<string, byte>();
            for (var i = 0; i <= 255; i++)
                hexindex.Add(i.ToString("X2"), (byte) i);

            var hexres = new List<byte>();
            for (var i = 0; i < str.Length; i += 2)
                hexres.Add(hexindex[str.Substring(i, 2)]);

            return hexres.ToArray();
        }

        public static string DecompressWallet(string data)
        {

            MemoryStream input = new MemoryStream(Convert.FromBase64String(data));
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return Encoding.UTF8.GetString(output.ToArray());
        }

        public static string ConvertStringtoSHA512(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            using (var hash = SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);

                var hashedInputStringBuilder = new StringBuilder(128);
                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("X2"));

                string hashToReturn = hashedInputStringBuilder.ToString();
                hashedInputStringBuilder.Clear();
                return hashToReturn;
            }
        }

        public static bool SocketIsConnected(TcpClient socket)
        {
            if (socket?.Client != null)
                try
                {
                    return !(socket.Client.Poll(100, SelectMode.SelectRead) && socket.Available == 0);
                }
                catch
                {
                    return false;
                }

            return false;
        }
    }
}