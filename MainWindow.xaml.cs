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
using System.Windows.Threading;
using Microsoft.Win32;



namespace tuto_epic
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ShapeManager manager;

        private Int32Collection gridSpacingData = new Int32Collection() { 5, 10, 15, 20, 25, 30, 35, 40, 50, 60, 70, 80, 100 };


        private Polyline polyLine;
        private Line tempLine;
        private Line oldLine;
        // Zoom
        private Double zoomMax = 5;
        private Double zoomMin = 0.5;
        private Double zoomSpeed = 0.001;
        private Double zoom = 1;

        public MainWindow()
        {
            InitializeComponent();
        }
        // Zoom on Mouse wheel
        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            zoom += zoomSpeed * e.Delta; // Ajust zooming speed (e.Delta = Mouse spin value )
            if (zoom < zoomMin) { zoom = zoomMin; } // Limit Min Scale
            if (zoom > zoomMax) { zoom = zoomMax; } // Limit Max Scale

            Point mousePos = e.GetPosition(Itc);

            if (zoom > 1)
            {
                Itc.RenderTransform = new ScaleTransform(zoom, zoom, mousePos.X, mousePos.Y); // transform Canvas size from mouse position
            }
            else
            {
                Itc.RenderTransform = new ScaleTransform(zoom, zoom); // transform Canvas size
            }
        }

        /********/
        private void Polygone_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
        private void minimizeButton_Click(object sender, RoutedEventArgs e)
        {
               WindowState = WindowState.Minimized;
        }
        private void maximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }
        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void import_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select a picture";
            op.Filter = "*.jpg,.png,.jpeg|*.jpg;*.jpeg;*.png|" +
              "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
              " (*.png)|*.png";
            if (op.ShowDialog() == true)
            {
                //imgPhoto.Source = new BitmapImage(new Uri(op.FileName));

            }

        }

        private void ExitMenu_Click(object sender, RoutedEventArgs e)                               //exit application
        {
            Quitterpop quit = new Quitterpop();// add text here
            quit.ShowDialog();
            if (quit.DialogResult == true)
            {
                Close();
            }

        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Enregistrer enreg = new Enregistrer();// add text here
            enreg.ShowDialog();
            if (enreg.DialogResult == false)
            {
                Close();
            }

        }
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            Exporter export = new Exporter();// add text here
            export.ShowDialog();
            if (export.DialogResult == false)
            {
                Close();
            }

        }


        private void mm_Click(object sender, RoutedEventArgs e)
        {

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            manager = new ShapeManager();
            DataContext = manager;
            manager.ClickedChanged += Manager_ClickedChanged;       //attach event handler to Clicked property changed event in manager
            Itc.MouseLeftButtonUp += Itc_MouseLeftButtonUp;
            SpacingCombo.ItemsSource = gridSpacingData;
            SpacingCombo.SelectedIndex = 3;
            manager.UpdateGrid(Itc.ActualWidth, Itc.ActualHeight, (int)SpacingCombo.SelectedItem);

        }

        private void Manager_ClickedChanged(object sender, EventArgs e)// * If shape was Clicked in manager event handler: initiate drag - move
        {
            if (manager.PolyLineMode)                                  //if PolyLine is being drawn, do not interrupt
                return;
            Itc.MouseMove += Itc_MouseMove;                             //otherwise start observing mouse movement **                                         
            Itc.Cursor = Cursors.Hand;
        }

        private void Itc_MouseMove(object sender, MouseEventArgs e)         // **
        {
            if (manager.PolyLineMode && tempLine != null)                   //if in polyline mode
            {
                Point point = new Point();                                  //then draw temporary lines of polyline
                if (manager.Snap)
                {
                    point = manager.SnapToGrid(e.GetPosition(Itc).X, e.GetPosition(Itc).Y);     //snap to grid
                }
                else
                {
                    point.X = e.GetPosition(Itc).X;                                                 //no snap
                    point.Y = e.GetPosition(Itc).Y;
                }
                tempLine.X2 = point.X;
                tempLine.Y2 = point.Y;
                double distance = Point.Subtract(new Point(tempLine.X1, tempLine.Y1), new Point(tempLine.X2, tempLine.Y2)).Length;
                manager.Status = distance.ToString();
            }
            else if (!manager.PolyLineMode)                                          //else drag - move selected shape
            {
                manager.Status = "Drag active";
                manager.DragShape(e.GetPosition(Itc).X, e.GetPosition(Itc).Y);      //initiate drag - move process
            }

        }

        private void Itc_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (manager.PolyLineMode)
                return;
            Itc.MouseMove -= Itc_MouseMove;                                 //finish drag - move, drop shape on position
            Itc.Cursor = Cursors.Arrow;

            if (manager.SelectedShape is Path)
            {
                manager.RecalculateGeometryBounds();
            }
        }

        private void SelectColorMenu_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow cpw = new ColorPickerWindow();                //open Color picker window
            cpw.Owner = this;
            cpw.SelectedFillBrush = manager.ActualFillBrush;                //restore last selection
            cpw.SelectedOutBrush = manager.ActualOutBrush;
            cpw.ShowDialog();
            if (cpw.OK)
            {
                manager.ActualFillBrush = cpw.SelectedFillBrush;
                manager.ActualOutBrush = cpw.SelectedOutBrush;
            }
        }



        private void RectMenu_Click(object sender, RoutedEventArgs e)       //Insert rectangle
        {
            manager.InsertShape("rectangle");
        }

        private void EllMenu_Click(object sender, RoutedEventArgs e)        //Insert ellipse
        {
            manager.InsertShape("ellipse");
        }

        private void PolyLineMenu_Click(object sender, RoutedEventArgs e)   //start drawing polyLine
        {
            manager.InsertShape("polyline");
            int count = manager.Shapes.Count;
            if (count > 0 && manager.Shapes[count - 1] is Polyline)
            {
                manager.PolyLineMode = true;
                polyLine = manager.Shapes[count - 1] as Polyline;           //polyline   starting
                Itc.Cursor = Cursors.Cross;
                Itc.MouseLeftButtonDown += Itc_MouseLeftButtonDown;             //attach handler to left mouse button click (add point to polyLine)
                Itc.MouseRightButtonUp += Itc_MouseRightButtonUp;                   //attach handler to right mouse button click event (add last point to polyLine)    
            }
        }

        private void Itc_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Itc.MouseMove -= Itc_MouseMove;
            if (oldLine != null)
            {
                manager.Shapes.Remove(oldLine);                                             //delete temporary line
            }

            Point point = new Point();
            if (manager.Snap)
            {
                point = manager.SnapToGrid(e.GetPosition(Itc).X, e.GetPosition(Itc).Y);     //snap to grid
            }
            else
            {
                point = new Point(e.GetPosition(Itc).X, e.GetPosition(Itc).Y);                                                 //no snap
            }
            polyLine.Points.Add(point);
            tempLine = new Line() { Stroke = Brushes.LightPink, StrokeThickness = 1 };       //new temporary line
            manager.Shapes.Add(tempLine);
            tempLine.X1 = point.X;
            tempLine.Y1 = point.Y;
            tempLine.X2 = point.X;                                                  //initially endpoint of temporary line = startpoint, on mouse move endpoint position updates
            tempLine.Y2 = point.Y;
            Itc.MouseMove += Itc_MouseMove;
            oldLine = tempLine;
        }

        private void Itc_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Itc.MouseLeftButtonDown -= Itc_MouseLeftButtonDown;                                 //finish polyline creation
            Itc.MouseRightButtonUp -= Itc_MouseRightButtonUp;
            Itc.MouseMove -= Itc_MouseMove;
            Itc.Cursor = Cursors.Arrow;
            if (oldLine != null)
            {
                manager.Shapes.Remove(oldLine);
            }
            manager.SelectedShape = null;                                                               //prevent conflict with drag
            manager.PolyLineMode = false;
            Point firstPoint = polyLine.Points[0];
            int lastIndex = polyLine.Points.Count - 1;
            Point endPoint = polyLine.Points[lastIndex];
            if (firstPoint == endPoint)
            {
                MessageBox.Show("Polygone creé.");
                manager.Shapes.Remove(polyLine);
                manager.ConvertPolyLineToGeometry(polyLine, true);
            }
            else
            {
                MessageBox.Show("PolyLine creé.");
                manager.Shapes.Remove(polyLine);
                manager.ConvertPolyLineToGeometry(polyLine, false);
            }
        }

        private void SnapMenu_Click(object sender, RoutedEventArgs e)                           //switch snap to grid
        {
            if (!manager.Snap)
            {
                manager.Snap = true;
                SnapMenu.Header = "Snap to grid √";
            }
            else
            {
                manager.Snap = false;
                SnapMenu.Header = "Snap to grid";
            }
        }

        private void MainW_SizeChanged(object sender, SizeChangedEventArgs e)                   //redraw grid on window resize
        {
            if (this.IsLoaded)
            {
                manager.UpdateGrid(Itc.ActualWidth, Itc.ActualHeight, (int)SpacingCombo.SelectedItem);
            }
        }

        private void DispGridMenu_Click(object sender, RoutedEventArgs e)                       //switch visibility of grid
        {
            if (!manager.DisplayGrid)
            {
                manager.DisplayGrid = true;
                DispGridMenu.Header = "Display grid √";
                manager.UpdateGrid(Itc.ActualWidth, Itc.ActualHeight, (int)SpacingCombo.SelectedItem);
            }
            else
            {
                manager.DisplayGrid = false;
                DispGridMenu.Header = "Display grid";
                manager.UpdateGrid(Itc.ActualWidth, Itc.ActualHeight, (int)SpacingCombo.SelectedItem);
            }
        }

        private void SpacingCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)     //redraw grid with new spacing
        {
            manager.UpdateGrid(Itc.ActualWidth, Itc.ActualHeight, (int)SpacingCombo.SelectedItem);
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)                             //preview visual brush in separate window
        {
            VisualBrush visualBrush = new VisualBrush();
            visualBrush.Visual = Itc;
            BrushWindow bw = new BrushWindow();
            bw.BackgroundBrush = visualBrush;
            bw.ShowDialog();
        }
        private void DeselectMenu_Click(object sender, RoutedEventArgs e)
        {
            manager.SelectedShape = null;
            manager.Status = "";

        }

        private void DeleteAllMenu_Click(object sender, RoutedEventArgs e)                          //delete all shapes
        {
            manager.Shapes.Clear();
            manager.CombiPair.Clear();
            manager.SelectedShape = null;
            manager.Status = "";
        }


        private void UnionMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                manager.Combine(0);
            }
            catch
            {
                MessageBox.Show("Cette combinaison est impossible.");
                manager.CombiPair.Clear();
            }

        }

        private void IntersectMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                manager.Combine(1);
            }
            catch
            {
                MessageBox.Show("Cette combinaison est impossible.");
                manager.CombiPair.Clear();
            }
        }

        private void ExcludeMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                manager.Combine(2);
            }
            catch
            {
                MessageBox.Show("Cette combinaison est impossible.");
                manager.CombiPair.Clear();
            }
        }

        private void XorMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                manager.Combine(3);
            }
            catch
            {
                MessageBox.Show("Cette combinaison est impossible.");
                manager.CombiPair.Clear();
            }
        }

        private void CombiClearMenu_Click(object sender, RoutedEventArgs e)
        {
            manager.CombiPair.Clear();
            manager.CombiPair.Clear();
        }

        private void PreviewMenu_Click(object sender, RoutedEventArgs e)
        {
            manager.DisplayGrid = false;
            VisualBrush visualBrush = new VisualBrush();
            visualBrush.Visual = Itc;
            BrushWindow bw = new BrushWindow();
            bw.BackgroundBrush = visualBrush;
            manager.DisplayGrid = true;
            bw.ShowDialog();
        }

        private void Nom_de_polygone_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        /*private void ShowHelpMenu_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow hw = new HelpWindow();
            hw.Owner = this;
            hw.Show();
        }*/
    }

}
