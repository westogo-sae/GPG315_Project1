using UnityEngine;
using UnityEditor.AssetImporters;

namespace Westhouse.WMidiTools
{
    using MidiRaw;

    [ScriptedImporter(1, "mid")]
    public class MidiImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {

            SMF rawMidi = MidiReader.ReadMidiFile(ctx.assetPath);
            rawMidi = MidiReader.ReadMidiFile(ctx.assetPath);

            ctx.AddObjectToAsset("Midi", rawMidi);
            
            Debug.LogWarning("Imported Midi File: " + ctx.assetPath.Split("/")[^1]);      
        }

    }



}
