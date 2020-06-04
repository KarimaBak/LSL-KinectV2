using System;
using System.Collections.Generic;
using Microsoft.Kinect;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;


namespace LSL_Kinect
{
    public class Dessins
    {

        public const long KINECT_MINIMAL_ID = 72057594037900000;

        public long idSqueletteChoisi { get; set; } = -1;

        /*        private MainWindow mainWindow = null;

                public Dessins(MainWindow mainWindow)
                {
                    this.mainWindow = mainWindow;
                }*/

        private Button boutonDrawId = new Button()
        {
            FontSize = 30
        };


        public Joint ScaleTo(Joint joint, double width, double height, float skeletonMaxX, float skeletonMaxY)
        {
            joint.Position = new CameraSpacePoint
            {
                X = Scale(width, skeletonMaxX, joint.Position.X),
                Y = Scale(height, skeletonMaxY, -joint.Position.Y),
                Z = joint.Position.Z    
            };

            return joint;
        }

        public  Joint ScaleTo(Joint joint, double width, double height)
        {
            return ScaleTo(joint, width, height, 1.0f, 1.0f);
        }

        public float Scale(double maxPixel, double maxSkeleton, float position)
        {
            float value = (float)((((maxPixel / maxSkeleton) / 2) * position) + (maxPixel / 2));

            if (value > maxPixel)
            {
                return (float)maxPixel;
            }

            if (value < 0)
            {
                return 0;
            }

            return value;
        }

        public  void DrawSkeleton(Canvas canvas, Body body)
        {
            //if (body == null) return;

            foreach (Joint joint in body.Joints.Values)
            {
                DrawPoint(canvas , joint);
                Etat_Main(canvas, body.HandLeftState, body.Joints[JointType.HandLeft]);
                Etat_Main(canvas, body.HandRightState, body.Joints[JointType.HandRight]);
            }
            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
            const float InferredZPositionClamp = 0.1f;
            foreach ( JointType jointType in joints.Keys)
            {
                CameraSpacePoint position = joints[jointType].Position;
                if (position.Z < 0)
                {
                    position.Z = InferredZPositionClamp;
                }
            }
            
            DrawId(canvas, body.Joints[JointType.Head], body.TrackingId);
     

            DrawLine(canvas, body.Joints[JointType.Head], body.Joints[JointType.Neck]);
            DrawLine(canvas, body.Joints[JointType.Neck], body.Joints[JointType.SpineShoulder]);
            DrawLine(canvas, body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderLeft]);
            DrawLine(canvas, body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderRight]);
            DrawLine(canvas, body.Joints[JointType.SpineShoulder], body.Joints[JointType.SpineMid]);
            DrawLine(canvas, body.Joints[JointType.ShoulderLeft], body.Joints[JointType.ElbowLeft]);
            DrawLine(canvas, body.Joints[JointType.ShoulderRight], body.Joints[JointType.ElbowRight]);
            DrawLine(canvas, body.Joints[JointType.ElbowLeft], body.Joints[JointType.WristLeft]);
            DrawLine(canvas, body.Joints[JointType.ElbowRight], body.Joints[JointType.WristRight]);
            DrawLine(canvas, body.Joints[JointType.WristLeft], body.Joints[JointType.HandLeft]);
            DrawLine(canvas, body.Joints[JointType.WristRight], body.Joints[JointType.HandRight]);
            DrawLine(canvas, body.Joints[JointType.HandLeft], body.Joints[JointType.HandTipLeft]);
            DrawLine(canvas, body.Joints[JointType.HandRight], body.Joints[JointType.HandTipRight]);
            DrawLine(canvas, body.Joints[JointType.HandTipLeft], body.Joints[JointType.ThumbLeft]);
            DrawLine(canvas, body.Joints[JointType.HandTipRight], body.Joints[JointType.ThumbRight]);
            DrawLine(canvas, body.Joints[JointType.SpineMid], body.Joints[JointType.SpineBase]);
            DrawLine(canvas, body.Joints[JointType.SpineBase], body.Joints[JointType.HipLeft]);
            DrawLine(canvas, body.Joints[JointType.SpineBase], body.Joints[JointType.HipRight]);
            DrawLine(canvas, body.Joints[JointType.HipLeft], body.Joints[JointType.KneeLeft]);
            DrawLine(canvas, body.Joints[JointType.HipRight], body.Joints[JointType.KneeRight]);
            DrawLine(canvas, body.Joints[JointType.KneeLeft], body.Joints[JointType.AnkleLeft]);
            DrawLine(canvas, body.Joints[JointType.KneeRight], body.Joints[JointType.AnkleRight]);
            DrawLine(canvas, body.Joints[JointType.AnkleLeft], body.Joints[JointType.FootLeft]);
            DrawLine(canvas, body.Joints[JointType.AnkleRight], body.Joints[JointType.FootRight]);
        }

        public  void DrawId(Canvas canvas, Joint head , ulong id )
        {
            if (head.TrackingState == TrackingState.Tracked)
            {
                Joint joint = ScaleTo(head, canvas.ActualWidth, canvas.ActualHeight);
                

                Canvas.SetLeft(boutonDrawId, joint.Position.X - 2.3 * 40);
                Canvas.SetTop(boutonDrawId, joint.Position.Y - 40 / 2);
                boutonDrawId.Content = " " + (id - KINECT_MINIMAL_ID).ToString() +" ";
                if ( canvas.Children.IndexOf(boutonDrawId)  == -1)
                {                    
                    boutonDrawId.Click += boutonClick;
                    canvas.Children.Add(boutonDrawId);
                }
                
                

                //ID: 
                // 72057594037940157
                // 72057594037940223
                // 72057594037940319
                // 72057594037940333

                // 72057594037928274

            }
        }

        public void boutonClick(object sender, RoutedEventArgs e)
        {
            Button bouton = (Button)sender;
            // Console.WriteLine("ID" + bouton.Content);            
            idSqueletteChoisi = Convert.ToInt64(bouton.Content);
            // mainWindow.etat_TrackingId = idSqueletteChoisi.ToString();
        }

        public void DrawPoint(Canvas canvas, Joint joint)
        {
            if (joint.TrackingState == TrackingState.Tracked)
            {
                joint = ScaleTo(joint, canvas.ActualWidth, canvas.ActualHeight);

                Ellipse ellipse = new Ellipse
                {
                    Width = 20,
                    Height = 20,
                    Fill = new SolidColorBrush(Colors.Blue)
                };

                Canvas.SetLeft(ellipse, joint.Position.X - ellipse.Width / 2);
                Canvas.SetTop(ellipse, joint.Position.Y - ellipse.Height / 2);

                canvas.Children.Add(ellipse);
            }
            if (joint.TrackingState == TrackingState.Inferred)
            {
                joint = ScaleTo(joint , canvas.ActualWidth, canvas.ActualHeight);

                Ellipse ellipse = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = new SolidColorBrush(Colors.Yellow)
                };

                Canvas.SetLeft(ellipse, joint.Position.X - ellipse.Width / 2);
                Canvas.SetTop(ellipse, joint.Position.Y - ellipse.Height / 2);

                canvas.Children.Add(ellipse);
            }
            if (joint.TrackingState == TrackingState.NotTracked)
            {
                joint = ScaleTo(joint , canvas.ActualWidth, canvas.ActualHeight);

                Ellipse ellipse = new Ellipse
                {
                    Width = 5,
                    Height = 5,
                    Fill = new SolidColorBrush(Colors.White)
                };

                Canvas.SetLeft(ellipse, joint.Position.X - ellipse.Width / 2);
                Canvas.SetTop(ellipse, joint.Position.Y - ellipse.Height / 2);

                canvas.Children.Add(ellipse);
            }
        }
        
        public  void Etat_Main(Canvas canvas,HandState handState, Joint positionMain)
        {
            if (handState != HandState.NotTracked)
            {
                positionMain = ScaleTo(positionMain , canvas.ActualWidth, canvas.ActualHeight);
                switch (handState)
                {
                    case HandState.Closed:

                        Ellipse ellipseClosed = new Ellipse
                        {
                            Width = 100,
                            Height = 100,
                            Fill = new SolidColorBrush(Colors.Red),
                            Opacity = 0.1
                        };

                        Canvas.SetLeft(ellipseClosed, positionMain.Position.X - ellipseClosed.Width / 2);
                        Canvas.SetTop(ellipseClosed, positionMain.Position.Y - ellipseClosed.Height / 2);
                        canvas.Children.Add(ellipseClosed);
                        break;
                    case HandState.Open:

                        Ellipse ellipseOpen = new Ellipse
                        {
                            Width = 100,
                            Height = 100,
                            Fill = new SolidColorBrush(Colors.Green),
                            Opacity = 0.1
                        };

                        Canvas.SetLeft(ellipseOpen, positionMain.Position.X - ellipseOpen.Width / 2);
                        Canvas.SetTop(ellipseOpen, positionMain.Position.Y - ellipseOpen.Height / 2);
                        canvas.Children.Add(ellipseOpen);
                        break;
                    case HandState.Lasso:

                        Ellipse ellipseLasso = new Ellipse
                        {
                            Width = 100,
                            Height = 100,
                            Fill = new SolidColorBrush(Colors.Blue),
                            Opacity = 0.1
                        };

                        Canvas.SetLeft(ellipseLasso, positionMain.Position.X - ellipseLasso.Width / 2);
                        Canvas.SetTop(ellipseLasso, positionMain.Position.Y - ellipseLasso.Height / 2);
                        canvas.Children.Add(ellipseLasso);
                        break;
                }
            }
        }

        public  void DrawLine(Canvas canvas, Joint first, Joint second)
        {
            
            if (first.TrackingState == TrackingState.Tracked && second.TrackingState == TrackingState.Tracked)
            {
                first = ScaleTo(first , canvas.ActualWidth, canvas.ActualHeight);
                second = ScaleTo(second , canvas.ActualWidth, canvas.ActualHeight);

                Line line = new Line
                {
                    X1 = first.Position.X,
                    Y1 = first.Position.Y,
                    X2 = second.Position.X,
                    Y2 = second.Position.Y,
                    StrokeThickness = 8,
                    Stroke = new SolidColorBrush(Colors.Green)
                };

                canvas.Children.Add(line);
            }
            else if (first.TrackingState == TrackingState.Inferred || second.TrackingState == TrackingState.Inferred)
            {
                first = ScaleTo(first, canvas.ActualWidth, canvas.ActualHeight);
                second = ScaleTo(second, canvas.ActualWidth, canvas.ActualHeight);

                Line line = new Line
                {
                    X1 = first.Position.X,
                    Y1 = first.Position.Y,
                    X2 = second.Position.X,
                    Y2 = second.Position.Y,
                    StrokeThickness = 4,
                    Stroke = new SolidColorBrush(Colors.Orange)
                };

                canvas.Children.Add(line);
            }
            else if (first.TrackingState == TrackingState.NotTracked || second.TrackingState == TrackingState.NotTracked)
            {
                first = ScaleTo(first , canvas.ActualWidth, canvas.ActualHeight);
                second = ScaleTo(second, canvas.ActualWidth, canvas.ActualHeight);

                Line line = new Line
                {
                    X1 = first.Position.X,
                    Y1 = first.Position.Y,
                    X2 = second.Position.X,
                    Y2 = second.Position.Y,
                    StrokeThickness = 2,
                    Stroke = new SolidColorBrush(Colors.Red)
                };

                canvas.Children.Add(line);
            }
        }



        public  void DrawIdv0(Canvas canvas, Joint head, ulong id)
        {
            if (head.TrackingState == TrackingState.Tracked)
            {
                Joint joint = ScaleTo(head, canvas.ActualWidth, canvas.ActualHeight);
                Ellipse ellipse = new Ellipse
                {
                    Width = 80,
                    Height = 80,
                    Fill = new SolidColorBrush(Colors.White)
                };

                Canvas.SetLeft(ellipse, joint.Position.X - 2.5 * ellipse.Width / 2);
                Canvas.SetTop(ellipse, joint.Position.Y - 1.5 * ellipse.Height / 2);

                TextBlock textBlock = new TextBlock
                {
                    FontSize = 30,
                    Text = (id - KINECT_MINIMAL_ID).ToString()

                };
                Canvas.SetLeft(textBlock, joint.Position.X - 2.3 * ellipse.Width / 2);
                Canvas.SetTop(textBlock, joint.Position.Y - ellipse.Height / 2);


                Button bouton = new Button()
                {
                    FontSize = 30
                };


                Canvas.SetLeft(bouton, joint.Position.X - 2.3 * ellipse.Width / 2);
                Canvas.SetTop(bouton, joint.Position.Y - ellipse.Height / 2);

                bouton.Content = (id - KINECT_MINIMAL_ID).ToString();
                canvas.Children.Add(ellipse);
                canvas.Children.Add(bouton);
                // canvas.Children.Add(textBlock);

                //ID: 
                // 72057594037940157
                // 72057594037940223
                // 72057594037940319
                // 72057594037940333

                // 72057594037928274

            }
        }


    }
}
