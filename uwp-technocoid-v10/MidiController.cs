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
        OpacityChange = 0,
        BPMChange = 1,
        PlayToggle = 2,
        RewindToggle = 3,
        TrackSelect = 4,
        Slot0Toggle = 5,
        Slot1Toggle = 6,
        Slot2Toggle = 7,
        Slot3Toggle = 8,
        Slot4Toggle = 9,
        Slot5Toggle = 10,
        Slot6Toggle = 11,
        Slot7Toggle = 12,
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

        // Channel and id of the learned MIDI message.
        // This is used to identify relevant MIDI messages.
        public int channel;
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
            if (this.availableMidiDevices == null)
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

            // First check if we are in MIDI learn mode.
            // If so, do not interpret the message, but instead use the message to associate
            // its type with an event type.
            if (this.midiLearningActive)
            {
                // Initialize new MIDI event.
                MidiEventTrigger learnedMidiEvent = new MidiEventTrigger();
                learnedMidiEvent.type = this.midiLearningType;
                learnedMidiEvent.rawOriginalMessage = rawMidiMessage;

                // Check if the message to learn is a ranged value.
                if ((int)this.midiLearningType < 2)
                {
                    // Ranged values are treated as controllers.
                    if (rawMidiMessage.Type == MidiMessageType.ControlChange)
                    {
                        MidiControlChangeMessage currentMidiMessage = (MidiControlChangeMessage)args.Message;
                        learnedMidiEvent.channel = currentMidiMessage.Channel;
                        learnedMidiEvent.id = currentMidiMessage.Controller;
                    }
                }
                // If it's not a ranged value, it should be a toggle.
                else
                {
                    // Toggles are treated as Note On events.
                    if (rawMidiMessage.Type == MidiMessageType.NoteOn)
                    {
                        MidiNoteOnMessage currentMidiMessage = (MidiNoteOnMessage)args.Message;
                        learnedMidiEvent.channel = currentMidiMessage.Channel;
                        learnedMidiEvent.id = currentMidiMessage.Note;
                    }
                }

                // Store identified MIDI event.
                this.learnedMidiTriggers[(int)this.midiLearningType] = learnedMidiEvent;
                this.midiLearningActive = false;

                // Notify that the event has been learned. 
                this.globalEventHandlerInstance.NotifyMidiEventLearned(this.midiLearningType);

                // Do not continue to analyze the input.
                return;
            }

            // Check if the MIDI message was sent by a controller.
            if (rawMidiMessage.Type == MidiMessageType.ControlChange)
            {
                // If so, check all controller based events if the message is relevant.
                MidiControlChangeMessage currentMidiMessage = (MidiControlChangeMessage)args.Message;
                for (int i = 0; i < 2; i++)
                {
                    if (this.learnedMidiTriggers[i].id == currentMidiMessage.Controller)
                    {
                        // Relevant event found, send the notification.
                        MidiEvent midiEvent = new MidiEvent();
                        midiEvent.type = this.learnedMidiTriggers[i].type;
                        midiEvent.value = currentMidiMessage.ControlValue;
                        this.globalEventHandlerInstance.NotifyMidiEventReceived(midiEvent);
                    }
                }
            }
            // Check if the MIDI message was sent by a note trigger.
            else if (rawMidiMessage.Type == MidiMessageType.NoteOn)
            {
                // If so, check all note based events if the message is relevant.
                MidiNoteOnMessage currentMidiMessage = (MidiNoteOnMessage)args.Message;
                for (int i = 2; i < Enum.GetNames(typeof(MidiEventType)).Length; i++)
                {
                    if (this.learnedMidiTriggers[i].id == currentMidiMessage.Note)
                    {
                        // Relevant event found, send the notification.
                        MidiEvent midiEvent = new MidiEvent();
                        midiEvent.type = this.learnedMidiTriggers[i].type;
                        midiEvent.value = currentMidiMessage.Note;
                        this.globalEventHandlerInstance.NotifyMidiEventReceived(midiEvent);
                    }
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
            } else
            {
                this.midiLearningActive = false;
                this.midiLearningType = (MidiEventType)midiEventToLearn;
            }
        }
    }
}
