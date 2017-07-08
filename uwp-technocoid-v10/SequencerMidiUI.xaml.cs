using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Midi;
using System.ComponentModel;
using Windows.UI.Core;
using Windows.UI;

namespace uwp_technocoid_v10
{
    public sealed partial class SequencerMidiUI : UserControl
    {
        // Access to all global classes.
        GlobalSequencerController globalSequencerControllerInstance;
        GlobalSequencerData globalSequencerDataInstance;
        GlobalEventHandler globalEventHandlerInstance;

        // MIDI controller class, handling all the MIDI input.
        MidiController midiController;

        public SequencerMidiUI()
        {
            this.InitializeComponent();

            // Get an instance to the sequencer controller.
            this.globalSequencerControllerInstance = GlobalSequencerController.GetInstance();

            // Get an instance to the event handler and subscribe to the SequencerPositionChanged event.
            this.globalEventHandlerInstance = GlobalEventHandler.GetInstance();

            // Get an instance to the sequencer data handler.
            this.globalSequencerDataInstance = GlobalSequencerData.GetInstance();

            // Initialise MIDI controller and start watcher for new devices.
            midiController = new MidiController();
            midiController.StartWatcher();

            // Register event to toggle the MIDI control UI.
            this.globalEventHandlerInstance.MidiControlsVisibilityChanged += this.ToggleMidiControls;

            // Register events if the available MIDI devices have changed and if a MIDI message was received.
            this.globalEventHandlerInstance.AvailableMidiDevicesChanged += this.UpdateMidiDeviceList;
            this.globalEventHandlerInstance.MidiEventReceived += this.MidiEventReceived;

            // Register event that a new MIDI event has been learned.
            this.globalEventHandlerInstance.MidiEventLearned += this.MidiEventLearned;
        }

        /// <summary>
        /// User wants to see MIDI controls.
        /// </summary>
        /// <param name="requestedVisibilityMode">Visibility option</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private void ToggleMidiControls(object requestedVisibilityMode, PropertyChangedEventArgs e)
        {
            midiMainControls.Visibility = (Visibility)requestedVisibilityMode;
        }

        /// <summary>
        /// The user has selected a MIDI device from the list.
        /// </summary>
        /// <param name="selectedMidiDeviceList">List that triggered the selection</param>
        /// <param name="e">SelectionChangedEventArgs</param>
        private void SelectedMidiDeviceChanged(object selectedMidiDeviceList, SelectionChangedEventArgs e)
        {
            // Notify the event handler about the change.
            this.globalEventHandlerInstance.NotifySelectedMidiDeviceChanged(midiInputDeviceListBox.SelectedIndex);
        }


        /// <summary>
        /// The list of MIDI devices has been updated.
        /// </summary>
        /// <param name="empty">Note that there are no parameters passed down as the list should be taken from the midi controller object</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        public async void UpdateMidiDeviceList(object empty, PropertyChangedEventArgs e)
        {
            await this.globalEventHandlerInstance.controllerDispatcher.RunAsync(
             CoreDispatcherPriority.Normal, () =>
             {
                 // Clear the current MIDI device list.
                 midiInputDeviceListBox.Items.Clear();

                 // If no MIDI devices could be found, add an information.
                 if (midiController.availableMidiDevices.Count == 0)
                 {
                     midiInputDeviceListBox.Items.Add("No MIDI devices found!");
                 }

                 // Iterate through the MIDI device list and add them to the list.
                 foreach (var deviceInformation in midiController.availableMidiDevices)
                 {
                     midiInputDeviceListBox.Items.Add(deviceInformation.Name);
                 }
             });
        }

        /// <summary>
        /// A new MIDI event has been received. Just output it in the status text box.
        /// </summary>
        /// <param name="receivedMidiEvent">Received MIDI event as MidiEvent object</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private async void MidiEventReceived(object receivedMidiEvent, PropertyChangedEventArgs e)
        {
            await this.globalEventHandlerInstance.controllerDispatcher.RunAsync(
             CoreDispatcherPriority.Normal, () =>
             {
                 MidiEvent midiEvent = (MidiEvent)receivedMidiEvent;
                 statusTextControl.Text = "MIDI controller message received of type: " + midiEvent.type + " and value: " + midiEvent.value;
             });
        }

        /// <summary>
        /// Received MIDI learn command from a button.
        /// </summary>
        /// <param name="learnCommandButton">Button the event was received from</param>
        /// <param name="e">RoutedEventArgs</param>
        private void LearnMidiCommand(object learnCommandButton, RoutedEventArgs e)
        {
            // Check if the button is already active, indicating an active learning session already.
            if (((SolidColorBrush)(learnCommandButton as Button).Background).Color == Color.FromArgb(255, 44, 44, 44))
            {
                // Clear all button highlights.
                for (int i = 0; i < 13; i++)
                {
                    Button currentButtonElement = (Button)this.FindName("MidiEventType" + i.ToString());
                    currentButtonElement.Background = new SolidColorBrush(Color.FromArgb(255, 44, 44, 44));
                }

                // Highlight the relevant button.
                (learnCommandButton as Button).Background = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));

                // Get ID of the MIDI event type.
                var eventType = (learnCommandButton as Button).Name.Substring(13);
                MidiEventType midiEventToLearn = (MidiEventType)int.Parse(eventType);

                // Notify subscribers about learning a new MIDI event.
                this.globalEventHandlerInstance.NotifyLearnMidiEvent(midiEventToLearn);
            }
            else
            {
                // Reset button color.
                (learnCommandButton as Button).Background = new SolidColorBrush(Color.FromArgb(255, 44, 44, 44));

                // Notify subscribers that no MIDI event should be learned.
                this.globalEventHandlerInstance.NotifyLearnMidiEvent(MidiEventType.Empty);
            }
        }

        /// <summary>
        /// A new MIDI event has been learned.
        /// </summary>
        /// <param name="midiEventLearned">Learned MIDI event as MidiEvent object</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private async void MidiEventLearned(object midiEventLearned, PropertyChangedEventArgs e)
        {
            await this.globalEventHandlerInstance.controllerDispatcher.RunAsync(
             CoreDispatcherPriority.Normal, () =>
             {
                 // Find the button associated with the learned event and reset the color.
                 Button currentButtonElement = (Button)this.FindName("MidiEventType" + ((int)midiEventLearned).ToString());
                 currentButtonElement.Background = new SolidColorBrush(Color.FromArgb(255, 44, 44, 44));
                 statusTextControl.Text = "MIDI event learned: " + midiEventLearned.ToString();
             });
        }
    }
}
