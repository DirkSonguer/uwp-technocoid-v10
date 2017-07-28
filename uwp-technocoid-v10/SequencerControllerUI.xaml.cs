using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace uwp_technocoid_v10
{
    public sealed partial class SequencerControllerUI : UserControl
    {
        // Access to all global classes.
        GlobalSequencerController globalSequencerControllerInstance;
        GlobalSequencerData globalSequencerDataInstance;
        GlobalEventHandler globalEventHandlerInstance;

        // Cache for the current theme button color.
        SolidColorBrush themeButtonColor;

        // Temp cache for the user tap input.
        private long[] bpmTapTimer = new long[5];

        /// <summary>
        /// Constructor.
        /// </summary>
        public SequencerControllerUI()
        {
            this.InitializeComponent();

            // Get an instance to the sequencer controller.
            this.globalSequencerControllerInstance = GlobalSequencerController.GetInstance();

            // Get an instance to the event handler.
            this.globalEventHandlerInstance = GlobalEventHandler.GetInstance();

            // Subscribe to MIDI events.
            this.globalEventHandlerInstance.MidiEventReceived += this.MidiEventReceived;

            // Get an instance to the sequencer data handler.
            this.globalSequencerDataInstance = GlobalSequencerData.GetInstance();

            // Clear out the tap cache.
            for (int i = 0; i < 5; i++)
            {
                this.bpmTapTimer[i] = 0;
            }

            // Register for the keyboard input event to register taps.
            CoreWindow.GetForCurrentThread().KeyDown += Keyboard_KeyDown;

            // Store current theme button color.
            this.themeButtonColor = (SolidColorBrush)StartSequencerButton.Background;

            // Set the initial BPM to 60.
            CurrentBpmSlider.Value = 60;
        }

        /// <summary>
        /// User pressed the play button.
        /// Start / stop the sequencer and change the button icon accordingly.
        /// </summary>
        /// <param name="sender">The button for the play functionality as Button</param>
        /// <param name="e">RoutedEventArgs</param>
        private void StartSequencer(object sender, RoutedEventArgs e)
        {
            if ("\uE102" == StartSequencerButton.Content.ToString())
            {
                StartSequencerButton.Content = "\uE103";
                this.globalEventHandlerInstance.NotifyCurrentlyPlayingChanged(true);
                (sender as Button).Background = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));
            }
            else
            {
                StartSequencerButton.Content = "\uE102";
                this.globalEventHandlerInstance.NotifyCurrentlyPlayingChanged(false);
                (sender as Button).Background = this.themeButtonColor;
            }
        }

        /// <summary>
        /// User pressed rewind button.
        /// This will rewind the sequencer, effectively restarting it from position 0.
        /// </summary>
        /// <param name="sender">The button for the rewind functionality as Button</param>
        /// <param name="e">RoutedEventArgs</param>
        private void RewindSequencer(object sender, RoutedEventArgs e)
        {
            this.globalEventHandlerInstance.NotifyCurrentlyPlayingChanged(false);
            this.globalEventHandlerInstance.NotifyCurrentlyPlayingChanged(true);
        }

        /// <summary>
        /// Simple exposure to the status message text element.
        /// </summary>
        /// <param name="newStatusMessage">String with the new status message</param>
        public void SetStatusMessage(String newStatusMessage)
        {
            StatusTextControl.Text = newStatusMessage;
        }

        /// <summary>
        /// Button to set the player window to fullscreen.
        /// </summary>
        /// <param name="sender">Button object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void ToggleFullscreen(object sender, RoutedEventArgs e)
        {
            this.globalEventHandlerInstance.NotifyFullscreenModeChanged(true);

            // TODO: Implement!
            StatusTextControl.Text = "This button does nothing yet.";
        }

        /// <summary>
        /// Button to show the MIDI controls in the UI.
        /// </summary>
        /// <param name="sender">Button object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void ToggleMidiControls(object sender, RoutedEventArgs e)
        {
            if (((SolidColorBrush)ToggleMidiControlsButton.Background).Color == this.themeButtonColor.Color)
            {
                (sender as Button).Background = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));
                this.globalEventHandlerInstance.NotifyMidiControlsVisibilityChangedd(Visibility.Visible);
                StatusTextControl.Text = "Showing MIDI controls.";
            }
            else
            {
                (sender as Button).Background = this.themeButtonColor;
                this.globalEventHandlerInstance.NotifyMidiControlsVisibilityChangedd(Visibility.Collapsed);
                StatusTextControl.Text = "Hiding MIDI controls.";
            }
        }

        /// <summary>
        /// Keyboard input has been detected.
        /// The input will be redirected to the respective funtionality.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Keyboard_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            // Right key detected. Increase current BPM count by 10.
            if (args.VirtualKey.ToString() == "Right")
            {
                this.PlusTenBpm(null, null);
            }

            // Left key detected. Decrease current BPM count by 10.
            if (args.VirtualKey.ToString() == "Left")
            {
                this.MinusTenBpm(null, null);
            }

            // Up key detected. Double current BPM count.
            if (args.VirtualKey.ToString() == "Up")
            {
                this.DoubleCurrentBpm(null, null);
            }

            // Down key detected. Halve current BPM count.
            if (args.VirtualKey.ToString() == "Down")
            {
                this.HalfCurrentBpm(null, null);
            }

            // Space bar detected. Trigger another tap for BPM detection.
            if (args.VirtualKey.ToString() == "Space")
            {
                this.TapDetectionTriggered();
            }
        }

        /// <summary>
        /// A new MIDI event has been received.
        /// </summary>
        /// <param name="receivedMidiEvent">Received MIDI event as MidiEvent object</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private async void MidiEventReceived(object receivedMidiEvent, PropertyChangedEventArgs e)
        {
            await this.globalEventHandlerInstance.controllerDispatcher.RunAsync(
             CoreDispatcherPriority.Normal, () =>
             {
                 MidiEvent midiEvent = (MidiEvent)receivedMidiEvent;

                 // Interpret BPM change.
                 if (midiEvent.type == MidiEventType.BPMChange)
                 {
                     CurrentBpmSlider.Value = (midiEvent.value * 2);
                 }

                 // Interpret BPM tap tempo.
                 if (midiEvent.type == MidiEventType.TapTempo)
                 {
                     this.TapDetectionTriggered();
                 }

                 // Interpret Play toggle.
                 if (midiEvent.type == MidiEventType.PlayToggle)
                 {
                     this.StartSequencer(StartSequencerButton, null);
                 }

                 // Interpret Rewind toggle.
                 if (midiEvent.type == MidiEventType.RewindToggle)
                 {
                     this.RewindSequencer(RewindSequencerButton, null);
                 }
             });
        }

        /// <summary>
        /// Change the BPM counter via the slider.
        /// Note that this is also used by every other UI control that wants to change the BPM count.
        /// </summary>
        /// <param name="sender">The text input object as TextBox</param>
        /// <param name="e">TextChangedEventArgs</param>
        private void CurrentBpmChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (this.globalSequencerControllerInstance != null)
            {
                // Convert the input to a number and update the BPM for the sequencer.
                this.globalSequencerControllerInstance.UpdateBPM(Convert.ToInt32(CurrentBpmSlider.Value));
                CurrentBpmOutput.Text = CurrentBpmSlider.Value.ToString();
                StatusTextControl.Text = "New BPM set to " + CurrentBpmSlider.Value.ToString();
            }
        }

        /// <summary>
        /// This is a very quick & dirty implementation of a tap-to-BPM.
        /// If the user taps the space bar, the BPM will be calculated based
        /// on their tap speed from the average time between individual taps.
        /// </summary>
        private void TapDetectionTriggered()
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
                    CurrentBpmSlider.Value = newBPM;
                }
            }
        }

        /// <summary>
        /// Button to half the current BPM speed.
        /// </summary>
        /// <param name="sender">Button object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void HalfCurrentBpm(object sender, RoutedEventArgs e)
        {
            // Convert and set the BPM. Note that we just set the TextBox value,
            // which will set the sequencer BPM.
            int currentBpm = Convert.ToInt32(CurrentBpmSlider.Value);
            if (currentBpm > 59)
            {
                currentBpm = currentBpm / 2;
                CurrentBpmSlider.Value = currentBpm;
            }
        }

        /// <summary>
        /// Button to double the current BPM speed.
        /// </summary>
        /// <param name="sender">Button object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void DoubleCurrentBpm(object sender, RoutedEventArgs e)
        {
            // Convert and set the BPM. Note that we just set the TextBox value,
            // which will set the sequencer BPM.
            int currentBpm = Convert.ToInt32(CurrentBpmSlider.Value);
            if (currentBpm < 121)
            {
                currentBpm *= 2;
                CurrentBpmSlider.Value = currentBpm;
            }
        }

        /// <summary>
        /// Button to decrease BPM speed by ten.
        /// </summary>
        /// <param name="sender">Button object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void MinusTenBpm(object sender, RoutedEventArgs e)
        {
            // Convert and set the BPM. Note that we just set the TextBox value,
            // which will set the sequencer BPM.
            int currentBpm = Convert.ToInt32(CurrentBpmSlider.Value);
            if (currentBpm > 39)
            {
                currentBpm -= 10;
                CurrentBpmSlider.Value = currentBpm;
            }
        }

        /// <summary>
        /// Button to increase BPM speed by ten.
        /// </summary>
        /// <param name="sender">Button object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void PlusTenBpm(object sender, RoutedEventArgs e)
        {
            // Convert and set the BPM. Note that we just set the TextBox value,
            // which will set the sequencer BPM.
            int currentBpm = Convert.ToInt32(CurrentBpmSlider.Value);
            if (currentBpm < 231)
            {
                currentBpm += 10;
                CurrentBpmSlider.Value = currentBpm;
            }
        }
    }
}
