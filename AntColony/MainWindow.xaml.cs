using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace AntColony
{

    public partial class MainWindow : Window
    {
        bool languageASM = true;
        int mode = 0;
        private ObservableCollection<City> Cities;
        //private ObservableCollection<City> FoundCities;
        private int attempts = 2;
        TwoDOneD<float> graph;

        IntPtr[] DistancesPointer256;
        IntPtr[] XCoordinatePointer256;
        IntPtr[] YCoordinatePointer256;
        IntPtr[] DistancesPointerASM;
        IntPtr XCoordinatePointerASM;
        IntPtr YCoordinatePointerASM;

        private bool allocated = false;
        private int PackageSize = 0;
        private int AllPackages256 = 0;
        private float[] xs;
        private float[] ys;
        private int PackageSize256 = 8;

        private int threads = 1;
        private int CityNumberCounter = 0;
        private TwoDOneD<float> CurrentTrails;
        CityCanvasWindow ccw;

        double timeDistances;

        [DllImport("C:/Users/KamilPC/source/repos/AntColony/x64/Release/Generator.dll")]
        public static extern int CreateRandomCities(int number_of_cities, string path);
        public MainWindow()
        {
            InitializeComponent();
            Cities = new ObservableCollection<City>();
            cListView.ItemsSource = Cities;        
            addCities("C:/Users/KamilPC/source/repos/AntColony/x64/Release/Cities.txt");
            HowMuchCities.Text = CityNumberCounter.ToString();
            ccw = new CityCanvasWindow();
        }
        ~MainWindow()
        {
            dealloc();
        }

        [DllImport("C:/Users/KamilPC/source/repos/AntColony/x64/Release/AntColonyASM.dll")]
        public static extern void GetDistances256ASM(IntPtr dist,  IntPtr xs, IntPtr ys, float x, float y);
        [DllImport("C:/Users/KamilPC/source/repos/AntColony/x64/Release/AntColonyC.dll")]
        public static extern void GetDistances256C(IntPtr dist, IntPtr xs, IntPtr ys, float x, float y);
        [DllImport("C:/Users/KamilPC/source/repos/AntColony/x64/Release/AntColonyC.dll")]
        public static extern void GetDistancesC(IntPtr dist, IntPtr xs, IntPtr ys, float x, float y);

        [DllImport("C:/Users/KamilPC/source/repos/AntColony/x64/Release/AntColonyASM.dll")]
        public static extern unsafe void GetDistancesASM(IntPtr dist, IntPtr xs, IntPtr ys, float x, float y, int size);
        [DllImport("C:/Users/KamilPC/source/repos/AntColony/x64/Release/AntColonyASM.dll")]
        public static extern void ChangeFloat(IntPtr trails, int index, float val);


        void dealloc()
        {
            if (allocated)
            {
                for (int i = 0; i < CityNumberCounter; i++)
                {
                    if (DistancesPointerASM[i] != IntPtr.Zero)
                        Marshal.FreeHGlobal(DistancesPointerASM[i]);
                }
                if (XCoordinatePointerASM != IntPtr.Zero)
                    Marshal.FreeHGlobal(XCoordinatePointerASM);
                if (YCoordinatePointerASM != IntPtr.Zero)
                    Marshal.FreeHGlobal(YCoordinatePointerASM);
                for (int i = 0; i < AllPackages256; i++)
                {
                    if (XCoordinatePointer256[i] != IntPtr.Zero)
                        Marshal.FreeHGlobal(XCoordinatePointer256[i]);
                    if (YCoordinatePointer256[i] != IntPtr.Zero)
                        Marshal.FreeHGlobal(YCoordinatePointer256[i]);
                }
                for (int i = 0; i < AllPackages256 * CityNumberCounter; i++)
                {
                    if (DistancesPointer256[i] != IntPtr.Zero)
                        Marshal.FreeHGlobal(DistancesPointer256[i]);
                }
                xs = null;
                ys = null;
                XCoordinatePointerASM = IntPtr.Zero;
                YCoordinatePointerASM = IntPtr.Zero;
                DistancesPointerASM = null;
                XCoordinatePointer256 = null;
                YCoordinatePointer256 = null;
                DistancesPointer256 = null;
                allocated = false;
                graph = null;
            }
        }
        void getDistances()
        {
            if (allocated == false)
            {
                /////////////////////////Variables//////////////////////////////
                ///Packed floats packeet size
                AllPackages256 = CityNumberCounter / PackageSize256;
                PackageSize = CityNumberCounter % 8 == 0 ? (int)(CityNumberCounter / 8) : (int)(CityNumberCounter / 8 + 1);
                ///256bit register pointers
                DistancesPointer256 = new IntPtr[AllPackages256 * CityNumberCounter];
                XCoordinatePointer256 = new IntPtr[AllPackages256];
                YCoordinatePointer256 = new IntPtr[AllPackages256];
                ///NORMAL POINTERS
                DistancesPointerASM = new IntPtr[CityNumberCounter];
                XCoordinatePointerASM = new IntPtr();
                YCoordinatePointerASM = new IntPtr();
                ///Copy tables             
                xs = new float[CityNumberCounter];
                ys = new float[CityNumberCounter];
                /////////////////////FILL XS AND YS/////////////////////////////
                int iter = 0;
                foreach (var c in Cities)
                {
                    xs[iter] = c.x;
                    ys[iter] = c.y;
                    iter++;
                }
                //////////////////ALLOCATE 256BIT POINTERS//////////////////////
                for (int i = 0; i < AllPackages256; i++)
                {
                    XCoordinatePointer256[i] = Marshal.AllocHGlobal(sizeof(float) * PackageSize256);
                    YCoordinatePointer256[i] = Marshal.AllocHGlobal(sizeof(float) * PackageSize256);
                    for (int j = 0; j < CityNumberCounter; j++)
                    {
                        DistancesPointer256[i * CityNumberCounter + j] = Marshal.AllocHGlobal(sizeof(float) * PackageSize256);
                    }

                    Marshal.Copy(xs, i * PackageSize256, XCoordinatePointer256[i], PackageSize256);
                    Marshal.Copy(ys, i * PackageSize256, YCoordinatePointer256[i], PackageSize256);
                }
                ///////////Just proper
                for (int i = 0; i < CityNumberCounter; i++)
                {
                    DistancesPointerASM[i] = Marshal.AllocHGlobal(sizeof(float) * PackageSize * 8);
                }
                XCoordinatePointerASM = Marshal.AllocHGlobal(sizeof(float) * PackageSize * 8);
                YCoordinatePointerASM = Marshal.AllocHGlobal(sizeof(float) * PackageSize * 8);
                Marshal.Copy(xs, 0, XCoordinatePointerASM, CityNumberCounter);
                Marshal.Copy(ys, 0, YCoordinatePointerASM, CityNumberCounter);
                allocated = true;
            }


            //////////////////STOPWATCH START//////////////////////////////
            Stopwatch stopwatchDistances = Stopwatch.StartNew();
            //////////////////STOPWATCH START//////////////////////////////
            
            if (languageASM)
            {
                switch (mode)
                {
                    case 1:
                        Parallel.For(0, CityNumberCounter, new ParallelOptions { MaxDegreeOfParallelism = threads }, i =>
                        {
                            GetDistancesASM(XCoordinatePointerASM, YCoordinatePointerASM, DistancesPointerASM[i], xs[i], ys[i], PackageSize);
                        });
                        break;

                    case 2:
                        for (int j = 0; j < AllPackages256; j++)
                            Parallel.For(0, CityNumberCounter, new ParallelOptions { MaxDegreeOfParallelism = threads }, i =>
                            {
                                GetDistances256ASM(DistancesPointer256[i * AllPackages256 + j], XCoordinatePointer256[j], YCoordinatePointer256[j], xs[i], ys[i]);
                            });
                        break;

                    default:
                        Parallel.For(0, CityNumberCounter, new ParallelOptions { MaxDegreeOfParallelism = threads }, i =>
                        {
                                GetDistancesASM(XCoordinatePointerASM, YCoordinatePointerASM, DistancesPointerASM[i], xs[i], ys[i], PackageSize);
                        });
                        break;
                }

            }
            else
            {
                switch (mode)
                {
                    case 1:
                        for (int j = 0; j < AllPackages256; j++)
                            Parallel.For(0, CityNumberCounter, new ParallelOptions { MaxDegreeOfParallelism = threads }, i =>
                            {
                                GetDistancesC(DistancesPointer256[i * AllPackages256 + j], XCoordinatePointer256[j], YCoordinatePointer256[j], xs[i], ys[i]);
                            });
                        break;

                    case 2:
                        for (int j = 0; j < AllPackages256; j++)
                            Parallel.For(0, CityNumberCounter, new ParallelOptions { MaxDegreeOfParallelism = threads }, i =>
                            {
                                GetDistances256C(DistancesPointer256[i * AllPackages256 + j], XCoordinatePointer256[j], YCoordinatePointer256[j], xs[i], ys[i]);
                            });
                        break;

                    default:
                        for (int j = 0; j < AllPackages256; j++)
                            Parallel.For(0, CityNumberCounter, new ParallelOptions { MaxDegreeOfParallelism = threads }, i =>
                            {
                                GetDistancesC(DistancesPointer256[i * AllPackages256 + j], XCoordinatePointer256[j], YCoordinatePointer256[j], xs[i], ys[i]);
                            });
                        break;
                }
            }
            //////////////////STOPWATCH STOP///////////////////////////////
            stopwatchDistances.Stop();
            DistanceTime.Text = stopwatchDistances.Elapsed.TotalMilliseconds.ToString();
            timeDistances = stopwatchDistances.Elapsed.TotalMilliseconds;
            //////////////////STOPWATCH STOP///////////////////////////////
            ///

            for (int i = 0; i < CityNumberCounter; i++)
            {
                ChangeFloat(DistancesPointerASM[i], i, float.PositiveInfinity);
                for (int j = 0; j < PackageSize * 8 - CityNumberCounter; j++)
                    ChangeFloat(DistancesPointerASM[i], CityNumberCounter, float.PositiveInfinity);
            }
            graph = new TwoDOneD<float>(new float[CityNumberCounter * CityNumberCounter], CityNumberCounter);
            byte[] bytes = new byte[CityNumberCounter * CityNumberCounter * 4];
            int counter2 = 0;
            if (mode != 2 && languageASM)
            {
                for (int i = 0; i < CityNumberCounter; i++)
                    for (int j = 0; j < CityNumberCounter * 4; j++)
                    {
                        bytes[counter2++] = Marshal.ReadByte(DistancesPointerASM[i] + j);
                    }
            }
            else
            {
                for (int i = 0; i < CityNumberCounter * AllPackages256; i++)
                {
                    for (int j = 0; j < 4 * PackageSize256; j++)
                    {
                        bytes[counter2++] = Marshal.ReadByte(DistancesPointer256[i] + j);
                    }
                }
            }
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            for(int i = 0; i < CityNumberCounter * CityNumberCounter; i++)
            {
                graph[i] = BitConverter.ToSingle(bytes, i * 4);
            }
            btn1.IsEnabled = true;
        }

        void OnClick1(object sender, RoutedEventArgs e)
        {
            if(graph == null)
            {
                return;
            }

            switch (comboMode.SelectedIndex)
            {               
                //Random
                case 0:
                    Stopwatch stopwatchCSharp = new Stopwatch();
                    stopwatchCSharp.Start();
                    ACO aco = new ACO(CityNumberCounter, graph.input);
                    aco.startOptimizing(attempts);
                    stopwatchCSharp.Stop();
                    ACOTime.Text = stopwatchCSharp.Elapsed.TotalMilliseconds.ToString();
                    ACOLength.Text = aco.bestTourLength.ToString();
                    using(StreamWriter stream = new StreamWriter("C:/Users/KamilPC/source/repos/AntColony/x64/Release/TourC#.txt"))
                    {
                        string tour = "";
                        for (int i = 0; i < CityNumberCounter; i++)
                            tour += aco.bestTourOrder[i] + "->";
                        stream.WriteLine(tour);
                    }
                    CurrentTrails = new TwoDOneD<float>(ACO.trails.input, CityNumberCounter);
                    ccw.CreateTrailsGraph(Cities, CurrentTrails, graph, 0.03);
                    ccw.Show();
                    break;
                
                //Fill
                case 1:
                    Stopwatch stopwatchASM = new Stopwatch();
                    stopwatchASM.Start();
                    ACOASM acoasm;
                    if(mode != 2)
                        acoasm = new ACOASM(CityNumberCounter, graph.input, threads, DistancesPointerASM);
                    else
                        acoasm = new ACOASM(CityNumberCounter, graph.input, threads);
                    acoasm.start(attempts);
                    stopwatchASM.Stop();
                    ACOTime.Text = stopwatchASM.Elapsed.TotalMilliseconds.ToString();
                    ACOLength.Text = acoasm.bestTourLength.ToString();
                    using (StreamWriter stream = new StreamWriter("C:/Users/KamilPC/source/repos/AntColony/x64/Release/TourASM.txt"))
                    {
                        string tour = "";
                        for (int i = 0; i < CityNumberCounter; i++)
                            tour += acoasm.bestTourOrder[i] + "->";
                        stream.WriteLine(tour);
                    }
                    break;

                default:
                    break;
            }
        
        }
        private void ASMRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            languageASM = true;
        }

        private void CRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            languageASM = false;
        }
        private void RadioButtonPLAIN_Checked(object sender, RoutedEventArgs e)
        {
            mode = 1;
        }

        private void RadioButton256_Checked(object sender, RoutedEventArgs e)
        {
            mode = 2;
        }
        
        private void GenerateButtonAction(object sender, RoutedEventArgs e)
        {
            WrongNumber.Text = "";
            int cN = 64;
            if (int.TryParse(HowMuchCities.Text, out cN))
            {
                dealloc();
                for (int i = 0; i < CityNumberCounter; i++)
                    Cities[i] = null;
                Cities = null;
                CityNumberCounter = 0;
                CreateRandomCities(cN, "C:/Users/KamilPC/source/repos/AntColony/x64/Release/Cities.txt");
                Cities = new ObservableCollection<City>();
                cListView.ItemsSource = Cities;
                addCities("C:/Users/KamilPC/source/repos/AntColony/x64/Release/Cities.txt");
                HowMuchCities.Text = CityNumberCounter.ToString();
                btn1.IsEnabled = false;
            }
            else
                WrongNumber.Text = "Provide integer";

        }

        private void TestButtonAction(object sender, RoutedEventArgs e)
        {
            testForGraphs();
        }

        private void FindDistancesAction(object sender, RoutedEventArgs e)
        {
            getDistances();
        }

        City SearchForCity(int cityNumber)
        {
            foreach(var c in Cities)
            {
                if (c.CityNumber == cityNumber)
                    return c;
            }
            return null;
        }

        void addCities(string path)
        {
            char separator = ',';
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    string[] words = line.Split(separator);
                    Cities.Add(new City() { x = float.Parse(words[1]), y = float.Parse(words[2]), CityNumber = CityNumberCounter++, CityName = words[0] });
                }
            }
            else
            {
                CreateRandomCities(100, path);
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);
                    foreach (string line in lines)
                    {
                        string[] words = line.Split(separator);
                        Cities.Add(new City() { x = float.Parse(words[1]), y = float.Parse(words[2]), CityNumber = CityNumberCounter++, CityName = words[0] });
                    }
                }

            }
        }
        void testForGraphs()
        {
            int size = 5;
            for (int i = 1; i < 13; i++)
            {
                using (StreamWriter writer = new StreamWriter("C:/Users/KamilPC/source/repos/AntColony/x64/Release/AlphaBeta"+i+".txt"))
                {
                    writer.WriteLine("alpha = " + i);
                    writer.WriteLine("beta,length,time");
                    for (int j = 1; j < 13; j++)
                    {
                        double s = 0;
                        float best = 0;
                        for (int k = 0; k < size; k++)
                        {
                            ACO aco = new ACO(CityNumberCounter, graph.input);
                            aco.setACO(i, j, 0.5f, 500f, 4f);
                            Stopwatch stp = new Stopwatch();
                            stp.Start();
                            aco.startOptimizing(1);
                            stp.Stop();
                            best += aco.bestTourLength;
                            s += stp.Elapsed.Milliseconds;
                            stp.Reset();
                        }
                        writer.WriteLine(j + "," + best / size + "," + s / size);
                    }
                }
            }
        }

    }


}
