# LSL-Kinect :
C# Program that broadcast motion capture datas by using Kinect and LabStreamingLayer technology.

# User Guide :

First be sure to have your Kinect device correctly plugged in your computer.

Go to LSL-Kinect\bin\Release and launch LSL_Kinect.app

When the app is open, place yourself or your test subject in front of the Kinect camera until the skeletton appears on the screen.
Then clic on the skeletton's id at the top of his head. This will select him and start to register his joints (body parts) positions in the LSL broadcast.

Then use a LSL receiver programm to get the broadcast and a viewing application for .xdf file to visualise the datas. 
I have tested it with LabRecorder and SigViewer.
You can download LabRecorder here : https://github.com/labstreaminglayer/App-LabRecorder/releases.
You can download SigViewer here :  https://github.com/cbrnr/sigviewer

You can also change the camera rendering of the app to depth rendering and infrared rendering, by using the corresponding buttons at the bottom.

You can enable/disable the skeletton drawing on the screen by clicking the "Visu Tracking" button.

# Known Issues :

- The "Export CSV", "Record" and "Modifier" buttons don't work yet.
- The programm broadcast both data and markers stream on his entire lifetime. This may cause the record to looks weird, since the data will only appear in a fraction of the record. You may have to scroll throught your viewing application to find them.


# Developper Guide :

There is a trello to track progress and incoming feature, please ask permission to access it if you are part of the developpement team :
https://trello.com/b/p3PkSvUN/lsl-kinect
