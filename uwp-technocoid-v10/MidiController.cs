using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Midi;
using System.ComponentModel;

namespace uwp_technocoid_v10
{
    // Enum with all MIDI triggerable events that are relevant to the application.
    // Note that those are events that can be triggered by a MIDI message,
    // not messages themselves.
    enum MidiEventType
    {
        Opacity0Change = 0,
        Opacity1Change = 1,
        Opacity2Change = 2,
        Opacity3Change = 3,
        PlaybackRate0Change = 4,
        PlaybackRate1Change = 5,
        PlaybackRate2Change = 6,
        PlaybackRate3Change = 7,
        BPMChange = 8,
        PlayToggle = 9,
        RewindToggle = 10,
        TapTempo = 11,
        Empty = 99
    };

    /// <summary>
    /// A trigger that can be associated with a MIDI message.
    /// Those are associated when MIDI messages are learned.
    /// </summary>
    class MidiEventTrigger
    {
        // Type of the event so that the recipients can check for relevance.
        public MidiEventType type;

        // ID of the learned MIDI message.
        // This is used to identify relevant MIDI messages.
        public int id;

        // The raw MIDI message that was used for training for reference.
        public IMidiMessage rawOriginalMessage;
    }

    /// <summary>
    /// An event based on a MIDI message. This type will be propagated
    /// by the event handler.
    /// </summary>
    class MidiEvent
    {
        // Type of the event so that the recipients can check for relevance.
        public MidiEventType type;

        // Every event just has one value that is relevant.
        // In the case of toggles, this is pretty much just a bool.
        // For BPM and Opacity changes it's a byte.
        public int value;
    }

    class MidiController
    {
        // Access to the global event handler.
        GlobalEventHandler globalEventHandlerInstance;

        // The device watcher keeps an eye on the available MIDI
        // devices and notifies in case of changes.
        DeviceWatcher deviceWatcher;
        string deviceSelectorString;

        // List of all currently available MIDI devices.
        public DeviceInformationCollection availableMidiDevices { get; set; }

        // Indicators when MIDI learning is active.
        bool midiLearningActive = false;
        MidiEventType midiLearningType = 0;

        // The learned MIDI triggers that will identify relevant events.
        public MidiEventTrigger[] learnedMidiTriggers = new MidiEventTrigger[Enum.GetNames(typeof(MidiEventType)).Length];

        /// <summary>
        /// Constructor.
        /// </summary>
        public MidiController()
        {
            // Get an instance to the event handler and subscribe to the SequencerPositionChanged event.
            this.globalEventHandlerInstance = GlobalEventHandler.GetInstance();

            // Selected MIDI device.
            deviceSelectorString = MidiInPort.GetDeviceSelector();

            // Activate device watcher and add callbacks for state changes.
            deviceWatcher = DeviceInformation.CreateWatcher(deviceSelectorString);
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;

            // Subscribe to changed MIDI device.
            this.globalEventHandlerInstance.SelectedMidiDeviceChanged += this.SelectedMidiDeviceChanged;

            // Subscribe to event to learn MIDI messages.
            this.globalEventHandlerInstance.LearnMidiEvent += this.LearnMidiEvent;

            // Initialize MIDI event triggers.
            for (int i = 0; i < Enum.GetNames(typeof(MidiEventType)).Length; i++)
            {
                learnedMidiTriggers[i] = new MidiEventTrigger();
            }
        }

        /// <summary>
        /// Deconstructor.
        /// </summary>
        ~MidiController()
        {
            deviceWatcher.Added -= DeviceWatcher_Added;
            deviceWatcher.Removed -= DeviceWatcher_Removed;
            deviceWatcher.Updated -= DeviceWatcher_Updated;

            deviceWatcher = null;
        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            this.availableMidiDevices = await DeviceInformation.FindAllAsync(deviceSelectorString);
            this.globalEventHandlerInstance.NotifyAvailableMidiDevicesChanged();
        }

        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            this.availableMidiDevices = await DeviceInformation.FindAllAsync(deviceSelectorString);
            this.globalEventHandlerInstance.NotifyAvailableMidiDevicesChanged();
        }

        private async void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            this.availableMidiDevices = await DeviceInformation.FindAllAsync(deviceSelectorString);
            this.globalEventHandlerInstance.NotifyAvailableMidiDevicesChanged();
        }

        private async void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            this.availableMidiDevices = await DeviceInformation.FindAllAsync(deviceSelectorString);
            this.globalEventHandlerInstance.NotifyAvailableMidiDevicesChanged();
        }

        public void StartWatcher()
        {
            deviceWatcher.Start();
        }

        public void StopWatcher()
        {
            deviceWatcher.Stop();
        }

        /// <summary>
        /// The user selected a new MIDI input device.
        /// </summary>
        /// <param name="selectedMidiDeviceIndex">Index of the currently selected MIDI input device as int</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private async void SelectedMidiDeviceChanged(object selectedMidiDeviceIndex, PropertyChangedEventArgs e)
        {
            // Check that the current list of devices actually contains any devices.
            if ((this.availableMidiDevices == null) || (this.availableMidiDevices.Count < 1))
            {
                return;
            }

            // Check that the currently selected index actually exists in the current list of devices.
            if (this.availableMidiDevices[(int)selectedMidiDeviceIndex] == null)
            {
                return;
            }

            // Get information about the device that was selected.
            DeviceInformation selectedDeviceInfo = this.availableMidiDevices[(int)selectedMidiDeviceIndex];
            // This might fail if the user has selected a device that was disconnected in the mean time.
            if (selectedDeviceInfo == null)
            {
                return;
            }

            // Bind the current MIDI input port to the selected device.
            var midiInPort = await MidiInPort.FromIdAsync(selectedDeviceInfo.Id);
            // This might fail if the device is not accepted as MIDI input source.
            if (midiInPort == null)
            {
                return;
            }

            // Subscribe to receive MIDI messages.
            midiInPort.MessageReceived += MidiMessageReceived;
        }

        /// <summary>
        /// Get the ID of the MIDI message.
        /// </summary>
        /// <param name="inputMidiMessage">Raw MIDI message.</param>
        /// <returns>The ID as int.</returns>
        private int ExtractMidiMessageID(IMidiMessage inputMidiMessage)
        {
            // Check controller type messages.
            if (inputMidiMessage.Type == MidiMessageType.ControlChange)
            {
                MidiControlChangeMessage currentMidiMessage = (MidiControlChangeMessage)inputMidiMessage;
                return currentMidiMessage.Controller;
            }

            // Check note on type messages.
            if (inputMidiMessage.Type == MidiMessageType.NoteOn)
            {
                MidiNoteOnMessage currentMidiMessage = (MidiNoteOnMessage)inputMidiMessage;
                return currentMidiMessage.Note;
            }

            return -1;
        }

        /// <summary>
        /// Get the relevant value of the MIDI message.
        /// </summary>
        /// <param name="inputMidiMessage">Raw MIDI message.</param>
        /// <returns>The value as int.</returns>
        private int ExtractMidiMessageValue(IMidiMessage inputMidiMessage)
        {
            // Check controller type messages.
            if (inputMidiMessage.Type == MidiMessageType.ControlChange)
            {
                MidiControlChangeMessage currentMidiMessage = (MidiControlChangeMessage)inputMidiMessage;
                return currentMidiMessage.ControlValue;
            }

            // Check note on type messages.
            if (inputMidiMessage.Type == MidiMessageType.NoteOn)
            {
                MidiNoteOnMessage currentMidiMessage = (MidiNoteOnMessage)inputMidiMessage;
                return currentMidiMessage.Velocity;
            }

            return 0;
        }

        /// <summary>
        /// A new MIDI message was received.
        /// Check if the message is relevant by checking the trained MIDI events
        /// and if so, send a notification.
        /// </summary>
        /// <param name="sender">MIDI port that sent the message</param>
        /// <param name="args">MidiMessageReceivedEventArgs</param>
        private void MidiMessageReceived(MidiInPort sender, MidiMessageReceivedEventArgs args)
        {
            // Get the message as simple IMidiMessage.
            IMidiMessage rawMidiMessage = (IMidiMessage)args.Message;

            // Get the id and relevant value of the MIDI message.
            int midiMessageID = this.ExtractMidiMessageID(rawMidiMessage);
            int midiMessageValue = this.ExtractMidiMessageValue(rawMidiMessage);

            // First check if we are in MIDI learn mode.
            // If so, do not interpret the message, but instead use the message to associate
            // its type with an event type.
            if (this.midiLearningActive)
            {
                // Initialize new MIDI event.
                MidiEventTrigger learnedMidiEvent = new MidiEventTrigger();
                learnedMidiEvent.type = this.midiLearningType;
                learnedMidiEvent.rawOriginalMessage = rawMidiMessage;

                // Set the ID of the message.
                learnedMidiEvent.id = midiMessageID;

                // Store identified MIDI event.
                this.learnedMidiTriggers[(int)this.midiLearningType] = learnedMidiEvent;
                this.midiLearningActive = false;

                // Notify that the event has been learned. 
                this.globalEventHandlerInstance.NotifyMidiEventLearned(this.midiLearningType);

                // Do not continue to analyze the input.
                return;
            }

            // Iterate through all types if the message was recognized.
            for (int i = 0; i < Enum.GetNames(typeof(MidiEventType)).Length; i++)
            {
                if (this.learnedMidiTriggers[i].id == midiMessageID)
                {
                    // Relevant event found, send the notification.
                    MidiEvent midiEvent = new MidiEvent();
                    midiEvent.type = this.learnedMidiTriggers[i].type;
                    midiEvent.value = midiMessageValue;
                    this.globalEventHandlerInstance.NotifyMidiEventReceived(midiEvent);
                }
            }
        }

        /// <summary>
        /// Event to learn a new MIDI event type.
        /// </summary>
        /// <param name="midiEventToLearn">The MIDI event that should be learned as MidiEventType</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private void LearnMidiEvent(object midiEventToLearn, PropertyChangedEventArgs e)
        {
            if ((MidiEventType)midiEventToLearn != MidiEventType.Empty)
            {
                this.midiLearningActive = true;
                this.midiLearningType = (MidiEventType)midiEventToLearn;
            }
            else
            {
                this.midiLearningActive = false;
                this.midiLearningType = (MidiEventType)midiEventToLearn;
            }
        }
    }
}
