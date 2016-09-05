/*
 * removed cliboard functions as not supported yet on iot 
 * 
 */



using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;

namespace Pololu.Usc.Sequencer
{
    
    public class Frame
    {
        public string name;
        private ushort[] privateTargets;
    	public ushort length_ms;

        /// <summary>
        /// Gets the target of the given channel.
        /// </summary>
        /// <remarks>
        /// By retreiving targets this way, we protect the application against
        /// any kind of case where the Frame object might have fewer targets
        /// than expected.
        /// </remarks>
        [System.Runtime.CompilerServices.IndexerName("target")]
        public ushort this[int channel]
        {
            get
            {
                if (privateTargets == null || channel >= privateTargets.Length)
                {
                    return 0;
                }

                return privateTargets[channel];
            }
        }

        public ushort[] targets
        {
            set
            {
                privateTargets = value;
            }
        }

        /// <summary>
        /// Returns a string with all the servo positions, separated by spaces,
        /// e.g. "0 0 4000 0 1000 0 0".
        /// </summary>
        /// <returns></returns>
        public string getTargetsString()
        {
            string targetsString = "";
            for (int i = 0; i < privateTargets.Length; i++)
            {
                if (i != 0)
                {
                    targetsString += " ";
                }

                targetsString += privateTargets[i].ToString();
            }
            return targetsString;
        }

        /// <summary>
        /// Returns a string the name, duration, and all servo positions, separated by tabs.
        /// e.g. "Frame 1   500 0   0   0   4000    8000"
        /// </summary>
        /// <returns></returns>
        private string getTabSeparatedString()
        {
            string tabString = name + "\t" + length_ms;
            foreach (ushort target in privateTargets)
            {
                tabString += "\t" + target;
            }
            return tabString;
        }

        /// <summary>
        /// Take a (potentially malformed) string with target numbers separated by spaces
        /// and use it to set the targets.
        /// </summary>
        /// <param name="targetsString"></param>
        /// <param name="servoCount"></param>
        public void setTargetsFromString(string targetsString, byte servoCount)
        {
            ushort[] tmpTargets = new ushort[servoCount];

            string[] targetStrings = targetsString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < targetStrings.Length && i < servoCount; i++)
            {
                try
                {
                    tmpTargets[i] = ushort.Parse(targetStrings[i]);
                }
                catch { }
            }
            this.targets = tmpTargets;
        }

        public void writeXml(XmlWriter writer)
        {
            writer.WriteStartElement("Frame");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("duration", length_ms.ToString());
            writer.WriteString(getTargetsString());
            writer.WriteEndElement();
        }

        
    }
}