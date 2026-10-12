using UnityEngine;
using UnityEditor;

public class MidiProcessingMenu : EditorWindow
{	
	string midiFile;
    public static void ShowWindow(string midiName)
    {
    	var window = GetWindow<MidiProcessingMenu>("Please Wait");

    	(window as MidiProcessingMenu).midiFile = midiName;
    	//window.maxSize = new Vector2(300, 250);
        window.Show();
        window.position = new Rect(Screen.width * 0.5f - 150, Screen.height * 0.5f - 125, 300, 250);
        window.Repaint();
    }

    private void OnGUI()
    {
    	GUILayout.Label($"Processing Midi File: {midiFile}");
    }

    public static void CloseWindow()
    {
    	GetWindow<MidiProcessingMenu>().Close();
    }
}
