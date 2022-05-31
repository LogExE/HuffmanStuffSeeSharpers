using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.IO.Compression;

namespace HuffmanCoder
{
    internal class Program
    {
        static Dictionary<char, BitArray> Encode(Dictionary<char, int> elems)
        {
            var arr = new List<CodingNode>();
            foreach (var pair in elems)
                arr.Add(new CodingNode(pair.Key, pair.Value));

            for (int i = 0; i < elems.Count - 1; i++)
            {
                arr.Sort();
                arr[0] = new CodingNode(arr[0], arr[1]);
                arr.RemoveAt(1);
            }

            return arr[0].Traverse();
        }

        static BitArray ReadBits(byte[] src, byte count, ref int pos)
        {
            var res = new BitArray(count);
            for (byte i = 0; i < count; i++)
            {
                res[i] = (src[pos / 8] >> (pos % 8)) % 2 == 1;
                ++pos;
            }
            return res;
        }

        static void WriteBits(List<byte> dst, BitArray value, ref int pos)
        {
            for (byte i = 0; i < value.Count; i++)
            {
                if (pos / 8 >= dst.Count)
                    dst.Add(0);
                dst[pos / 8] += (byte)((value[i] ? 1 : 0) << (pos % 8));
                ++pos;
            }
        }

        static readonly Encoding encoding = Encoding.Unicode;

        static void Main(string[] args)
        {
            Console.OutputEncoding = encoding;

#if DEBUG
            args = new string[] { "encode", "file.txt", "fileenc.zip" };
            //args = new string[] { "decode", "fileenc.zip", "decoded.txt" };
            Console.WriteLine("Compiled with DEBUG, passing arguments: " + string.Join(' ', args));
#endif

            if (args.Length != 3)
            {
                Console.WriteLine("Please pass a command as an argument (encode or decode) with file name (in and out).");
                return;
            }

            string cmd = args[0];
            string fileName = args[1];
            string fileNameOut = args[2];

            if (cmd == "encode")
            {
                if (!File.Exists(fileName))
                {
                    Console.WriteLine("\"{0}\" doesn't exist!", fileName);
                    return;
                }
                string text = File.ReadAllText(fileName, encoding);
                var elems = new Dictionary<char, int>();
                foreach (char c in text)
                    if (elems.ContainsKey(c))
                        ++elems[c];
                    else elems[c] = 1;
                var encoded = Encode(elems);

#if DEBUG
                Console.WriteLine("Codes:");
                foreach (var p in encoded)
                {
                    var sb = new StringBuilder();

                    for (int i = 0; i < p.Value.Count; i++)
                    {
                        char c = p.Value[i] ? '1' : '0';
                        sb.Append(c);
                    }

                    Console.WriteLine("{0} {1}", p.Key, sb);
                }
#endif

                int pos = 0;
                var output = new List<byte>();
                WriteBits(output, new BitArray(BitConverter.GetBytes(encoded.Count)), ref pos);

                foreach (var p in encoded)
                {
                    var charBits = new BitArray(BitConverter.GetBytes(p.Key));
                    WriteBits(output, charBits, ref pos);
                    var countBits = new BitArray(new byte[] { (byte)p.Value.Count });
                    WriteBits(output, countBits, ref pos);
                    WriteBits(output, p.Value, ref pos);
                }
                
                WriteBits(output, new BitArray(BitConverter.GetBytes(text.Length)), ref pos);

                foreach (char charToEncode in text)
                {
                    BitArray bits = encoded[charToEncode];
                    WriteBits(output, bits, ref pos);
                }

                File.WriteAllBytes("tempfile", output.ToArray());

                using (var arch = ZipFile.Open(fileNameOut, ZipArchiveMode.Update))
                {
                    arch.CreateEntryFromFile("tempfile", "encoded.bin");
                }

                File.Delete("tempfile");

                Console.WriteLine("Done!");
            }
            else if (cmd == "decode")
            {
                if (!File.Exists(fileName))
                {
                    Console.WriteLine("\"{0}\" doesn't exist!", fileName);
                    return;
                }

                ZipFile.ExtractToDirectory(fileName, ".");

                int pos = 0;
                byte[] input = File.ReadAllBytes("encoded.bin");

                File.Delete("encoded.bin");

                int[] ibuf = new int[1];
                byte[] bbuf = new byte[1];
                byte[] cbuf = new byte[sizeof(char)];

                ReadBits(input, sizeof(int) * 8, ref pos).CopyTo(ibuf, 0);
                int cnt = ibuf[0];

                var codeToChar = new Dictionary<string, char>();

                for (int i = 0; i < cnt; i++)
                {
                    ReadBits(input, sizeof(char) * 8, ref pos).CopyTo(cbuf, 0);
                    char charToRestore = BitConverter.ToChar(cbuf);
                    ReadBits(input, sizeof(byte) * 8, ref pos).CopyTo(bbuf, 0);
                    byte bitCount = bbuf[0];

                    byte[] bitarr = new byte[bitCount];
                    ReadBits(input, bitCount, ref pos).CopyTo(bitarr, 0);

                    var sb = new StringBuilder();

                    for (int j = 0; j < bitCount; ++j)
                        sb.Append((bitarr[j / 8] >> (j % 8)) % 2 == 1 ? '1' : '0');

                    codeToChar.Add(sb.ToString(), charToRestore);
                }

                ReadBits(input, sizeof(int) * 8, ref pos).CopyTo(ibuf, 0);
                int len = ibuf[0];

                var code = new StringBuilder();
                var msg = new StringBuilder();

                int alreadyRead = 0;

                while (alreadyRead < len)
                {
                    code.Append(ReadBits(input, 1, ref pos)[0] ? '1' : '0');
                    string fixedStr = code.ToString();
                    if (codeToChar.ContainsKey(fixedStr))
                    {
                        msg.Append(codeToChar[fixedStr]);
                        code.Clear();
                        ++alreadyRead;
                    }
                }

#if DEBUG
                Console.WriteLine("Text:");
                Console.WriteLine(msg.ToString());
#endif

                File.WriteAllText(fileNameOut, msg.ToString(), encoding);
                Console.WriteLine("Done!");
            }
            else Console.WriteLine("Command unrecognized!");
        }
    }
}
