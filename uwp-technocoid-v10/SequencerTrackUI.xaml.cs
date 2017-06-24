using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace uwp_technocoid_v10
{
    public sealed partial class SequencerTrackUI : UserControl
    {
        // Access to all global classes.
        GlobalSequencerController globalSequencerControllerInstance;
        GlobalSequencerData globalSequencerDataInstance;
        GlobalEventHandler globalEventHandlerInstance;

        public SequencerTrackUI()
        {
            this.InitializeComponent();

            // Get an instance to the sequencer controller.
            this.globalSequencerControllerInstance = GlobalSequencerController.GetInstance();

            // Get an instance to the event handler.
            this.globalEventHandlerInstance = GlobalEventHandler.GetInstance();

            // Get an instance to the sequencer data handler.
            this.globalSequencerDataInstance = GlobalSequencerData.GetInstance();
        }

        /// <summary>
        /// TODO!
        /// </summary>
        /// <param name="highlightedSlot"></param>
        /// <param name="highlightState"></param>
        public void HightlightSlot(int highlightedSlot, bool highlightState)
        {
            StackPanel highlightedStackPanel = (StackPanel)this.FindName("Slot" + highlightedSlot.ToString());

            if (highlightState)
            {
                highlightedStackPanel.Background = new SolidColorBrush(Windows.UI.Colors.Teal);

            } else
            {
                highlightedStackPanel.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);
            }
        }

        /// <summary>
        /// Change the opacity of the player for the video player.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeOpacityForTrack(object sender, RangeBaseValueChangedEventArgs e)
        {
            var parentSlotStackPanel = (sender as Slider).Parent;
            if (parentSlotStackPanel != null)
            {
                var parentTrackGrid = (parentSlotStackPanel as StackPanel).Parent;
                var parentSequencerTrackUI = (parentTrackGrid as Grid).Parent;
                var trackName = (parentSequencerTrackUI as SequencerTrackUI).Name;

                // Extract the track ID for the button that triggered the loading event.
                trackName = trackName.Substring(14);
                int selectedTrack = 0;
                selectedTrack = int.Parse(trackName);

                if (this.globalSequencerDataInstance != null)
                {
                    this.globalSequencerDataInstance.setOpacityForTrack(selectedTrack, ((sender as Slider).Value / 100));
                    this.globalEventHandlerInstance.NotifyTrackOpacityChanged(selectedTrack);
                }
            }
        }

        /// <summary>
        /// A sequencer slot has been clicked to load a new video into the
        /// respective slot.
        /// </summary>
        /// <param name="sender">The button for the respective slot as Button.</param>
        /// <param name="e">RoutedEventArgs.</param>
        private async void LoadNewVideoForSlot(object sender, RoutedEventArgs e)
        {
            var parentSlotStackPanel = (sender as Button).Parent;
            var slotName = (parentSlotStackPanel as StackPanel).Name;
            var parentTrackStackPanel = (parentSlotStackPanel as StackPanel).Parent;
            var parentTrackGrid = (parentTrackStackPanel as StackPanel).Parent;
            var parentSequencerTrackUI = (parentTrackGrid as Grid).Parent;
            var trackName = (parentSequencerTrackUI as SequencerTrackUI).Name;

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
                trackName = trackName.Substring(14);
                int selectedTrack = 0;
                selectedTrack = int.Parse(trackName);

                // Extract the slot ID for the button that triggered the loading event.
                slotName = slotName.Substring(4);
                int selectedSlot = 0;
                selectedSlot = int.Parse(slotName);

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
                //                Button currentSequencerUIElement = (Button)this.FindName("sequencerTrack" + senderElementTrack + "Slot" + senderElementSlot + "Button");
                imagebrush.ImageSource = image;
                (sender as Button).Background = imagebrush;
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
            var parentSlotStackPanel = (sender as CheckBox).Parent;
            var slotName = (parentSlotStackPanel as StackPanel).Name;
            var parentTrackStackPanel = (parentSlotStackPanel as StackPanel).Parent;
            var parentTrackGrid = (parentTrackStackPanel as StackPanel).Parent;
            var parentSequencerTrackUI = (parentTrackGrid as Grid).Parent;
            var trackName = (parentSequencerTrackUI as SequencerTrackUI).Name;

            // Extract the track ID for the button that triggered the loading event.
            trackName = trackName.Substring(14);
            int selectedTrack = 0;
            selectedTrack = int.Parse(trackName);

            // Extract the slot ID for the button that triggered the loading event.
            slotName = slotName.Substring(4);
            int selectedSlot = 0;
            selectedSlot = int.Parse(slotName);

            // Get the respective item, update the active state and store it back again.
            SequencerSlot slotItemForUpdate = this.globalSequencerDataInstance.getSlotAtPosition(selectedTrack, selectedSlot);
            slotItemForUpdate.active = (bool)(sender as CheckBox).IsChecked;
            this.globalSequencerDataInstance.setSlotAtPosition(selectedTrack, selectedSlot, slotItemForUpdate);

        }
    }
}
