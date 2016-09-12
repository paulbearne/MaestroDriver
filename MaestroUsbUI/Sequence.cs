/*
 * 
 * modified to avoid anything registry which is not supported in iot yet
 * we now create a local file to hold frame info based on idName
 */

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.IO;
using Windows.Storage;
using System.Collections.ObjectModel;
using System.Xml;
using Pololu.Usc.Sequencer;
using Windows.Data.Xml.Dom;
using System.Xml.Linq;

// TODO: stop suppressing error 1591 (in the project properties) and add XML comments for everything in this assembly

namespace Pololu.Usc.Sequencer
{
    class sequenceFrame
    {
        string name;
        UInt16 duration;
        string targets;
    }

    class sequenceItem
    {
        string name;
        uint framecount;
        sequenceFrame[] frames;
    }

    public class Sequence
    {
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private ApplicationDataCompositeValue composite = new Windows.Storage.ApplicationDataCompositeValue();
        
        
        public string name;
        public List<Frame> frames = new List<Frame>();

        public Sequence(string name) { this.name = name; }

    	public Sequence() { }

        /// <summary>
        /// Saves sequences in the registry in the "sequences" subkey of the given key.
        /// </summary>
        /// <param name="list">A list of sequences to save in key/sequences in the registry.</param>
        /// <param name="parentKey">A key that has been opened so as to allow editing.</param>
        public static void saveSequencesInRegistry(IList<Sequence> list, BinaryWriter writer)
        {
            // start sequences element

            //  writer.WriteStartElement("sequences");
            writer.Write((Int32)list.Count);


           
            for (int sequenceIndex = 0; sequenceIndex < list.Count; sequenceIndex++)
            {
               
                Sequence sequence = list[sequenceIndex];
                writer.Write(sequenceIndex.ToString("d2"));
                writer.Write((Int32)sequence.frames.Count);

                for (int frameIndex = 0; frameIndex < sequence.frames.Count; frameIndex++)
                {
                    Frame frame = sequence.frames[frameIndex];
                    string frameKeyName = frameIndex.ToString("d4"); // e.g. 0001
                    //RegistryKey frameKey = sequenceKey.CreateSubKey(frameKeyName);
                    writer.Write(frameKeyName);
                    writer.Write((UInt16)frame.length_ms);
                    writer.Write((Int32)frame.targets.Length);
                    for (uint targetindex = 0; targetindex < frame.targets.Length; targetindex++)
                    {
                        writer.Write((UInt16)frame.targets[targetindex]);
                    }
                }
                
            }
           
            // sequencesKey.Close();
           
       //     XmlWriter writer = XmlWriter.Create(File.Create(fname));
        //    writer.Settings.Indent = true;
        //    doc.WriteContentTo(writer);
        //    writer.Flush();
            
        }

        public static List<Sequence> readSequencesFromRegistry(BinaryReader reader,  byte servoCount)
        {
            List<Sequence> sequences = new List<Sequence>();
            int sequencecount = reader.ReadInt32();
            for (int sequenceIndex = 0; sequenceIndex < sequencecount; sequenceIndex++)
            {

                Sequence sequence = new Sequence();
                sequence.name = reader.ReadString();
                int framecount = reader.ReadInt32();

                for (int frameIndex = 0; frameIndex < framecount; frameIndex++)
                {
                    Frame frame = new Frame();
                    frame.name = reader.ReadString();
                    frame.length_ms = reader.ReadUInt16();
                    int targetCount = reader.ReadInt32();
                    frame.targets = new ushort[targetCount];
                    for (int targetIndex = 0; targetIndex < targetCount; targetIndex++)
                    {
                        frame.targets[targetIndex] = reader.ReadUInt16();
                    }
                    sequence.frames.Add(frame);
                }
                sequences.Add(sequence);

            }
            return sequences;
        }



        /// <summary>
        /// Generates the script for this sequence - just the code for calling the frame functions.
        /// Adds any channel lists for required frame commands to the needed_channel_lists array.
        /// </summary>
        private string generateScript(List<byte> enabled_channels, List<List<byte>> needed_channel_lists)
        {
            string script = "";
            Frame last_frame = null; // need to initialize to avoid compiler error

            foreach (Frame frame in frames)
            {
                List<byte> needed_channels = new List<byte>();
                List<ushort> changed_targets = new List<ushort>();

                // The first time, we need to set all channels.
                // Otherwise, set needed_channels to a list of just the
                // channels that change.  Note that non-enabled channels
                // should never change, but we skip them anyway.
                foreach (byte channel in enabled_channels)
                {
                    if (last_frame == null || frame[channel] != last_frame[channel])
                    {
                        needed_channels.Add(channel);
                        changed_targets.Add(frame[channel]);
                    }
                }

                // set last_targets
                last_frame = frame;

                if (changed_targets.Count != 0)
                {
                    // search for an existing list that matches
                    bool found = false;
                    foreach (List<byte> existing_list in needed_channel_lists)
                    {
                        if (existing_list.Count != needed_channels.Count)
                            continue;

                        for (int i = 0; i < existing_list.Count; i++)
                        {
                            if (existing_list[i] != needed_channels[i])
                                goto does_not_match;
                        }

                        // every one matched, so the list matches
                        found = true;
                        break;

                        // this one does not match, so continue
                    does_not_match:
                        continue;
                    }

                    if (!found)
                    {
                        // add the set of channels we need this time to the list
                        needed_channel_lists.Add(needed_channels);
                    }
                }

                // actually add the code for this frame
                script += "  "; // indent
                script += frame.length_ms + " ";

                if (needed_channels.Count == 0)
                {
                    // no channels changed - just delay
                    script += "delay";
                }
                else
                {
                    byte targetsOnThisLine = 0;
                    foreach (ushort target in changed_targets)
                    {
                        // If there are already 6 targets on this line, then wrap,
                        // but don't let the call to the frame subroutine be on its
                        // own line.
                        if (targetsOnThisLine == 6)
                        {
                            script += "\n  ";
                            targetsOnThisLine = 0;
                        }
                        targetsOnThisLine++;

                        script += target + " ";
                    }
                    script += getFrameSubroutineName(needed_channels);
                }
                script += " # " + frame.name + "\n";
            }
            return script;
        }

        /// <summary>
        /// Generates the name of the frame subroutine that sets all of the specified channels.
        /// It will be of the form frame_1_3_4_6..8.
        /// </summary>
        /// <param name="channels">A non-empty list of channels, in ascending numeric order.</param>
        /// <returns></returns>
        public static string getFrameSubroutineName(List<byte> channels)
        {
            if (channels.Count == 0)
            {
                throw new Exception("getFrameSubroutineName: Expected channels list to be non-empty.");
            }

            string name = "frame_";

            int index = 0;
            while (true)
            {
                // Each iteration of this loop will identify one block of consecutive channels
                // starting at channel[index], add a representation of that block to the name,
                // (e.g. "1", "2_3", or "4..6"), and increment index to point to the next block
                // of consecutive channels.

                // Determine where the block ends.  blockEnd is the exclusive upper limit of the array index.
                int blockEnd = index + 1;
                while(blockEnd < channels.Count && channels[blockEnd - 1] + 1 == channels[blockEnd])
                {
                    blockEnd ++ ;
                }

                // startChannel and endChannel are inclusive limits of the channel number.
                byte startChannel = channels[index];
                byte endChannel = channels[blockEnd-1];

                if (endChannel == startChannel)
                {
                    // This block contains just one channel.
                    name += startChannel;
                }
                else if (endChannel == startChannel + 1)
                {
                    // This block contains exactly two channels.  Use an
                    // underscore because it is more compact than "..".
                    name += startChannel + "_" + endChannel;
                }
                else
                {
                    // This block contains three or more channels.
                    name += startChannel + ".." + endChannel;
                }
                
                if (blockEnd == channels.Count)
                {
                    return name;
                }

                // Prepare to process the next block.
                index = blockEnd;
                name += "_";
            }
        }

        /// <summary>
        /// Generates the subroutine that sets the specified channels, then
        /// delays.  The channels are expected to be on the stack in the same
        /// order as the channels list - e.g. the command will be something like
        /// 500 1 2 3 frame_1..3.
        /// </summary>
        public static string generateFrameSubroutine(List<byte> channels)
        {
            string script = "sub " + getFrameSubroutineName(channels) + "\n";
            for(int i = channels.Count - 1; i >= 0; i--)
            {
                script += "  " + channels[i].ToString() + " servo\n";
            }
            script += "  delay\n";
            script += "  return\n";
            return script;
        }

        public string generateLoopedScript(List<byte> enabled_channels)
        {
            List<List<byte>> needed_channel_lists = new List<List<byte>>();

            string script = "# " + name + "\n" + "begin\n";
            script += generateScript(enabled_channels, needed_channel_lists);
            script += "repeat\n\n";

            foreach (List<byte> needed_channels in needed_channel_lists)
            {
                script += generateFrameSubroutine(needed_channels) + "\n";
            }
            return script;
        }

        public string generateSubroutine(List<byte> enabled_channels, List<List<byte>> needed_channel_lists)
        {
            string nice_name = name;

            // turn spaces into underscores
            var exp = new Regex(@"\s+");
            nice_name = exp.Replace(nice_name, "_");

            // get rid of unusual characters
            exp = new Regex(@"[^a-z0-9_]", RegexOptions.IgnoreCase);
            nice_name = exp.Replace(nice_name, "");

            string script = "# " + name + "\n" + "sub " + nice_name + "\n";
            script += generateScript(enabled_channels, needed_channel_lists);
            script += "  return\n";
            return script;
        }

        public static string generateSubroutineList(List<byte> enabled_channels, List<Sequence> sequences)
        {
            List<List<byte>> needed_channel_lists = new List<List<byte>>();
            string script = "";

            foreach (Sequence sequence in sequences)
            {
                script += sequence.generateSubroutine(enabled_channels, needed_channel_lists);
            }

            foreach (var channel_list in needed_channel_lists)
            {
                script += "\n"+Sequence.generateFrameSubroutine(channel_list);
            }
            return script;
        }

        /// <summary>
        /// We want the frame names to be sorted correctly when retrieved from the registry.
        /// This means converting the names (e.g. "0013") to integers.
        /// </summary>
        /// <remarks>http://msdn.microsoft.com/en-us/library/system.collections.icomparer.compare.aspx</remarks>
        private class FrameKeyNameComparer : IComparer<String>
        {
            public int Compare(string x, string y)
            {
                try
                {
                    return ushort.Parse(x) - ushort.Parse(y);
                }
                catch
                {
                    return 0;
                }
            }
        }
    }
}

// Local Variables: **
// mode: java **
// c-basic-offset: 4 **
// tab-width: 4 **
// indent-tabs-mode: nil **
// end: **
