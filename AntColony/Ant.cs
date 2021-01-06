using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace AntColony
{

    public class ReversedBool
    {
        [DllImport("C:/Users/KamilPC/source/repos/AntColony/x64/Debug/AntColonyASM.dll")]
        public static extern void ChangeFloat(IntPtr trails, int index, float val);

        public float[] input;
        IntPtr packedInput;
        public ReversedBool(float[] input, IntPtr packedInput)
        {
            this.input = input;
            this.packedInput = packedInput;
        }
        public bool this[int index]
        {
            get
            {
                if (input[index] == 0f)
                    return true;
                else
                    return false;
            }
            set
            {
                if (value == true)
                { 
                    input[index] = 0f;
                    ChangeFloat(packedInput, index, 0f);
                }
                else 
                {
                    input[index] = 1f;
                    ChangeFloat(packedInput, index, 1f);
                }
                   
            }
        }
    }
    public class Ant
    {
        public int trailSize;
        public int[] trail;
        public bool[] visited;
        public ReversedBool reversedVisited;
        private float[] clearVisited;
        public IntPtr packedVisited;
        public Ant(int tourSize)
        {
            this.trailSize = tourSize;
            this.trail = new int[tourSize];
            this.visited = new bool[tourSize];
        }
        public Ant(int tourSize, int nP)
        {
            this.trailSize = tourSize;
            this.trail = new int[tourSize];
            this.clearVisited = new float[nP * 8];

            for (int i = 0; i < tourSize; i++)
                clearVisited[i] = 1f;

            packedVisited = Marshal.AllocHGlobal(sizeof(float) * nP * 8);
            reversedVisited = new ReversedBool(new float[tourSize], packedVisited);

        }
        ~Ant()
        {
            if (packedVisited != null)
                Marshal.FreeHGlobal(packedVisited);
        }
        public void visitCity(int currIndex, int numberOfCity)
        {
            trail[currIndex + 1] = numberOfCity;
            visited[numberOfCity] = true;
        }

        public bool getVisited(int index)
        {
            return visited[index];
        }
        public float trailLength(float[] graph)
        {
            float length = graph[trail[trailSize - 1] * trailSize + trail[0]];

            //TODO
            for(int i = 0; i < trailSize -1; i++)
            {
                length += graph[trail[i] * trailSize + trail[i + 1]];
            }

            return length;
        }

        public void clear()
        {
            for (int i = 0; i < trailSize; i++)
                visited[i] = false;
        }
        public void packedClear()
        {
            Array.Copy(clearVisited, reversedVisited.input, reversedVisited.input.Length);
            Marshal.Copy(clearVisited, 0, packedVisited, clearVisited.Length);
        }
    }
}
