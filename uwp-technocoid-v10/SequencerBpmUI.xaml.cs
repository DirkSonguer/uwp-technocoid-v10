using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace uwp_technocoid_v10
{
    public sealed partial class SequencerBpmUI : UserControl
    {
        // Access to all global classes.
        GlobalSequencerController globalSequencerControllerInstance;
        GlobalSequencerData globalSequencerDataInstance;
        GlobalEventHandler globalEventHandlerInstance;

        private long[] bpmTapTimer = new long[5];

        public SequencerBpmUI()
        {
            this.InitializeComponent();

            // Get an instance to the sequencer controller.
            this.globalSequencerControllerInstance = GlobalSequencerController.GetInstance();

            // Get an instance to the event handler.
            this.globalEventHandlerInstance = GlobalEventHandler.GetInstance();

            // Get an instance to the sequencer data handler.
            this.globalSequencerDataInstance = GlobalSequencerData.GetInstance();

            for (int i = 0; i < 5; i++)
            {
                this.bpmTapTimer[i] = 0;
            }

            CoreWindow.GetForCurrentThread().KeyDown += Keyboard_KeyDown;

            // Set the initial BPM to 60.
            currentBpmInput.Text = "60";
        }

        /// <summary>
        /// This is a very quick & dirty implementation of a tap-to-BPM.
        /// If the user taps the space bar, the BPM will be calculated based
        /// on their tap speed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Keyboard_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            // Only react to the space bar
            if (args.VirtualKey.ToString() == "Space")
            {
                // Check if the last inpout was longer tham 5 seconds ago.
                if (DateTime.Now.Ticks > (this.bpmTapTimer[0] + 50000000))
                {
                    // If so, reset the measurements to get a clean average.
                    for (int i = 0; i < 5; i++)
                    {
                        this.bpmTapTimer[i] = 0;
                    }
                }

                // If we have more than 4 measure points, roll over.
                if (this.bpmTapTimer[4] > 0)
                {
                    for (int i = 1; i < 5; i++)
                    {
                        this.bpmTapTimer[i - 1] = this.bpmTapTimer[i];
                    }
                    this.bpmTapTimer[4] = 0;
                }

                // Store the current time in the most recent bpm timer slot.
                bool bpmCounterUpdated = false;
                for (int i = 0; i < 5; i++)
                {
                    if (this.bpmTapTimer[i] == 0)
                    {
                        this.bpmTapTimer[i] = DateTime.Now.Ticks;
                        bpmCounterUpdated = true;
                        break;
                    }
                }

                // Calculate the average bpm based on the tap distances.
                if (bpmCounterUpdated)
                {
                    double bpmAverage = 0.0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (this.bpmTapTimer[i + 1] > 0)
                        {
                            long tapDistance = this.bpmTapTimer[i + 1] - this.bpmTapTimer[i];
                            if (i > 0)
                            {
                                bpmAverage = (bpmAverage + tapDistance) / 2;
                            }
                            else
                            {
                                bpmAverage = tapDistance;
                            }
                        }
                    }

                    // If the average could be calculated, set it in the UI.
                    if (bpmAverage > 0)
                    {
                        bpmAverage = bpmAverage / 10000;
                        int newBPM = Convert.ToInt32(60000 / bpmAverage);
                        currentBpmInput.Text = newBPM.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Change the BPM counter via the text input box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void currentBpmChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                this.globalSequencerControllerInstance.UpdateBPM(int.Parse(currentBpmInput.Text));
                currentBpmSlider.Value = int.Parse(currentBpmInput.Text);
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void halfCurrentBpm(object sender, RoutedEventArgs e)
        {
            int currentBpm = int.Parse(currentBpmInput.Text);
            currentBpm *= 2;
            currentBpmInput.Text = currentBpm.ToString();
            currentBpmSlider.Value = currentBpm;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void doubleCurrentBpm(object sender, RoutedEventArgs e)
        {
            int currentBpm = int.Parse(currentBpmInput.Text);
            currentBpm = currentBpm / 2;
            currentBpmInput.Text = currentBpm.ToString();
            currentBpmSlider.Value = currentBpm;
        }
    }
}
