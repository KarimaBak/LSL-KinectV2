using CsvHelper;
using LSL;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
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

    internal enum NUI_SKELETON_TRACKING_STATE
    {
        NUI_SKELETON_NOT_TRACKED = 0,
        NUI_SKELETON_POSITION_ONLY,
        NUI_SKELETON_TRACKED
    };

    #endregion enum

    public partial class MainWindow : Window
    {
        #region Private Variables

        //-------------Variables-----------------------
        private KinectSensor currentKinectSensor;
        private MultiSourceFrameReader readerMultiFrame;

        private Body[] bodies = null;
        private Dessins dessin = new Dessins();
        private Stopwatch t0 = new Stopwatch();
        private int spaceBarPressCounter = 0;

        private liblsl.StreamOutlet outletData = null;
        private liblsl.StreamOutlet outletMarker = null;

        private DataTable currentDataTable = null;
        private string currentCSVpath = null;

        private CameraMode _mode = CameraMode.Color;
        private bool drawSkeletonOverCamera = true;
        private bool isKinectAvailable = false;
        private bool isRecording = false;
        //TODO remove
        private bool Fermeture_Du_Programme = false;
        private Thread thUpdate_Couleur_Boutton;

        #endregion Private Variables

        #region LSL Constante

        /* PJE: partie LSL */
        private const int NUI_SKELETON_POSITION_COUNT = 25;
        private const int NUM_CHANNELS_PER_JOINT = 4;
        private const int NUM_CHANNELS_PER_SKELETON = (NUI_SKELETON_POSITION_COUNT * NUM_CHANNELS_PER_JOINT) + 2;
        private const int NUI_SKELETON_MAX_TRACKED_COUNT = 1;
        private const int NUM_CHANNELS_PER_STREAM = NUI_SKELETON_MAX_TRACKED_COUNT * NUM_CHANNELS_PER_SKELETON;
        private const int NUI_MAX_SKELETON_COUNT = 6;

        public static readonly IList<String> jointInfoSuffix =
          new ReadOnlyCollection<string>(new List<String> { "_X", "_Y", "_Z", "_Conf" });

        #endregion LSL Constante

        //--------------------MAIN-------------------
        public MainWindow()
        {
            InitializeComponent();

            RegisterKinect();

            UpdateRenderingButtonsState();

            //C'est un peu extrême
            thUpdate_Couleur_Boutton = new Thread(UpdateButtonsColor);
            thUpdate_Couleur_Boutton.Start();
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

        private void SetLSLStreamInfo(String sensorId)
        {
            liblsl.StreamInfo infoMetaData = new liblsl.StreamInfo("Kinect-LSL-MetaData", "Kinect", NUM_CHANNELS_PER_STREAM, 30, liblsl.channel_format_t.cf_float32, sensorId);

            liblsl.XMLElement channels = infoMetaData.desc().append_child("channels");
            for (int skelettonID = 0; skelettonID < NUI_SKELETON_MAX_TRACKED_COUNT; skelettonID++)
            {
                for (int skelettonPosCount = 0; skelettonPosCount < NUI_SKELETON_POSITION_COUNT; skelettonPosCount++)
                {
                    String currentJointName = Enum.GetName(typeof(JointType), skelettonPosCount);

                    channels.append_child("channel")
                        .append_child_value("label", currentJointName + "_X")
                        .append_child_value("type", "PositionX")
                        .append_child_value("unit", "meters");
                    channels.append_child("channel")
                        .append_child_value("label", currentJointName + "_Y")
                        .append_child_value("type", "PositionY")
                        .append_child_value("unit", "meters");
                    channels.append_child("channel")
                        .append_child_value("label", currentJointName + "_Z")
                        .append_child_value("type", "PositionZ")
                        .append_child_value("unit", "meters");
                    channels.append_child("channel")
                        .append_child_value("label", currentJointName + "_Conf")
                        .append_child_value("type", "Confidence")
                        .append_child_value("unit", "normalized");
                }
                channels.append_child("channel")
                    .append_child_value("label", "SkeletonTrackingId" + skelettonID)
                    .append_child_value("type", "TrackingId");
                channels.append_child("channel")
                    .append_child_value("label", "SkeletonQualityFlags" + skelettonID);
            }

            // misc meta-data
            infoMetaData.desc().append_child("acquisition")
                .append_child_value("manufacturer", "Microsoft")
                .append_child_value("model", "Kinect 2.0");

            outletData = new liblsl.StreamOutlet(infoMetaData);

            //Marker data
            liblsl.StreamInfo streamMarker = new liblsl.StreamInfo("Kinect-LSL-Markers", "Kinect", 1, 0, liblsl.channel_format_t.cf_string, sensorId);
            outletMarker = new liblsl.StreamOutlet(streamMarker);
        }

        private void SendLslDataOneBodyTracked(float[] data)
        {
            // PJE
            //Console.WriteLine("outletData.push_sample(data);   traking court: " + dessin.idSqueletteChoisi + " traking : " + body.TrackingId );
            outletData.push_sample(data, liblsl.local_clock());
        }

        private float[] GetDataBody(Body body)
        {
            float[] data = new float[NUM_CHANNELS_PER_STREAM];// data length = 102 alors que (25 jointures) * 4 valeurs (x y z) + body.trackiId + isTracked

            // Comparaison entre le numéro cours choisi par le bouton dans Dessins.cs et le numéro de squelette détecté.
            String shortTrakingIdCalcul = (body.TrackingId - Dessins.KINECT_MINIMAL_ID).ToString();

            if ((dessin.idSqueletteChoisi != -1) && (dessin.idSqueletteChoisi == Convert.ToInt64(shortTrakingIdCalcul)))
            {
                int channelIndex = 0, jointNumber = 0;

                while (jointNumber < Enum.GetValues(typeof(JointType)).Length)
                {
                    channelIndex = BuildLSLData(data, channelIndex, body.Joints[(JointType)jointNumber]);
                    jointNumber++;
                }

                data[channelIndex++] = body.TrackingId;
                //PJE Test avec numéro squelette court ne pas utiliser l'ID court !!!
                // data[i++] = Convert.ToInt64(shortTrakingIdCalcul);
                data[channelIndex++] = (body.IsTracked) ? 1f : -1f;
            }

            return data;
        }

        private static int BuildLSLData(float[] data, int channelIndex, Joint joint)
        {
            CameraSpacePoint jointPosition = joint.Position;
            data[channelIndex++] = jointPosition.X;
            data[channelIndex++] = jointPosition.Y;
            data[channelIndex++] = jointPosition.Z;
            data[channelIndex++] = (float)joint.TrackingState / 2.0f;
            return channelIndex;
        }

        private void SendLslDataAllBodies(String sensorId, Body[] bodies)
        {
            float[] data = new float[NUM_CHANNELS_PER_STREAM];
            int i = 0;

            foreach (var body in bodies)
            {
                if (body.IsTracked)
                {
                    // Comparaison entre le numéro cours choisi par le bouton dans Dessins.cs et le numéro de squelette détecté.
                    String shortTrakingIdCalcul = (body.TrackingId - Dessins.KINECT_MINIMAL_ID).ToString();

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
            // PJE Console.WriteLine("outletData.push_sample(data);   ");
        }

       



        private void ManageMultiSourceFrame(object sender, MultiSourceFrameArrivedEventArgs frameArgs)
        {
            MultiSourceFrame acquiredFrame = frameArgs.FrameReference.AcquireFrame();
            SetCameraImage(acquiredFrame);
            GetBodiesData(acquiredFrame);

            if (bodies != null)
            {
                //PJE LSL
                // SendLslDataAllBodies(kinectSensor.UniqueKinectId , bodies);

                foreach (var body in bodies)
                {
                    if (body.IsTracked)
                    {
                        float[] data = GetDataBody(body);

                        SendLslDataOneBodyTracked(data);

                        //Update ID labels
                        UpdateSkelettonIDText(body);

                        if (isRecording)
                        {
                            AddRowToDataTable(data);
                        }

                        if (drawSkeletonOverCamera)
                        {
                            dessin.DrawSkeleton(canvas, body);
                        }
                    }
                }
            }
        }

        private void GetBodiesData(MultiSourceFrame acquiredFrame)
        {
            // Body
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

        private void SetCameraImage(MultiSourceFrame acquiredFrame)
        {
            //Color
            using (var frame = acquiredFrame.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
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


        #region CSV
        //Create a data table to store body data
        private void CreateDataTable()
        {
            currentDataTable = new DataTable();

            foreach (String jointName in Enum.GetNames(typeof(JointType)))
            {
                foreach (String suffix in jointInfoSuffix)
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
        #endregion


        private void UpdateSkelettonIDText(Body body)
        {
            trakingIdCourt.Text = dessin.idSqueletteChoisi.ToString();
            trakingIdFull.Text = body.TrackingId.ToString();
        }

        private void UpdateRenderingButtonsState()
        {
            if (_mode == CameraMode.Color)
            {
                color_btn.Background = Brushes.LightGreen;
                depth_btn.Background = Brushes.LightBlue;
                infraRed_btn.Background = Brushes.LightBlue;
            }
            else if (_mode == CameraMode.Depth)
            {
                color_btn.Background = Brushes.LightBlue;
                depth_btn.Background = Brushes.LightGreen;
                infraRed_btn.Background = Brushes.LightBlue;
            }
            else if (_mode == CameraMode.Infrared)
            {
                color_btn.Background = Brushes.LightBlue;
                depth_btn.Background = Brushes.LightBlue;
                infraRed_btn.Background = Brushes.LightGreen;
            }
            else
            {
                color_btn.Background = Brushes.Red;
                depth_btn.Background = Brushes.Red;
                infraRed_btn.Background = Brushes.Red;
            }

            if (drawSkeletonOverCamera)
                VisuTracking_btn.Background = Brushes.LightGreen;
            else
                VisuTracking_btn.Background = Brushes.LightBlue;
        }

        private void UpdateKinectState(bool available)
        {
            isKinectAvailable = available;
            UpdateIndicator(kinectStateIndicator, isKinectAvailable);
        }

        private void UpdateRecordState()
        {
            isRecording = !isRecording;
            UpdateIndicator(recordingStateIndicator, isRecording);
            record_btn.Content = (isRecording == true) ? "Stop record" : "Start record";
        }

        private void UpdateIndicator(Ellipse currentIndicator, bool status)
        {
            currentIndicator.Fill = (status) ? Brushes.Green  : Brushes.Red; 
        }

        private void UpdateButtonsColor()
        {
            while (!Fermeture_Du_Programme)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(5));
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateRenderingButtonsState();
                }));
            }
        }

        #region Events
        private void OnKinectIsAvailableChanged(object kinect, IsAvailableChangedEventArgs args)
        {
            if (args.IsAvailable)
            {
                Fermeture_Du_Programme = false;

                //TODO move elsewhere
                SetLSLStreamInfo(currentKinectSensor.UniqueKinectId);
            }

            UpdateKinectState(args.IsAvailable);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
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
            Fermeture_Du_Programme = true;
        }

        #region Button Event

        private void OnRecordButtonClicked(object sender, RoutedEventArgs e)
        {
            UpdateRecordState();
            if (isRecording == true)
            {
                CreateDataTable();
            }
            else
            {
                t0.Reset();
            }

            CSV_btn.IsEnabled = !isRecording;
        }

        private void OnColorModeButtonClicked(object sender, RoutedEventArgs e)
        {
            _mode = CameraMode.Color;
        }

        private void OnDepthModeButtonClicked(object sender, RoutedEventArgs e)
        {
            _mode = CameraMode.Depth;
        }

        private void OnInfraredButtonClicked(object sender, RoutedEventArgs e)
        {
            _mode = CameraMode.Infrared;
        }

        private void OnBodyButtonIDCliked(object sender, RoutedEventArgs e)
        {
            drawSkeletonOverCamera = !drawSkeletonOverCamera;
        }

        private void OnCSVBtnClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                WriteCSVFile(currentDataTable);
                //Ecrire_CSV();
                //Initialise_BkgwrLireServeur();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Erreur dans CSV_button_Click");
            }
        }

        private void OnSendLSLClicked(object sender, RoutedEventArgs e)
        {
            spaceBarPressCounter++;
            String[] dataMarker = new String[] { spaceBarPressCounter.ToString() };
            outletMarker.push_sample(dataMarker, liblsl.local_clock());
            LslNumberSpaceBarPress.Text = "" + (spaceBarPressCounter - 1) + "    at timeStamp: " + DateTime.Now.ToString("hh:mm:ss.fff");
        }

        #endregion Button Event

        #region Keyboard event

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Space)
            {
                OnSendLSLClicked(null, null);
            }
        }

        #endregion Keyboard event

        #endregion
        #region Old code

        /* Unused
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