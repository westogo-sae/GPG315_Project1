using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;


public class HexDumper : EditorWindow
{
    [MenuItem("Tools/Hex Dumper")]
    private static void ShowWindow()
    {
    	var window = GetWindow<HexDumper>();
    	window.Show();
    }

    string filePath = "Assets/";
    string outputPath = "Assets/HexDump.txt";
    int blockWidth = 5;
    bool dumpToFile;
    string[] formatOptions = {"Binary", "Decimal", "Hexedecimal"};
    int formatIndex = 2;
    private void OnGUI()
    {

    	GUILayout.Label("File Path");
    	filePath = GUILayout.TextField(filePath);

    	GUILayout.BeginHorizontal();
    	GUILayout.Label("Dump To File");
    	dumpToFile = EditorGUILayout.Toggle(dumpToFile);
    	GUILayout.EndHorizontal();

    	if(dumpToFile)
    	{
	    	GUILayout.Label("Output Path");
	    	outputPath = GUILayout.TextField(outputPath);
    	}


    	GUILayout.Label("Block Width");
    	blockWidth = int.Parse(GUILayout.TextField(blockWidth.ToString()));

    	GUILayout.BeginHorizontal();
    	GUILayout.Label("Dump Format");
    	formatIndex = EditorGUILayout.Popup(formatIndex, formatOptions);
    	GUILayout.EndHorizontal();

    	
    	if(GUILayout.Button("Dump"))
    	{
    		if(outputPath == null || outputPath == "")
    		{
    			Debug.LogError("Output Path cannot be Empty!");
    		}

    		byte[] data = null;

    		try
    		{
    			Debug.Log("Reading File: " + filePath);
    			data = File.ReadAllBytes(filePath);
    			Debug.Log("File loaded");
    		}
    		catch(Exception e)
    		{
    			Debug.LogError("Error reading File: " + e);
    		}

    		if(data != null)
    		{	
    			Debug.Log("Writing File");

    			if(dumpToFile)
    			{
	    			try
	    			{
	    				File.WriteAllText(outputPath, "Hex Dump for file: " + filePath);
	    			}
					catch(Exception e)
					{
						Debug.LogError("Error Writing to File: " + e);
						return;
					}
					Debug.Log("Writing to File: " + outputPath);
				}else
					Debug.Log("Writing data");

    			string[] lines = new string[((int)(data.Length / blockWidth)) + 1];


    			switch(formatIndex)
    			{
    				case 0:
		    			for(int i = 0; i < data.Length; i++)
		    			{	

		    				lines[(int)(i / blockWidth) ] += (Convert.ToString(data[i], 2).PadLeft(8, '0') + " ");
		    			}
		    		break;
		    		case 1:
		    			for(int i = 0; i < data.Length; i++)
		    			{	

		    				lines[(int)(i / blockWidth) ] += (data[i].ToString("D3") + " ");
		    			}
		    		break;
		    		case 2:
		    			for(int i = 0; i < data.Length; i++)
		    			{	

		    				lines[(int)(i / blockWidth) ] += (data[i].ToString("X2") + " ");
		    			}
		    		break;
    			}

    			if(dumpToFile)
    			{
    				File.WriteAllLines(outputPath, lines);
    			}else
    			{
    				foreach(var line in lines)
    				{
    					Debug.Log(line);
    				}
    			}

    			Debug.Log("Dump Completed");
    		}

    	}

    }

}
