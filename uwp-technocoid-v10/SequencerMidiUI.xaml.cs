using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
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

        // Cache for the current theme button color.
        SolidColorBrush themeButtonColor;

        // MIDI controller class, handling all the MIDI input.
        MidiController midiController;

        // Local Windows app storage.
        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        public SequencerMidiUI()
        {
            this.InitializeComponent();

            // Get an instance to the sequencer controller.
            this.globalSequencerControllerInstance = GlobalSequencerController.GetInstance();

            // Get an instance to the event handler and subscribe to the SequencerPositionChanged event.
            this.globalEventHandlerInstance = GlobalEventHandler.GetInstance();

            // Store the current dispatcher to the global event handler.
            this.globalEventHandlerInstance.controllerDispatcher = Dispatcher;

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

            // Update the MIDI device list initially, assume no MIDI devices initially.
            this.UpdateMidiDeviceList(null, null);

            // Store current theme button color.
            this.themeButtonColor = (SolidColorBrush)MidiEventType4.Background;

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
            MidiMainControls.Visibility = (Visibility)requestedVisibilityMode;
        }

        /// <summary>
        /// The user has selected a MIDI device from the list.
        /// </summary>
        /// <param name="selectedMidiDeviceList">List that triggered the selection</param>
        /// <param name="e">SelectionChangedEventArgs</param>
        private void SelectedMidiDeviceChanged(object selectedMidiDeviceList, SelectionChangedEventArgs e)
        {
            // Notify the event handler about the change.
            this.globalEventHandlerInstance.NotifySelectedMidiDeviceChanged(MidiInputDeviceListBox.SelectedIndex);
        }

        /// <summary>
        /// The list of MIDI devices has been updated.
        /// </summary>
        /// <param name="empty">Note that there are no parameters passed down as the list should be taken from the midi controller object</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        public async void UpdateMidiDeviceList(object empty, PropertyChangedEventArgs e)
        {
            if (this.globalEventHandlerInstance.controllerDispatcher != null)
            {
                await this.globalEventHandlerInstance.controllerDispatcher.RunAsync(
                 CoreDispatcherPriority.Normal, () =>
                 {
                     // Clear the current MIDI device list.
                     MidiInputDeviceListBox.Items.Clear();

                     // If no MIDI devices could be found, add an information.
                     if (midiController.availableMidiDevices.Count == 0)
                     {
                         MidiInputDeviceListBox.Items.Add("No MIDI devices found!");

                         // Hide the MIDI learn controls.
                         StatusTextControl.Text = "No MIDI device found.";
                         MidiLearnControls.Visibility = Visibility.Collapsed;
                     }
                     else
                     {
                         // Iterate through the MIDI device list and add them to the list.
                         foreach (var deviceInformation in midiController.availableMidiDevices)
                         {
                             MidiInputDeviceListBox.Items.Add(deviceInformation.Name);
                         }

                         // Make the MIDI learn controls visible.
                         StatusTextControl.Text = "Select function to learn MIDI command.";
                         MidiLearnControls.Visibility = Visibility.Visible;
                     }
                 });
            }
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
                 StatusTextControl.Text = "MIDI message received, type: " + midiEvent.type + ", value: " + midiEvent.value;
             });
        }

        /// <summary>
        /// Received MIDI learn command from a button.
        /// </summary>
        /// <param name="learnCommandButton">Button the event was received from</param>
        /// <param name="e">RoutedEventArgs</param>
        private void LearnMidiCommand(object learnCommandButton, RoutedEventArgs e)
        {
            // Check if the button is not already active, indicating an active learning session.
            // If not, initiate the learning session.
            if (((SolidColorBrush)(learnCommandButton as Button).Background).Color != Color.FromArgb(255, 0, 120, 215))
            {
                // Clear all button highlights.
                for (int i = 0; i < 12; i++)
                {
                    Button currentButtonElement = (Button)this.FindName("MidiEventType" + i.ToString());

                    // Check if the type has been trained yet.
                    if (midiController.learnedMidiTriggers[i].id == 0)
                    {
                        // If not, use the standard color for the button.
                        currentButtonElement.Background = this.themeButtonColor;
                    }
                    else
                    {
                        // Otherwise use the highlight color.
                        currentButtonElement.Background = new SolidColorBrush(Color.FromArgb(255, 0, 71, 138));
                    }
                }

                // Highlight the relevant button that should be learned.
                (learnCommandButton as Button).Background = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));

                // Get ID of the MIDI event type.
                var eventType = (learnCommandButton as Button).Name.Substring(13);
                MidiEventType midiEventToLearn = (MidiEventType)int.Parse(eventType);

                // Notify subscribers about learning a new MIDI event.
                this.globalEventHandlerInstance.NotifyLearnMidiEvent(midiEventToLearn);
            }
            else
            {
                // Check if the type has been trained yet.
                var eventType = (learnCommandButton as Button).Name.Substring(13);
                if (midiController.learnedMidiTriggers[int.Parse(eventType)].id == 0)
                {
                    // If not, use the standard color for the button.
                    (learnCommandButton as Button).Background = this.themeButtonColor;
                }
                else
                {
                    // Otherwise use the highlight color.
                    (learnCommandButton as Button).Background = new SolidColorBrush(Color.FromArgb(255, 0, 71, 138));
                }

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
                 currentButtonElement.Background = new SolidColorBrush(Color.FromArgb(255, 0, 71, 138));
                 StatusTextControl.Text = "MIDI event learned: " + midiEventLearned.ToString();
                 StoreMidiSettingsButton.IsEnabled = true;
             });
        }

        /// <summary>
        /// Load the MIDI event list from the settings.
        /// </summary>
        /// <param name="sender">Button the event was received from</param>
        /// <param name="e">RoutedEventArgs</param>
        private void LoadMidiSettings(object sender, RoutedEventArgs e)
        {
            // Iterate through all MIDI event types.
            for (int i = 0; i < 12; i++)
            {
                // Create a new MidiEventTrigger object.
                MidiEventTrigger tempMidiEventTrigger = new MidiEventTrigger();

                // Set the type based on the current step.
                tempMidiEventTrigger.type = (MidiEventType)i;

                // Get trigger id from the storage.
                tempMidiEventTrigger.id = (int)localSettings.Values[i.ToString()];

                // Set the extracted data to the active MIDI triggers.
                midiController.learnedMidiTriggers[i] = tempMidiEventTrigger;

                // Find the relevant button for this type.
                Button currentButtonElement = (Button)this.FindName("MidiEventType" + i.ToString());
                if (tempMidiEventTrigger.id != 0)
                {
                    // If this is an active trigger, highlight it.
                    currentButtonElement.Background = new SolidColorBrush(Color.FromArgb(255, 0, 71, 138));
                }
                else
                {
                    // If not, use the standard color for the button.
                    currentButtonElement.Background = this.themeButtonColor;
                }
            }

            // Done, set status text.
            StatusTextControl.Text = "MIDI settings restored.";
        }

        /// <summary>
        /// Store the MIDI event list from the settings.
        /// </summary>
        /// <param name="sender">Button the event was received from</param>
        /// <param name="e">RoutedEventArgs</param>
        private void StoreMidiSettings(object sender, RoutedEventArgs e)
        {
            // Iterate through all MIDI event types.
            for (int i = 0; i < 12; i++)
            {
                // Store the trigger in the storage.
                localSettings.Values[i.ToString()] = midiController.learnedMidiTriggers[i].id;
            }

            // Done, set status text.
            StatusTextControl.Text = "MIDI settings saved.";
        }

        private void ResetMidiSettings(object sender, RoutedEventArgs e)
        {
            // Iterate through all MIDI event types.
            for (int i = 0; i < 12; i++)
            {
                // Create a new MidiEventTrigger object and fill it with empty data.
                MidiEventTrigger tempMidiEventTrigger = new MidiEventTrigger();
                tempMidiEventTrigger.type = (MidiEventType)i;
                tempMidiEventTrigger.id = 0;

                // Set the extracted data to the active MIDI triggers.
                midiController.learnedMidiTriggers[i] = tempMidiEventTrigger;

                // Find the relevant button for this type and use the standard color.
                Button currentButtonElement = (Button)this.FindName("MidiEventType" + i.ToString());
                currentButtonElement.Background = this.themeButtonColor;
            }
        }
    }
}
