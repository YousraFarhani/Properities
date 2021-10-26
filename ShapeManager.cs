using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace tuto_epic
{
    class ShapeManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void notify(string propertyName)                                                                  //notification interface                                
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ObservableCollection<Shape> Shapes { get; set; } = new ObservableCollection<Shape>();                //Shapes collection, bound to Canvas
        public SolidColorBrush ActualFillBrush { get; set; } = new SolidColorBrush(Color.FromRgb(255, 255, 255));   //Fill brush
        public SolidColorBrush ActualOutBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0, 0, 0));          //Outline = stroke brush

        public ImageBrush BackgroundGrid                                                                            //grid
        {
            get { return backgroundGrid; }
            set { backgroundGrid = value; notify("BackgroundGrid"); }
        }
        private ImageBrush backgroundGrid;

        public string Status                                                                                        //Shape properties infotext
        {
            get { return status; }
            set { status = value; notify("Status"); }
        }
        private string status;

        private double x, y;
        private DimensionPolygone dw;
        public Shape SelectedShape { get; set; }
        private Polyline polyLine;
        private Grid grid = new Grid();

        public bool PolyLineMode { get; set; }

        public bool Snap { get; set; } = true;
        public bool DisplayGrid { get; set; } = true;

        public int Clicked                                                                                          //helper variable, serves as notification of Shape being clicked
        {
            get { return clicked; }
            set { clicked = value; OnClickedChanged(); }
        }
        private int clicked;

        public event EventHandler ClickedChanged;

        public void OnClickedChanged()                                                                              //click event is fired (see MainWindow.cs handler]
        {
            if (ClickedChanged != null)
                ClickedChanged(this, EventArgs.Empty);
        }

        private Path myPath;
        private double transformX = 0;
        private double transformY = 0;

        private double originX = 0;
        private double originY = 0;

        public List<Shape> CombiPair { get; set; } = new List<Shape>();


        public void UpdateGrid(double cnvWidth, double cnvHeight, int spacing)
        {
            grid.Spacing = spacing;
            grid.Width = cnvWidth;
            grid.Height = cnvHeight;
            BackgroundGrid = grid.UdateGrid(DisplayGrid);
        }

        public void InsertShape(string option)
        {
            Shape s = null;
            if (option == "rectangle" || option == "ellipse" || option == "polyline" || option == "text")  //in case of interactively generated shapes or text, no dimension values are needed
            {
                dw = new DimensionPolygone();
                if (option == "polyline" || option == "text")
                {
                    dw.ThicknessOnly = true;                     //in case of polyline and text, only stroke thickness value is needed
                }
                if (option == "ellipse")
                    dw.RadiusEnable = false;
                if (option == "rectangle")
                    dw.RadiusEnable = true;
                dw.Owner = Application.Current.MainWindow;      //DimensionWindow is Parent centered
                dw.ShowDialog();

                if (!dw.OK)                                     //if values not confirmed
                {
                    return;
                }
            }

            if (option == "polyline")                  //temporary PolyLine, can result in finished polyline or closed polygon
            {
                polyLine = new Polyline()
                {
                    Stroke = ActualOutBrush,
                    StrokeThickness = dw.S,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeStartLineCap = PenLineCap.Round
                };
                s = polyLine;
            }

            else if (option == "rectangle")
            {
                RectangleGeometry r = new RectangleGeometry(new Rect(0, 0, dw.W, dw.H));
                r.RadiusX = dw.R;
                r.RadiusY = dw.R;

                myPath = new Path();
                myPath.Data = r;
                myPath.Fill = ActualFillBrush;
                myPath.Stroke = ActualOutBrush;
                myPath.StrokeThickness = dw.S;
                s = myPath;
            }

            else if (option == "ellipse")
            {
                EllipseGeometry e = new EllipseGeometry(new Rect(0, 0, dw.W, dw.H));

                myPath = new Path();
                myPath.Data = e;
                myPath.Fill = ActualFillBrush;
                myPath.Stroke = ActualOutBrush;
                myPath.StrokeThickness = dw.S;
                s = myPath;
            }

            else if (option == "duplicate")
            {
                s = myPath;
                option = SelectedShape.Tag.ToString();        //temporarily store original shape tag to option variable
            }

            else if (option == "text")
            {
                myPath.StrokeThickness = dw.S;
                s = myPath;
            }

            else                                            //polygon, combination, finished polyline
            {
                s = myPath;
            }

            s.SnapsToDevicePixels = true;
            Shapes.Add(s);                                  //Add shape to observable collection
            Canvas.SetLeft(s, 0);                           //define 0, 0 position on Canvas
            Canvas.SetTop(s, 0);
            SelectedShape = s;
            s.Tag = option;                                 //duplicate shape final tag

            s.MouseLeftButtonDown += S_MouseLeftButtonDown; //attach handlers to events                         
            s.ContextMenu = new ContextMenu();
            s.ContextMenu.Loaded += ContextMenu_Loaded;

            MenuItem item1 = null;
            MenuItem item2 = null;
            if ((string)s.Tag != "finishedpolyline")                                                                          //combinable geometries need appropriate context menu items
            {
                item1 = new MenuItem() { Name = "combiAdd", Header = "Ajouter a la pile de combinaison" };
                item1.Click += Item1_Click;
                s.ContextMenu.Items.Add(item1);
                item2 = new MenuItem() { Name = "combiRemove", Header = "Retirer de la pile de combinaison" };
                item2.Click += Item2_Click;
                s.ContextMenu.Items.Add(item2);
            }
            MenuItem item3 = new MenuItem() { Name = "front", Header = "Mettre en avant" };          //for all geomteries
            MenuItem item4 = new MenuItem() { Name = "back", Header = "Mettre en arriere plan" };
            MenuItem item5 = new MenuItem() { Name = "repaint", Header = "Colorer" };
            MenuItem item6 = new MenuItem() { Name = "duplicate", Header = "Dupliquer" };
            MenuItem item7 = new MenuItem() { Name = "delete", Header = "Supprimer" };

            item3.Click += Item3_Click;                                                             //attach handlers to context menu click events
            item4.Click += Item4_Click;
            item5.Click += Item5_Click;
            item6.Click += Item6_Click;
            item7.Click += Item7_Click;

            s.ContextMenu.Items.Add(item3);
            s.ContextMenu.Items.Add(item4);
            s.ContextMenu.Items.Add(item5);
            s.ContextMenu.Items.Add(item6);
            s.ContextMenu.Items.Add(item7);

            if (s is Polyline)
            {
                Status = string.Format("{0}, Left: {1} Top: {2}", s.ToString(), Canvas.GetLeft(SelectedShape), Canvas.GetTop(SelectedShape));   //info text for PolyLine
            }
            else if (s is Path)
            {
                Path path = s as Path;
                Geometry geo = path.Data;
                Status = string.Format("{0}, Left: {1} Top: {2} BoundsX: {3} BoundsY: {4}", geo.ToString(), Canvas.GetLeft(SelectedShape), Canvas.GetTop(SelectedShape), geo.Bounds.X, geo.Bounds.Y);   //for rest
            }
            SelectedShape = null;
        }



        private void S_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)       //on LMB click
        {
            SelectedShape = sender as Shape;                                              //identify shape
            x = e.GetPosition(SelectedShape).X;                                           //get click coordinates within shape (relative to top left corner)
            y = e.GetPosition(SelectedShape).Y;
            Clicked++;                                                                  //by changing Clicked value, MainWindow is notified, that some shape was clicked, so drag can be initiated

            if (sender is Path)                                                         //PolyLine is only temporary shape when creating finished polyline or polygon
            {
                Path path = sender as Path;                                             //identify sender
                Geometry geometry = path.Data;                                          //get his geometry
                Status = string.Format("{0}, Left: {1} Top: {2} BoundsX: {3} BoundsY: {4}", geometry.ToString(), Canvas.GetLeft(SelectedShape), Canvas.GetTop(SelectedShape), geometry.Bounds.X, geometry.Bounds.Y);
            }                                                                           //code continues in MainWindow.xaml.cs *
        }

        public void DragShape(double X, double Y)                              //Change shape position when dragged. This method is called from MainWindow, if mouse is moved over Canvas after LMB click on Shape
        {                                                                       //X, Y are mouse pointer coordinates on Canvas
            if (SelectedShape == null) { return; }
            if (Snap)                                                           //snap to grid
            {
                Point p = SnapToGrid(X - x, Y - y);
                Canvas.SetLeft(SelectedShape, p.X);
                Canvas.SetTop(SelectedShape, p.Y);
            }
            else
            {
                Canvas.SetLeft(SelectedShape, X - x);                             //when placing Shape on Canvas, mouse position within Shape must be kept (x, y)
                Canvas.SetTop(SelectedShape, Y - y);
            }

            Path path = SelectedShape as Path;
            Geometry geometry = path.Data;
            Status = string.Format("{0}, Left: {1} Top: {2} BoundsX: {3} BoundsY: {4}", geometry.ToString(), Canvas.GetLeft(SelectedShape), Canvas.GetTop(SelectedShape), geometry.Bounds.X, geometry.Bounds.Y);//update info text
        }

        public Point SnapToGrid(double X, double Y)                             //Snap to grid method. Works for Shape drag - moving or PolyLine / Polygon points placement
        {
            int boundsCorrectX = 0;
            int boundsCorrectY = 0;
            if (SelectedShape is Path)
            {
                Path path = SelectedShape as Path;
                Geometry geometry = path.Data; //as Geometry;
                boundsCorrectX = Convert.ToInt32(geometry.Bounds.X % grid.Spacing);     //in case Shape of type Path, bounding box letf / top point correction is needed
                boundsCorrectY = Convert.ToInt32(geometry.Bounds.Y % grid.Spacing);
            }

            int xFinal = 0;
            int yFinal = 0;
            int xValue = Convert.ToInt32(X);
            int yValue = Convert.ToInt32(Y);

            int xMod = xValue % grid.Spacing + boundsCorrectX;                          //in other cases bounding box correction is 0
            int yMod = yValue % grid.Spacing + boundsCorrectY;
            if (xMod < grid.Spacing / 2)                                                //snap to previous grid column
            {
                xFinal = xValue - xMod;
            }
            else
            {
                xFinal = xValue - xMod + grid.Spacing;                                  //snap to next grid column
            }
            if (yMod < grid.Spacing / 2)                                                //ditto grid line
            {
                yFinal = yValue - yMod;
            }
            else
            {
                yFinal = yValue - yMod + grid.Spacing;
            }
            Point p = new Point(xFinal, yFinal);                                        //resulting point        
            return p;
        }

        private void ContextMenu_Loaded(object sender, RoutedEventArgs e)       //inactivate some context menu items based on combination group status
        {
            ContextMenu menu = sender as ContextMenu;

            if (PolyLineMode)
                menu.Visibility = Visibility.Hidden;                            //when finishing polyline, context menu should not appear on right mouse up, if clicked on some other shape

            Shape s = menu.PlacementTarget as Shape;                            //identify shape
            SelectedShape = s;
            Path path = s as Path;
            Geometry geometry = path.Data;
            Status = string.Format("{0}, Left: {1} Top: {2} BoundsX: {3} BoundsY: {4}", geometry.ToString(), Canvas.GetLeft(SelectedShape), Canvas.GetTop(SelectedShape), geometry.Bounds.X, geometry.Bounds.Y);//update info text
            if (s is Polyline)
                return;
            if (CombiPair.Count > 1 || CombiPair.Contains(s))                   //if 2 shapes are already in group or parent shape is already present
            {
                MenuItem item = menu.Items[0] as MenuItem;
                item.IsEnabled = false;                                         //inactivate adding to group
            }
            else
            {
                MenuItem item = menu.Items[0] as MenuItem;
                item.IsEnabled = true;
            }

            if (!CombiPair.Contains(s))
            {
                MenuItem item = menu.Items[1] as MenuItem;                      //if group does not contain shape, shape cannot be removed from group
                item.IsEnabled = false;
            }
            else
            {
                MenuItem item = menu.Items[1] as MenuItem;
                item.IsEnabled = true;
            }
        }

        private void Item1_Click(object sender, RoutedEventArgs e)
        {
            CombiPair.Add(SelectedShape);
        }

        private void Item2_Click(object sender, RoutedEventArgs e)
        {
            CombiPair.Remove(SelectedShape);
        }

        private void Item3_Click(object sender, RoutedEventArgs e)
        {
            Shapes.Remove(SelectedShape);                                        //remove
            Shapes.Add(SelectedShape);                                          //then add to top
        }

        private void Item4_Click(object sender, RoutedEventArgs e)
        {
            Shapes.Remove(SelectedShape);                                       //remove
            Shapes.Insert(0, SelectedShape);                                    //then insert at bottom position
        }

        private void Item5_Click(object sender, RoutedEventArgs e)
        {
            Shape s = SelectedShape;
            ColorPickerWindow cpw = new ColorPickerWindow();
            if (s.Fill != null)                                                    //for finished polyline
                cpw.SelectedFillBrush = s.Fill as SolidColorBrush;                //restore last selection
            cpw.SelectedOutBrush = s.Stroke as SolidColorBrush;
            cpw.Owner = Application.Current.MainWindow;
            cpw.ShowDialog();
            if (cpw.OK)
            {
                if (s.Fill != null)
                    s.Fill = cpw.SelectedFillBrush;
                s.Stroke = cpw.SelectedOutBrush;
            }
        }

        private void Item6_Click(object sender, RoutedEventArgs e)
        {
            Path p = SelectedShape as Path;
            Geometry g = p.Data; //as Geometry;
            Geometry clonedGeometry = g.Clone();
            myPath = new Path();
            myPath.Data = clonedGeometry;
            myPath.Fill = p.Fill;
            myPath.Stroke = p.Stroke;
            myPath.StrokeThickness = p.StrokeThickness;
            InsertShape("duplicate");
        }

        private void Item7_Click(object sender, RoutedEventArgs e)
        {
            Shapes.Remove(SelectedShape);
            if (CombiPair.Contains(SelectedShape))
                CombiPair.Remove(SelectedShape);
        }
        private void delete_Click(object sender, RoutedEventArgs e)
        {
            Shapes.Remove(SelectedShape);
            if (CombiPair.Contains(SelectedShape))
                CombiPair.Remove(SelectedShape);
        }

        public void ConvertPolyLineToGeometry(Polyline polyLine, bool closed) //Convert PolyLine to polygon, if shaped was closed during creation
        {
            PathGeometry pathGeom = new PathGeometry();
            PathFigure figure = new PathFigure();
            figure.IsClosed = closed;
            if (closed)                                             //for polygon
            {
                int index = 0;
                foreach (Point point in polyLine.Points)
                {
                    if (index == 0)                                 //define first point of polyline as startpoint
                        figure.StartPoint = point;
                    else if (index == polyLine.Points.Count - 1)    //exclude last point, identical with startpoint
                    {
                        break;
                    }
                    else
                    {
                        figure.Segments.Add(new LineSegment((point), true));    //other points are ok
                    }
                    index++;
                }
                pathGeom.Figures.Add(figure);

                myPath = new Path();
                myPath.Data = pathGeom;
                myPath.Stroke = ActualOutBrush;
                myPath.StrokeThickness = polyLine.StrokeThickness;
                myPath.Fill = ActualFillBrush;
                InsertShape("polygon");
            }
            else
            {                                                       //for finished polyline
                int index = 0;
                foreach (Point point in polyLine.Points)
                {
                    if (index == 0)                                 //define first point of polyline as startpoint
                        figure.StartPoint = point;
                    else
                    {
                        figure.Segments.Add(new LineSegment((point), true));    //other points are ok
                    }
                    index++;
                }
                pathGeom.Figures.Add(figure);

                myPath = new Path();
                myPath.Data = pathGeom;
                myPath.Stroke = ActualOutBrush;
                myPath.StrokeThickness = polyLine.StrokeThickness;
                InsertShape("finishedpolyline");
            }
        }

        public void RecalculateGeometryBounds()                   //recalculate geometry after drag - move. convert Canvas Left and Top to geometry Bounds.X , Y.
        {
            Geometry geometry = null;

            Path path = SelectedShape as Path;
            geometry = path.Data; //as Geometry;
            if (geometry is PathGeometry)                       //polygon or finishedpolyline are calculated point by point
            {
                double deltaX = Canvas.GetLeft(SelectedShape);
                double deltaY = Canvas.GetTop(SelectedShape);
                PathGeometry pathGeometry = geometry as PathGeometry;
                PathFigure figure = pathGeometry.Figures[0];
                Point newStartPoint = new Point(figure.StartPoint.X + deltaX, figure.StartPoint.Y + deltaY);
                PathFigure newFigure = new PathFigure();
                newFigure.StartPoint = newStartPoint;
                if ((string)SelectedShape.Tag == "polygon")
                    newFigure.IsClosed = true;
                foreach (LineSegment segment in figure.Segments)
                {
                    double oldX = segment.Point.X;
                    double oldY = segment.Point.Y;
                    LineSegment newSegment = new LineSegment(new Point(oldX + deltaX, oldY + deltaY), true);
                    newFigure.Segments.Add(newSegment);
                }
                pathGeometry.Figures.Clear();
                pathGeometry.Figures.Add(newFigure);
            }
            else
            {
                transformX = geometry.Bounds.X + Canvas.GetLeft(SelectedShape);
                transformY = geometry.Bounds.Y + Canvas.GetTop(SelectedShape);
                geometry.Transform = new TranslateTransform(transformX, transformY);
            }

            Canvas.SetLeft(SelectedShape, 0);
            Canvas.SetTop(SelectedShape, 0);
            Status = string.Format("{0}, Left: {1} Top: {2} BoundsX: {3} BoundsY: {4}", geometry.ToString(), Canvas.GetLeft(SelectedShape), Canvas.GetTop(SelectedShape), geometry.Bounds.X, geometry.Bounds.Y);
        }

        public void Combine(int selectedIndex)
        {
            if (CombiPair.Count != 2)
            {
                MessageBox.Show("La pile de combinaison doit contenir deux polygones au minimum.");
                return;
            }
            double x1 = 0;                                                      //Bounding box left / top corner coordinates of combination group members
            double y1 = 0;
            double x2 = 0;
            double y2 = 0;

            int count = 1;                                                      //counter for 2 group members
            SolidColorBrush combinationFill = null;
            SolidColorBrush combinationStroke = null;
            double combinationStrokeThickness = 0;
            CombinedGeometry combination = new CombinedGeometry();
            foreach (Shape s in CombiPair)
            {
                Path path = s as Path;
                Geometry geometry = path.Data as Geometry;// null;           //abstract class, from which PathGeometry and CombinedGeometry are derived (both are possible)

                if (count == 1)
                {
                    combination.Geometry1 = geometry;                       //define 1. member of combined geometry
                    combinationFill = s.Fill as SolidColorBrush;
                    combinationStroke = s.Stroke as SolidColorBrush;
                    combinationStrokeThickness = s.StrokeThickness;
                    x1 = Math.Round(geometry.Bounds.X);                                 //locate bounding box 1
                    y1 = Math.Round(geometry.Bounds.Y);
                }

                if (count == 2)
                {
                    combination.Geometry2 = geometry;                       //define 2. member of combined geometry
                    x2 = Math.Round(geometry.Bounds.X);                                 //locate bounding box 2
                    y2 = Math.Round(geometry.Bounds.Y);
                }
                count++;
            }

            switch (selectedIndex)                                              //set combination mode
            {
                case 0:
                    combination.GeometryCombineMode = GeometryCombineMode.Union;
                    break;
                case 1:
                    combination.GeometryCombineMode = GeometryCombineMode.Intersect;
                    break;
                case 2:
                    combination.GeometryCombineMode = GeometryCombineMode.Exclude;
                    break;
                case 3:
                    combination.GeometryCombineMode = GeometryCombineMode.Xor;
                    break;
            }

            double x = Math.Round(combination.Bounds.X);                                        //check bounding box of resulting geometry
            double y = Math.Round(combination.Bounds.Y);
            if (double.IsNegativeInfinity(x) || double.IsPositiveInfinity(x) || double.IsNaN(x) || double.IsNegativeInfinity(y) || double.IsPositiveInfinity(y) || double.IsNaN(y))
            {
                throw (new ArgumentException("Cette combinaison est impossible"));
            }
            if (x > 0 || y > 0)                                                     //if result is not located at 0, 0 (new Shape must start at 0, 0), repositioning must be done (method first pass)
            {
                originX = x;                                                        //save original combination bounding box position
                originY = y;
                foreach (Shape s in CombiPair)
                {
                    Path p = s as Path;
                    Geometry g = p.Data as Geometry;
                    if ((string)s.Tag == "polygon")
                    {
                        g.Transform = new TranslateTransform(-x, -y);            //reposition original components to get resulting combination geometry at 0, 0. 
                    }                                                               //Polygon requires different code, as it was (likely) not generated at 0, 0, unlike Rectangle or Ellipse, that are always generated at 0, 0. 
                    else
                    {
                        g.Transform = new TranslateTransform(Math.Round(g.Bounds.X - x), Math.Round(g.Bounds.Y - y));   //reposition original components to get resulting combination geometry at 0, 0. NOT WORKING WITH POLYGON
                    }
                }                                                                               //after repositioning, relative distances betwen two members are kept, just whole group bounding box is at 0, 0
                Combine(selectedIndex);                                                         //Whole method is called again with corrected bounds of both members

            }
            else
            {
                foreach (Shape s in CombiPair)                          //in second method pass, resulting combination bounding box should be at 0, 0 now
                {
                    Shapes.Remove(s);                                   //remove originals from Canvas bound collection
                }
                CombiPair.Clear();
                myPath = new Path();
                myPath.Fill = combinationFill;
                myPath.Stroke = combinationStroke;
                myPath.StrokeThickness = combinationStrokeThickness;
                combination.Transform = new TranslateTransform(originX, originY);   //restore combination correct original position
                myPath.Data = combination;
                InsertShape("combinaison");
            }
        }

    }
}
