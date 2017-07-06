using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using System.ComponentModel;

namespace uwp_technocoid_v10
{
    class MidiController
    {
        // Access to the global event handler.
        GlobalEventHandler globalEventHandlerInstance;

        DeviceWatcher deviceWatcher;
        string deviceSelectorString;

        public DeviceInformationCollection availableMidiDevices { get; set; }

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
            this.globalEventHandlerInstance.NotifyMidiMessageReceived(args.Message);
        }
    }
}
