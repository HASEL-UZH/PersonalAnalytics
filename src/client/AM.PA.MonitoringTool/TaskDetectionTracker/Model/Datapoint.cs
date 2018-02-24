// Created by Katja Kevic (kevic@ifi.uzh.ch) from the University of Zurich
// Created: 2017-05-16
// 
// Licensed under the MIT License.

using System;

namespace TaskDetectionTracker.Model
{
    public class Datapoint
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }


        private float lexSim1_win;
        public float LexSim1_Win
        {
            get { return lexSim1_win; }
            set { lexSim1_win = value; }
        }

        private float lexSim2_win;
        public float LexSim2_Win
        {
            get { return lexSim2_win; }
            set { lexSim2_win = value; }
        }

        private float lexSim3_win;
        public float LexSim3_Win
        {
            get { return lexSim3_win; }
            set { lexSim3_win = value; }
        }

        private float lexSim4_win;
        public float LexSim4_Win
        {
            get { return lexSim4_win; }
            set { lexSim4_win = value; }
        }

        private float lexSim1_pro;
        public float LexSim1_Pro
        {
            get { return lexSim1_pro; }
            set { lexSim1_pro = value; }
        }

        private float lexSim2_pro;
        public float LexSim2_Pro
        {
            get { return lexSim2_pro; }
            set { lexSim2_pro = value; }
        }

        private float lexSim3_pro;
        public float LexSim3_Pro
        {
            get { return lexSim3_pro; }
            set { lexSim3_pro = value; }
        }

        private float lexSim4_pro;
        public float LexSim4_Pro
        {
            get { return lexSim4_pro; }
            set { lexSim4_pro = value; }
        }

        private int totalKeystrokesDiff;
        public int TotalKeystrokesDiff
        {
            get { return totalKeystrokesDiff; }
            set { totalKeystrokesDiff = value; }
        }

        private int totalMouseClicksDiff;
        public int TotalMouseClicksDiff
        {
            get { return totalMouseClicksDiff; }
            set { totalMouseClicksDiff = value; }
        }
    }
}
