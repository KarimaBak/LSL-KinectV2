using CsvHelper;
using LSL_Kinect.Classes;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static LSL.liblsl;
using Brushes = System.Windows.Media.Brushes;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace LSL_Kinect
{
    #region enum

    internal enum CameraMode
    {
        Color,
        Depth,
        Infrared
    }

    internal enum MoCapChannelType
    {
        PositionX,
        PositionY,
        PositionZ,
        Confidence
    }

    internal enum JointValueNameSuffix
    {
        _X,
        _Y,
        _Z,
        _Conf
    }

    #endregion enum

    public partial class MainWindow : Window
    {
        #region Private Variables

        //-------------Variables-----------------------
        private KinectSensor currentKinectSensor;

        private MultiSourceFrameReader readerMultiFrame;

        private Body[] bodies = null;
        private List<Drawing> skelettonsDrawing = new List<Drawing>();
        private MainWindowViewModel currentViewModel = new MainWindowViewModel();

        private BodyIdWrapper selectedBodyID = null;

        private double localClockStartingPoint = -1;
        private StreamOutlet outletData = null;
        private StreamOutlet outletMarker = null;
        private int customMarkerCount = 0;

        private StreamInlet instructionMarkerStream = null;

        private DataTable moCapDataTable = null;
        private DataTable markerDataTable = null;
        private string currentCSVpath = null;

        private bool isKinectAvailable = false;
        private bool isBroadcasting = false;

        private Int32Rect cameraColorDetectionRect = Int32Rect.Empty;

        public delegate void InstructionStreamFoundHandler();

        public event InstructionStreamFoundHandler InstructionStreamFound;

        private static readonly Regex doRecordRegex = new Regex("DoRecord");
        private static readonly Regex doPauseRegex = new Regex("DoPause");
        private static readonly Regex closeRegex = new Regex("WINDOW_CLOSING");

        #endregion Private Variables

        #region Constants

        private const int JOINT_COUNT = 25;
        private const int CHANNELS_PER_JOINT = 4;
        private const int CHANNELS_PER_SKELETON = (JOINT_COUNT * CHANNELS_PER_JOINT);
        private const int MAX_SKELETON_TRACKED = 1;

        /* Can be used if we decide to track multiple skeletons at the same time
        private const int CHANNELS_PER_STREAM = MAX_SKELETON_TRACKED * CHANNELS_PER_SKELETON;
        private const int MAX_SKELETON_COUNT = 6;
        */

        #endregion Constants

        public MainWindow()
        {
            DataContext = currentViewModel;
            InitializeComponent();

            RegisterKinect();
            InitiateDisplay();

            SetBaseCSVPath();
            SetLSLStreamInfo();

            GetLslInstructionMarkerStream();
        }

        private void InitiateDisplay()
        {
            cameraColorDetectionRect = GetDetectionBoundaries();
            UpdateBroadcastRelatedButtons();
        }

        private void RegisterKinect()
        {
            currentKinectSensor = KinectSensor.GetDefault();
            currentKinectSensor.Open();
            currentKinectSensor.IsAvailableChanged += OnKinectIsAvailableChanged;

            readerMultiFrame =
                currentKinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Body);
            readerMultiFrame.MultiSourceFrameArrived += OnKinectFrameArrived;
        }

        private void SetLSLStreamInfo()
        {
            localClockStartingPoint = local_clock();
            SetMoCapStreamDefinition();
            SetMarkersStreamDefinition();
        }

        private void SetMarkersStreamDefinition()
        {
            StreamInfo streamMarker = new StreamInfo("EuroMov-Markers-Kinect", "Markers", 1, 0, channel_format_t.cf_string, currentKinectSensor.UniqueKinectId);
            outletMarker = new StreamOutlet(streamMarker);
        }

        private void SetMoCapStreamDefinition()
        {
            StreamInfo mocapStreamMetaData =
                            new StreamInfo("EuroMov-Mocap-Kinect", "MoCap",
                            CHANNELS_PER_SKELETON, 15, channel_format_t.cf_float32, currentKinectSensor.UniqueKinectId);

            XMLElement channels = mocapStreamMetaData.desc().append_child("channels");

            for (int skeletonNumber = 0; skeletonNumber < MAX_SKELETON_TRACKED; skeletonNumber++)
            {
                for (int skeletonJointNumber = 0; skeletonJointNumber < JOINT_COUNT; skeletonJointNumber++)
                {
                    String currentJointName = Enum.GetName(typeof(JointType), skeletonJointNumber);

                    for (int i = 0; i < 3; i++)
                    {
                        AddNewJointChannel(channels, currentJointName + (JointValueNameSuffix)i, (MoCapChannelType)i, "meters");
                    }
                    AddNewJointChannel(channels, currentJointName + JointValueNameSuffix._Conf, MoCapChannelType.Confidence, "normalized");
                }
            }

            // misc meta-data
            mocapStreamMetaData.desc().append_child("acquisition")
                .append_child_value("manufacturer", "Microsoft")
                .append_child_value("model", "Kinect 2.0");

            outletData = new StreamOutlet(mocapStreamMetaData);
        }

        private void AddNewJointChannel(XMLElement parent, string jointValueName, MoCapChannelType type, string unit)
        {
            parent.append_child("channel")
                .append_child_value("label", jointValueName)
                .append_child_value("type", type.ToString())
                .append_child_value("unit", unit);
        }

        private void SendLslDataOneBodyTracked(float[] data)
        {
            outletData.push_sample(data, local_clock() - localClockStartingPoint);
        }

        private float[] GetSelectedBodyData(Body body)
        {
            float[] data = new float[CHANNELS_PER_SKELETON];
            int channelIndex = 0;

            if (body != null)
            {
                foreach (Joint joint in body.Joints.Values)
                {
                    channelIndex = AddOneBodyJointData(data, channelIndex, joint);
                }
            }

            return data;
        }

        private static int AddOneBodyJointData(float[] data, int channelIndex, Joint joint)
        {
            CameraSpacePoint jointPosition = joint.Position;
            data[channelIndex++] = jointPosition.X;
            data[channelIndex++] = jointPosition.Y;
            data[channelIndex++] = jointPosition.Z;
            data[channelIndex++] = (float)joint.TrackingState / 2.0f;
            return channelIndex;
        }

        private Drawing CheckExistingSkeletons(ulong id)
        {
            foreach (Drawing skeleton in skelettonsDrawing)
            {
                if (skeleton.associatedBodyID.kinectID == id)
                {
                    return skeleton;
                }
            }
            return null;
        }

        private void ManageBodiesData()
        {
            float[] data = new float[CHANNELS_PER_SKELETON];

            if (bodies != null)
            {
                foreach (var body in bodies)
                {
                    if (body.IsTracked)
                    {
                        ManageTrackedBody(body);
                    }

                    if (selectedBodyID != null && body.TrackingId == selectedBodyID.kinectID)
                    {
                        data = GetSelectedBodyData(body);
                    }
                }
            }
            SendSelectedBodyData(data);
        }

        private void SendSelectedBodyData(float[] data)
        {
            if (isBroadcasting)
            {
                SendLslDataOneBodyTracked(data);
                AddRowToDataTable(moCapDataTable, data);
            }
        }

        private void ManageTrackedBody(Body body)
        {
            Drawing correspondingSkeletton = CheckExistingSkeletons(body.TrackingId);

            if (correspondingSkeletton == null)
            {
                BodyIdWrapper newWrapper = new BodyIdWrapper(body.TrackingId);
                currentViewModel.AddBodyID(newWrapper);

                correspondingSkeletton = new Drawing(newWrapper, currentKinectSensor.CoordinateMapper);
                skelettonsDrawing.Add(correspondingSkeletton);
            }

            correspondingSkeletton.DrawSkeleton(canvas, body);
        }

        private void GetBodiesData(MultiSourceFrame acquiredFrame)
        {
            using (BodyFrame bodyFrame = acquiredFrame.BodyFrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    canvas.Children.Clear();

                    if (bodies == null)
                    {
                        bodies = new Body[bodyFrame.BodyFrameSource.BodyCount];
                    }
                    bodyFrame.GetAndRefreshBodyData(bodies);
                }
            }
        }

        #region Remote Control

        private void GetLslInstructionMarkerStream()
        {
            Thread findStreamThread = new Thread(GetInstructionMarkerStream)
            {
                Name = "GetInstructionMarkerStream",
                IsBackground = true
            };

            InstructionStreamFound += OnInstructionStreamFound;
            findStreamThread.Start();
        }

        private void StartReadingInstructionsMarkers()
        {
            Thread getMessageThread = new Thread(ReadInstructionMarker)
            {
                Name = "ReadInstructionMarker",
                IsBackground = true
            };

            getMessageThread.Start();
        }

        private void GetInstructionMarkerStream()
        {
            Console.WriteLine("Looking for instruction stream...");

            string predicate = "starts-with(name,'Mouse') and type='Markers' and source_id='MouseCircularMarkers'";
            //By default, resolve_stream() run forever until it find somethign
            StreamInfo[] results = resolve_stream(predicate);
            instructionMarkerStream = new StreamInlet(results[0]);
            InstructionStreamFound();

            Console.WriteLine("Instruction stream found !");
        }

        private void ReadInstructionMarker()
        {
            Console.WriteLine("Reading instructions...");

            while (instructionMarkerStream != null)
            {
                string[] message = new string[1];
                //By default, pull_sample() run forever until it find something
                instructionMarkerStream.pull_sample(message);

                if (doRecordRegex.IsMatch(message[0]))
                {
                    Console.WriteLine("On start record");
                }
                if (doPauseRegex.IsMatch(message[0]))
                {
                    Console.WriteLine("On pause record");
                }
                if (closeRegex.IsMatch(message[0]))
                {
                    Console.WriteLine("On windows close");
                    instructionMarkerStream = null;
                }
            }

            Console.WriteLine("Stop Reading instruction.");
        }

        #endregion Remote Control

        #region Marker

        private void SendMarker(string[] dataMarker)
        {
            markerDescriptionTextBlock.Text = "\"" + dataMarker[0] + "\" at timeStamp: " + DateTime.Now.ToString("hh:mm:ss.fff");
            AddRowToDataTable(markerDataTable, dataMarker);
            outletMarker.push_sample(dataMarker, local_clock() - localClockStartingPoint);
        }

        private void SendStartBroadcastMarker()
        {
            SendMarker(new string[] { "Start broadcasting" });
        }

        private void SendEndBroadcastMarker()
        {
            SendMarker(new string[] { "Stop broadcasting" });
        }

        #endregion Marker

        #region CSV

        private void SetBaseCSVPath()
        {
            currentCSVpath = Directory.GetCurrentDirectory() + "\\";
            currentViewModel.CsvPath = currentCSVpath;
        }

        //Create a data table to store stream data
        private void CreateDataTables()
        {
            moCapDataTable = new DataTable("Kinect_Capture_MoCap_Data");

            moCapDataTable.Columns.Add("TimeSpan", typeof(string));

            foreach (String jointName in Enum.GetNames(typeof(JointType)))
            {
                foreach (String suffix in Enum.GetNames(typeof(JointValueNameSuffix)))
                {
                    moCapDataTable.Columns.Add(jointName + suffix, typeof(float));
                }
            }

            markerDataTable = new DataTable("Kinect_Capture_Markers_Data");

            markerDataTable.Columns.Add("TimeSpan", typeof(string));
            markerDataTable.Columns.Add("Marker Message", typeof(string));
        }

        private void AddRowToDataTable<T>(DataTable table, params T[] data)
        {
            var newRow = table.NewRow();
            newRow[0] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

            for (int i = 1; i < table.Columns.Count; i++)
            {
                newRow[i] = data[i - 1];
            }

            table.Rows.Add(newRow);
        }

        private void WriteCSVFile(DataTable dataTable)
        {
            string extension = ".csv";
            string date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string FileName = dataTable.TableName + " " + date;

            using (var writer = new StreamWriter(currentCSVpath + FileName + extension))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                WriteCSVHeader(csv);

                foreach (DataColumn column in dataTable.Columns)
                {
                    csv.WriteField(column.ColumnName);
                }
                csv.NextRecord();

                foreach (DataRow row in dataTable.Rows)
                {
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        csv.WriteField(row[column]);
                    }
                    csv.NextRecord();
                }

                writer.Dispose();
            }
        }

        private static void WriteCSVHeader(CsvWriter csv)
        {
            csv.WriteField("Software : " + Assembly.GetExecutingAssembly().GetName().Name);
            csv.WriteField("Version :" + Assembly.GetExecutingAssembly().GetName().Version);

            csv.NextRecord();
            csv.NextRecord();
        }

        #endregion CSV

        #region Display

        private void UpdateKinectCaptureRelatedPanels()
        {
            Visibility visibility = (isKinectAvailable) ? Visibility.Visible : Visibility.Collapsed;
            bodyTrackingPanel.Visibility = visibility;
        }

        private void UpdateCameraImage(MultiSourceFrame acquiredFrame)
        {
            using (var frame = acquiredFrame.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    SetFpsCounter(frame);
                    BitmapSource bitmapSource = frame.ToBitmap();
                    CroppedBitmap croppedBitmap = new CroppedBitmap(bitmapSource, cameraColorDetectionRect);
                    camera.Source = croppedBitmap;
                }
            }
        }

        private Int32Rect GetDetectionBoundaries()
        {
            //We know that the depth Camera has a greater height than the color camera so it's safe to put the max value
            Int32Rect depthSpaceInColorFormat =
                new Int32Rect(Constants.PIXEL_TOTAL_OFFSET_BETWEEN_DEPTH_AND_COLOR, 0, Constants.CROPPED_CAMERA_WIDTH, 1080);

            return depthSpaceInColorFormat;
        }

        private void SetFpsCounter(ColorFrame frame)
        {
            double fps = 1.0 / frame.ColorCameraSettings.FrameInterval.TotalSeconds;
            fpsCounterLabel.Text = fps.ToString("0.") + " FPS";
        }

        private void UpdateBroadcastRelatedButtons()
        {
            SendLslMarkerButton.IsEnabled = isBroadcasting;
        }

        private void UpdateBroadcastState()
        {
            isBroadcasting = !isBroadcasting;

            UpdateIndicator(broadcastingStateIndicator, isBroadcasting);
            broadcastButton.Content = (isBroadcasting == true) ? "Stop broadcast" : "Start broadcast";
        }

        private void UpdateIndicator(Ellipse currentIndicator, bool status)
        {
            currentIndicator.Fill = (status) ? Brushes.Green : Brushes.Red;
        }

        #endregion Display

        #region Events

        private void OnKinectFrameArrived(object sender, MultiSourceFrameArrivedEventArgs frameArgs)
        {
            MultiSourceFrame acquiredFrame = frameArgs.FrameReference.AcquireFrame();
            UpdateCameraImage(acquiredFrame);
            GetBodiesData(acquiredFrame);
            ManageBodiesData();
        }

        private void OnInstructionStreamFound()
        {
            StartReadingInstructionsMarkers();
        }

        private void OnKinectIsAvailableChanged(object kinect, IsAvailableChangedEventArgs args)
        {
            isKinectAvailable = args.IsAvailable;
            UpdateKinectCaptureRelatedPanels();

            UpdateIndicator(kinectStateIndicator, isKinectAvailable);
        }

        private void OnIdListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            broadcastPanel.Visibility = Visibility.Visible;

            ComboBox comboBox = (sender as ComboBox);
            selectedBodyID = (BodyIdWrapper)comboBox.SelectedItem;
        }

        #region Windows Events

        private void OnWindowLostFocus(object sender, EventArgs e)
        {
            //Add here function to alert the windows lost focus.
            //But remember the camera rendering needs to stay visible, even if the window is not the main focus
            //blurEffect.Radius = 15;
        }

        private void OnWindowGetFocus(object sender, EventArgs e)
        {
            //blurEffect.Radius = 0;
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (this.readerMultiFrame != null)
            {
                this.readerMultiFrame.Dispose();
                this.readerMultiFrame = null;
            }

            if (this.currentKinectSensor != null)
            {
                this.currentKinectSensor.Close();
                this.currentKinectSensor = null;
            }
        }

        #endregion Windows Events

        #region Button Event

        private void OnBroadcastButtonClicked(object sender, RoutedEventArgs e)
        {
            UpdateBroadcastState();
            UpdateBroadcastRelatedButtons();

            if (isBroadcasting)
            {
                CreateDataTables();
                SendStartBroadcastMarker();
            }
            else
            {
                SendEndBroadcastMarker();
                WriteCSVFile(moCapDataTable);
                WriteCSVFile(markerDataTable);
            }
        }

        private void OnSendMarkerKeyPressed(object sender, RoutedEventArgs e)
        {
            customMarkerCount++;
            String[] dataMarker = new String[] { "Custom marker : " + customMarkerCount.ToString() };
            SendMarker(dataMarker);
        }

        #endregion Button Event

        #region Keyboard event

        //Try to replace with command in view model instead
        private void OnKeyDown(object eventSender, KeyEventArgs keyEventArgs)
        {
            if (isBroadcasting)
            {
                if (keyEventArgs.KeyboardDevice.Modifiers == ModifierKeys.Control)
                {
                    switch (keyEventArgs.Key)
                    {
                        case Key.D1:
                            SendMarker(new string[] { Properties.Resources.Marker1 });
                            break;

                        case Key.D2:
                            SendMarker(new string[] { Properties.Resources.Marker2 });
                            break;

                        case Key.D3:
                            SendMarker(new string[] { Properties.Resources.Marker3 });
                            break;

                        case Key.D4:
                            SendMarker(new string[] { Properties.Resources.Marker4 });
                            break;

                        case Key.D5:
                            SendMarker(new string[] { Properties.Resources.Marker5 });
                            break;

                        case Key.D6:
                            SendMarker(new string[] { Properties.Resources.Marker6 });
                            break;

                        case Key.D7:
                            SendMarker(new string[] { Properties.Resources.Marker7 });
                            break;

                        case Key.D8:
                            SendMarker(new string[] { Properties.Resources.Marker8 });
                            break;

                        case Key.D9:
                            SendMarker(new string[] { Properties.Resources.Marker9 });
                            break;
                    }
                }
                else if (keyEventArgs.Key == Key.M)
                {
                    OnSendMarkerKeyPressed(null, null);
                }
            }
            else if (keyEventArgs.Key == Key.Space)
            {
                OnBroadcastButtonClicked(null, null);
            }
        }

        #endregion Keyboard event

        #endregion Events
    }
}