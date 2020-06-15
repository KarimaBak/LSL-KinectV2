using CsvHelper;
using LSL;
using LSL_Kinect.Classes;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Shapes;
using Brushes = System.Windows.Media.Brushes;
using Path = System.IO.Path;

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
        private DataContextIdViewModel idWrapperList = new DataContextIdViewModel();

        private BodyIdWrapper selectedBodyID = null;

        private double localClockStartingPoint = -1;
        private liblsl.StreamOutlet outletData = null;
        private liblsl.StreamOutlet outletMarker = null;
        private int spaceBarPressCounter = 0;

        private DataTable currentDataTable = null;
        private string currentCSVpath = null;

        private CameraMode _mode = CameraMode.Color;
        private bool drawSkeletonOverCamera = true;
        private bool isKinectAvailable = false;
        private bool isRecording = false;
        private bool isBroadcasting = false;

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
            DataContext = idWrapperList;

            InitializeComponent();

            RegisterKinect();
            InitiateDisplay();

            SetLSLStreamInfo();
        }

        private void InitiateDisplay()
        {
            UpdateRenderingButtonsState();
            UpdateVisualTrackingButtonState();
            UpdateSendLslMarkerButton();
            UpdateExportCSVButton();
        }

        private void RegisterKinect()
        {
            currentKinectSensor = KinectSensor.GetDefault();
            currentKinectSensor.Open();
            currentKinectSensor.IsAvailableChanged += OnKinectIsAvailableChanged;

            readerMultiFrame =
                currentKinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Body
                | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex | FrameSourceTypes.Infrared);
            readerMultiFrame.MultiSourceFrameArrived += ManageMultiSourceFrame;
        }

        private void SetLSLStreamInfo()
        {
            localClockStartingPoint = liblsl.local_clock();
            SetMoCapStreamDefinition();
            SetMarkersStreamDefinition();
        }

        private void SetMarkersStreamDefinition()
        {
            liblsl.StreamInfo streamMarker = new liblsl.StreamInfo("EuroMov-Markers-Kinect", "Markers", 1, 0, liblsl.channel_format_t.cf_string, currentKinectSensor.UniqueKinectId);
            outletMarker = new liblsl.StreamOutlet(streamMarker);
        }

        private void SetMoCapStreamDefinition()
        {
            liblsl.StreamInfo mocapStreamMetaData =
                            new liblsl.StreamInfo("EuroMov-Mocap-Kinect", "MoCap",
                            CHANNELS_PER_SKELETON, 15, liblsl.channel_format_t.cf_float32, currentKinectSensor.UniqueKinectId);

            liblsl.XMLElement channels = mocapStreamMetaData.desc().append_child("channels");

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

            outletData = new liblsl.StreamOutlet(mocapStreamMetaData);
        }

        private void AddNewJointChannel(liblsl.XMLElement parent, string jointValueName, MoCapChannelType type, string unit)
        {
            parent.append_child("channel")
                .append_child_value("label", jointValueName)
                .append_child_value("type", type.ToString())
                .append_child_value("unit", unit);
        }

        private void SendLslDataOneBodyTracked(float[] data)
        {
            outletData.push_sample(data, liblsl.local_clock() - localClockStartingPoint);
        }

        private float[] GetSelectedBodyData(Body body)
        {
            float[] data = new float[CHANNELS_PER_SKELETON];
            int channelIndex = 0, jointNumber = 0;

            if(body != null)
            {
                while (jointNumber < Enum.GetValues(typeof(JointType)).Length)
                {
                    channelIndex = AddOneBodyJointData(data, channelIndex, body.Joints[(JointType)jointNumber]);
                    jointNumber++;
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

        private void ManageMultiSourceFrame(object sender, MultiSourceFrameArrivedEventArgs frameArgs)
        {
            MultiSourceFrame acquiredFrame = frameArgs.FrameReference.AcquireFrame();
            UpdateCameraImage(acquiredFrame);
            GetBodiesData(acquiredFrame);
            ManageBodiesData();
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
            }

            if (isRecording)
            {
                AddRowToDataTable(data);
            }
        }

        private void ManageTrackedBody(Body body)
        {
            Drawing correspondingSkeletton = CheckExistingSkeletons(body.TrackingId);

            if (correspondingSkeletton == null)
            {
                BodyIdWrapper newWrapper = new BodyIdWrapper(body.TrackingId);
                idWrapperList.AddData(newWrapper);

                correspondingSkeletton = new Drawing(newWrapper);
                skelettonsDrawing.Add(correspondingSkeletton);
            }

            if (drawSkeletonOverCamera)
            {
                correspondingSkeletton.DrawSkeleton(canvas, body);
            }
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

        #region Marker

        private void SendMarker(string[] dataMarker)
        {
            outletMarker.push_sample(dataMarker, liblsl.local_clock() - localClockStartingPoint);
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

        //Create a data table to store body data
        private void CreateDataTable()
        {
            currentDataTable = new DataTable();

            foreach (String jointName in Enum.GetNames(typeof(JointType)))
            {
                foreach (String suffix in Enum.GetNames(typeof(JointValueNameSuffix)))
                {
                    currentDataTable.Columns.Add(jointName + suffix, typeof(float));
                }
            }
        }

        private void AddRowToDataTable(float[] data)
        {
            var newRow = currentDataTable.NewRow();

            for (int i = 0; i < currentDataTable.Columns.Count; i++)
            {
                newRow[i] = data[i];
            }

            currentDataTable.Rows.Add(newRow);
        }

        private void WriteCSVFile(DataTable dataTable)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV files(*.csv) | *.csv";
            saveFileDialog.InitialDirectory = currentCSVpath;

            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                currentCSVpath = Path.GetDirectoryName(saveFileDialog.FileName);

                using (var writer = new StreamWriter(saveFileDialog.FileName))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
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
        }

        #endregion CSV

        #region Display

        private void UpdateKinectCaptureRelatedPanels()
        {
            Visibility visibility = (isKinectAvailable) ? Visibility.Visible : Visibility.Collapsed;
            bodyTrackingPanel.Visibility = visibility;
            cameraPanel.Visibility = visibility;
        }

        private void UpdateCameraImage(MultiSourceFrame acquiredFrame)
        {
            //TODO Refactor
            //Color
            using (var frame = acquiredFrame.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    SetFpsCounter(frame);

                    if (_mode == CameraMode.Color)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Infrared
            using (var frame = acquiredFrame.InfraredFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == CameraMode.Infrared)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Depth
            using (var frame = acquiredFrame.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == CameraMode.Depth)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }
        }

        private void SetFpsCounter(ColorFrame frame)
        {
            double fps = 1.0 / frame.ColorCameraSettings.FrameInterval.TotalSeconds;
            fpsCounterLabel.Text = fps.ToString("0.") + " FPS";
        }

        private void UpdateRenderingButtonsState()
        {
            colorButton.Background = Brushes.LightGray;
            depthButton.Background = Brushes.LightGray;
            infraredButton.Background = Brushes.LightGray;

            if (_mode == CameraMode.Color)
            {
                colorButton.Background = Brushes.LightGreen;
            }
            else if (_mode == CameraMode.Depth)
            {
                depthButton.Background = Brushes.LightGreen;
            }
            else if (_mode == CameraMode.Infrared)
            {
                infraredButton.Background = Brushes.LightGreen;
            }
        }

        private void UpdateSendLslMarkerButton()
        {
            SendLslMarkerButton.IsEnabled = isBroadcasting;
        }

        private void UpdateExportCSVButton()
        {
            ExportCSVButton.IsEnabled = !isRecording;
        }

        private void UpdateBroadcastState()
        {
            isBroadcasting = !isBroadcasting;
            if (isBroadcasting)
                SendStartBroadcastMarker();
            else
                SendEndBroadcastMarker();

            UpdateIndicator(broadcastingStateIndicator, isBroadcasting);
            broadcastButton.Content = (isBroadcasting == true) ? "Stop broadcast" : "Start broadcast";
        }

        private void UpdateRecordState()
        {
            isRecording = !isRecording;
            UpdateIndicator(recordingStateIndicator, isRecording);
            recordButton.Content = (isRecording == true) ? "Stop record" : "Start record";
        }

        private void UpdateIndicator(Ellipse currentIndicator, bool status)
        {
            currentIndicator.Fill = (status) ? Brushes.Green : Brushes.Red;
        }

        private void UpdateVisualTrackingButtonState()
        {
            VisuTracking_btn.Background = (drawSkeletonOverCamera) ? Brushes.LightGreen : Brushes.LightBlue;
        }

        #endregion Display

        #region Events
        private void OnWindowLostFocus(object sender, EventArgs e)
        {
            blurEffect.Radius = 15;
        }

        private void OnWindowGetFocus(object sender, EventArgs e)
        {
            blurEffect.Radius = 0;
        }


        private void OnKinectIsAvailableChanged(object kinect, IsAvailableChangedEventArgs args)
        {
            isKinectAvailable = args.IsAvailable;
            UpdateKinectCaptureRelatedPanels();

            UpdateIndicator(kinectStateIndicator, isKinectAvailable);
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

        private void OnIdListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            broadcastPanel.Visibility = Visibility.Visible;
            csvPanel.Visibility = Visibility.Visible;

            System.Windows.Controls.ComboBox comboBox = (sender as System.Windows.Controls.ComboBox);
            selectedBodyID = (BodyIdWrapper)comboBox.SelectedItem;
        }

        #region Button Event

        private void OnBroadcastButtonClicked(object sender, RoutedEventArgs e)
        {
            UpdateBroadcastState();
            UpdateSendLslMarkerButton();
        }

        private void OnRecordButtonClicked(object sender, RoutedEventArgs e)
        {
            UpdateRecordState();
            UpdateExportCSVButton();

            if (isRecording == true)
            {
                CreateDataTable();
            }
        }

        private void OnColorModeButtonClicked(object sender, RoutedEventArgs e)
        {
            _mode = CameraMode.Color;
            UpdateRenderingButtonsState();
        }

        private void OnDepthModeButtonClicked(object sender, RoutedEventArgs e)
        {
            _mode = CameraMode.Depth;
            UpdateRenderingButtonsState();
        }

        private void OnInfraredButtonClicked(object sender, RoutedEventArgs e)
        {
            _mode = CameraMode.Infrared;
            UpdateRenderingButtonsState();
        }

        private void OnVisualTrackingButtonClicked(object sender, RoutedEventArgs e)
        {
            drawSkeletonOverCamera = !drawSkeletonOverCamera;
            UpdateVisualTrackingButtonState();
        }

        private void OnCSVBtnClicked(object sender, RoutedEventArgs e)
        {
            if (currentDataTable != null)
            {
                WriteCSVFile(currentDataTable);
            }
        }

        private void OnSendLSLClicked(object sender, RoutedEventArgs e)
        {
            spaceBarPressCounter++;
            String[] dataMarker = new String[] { spaceBarPressCounter.ToString(), spaceBarPressCounter.ToString() };
            SendMarker(dataMarker);
            LslNumberSpaceBarPress.Text = (spaceBarPressCounter - 1) + " at timeStamp: " + DateTime.Now.ToString("hh:mm:ss.fff");
        }

        #endregion Button Event

        #region Keyboard event

        private void OnKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Space)
            {
                OnSendLSLClicked(null, null);
            }
        }

        #endregion Keyboard event

        #endregion Events

        #region Old code

        /* Unused
        /*
        private void SendLslDataAllBodies(String sensorId, Body[] bodies)
        {
            float[] data = new float[NUM_CHANNELS_PER_STREAM];
            int i = 0;

            foreach (var body in bodies)
            {
                if (body.IsTracked)
                {
                    // Comparaison entre le numéro cours choisi par le bouton dans Dessins.cs et le numéro de squelette détecté.
                    String shortTrakingIdCalcul = (body.TrackingId - Drawing.KINECT_MINIMAL_ID).ToString();

                    if ((dessin.idSqueletteChoisi != -1) && (dessin.idSqueletteChoisi.ToString() == shortTrakingIdCalcul))
                    {
                        foreach (Joint joint in body.Joints.Values)
                        {
                            CameraSpacePoint jointPosition = joint.Position;
                            data[i++] = jointPosition.X;
                            data[i++] = jointPosition.Y;
                            data[i++] = jointPosition.Z;
                            data[i++] = (float)joint.TrackingState / 2.0f; // 0.0, 0.5, or 1.0
                        }
                        data[i++] = body.TrackingId;
                        if (body.IsTracked)
                        {
                            data[i++] = 1f;
                        }
                        else
                        {
                            data[i++] = -1f;
                        }
                        i++;
                    }
                }
            }
            outletData.push_sample(data, liblsl.local_clock());
        }

        private int Reduction_Id_Body(Body[] _bodies, Body _body)
        {
            int id = 999;
            for (int i = 0; i < 6; i++)
            {
                if (_bodies[i].TrackingId == _body.TrackingId)
                    id = i;
            }
            return id;
        }

      private void Ecrire_CSV(DataTable _dataTable, string _nomFichier)
      {
          NumberFormatInfo nfi = new NumberFormatInfo();
          nfi.NumberDecimalSeparator = ".";
          using (var writer = new StreamWriter(currentCSVpath))
          {
              writer.AutoFlush = true;
              writer.WriteLine(_nomFichier);
              writer.Write("TimeStamp(s)");
              for (int i = 0; i < 25; i++)
                  writer.Write(";X." + ((JointType)i).ToString() + "(m);" + "Y." + ((JointType)i).ToString() + "(m);" + "Z." + ((JointType)i).ToString()
                      + "(m);" + "TrkState." + ((JointType)i).ToString());

              writer.WriteLine(";HandLeftState;" +
                  "HandLeftConfidence;HandRightState;" +
                  "HandRightConfidence");

              foreach (DataRow row in _dataTable.Rows)
              {
                  string[] tempX = row.ReworkRow("X_");
                  string[] tempY = row.ReworkRow("Y_");
                  string[] tempZ = row.ReworkRow("Z_");
                  string ts = row.Coma_To_Dot("TimeStamp");

                  writer.Write(ts + ";");
                  for (int i = 0; i < 25; i++)
                      writer.Write(tempX[i] + ";" +
                          tempY[i] + ";" + tempZ[i] + ";"
                          + row["TrackingState_" + ((JointType)i).ToString()] + ";");

                  writer.WriteLine(row["HandLeftState"] + ";" + row["HandLeftConfidence"] +
                      ";" + row["HandRightState"] + ";" + row["HandRightConfidence"]);
              }
          }
      }

      private double Median_TimeStamp(List<Squelette> _liste)
      {
          long[] t0Tab = new long[25];
          double t0Median = 0;

          for (int i = 0; i < 25; i++)
          {
              if (_liste[i] != null)
              {
                  t0Tab[i] = _liste[i].Timestamp;
              }
          }

          Array.Sort(t0Tab);
          t0Median = ((t0Tab[12] + t0Tab[13]) / 2) * 0.001;

          return t0Median;
      }*/

        /* Unused
        public static string AbregeTrackingState(TrackingState trk)
        {
            string abrege = null;
            switch (trk)
            {
                case TrackingState.Tracked:
                    abrege = "T";
                    break;

                case TrackingState.Inferred:
                    abrege = "I";
                    break;

                case TrackingState.NotTracked:
                    abrege = "NT";
                    break;

                default:
                    abrege = "UNK";
                    break;
            }
            return abrege;
        }

        public static string AbregeHandConfidence(TrackingConfidence trkconf)
        {
            string abrege = null;
            switch (trkconf)
            {
                case TrackingConfidence.Low:
                    abrege = "L";
                    break;

                case TrackingConfidence.High:
                    abrege = "H";
                    break;

                default:
                    abrege = "UNK";
                    break;
            }
            return abrege;
        }

        public static string AbregeHandState(HandState state)
        {
            string abrege = null;
            switch (state)
            {
                case HandState.Closed:
                    abrege = "C";
                    break;

                case HandState.Lasso:
                    abrege = "L";
                    break;

                case HandState.NotTracked:
                    abrege = "NT";
                    break;

                case HandState.Open:
                    abrege = "O";
                    break;

                case HandState.Unknown:
                    abrege = "UNK";
                    break;

                default:
                    abrege = "UNK";
                    break;
            }
            return abrege;
        }*/

        //Unused
        //private string Formatage_DateHeure()
        //{
        //    string dateHeureFormatee = null;
        //    string dateHeureNonFormatee = null;
        //    dateHeureNonFormatee = DateTime.Now.ToString();
        //    char[] dateHeureNonFormateeTableau = null;
        //    dateHeureNonFormateeTableau = dateHeureNonFormatee.ToCharArray();
        //    for (int i = 0; i < dateHeureNonFormatee.Length; i++)
        //    {
        //        if (dateHeureNonFormateeTableau[i] == ':' ||
        //            dateHeureNonFormateeTableau[i] == ' ' ||
        //            dateHeureNonFormateeTableau[i] == '\\' ||
        //            dateHeureNonFormateeTableau[i] == '/')
        //        {
        //            dateHeureNonFormateeTableau[i] = '_';
        //        }
        //    }
        //    dateHeureFormatee = new string(dateHeureNonFormateeTableau);

        //    return dateHeureFormatee;
        //}

        #endregion Old code

        
    }
}