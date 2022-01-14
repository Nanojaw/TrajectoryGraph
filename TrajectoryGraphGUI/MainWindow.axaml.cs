using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using Microsoft.Win32.SafeHandles;



namespace TrajectoryGraphGUI
{
    public partial class MainWindow : Window
    {
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

            var grid = this.FindControl<Polyline>("Grid");
            grid.Stroke = new SolidColorBrush(new Color(127, 200, 200, 200));
            grid.Points = GridPoints(1010, 610, 50, 5, 5);
            
            var graph = this.FindControl<Polyline>("Graph1");
            graph.Points = new List<Point>(GetPoints(1000, 5, 45, 0));
        }

        void Add(double x, double y, string name)
        {
            var graph = this.FindControl<Polyline>(name);

            var points = new List<Point>(graph.Points);

            for(int i = 0; i < points.Count; i++)
            {
                points[i] = new Point(points[i].X + x, points[i].Y + y);
            }

            graph.Points = points;
        }
        
        double Distance(double velocity, double angle_of_attack, double initial_height)
        {
            const double g = 9.818060721453536;

            return velocity * Math.Cos(angle_of_attack * (Math.PI / 180)) * (velocity * Math.Sin(angle_of_attack * (Math.PI / 180)) + Math.Sqrt(Math.Pow(velocity * Math.Sin(angle_of_attack * (Math.PI / 180)), 2) + 2 * g * initial_height)) / g;
        }

        private IList<Point> GridPoints(double width, double height, int size, double xOffset, double yOffset)
        {
            var points = new List<Point>();

            for (var i = 0; i < Math.Ceiling(width / size); i++)
            {
                if (i % 2 == 0)
                {
                    points.Add(new Point(i * size + xOffset, 0));
                    points.Add(new Point(i * size + xOffset, height));
                }
                else
                {
                    points.Add(new Point(i * size + xOffset, height));
                    points.Add(new Point(i * size + xOffset, 0));
                }
            }

            if (Math.Ceiling(width / size) % 2 == 0) points.Add(new Point(width, 0));
            points.Add(new Point(width, height));
            
             for (var i = 0; i < Math.Ceiling(height / size); i++)
             {
                 if (i % 2 == 0)
                 {
                     points.Add(new Point(width, i * size + yOffset));
                     points.Add(new Point(0, i * size + yOffset));
                 }
                 else
                 {
                     points.Add(new Point(0, i * size + yOffset));
                     points.Add(new Point(width, i * size + yOffset));
                 }
             }

            return points;
        }

        private unsafe Point[] GetPoints(int num_points, double velocity, double angle_of_attack, double initial_height)
        {
            GenerateItemsWrapper(num_points, velocity, angle_of_attack, initial_height, out var heights, out var heightsCount);
            
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
                points[i] = new Point(i * step * (600 / max) + 5, 605 - (heights[i] * (600 / max)));
            }

            return points;
        }
        
        private void AvaloniaObject_OnPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (!e.IsEffectiveValueChange) return;
            
            var win = (Window)sender;
            Canvas canvas;
            Polyline grid;
            
            try
            {
                canvas = win.FindControl<Canvas>("Canvas");
                grid = win.FindControl<Polyline>("Grid");
            }
            catch (InvalidOperationException exception) { return; }
            
            switch (e.Property.Name)
            {
                case "Width":
                {
                    canvas.Width = canvas.Width + (double)e.NewValue - (double)e.OldValue;

                    grid.Points = GridPoints(canvas.Width, canvas.Height, 50, 5, canvas.Height % 50 - 5);
                    break;
                }
                case "Height":
                {
                    canvas.Height = canvas.Height + (double)e.NewValue - (double)e.OldValue;
                    Add(0, (double)e.NewValue - (double)e.OldValue, "Graph1");
                    
                    grid.Points = GridPoints(canvas.Width, canvas.Height, 50, 5, canvas.Height % 50 - 5);
                    break;
                }
            }
        }

        private void ScrollLeft_OnClick(object? sender, RoutedEventArgs e)
        {
            var step = int.Parse(this.FindControl<TextBox>("ScrollStep").Text);

            Add(step, 0, "Graph1");
        }
        private void ScrollRight_OnClick(object? sender, RoutedEventArgs e)
        {
            var step = int.Parse(this.FindControl<TextBox>("ScrollStep").Text);

            Add(step * -1, 0, "Graph1");
        }
    }
}

