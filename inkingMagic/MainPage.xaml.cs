using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Linq;
using Windows.UI.Xaml.Shapes;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.Globalization;
using Windows.UI.Text.Core;
using SimpleInk;
//using System.Drawing;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace inkingMagic
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        InkRecognizerContainer inkRecognizerContainer = null;
        private IReadOnlyList<InkRecognizer> recoView = null;
        private Language previousInputLanguage = null;
        private CoreTextServicesManager textServiceManager = null;

        public MainPage()
        {
            this.InitializeComponent();

            ink.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Touch | Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Mouse;
            ink.InkPresenter.StrokeInput.StrokeContinued += StrokeInput_StrokeContinued;

           ink.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;

            var vec1 = new Vector() { Start = new Point() { X = 0, Y = 0 }, End = new Point { X = 1, Y = 1 } };
            var vec2 = new Vector() { Start = new Point() { X = 0, Y = 0 }, End = new Point { X = 1, Y = 0 } };

            System.Diagnostics.Debug.WriteLine("Ängle =" + vec2.Angle(vec1));



            inkRecognizerContainer = new InkRecognizerContainer();
            recoView = inkRecognizerContainer.GetRecognizers();
         

            // Set the text services so we can query when language changes
            textServiceManager = CoreTextServicesManager.GetForCurrentView();
            textServiceManager.InputLanguageChanged += TextServiceManager_InputLanguageChanged;

            SetDefaultRecognizerByCurrentInputMethodLanguageTag();


            
        }


        async void OnRecognizeAsync(object sender, RoutedEventArgs e)
        {
            IReadOnlyList<InkStroke> currentStrokes = ink.InkPresenter.StrokeContainer.GetStrokes();
            if (currentStrokes.Count > 0)
            {
                RecognizeBtn.IsEnabled = false;
              
                var recognitionResults = await inkRecognizerContainer.RecognizeAsync(ink.InkPresenter.StrokeContainer, InkRecognitionTarget.All);

                if (recognitionResults.Count > 0)
                {
                    // Display recognition result
                    string str = "";
                    foreach (var r in recognitionResults)
                    {
                        str += " " + r.GetTextCandidates()[0];
                    }
                   // shapeText.Text = str;
                }
                else
                {
                    shapeText.Text = "¯\\_(ツ)_/¯";
                }

                RecognizeBtn.IsEnabled = true;
            }
            else
            {
                shapeText.Text = "¯\\_(ツ)_/¯";
            }
        }


        private void SetDefaultRecognizerByCurrentInputMethodLanguageTag()
        {
            // Query recognizer name based on current input method language tag (bcp47 tag)
            Language currentInputLanguage = textServiceManager.InputLanguage;

            if (currentInputLanguage != previousInputLanguage)
            {
                // try query with the full BCP47 name
                string recognizerName = RecognizerHelper.LanguageTagToRecognizerName(currentInputLanguage.LanguageTag);

                if (recognizerName != string.Empty)
                {
                    for (int index = 0; index < recoView.Count; index++)
                    {
                        if (recoView[index].Name == recognizerName)
                        {
                            inkRecognizerContainer.SetDefaultRecognizer(recoView[index]);
                            previousInputLanguage = currentInputLanguage;
                            break;
                        }
                    }
                }
            }
        }

        private void TextServiceManager_InputLanguageChanged(CoreTextServicesManager sender, object args)
        {
            SetDefaultRecognizerByCurrentInputMethodLanguageTag();
        }

        private void InkPresenter_StrokesCollected(Windows.UI.Input.Inking.InkPresenter sender, Windows.UI.Input.Inking.InkStrokesCollectedEventArgs args)
        {

            var strokes = args.Strokes.ToList();
            var p = new List<Point>();

            strokes.ForEach(stroke =>
            stroke.GetInkPoints().ToList().ForEach(point =>
            {
                p.Add(point.Position);
                //System.Diagnostics.Debug.WriteLine(point.Position);
            }));

            Detect(p);
            // System.Diagnostics.Debug.WriteLine("colected");
        }

        private void StrokeInput_StrokeContinued(Windows.UI.Input.Inking.InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            var strokes = ink.InkPresenter.StrokeContainer.GetStrokes();
            var str = strokes.ToList();

        }

        private async void Detect(List<Point> points)
        {
            var vectors = new List<Vector>();
            vectors.Add(new Vector() { Start = points[0], End = points[1] });

            var added = 0;
            var skiped = 0;

            for (int i = 2; i < points.Count; i++)
            {
                var vector1 = vectors[vectors.Count - 1];

                var vector2 = new Vector() { Start = points[i - 1], End = points[i] };

                if (Math.Abs(vector1.Angle(vector2)) > 40)
                {

                    System.Diagnostics.Debug.WriteLine("angle " + Math.Abs(vector1.Angle(vector2)));
                    vectors.Add(vector2);
                    added++;

                }
                else
                {
                    vectors[vectors.Count - 1] = new Vector() { Start = vector1.Start, End = vector2.End };
                    skiped++;
                }
            }

            vectors = vectors.Where(v => v.Long > 10).ToList();
            System.Diagnostics.Debug.WriteLine("added {0}  - skiped {1}", added, skiped);
            System.Diagnostics.Debug.WriteLine("Cantidad de vectores detectados: {0}", vectors.Count);

            if (vectors.Count == 0)
                return;

            var shape = new Shape(vectors);

            System.Diagnostics.Debug.WriteLine("the shape is {0}", shape.IsClosed ? "closed" : "open", 1);

            var dp = shape.DrawPoints;
            dp.Add(dp.First());
            var vecs = dp.Zip(dp.Skip(1), (a, b) => new Vector() { Start = a, End = b }).ToList();

           // if (shape.IsClosed || shape.Vectors.Count < 2)
                vectors.ForEach(v =>
                {
                    var line = new Line();

                    line.StrokeThickness = 4;

                    line.X1 = v.Start.X;
                    line.X2 = v.End.X;
                    line.Y1 = v.Start.Y;
                    line.Y2 = v.End.Y;

                    line.Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 100, 0));

                    canvas.Children.Add(line);
                });



            vectors.ForEach(vector =>
              System.Diagnostics.Debug.WriteLine("Vector: {0} a {1}   => {2}  Long => {3}", vector.Start, vector.End, vector.ToZero, vector.Long)
            );

            var rec = shape.DetectShape();

            if (rec == "Nothing")
              OnRecognizeAsync(null, null);
            else
                shapeText.Text = rec;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.canvas.Children.Clear();
            this.ink.InkPresenter.StrokeContainer.Clear();
        }
    }
    public class Vector
    {
        public Point Start { get; set; }

        public Point End { get; set; }

        public double Long { get { return Math.Sqrt(this.X * this.X + this.Y * this.Y); } }

        public Point ToZero
        {
            get { return new Point(End.X - Start.X, End.Y - Start.Y); }
        }

        public double X { get { return this.ToZero.X; } }

        public double Y { get { return this.ToZero.Y; } }

        public double Angle(Vector other)
        {
            return (Math.Atan2(other.Y, other.X) - Math.Atan2(this.Y, this.X)) * 180 / Math.PI;
        }

        public double Distance(Vector other)
        {
            return Math.Sqrt(Math.Pow(this.Start.X - other.End.X, 2) + Math.Pow(this.Start.Y - other.End.Y, 2));
        }
    }

    public class Shape
    {
        public List<Vector> Vectors { get; set; }

        public List<double> Angles
        {

            get
            {
                var vectors2 = new List<Vector>();
                vectors2.AddRange(this.Vectors.Skip(1));
                vectors2.Add(this.Vectors.First());
                return Vectors.Zip(vectors2, (a, b) => a.Angle(b)).ToList();
            }
        }

        public bool IsClosed
        {
            get
            {
                return
                    Vectors.First().Distance(Vectors.Last()) * 3 < BoundingRect.Height ||
                    Vectors.First().Distance(Vectors.Last()) * 3 < BoundingRect.Width;
            }
        }

        public Rect BoundingRect { get { return RectHelper.FromPoints(new Point(Vectors.Min(v => v.X), Vectors.Min(v => v.Y)), new Point(Vectors.Max(v => v.X), Vectors.Max(v => v.Y))); } }

        public Shape() { }

        public Shape(List<Vector> vectors)
        {
            this.Vectors = vectors;
        }

        public List<Point> DrawPoints
        {
            get
            {
                var vec = Vectors.ToList();
                vec.Add(vec.First());
                return vec.Zip(vec.Skip(1), (a, b) => new Point((a.End.X + b.Start.X) / 2, (a.End.Y + b.Start.Y) / 2)).ToList();
            }
        }

        private List<Vector> MergeSimilarVectors(int ExpectedSides)
        {
            var p = new Dictionary<string, float>() { { "Nothing", 0 }, { "Square", 4 }, { "Triangle", 3 }, { "Line", 1 }, { "Circle", 0 } };

            var vectors = this.Vectors.ToList();
            return vectors;
        }

        internal string DetectShape()
        {
            var p = new Dictionary<string, float>() { { "Nothing", 30 }, { "Square", SquareScore() }, { "Triangle", TriangleScore() }, { "Line", LineScore() }, { "Circle", CircleScore() } };
            var shape = p.ToList().OrderBy(x => x.Value).Last().Key;
            return shape;
        }

        public float SquareScore()
        {
            if (!IsClosed)
                return 0;

            var score = 0;
            var angles = Angles.Select(a => Math.Abs(a) % 180).ToList();
            var rects = angles.Where(a => 70 < a && a < 120).ToList();
            var wides = angles.Where(a => 160 < a || a < 20).ToList();
            int difference = angles.Count - rects.Count - wides.Count;

            if (rects.Count == 4)
                score += 100;

            score += -30 * difference;

            return score;
        }

        public float TriangleScore()
        {
            if (!IsClosed)
                return 0;

            var score = 0;
            var angles = Angles.Select(a => Math.Abs(a) % 180).ToList();
            // var rects = angles.Where(a => 70 < a && a < 120).ToList();
            var wides = angles.Where(a => 160 < a || a < 20).ToList();
            int difference = angles.Count - wides.Count;

            if (difference == 3)

                score += 100;

            return score;
        }

        public float LineScore()
        {
            if (IsClosed)
                return 0;

            var score = 0;
            var angles = Angles.Select(a => Math.Abs(a) % 180).ToList();
            // var rects = angles.Where(a => 70 < a && a < 120).ToList();
            var wides = angles.Where(a => 160 < a || a < 20).ToList();
            int difference = angles.Count - wides.Count;

            if (difference < 4)

                score += 100;

            return score;
        }

        public float CircleScore()
        {
            if (!IsClosed || this.Vectors.Count < 5)
                return 0;

            var score = 0;
            var angles = Angles.Select(a => Math.Abs(a) % 180).Take(Angles.Count - 1).ToList();
            var acutes = angles.Where(a => 25 < a && a < 45).ToList();
            var wides = angles.Where(a => 160 < a || a < 20).ToList();
            int difference = angles.Count - wides.Count;

            var positive = Angles.Where(x => x % 180 > 0).Take(Angles.Count - 1).ToList().Count;

            var negative = angles.Count - positive;

            if (positive > 2 && negative > 2)
                return 0;


            score += difference * 15 - acutes.Count * 30;

            return score;
        }

    }
}
