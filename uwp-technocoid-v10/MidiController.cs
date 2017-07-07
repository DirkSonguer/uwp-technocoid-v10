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
    enum MidiEventType
    {
        OpacityChange = 0,
        BPMChange = 1,
        playToggle = 2,
        RewindToggle = 3,
        TrackSelect = 4,
        Slot0Toggle = 5,
        Slot1Toggle = 6,
        Slot2Toggle = 7,
        Slot3Toggle = 8,
        Slot4Toggle = 9,
        Slot5Toggle = 10,
        Slot6Toggle = 11,
        Slot7Toggle = 12
    };

    /// <summary>
    /// TODO
    /// </summary>
    class MidiEventTrigger
    {
        public MidiEventType triggerMessageType;

        public int triggerChannel;
        public int triggerID;

        // The raw message for reference.
        public IMidiMessage rawOriginalMessage;
    }

    /// <summary>
    /// TODO
    /// </summary>
    class MidiEvent
    {
        public MidiEventType eventType;

        // Every event just has one value that is relevant.
        // In the case of toggles, this is pretty much just a bool.
        // For BPM and Opacity changes it's a byte.
        public int eventValue;
    }

    class MidiController
    {
        // Access to the global event handler.
        GlobalEventHandler globalEventHandlerInstance;

        DeviceWatcher deviceWatcher;
        string deviceSelectorString;

        public DeviceInformationCollection availableMidiDevices { get; set; }

        bool midiLearningActive = false;
        MidiEventType midiLearningType = 0;

        public MidiEventTrigger[] learnedMidiTriggers = new MidiEventTrigger[Enum.GetNames(typeof(MidiEventType)).Length];

        public MidiController()
        {
            // Get an instance to the event handler and subscribe to the SequencerPositionChanged event.
            this.globalEventHandlerInstance = GlobalEventHandler.GetInstance();

            deviceSelectorString = MidiInPort.GetDeviceSelector();

            deviceWatcher = DeviceInformation.CreateWatcher(deviceSelectorString);
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;

            this.globalEventHandlerInstance.SelectedMidiDeviceChanged += this.SelectedMidiDeviceChanged;
            this.globalEventHandlerInstance.LearnMidiEvent += this.LearnMidiEvent;

            for (int i = 0; i < Enum.GetNames(typeof(MidiEventType)).Length; i++)
            {
                learnedMidiTriggers[i] = new MidiEventTrigger();
            }
        }

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
        /// The sequencer triggered a step progression.
        /// CHange the UI accordingly.
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

            midiInPort.MessageReceived += MidiMessageReceived;
        }

        private void MidiMessageReceived(MidiInPort sender, MidiMessageReceivedEventArgs args)
        {
            IMidiMessage rawMidiMessage = (IMidiMessage)args.Message;

            // First check if we are in MIDI learn mode.
            // If so, do not interpret the message, but instead use the message to associate
            // its type with an event type.
            if (this.midiLearningActive)
            {
                //learnedMidiTriggers
                MidiEventTrigger learnedMidiEvent = new MidiEventTrigger();
                learnedMidiEvent.triggerMessageType = this.midiLearningType;
                learnedMidiEvent.rawOriginalMessage = rawMidiMessage;

                // Check if the message to learn is a ranged value.
                if ((int)this.midiLearningType < 2)
                {
                    // Ranged values are treated as controllers.
                    if (rawMidiMessage.Type == MidiMessageType.ControlChange)
                    {
                        MidiControlChangeMessage currentMidiMessage = (MidiControlChangeMessage)args.Message;
                        learnedMidiEvent.triggerChannel = currentMidiMessage.Channel;
                        learnedMidiEvent.triggerID = currentMidiMessage.Controller;
                    }
                }
                // If it's not a ranged value, it should be a toggle.
                else
                {
                    // Toggles are treated as Note On events.
                    if (rawMidiMessage.Type == MidiMessageType.NoteOn)
                    {
                        MidiNoteOnMessage currentMidiMessage = (MidiNoteOnMessage)args.Message;
                        learnedMidiEvent.triggerChannel = currentMidiMessage.Channel;
                        learnedMidiEvent.triggerID = currentMidiMessage.Note;
                    }
                }

                this.learnedMidiTriggers[(int)this.midiLearningType] = learnedMidiEvent;
                this.midiLearningActive = false;
                return;
            }

            if (rawMidiMessage.Type == MidiMessageType.ControlChange)
            {
                MidiControlChangeMessage currentMidiMessage = (MidiControlChangeMessage)args.Message;
                for (int i = 0; i < 2; i++)
                {
                    if (this.learnedMidiTriggers[i].triggerID == currentMidiMessage.Controller)
                    {
                        MidiEvent midiEvent = new MidiEvent();
                        midiEvent.eventType = this.learnedMidiTriggers[i].triggerMessageType;
                        midiEvent.eventValue = currentMidiMessage.ControlValue;
                        this.globalEventHandlerInstance.NotifyMidiMessageReceived(midiEvent);
                    }
                }
            }
            else if(rawMidiMessage.Type == MidiMessageType.NoteOn)
            {
                MidiNoteOnMessage currentMidiMessage = (MidiNoteOnMessage)args.Message;
                for (int i = 2; i < Enum.GetNames(typeof(MidiEventType)).Length; i++)
                {
                    if (this.learnedMidiTriggers[i].triggerID == currentMidiMessage.Note)
                    {
                        MidiEvent midiEvent = new MidiEvent();
                        midiEvent.eventType = this.learnedMidiTriggers[i].triggerMessageType;
                        midiEvent.eventValue = currentMidiMessage.Note;
                        this.globalEventHandlerInstance.NotifyMidiMessageReceived(midiEvent);
                    }
                }
            }
        }

        private void LearnMidiEvent(object midiEventToLearn, PropertyChangedEventArgs e)
        {
            this.midiLearningActive = true;
            this.midiLearningType = (MidiEventType)midiEventToLearn;
        }
    }
}
