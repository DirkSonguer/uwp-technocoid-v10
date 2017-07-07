using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using Windows.UI.Core;

namespace uwp_technocoid_v10
{
    /// <summary>
    /// Global Event Handler.
    /// This class manages the communication between the individual UIs
    /// and systems. Note that it also stores and provides information on
    /// the different windows and threads.
    /// </summary>
    class GlobalEventHandler
    {
        // This class is a singleton, allowing every UI class to access the same data.
        private static readonly GlobalEventHandler eventHandlerInstance = new GlobalEventHandler();
        public static GlobalEventHandler GetInstance()
        {
            return eventHandlerInstance;
        }

        // This will store the thread dispatchers for the controller and
        // fullscreen player windows. It can be used to control the pages running
        // within these threads.
        public CoreDispatcher controllerDispatcher;
        public CoreDispatcher playerDispatcher;

        // An event indicating that the current sequencer position has changed.
        // Classes can subscribe to this event and get notified.
        public event PropertyChangedEventHandler SequencerPositionChanged;
        public void NotifySequencerPositionChanged(int currentSequencerPosition)
        {
            if (SequencerPositionChanged != null)
            {
                SequencerPositionChanged(currentSequencerPosition, new PropertyChangedEventArgs("int currentSequencerPosition"));
            }
        }
        
        // An event indicating that the sequencer has been started or stopped.
        // Classes can subscribe to this event and get notified.
        public event PropertyChangedEventHandler CurrentlyPlayingChanged;
        public void NotifyCurrentlyPlayingChanged(bool isCurrentlyPlaying)
        {
            if (CurrentlyPlayingChanged != null)
            {
                CurrentlyPlayingChanged(isCurrentlyPlaying, new PropertyChangedEventArgs("bool isCurrentlyPlaying"));
            }
        }

        // An event indicating that the sequencer has been started or stopped.
        // Classes can subscribe to this event and get notified.
        public event PropertyChangedEventHandler TrackOpacityChanged;
        public void NotifyTrackOpacityChanged(int sequencerTrack)
        {
            if (TrackOpacityChanged != null)
            {
                TrackOpacityChanged(sequencerTrack, new PropertyChangedEventArgs("int sequencerTrack"));
            }
        }

        // An event indicating that player should enter or leave fullscreen mode.
        // Classes can subscribe to this event and get notified.
        public event PropertyChangedEventHandler FullscreenModeChanged;
        public void NotifyFullscreenModeChanged(bool requestedFullscreenMode)
        {
            if (FullscreenModeChanged != null)
            {
                FullscreenModeChanged(requestedFullscreenMode, new PropertyChangedEventArgs("bool requestedFullscreenMode"));
            }
        }

        // An event indicating that the available MIDI devices have changed.
        // Classes can subscribe to this event and get notified.
        public event PropertyChangedEventHandler AvailableMidiDevicesChanged;
        public void NotifyAvailableMidiDevicesChanged()
        {
            if (AvailableMidiDevicesChanged != null)
            {
                AvailableMidiDevicesChanged(null, new PropertyChangedEventArgs("DeviceInformationCollection availableMidiDevices"));
            }
        }

        // An event indicating that the selected MIDI device has changed.
        // Classes can subscribe to this event and get notified.
        public event PropertyChangedEventHandler SelectedMidiDeviceChanged;
        public void NotifySelectedMidiDeviceChanged(int selectedMidiDeviceIndex)
        {
            if (SelectedMidiDeviceChanged != null)
            {
                SelectedMidiDeviceChanged(selectedMidiDeviceIndex, new PropertyChangedEventArgs("int selectedIndex"));
            }
        }

        // An event indicating that a MIDI message has been received.
        // Classes can subscribe to this event and get notified.
        public event PropertyChangedEventHandler MidiMessageReceived;
        public void NotifyMidiMessageReceived(MidiEvent receivedMidiMessage)
        {
            if (MidiMessageReceived != null)
            {
                MidiMessageReceived(receivedMidiMessage, new PropertyChangedEventArgs("MidiEvent receivedMidiMessage"));
            }
        }

        // Event to trigger MIDI learning.
        // Classes can subscribe to this event and get notified.
        public event PropertyChangedEventHandler LearnMidiEvent;
        public void NotifyLearnMidiEvent(MidiEventType midiEventToLearn)
        {
            if (LearnMidiEvent != null)
            {
                LearnMidiEvent(midiEventToLearn, new PropertyChangedEventArgs("MidiEventType midiEventToLearn"));
            }
        }
    }
}
