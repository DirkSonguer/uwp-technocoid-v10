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

        private long[] bpmTapTimer = new long[5];

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            // Get an instance to the sequencer controller.
            this.globalSequencerControllerInstance = GlobalSequencerController.GetInstance();

            // Get an instance to the event handler and subscribe to the SequencerPositionChanged event.
            this.globalEventHandlerInstance = GlobalEventHandler.GetInstance();
            this.globalEventHandlerInstance.SequencerPositionChanged += this.SequencerTrigger;

            // Get an instance to the sequencer data handler.
            this.globalSequencerDataInstance = GlobalSequencerData.GetInstance();

            for (int i = 0; i < 5; i++)
            {
                this.bpmTapTimer[i] = 0;
            }

            CoreWindow.GetForCurrentThread().KeyDown += Keyboard_KeyDown;

            // Initially create the UI.
            CreateUI();
        }

        /// <summary>
        /// This is a very quick & dirty implementation of a tap-to-BPM.
        /// If the user taps the space bar, the BPM will be calculated based
        /// on their tap speed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Keyboard_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            // Only react to the space bar
            if (args.VirtualKey.ToString() == "Space")
            {
                // Check if the last inpout was longer tham 5 seconds ago.
                if (DateTime.Now.Ticks > (this.bpmTapTimer[0] + 50000000))
                {
                    // If so, reset the measurements to get a clean average.
                    for (int i = 0; i < 5; i++)
                    {
                        this.bpmTapTimer[i] = 0;
                    }
                }

                // If we have more than 4 measure points, roll over.
                if (this.bpmTapTimer[4] > 0)
                {
                    for (int i = 1; i < 5; i++)
                    {
                        this.bpmTapTimer[i-1] = this.bpmTapTimer[i];
                    }
                    this.bpmTapTimer[4] = 0;
                }

                // Store the current time in the most recent bpm timer slot.
                bool bpmCounterUpdated = false;
                for (int i = 0; i < 5; i++)
                {
                    if (this.bpmTapTimer[i] == 0)
                    {
                        this.bpmTapTimer[i] = DateTime.Now.Ticks;
                        bpmCounterUpdated = true;
                        break;
                    }
                }

                // Calculate the average bpm based on the tap distances.
                if (bpmCounterUpdated)
                {
                    double bpmAverage = 0.0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (this.bpmTapTimer[i + 1] > 0)
                        {
                            long tapDistance = this.bpmTapTimer[i + 1] - this.bpmTapTimer[i];
                            if (i > 0)
                            {
                                bpmAverage = (bpmAverage + tapDistance) / 2;
                            } else
                            {
                                bpmAverage = tapDistance;
                            }
                        }
                    }

                    // If the average could be calculated, set it in the UI.
                    if (bpmAverage > 0)
                    {
                        bpmAverage = bpmAverage / 10000;
                        int newBPM = Convert.ToInt32(60000 / bpmAverage);
                        textCurrentBPM.Text = newBPM.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// This will initially create the UI.
        /// Note that some of the the UI does not exist at first and is created dynamically.
        /// </summary>
        public async void CreateUI()
        {
            // Set the initial BPM to 60. Note that if we set the slider, we will also set the
            // Text box.
            sliderCurrentBPM.Value = 60;

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
            statusTextControl.Text = "External view created";
        }

        /// <summary>
        /// The sequencer triggered a step progression.
        /// CHange the UI accordingly.
        /// </summary>
        /// <param name="currentSequencerPosition">Current position slot as int.</param>
        /// <param name="e">PropertyChangedEventArgs.</param>
        private void SequencerTrigger(object currentSequencerPosition, PropertyChangedEventArgs e)
        {
            statusTextControl.Text = "Sequencer is at step " + currentSequencerPosition.ToString();

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
        /// TODO!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startSequencer_Click(object sender, RoutedEventArgs e)
        {
            if ("\uE102" == startSequencer.Content.ToString())
            {
                startSequencer.Content = "\uE103";
                globalEventHandlerInstance.NotifyCurrentlyPlayingChanged(true);
            }
            else
            {
                startSequencer.Content = "\uE102";
                globalEventHandlerInstance.NotifyCurrentlyPlayingChanged(false);
            }
        }

        /// <summary>
        /// Change the BPM counter via the slider.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sliderCurrentBPM_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            textCurrentBPM.Text = (sender as Slider).Value.ToString();
        }

        /// <summary>
        /// Change the BPM counter via the text input box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textCurrentBPM_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.globalSequencerControllerInstance.UpdateBPM(int.Parse(textCurrentBPM.Text));
        }
    }
}
