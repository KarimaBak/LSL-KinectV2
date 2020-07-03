# LSL Kinect documentation

* [CSV files output](#CSV-files-output)
	* [Header line](#header-line)
	* [Mocap data csv](#Mocap-data-csv)

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
|  Stream nominal rate |  Hertz | The effective rate of the record can vary due to the Kinect, that change its framerate on its own, depending on its exposure to light.
|  Sequence Name | string | If the record has been done without using sequence, then this value is irrevelant.

## Mocap data csv
A typical data file looks as follows :
```
INSERT EXAMPLE HERE
```

There is the header line on the first line as mention on the previous section. Followed by an empty line.
And then, there is the data part compound by 101 columns of data :

The third line is a header line that specify the date type of each column.
- Column 1: `Timestamp` : recording time with the following format in [UNIXTIME](https://cloud.google.com/dataprep/docs/html/UNIXTIME-Function_57344718) format .

- Column 2 - 101 : Theses columns need are grouped by `joint` (body part recognize by the kinect). Each of these group contains 4 columns :
	- JointName_X : The X position in meter (On the Kinect camera's horizontal axis).
	- JointName_Y : The Y position in meter (On the Kinect camera's vertical axis).
	- JointName_Z : The Z position in meter (On the Kinect camera's depth).
	- JointName_Conf : The confidence index of the Kinect on the exactitude of these data. 0 means it didn't find it, 0.5 means he assumed it with the others joints positions and 1 means the data is exact.
	
All the lines below the third one are data.