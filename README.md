# Overview :
C# Program that broadcast motion capture data by using Kinect and LabStreamingLayer technology.

# System Requirements :

You must meet the following system requirements :
https://docs.microsoft.com/en-us/previous-versions/windows/kinect/dn782036(v=ieb.10)?redirectedfrom=MSDN	

# User Guide :

First be sure to have your Kinect device correctly plugged in your computer on a USB 3 port.

Go to LSL-Kinect\bin\Release and launch LSL_Kinect.app

When the app is open, place yourself or your test subject in front of the Kinect camera until the skeletton appears on the screen.
Then choose the correct skeleton ID, you can only record one body data at a time.
From there, you can start to broadcast these data using LSL and to record them on a local CSV file. 

Then use a LSL receiver program to get the broadcast and a viewing application for .xdf file to visualize the data. 
The software has been tested with LabRecorder and SigViewer.

- Link to download LabRecorder : https://github.com/labstreaminglayer/App-LabRecorder/releases.
- Link to download SigViewer :  https://github.com/cbrnr/sigviewer.

# Known Issues :

- The FPS are not constant since the Kinect change its own frame rate by itself depending on the light exposure. You might need to look for the "changing framerate" markers to retrieve the original sampling rate of the signal.

# Developper Guide :

To develop with Kinect you need the correct SDK and driver installed, you can find an installation guide here :
https://docs.microsoft.com/en-us/previous-versions/windows/kinect/dn782035(v=ieb.10)

There is a trello to track progress and incoming features, please ask permission to access it if you are part of the developpement team :
https://trello.com/b/p3PkSvUN/lsl-kinect
