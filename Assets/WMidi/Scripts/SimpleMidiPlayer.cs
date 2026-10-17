using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Westhouse.WMidiTools;

public class SimpleMidiPlayer : MonoBehaviour
{
    //constants
    //approximate frequeincy multiplier between 1 semitone (12th root of 2)
    const float semitoneFactor = 1.0594631f;

    ///<summary>Do Not Use Directly!!!</summary>
    [SerializeField] private MidiObject midi;
    public bool playOnAwake = true;

    private MidiObject midiObject;

    [SerializeField]
    private AudioClip noteClip;
    [Range(-4, 4)]
    [SerializeField] int transposeOctaves = 0;
    [Tooltip("Transpose the audio Clip in semitones(Assumed to be C4 by default)")]
    [Range(-11, 11)]
    [SerializeField] int transposeSemitones = 0;
    [Range(0.25f, 4f)]
    public float playbackSpeed = 1f;
    public bool loop = false;
    [Range(0.25f, 4f)]
    public float volume = 1;
    [Tooltip("How long the note AudioSource remains active for, Shorter times are more efficient")]
    [SerializeField] float noteLife = 1.5f;

    void Start()
    {
        // midi.tracks[1].events[0].Call();
        if(playOnAwake && midi != null) Play();
    }

    public Action OnSongComplete;

    public void SetSong(MidiObject midiSong)
    {
        midi = midiSong;

        runningTime = 0;
        playing = false; // stop playing current song
    }

    private float runningTime;
    private bool playing = false;
    private int[] nextEvents;
    public void Play()
    {
        //create a unique copy in order to update values without damaging original file;
        //also allows for midi Object to be updated at runtime without breaking anything;
        midiObject = Instantiate(midi);
        midiObject.OnNoteEventCalled += NotePlayedEvent;

        playing = true;
        runningTime = 0; // will force restart if already playing

        nextEvents = new int[midiObject.tracks.Length];
        for(int i = 0; i < nextEvents.Length; i++) nextEvents[i] = 0;

    }

    private void SongComplete()
    {
        Debug.Log("midi track complete: " + midiObject.name);
        playing = false;
        midiObject.OnNoteEventCalled -= NotePlayedEvent; // just in case
        Destroy(midiObject); // uninstantiate object;


        if(loop) Play();
        else OnSongComplete?.Invoke(); // looping does not count as song completing

    }

    void Update()
    {
        if(playing == true)
        {
            float deltaBeatTime = midiObject.tempo * Time.deltaTime * playbackSpeed;


            //reached the end of the song
            if(IsSongComplete())
            {
                SongComplete();
            }else
            {   
                //all tracks play simultaniously
                for(int i = 0; i < midiObject.tracks.Length; i++)
                {
                    MOTrack track = midiObject.tracks[i];

                    // while not at the end of track and any notes ocuur within deltaBeatTime
                    while (nextEvents[i] < track.events.Length && track.events[nextEvents[i]].time < runningTime + deltaBeatTime)
                    {

                        MOEvent nextEvent = track.events[nextEvents[i]]; 

                        nextEvent.Call();
                        nextEvents[i] ++;

                    } 

                }
            }
            runningTime += deltaBeatTime;
        }
    }

    bool IsSongComplete()
    {
        bool songComplete = true;
        for(int i = 0; i < nextEvents.Length; i++)
        {
            //if any of the tracks are still playing
            if(nextEvents[i] < midiObject.tracks[i].events.Length) songComplete = false;
        }
        return songComplete;
    }

    private void NotePlayedEvent(NoteEvent noteEvent)
    {
        float delay = (noteEvent.time - runningTime) / midiObject.tempo; //convert to seconds
        PlayNote(noteEvent.note, noteEvent.velocity, delay);
    }

    private void PlayNote(int midiNote, float velocity, float delay)
    {
        //midi note reletive to C4;
        int noteRel = midiNote - 60;

        AudioSource source = AudioSourcePooler.Instantiate(transform);

        //destroy note after lifeTime
        StartCoroutine(DestroySource(source, noteLife));

        source.clip = noteClip;
        source.pitch = TransposeValue(1, transposeOctaves * 12 + transposeSemitones + noteRel);
        source.volume = velocity * volume;
        source.PlayDelayed(delay);
    }

    // private IEnumerator PlayDelayed(AudioSource noteSource, float delay)
    // {
    //     yield return new WaitForSeconds(delay);
    //     noteSource.Play();
    // }

    private IEnumerator DestroySource(AudioSource noteSource, float life)
    {
        yield return new WaitForSeconds(life);
        AudioSourcePooler.Destroy(noteSource);
    }

    private float TransposeValue(float value, int semitones)
    {
        return value * Mathf.Pow(semitoneFactor, semitones);
    }


    private static class AudioSourcePooler
    {

        static List<AudioSource> pool;

        public static AudioSource Instantiate(Transform parent)
        {
            if(pool == null) pool = new();
            AudioSource source;

            if(pool.Count > 0)
            {
                source = pool[0];
                pool.RemoveAt(0);
            }else
            {
                GameObject obj = new GameObject("note player", typeof(AudioSource));
                obj.transform.parent = parent;
                source = obj.GetComponent<AudioSource>();

                source.volume = 1;
                source.playOnAwake = false;
            }

            source.gameObject.SetActive(true);
            return source;
        }

        public static void Destroy(AudioSource source)
        {
            pool.Add(source);
            source.gameObject.SetActive(false);
        }
    }

}
