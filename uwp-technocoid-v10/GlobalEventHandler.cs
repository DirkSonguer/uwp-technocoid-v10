﻿//---------------------------------------------------------------------------//
// Technocoid v10
// link: https://github.com/DirkSonguer/uwp-technocoid-v10
// authors: Dirk Songuer
//
// You should have received a copy of the MIT License
// along with this program called LICENSE.md
// If not, see <https://choosealicense.com/licenses/mit/>
//---------------------------------------------------------------------------//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.UI.Core;
using Windows.UI.Xaml;

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

        // An event indicating that the opacity for a track has been changed.
        // Classes can subscribe to this event and get notified.
        public event PropertyChangedEventHandler TrackOpacityChanged;
        public void NotifyTrackOpacityChanged(int sequencerTrack)
        {
            if (TrackOpacityChanged != null)
            {
                TrackOpacityChanged(sequencerTrack, new PropertyChangedEventArgs("int sequencerTrack"));
            }
        }

        // An event indicating that the playback rate (speed) for a track has been changed.
        // Classes can subscribe to this event and get notified.
        public event PropertyChangedEventHandler TrackPlaybackRateChanged;
        public void NotifyTrackPlaybackRateChanged(int sequencerTrack)
        {
            if (TrackPlaybackRateChanged != null)
            {
                TrackPlaybackRateChanged(sequencerTrack, new PropertyChangedEventArgs("int sequencerTrack"));
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

        // An event indicating that MIDI controls should be visible.
        // Classes can subscribe to this event and get notified.
        public event PropertyChangedEventHandler MidiControlsVisibilityChanged;
        public void NotifyMidiControlsVisibilityChangedd(Visibility requestedVisibilityMode)
        {
            if (MidiControlsVisibilityChanged != null)
            {
                MidiControlsVisibilityChanged(requestedVisibilityMode, new PropertyChangedEventArgs("Visibility requestedVisibilityMode"));
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

        // An event indicating that a MIDI event has been received.
        // Classes can subscribe to this event and get notified.
        public event PropertyChangedEventHandler MidiEventReceived;
        public void NotifyMidiEventReceived(MidiEvent receivedMidiEvent)
        {
            if (MidiEventReceived != null)
            {
                MidiEventReceived(receivedMidiEvent, new PropertyChangedEventArgs("MidiEvent receivedMidiEvent"));
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

        // MIDI event that has been learned.
        // Classes can subscribe to this event and get notified.
        public event PropertyChangedEventHandler MidiEventLearned;
        public void NotifyMidiEventLearned(MidiEventType midiEventLearned)
        {
            if (MidiEventLearned != null)
            {
                MidiEventLearned(midiEventLearned, new PropertyChangedEventArgs("MidiEventType midiEventLearned"));
            }
        }
    }
}
