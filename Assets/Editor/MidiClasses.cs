using UnityEngine;

namespace Westhouse.WMidiTools
{
	public class MidiObject : ScriptableObject
	{
		public float bpm = 120;
		public int timeSignatureNum = 4;
		public int timeSignatureDenom = 4;
		public int timeSignature {get {return timeSignatureNum / timeSignatureDenom;}}

		MetaTrack metaTrack;
		NoteTrack[] noteTracks;

	}

	public class MidiEvent
	{
		public float startTime;
	}

	public class MetaTrack
	{
		public MetaEvent[] events;
	}

	public class MetaEvent : MidiEvent
	{

	}


	public class NoteTrack
	{
		public NoteEvent[] events;
	}

	public class NoteEvent : MidiEvent
	{
		public float length;
		public float endTime {get {return startTime + length;}}
	}




	namespace MidiRaw
	{

		public class SMF : ScriptableObject
		{
			public int format;
			public int timeDiv;
			public EventTrack[] midiTracks;
		}

		[System.Serializable]
		public class EventTrack
		{
			public Event[] events;

			public EventTrack(Event[] trackEvents)
			{
				events = trackEvents;
			}
		}

		[System.Serializable]
		public class Event
		{
			public int deltaTime;
		}

		[System.Serializable]
		public class MetaEvent : Event
		{
			MetaEventType type;
			byte[] data;

			public MetaEvent(MetaEventType metaType, byte[] metaData)
			{
				type = metaType;
				data = metaData;
			}
		}


		[System.Serializable]
		public class SysexEvent : Event
		{
			byte[] data;

			public SysexEvent(byte[] sysData)
			{
				data = sysData;
			}
		}


		[System.Serializable]
		public abstract class MidiEvent : Event
		{
			public int channel;

			// static Midi Factory
			public static MidiEvent Construct(MidiStatus status, byte[] data)
			{
				MidiEvent mEvent = null;
				switch(status.type)
				{
					case MidiType.NOTE_OFF:
						mEvent = new NoteEvent(false, data);
					break;

					case MidiType.NOTE_ON:
						mEvent = new NoteEvent(true, data);
					break;

					case MidiType.POLY_AFTERTOUCH:
						mEvent = new PolyATEvent(data);
					break;

					case MidiType.CTRL_CHANGE:
						mEvent = new CtrlChangeEvent(data);
					break;

					case MidiType.PROG_CHANGE:
						mEvent = new ProgChangeEvent(data[0]);
					break;

					case MidiType.CHANNEL_AFTERTOUCH:
						mEvent = new ChannelATEvent(data[0]);
					break;

					case MidiType.PITCH_WHEEL:
						mEvent = new PitchWheelEvent(data);
					break;
				}

				if(mEvent != null)
				{
					mEvent.channel = status.channel;
				}

				return mEvent;
			}
		}

		[System.Serializable]
		public class NoteEvent : MidiEvent
		{
			public bool noteOn;

			public int note;
			public int velocity;

			public NoteEvent(bool NoteOn, byte[] data)
			{
				noteOn = NoteOn;

				note  = data[0]; // No need to remove highest bit, should be 0
				velocity = data[1]; 
			}
		}
		[System.Serializable]
		public class PolyATEvent : MidiEvent
		{
			public int note;
			public int pressure;

			public PolyATEvent(byte[] data)
			{

				note  = data[0]; 
				pressure = data[1]; 
			}
		}
		[System.Serializable]
		public class CtrlChangeEvent : MidiEvent
		{
			public int controller;
			public byte eventData;

			public CtrlChangeEvent(byte[] data)
			{

				controller  = data[0]; 
				eventData = data[1]; 
			}
		}
		[System.Serializable]
		public class ProgChangeEvent : MidiEvent
		{
			public int program;

			public ProgChangeEvent(byte data)
			{
				program = data; 
			}
		}
		[System.Serializable]
		public class ChannelATEvent : MidiEvent
		{
			public int pressure;

			public ChannelATEvent(byte data)
			{
				pressure = data;
			}
		}
		[System.Serializable]
		public class PitchWheelEvent : MidiEvent
		{
			public int pitch;

			public PitchWheelEvent(byte[] data)
			{
				pitch = data[0] << 7 
					  |	data[1];
			}
		}

		[System.Serializable]
		public class MidiStatus
	    {
	        public MidiType type;
	        public int channel;
	        public int dataBytesCount;

	        public MidiStatus(byte midiStatusByte)
	        {
	            type = (MidiType)((midiStatusByte >> 4) & 7);
	            channel  = (midiStatusByte & 15);

	            dataBytesCount = (type == MidiType.PROG_CHANGE || type == MidiType.CHANNEL_AFTERTOUCH) ? 1 : 2; // type 4/5 only use 1 byte for data
	        }
	    }



	    public enum MidiType
	    {
	        NOTE_OFF,
	        NOTE_ON,
	        POLY_AFTERTOUCH,
	        CTRL_CHANGE,
	        PROG_CHANGE,
	        CHANNEL_AFTERTOUCH,
	        PITCH_WHEEL
	    }

	    public enum MetaEventType : byte
		{
			SEQUENCE_NUMBER = 0x00,
			TEXT_EVENT = 0x01,
			COPYRIGHT_NOTICE = 0x02,
			SEQUENCE_NAME = 0x03,
			INSTRUMENT = 0x04,
			LYRIC = 0x05,
			MARKER = 0x06,
			CUE_POINT = 0x07,
			MIDI_CHANNEL_PREFIX = 0x20,
			END_OF_TRACK = 0x2F,
			SET_TEMPO = 0x51,
			SMPTE_OFFSET = 0x54,
			TIME_SIGNATURE = 0x58,
			KEY_SIGNATURE = 0x59,
			SEQUENCER_SPECIFIC_META_DATA = 0x7F,
		}

	    

	}
}