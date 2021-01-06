using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AntColony
{
    public partial class CityCanvasWindow : Window
    {
        ObservableCollection<City> Cities;
        TwoDOneD<float> trails;
        TwoDOneD<float> graph;
        Point scrollMousePoint = new Point();
        double hOff = 1;
        double vOff = 1;
        bool CTRLPressed = false;
        Popup ActivePopup;
        bool IsPopupStatic = false;
        public CityCanvasWindow()
        {
            InitializeComponent();
        }

        private void CityScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            scrollMousePoint = e.GetPosition(CityScrollViewer);
            hOff = CityScrollViewer.HorizontalOffset;
            vOff = CityScrollViewer.VerticalOffset;
            CityScrollViewer.CaptureMouse();
        }
        private void CityScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if(CityScrollViewer.IsMouseCaptured)
            {
                CityScrollViewer.ScrollToHorizontalOffset(hOff + (scrollMousePoint.X - e.GetPosition(CityScrollViewer).X));
                CityScrollViewer.ScrollToVerticalOffset(vOff + (scrollMousePoint.Y - e.GetPosition(CityScrollViewer).Y));
            }
        }
        private void CityScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CityScrollViewer.ReleaseMouseCapture();
        }
        private void CityScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if(CTRLPressed)
            {
                if (e.Delta > 0)
                    ZoomingIn(sender, e);
                else
                    ZoomingOut(sender, e);
            }
            e.Handled = true;
        }
        private void ZoomingIn(object sender, RoutedEventArgs e)
        {
            var scaler = GridxD.LayoutTransform as ScaleTransform;
            var stackScaler = StackZoom.LayoutTransform as ScaleTransform;
            if(scaler == null)
            {
                GridxD.LayoutTransform = new ScaleTransform(1.05, 1.05);
                ScrollText.Text = "105%";
                StackZoom.LayoutTransform = new ScaleTransform(1.0/1.05, 1.0/1.05);
                StackZoom.Margin = new Thickness(10 / 1.05, 10 / 1.05, 0, 0);
            }
            else
            {
                if(scaler.HasAnimatedProperties)
                {
                    scaler.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    scaler.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                }
                scaler.ScaleX += 0.05;
                scaler.ScaleY += 0.05;
                ScrollText.Text = ((int)(scaler.ScaleY * 100)).ToString() + "%";
                if(stackScaler.HasAnimatedProperties)
                {
                    stackScaler.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    stackScaler.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                }
                stackScaler.ScaleX = 1.0 / scaler.ScaleX;
                stackScaler.ScaleY = 1.0 / scaler.ScaleY;
                StackZoom.Margin = new Thickness(10 / scaler.ScaleX, 10 / scaler.ScaleY, 0, 0);

            }
            
        }
        private void ZoomingOut(object sender, RoutedEventArgs e)
        {
            var scaler = GridxD.LayoutTransform as ScaleTransform;
            var stackScaler = StackZoom.LayoutTransform as ScaleTransform;
            if (scaler == null)
            {
                GridxD.LayoutTransform = new ScaleTransform(0.95, 0.95);
                ScrollText.Text = "95%";
                StackZoom.LayoutTransform = new ScaleTransform(1.0 / 0.95, 1.0 / 0.95);
                StackZoom.Margin = new Thickness(10 / 0.95, 10 / 0.95, 0, 0);
            }
            else
            {
                if (scaler.HasAnimatedProperties)
                {
                    scaler.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    scaler.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                }
                scaler.ScaleX -= 0.05;
                scaler.ScaleY -= 0.05;
                ScrollText.Text = ((int)(scaler.ScaleY * 100)).ToString() + "%";
                if (stackScaler.HasAnimatedProperties)
                {
                    stackScaler.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    stackScaler.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                }
                stackScaler.ScaleX = 1.0 / scaler.ScaleX;
                stackScaler.ScaleY = 1.0 / scaler.ScaleY;
                StackZoom.Margin = new Thickness(10 / scaler.ScaleX, 10 / scaler.ScaleY, 0, 0);
            }

        }

        public void CreateTrailsGraph(ObservableCollection<City> Cities, TwoDOneD<float> trails, TwoDOneD<float> graph, double baseThickness)
        {
            this.Cities = Cities;
            this.trails = trails;
            this.graph = graph;
            int CityCount = 0;
            //Points = new List<Ellipse>();
            for (int i = 0; i < trails.GetLength(); i++)
            {
                for (int j = i + 1; j < trails.GetLength(); j++)
                {
                    if (trails[i, j] > 0.5f)
                    {
                        var line = new Line()
                        {
                            X1 = Cities[i].x + 600,
                            Y1 = Cities[i].y + 600,
                            X2 = Cities[j].x + 600,
                            Y2 = Cities[j].y + 600,
                            Stroke = new SolidColorBrush(Colors.Magenta),
                            StrokeThickness = baseThickness * Math.Abs((1000.0 - 250.0 * (trails[i, j] + trails[j, i]+ 4)) / (trails[i, j] + trails[j, i] + 200))
                        };
                        CityCanvas.Children.Add(line);
                    }

                }
            }

            foreach (City city in Cities)
            {

                var ellipse = new Ellipse()
                {
                    Name = "ellipse" + CityCount++.ToString(),
                    Width = 8,
                    Height = 8,
                    Stroke = new SolidColorBrush(Colors.DarkBlue),
                    StrokeThickness = 4.0,
                };
                ellipse.PreviewMouseLeftButtonDown += Ellipse_PreviewMouseLeftButtonDown;
                ellipse.MouseEnter += Ellipse_MouseEnter;
                ellipse.MouseLeave += Ellipse_MouseLeave;
                //Points.Add(ellipse);
                Canvas.SetLeft(ellipse, city.x + 596);
                Canvas.SetTop(ellipse, city.y + 596);
                CityCanvas.Children.Add(ellipse);
            }
        }

        private void Ellipse_MouseLeave(object sender, MouseEventArgs e)
        {
            if(!IsPopupStatic)
                ActivePopup.IsOpen = false;
        }

        private void Ellipse_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ActivePopup != null)
                ActivePopup.IsOpen = false;
            IsPopupStatic = true;
            var s = sender as Ellipse;
            int number = 0;
            if(int.TryParse(s.Name.Substring(7), out number))
            {
                var popup = new Popup()
                {
                    Name = "popup" + number.ToString(),
                    Margin = new Thickness(s.Margin.Left, s.Margin.Top, 0, 0),
                    Width = 220,
                    Height = 320,
                    IsOpen = true
                    
                };
                popup.PopupAnimation = PopupAnimation.Slide;
                popup.Placement = PlacementMode.MousePoint;               
                var stck = new StackPanel()
                {
                    Name = "StackPanel" + number.ToString()
                };
                stck.Background = new SolidColorBrush(Colors.LightGray);
                var txt1 = new TextBlock()
                {
                    Name = "CityName" + number.ToString(),
                    Text = "City name: " + Cities[number].CityName,
                    Margin = new Thickness(10, 10, 10, 0),
                    FontSize = 16
                };
                txt1.Background = new SolidColorBrush(Colors.White);
                stck.Children.Add(txt1);
                var txt2 = new TextBlock()
                {
                    Name = "CityNumber" + number.ToString(),
                    Text = "City number: " + Cities[number].CityNumber.ToString(),
                    Margin = new Thickness(10, 0, 10, 0),
                    FontSize = 16
                };
                txt2.Background = new SolidColorBrush(Colors.White);
                stck.Children.Add(txt2);
                var txt3 = new TextBlock()
                {
                    Name = "CityX" + number.ToString(),
                    Text = "X: " + Cities[number].x.ToString(),
                    Margin = new Thickness(10, 0, 10, 0),
                    FontSize = 16
                };
                txt3.Background = new SolidColorBrush(Colors.White);
                stck.Children.Add(txt3);
                var txt4 = new TextBlock()
                {
                    Name = "CityY" + number.ToString(),
                    Text = "Y: " + Cities[number].y.ToString(),
                    Margin = new Thickness(10, 0, 10, 0),
                    FontSize = 16
                };
                txt4.Background = new SolidColorBrush(Colors.White);
                stck.Children.Add(txt4);
                var obsc = new ObservableCollection<NewFloat>();
                for(int i = 0; i < trails.GetLength(); i++)
                {
                    if(trails[number, i] > 0.01)
                        obsc.Add(new NewFloat() { _float = trails[number, i], _to = i, _distance = graph[number, i]});
                }
                var lsv = new ListView()
                {
                    Margin = new Thickness(10, 10, 10, 0),
                    Height = 200
                };
                lsv.ItemsSource = obsc;
                stck.Children.Add(lsv);
                popup.Child = stck;
                popup.MouseLeave += Popup_MouseLeave;
                ActivePopup = popup;
            }
        }

        private void Popup_MouseLeave(object sender, MouseEventArgs e)
        {
            ActivePopup.IsOpen = false;
        }

        void Ellipse_MouseEnter(object sender, MouseEventArgs e)
        {
            if(ActivePopup != null)
                ActivePopup.IsOpen = false;
            IsPopupStatic = false;
            var s = sender as Ellipse;
            int number = 0;
            if (int.TryParse(s.Name.Substring(7), out number))
            {
                var popup = new Popup()
                {
                    Name = "popup" + number.ToString(),
                    Margin = new Thickness(s.Margin.Left, s.Margin.Top, 0, 0),
                    Width = 220,
                    Height = 320,
                    IsOpen = true

                };
                popup.PopupAnimation = PopupAnimation.Slide;
                popup.Placement = PlacementMode.MousePoint;
                var stck = new StackPanel()
                {
                    Name = "StackPanel" + number.ToString()
                };
                stck.Background = new SolidColorBrush(Colors.LightGray);
                var txt1 = new TextBlock()
                {
                    Name = "CityName" + number.ToString(),
                    Text = "City name: " + Cities[number].CityName,
                    Margin = new Thickness(10, 10, 10, 0),
                    FontSize = 16
                };
                txt1.Background = new SolidColorBrush(Colors.White);
                stck.Children.Add(txt1);
                var txt2 = new TextBlock()
                {
                    Name = "CityNumber" + number.ToString(),
                    Text = "City number: " + Cities[number].CityNumber.ToString(),
                    Margin = new Thickness(10, 0, 10, 0),
                    FontSize = 16
                };
                txt2.Background = new SolidColorBrush(Colors.White);
                stck.Children.Add(txt2);
                var txt3 = new TextBlock()
                {
                    Name = "CityX" + number.ToString(),
                    Text = "X: " + Cities[number].x.ToString(),
                    Margin = new Thickness(10, 0, 10, 0),
                    FontSize = 16
                };
                txt3.Background = new SolidColorBrush(Colors.White);
                stck.Children.Add(txt3);
                var txt4 = new TextBlock()
                {
                    Name = "CityY" + number.ToString(),
                    Text = "Y: " + Cities[number].y.ToString(),
                    Margin = new Thickness(10, 0, 10, 0),
                    FontSize = 16
                };
                txt4.Background = new SolidColorBrush(Colors.White);
                stck.Children.Add(txt4);
                var obsc = new ObservableCollection<NewFloat>();
                for (int i = 0; i < trails.GetLength(); i++)
                {
                    obsc.Add(new NewFloat() { _float = trails[number, i], _to = i, _distance = graph[number, i] });
                }
                var lsv = new ListView()
                {
                    Margin = new Thickness(10, 10, 10, 0),
                    Height = 200
                };
                lsv.ItemsSource = obsc;
                stck.Children.Add(lsv);
                popup.Child = stck;

                ActivePopup = popup;
            }
        }

        public void ClearCanvas()
        {
            CityCanvas.Children.Clear();
        }

        private void ZoomOut_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var scaler = GridxD.LayoutTransform as ScaleTransform;
            var stackScaler = StackZoom.LayoutTransform as ScaleTransform;
            if (scaler == null)
            {
                GridxD.LayoutTransform = new ScaleTransform(0.95, 0.95);
                ScrollText.Text = "95%";
                StackZoom.LayoutTransform = new ScaleTransform(1.0 / 0.95, 1.0 / 0.95);
                StackZoom.Margin = new Thickness(10 / 0.95, 10 / 0.95, 0, 0);
            }
            else
            {
                if (scaler.HasAnimatedProperties)
                {
                    scaler.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    scaler.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                }
                scaler.ScaleX -= 0.05;
                scaler.ScaleY -= 0.05;
                ScrollText.Text = ((int)(scaler.ScaleY * 100)).ToString() + "%";
                if (stackScaler.HasAnimatedProperties)
                {
                    stackScaler.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    stackScaler.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                }
                stackScaler.ScaleX = 1.0 / scaler.ScaleX;
                stackScaler.ScaleY = 1.0 / scaler.ScaleY;
                StackZoom.Margin = new Thickness(10 / scaler.ScaleX, 10 / scaler.ScaleY, 0, 0);
            }
        }

        private void ZoomIn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var scaler = GridxD.LayoutTransform as ScaleTransform;
            var stackScaler = StackZoom.LayoutTransform as ScaleTransform;
            if (scaler == null)
            {
                GridxD.LayoutTransform = new ScaleTransform(1.05, 1.05);
                ScrollText.Text = "105%";
                StackZoom.LayoutTransform = new ScaleTransform(1.0 / 1.05, 1.0 / 1.05);
                StackZoom.Margin = new Thickness(10 / 1.05, 10 / 1.05, 0, 0);
            }
            else
            {
                if (scaler.HasAnimatedProperties)
                {
                    scaler.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    scaler.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                }
                scaler.ScaleX += 0.05;
                scaler.ScaleY += 0.05;
                ScrollText.Text = ((int)(scaler.ScaleY * 100)).ToString() + "%";
                if (stackScaler.HasAnimatedProperties)
                {
                    stackScaler.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    stackScaler.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                }
                stackScaler.ScaleX = 1.0 / scaler.ScaleX;
                stackScaler.ScaleY = 1.0 / scaler.ScaleY;
                StackZoom.Margin = new Thickness(10 / scaler.ScaleX, 10 / scaler.ScaleY, 0, 0);

            }
        }

        private void CityScrollViewer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftCtrl:
                    CTRLPressed = true;
                    break;
            }

        }

        private void CityScrollViewer_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftCtrl:
                    CTRLPressed = false;
                    break;
            }
        }
    }
}
