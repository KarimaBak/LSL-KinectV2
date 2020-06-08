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
        private MultiSourceFrameReader readerMultiFrame;

        private KinectSensor kinectSensor;
        private Body[] bodies = null;
        private CameraMode _mode = CameraMode.Color;
        private bool Fermeture_Du_Programme = false;
        private Thread thUpdate_Rec_State;
        private Thread thUpdate_Kinect_State;
        private Thread thUpdate_Path_State;
        private Thread thUpdate_Couleur_Boutton;
        private CoordinateMapper coordinateMapper = null;
        private int displayWidth;
        private int displayHeight;
        private List<Squelette> ListeDesSquelettes = new List<Squelette>();
        private Stopwatch t0 = new Stopwatch();
        private bool isRecording = false;
        private string currentCSVpath = null;

        // PJE
        private bool drawSkeletonOverCamera = true;

        private int spaceBarPressCounter = 0;

        private Dessins dessin = new Dessins();

        private liblsl.StreamOutlet outletData = null;
        private liblsl.StreamOutlet outletMarker = null;

        private DataTable currentDataTable = null;
        private String currentCSVname = null;

        #endregion Private Variables

        #region Properties

        public string etat_Record
        {
            get { return (string)GetValue(etatRecordProperty); }
            set { SetValue(etatRecordProperty, value); }
        }

        public string etat_Kinect
        {
            get { return (string)GetValue(etatKinectProperty); }
            set { SetValue(etatKinectProperty, value); }
        }

        public string etat_Path
        {
            get { return (string)GetValue(etatPathProperty); }
            set { SetValue(etatPathProperty, value); }
        }

        public string selectionCombo { get; set; }

        #endregion Properties

        #region LSL Constante

        /* PJE: partie LSL */
        private const int NUI_SKELETON_POSITION_COUNT = 25;
        private const int NUM_CHANNELS_PER_JOINT = 4;
        private const int NUM_CHANNELS_PER_SKELETON = (NUI_SKELETON_POSITION_COUNT * NUM_CHANNELS_PER_JOINT) + 2;
        private const int NUI_SKELETON_MAX_TRACKED_COUNT = 1;
        private const int NUM_CHANNELS_PER_STREAM = NUI_SKELETON_MAX_TRACKED_COUNT * NUM_CHANNELS_PER_SKELETON;
        private const int NUI_SKELETON_COUNT = 6;
        private String[] joint_names = { "HipCenter", "Spine", "ShoulderCenter", "Head", "ShoulderLeft", "ElbowLeft", "WristLeft", "HandLeft", "ShoulderRight", "ElbowRight", "WristRight", "HandRight", "HipLeft", "KneeLeft", "AnkleLeft", "FootLeft", "HipRight", "KneeRight", "AnkleRight", "FootRight" };

        public static readonly IList<String> jointInfoSuffix =
          new ReadOnlyCollection<string>(new List<String> { "_X", "_Y", "_Z", "_Conf" });

        #endregion LSL Constante

        //--------------------MAIN-------------------
        public MainWindow()
        {
            InitializeComponent();
            Initialise_Kinect();
            Thread.Sleep(500);
            Affiche_Etat_Kinect();
            Affiche_Etat_Record();
            Affiche_Etat_Path();
            Affiche_Etat_Boutton();
            thUpdate_Kinect_State = new Thread(Update_Kinect_State);
            thUpdate_Kinect_State.SetApartmentState(ApartmentState.STA);
            thUpdate_Kinect_State.Start();

            //C'est un peu extrême
            thUpdate_Path_State = new Thread(Update_Path_State);
            thUpdate_Path_State.SetApartmentState(ApartmentState.STA);
            thUpdate_Path_State.Start();
            thUpdate_Couleur_Boutton = new Thread(Update_Couleur_Boutton);
            thUpdate_Couleur_Boutton.Start();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (readerMultiFrame != null)
            {
                if (outletData == null || outletMarker == null)
                {
                    SetLSLStreamInfo(kinectSensor.UniqueKinectId);
                }
                readerMultiFrame.MultiSourceFrameArrived += ManageMultiSourceFrame;
            }
        }

        //Create a data table to store data
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
                //currentCSVpath = saveFileDialog.FileName;
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

        // PJE Ce code est supposé marché mais remplacé par la fonction de test ci-dessous
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

        // --------------------METHODES ET FONCTIONS-------------------

        public static readonly DependencyProperty etatKinectProperty =
            DependencyProperty.Register("etat_Kinect", typeof(string),
                typeof(MainWindow), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty etatRecordProperty =
           DependencyProperty.Register("etat_Record", typeof(string),
               typeof(MainWindow), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty etatPathProperty =
           DependencyProperty.Register("etat_Path", typeof(string),
               typeof(MainWindow), new PropertyMetadata(string.Empty));

        private void Affiche_Etat_Record()
        {
            if (isRecording == true)
            {
                this.etat_Record = "Record in Progress...";
            }
            else
            {
                this.etat_Record = " Waiting for a record ";
            }
        }

        private void Affiche_Etat_Path()
        {
            if (currentCSVpath != null)
            {
                this.etat_Path = currentCSVpath;
            }
            else
            {
                this.etat_Path = " Choisissez un chemin d'enregistrement... ";
            }
        }

        private void Affiche_Etat_Boutton()
        {
            if (_mode == CameraMode.Color)
            {
                color_btn.Background = System.Windows.Media.Brushes.LightGreen;
                depth_btn.Background = System.Windows.Media.Brushes.LightBlue;
                infraRed_btn.Background = System.Windows.Media.Brushes.LightBlue;
            }
            else if (_mode == CameraMode.Depth)
            {
                color_btn.Background = System.Windows.Media.Brushes.LightBlue;
                depth_btn.Background = System.Windows.Media.Brushes.LightGreen;
                infraRed_btn.Background = System.Windows.Media.Brushes.LightBlue;
            }
            else if (_mode == CameraMode.Infrared)
            {
                color_btn.Background = System.Windows.Media.Brushes.LightBlue;
                depth_btn.Background = System.Windows.Media.Brushes.LightBlue;
                infraRed_btn.Background = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                color_btn.Background = System.Windows.Media.Brushes.Red;
                depth_btn.Background = System.Windows.Media.Brushes.Red;
                infraRed_btn.Background = System.Windows.Media.Brushes.Red;
            }

            if (drawSkeletonOverCamera)
                VisuTracking_btn.Background = System.Windows.Media.Brushes.LightGreen;
            else
                VisuTracking_btn.Background = System.Windows.Media.Brushes.LightBlue;
        }

        private void Affiche_Etat_Kinect()
        {
            if (kinectSensor != null)
            {
                if (this.kinectSensor.IsOpen)
                {
                    this.etat_Kinect = "Open...";
                }
                else
                {
                    this.etat_Kinect = "Not open";
                }
            }
        }

        private void Initialise_Kinect()
        {
            kinectSensor = KinectSensor.GetDefault();
            if (kinectSensor != null)
            {
                kinectSensor.Open();
                Fermeture_Du_Programme = false;

                //// open the reader for the body frames
                readerMultiFrame = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Body | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex | FrameSourceTypes.Infrared);
                this.coordinateMapper = this.kinectSensor.CoordinateMapper;
                this.displayWidth = kinectSensor.ColorFrameSource.FrameDescription.Width;
                this.displayHeight = kinectSensor.ColorFrameSource.FrameDescription.Height;
            }
            else
                Console.WriteLine("Could not obtain KinectSensor");
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
                        int id = Reduction_Id_Body(bodies, body);
                        float[] data = GetDataBody(body);
                        // PJE
                        SendLslDataOneBodyTracked(data);

                        //Update ID labels
                        UpdateSkelettonIDText(body);

                        // COORDINATE MAPPING
                        foreach (Joint joint in body.Joints.Values)
                        {
                            // 3D space point
                            CameraSpacePoint jointPosition = joint.Position;
                            if (isRecording)
                            {
                                AddRowToDataTable(data);

                                //Unused
                                //t0.Start();
                                //ListeDesSquelettes.Add(new Squelette(t0.ElapsedMilliseconds,
                                //   id, jointPosition.X, jointPosition.Y,
                                //    jointPosition.Z, joint.JointType, AbregeHandState(body.HandLeftState), AbregeHandConfidence(body.HandLeftConfidence)
                                //    , AbregeHandState(body.HandRightState), AbregeHandConfidence(body.HandRightConfidence), AbregeTrackingState(joint.TrackingState)));
                                //Unused
                            }

                            //Unused
                            //// 2D space point
                            //Point point = new Point();

                            //if (_mode == CameraMode.Color)
                            //{
                            //    ColorSpacePoint colorPoint =
                            //        kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(
                            //            jointPosition);

                            //    point.X = float.IsInfinity(colorPoint.X) ? 0 : colorPoint.X;
                            //    point.Y = float.IsInfinity(colorPoint.Y) ? 0 : colorPoint.Y;
                            //}
                            //else if (_mode == CameraMode.Depth || _mode == CameraMode.Infrared)
                            //{
                            //    DepthSpacePoint depthPoint
                            //        = kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(
                            //            jointPosition);

                            //    point.X = float.IsInfinity(depthPoint.X) ? 0 : depthPoint.X;
                            //    point.Y = float.IsInfinity(depthPoint.Y) ? 0 : depthPoint.Y;
                            //}
                            //Unused
                        }
                    }

                    if (drawSkeletonOverCamera)
                    {
                        dessin.DrawSkeleton(canvas, body);
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
                    if (this.bodies == null)
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

        private void UpdateSkelettonIDText(Body body)
        {
            trakingIdCourt.Text = dessin.idSqueletteChoisi.ToString();
            trakingIdFull.Text = body.TrackingId.ToString();
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (this.readerMultiFrame != null)
            {
                this.readerMultiFrame.Dispose();
                this.readerMultiFrame = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
            Fermeture_Du_Programme = true;

            if (this.thUpdate_Kinect_State != null)
            {
                thUpdate_Kinect_State.Abort();
            }
            if (this.thUpdate_Rec_State != null)
            {
                thUpdate_Rec_State.Abort();
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            throw new NotImplementedException();
        }

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
        }

        private void Affiche_Arborescence()
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                currentCSVpath = folderBrowserDialog.SelectedPath + "\\";
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
        }

        private void Update_Rec_State()
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(5));
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                Affiche_Etat_Record();
            }));
        }

        private void Update_Kinect_State()
        {
            while (!Fermeture_Du_Programme)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(5));
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Affiche_Etat_Kinect();
                }));
            }
        }

        private void Update_Couleur_Boutton()
        {
            while (!Fermeture_Du_Programme)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(5));
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Affiche_Etat_Boutton();
                }));
            }
        }

        private void Update_Path_State()
        {
            while (!Fermeture_Du_Programme)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(5));
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Affiche_Etat_Path();
                }));
            }
        }

        private string Formatage_DateHeure()
        {
            string dateHeureFormatee = null;
            string dateHeureNonFormatee = null;
            dateHeureNonFormatee = DateTime.Now.ToString();
            char[] dateHeureNonFormateeTableau = null;
            dateHeureNonFormateeTableau = dateHeureNonFormatee.ToCharArray();
            for (int i = 0; i < dateHeureNonFormatee.Length; i++)
            {
                if (dateHeureNonFormateeTableau[i] == ':' ||
                    dateHeureNonFormateeTableau[i] == ' ' ||
                    dateHeureNonFormateeTableau[i] == '\\' ||
                    dateHeureNonFormateeTableau[i] == '/')
                {
                    dateHeureNonFormateeTableau[i] = '_';
                }
            }
            dateHeureFormatee = new string(dateHeureNonFormateeTableau);

            return dateHeureFormatee;
        }

        private int Verification_CSV(string _path)
        {
            int nombreDeLignes = 0;
            if (File.Exists(_path))
            {
                using (var reader = new StreamReader(_path))
                {
                    while (reader.Peek() >= 0)
                    {
                        if (reader.Read() == '\n')
                        {
                            nombreDeLignes++;
                        }
                    }
                }
            }
            return nombreDeLignes;
        }

        #region Button Event

        private void OnRecordButtonClicked(object sender, RoutedEventArgs e)
        {
            isRecording = !isRecording;
            if (isRecording == true)
            {
                CreateDataTable();

                CSV_btn.IsEnabled = false;
                //Trop extrême
                thUpdate_Rec_State = new Thread(Update_Rec_State);
                thUpdate_Rec_State.SetApartmentState(ApartmentState.STA);
                thUpdate_Rec_State.Start();
                record_btn.Content = "Stop record";
            }
            else
            {
                t0.Reset();

                CSV_btn.IsEnabled = true;
                record_btn.Content = "Start record";
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Affiche_Etat_Record();
                }));
            }
        }

        private void Button_Click_Color(object sender, RoutedEventArgs e)
        {
            _mode = CameraMode.Color;
        }

        private void Button_Click_Depth(object sender, RoutedEventArgs e)
        {
            _mode = CameraMode.Depth;
        }

        private void Button_Click_IR(object sender, RoutedEventArgs e)
        {
            _mode = CameraMode.Infrared;
        }

        private void Button_Click_Body(object sender, RoutedEventArgs e)
        {
            drawSkeletonOverCamera = !drawSkeletonOverCamera;
        }

        private void CSV_btn_Click(object sender, RoutedEventArgs e)
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

        private void OnSetPathButtonClicked(object sender, RoutedEventArgs e)
        {
            Affiche_Arborescence();
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
    }
}