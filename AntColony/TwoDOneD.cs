using System;
using System.Collections.Generic;
using System.Text;

namespace AntColony
{
    public class TwoDOneD<T>
    {
        public T[] input;
        public int length0 { get; set; }
        public TwoDOneD(T[] input, int length0)
        {
            this.input = input;
            this.length0 = length0;
        }
        public T this[int index0, int index1]
        {
            get { return input[index0 * this.length0 + index1]; }
            set { input[index0 * this.length0 + index1] = value; }
        }
        public T this[int index]
        {
            get { return input[index]; }
            set { input[index] = value; }
        }
        public float[] GetRow(int index)
        {
            var tmp = new float[length0];
            Array.Copy(input, index, tmp, 0, length0);
            return tmp;
        }
        public int GetLength()
        {
            return length0;
        }
    }
}
