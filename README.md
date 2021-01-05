# Overview :
LSL Kinect is a C# Program that broadcast motion capture data, on your local network, by using Kinect V2 and LabStreamingLayer technology. It also save these data under CSV files.

# System Requirements :

You must meet the following system requirements :
https://docs.microsoft.com/en-us/previous-versions/windows/kinect/dn782036(v=ieb.10)?redirectedfrom=MSDN	

# User Guide :

First be sure to have your `Kinect V2` device correctly plugged in your computer on a `USB 3` port.

Go to  [Release](/bin/Release/) and launch `LSL_Kinect.exe`. 

When the app is open, place yourself or your test subject in front of the Kinect camera until the skeleton appears on the screen.
Then choose the correct skeleton ID, you can only record one body data at a time.
From there, you can start to broadcast these data using LSL and to record them on a local CSV file. 

Then use a LSL receiver program to get the broadcast and a viewing application for .xdf file to visualize the data. 
The software has been tested with LabRecorder and SigViewer.

- To download LabRecorder : [click here](https://github.com/labstreaminglayer/App-LabRecorder/releases).
- To download SigViewer  : [click here](https://github.com/cbrnr/sigviewer).

You can also define a custom sequence of action, by adding one to the configuration file called `SequenceConfig`, that is located next to the .exe file.
There is a specific documentation file for this : [Sequence Documentation](https://github.com/Benoit-Prigent/LSL-Kinect/blob/master/Documentation/Sequence%20Configuration%20File%20Documentation.pdf "Sequence Configuration File Documentation.pdf").

## User input
- Spacebar : switch to next sequence's step.
- Any key : send a message marker containing the key code.

# Known Issues :

- The FPS are not constant since the Kinect change its own frame rate by itself depending on the light exposure. You might need to look for the "changing framerate" markers to retrieve the original sampling rate of the signal.

# Developer Guide :

To develop with Kinect you need the correct SDK and driver installed, you can find an installation guide here :
https://docs.microsoft.com/en-us/previous-versions/windows/kinect/dn782035(v=ieb.10)

There is a trello board to track progress and incoming features, please ask permission to access it if you are part of the developpement team :
https://trello.com/b/p3PkSvUN/lsl-kinect


-----  

For more detailed informations, please see [Documentation](/Documentation/).  

-----  
Developed by Pierre JEAN (IMT Mines Alès), Denis MOTTET (Université Montpellier) and Benoit Prigent (Euromov) in collaboration with Makii MUTHALIB and Karima BAKHTI (CHU Montpellier) for the ReArm project (PHRIP-18-0731) funded by the French Ministry of Health.

Cite as
Benoit Prigent, Pierre Jean, Denis Mottet, Germain Faity, Karima Bakhti, & Makii Muthalib. (2020, December 1). KarimaBak/LSL-KinectV2: LSL-KinectV2 (Version V1.12). Zenodo. http://doi.org/10.5281/zenodo.4300183
