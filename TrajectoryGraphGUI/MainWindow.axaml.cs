using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Microsoft.Win32.SafeHandles;



namespace TrajectoryGraphGUI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            var graph = this.FindControl<Polyline>("Graph1");

            graph.Points = new List<Point>(GetPoints(1000, 5, 45, 0));
            graph.Stroke = new SolidColorBrush(new Color(255, 255, 255, 255));
        }

        void OnAdd(object sender, RoutedEventArgs e)
        {
            var graph = this.FindControl<Polyline>("Graph1");

            var points = new List<Point>(graph.Points);

            for(int i = 0; i < points.Count; i++)
            {
                points[i] = points[i].WithX(points[i].X - 1);
            }

            graph.Points = points;
        }
        
        double Distance(double velocity, double angle_of_attack, double initial_height)
        {
            const double g = 9.818060721453536;

            return velocity * Math.Cos(angle_of_attack * (Math.PI / 180)) * (velocity * Math.Sin(angle_of_attack * (Math.PI / 180)) + Math.Sqrt(Math.Pow(velocity * Math.Sin(angle_of_attack * (Math.PI / 180)), 2) + 2 * g * initial_height)) / g;
        }

        private unsafe Point[] GetPoints(int num_points, double velocity, double angle_of_attack, double initial_height)
        {
            double* heights;
            int heightsCount;
            GenerateItemsWrapper(num_points, velocity, angle_of_attack, initial_height, out heights, out heightsCount);
            
            var dist = Distance(5, 45, 0);

            var step = dist / num_points;

            double max = heights[0];
            for (int i = 1; i < heightsCount; i++)
            {
                if (heights[i] > max)
                {
                    max = heights[i];
                } 
            }

            Point[] points = new Point[heightsCount];
            for (int i = 0; i < heightsCount; i++)
            {
                points[i] = new Point(i * step * (600 / max), 600 - (heights[i] * (600 / max)));
            }

            return points;
        }
        
        
        #region wrapper

        [DllImport("testStuffCpp", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        static extern unsafe bool GenerateItems(int num_points, double velocity, double angle_of_attack, double initial_height, out ItemsSafeHandle itemsHandle, out double* items, out int itemCount);

        [DllImport("testStuffCpp", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        static extern unsafe bool ReleaseItems(IntPtr itemsHandle);

        static unsafe ItemsSafeHandle GenerateItemsWrapper(int num_points, double velocity, double angle_of_attack, double initial_height, out double* items, out int itemsCount)
        {
            ItemsSafeHandle itemsHandle;
            if (!GenerateItems(num_points, velocity, angle_of_attack, initial_height, out itemsHandle, out items, out itemsCount))
            {
                throw new InvalidOperationException();
            }
            return itemsHandle;
        }

        class ItemsSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public ItemsSafeHandle()
                : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return ReleaseItems(handle);
            }
        }

        #endregion 
    }
}

