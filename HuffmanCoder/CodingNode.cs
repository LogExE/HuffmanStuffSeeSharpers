
using System;
using System.Collections;
using System.Collections.Generic;

namespace HuffmanCoder
{
    internal class CodingNode : IComparable<CodingNode>
    {
        private CodingNode left, right;
        private char? sym;
        private int freq;
        private int height;

        public CodingNode(CodingNode left, CodingNode right)
        {
            this.left = left;
            this.right = right;

            freq = left.freq + right.freq;
            sym = null;

            height = Math.Max(left.height, right.height) + 1;
        }

        public CodingNode(char sym, int freq)
        {
            left = right = null;

            this.freq = freq;
            this.sym = sym;

            height = 0;
        }

        private static void Traverse(CodingNode cur, ref List<bool> code, ref Dictionary<char, BitArray> res)
        {
            if (cur.sym != null)
                res.Add(cur.sym ?? '\0', new BitArray(code.ToArray()));

            if (cur.left != null)
            {
                code.Add(false);
                Traverse(cur.left, ref code, ref res);
                code.RemoveAt(code.Count - 1);
            }
            if (cur.right != null)
            {
                code.Add(true);
                Traverse(cur.right, ref code, ref res);
                code.RemoveAt(code.Count - 1);
            }   
        }

        public Dictionary<char, BitArray> Traverse()
        {
            var res = new Dictionary<char, BitArray>();
            var code = new List<bool>();
            Traverse(this, ref code, ref res);
            return res;
        }

        public int CompareTo(CodingNode other)
        {
            int byFreq = freq.CompareTo(other.freq);
            int byHeight = height.CompareTo(other.height);
            if (byFreq == -1)
                return -1;
            else if (byFreq == 0)
                return byHeight;
            else return 1;
        }
    }
}
