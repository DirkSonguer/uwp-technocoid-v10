using System;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.ViewManagement;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using System.ComponentModel;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;

namespace uwp_technocoid_v10
{
    /// <summary>
    /// The page containing the controller UI.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Access to all global classes.
        GlobalSequencerController globalSequencerControllerInstance;
        GlobalSequencerData globalSequencerDataInstance;
        GlobalEventHandler globalEventHandlerInstance;

        // MIDI controller class, handling all the MIDI input.
        MidiController midiController;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            // Explicitly show the title bar.
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;

            // Get an instance to the sequencer controller.
            this.globalSequencerControllerInstance = GlobalSequencerController.GetInstance();

            // Get an instance to the event handler and subscribe to the SequencerPositionChanged event.
            this.globalEventHandlerInstance = GlobalEventHandler.GetInstance();
            this.globalEventHandlerInstance.SequencerPositionChanged += this.SequencerTrigger;
            this.globalEventHandlerInstance.CurrentlyPlayingChanged += this.SequencerPlayingChanged;

            // Store the current dispatcher to the global event handler.
            this.globalEventHandlerInstance.controllerDispatcher = Dispatcher;

            // Get an instance to the sequencer data handler.
            this.globalSequencerDataInstance = GlobalSequencerData.GetInstance();

            // Initialise MIDI controller and start watcher for new devices.
            midiController = new MidiController();
            midiController.StartWatcher();

            // Register events if the available MIDI devices have changed and if a MIDI message was received.
            this.globalEventHandlerInstance.AvailableMidiDevicesChanged += this.UpdateMidiDeviceList;
            this.globalEventHandlerInstance.MidiMessageReceived += this.MidiMessageReceived;

            // Initially create the UI.
            CreateUI();
        }

        /// <summary>
        /// This will initially create the UI.
        /// Note that some of the the UI does not exist at first and is created dynamically.
        /// </summary>
        public async void CreateUI()
        {
            // Create a new new core application view.
            // this will be the external windows to host the media player.
            CoreApplicationView newView = CoreApplication.CreateNewView();

            // This will hold the ID of the new view.
            int newViewId = 0;

            // Remember the dispatcher for the new view so it can later be addressed as the
            // view runs in its own thread.
            // This is stored globally in the event handler.
            this.globalEventHandlerInstance.playerDispatcher = newView.Dispatcher;

            // Run the new view in a new thread based on the new dispatcher.
            await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Create a new content frame and load the fullscreen viewer into it.
                Frame frame = new Frame();
                frame.Navigate(typeof(FullScreenViewer), null);
                Window.Current.Content = frame;

                // The window needs to be active in order to show it later.
                Window.Current.Activate();

                // Get the IDs for the view and window.
                newViewId = ApplicationView.GetForCurrentView().Id;
                var newWindowId = ApplicationView.GetApplicationViewIdForWindow(newView.CoreWindow);
            });

            // Activate and show the new window.
            bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
            sequencerControls.SetStatusMessage("Player window created, ready.");
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
        /// A new MIDI message has been received.
        /// </summary>
        /// <param name="receivedMidiMessage">Received MIDI message as IMidiMessage object</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private void MidiMessageReceived(object receivedMidiMessage, PropertyChangedEventArgs e)
        {
/*
            System.Diagnostics.Debug.WriteLine((IMidiMessage)receivedMidiMessage.Timestamp.ToString());

            if ((IMidiMessage)receivedMidiMessage.Type == MidiMessageType.NoteOn)
            {
                System.Diagnostics.Debug.WriteLine(((MidiNoteOnMessage)receivedMidiMessage).Channel);
                System.Diagnostics.Debug.WriteLine(((MidiNoteOnMessage)receivedMidiMessage).Note);
                System.Diagnostics.Debug.WriteLine(((MidiNoteOnMessage)receivedMidiMessage).Velocity);
            }
            */
        }

        /// <summary>
        /// The sequencer triggered a step progression.
        /// Change the UI accordingly.
        /// </summary>
        /// <param name="currentSequencerPosition">Current position slot as int</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private void SequencerTrigger(object currentSequencerPosition, PropertyChangedEventArgs e)
        {
            sequencerControls.SetStatusMessage("Sequencer running, step " + ((int)currentSequencerPosition + 1).ToString() + ".");

            // Get current and last sequencer position.
            int lastSequencerPosition = (int)currentSequencerPosition - 1;
            if (lastSequencerPosition < 0) lastSequencerPosition = 7;

            // Mark the current position.
            sequencerTrack0.HightlightSlot((int)currentSequencerPosition, true);
            sequencerTrack1.HightlightSlot((int)currentSequencerPosition, true);
            sequencerTrack2.HightlightSlot((int)currentSequencerPosition, true);
            sequencerTrack3.HightlightSlot((int)currentSequencerPosition, true);

            // Unmark the last position.
            sequencerTrack0.HightlightSlot(lastSequencerPosition, false);
            sequencerTrack1.HightlightSlot(lastSequencerPosition, false);
            sequencerTrack2.HightlightSlot(lastSequencerPosition, false);
            sequencerTrack3.HightlightSlot(lastSequencerPosition, false);
        }

        /// <summary>
        /// The sequencer has been started or stopped.
        /// CHange the UI accordingly.
        /// </summary>
        /// <param name="currentSequencerPosition">Bool to indicate if sequencer has been started or stopped</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private void SequencerPlayingChanged(object currentSequencerPlaying, PropertyChangedEventArgs e)
        {
            // If the sequencer has been stopped, clear all track highlights.
            if (!(bool)currentSequencerPlaying)
            {
                sequencerControls.SetStatusMessage("Sequencer stopped, ready.");
                for (int i = 0; i < 8; i++)
                {
                    sequencerTrack0.HightlightSlot(i, false);
                    sequencerTrack1.HightlightSlot(i, false);
                    sequencerTrack2.HightlightSlot(i, false);
                    sequencerTrack3.HightlightSlot(i, false);
                }
            }
            else
            {
                sequencerControls.SetStatusMessage("Sequencer is running.");
            }
        }
    }
}
