# LSL-Kinect :
C# Program that broadcast motion capture datas by using Kinect and LabStreamingLayer technology.

# System Requirements :

You must meet the following system requirements :
https://docs.microsoft.com/en-us/previous-versions/windows/kinect/dn782036(v=ieb.10)?redirectedfrom=MSDN	

# User Guide :

First be sure to have your Kinect device correctly plugged in your computer on a USB 3 port.

Go to LSL-Kinect\bin\Release and launch LSL_Kinect.app

When the app is open, place yourself or your test subject in front of the Kinect camera until the skeletton appears on the screen.
Then choose the correct skeleton ID, you can only record one body data at a time.
From there, you can choose to broadcast these data using LSL on your local network, or to record them on a local CSV file. 

Then use a LSL receiver programm to get the broadcast and a viewing application for .xdf file to visualise the datas. 
The software has been tested with LabRecorder and SigViewer.

- Link to download LabRecorder : https://github.com/labstreaminglayer/App-LabRecorder/releases.
- Link to download SigViewer :  https://github.com/cbrnr/sigviewer.

You can also change the camera rendering of the app to depth rendering and infrared rendering, by using the corresponding buttons at the bottom.

You can enable/disable the skeletton drawing on the screen by clicking the "Visu Tracking" button.

# Known Issues :

- The FPS are lower than expected (15 FPS)

# Developper Guide :

There is a trello to track progress and incoming features, please ask permission to access it if you are part of the developpement team :
https://trello.com/b/p3PkSvUN/lsl-kinect
