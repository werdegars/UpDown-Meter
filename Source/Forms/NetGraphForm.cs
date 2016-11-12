﻿using ScriptFUSION.UpDown_Meter.Properties;
using System;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace ScriptFUSION.UpDown_Meter {
    public partial class NetGraphForm : Form {
        private Options options;

        /// <summary>
        /// Point at which the user starts dragging the form.
        /// </summary>
        private Point dragPoint;

        /// <summary>
        /// Application options.
        /// </summary>
        private Options Options
        {
            get { return options; }
            set { SyncOptionsWithGraph(options = value); }
        }

        /// <summary>
        /// Latest absolute sample taken from the current NIC.
        /// </summary>
        private Sample LastSample { get; set; }

        public NetGraphForm() {
            InitializeComponent();

            Options = Options.FromSettings(Settings.Default);

            netGraphBindingSource.Add(netGraph);

            // Timer does not fire at start-up so trigger manually.
            SampleAdapter();
        }

        private void SyncOptionsWithGraph(Options options) {
            var nic = options.NetworkInterface;

            if (nic != null) {
                netGraph.MaximumSpeed = options.NicSpeeds[nic.Id];
            }
        }

        public void SampleAdapter() {
            netGraph.AddSample(CreateRelativeSample());

            UpdateStats();
        }

        private Sample CreateRelativeSample() {
            var nic = Options.NetworkInterface;

            if (nic != null) {
                var stats = nic.GetIPStatistics();
                var lastSample = LastSample;
                var currentSample = LastSample = CreateAbsoluteSample(stats);

                // Do not diff a zero-sample because the reading will be inaccurate and off the scale.
                // This only happens after LastSample has been reset to zero.
                if (lastSample?.Max > 0) {
                    return currentSample - lastSample;
                }
            }

            return new Sample(0, 0);
        }

        private Sample CreateAbsoluteSample(IPInterfaceStatistics stats) {
            return new Sample(stats.BytesReceived, stats.BytesSent);
        }

        private void UpdateStats() {
            var lastSample = netGraph.GetSamples().First();
            dlRaw.Text = lastSample.Downstream.ToString();
            ulRaw.Text = lastSample.Upstream.ToString();

            var sampleSet = netGraph.GetSamples().Take(10);
            dlAvg.Text = ((long)sampleSet.Average(sample => sample.Downstream)).ToString();
            ulAvg.Text = ((long)sampleSet.Average(sample => sample.Upstream)).ToString();
        }

        #region Event handlers

        private void timer_Tick(object sender, EventArgs e) {
            SampleAdapter();
        }

        private void settings_Click(object sender, EventArgs e) {
            using (var optionsForm = new OptionsForm(Options.Clone())) {
                if (optionsForm.ShowDialog(this) == DialogResult.OK) {
                    Options = optionsForm.Options;
                }
            }
        }

        private void close_Click(object sender, EventArgs e) {
            Close();
        }

        private void minimize_Click(object sender, EventArgs e) {
            WindowState = FormWindowState.Minimized;
        }

        private void NetGraphForm_MouseDown(object sender, MouseEventArgs e) {
            // Record drag start location.
            if ((e.Button & MouseButtons.Left) > 0) {
                dragPoint = e.Location;
            }
        }

        private void NetGraphForm_MouseMove(object sender, MouseEventArgs e) {
            // Drag form.
            if ((e.Button & MouseButtons.Left) > 0) {
                Location = new Point(Location.X + e.X - dragPoint.X, Location.Y + e.Y - dragPoint.Y);
            }
        }

        private void NetGraphForm_Paint(object sender, PaintEventArgs e) {
            // Force bottom and right to be drawn before top and left, causing the latter to overlap the former at the corners.
            ControlPaint.DrawBorder3D(e.Graphics, ClientRectangle, Border3DStyle.RaisedInner, Border3DSide.Bottom | Border3DSide.Right);
            ControlPaint.DrawBorder3D(e.Graphics, ClientRectangle, Border3DStyle.RaisedInner, Border3DSide.Top | Border3DSide.Left);
        }

        #endregion
    }
}