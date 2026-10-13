using UnityEngine;

namespace Westhouse.WMidiTools
{
	public class MidiObject : ScriptableObject
	{
		public float bpm = 120;
		public int timeSignatureNum = 4;
		public int timeSignatureDenom = 4;
		public int timeSignature {get {return timeSignatureNum / timeSignatureDenom;}}

		public MetaTrack metaTrack {get; private set;}
		public NoteTrack[] noteTracks {get; private set;}

		public void SetTracks(MetaTrack meta, NoteTrack[] notes)
		{
			metaTrack = meta;
			noteTracks = notes;
		}

	}

	public class MidiEvent
	{
		/// floating point note time in quarter notes
		public float startTime;

		// possible delagate setup with injection
		// protected MidiObject midiObject;

		// public void InjectMO(MidiObject MO)
		// {
		// 	midiObject = MO;
		// }
	}

	public class MetaTrack
	{
		public MetaEvent[] events;

		public MetaTrack(MetaEvent[] metaEvents)
		{
			events = metaEvents;
		}
	}

	public class MetaEvent : MidiEvent
	{

	}


	public class NoteTrack
	{
		public NoteEvent[] events;

		public NoteTrack(NoteEvent[] noteEvents)
		{
			events = noteEvents;
		}
	}

	public class NoteEvent : MidiEvent
	{
		public int channel;
		/// velocity of note from 0 to 1
		public float velocity;
		public float endTime;
		public float length {get {return endTime - startTime;}}
	}

	public class PitchWheelEvent : MidiEvent
	{
		//pitch variation from -1 to 1
		public float pitchVariation;
	}


	namespace MidiRaw
	{
		//////////////////////////////
		////////// Generic //////////
		//////////////////////////////

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

		//////////////////////////////
		//////// Meta Events//////////
		//////////////////////////////

		[System.Serializable]
		public class MetaEvent : Event
		{
			public MetaEventType type;
			public byte[] rawData;

			protected MetaEvent(MetaEventType metaType, byte[] metaData)
			{
				type = metaType;
				rawData = metaData;
			}

			public static MetaEvent Construct(MetaEventType metaType, byte[] metaData)
			{
				MetaEvent metaEvent = null;
				switch(metaType)
				{
					case MetaEventType.SET_TEMPO:

						metaEvent = new SetTempoEvent(metaType, metaData);
						break;
					case MetaEventType.TIME_SIGNATURE:

						metaEvent = new SetTimeSignatureEvent(metaType, metaData);
						break;
					default:
						//raw meta event
						metaEvent = new MetaEvent(metaType, metaData);
						break;
				}

				if(metaEvent != null)
				{
					metaEvent.type = metaType;
					metaEvent.rawData = metaData;
				}
				return metaEvent;
			}
		}

        public class SetTempoEvent : MetaEvent
        {
        	///microseconds per quarternote
        	private int tempo;

            public SetTempoEvent(MetaEventType metaType, byte[] metaData) : base(metaType, metaData)
            {
            	tempo = metaData[0] << 16 |
            			metaData[1] << 8  |
            			metaData[2];
            }
        }
			
		public class SetTimeSignatureEvent : MetaEvent
		{
			public int numerator;
			public int denominatorPow;
			public int clocksPerClick;
			public int QuartSpec;

			public SetTimeSignatureEvent(MetaEventType metaType, byte[] metaData) : base(metaType, metaData)
            {
            	numerator = metaData[0];
            	denominatorPow = metaData[1];
            	clocksPerClick = metaData[2];
            	QuartSpec = metaData[3];
            }
		}

		//////////////////////////////
		//////// Sysex Events/////////
		//////////////////////////////

		[System.Serializable]
		public class SysexEvent : Event
		{
			byte[] data;

			public SysexEvent(byte[] sysData)
			{
				data = sysData;
			}
		}

		//////////////////////////////
		//////// Midi Events//////////
		//////////////////////////////

		[System.Serializable]
		public abstract class MidiEvent : Event
		{
			public int channel;
			public MidiType type;

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
					mEvent.type = status.type;
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