using UnityEngine;

namespace Westhouse.WMidiTools
{
    using System.Collections.Generic;
    using MidiRaw;

	public class MidiProcessor
	{
	    public MidiObject ProcessRaw(SMF midiRaw)
	    {


	    	switch (midiRaw.format)
	    	{
	    		case 1:
	    			return ProcessRawFormat1(midiRaw);
	    		default:
	    			Debug.LogError("Midi Format is unsupported");
	    			return null;

	    	}
	    }



	    private MidiObject ProcessRawFormat1(SMF midiRaw)
	    {
	    	MidiObject midiObj = ScriptableObject.CreateInstance<MidiObject>();


	    	//process metaTrack 
	    	float runningTime = 0; // god i love floating point precision

	    	List<MetaEvent> metaEvents = new();
	    	foreach(MidiRaw.Event rawEvent in midiRaw.midiTracks[0].events)
	    	{
	    		runningTime += (float)rawEvent.deltaTime / midiRaw.timeDiv; // time reletive to quarter notes

	    		switch((rawEvent as MidiRaw.MetaEvent).type)
	    		{
	    			case MetaEventType.SET_TEMPO:
	    				
	    				break;
	    			case MetaEventType.TIME_SIGNATURE:

	    				break;

	    		}

	    	}

	    	NoteTrack[] noteTracks = new NoteTrack[midiRaw.midiTracks.Length - 1]; // ignore first track (Meta)

	    	//process other tracks
	    	for(int i = 1; i < midiRaw.midiTracks.Length; i++)
	    	{
	    		runningTime = 0;

	    		List<NoteEvent> noteEvents = new();
	    		Dictionary<int, NoteEvent> openNoteChannels = new();

	    		foreach(MidiRaw.Event rawEvent in midiRaw.midiTracks[i].events)
	    		{

	    			runningTime += (float)rawEvent.deltaTime / midiRaw.timeDiv; // time reletive to quarter notes

	    			if(rawEvent is not MidiRaw.MidiEvent) //ignore if not midi
	    				continue;

	    			

	    			switch((rawEvent as MidiRaw.MidiEvent).type)
	    			{
	    				case MidiType.NOTE_ON:

	    					// start an open note and assign the start values
	    					MidiRaw.NoteEvent rawNote = rawEvent as MidiRaw.NoteEvent;

	    					NoteEvent openNote = new NoteEvent();
	    					openNote.channel = rawNote.channel;
	    					openNote.velocity = rawNote.velocity / 127;

	    					openNote.startTime = runningTime;

	    					//add to open notes
	    					openNoteChannels.TryAdd(openNote.channel, openNote);
	    					
	    					//add to MO track
	    					noteEvents.Add(openNote);

	    					break;
	    				case MidiType.NOTE_OFF:

	    					//check if a note is open
	    					int channel = (rawEvent as MidiRaw.NoteEvent).channel;
	    					if(openNoteChannels.ContainsKey(channel))
	    					{
	    						openNoteChannels[channel].endTime = runningTime;
	    						openNoteChannels.Remove(channel);
	    					}
	    					break;
	    				// case MidiType.PITCH_WHEEL:

	    				// 	PitchWheelEvent PWEvent = new PitchWheelEvent();

	    				// 	//convert pitch from 14bit +int to float range
	    				// 	PWEvent.pitchVariation = ((rawEvent as MidiRaw.PitchWheelEvent).pitch - 8192) / 8192f; // 8192 -> middle value for 14bit +int

	    				// 	PWEvent.startTime = runningTime;

	    				// 	midiEvents.Add(PWEvent);
	    				// 	break;

	    				//Currently only support for midi notes;
	    			}

	    		}


	    		noteTracks[i-1] = new NoteTrack(noteEvents.ToArray());
	    	}

	    	midiObj.SetTracks(new MetaTrack(metaEvents.ToArray()), noteTracks);
	    	return midiObj;
	    }

	    private float divToQuart(int divs, int divsPerQuart)
	    {
	    	return divs / divsPerQuart;
	    }
	}

}
