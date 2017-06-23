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

            // Initially create the UI.
            CreateUI();

            // Make sure we update the UI when the main window is resized.
            Window.Current.SizeChanged += UpdateUI;
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
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateUI(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            /*
            var newWindowBounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var newWindowScaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var newWindowSize = new Size(newWindowBounds.Width * newWindowScaleFactor, newWindowBounds.Height * newWindowScaleFactor);
            var newButtonSize = Convert.ToInt32(newWindowSize.Width / (12 * newWindowScaleFactor));

            for (int i = 0; i < 12; i++)
            {
                Button addedButtonElement = (Button)this.FindName("_" + i.ToString());
                addedButtonElement.Width = newButtonSize;
                addedButtonElement.Height = newButtonSize;
            }
            */
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
            StackPanel currentSequencerUIElement = (StackPanel)this.FindName("sequencerTrack1Slot" + currentSequencerPosition.ToString());
            currentSequencerUIElement.Background = new SolidColorBrush(Windows.UI.Colors.Teal);

            // Unmark the last position.
            StackPanel lastSequencerUIElement = (StackPanel)this.FindName("sequencerTrack1Slot" + lastSequencerPosition.ToString());
            lastSequencerUIElement.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);
        }

        /// <summary>
        /// A sequencer slot has been clicked to load a new video into the
        /// respective slot.
        /// </summary>
        /// <param name="sender">The button for the respective slot as Button.</param>
        /// <param name="e">RoutedEventArgs.</param>
        private async void LoadNewVideoForSlot(object sender, RoutedEventArgs e)
        {
            // Create the file picker.
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.VideosLibrary;

            // Define that only video files are interesting.
            openPicker.FileTypeFilter.Add(".mp4");
            openPicker.FileTypeFilter.Add(".mkv");
            openPicker.FileTypeFilter.Add(".avi");

            // Open the file picker and wait for the result.
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                // Extract the track ID for the button that triggered the loading event.
                String senderElementName = (sender as Button).Name.Substring(14);
                String senderElementTrack = senderElementName.Substring(0, senderElementName.IndexOf("Slot"));
                int selectedTrack = 0;
                selectedTrack = int.Parse(senderElementTrack);

                // Extract the slot ID for the button that triggered the loading event.
                senderElementName = (sender as Button).Name.Substring(19);
                String senderElementSlot = senderElementName.Substring(0, senderElementName.IndexOf("Button"));
                int selectedSlot = 0;
                selectedSlot = int.Parse(senderElementSlot);

                // Create a new slot item to fill.
                SequencerSlot newSlotItem = new SequencerSlot();

                // Fill the slot items with the video source and other info.
                newSlotItem.active = false;
                newSlotItem.videoFile = file;
                newSlotItem.videoMediaSource = MediaSource.CreateFromStorageFile(file);
                newSlotItem.thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem);

                // Store new slot information in global squencer data.
                this.globalSequencerDataInstance.setSlotAtPosition(selectedTrack, selectedSlot, newSlotItem);

                // Load the thumbnail image and show it in the UI on the respective button.
                BitmapImage image = new BitmapImage();
                ImageBrush imagebrush = new ImageBrush();
                image.SetSource(newSlotItem.thumbnail);
                Button currentSequencerUIElement = (Button)this.FindName("sequencerTrack" + senderElementTrack + "Slot" + senderElementSlot + "Button");
                imagebrush.ImageSource = image;
                currentSequencerUIElement.Background = imagebrush;
            }
            else
            {
                // rootPage.NotifyUser("Operation cancelled.", NotifyType.ErrorMessage);
            }
        }

        /// <summary>
        /// A slot activity indicator has been clicked to either activate or
        /// deactivate the slot.
        /// </summary>
        /// <param name="sender">The activation element for the respective slot as CheckBox.</param>
        /// <param name="e">RoutedEventArgs.</param>
        private void ActivateSlot(object sender, RoutedEventArgs e)
        {
            // Extract the track ID for the checkbox that triggered the event.
            String senderElementName = (sender as CheckBox).Name.Substring(14);
            String senderElementTrack = senderElementName.Substring(0, senderElementName.IndexOf("Slot"));
            int selectedTrack = 0;
            selectedTrack = int.Parse(senderElementTrack);

            // Extract the slot ID for the checkbox that triggered the event.
            senderElementName = (sender as CheckBox).Name.Substring(19);
            String senderElementSlot = senderElementName.Substring(0, senderElementName.IndexOf("Active"));
            int selectedSlot = 0;
            selectedSlot = int.Parse(senderElementSlot);

            // Get the respective item, update the active state and store it back again.
            SequencerSlot slotItemForUpdate = this.globalSequencerDataInstance.getSlotAtPosition(selectedTrack, selectedSlot);
            slotItemForUpdate.active = (bool)(sender as CheckBox).IsChecked;
            this.globalSequencerDataInstance.setSlotAtPosition(selectedTrack, selectedSlot, slotItemForUpdate);

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
        /// TODO!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sliderCurrentBPM_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            textCurrentBPM.Text = (sender as Slider).Value.ToString();
        }

        /// <summary>
        /// TODO!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textCurrentBPM_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.globalSequencerControllerInstance.UpdateBPM(int.Parse(textCurrentBPM.Text));
        }

        /// <summary>
        /// TODO!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeOpacityForTrack(object sender, RangeBaseValueChangedEventArgs e)
        {
            // Extract the track ID for the checkbox that triggered the event.
            String senderElementName = (sender as Slider).Name.Substring(14);
            String senderElementTrack = senderElementName.Substring(0, senderElementName.IndexOf("OpacitySlider"));
            int selectedTrack = 0;
            selectedTrack = int.Parse(senderElementTrack);

            this.globalSequencerDataInstance.setOpacityForTrack(selectedTrack, ((sender as Slider).Value/100));
            this.globalEventHandlerInstance.NotifyTrackOpacityChanged(selectedTrack);
        }
    }
}
