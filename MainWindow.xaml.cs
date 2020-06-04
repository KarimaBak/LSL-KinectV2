﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Kinect;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Data;
using System.Globalization;
using LSL;

namespace LSL_Kinect
{

    enum CameraMode
    {
        Color,
        Depth,
        Infrared
    }

/*
    SpineBase = 0,
    SpineMid = 1,
    Neck = 2,
    Head = 3,
    ShoulderLeft = 4,
    ElbowLeft = 5,
    WristLeft = 6,
    HandLeft = 7,
    ShoulderRight = 8,
    ElbowRight = 9,
    WristRight = 10,
    HandRight = 11,
    HipLeft = 12,
    KneeLeft = 13,
    AnkleLeft = 14,
    FootLeft = 15,
    HipRight = 16,
    KneeRight = 17,
    AnkleRight = 18,
    FootRight = 19,
    SpineShoulder = 20,
    HandTipLeft = 21,
    ThumbLeft = 22,
    HandTipRight = 23,
    ThumbRight = 24
*/
    enum NUI_SKELETON_POSITION_INDEX
    {
        NUI_SKELETON_POSITION_HIP_CENTER = 0,
        NUI_SKELETON_POSITION_SPINE,
        NUI_SKELETON_POSITION_SHOULDER_CENTER,
        NUI_SKELETON_POSITION_HEAD,
        NUI_SKELETON_POSITION_SHOULDER_LEFT,
        NUI_SKELETON_POSITION_ELBOW_LEFT,
        NUI_SKELETON_POSITION_WRIST_LEFT,
        NUI_SKELETON_POSITION_HAND_LEFT,
        NUI_SKELETON_POSITION_SHOULDER_RIGHT,
        NUI_SKELETON_POSITION_ELBOW_RIGHT,
        NUI_SKELETON_POSITION_WRIST_RIGHT,
        NUI_SKELETON_POSITION_HAND_RIGHT,
        NUI_SKELETON_POSITION_HIP_LEFT,
        NUI_SKELETON_POSITION_KNEE_LEFT,
        NUI_SKELETON_POSITION_ANKLE_LEFT,
        NUI_SKELETON_POSITION_FOOT_LEFT,
        NUI_SKELETON_POSITION_HIP_RIGHT,
        NUI_SKELETON_POSITION_KNEE_RIGHT,
        NUI_SKELETON_POSITION_ANKLE_RIGHT,
        NUI_SKELETON_POSITION_FOOT_RIGHT,
        NUI_SKELETON_POSITION_SPINE_SHOULDER,
        NUI_SKELETON_POSITION_HAND_TIP_LEFT,
        NUI_SKELETON_POSITION_HAND_THUMB_LEFT,
        NUI_SKELETON_POSITION_HAND_TIP_RIGHT,
        NUI_SKELETON_POSITION_HAND_THUMB_RIGHT,
        NUI_SKELETON_POSITION_COUNT
    };


    enum NUI_SKELETON_TRACKING_STATE
    {
        NUI_SKELETON_NOT_TRACKED = 0,
        NUI_SKELETON_POSITION_ONLY = (NUI_SKELETON_NOT_TRACKED + 1),
        NUI_SKELETON_TRACKED = (NUI_SKELETON_POSITION_ONLY + 1)
    };

   

    public partial class MainWindow : Window
    {
        //-------------Variables-----------------------
        private MultiSourceFrameReader readerMultiFrame;
        private KinectSensor kinectSensor;
        private Body[] bodies = null;
        CameraMode _mode = CameraMode.Color;
        bool Fermeture_Du_Programme = false;
        Thread thUpdate_Rec_State;
        Thread thUpdate_Kinect_State;
        Thread thUpdate_Path_State;
        Thread thUpdate_Couleur_Boutton;
        private CoordinateMapper coordinateMapper = null;
        private int displayWidth;
        private int displayHeight;
        private List<Squelette> ListeDesSquelettes = new List<Squelette>();
        private Stopwatch t0 = new Stopwatch();
        private bool recEnCours = false;
        private string path = @"C:\Users\tomas.barbe\Desktop\CSV_Record\Kinect_2\";
        private string pathSelected = null;
        // PJE 
        private bool afficheCorps = true;
        private bool saisieBool = false;
        private int numberSpaceBarPress = 0;

        private Dessins dessin = new Dessins();

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


        /* PJE: partie LSL */
        const int NUI_SKELETON_POSITION_COUNT = (int)NUI_SKELETON_POSITION_INDEX.NUI_SKELETON_POSITION_FOOT_RIGHT + 1;
        const int NUM_CHANNELS_PER_JOINT = 4;
        const int NUM_CHANNELS_PER_SKELETON = ((int)NUI_SKELETON_POSITION_INDEX.NUI_SKELETON_POSITION_COUNT * NUM_CHANNELS_PER_JOINT) + 2;
        const int NUI_SKELETON_MAX_TRACKED_COUNT = 1;
        const int NUM_CHANNELS_PER_STREAM = NUI_SKELETON_MAX_TRACKED_COUNT * NUM_CHANNELS_PER_SKELETON;
        const int NUI_SKELETON_COUNT = 6;
        String[] joint_names = { "HipCenter", "Spine", "ShoulderCenter", "Head", "ShoulderLeft", "ElbowLeft", "WristLeft", "HandLeft", "ShoulderRight", "ElbowRight", "WristRight", "HandRight", "HipLeft", "KneeLeft", "AnkleLeft", "FootLeft", "HipRight", "KneeRight", "AnkleRight", "FootRight" };

        private liblsl.StreamInfo infoData = null;
        private liblsl.StreamOutlet outletData = null;
        private liblsl.StreamOutlet outletMarker = null;


        //--------------------MAIN-------------------
        public MainWindow()
        {
            InitializeComponent();           
            Initialise_Kinect();
            System.Threading.Thread.Sleep(500);            
            Affiche_Etat_Kinect();
            Affiche_Etat_Record();
            Affiche_Etat_Path();
            Affiche_Etat_Boutton();
            thUpdate_Kinect_State = new Thread(Update_Kinect_State);
            thUpdate_Kinect_State.SetApartmentState(ApartmentState.STA);
            thUpdate_Kinect_State.Start();
            thUpdate_Path_State = new Thread(Update_Path_State);
            thUpdate_Path_State.SetApartmentState(ApartmentState.STA);
            thUpdate_Path_State.Start();
            thUpdate_Couleur_Boutton = new Thread(Update_Couleur_Boutton);
            thUpdate_Couleur_Boutton.Start();
        }

        private void lslInfo(String sensorId)
        {
            liblsl.StreamInfo infoMetaData = new liblsl.StreamInfo("Kinect-LSL-MetaData", "Kinect", NUM_CHANNELS_PER_STREAM, 30, liblsl.channel_format_t.cf_float32, sensorId);

            liblsl.XMLElement channels = infoMetaData.desc().append_child("channels");
            for (int s = 0; s < NUI_SKELETON_MAX_TRACKED_COUNT; s++)
            {
                for (int k = 0; k < NUI_SKELETON_POSITION_COUNT; k++)
                {
                    channels.append_child("channel")
                        .append_child_value("label", joint_names[k] += "_X" )
                        .append_child_value("marker", joint_names[k])
                        .append_child_value("type", "PositionX")
                        .append_child_value("unit", "meters");
                    channels.append_child("channel")
                        .append_child_value("label", joint_names[k] += "_Y")
                        .append_child_value("marker", joint_names[k])
                        .append_child_value("type", "PositionY")
                        .append_child_value("unit", "meters");
                    channels.append_child("channel")
                        .append_child_value("label", joint_names[k] += "_Z")
                        .append_child_value("marker", joint_names[k])
                        .append_child_value("type", "PositionZ")
                        .append_child_value("unit", "meters");
                    channels.append_child("channel")
                        .append_child_value("label", joint_names[k] += "_Conf") 
                        .append_child_value("marker", joint_names[k])
                        .append_child_value("type", "Confidence")
                        .append_child_value("unit", "normalized");
                }
                channels.append_child("channel")
                    .append_child_value("label", "SkeletonTrackingId" + s)
                    .append_child_value("type", "TrackingId");
                channels.append_child("channel")
                    .append_child_value("label", "SkeletonQualityFlags" + s);
            }

            // misc meta-data
            infoMetaData.desc().append_child("acquisition")
                .append_child_value("manufacturer", "Microsoft")
                .append_child_value("model", "Kinect 2.0");

            
            liblsl.StreamOutlet outletMetaData = new liblsl.StreamOutlet(infoMetaData);
            

            infoData = new liblsl.StreamInfo("Kinect-LSL-Data", "Kinect", NUM_CHANNELS_PER_STREAM, 30, liblsl.channel_format_t.cf_float32, sensorId);
            outletData = new liblsl.StreamOutlet(infoData);



            //Marker data
            liblsl.StreamInfo streamMarker = new liblsl.StreamInfo("Kinect-LSL-Markers", "Kinect", 1, 0, liblsl.channel_format_t.cf_string, sensorId);
            outletMarker = new liblsl.StreamOutlet(streamMarker);

        }

        private void SendLslDataOneBodyTracked(String sensorId, Body body, int nombreSquelette)
        {
            float[] data = new float[NUM_CHANNELS_PER_STREAM];// data length = 102 alors que (25 jointures) * 4 valeurs (x y z) + body.trackiId + isTracked ? 
                     

            // Comparaison entre le numéro cours choisi par le bouton dans Dessins.cs et le numéro de squelette détecté.
            String shortTrakingIdCalcul = (body.TrackingId - Dessins.KINECT_MINIMAL_ID).ToString();
            
            if ((dessin.idSqueletteChoisi != -1) && (dessin.idSqueletteChoisi == Convert.ToInt64(shortTrakingIdCalcul)) )
            {
                
                int i = BuildLSLData(data, 0, body.Joints[JointType.SpineBase] );
                i = BuildLSLData(data, i, body.Joints[JointType.SpineMid]);
                i = BuildLSLData(data, i, body.Joints[JointType.Neck]);
                i = BuildLSLData(data, i, body.Joints[JointType.Head]);
                i = BuildLSLData(data, i, body.Joints[JointType.ShoulderLeft]);
                i = BuildLSLData(data, i, body.Joints[JointType.ElbowLeft]);
                i = BuildLSLData(data, i, body.Joints[JointType.WristLeft]);
                i = BuildLSLData(data, i, body.Joints[JointType.HandLeft]);
                i = BuildLSLData(data, i, body.Joints[JointType.ShoulderRight]);
                i = BuildLSLData(data, i, body.Joints[JointType.ElbowRight]);
                i = BuildLSLData(data, i, body.Joints[JointType.WristRight]);
                i = BuildLSLData(data, i, body.Joints[JointType.HandRight]);
                i = BuildLSLData(data, i, body.Joints[JointType.HipLeft]);
                i = BuildLSLData(data, i, body.Joints[JointType.KneeLeft]);
                i = BuildLSLData(data, i, body.Joints[JointType.AnkleLeft]);
                i = BuildLSLData(data, i, body.Joints[JointType.FootLeft]);
                i = BuildLSLData(data, i, body.Joints[JointType.HipRight]);
                i = BuildLSLData(data, i, body.Joints[JointType.KneeRight]);
                i = BuildLSLData(data, i, body.Joints[JointType.AnkleRight]);
                i = BuildLSLData(data, i, body.Joints[JointType.FootRight]);
                i = BuildLSLData(data, i, body.Joints[JointType.SpineShoulder]);
                i = BuildLSLData(data, i, body.Joints[JointType.HandTipLeft]);
                i = BuildLSLData(data, i, body.Joints[JointType.ThumbLeft]);
                i = BuildLSLData(data, i, body.Joints[JointType.HandTipRight]);
                i = BuildLSLData(data, i, body.Joints[JointType.ThumbRight]);

                
                data[i++] = body.TrackingId;
                //PJE Test avec numéro squelette court ne pas utiliser l'ID court !!! 
                // data[i++] = Convert.ToInt64(shortTrakingIdCalcul);
                if (body.IsTracked)
                {
                    data[i++] = 1f;
                }
                else
                {
                    data[i++] = -1f;
                }               
            }

            outletData.push_sample(data);
            // PJE 
            Console.WriteLine("outletData.push_sample(data);   traking court: " + dessin.idSqueletteChoisi + " traking : " + body.TrackingId );

            trakingIdCourt.Text = dessin.idSqueletteChoisi.ToString();
            trakingIdFull.Text = body.TrackingId.ToString();

        }

        // PJE Ce code est supposé marché mais remplacé par la fonction de test ci-dessous
        private static int BuildLSLData(float[] data, int i, Joint joint)
        {
            CameraSpacePoint jointPosition = joint.Position;
            data[i++] = jointPosition.X;
            data[i++] = jointPosition.Y;
            data[i++] = jointPosition.Z;
            data[i++] = (float)joint.TrackingState / 2.0f;
            return i;
        }

        // PJE Version de test
 /*       private static int BuildLSLData(float[] data, int i, Joint joint)
        {
            CameraSpacePoint jointPosition = joint.Position;
            data[i++] = 1;
            data[i++] = 2;
            data[i++] = 3;
            data[i++] = 4;
            return i;
        }
*/

        private void SendLslDataAllBodies(String sensorId , Body[] bodies)
        {            
            float[] data = new float[NUM_CHANNELS_PER_STREAM];
            int i = 0;
            
            foreach (var body in bodies)
            {
                if (body.IsTracked)
                {
                    // Comparaison entre le numéro cours choisi par le bouton dans Dessins.cs et le numéro de squelette détecté.
                    String shortTrakingIdCalcul = (body.TrackingId - Dessins.KINECT_MINIMAL_ID).ToString();
                    
                    if ( (dessin.idSqueletteChoisi  != -1) && (dessin.idSqueletteChoisi.ToString() == shortTrakingIdCalcul) )
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
                        }else{
                            data[i++] = -1f;
                        }
                        i++;
                    }
                }

                
            }
            outletData.push_sample(data);
            // PJE Console.WriteLine("outletData.push_sample(data);   ");
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.readerMultiFrame != null)
            {
                this.readerMultiFrame.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;
                lslInfo(kinectSensor.UniqueKinectId );                
            }
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
            if (recEnCours==true)
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
            if (path != null)
            {
                this.etat_Path = path;
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
            else if(_mode==CameraMode.Depth)
            {
                color_btn.Background = System.Windows.Media.Brushes.LightBlue;
                depth_btn.Background = System.Windows.Media.Brushes.LightGreen;
                infraRed_btn.Background = System.Windows.Media.Brushes.LightBlue;
            }
            else if(_mode==CameraMode.Infrared)
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

            if(afficheCorps)
                VisuTracking_btn.Background= System.Windows.Media.Brushes.LightGreen;
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

        // reader selon la source
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var acquireFrame = e.FrameReference.AcquireFrame();
            //Color 
            using (var frame = acquireFrame.ColorFrameReference.AcquireFrame())
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
            using (var frame = acquireFrame.InfraredFrameReference.AcquireFrame())
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
            using (var frame = acquireFrame.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == CameraMode.Depth)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }
            // Body
            using (BodyFrame bodyFrame = acquireFrame.BodyFrameReference.AcquireFrame())
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
            if (bodies != null)
            {
                //PJE LSL 
                // SendLslDataAllBodies(kinectSensor.UniqueKinectId , bodies);
                

                foreach (var body in bodies)
                {
                    if (body.IsTracked)
                    {

                         int id = Reduction_Id_Body(bodies, body);
                        // PJE
                        SendLslDataOneBodyTracked(kinectSensor.UniqueKinectId, body , bodies.Length);


                        // COORDINATE MAPPING
                        foreach (Joint joint in body.Joints.Values){
                            // 3D space point
                            CameraSpacePoint jointPosition = joint.Position;
                            if (recEnCours && saisieBool)
                            {
                                t0.Start();
                                ListeDesSquelettes.Add(new Squelette(t0.ElapsedMilliseconds,
                                   id, jointPosition.X, jointPosition.Y,
                                    jointPosition.Z, joint.JointType, AbregeHandState(body.HandLeftState),AbregeHandConfidence(body.HandLeftConfidence)
                                    , AbregeHandState(body.HandRightState), AbregeHandConfidence(body.HandRightConfidence), AbregeTrackingState(joint.TrackingState)));
                            }
                            // 2D space point
                            Point point = new Point();

                            if (_mode == CameraMode.Color)
                            {
                                ColorSpacePoint colorPoint =
                                    kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(
                                        jointPosition);

                                point.X = float.IsInfinity(colorPoint.X) ? 0 : colorPoint.X;
                                point.Y = float.IsInfinity(colorPoint.Y) ? 0 : colorPoint.Y;
                            }
                            else if (_mode == CameraMode.Depth || _mode == CameraMode.Infrared)
                            {
                                DepthSpacePoint depthPoint
                                    = kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(
                                        jointPosition);

                                point.X = float.IsInfinity(depthPoint.X) ? 0 : depthPoint.X;
                                point.Y = float.IsInfinity(depthPoint.Y) ? 0 : depthPoint.Y;
                            }
                        }
                    }
                    if (afficheCorps)
                    {
                        // canvas.DrawSkeleton(body);
                        dessin.DrawSkeleton(canvas, body);
                       
                    }
                }
            }
        }

        private int Reduction_Id_Body(Body[] _bodies, Body _body)
        {
            int id = 999;
            if (_bodies[0].TrackingId == _body.TrackingId)
                id = 0;
            else if (_bodies[1].TrackingId == _body.TrackingId)
                id = 1;
            else if (_bodies[2].TrackingId == _body.TrackingId)
                id = 2;
            else if (_bodies[3].TrackingId == _body.TrackingId)
                id = 3;
            else if (_bodies[4].TrackingId == _body.TrackingId)
                id = 4;
            else if (_bodies[5].TrackingId == _body.TrackingId)
                id = 5;
            else
                id = 999;
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
                pathSelected = folderBrowserDialog.SelectedPath+"\\";
        }

        private void Button_Click_Record(object sender, RoutedEventArgs e)
        {
            saisieBool = false;
            recEnCours = !recEnCours;
            if (recEnCours == true)
            {
                if (path == null)
                {
                    Affiche_Arborescence();
                }                
                CSV_btn.IsEnabled = false;
                thUpdate_Rec_State = new Thread(Update_Rec_State);
                thUpdate_Rec_State.SetApartmentState(ApartmentState.STA);
                thUpdate_Rec_State.Start();
                Saisie saisie_window = new Saisie();
                saisie_window.ShowDialog();
                saisieBool = saisie_window.selectionBool;
                if (saisieBool == true)
                {
                    // Initialise_BkgwrEcrireSurServeur();
                    record_btn.Content = "Stop record";
                }
                else
                    recEnCours = !recEnCours;
            }
            if (recEnCours == false)
            {
                t0.Reset();
                
                CSV_btn.IsEnabled = true;
                record_btn.Content = "Record";
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Affiche_Etat_Record();
                }));
            }
        }
 
        private void Ecrire_CSV(DataTable _dataTable, string _nomFichier)
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            using (var writer = new StreamWriter(path))
            {
                writer.AutoFlush = true;
                writer.WriteLine(_nomFichier);
                writer.Write("TimeStamp(s)");
                for(int i=0;i<25;i++)
                    writer.Write(";X." + ((JointType)i).ToString()+"(m);"+"Y." + ((JointType)i).ToString() + "(m);"+ "Z." + ((JointType)i).ToString()
                        + "(m);"+"TrkState."+ ((JointType)i).ToString());

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
                            tempY[i]+";"+ tempZ[i]+";"
                            +row["TrackingState_"+((JointType)i).ToString()]+";");

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
            t0Median = ((t0Tab[12] + t0Tab[13]) / 2)*0.001;
            
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
            while(!Fermeture_Du_Programme)
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
            afficheCorps = !afficheCorps;
        }

        private void CSV_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Initialise_BkgwrLireServeur();
            }
            catch(Exception ex)
            {

                System.Windows.Forms.MessageBox.Show(ex.Message, "Erreur dans CSV_button_Click");
            }
        }
       
        private void Modifier_btn_Click(object sender, RoutedEventArgs e)
        {
            Affiche_Arborescence();
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ( e.Key == System.Windows.Input.Key.Space)
            {
                LSLMarkerSendButton_Click(null, null);
            }
        }

        private void LSLMarkerSendButton_Click(object sender, RoutedEventArgs e)
        {
            numberSpaceBarPress++;
            String[] dataMarker = new String[1];
            dataMarker[0] = numberSpaceBarPress.ToString();
            outletMarker.push_sample(dataMarker);
            LslNumberSpaceBarPress.Text = "" + (numberSpaceBarPress - 1) + "    at timeStamp: " + DateTime.Now.ToString("hh:mm:ss.fff");
        }
    }


}
