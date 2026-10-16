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

                Texture2D icon = Resources.Load<Texture2D>("MidiIcon");
                ctx.AddObjectToAsset("Midi", midiObject, icon);
                ctx.SetMainObject(midiObject);
                Debug.Log("Imported Midi File: " + ctx.assetPath.Split("/")[^1]);
             }

            
        }

    }



}
