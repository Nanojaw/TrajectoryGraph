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

            var graph = this.FindControl<Polyline>("Graph1");

            graph.Points = new List<Point>(GetPoints(1000, 5, 45, 0));
        }

        void Add(double x, double y)
        {
            var graph = this.FindControl<Polyline>("Graph1");

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
            Line xLine;
            Line yLine;
            try
            {
                canvas = win.GetVisualDescendants().OfType<Canvas>().First();
                xLine = this.FindControl<Line>("XLine");
                yLine = this.FindControl<Line>("YLine");
            }
            catch (InvalidOperationException exception) { return; }
            
            switch (e.Property.Name)
            {
                case "Width":
                {
                    canvas.Width = canvas.Width + (double)e.NewValue - (double)e.OldValue;
                    xLine.EndPoint = xLine.EndPoint.WithX(canvas.Width);
                    break;
                }
                case "Height":
                {
                    canvas.Height = canvas.Height + (double)e.NewValue - (double)e.OldValue;
                    Add(0, ((double)e.NewValue - (double)e.OldValue));
                    xLine.StartPoint = xLine.StartPoint.WithY(canvas.Height - 5);
                    xLine.EndPoint = xLine.EndPoint.WithY(canvas.Height - 5);
                    yLine.EndPoint = yLine.EndPoint.WithY(canvas.Height);
                    break;
                }
            }
        }

        private void ScrollLeft_OnClick(object? sender, RoutedEventArgs e)
        {
            var step = int.Parse(this.FindControl<TextBox>("ScrollStep").Text);

            Add(step, 0);
        }
        private void ScrollRight_OnClick(object? sender, RoutedEventArgs e)
        {
            var step = int.Parse(this.FindControl<TextBox>("ScrollStep").Text);

            Add(step * -1, 0);
        }
    }
}

