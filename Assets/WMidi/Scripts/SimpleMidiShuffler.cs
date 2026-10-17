using System.Collections.Generic;
using UnityEngine;
using Westhouse.WMidiTools;

[RequireComponent(typeof(SimpleMidiPlayer))]
public class SimpleMidiShuffler : MonoBehaviour
{

    [SerializeField] bool PlayOnAwake = true;
    [SerializeField] bool loop;

    [SerializeField] List<MidiObject> shuffleList;

    private SimpleMidiPlayer player;

    private void Awake()
    {
        player = GetComponent<SimpleMidiPlayer>();
        player.OnSongComplete += OnSongComplete;
        player.playOnAwake = false;

        StartShuffle();
    }

    int currentSong;
    public void StartShuffle()
    {
        ShuffleList(ref shuffleList);

        currentSong = 0;

        if(shuffleList.Count > 0)
        {
            player.SetSong(shuffleList[0]);
            player.Play();
        }
    }

    private void OnSongComplete()
    {
        currentSong++;

        if(currentSong >= shuffleList.Count) ShuffleComplete();
        else 
        {
            player.loop = false; // disable to avoid issues
            player.SetSong(shuffleList[currentSong]);
            player.Play();
        }
        
    }

    private void ShuffleComplete()
    {
        if(loop) StartShuffle();
        else Debug.Log("Shuffle Complete");
    }



    void ShuffleList<T>(ref List<T> list)
    {
        List<T> tempList = new();
        int count = list.Count;

        for(int i = 0; i < count; i++) 
        {
            int remove = Random.Range(0, list.Count);
            tempList.Add(list[remove]);
            list.RemoveAt(remove);
        }

        list = tempList;
    }
}
