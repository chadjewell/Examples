using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViDi2.VisionPro;
using ViDi2.VisionPro.Records;

namespace Example.VisionPro.Integration
{
    public class LabeledViewRecord
    {
        /// <summary>
        /// Attaches a label to a given view's results,
        /// allowing the view with the same label to be
        /// found across multiple tool executions.
        /// </summary>
        public LabeledViewRecord(string label, ViewRecord record)
        {
            Label = label;
            Record = record;
        }

        /// <summary>
        /// The name of the view.
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// The record containing result graphics
        /// for the view.
        /// </summary>
        public ViewRecord Record { get; private set; }
    }
}