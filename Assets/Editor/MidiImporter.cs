using UnityEngine;
using UnityEditor.AssetImporters;

namespace Westhouse.WMidiTools
{

    [ScriptedImporter(1, "mid")]
    public class MidiImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {

            MidiObject midiObject = ScriptableObject.CreateInstance<MidiObject>();

             if(MidiReader.ReadMidiFileToObj(ctx.assetPath, ref midiObject))
             {

                ctx.AddObjectToAsset("Midi", midiObject);
                Debug.LogWarning("Imported Midi File: " + ctx.assetPath.Split("/")[^1]);      
             }

            
        }

    }



}
