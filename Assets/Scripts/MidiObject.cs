using System;
using UnityEngine;

namespace Westhouse.WMidiTools
{

	//////////////////////////////
	////////// Generic //////////
	//////////////////////////////

	public class MidiObject : ScriptableObject
	{
		//processing vars (used once on import)
		public int timeDiv;
		public int format;


		/////runtime vars (can vary over playback)

		///<summary>>beats per second</summary>
		public float tempo = 2f; //120 bpm
		public int timeSignatureNum = 4;
		public int timeSignatureDenom = 4;
		public int timeSignature {get {return timeSignatureNum / timeSignatureDenom;}}
		///<summary> state of pitch wheel from -1 to 1 </summary>
		public float pitchVariation = 0;

		[SerializeReference]
		public MOTrack[] tracks;


		///<summary> Invoked when a note on event was called</summary>
		public Action<NoteEvent> OnNoteEventCalled;
		///<summary> Invoked when a note off event was called</summary>
		public Action<NoteEvent> OnNoteEventCancelled;

		private void OnEnable()
		{	
			if(tracks == null) return;
			//Inject into events
			foreach(MOTrack track in tracks)
			{
				foreach(MOEvent ev in track.events)
				{
					ev.InjectMidiObject(this);
				}
			}	
		}	

	}



	[System.Serializable]
	public class MOTrack
	{
		[SerializeReference]
		public MOEvent[] events;

		public MOTrack(MOEvent[] trackEvents)
		{
			events = trackEvents;
			Debug.Log(events);
		}
	}

	[System.Serializable]
	public  class MOEvent
	{
		/// the time of the event in quarter notes
		public float time;
		[SerializeReference]
		protected MidiObject midiParent;

		public void InjectMidiObject(MidiObject MO)
		{
			midiParent = MO;
		}

		public void SetTimeFromRaw(long divTime, int divsPerQuart)
		{
			time = (float)divTime / divsPerQuart;
		}

		public virtual void Call()
		{
			Debug.LogWarning("Empty Midi Object Events Should not be called!!");
		}
	}

	//////////////////////////////
	//////// Meta Events//////////
	//////////////////////////////


	[System.Serializable]
	public class MetaEvent : MOEvent
	{
		public MetaEventType type;
		public byte[] rawData;
		

		protected MetaEvent(MetaEventType metaType, byte[] metaData)
		{
			type = metaType;
			rawData = metaData;
		}

		//static MetaEvent Factory
		public static MetaEvent Construct(MetaEventType metaType, byte[] metaData)
		{
			MetaEvent metaEvent = null;
			switch(metaType)
			{
				case MetaEventType.TEXT_EVENT | MetaEventType.LYRIC | MetaEventType.COPYRIGHT_NOTICE | MetaEventType.CUE_POINT:

					metaEvent = new TextEvent(metaType, metaData);
					break;
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

		public override void Call()
		{
			Debug.LogWarning("Unrecognised Meta Event triggered");
		}

		public override string ToString()
        {
            return $"Meta Event | time: {time} | type: {Enum.GetName(typeof(MetaEventType), type)}";
        }
	}


	[System.Serializable]
    public class TextEvent : MetaEvent
    {
    	public string text;

        public TextEvent(MetaEventType metaType, byte[] metaData) : base(metaType, metaData)
        {
        	text = ByteReader.ReadAsString(metaData, 0, metaData.Length);
        }

        public override void Call()
        {

			Debug.Log($"[{time}] : {text}");
		}
    }


	[System.Serializable]
    public class SetTempoEvent : MetaEvent
    {
    	///microseconds per quarternote
    	[SerializeReference]
    	private int tempo;

        public SetTempoEvent(MetaEventType metaType, byte[] metaData) : base(metaType, metaData)
        {
        	tempo = metaData[0] << 16 |
        			metaData[1] << 8  |
        			metaData[2];
        }

        public override void Call()
        {
        	midiParent.tempo = 1000000f / tempo; // convert from microseconds per beat to beats per second
        }
    }
	

	[System.Serializable]
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

        public override void Call()
        {
			midiParent.timeSignatureNum = numerator;
			midiParent.timeSignatureDenom = (int)Math.Pow(2, denominatorPow);
		}
	}


	//////////////////////////////
	//////// Sysex Events/////////
	//////////////////////////////


	[System.Serializable]
	public class SysexEvent : MOEvent
	{
		byte[] data;

		public SysexEvent(byte[] sysData)
		{
			data = sysData;
		}

		public override void Call(){
			Debug.Log("Unrecognised Sysex Event triggered");
		}
	}

	//////////////////////////////
	//////// Midi Events//////////
	//////////////////////////////


	[System.Serializable]
	public class MidiEvent : MOEvent
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

        public override string ToString()
        {
            return $"Midi Event | time: {time} | type: {Enum.GetName(typeof(MidiType), type)}";
        }
	}

	[System.Serializable]
	public class NoteEvent : MidiEvent
	{
		public bool noteOn;

		public int note;
		public float velocity;

		public NoteEvent(bool NoteOn, byte[] data)
		{
			noteOn = NoteOn;

			note  = data[0]; // No need to remove highest bit, should be 0
			velocity = (data[1] / 127f);  //convert to float 0 - 1
		}

        public override void Call()
        {
            if(noteOn) midiParent.OnNoteEventCalled?.Invoke(this);
            else midiParent.OnNoteEventCancelled?.Invoke(this);
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

        public override void Call()
        {
            Debug.LogWarning("Poly Aftertouch is currently unsupported");
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

        public override void Call()
        {
            Debug.LogWarning("Control Changes are currently unsupported");
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

        public override void Call()
        {
            Debug.LogWarning("Program Change is currently unsupported");
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

        public override void Call()
        {
            Debug.LogWarning("Channel Aftertouch is currently unsupported");
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

        public override void Call()
        {
        	//convert from 14bit int to floating point value -1 to 1
        	midiParent.pitchVariation = (pitch - 8192) / 8192f; // 8192f >> centerpoint of 14bit int
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
// crunchy nut.