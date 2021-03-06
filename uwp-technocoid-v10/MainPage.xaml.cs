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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.ViewManagement;
using Windows.UI.Core;
using Windows.UI.Composition;
using Windows.ApplicationModel.Core;
using System.ComponentModel;
using Windows.Devices.Midi;
using Windows.Foundation;
using Windows.System.Threading;
using System.Numerics;

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

        // Timer to measure if window resize is finished.
        // + min sizes for the window.
        static ThreadPoolTimer windowResizeTimer;
        int minWindowWidth = 950;
        int minWindowHeight = 740;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            // Setting minimum window size.
            ApplicationView.PreferredLaunchViewSize = new Size(this.minWindowWidth, this.minWindowHeight);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            // Get an instance to the sequencer data handler.
            this.globalSequencerDataInstance = GlobalSequencerData.GetInstance();

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

            // Subscribe to the window resize event.
            Window.Current.CoreWindow.SizeChanged += UpdateUI;

            // Subscribe to window close event.
            ApplicationView.GetForCurrentView().Consolidated += MainWindowClosed;

            // Initially create the UI.
            CreateUI();
        }

        /// <summary>
        /// Main window has been closed. Exit the app and thus close the player window as a result.
        /// </summary>
        /// <param name="sender">ApplicationView</param>
        /// <param name="e">ApplicationViewConsolidatedEventArgs</param>
        private void MainWindowClosed(object sender, object e)
        {
            Application.Current.Exit();
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
        /// Update the UI if the user has resized the window.
        /// At this point, the window is resized to the minimum size if the user resizes it too small.
        /// This is considered VERY bad behaviour.
        /// TODO: Make the UI properly responsive.
        /// </summary>
        /// <param name="sender">CoreWindow</param>
        /// <param name="e">WindowSizeChangedEventArgs containing the new window size</param>
        public void UpdateUI(CoreWindow sender, WindowSizeChangedEventArgs e)
        {
            // Check if the timer is already running.
            // If so, then cancel it. It will be re-created below if the window is still too small.
            if (windowResizeTimer != null) windowResizeTimer.Cancel();

            // Get the new window width and height.
            double newHeight = e.Size.Height;
            double newWidth = e.Size.Width;

            // Check if the window is too narrow.
            if ((newWidth < this.minWindowWidth) || (newHeight < this.minWindowHeight))
            {
                // Set the timeout to 1 second.
                TimeSpan timeout = new TimeSpan(0, 0, 0, 1);

                // Create a new timer. After the timeout, the resize code will be executed.
                windowResizeTimer = ThreadPoolTimer.CreateTimer(async (ThreadPoolTimer timer) =>
                {
                    await this.globalEventHandlerInstance.controllerDispatcher.RunAsync(
                     CoreDispatcherPriority.Normal, () =>
                     {
                         ApplicationView.GetForCurrentView().TryResizeView(new Size(this.minWindowWidth, this.minWindowHeight));
                     });
                }, timeout);
            }

        }

        /// <summary>
        /// A new MIDI message has been received.
        /// </summary>
        /// <param name="receivedMidiMessage">Received MIDI message as IMidiMessage object</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private async void MidiMessageReceived(object receivedMidiMessage, PropertyChangedEventArgs e)
        {
            await this.globalEventHandlerInstance.controllerDispatcher.RunAsync(
             CoreDispatcherPriority.Normal, () =>
             {
                 MidiControlChangeMessage currentMidiMessage = (MidiControlChangeMessage)receivedMidiMessage;
             });
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
                // Unmark the current step.
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
