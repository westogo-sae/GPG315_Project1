using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

namespace Westhouse.WMidiTools
{


	public class MidiReader
	{
	    private static MidiStatus midiRunnningStatus;

	    public static bool ReadMidiFileToObj(string filePath, ref MidiObject midiObj)
	    {
	        

	    	var data = File.ReadAllBytes(filePath);

	    	if(data.Length < 4)
	    	{
	    		Debug.LogError("Error, File is Empty");
	    		return false;
	    	}


	    	// Reading Header

	    	// confirm MtHd 0 -> 3
	    	if(ByteReader.ReadAsString(data, 0, 4) != "MThd")
	    	{
	    		Debug.LogError("Error File is not a Standard Midi File");
	    		return false;
	    	}


	        //header length (should always be 6) 4 -> 7
	        int headerLength = ByteReader.ReadAsInt32(data, 4);

	        // midi format 8 -> 9
	        int format = ByteReader.ReadAsInt16(data, 8);
	        midiObj.format = format;

	        if(format != 1)
	        {
	        	Debug.LogError("Midi Importer Currently only supprts midi type 1");
	        	return false;
	        }

	        // number of Tracks 10 -> 11
	        int numTracks = ByteReader.ReadAsInt16(data, 10);
	        midiObj.tracks = new Track[numTracks];

	        //Time Divisions 12 -> 13
	        int timeDiv = ByteReader.ReadAsInt16(data, 12);
	        if((timeDiv & 0x80) != 0) // Midi is using SMPTE format
	        {
	        	Debug.LogError("Midi Importer currently does not support SMPTE time format");
	        	return false;
	        }
	        midiObj.timeDiv = timeDiv;

	        //first track chunk starts at bit 14)

	        int trkOffset = 14;

	        for(int i = 0; i < numTracks; i++) // iterate through tracks
	        {
	            //Confirm MTrk
	            if(ByteReader.ReadAsString(data, trkOffset, 4) != "MTrk")
	            {
	            	Debug.LogError($"Error, Midi Track {i} is not a supported Format");
	            	continue;
	            }



	            //track length
	            int trkChunkLength = ByteReader.ReadAsInt32(data, trkOffset + 4);

	            //value for scanning through chunk's bytes
	            int byteOffset = trkOffset + 8; // move past the trackchunk header
	            int trkChunkEnd = byteOffset + trkChunkLength;


	            List<Event> trackEvents = new();

	            long runningTime = 0;

	            while(byteOffset < trkChunkEnd)
	            {

	            	Event newEvent = null;

	                int eventTime = ByteReader.ReadVarInt(data, byteOffset, out int timeBytesLength);
	                byteOffset += timeBytesLength;

	                //Debug.Log(timeBytesLength);

	                byte eventID = data[byteOffset];

	                if(eventID == 0xFF) // meta Event
	                {
	                    newEvent = ParseMetaEvent(data, byteOffset, out int metaSize);
	                    byteOffset += metaSize;
	                }
	                else 
	                if(eventID == 0xF0 || eventID == 0xF7) // Sysex Event
	                {
	                    newEvent = ParseSysexEvent(data, byteOffset, out int SysexSize);
	                    byteOffset += SysexSize;
	                }
	                else // Midi Event
	                {
	                    newEvent = ParseMidiEvent(data, byteOffset, out int midiSize);
	                    byteOffset += midiSize; // midi event size
	                }


	                //set this Event's Delta Time;
	                if(newEvent != null)
	                {
	                	runningTime += eventTime;

	                	newEvent.SetTimeFromRaw(runningTime, midiObj.timeDiv);
	                	newEvent.InjectMidiObject(midiObj);
	                }

	                trackEvents.Add(newEvent);
	            }

	            //set midi file track events
	            midiObj.tracks[i] = new Track(trackEvents.ToArray());

	            //start of next track
	            trkOffset = trkChunkEnd; 

	        }

	        return midiObj;

	    }




	    ///// Evaluate Event types

	    static MidiEvent ParseMidiEvent(byte[] data, int start, out int midiSize)
	    {
	        int runningOffset = 0;
	        if((data[start] & 128) == 128) // if starting bit is set
	        {
	            //update using running status
	            midiRunnningStatus = new MidiStatus(data[start]);

	            midiSize = 1 + midiRunnningStatus.dataBytesCount;
	            runningOffset = 1;
	            
	        }else
	        {
	            midiSize = midiRunnningStatus.dataBytesCount;    
	        }

	        byte[] midiData = new byte[midiRunnningStatus.dataBytesCount];

	        Array.Copy(data, start + runningOffset, midiData, 0, midiData.Length);

	        return MidiEvent.Construct(midiRunnningStatus, midiData);

	    }

	    static MetaEvent ParseMetaEvent(byte[] data, int start, out int eventSize)
	    {  
	        // FF : 0
	        // type : 1
	        MetaEventType metaType = (MetaEventType)data[start + 1];

	        // length : 2 -> Variable
	        int length = ByteReader.ReadVarInt(data, start + 2, out int lenByteSize);

	        // data : 2+var -> end of chunk
	        byte[] metaData = new byte[length];


	        Array.Copy(data, start + 2 + lenByteSize, metaData, 0, length);


	        eventSize = 2 + lenByteSize + length;

	        return MetaEvent.Construct(metaType, metaData);
	    }

	    static SysexEvent ParseSysexEvent(byte[] data, int start, out int eventSize)
	    {
	        // F7/F0 : 0
	        // length : 1
	        int length = data[start + 1];

	        // data : 2 -> 2+len
	        byte[] sysData = new byte[length];

	        Array.Copy(data, start + 2, sysData, 0, length);

	        // F7 : end of chunk

	        eventSize = length + 3;

	        return new SysexEvent(sysData);
	    }

	    
	}
}