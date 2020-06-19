using LSL_Kinect.Classes;
using Microsoft.Kinect;
using LSL_Kinect.Tools;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LSL_Kinect
{
    public class Drawing
    {

        public BodyIdWrapper associatedBodyID = null;

        private Label IdLabel = new Label(){ FontSize = 30, Background = Brushes.White };
        private CoordinateMapper coordinateMapper = null;

        public Drawing(BodyIdWrapper _idWrapper, CoordinateMapper _coordinateMapper)
        {
            associatedBodyID = _idWrapper;
            coordinateMapper = _coordinateMapper;
        }

        public ColorSpacePoint ScaleToCanvas(Joint joint, double canvasWidth, double canvasHeight)
        {
            //For color mode only
            ColorSpacePoint colorPoint = coordinateMapper.MapCameraPointToColorSpace(joint.Position);

            colorPoint.X = Scale(0, canvasWidth,
                Constants.PIXEL_TOTAL_OFFSET_BETWEEN_DEPTH_AND_COLOR,  
                Constants.CROPPED_CAMERA_WIDTH + Constants.PIXEL_TOTAL_OFFSET_BETWEEN_DEPTH_AND_COLOR, 
                colorPoint.X);

            colorPoint.Y = Scale(0,canvasHeight, 
                0, Constants.KINECT_COLOR_CAMERA_HEIGHT, 
                colorPoint.Y);

            return colorPoint;
        }

        public float Scale(double minPixel, double maxPixel, double minAxis, double maxAxis, float position)
        {
            float normalizedPos = (float) Tools.Tools.InverseLerp(minAxis, maxAxis, position);

            float posScaledToCanvas = (float) Tools.Tools.UnclampedLerp(minPixel,maxPixel,normalizedPos);

            if (posScaledToCanvas > maxPixel)
            {
                return (float)maxPixel;
            }

            if (posScaledToCanvas < 0)
            {
                return 0;
            }

            return posScaledToCanvas;
        }

        public void DrawSkeleton(Canvas canvas, Body body)
        {
            foreach (Joint joint in body.Joints.Values)
            {
                DrawPoint(canvas, joint);
            }

            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
            const float InferredZPositionClamp = 0.1f;
            foreach (JointType jointType in joints.Keys)
            {
                CameraSpacePoint position = joints[jointType].Position;
                if (position.Z < 0)
                {
                    position.Z = InferredZPositionClamp;
                }
            }

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
            DrawLine(canvas, body.Joints[JointType.WristLeft], body.Joints[JointType.ThumbLeft]);
            DrawLine(canvas, body.Joints[JointType.WristRight], body.Joints[JointType.ThumbRight]);
            DrawLine(canvas, body.Joints[JointType.SpineMid], body.Joints[JointType.SpineBase]);
            DrawLine(canvas, body.Joints[JointType.SpineBase], body.Joints[JointType.HipLeft]);
            DrawLine(canvas, body.Joints[JointType.SpineBase], body.Joints[JointType.HipRight]);
            DrawLine(canvas, body.Joints[JointType.HipLeft], body.Joints[JointType.KneeLeft]);
            DrawLine(canvas, body.Joints[JointType.HipRight], body.Joints[JointType.KneeRight]);
            DrawLine(canvas, body.Joints[JointType.KneeLeft], body.Joints[JointType.AnkleLeft]);
            DrawLine(canvas, body.Joints[JointType.KneeRight], body.Joints[JointType.AnkleRight]);
            DrawLine(canvas, body.Joints[JointType.AnkleLeft], body.Joints[JointType.FootLeft]);
            DrawLine(canvas, body.Joints[JointType.AnkleRight], body.Joints[JointType.FootRight]);

            DrawId(canvas, body.Joints[JointType.Head], body.TrackingId);
        }

        public void DrawId(Canvas canvas, Joint head, ulong id)
        {
            if (head.TrackingState == TrackingState.Tracked)
            {
                ColorSpacePoint headJoint = ScaleToCanvas(head, canvas.ActualWidth, canvas.ActualHeight);

                Canvas.SetLeft(IdLabel, headJoint.X + (IdLabel.ActualWidth * 0.5));
                Canvas.SetTop(IdLabel, headJoint.Y - (IdLabel.ActualHeight * 0.5));
                IdLabel.Content = associatedBodyID.shortIDString;
                if (canvas.Children.IndexOf(IdLabel) == -1)
                {
                    canvas.Children.Add(IdLabel);
                }
            }
        }

        public void DrawPoint(Canvas canvas, Joint joint)
        {
            ColorSpacePoint jointPos = ScaleToCanvas(joint, canvas.ActualWidth, canvas.ActualHeight);

            int size = 0;
            SolidColorBrush color = null;

            switch (joint.TrackingState)
            {
                case TrackingState.NotTracked:
                    size = 5;
                    color = new SolidColorBrush(Colors.White);
                    break;

                case TrackingState.Inferred:
                    size = 10;
                    color = new SolidColorBrush(Colors.Yellow);
                    break;

                case TrackingState.Tracked:
                    size = 20;
                    color = new SolidColorBrush(Colors.Blue);
                    break;
            }

            Ellipse ellipse = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = color
            };

            Canvas.SetLeft(ellipse, jointPos.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, jointPos.Y - ellipse.Height / 2);

            canvas.Children.Add(ellipse);
        }

        public void DrawLine(Canvas canvas, Joint first, Joint second)
        {
            ColorSpacePoint firstJointPos = ScaleToCanvas(first, canvas.ActualWidth, canvas.ActualHeight);
            ColorSpacePoint secondJointPos = ScaleToCanvas(second, canvas.ActualWidth, canvas.ActualHeight);

            int thickness = 0;
            SolidColorBrush color = null;

            if (first.TrackingState == TrackingState.Tracked && second.TrackingState == TrackingState.Tracked)
            {
                thickness = 8;
                color = new SolidColorBrush(Colors.Green);
            }
            else if (first.TrackingState == TrackingState.Inferred || second.TrackingState == TrackingState.Inferred)
            {
                thickness = 4;
                color = new SolidColorBrush(Colors.Orange);
            }
            else if (first.TrackingState == TrackingState.NotTracked || second.TrackingState == TrackingState.NotTracked)
            {
                thickness = 2;
                color = new SolidColorBrush(Colors.Red);
            }

            Line line = new Line{X1 = firstJointPos.X, Y1 = firstJointPos.Y, X2 = secondJointPos.X, 
                Y2 = secondJointPos.Y, StrokeThickness = thickness, Stroke = color };

            canvas.Children.Add(line);
        }

        /* Unused
        public void DrawIdv0(Canvas canvas, Joint head, ulong id)
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
                    Text = shortID;
                };
                Canvas.SetLeft(textBlock, joint.Position.X - 2.3 * ellipse.Width / 2);
                Canvas.SetTop(textBlock, joint.Position.Y - ellipse.Height / 2);

                Button bouton = new Button()
                {
                    FontSize = 30
                };

                Canvas.SetLeft(bouton, joint.Position.X - 2.3 * ellipse.Width / 2);
                Canvas.SetTop(bouton, joint.Position.Y - ellipse.Height / 2);

                bouton.Content = shortID;
                canvas.Children.Add(ellipse);
                canvas.Children.Add(bouton);
                // canvas.Children.Add(textBlock);
            }
        
        }*/
    }
}