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
        #region Constants

        private const int JOINT_COUNT = 25;
        private const int CHANNELS_PER_JOINT = 4;
        private const int CHANNELS_PER_SKELETON = (JOINT_COUNT * CHANNELS_PER_JOINT) + 1;
        private const int MAX_SKELETON_TRACKED = 1;
        private const int DATA_STREAM_NOMINAL_RATE = 15;

        /* Can be used if we decide to track multiple skeletons at the same time
        private const int CHANNELS_PER_STREAM = MAX_SKELETON_TRACKED * CHANNELS_PER_SKELETON;
        private const int MAX_SKELETON_COUNT = 6;
        */

        #endregion Constants

        #region Private Variables

        //-------------Variables-----------------------
        private KinectSensor currentKinectSensor;

        private MultiSourceFrameReader readerMultiFrame;

        private Body[] bodies = null;
        private List<Drawing> skelettonsDrawing = new List<Drawing>();
        private MainWindowViewModel currentViewModel = new MainWindowViewModel();

        private BodyIdWrapper selectedBodyID = null;
        private Sequence currentSequence = null;

        private int currentFramerate = -1;
        private double localClockStartingPoint = -1;
        private StreamOutlet outletData = null;
        private StreamOutlet outletMarker = null;

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

        public MainWindow()
        {
            DataContext = currentViewModel;
            currentViewModel.AddAllSequences(SequenceList.Deserialize());

            InitializeComponent();

            RegisterKinect();
            InitiateDisplay();

            SetBaseCSVPath();
            CreateDataTables();
        }


        private void InitiateDisplay()
        {
            cameraColorDetectionRect = GetDetectionBoundaries();
            UpdateBroadcastRelatedUI();
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

        private float[] GetSelectedBodyData(Body body)
        {
            float[] data = new float[CHANNELS_PER_SKELETON];
            int channelIndex = 0;

            data[channelIndex] = Convert.ToSingle(Tools.Tools.ConvertDatetimeToUnixTime(DateTime.Now));
            channelIndex++;

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
                    if (bodies == null)
                    {
                        bodies = new Body[bodyFrame.BodyFrameSource.BodyCount];
                    }
                    bodyFrame.GetAndRefreshBodyData(bodies);
                }
            }
        }

        private void ManageFramerate(ColorFrame frame)
        {
            int lastFramerate = currentFramerate;
            currentFramerate = Convert.ToInt32(1.0 / frame.ColorCameraSettings.FrameInterval.TotalSeconds);

            UpdateFpsCounter();

            if (isBroadcasting && currentFramerate != lastFramerate)
            {
                SendMarker(new Marker("The framerate has changed to " + currentFramerate,MarkerType.Message));
            }
        }


        #region Broadcast

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
                            CHANNELS_PER_SKELETON, DATA_STREAM_NOMINAL_RATE, channel_format_t.cf_float32, currentKinectSensor.UniqueKinectId);

            XMLElement channels = mocapStreamMetaData.desc().append_child("channels");

            //Timestamp channel
            AddNewChannel(channels, "Timestamp", "Time", "Unix time");

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
            AddNewChannel(parent, jointValueName, type.ToString(), unit);
        }

        private void AddNewChannel(XMLElement parent, string jointValueName, string type, string unit)
        {
            parent.append_child("channel")
                .append_child_value("label", jointValueName)
                .append_child_value("type", type)
                .append_child_value("unit", unit);
        }

        private void StartBroadcast(Marker marker = null)
        {
            isBroadcasting = true;

            if (marker == null)
            {
                marker = new Marker("Start broadcasting", MarkerType.Start, true);
            }

            if (marker.affectCSV)
            {
                ClearDataTables();
            }

            currentFramerate = DATA_STREAM_NOMINAL_RATE;

            SendMarker(marker);
        }

        private void StopBroadcast(Marker marker = null)
        {
            isBroadcasting = false;

            if (marker == null)
            {
                marker = new Marker("Stop broadcasting", MarkerType.Stop, true);
            }

            SendMarker(marker);

            if (marker.affectCSV)
            {
                DateTime now = DateTime.Now;
                WriteCSVFile(moCapDataTable, now);
                WriteCSVFile(markerDataTable, now);
            }
        }

        private void SendSelectedBodyData(float[] data)
        {
            if (isBroadcasting)
            {
                double timestamp = local_clock() - localClockStartingPoint;
                outletData.push_sample(data, timestamp);
                AddRowToDataTable(moCapDataTable, data);
            }
        }

        private void ManageSequenceStep(Marker marker)
        {
            switch (marker.Type)
            {
                case MarkerType.Start:
                    StartBroadcast(marker);
                    break;
                case MarkerType.Stop:
                    StopBroadcast(marker);
                    break;
                case MarkerType.Message:
                    SendMarker(marker);
                    break;
            }

            UpdateBroadcastRelatedUI();
        }

        private void SendMarker(Marker marker)
        {
            string message = marker.Type.ToString() + " : " + marker.Content;
            markerDescriptionTextBlock.Text = "\"" + message + "\"\n"
                + "At timestamp : " + DateTime.Now.ToString("HH:mm:ss.fff");

            string[] data = new string[] { message };
            AddRowToDataTable(markerDataTable, data);
            outletMarker.push_sample(data, local_clock() - localClockStartingPoint);
        }

        #endregion Broadcast

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

        private void WriteCSVFile(DataTable dataTable, DateTime time)
        {
            string extension = ".csv";
            string date = time.ToString("yyyy.MM.dd hh.mm tt", CultureInfo.InvariantCulture);
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

        private void WriteCSVHeader(CsvWriter csv)
        {
            csv.WriteField("Software : " + Assembly.GetExecutingAssembly().GetName().Name);
            csv.WriteField("Version : " + Assembly.GetExecutingAssembly().GetName().Version);
            csv.WriteField("Stream nominal rate : " + DATA_STREAM_NOMINAL_RATE.ToString());
            csv.WriteField("Sequence Name : " + currentSequence.Name);

            csv.NextRecord();
            csv.NextRecord();
        }

        private void ClearDataTables()
        {
            moCapDataTable.Clear();
            markerDataTable.Clear();
        }

        #endregion CSV

        #region Display

        private void UpdateKinectCaptureRelatedUI()
        {
            Visibility visibility = (isKinectAvailable) ? Visibility.Visible : Visibility.Collapsed;
            bodyTrackingPanel.Visibility = visibility;
            UpdateIndicator(kinectStateIndicator, isKinectAvailable);
        }

        private void UpdateCameraImage(MultiSourceFrame acquiredFrame)
        {
            using (var frame = acquiredFrame.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    ManageFramerate(frame);
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

        private void UpdateFpsCounter()
        {
            fpsCounterLabel.Text = currentFramerate.ToString() + " FPS";
        }

        private void UpdateBroadcastRelatedUI()
        {
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
            canvas.Children.Clear();
            GetBodiesData(acquiredFrame);
            ManageBodiesData();
        }

        private void OnKinectIsAvailableChanged(object kinect, IsAvailableChangedEventArgs args)
        {
            isKinectAvailable = args.IsAvailable;

            if (outletData == null)
            {
                SetLSLStreamInfo();
            }

            UpdateKinectCaptureRelatedUI();
        }

        private void OnIdListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            broadcastPanel.Visibility = Visibility.Visible;

            ComboBox comboBox = (sender as ComboBox);
            selectedBodyID = (BodyIdWrapper)comboBox.SelectedItem;
        }
        private void OnSequenceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            sequenceButton.IsEnabled = true;

            ComboBox comboBox = (sender as ComboBox);
            currentSequence = (Sequence)comboBox.SelectedItem;
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
            if (!isBroadcasting)
            {
                StartBroadcast();
            }
            else
            {
                StopBroadcast();
            }

            UpdateBroadcastRelatedUI();
        }

        private void OnSequenceButtonClicked(object sender, RoutedEventArgs e)
        {
            sequenceButton.Content = (currentSequence.isOnLastStep()) ? "Start Sequence" : "Do next step" ;

            ManageSequenceStep(currentSequence.DoNextStep());
            currentViewModel.ActualizeStep();
        }
        #endregion Button Event

        #region Keyboard event

        private void OnKeyDown(object eventSender, KeyEventArgs keyEventArgs)
        {
            SendMarker(new Marker("Key Pressed : "+ keyEventArgs.Key.ToString(), MarkerType.Message));
            if (keyEventArgs.Key == Key.Space)
            {
                OnBroadcastButtonClicked(null, null);
            }
        }
        #endregion Keyboard event

        #endregion Events

       
    }
}