
# LSL Kinect documentation
  
## Table of Contents

* [Quick Start](#quick-start)
* [Sequence](#sequence)
* [LSL Streams](#lsl-streams)
	*  [Visualisation](#visualisation)
* [CSV files output](#csv-files-output)
	* [Header line](#header-line)
	* [Mocap data csv](#mocap-data-csv)
	* [Marker csv](#marker-csv)
		* [Marker Message](#marker-message)

# Quick Start
LSL Kinect is a C# Program that broadcast motion capture data, on your local network, by using Kinect V2 and LabStreamingLayer technology. It also save these data under CSV files.

The program is meant to be work with a specific device called `Kinect V2`, and won't work with previous or next version of the Kinect.
Be sure to have the LSL library (`liblsl32.dll`) next to the program's .exe file.

# Sequence

LSL Kinect program allows the user to predefine several sequence of actions, by using a XML configuration file. There is a dedicated documentation for this : [Sequence Documentation](https://github.com/Benoit-Prigent/LSL-Kinect/blob/master/Documentation/Sequence%20Configuration%20File%20Documentation.pdf "Sequence Configuration File Documentation.pdf").

# LSL Streams
LSL Kinect streams data and markers following the LSL specifications : https://github.com/sccn/xdf/wiki/Specifications.

Data are stored in a stream of type [MoCap](https://github.com/sccn/xdf/wiki/MoCap-Meta-Data).
Markers are stored in a stream of type [Markers](https://github.com/sccn/xdf/wiki/Markers-Meta-Data).

The markers and the data share the same timeline.
The broadcast doesn't verify if there is any listener to the stream before sending the data.
The data broadcast through LSL will always be recorded as well in CSV files.

## Visualisation

You can record the LSL stream onto a XDF file using [LabRecorder](https://github.com/labstreaminglayer/App-LabRecorder/releases).
The XDF file can be visualised by using [SigViewer](https://github.com/cbrnr/sigviewer) or any other XDF visualiser tools. For SigViewer, you can display both the data and the markers on the same graphic, if you record them on the same file.

Below an example of body motion data recorded using LSL Kinect and displayed using SigViewer :
![SigViewer Sample Data](https://github.com/Benoit-Prigent/LSL-Kinect/blob/master/Documentation/LSL_Kinect_SigViewer_Sample_Data.PNG)

![SigViewer Sample Markers](https://github.com/Benoit-Prigent/LSL-Kinect/blob/master/Documentation/LSL_Kinect_SigViewer_Sample_Markers.PNG)

# CSV files output
LSL-Kinect CSV output consists in 2 files :
- `LSL_Kinect_Markers_Data--TIME.csv` : the markers generated over time  
- `LSL_Kinect_Capture_MoCap_Data--TIME.csv` : the body motion data captured over time

Both of these files will include the time in their name with the following format `yyyy-MM-dd--HH-mm-ss`
Example of a file name : `LSL_Kinect_Markers_Data--2020-07-03--16-01-47`

These CSV files are organised as follows :
- Header line
- Data block

## Header line
The first line in the CSV files is the configuration line. The values it contains allow you to retrieve the configuration of the software used to record the data.
The header block is **identical** in all CSV files corresponding to the same record.

The configuration line is a collection of name-value pairs, where pairs are separated by `','`, within which name and value are separated by `' : '`.  
An exemple of the first line is:
```
Software : LSL_Kinect,Version : 1.0.4.1,Stream nominal rate : 15,Sequence Name : Reaching Task
```
After parsing the previous line, we get :
```
Software : LSL_Kinect
Version : 1.0.4.1
Stream nominal rate : 15
Sequence Name : Reaching Task
```

The name of each value should be easy to understand, and the table below provides more information:  


| Value |  Unit | Comment |
| ------------- |------------- | ------------ |
|  Software |  String | 
|  Version |  Assembly version | Example : `1.0.4.1`
|  Stream nominal rate |  Hertz | The effective rate of the record can vary due to the Kinect, that change its frame rate on its own, depending on its exposure to light.
|  Sequence Name | string | If the record has been done without using sequence, then this value is irrelevant.

## Mocap data csv
A typical data file looks as follows :
```
INSERT EXAMPLE HERE
```

There is the header line on the first line as mention on the previous section. Followed by an empty line.
And then, there is the data part compound by 101 columns of data :

The third line is a header line that specify the date type of each column.
- Column 1: `Timestamp` : recording time in [UNIXTIME](https://cloud.google.com/dataprep/docs/html/UNIXTIME-Function_57344718) format .

- Column 2 - 101 : Theses columns need are grouped by `joint` (body part recognise by the kinect). Each of these group contains 4 columns :
	- JointName_X : The X position in meter (On the Kinect camera's horizontal axis).
	- JointName_Y : The Y position in meter (On the Kinect camera's vertical axis).
	- JointName_Z : The Z position in meter (On the Kinect camera's depth).
	- JointName_Conf : The confidence index of the Kinect on the exactitude of these data. 0 means it didn't find it, 0.5 means he assumed it with the others joints positions and 1 means the data is exact.
	
All the lines below the third one are data.# LSL Kinect documentation

* [CSV files output](#csv-files-output)
	* [Header line](#header-line)
	* [Mocap data csv](#mocap-data-csv)

# CSV files output
LSL-Kinect CSV output consists in 2 files :
- `LSL_Kinect_Markers_Data--TIME.csv` : the markers generated over time  
- `LSL_Kinect_Capture_MoCap_Data--TIME.csv` : the body motion data captured over time

Both of these files will include the time in their name with the following format `yyyy-MM-dd--HH-mm-ss`
Example of a file name : `LSL_Kinect_Markers_Data--2020-07-03--16-01-47`

These CSV files are organized as follows :
- Header line
- Data block

## Header line
The first line in the CSV files is the configuration line. The values it contains allow you to retrieve the configuration of the software used to record the data.
The header block is **identical** in all CSV files corresponding to the same record.

The configuration line is a collection of name-value pairs, where pairs are separated by `','`, within which name and value are separated by `' : '`.  
An example of the first line is:
```
Software : LSL_Kinect,Version : 1.0.4.1,Stream nominal rate : 15,Sequence Name : Reaching Task
```
After parsing the previous line, we get :
```
Software : LSL_Kinect
Version : 1.0.4.1
Stream nominal rate : 15
Sequence Name : Reaching Task
```

The name of each value should be easy to understand, and the table below provides more information:  


| Value |  Unit | Comment |
| ------------- |------------- | ------------ |
|  Software |  String | 
|  Version |  Assembly version | Example : `1.0.4.1`
|  Stream nominal rate |  Hertz | The effective rate of the record can vary due to the Kinect, that change its framerate on its own, depending on its exposure to light.
|  Sequence Name | string | If the record has been done without using sequence, then this value is irrevelant.

## Mocap data csv
A typical data file looks as follows :
```
Software : LSL_Kinect,Version : 1.0.4.1,Stream nominal rate : 15,Sequence Name : Circular Steering Task

TimeSpan,SpineBase_X,SpineBase_Y,SpineBase_Z,SpineBase_Conf,SpineMid_X,SpineMid_Y,SpineMid_Z,SpineMid_Conf,Neck_X,Neck_Y,Neck_Z,Neck_Conf,Head_X,Head_Y,Head_Z,Head_Conf,ShoulderLeft_X,ShoulderLeft_Y,ShoulderLeft_Z,ShoulderLeft_Conf,ElbowLeft_X,ElbowLeft_Y,ElbowLeft_Z,ElbowLeft_Conf,WristLeft_X,WristLeft_Y,WristLeft_Z,WristLeft_Conf,HandLeft_X,HandLeft_Y,HandLeft_Z,HandLeft_Conf,ShoulderRight_X,ShoulderRight_Y,ShoulderRight_Z,ShoulderRight_Conf,ElbowRight_X,ElbowRight_Y,ElbowRight_Z,ElbowRight_Conf,WristRight_X,WristRight_Y,WristRight_Z,WristRight_Conf,HandRight_X,HandRight_Y,HandRight_Z,HandRight_Conf,HipLeft_X,HipLeft_Y,HipLeft_Z,HipLeft_Conf,KneeLeft_X,KneeLeft_Y,KneeLeft_Z,KneeLeft_Conf,AnkleLeft_X,AnkleLeft_Y,AnkleLeft_Z,AnkleLeft_Conf,FootLeft_X,FootLeft_Y,FootLeft_Z,FootLeft_Conf,HipRight_X,HipRight_Y,HipRight_Z,HipRight_Conf,KneeRight_X,KneeRight_Y,KneeRight_Z,KneeRight_Conf,AnkleRight_X,AnkleRight_Y,AnkleRight_Z,AnkleRight_Conf,FootRight_X,FootRight_Y,FootRight_Z,FootRight_Conf,SpineShoulder_X,SpineShoulder_Y,SpineShoulder_Z,SpineShoulder_Conf,HandTipLeft_X,HandTipLeft_Y,HandTipLeft_Z,HandTipLeft_Conf,ThumbLeft_X,ThumbLeft_Y,ThumbLeft_Z,ThumbLeft_Conf,HandTipRight_X,HandTipRight_Y,HandTipRight_Z,HandTipRight_Conf,ThumbRight_X,ThumbRight_Y,ThumbRight_Z,ThumbRight_Conf
"1,594038E+12",0.175551891,-0.119142294,1.52407134,1,0.3144471,0.1296859,1.50274742,1,0.4455801,0.368258566,1.47191107,1,0.5880223,0.44030574,1.48123956,1,0.2681119,0.361614943,1.46703887,1,0.0352887958,0.29846403,1.509149,1,-0.141383246,0.192453653,1.41446292,1,-0.242076889,0.149071708,1.35373056,1,0.492355436,0.182631537,1.52888656,1,0.5584163,0.005306263,1.44260383,1,0.51642406,-0.09247108,1.263069,0.5,0.5100887,-0.125957876,1.092428,1,0.1161809,-0.0847598761,1.48362911,1,-0.0221339464,-0.3592159,1.33572555,1,-0.05273804,-0.6712574,1.33773029,1,-0.0361741632,-0.7123651,1.21535468,0.5,0.226979077,-0.1480456,1.49643779,1,0.231284291,-0.454292327,1.374002,1,0.239877477,-0.7874551,1.29715085,0.5,0.2549475,-0.8147666,1.17117822,0.5,0.413640618,0.3101986,1.48169649,1,-0.293772161,0.125051931,1.33037353,1,-0.250790119,0.103520155,1.34783483,1,0.476465285,-0.161461383,1.04780519,1,0.5863485,-0.1510081,1.10009944,1
"1,594038E+12",0.176605865,-0.118880816,1.52548409,1,0.315454125,0.1299321,1.50346982,1,0.446328551,0.368252933,1.47174,1,0.589692533,0.439867228,1.48060346,1,0.268727243,0.3617769,1.46692908,1,0.0365552753,0.299530566,1.508606,1,-0.140097722,0.191860661,1.41220534,1,-0.233816937,0.13972196,1.350207,1,0.494170874,0.183426112,1.5287987,1,0.6015063,-0.0117291929,1.5718143,1,0.492389828,-0.08982421,1.41188443,0.5,0.4570438,-0.117284745,1.2810868,1,0.117044114,-0.0847066641,1.48547435,1,-0.0207647439,-0.362441331,1.33741367,1,-0.0522312857,-0.673394859,1.33756053,1,-0.03602031,-0.7142447,1.21501625,0.5,0.22818771,-0.147628188,1.4974277,1,0.231671989,-0.4560771,1.37520373,1,0.238736674,-0.78713274,1.29838765,0.5,0.253462434,-0.8145419,1.17236221,0.5,0.414508939,0.310242951,1.48168349,1,-0.282737821,0.111965932,1.32757568,1,-0.2123118,0.106143393,1.32524586,1,0.450742,-0.1256609,1.2385956,1,0.479238838,-0.0823495239,1.24120009,1

```

There is the header line on the first line as mention on the previous section. Followed by an empty line.
And then, there is the data part compound by 101 columns of data :

The third line is a header line that specify the date type of each column.
- Column 1: `Timestamp` : recording time in [UNIXTIME](https://cloud.google.com/dataprep/docs/html/UNIXTIME-Function_57344718) format .

- Column 2 - 101 : Theses columns need are grouped by `joint` (body part recognise by the kinect). Each of these group contains 4 columns :
	- JointName_X : The X position in meter (On the Kinect camera's horizontal axis).
	- JointName_Y : The Y position in meter (On the Kinect camera's vertical axis).
	- JointName_Z : The Z position in meter (On the Kinect camera's depth).
	- JointName_Conf : The confidence index of the Kinect on the exactitude of these data. 0 means it didn't find it, 0.5 means he assumed it with the others joints positions and 1 means the data is exact.
	
All the lines below the third one are data.

## Marker csv
A typical marker file looks as follows :
```
Software : LSL_Kinect,Version : 1.0.4.1,Stream nominal rate : 15,Sequence Name : Circular Steering Task

TimeSpan,Marker Message
2020-07-06 14:20:30.433,Start : Start recording
2020-07-06 14:20:32.725,Message : Key Pressed : P
2020-07-06 14:20:35.503,Stop : Stop recording
```
There is the header line on the first line as mention on the "header line" section. Followed by an empty line.  
And then, there is the marker part compound by only 2 columns of data :

- Column 1 : `Timestamp` : recording time in `yyyy-MM-dd HH:mm:ss.fff` format .
- Column 2 : `Marker Message` : the message sent by LSL Kinect.

### Marker Message

The marker message depend on the user's action. 
- If he start the recording manually or using the sequence step, the marker will start with `Start :`.
- If he stop the recording manually or using the sequence step, the marker will start with `Stop :`.
- If the marker is only an information and does not affect the LSL broadcast flow, it will start with `Message :`. A message marker is sent on several occasion :
-- On any key pressed by the user, e.g. `2020-07-06 14:20:32.725,Message : Key Pressed : P`
-- When the Kinect change its frame rate due to light exposure. This is useful to retrieve the effective sampling rate.
-- When a sequence contains a `Message` step.