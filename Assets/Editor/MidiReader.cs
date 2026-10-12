using System;
using System.IO;
using UnityEngine;

namespace Westhouse.WMidiTools
{
    using System.Collections.Generic;
    using MidiRaw;


	public class MidiReader
	{
	    private static MidiStatus midiRunnningStatus;

	    public static SMF ReadMidiFile(string filePath)
	    {
	        
	    	SMF midiFile = ScriptableObject.CreateInstance<SMF>();

	    	var data = File.ReadAllBytes(filePath);

	    	if(data.Length < 4)
	    	{
	    		Debug.LogError("Error, File is Empty");
	    		return null;
	    	}


	    	// Reading Header

	    	// confirm MtHd 0 -> 3
	    	if(ReadAsString(data, 0, 4) != "MThd")
	    	{
	    		Debug.LogError("Error File is not a Standard Midi File");
	    		return null;
	    	}


	        //header length (should always be 6) 4 -> 7
	        int headerLength = ReadAsInt32(data, 4);

	        // midi format 8 -> 9
	        int format = ReadAsInt16(data, 8);
	        midiFile.format = format;

	        if(format != 1)
	        {
	        	Debug.LogError("Midi Importer Currently only supprts midi type 1");
	        	return null;
	        }

	        // number of Tracks 10 -> 11
	        int numTracks = ReadAsInt16(data, 10);
	        midiFile.midiTracks = new MidiRaw.Track[numTracks];

	        //Time Divisions 12 -> 13
	        int timeDiv = ReadAsInt16(data, 12);
	        midiFile.timeDiv = timeDiv;

	        //first track chunk starts at bit 14)

	        int trkOffset = 14;

	        for(int i = 0; i < numTracks; i++) // iterate through tracks
	        {
	            //Confirm MTrk
	            if(ReadAsString(data, trkOffset, 4) != "MTrk")
	            {
	            	Debug.LogError($"Error, Midi Track {i} is not a supported Format");
	            	continue;
	            }



	            //track length
	            int trkChunkLength = ReadAsInt32(data, trkOffset + 4);

	            //value for scanning through chunk's bytes
	            int byteOffset = trkOffset + 8; // move past the trackchunk header
	            int trkChunkEnd = byteOffset + trkChunkLength;


	            List<MidiRaw.Event> trackEvents = new();


	            while(byteOffset < trkChunkEnd)
	            {

	            	MidiRaw.Event newEvent = null;

	                int eventTime = ReadVarInt(data, byteOffset, out int timeBytesLength);
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
	                	newEvent.deltaTime = eventTime;
	                }

	                trackEvents.Add(newEvent);
	            }

	            //set midi file track events
	            midiFile.midiTracks[i] = new EventTrack(trackEvents.ToArray());

	            //start of next track
	            trkOffset = trkChunkEnd; 

	        }

	        return midiFile;

	    }




	    static string ReadAsString(byte[] bytes, int start, int length)
	    {
	    	string str = "";

	    	for(int i = start; i < start + length; i++)
	    	{
	    		str += (char)bytes[i];
	    	}
	    	return str;
	    }

	    static string ReadAsDump(byte[] bytes, int start, int length)
	    {
	        string str = "";

	        for(int i = start; i < start + length; i++)
	        {
	            str += bytes[i].ToString("X2") + " ";
	        }
	        return str;
	    }

	    public static string ReadAsBin(byte[] bytes, int start, int length)
	    {
	        string str = "";

	        for(int i = start; i < start + length; i++)
	        {
	            str += Convert.ToString(bytes[i], 2).PadLeft(8, '0') + " ";
	        }
	        return str;
	    }

	    // Read as Big-Endian 16 bit integer
	    static int ReadAsInt16(byte[] bytes, int start)
	    {
	        //most significant bit in first index
	        return  (bytes[start] << 8) |
	                (bytes[start + 1]);
	    }

	    // Read as Big-Endian 32 bit integer
	    static int ReadAsInt32(byte[] bytes, int start)
	    {
	        //most significant bit in first index
	        return  (bytes[start] << 24) |
	                (bytes[start + 1] << 16) |
	                (bytes[start + 2] << 8) |
	                (bytes[start + 3]);
	    }

	    static int ReadVarInt(byte[] bytes, int start, out int varLength)
	    {
	        varLength = 0;
	        int value = 0;

	        do
	        {
	            value = (value << 7); // shift current value accross 7 bits

	            value += bytes[start + varLength] & 127; //127 equivelent to 01111111 to remove high order bit

	        }
	        while((bytes[start + varLength++ ] & 128) == 128); // 128 equivelent to 100000000
	        // while the highest order bit is 1
	        // additionally increment varlength after comaprison
	        
	        return value;
	    }


	    ///// Evaluate Event types

	    static MidiEvent ParseMidiEvent(byte[] data, int start, out int midiSize)
	    {
	        int runningOffset = 0;
	        if((data[start] & 128) == 128) // if starting bit is set
	        {
	            //update using running status
	            midiRunnningStatus = new MidiRaw.MidiStatus(data[start]);

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
	        int length = ReadVarInt(data, start + 2, out int lenByteSize);

	        // data : 2+var -> end of chunk
	        byte[] metaData = new byte[length];


	        Array.Copy(data, start + 2 + lenByteSize, metaData, 0, length);


	        eventSize = 2 + lenByteSize + length;

	        return new MetaEvent(metaType, metaData);
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