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
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace uwp_technocoid_v10
{
    public sealed partial class SequencerTrackUI : UserControl
    {
        // Access to all global classes.
        GlobalSequencerController globalSequencerControllerInstance;
        GlobalSequencerData globalSequencerDataInstance;
        GlobalEventHandler globalEventHandlerInstance;

        /// <summary>
        /// Constructor.
        /// </summary>
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
        /// Highlight the given slot in the track UI.
        /// </summary>
        /// <param name="highlightedSlot">ID of the slot to highlight</param>
        /// <param name="highlightState">Bool flag if the highlicht should be active or nor highlighted</param>
        public void HightlightSlot(int highlightedSlot, bool highlightState)
        {
            // Get the element to highlight
            StackPanel highlightedStackPanel = (StackPanel)this.FindName("Slot" + highlightedSlot.ToString());

            // if the element should be highlighted, make it teal,
            // otherwise make it light gray.
            if (highlightState)
            {
                highlightedStackPanel.Background = new SolidColorBrush(Color.FromArgb(155, 0, 120, 215));
            }
            else
            {
                highlightedStackPanel.Background = new SolidColorBrush(Windows.UI.Colors.Transparent);
            }
        }

        /// <summary>
        /// Change the opacity of the player for the video player.
        /// </summary>
        /// <param name="sender">Object for the opacity slider as Slider</param>
        /// <param name="e">RangeBaseValueChangedEventArgs</param>
        private void ChangeOpacityForTrack(object sender, RangeBaseValueChangedEventArgs e)
        {
            // Get parent element of the slider.
            var parentSlotStackPanel = (sender as Slider).Parent;

            // Note that this will be null for the first time when the UI
            // is being created.
            if (parentSlotStackPanel != null)
            {
                // Retrieve slot and track names.
                var parentTrackGrid = (parentSlotStackPanel as StackPanel).Parent;
                var parentSequencerTrackUI = (parentTrackGrid as Grid).Parent;
                var trackName = (parentSequencerTrackUI as SequencerTrackUI).Name;

                // Extract the track ID for the button that triggered the loading event.
                trackName = trackName.Substring(14);
                int selectedTrack = 0;
                selectedTrack = int.Parse(trackName);

                // Set the opacity for the track.
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
            // Retrieve slot and track names.
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
            // Retrieve slot and track names.
            var parentSlotStackPanel = (sender as Button).Parent;
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
            slotItemForUpdate.active = !slotItemForUpdate.active;
            this.globalSequencerDataInstance.setSlotAtPosition(selectedTrack, selectedSlot, slotItemForUpdate);

            if (slotItemForUpdate.active)
            {
                (sender as Button).Background = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));
            }
            else
            {
                (sender as Button).Background = new SolidColorBrush(Color.FromArgb(255, 44, 44, 44));
            }
        }

        /// <summary>
        /// Button to activate all slots in the current track.
        /// </summary>
        /// <param name="sender">Button object for event as Button</param>
        /// <param name="e">RoutedEventArgs</param>
        private void ActivateAllSlots(object sender, RoutedEventArgs e)
        {
            // Retrieve track name.
            var parentStackPanel = (sender as Button).Parent;
            var parentTrackStackPanel = (parentStackPanel as StackPanel).Parent;
            var parentTrackGrid = (parentTrackStackPanel as StackPanel).Parent;
            var parentSequencerTrackUI = (parentTrackGrid as Grid).Parent;
            var trackName = (parentSequencerTrackUI as SequencerTrackUI).Name;

            // Extract the track ID for the button that triggered the loading event.
            trackName = trackName.Substring(14);
            int selectedTrack = 0;
            selectedTrack = int.Parse(trackName);

            // Iterate through the UI elements and activate the checkboxes.
            for (int i = 0; i < 8; i++)
            {
                // Get the respective item, update the active state and store it back again.
                SequencerSlot slotItemForUpdate = this.globalSequencerDataInstance.getSlotAtPosition(selectedTrack, i);
                slotItemForUpdate.active = false;

                // Get the current slot activity button.
                Button slotActivateButtonElement = (Button)this.FindName("Slot" + i.ToString() + "Active");
                this.ActivateSlot(slotActivateButtonElement, null);
            }
        }

        /// <summary>
        /// Button to deactivate all slots in the current track.
        /// </summary>
        /// <param name="sender">Button object for event as Button</param>
        /// <param name="e">RoutedEventArgs</param>
        private void DeactivateAllSlots(object sender, RoutedEventArgs e)
        {
            var parentStackPanel = (sender as Button).Parent;
            var parentTrackStackPanel = (parentStackPanel as StackPanel).Parent;
            var parentTrackGrid = (parentTrackStackPanel as StackPanel).Parent;
            var parentSequencerTrackUI = (parentTrackGrid as Grid).Parent;
            var trackName = (parentSequencerTrackUI as SequencerTrackUI).Name;

            // Extract the track ID for the button that triggered the loading event.
            trackName = trackName.Substring(14);
            int selectedTrack = 0;
            selectedTrack = int.Parse(trackName);

            // Iterate through the UI elements and deactivate the checkboxes.
            for (int i = 0; i < 8; i++)
            {
                // Get the respective item, update the active state and store it back again.
                SequencerSlot slotItemForUpdate = this.globalSequencerDataInstance.getSlotAtPosition(selectedTrack, i);
                slotItemForUpdate.active = true;

                // Get the current slot activity button.
                Button slotActivateButtonElement = (Button)this.FindName("Slot" + i.ToString() + "Active");
                this.ActivateSlot(slotActivateButtonElement, null);
            }
        }


        /// <summary>
        /// Button to clear all slots in the current track.
        /// </summary>
        /// <param name="sender">Button object for event as Button</param>
        /// <param name="e">RoutedEventArgs</param>
        private void ClearAllSlots(object sender, RoutedEventArgs e)
        {
            var parentStackPanel = (sender as Button).Parent;
            var parentTrackStackPanel = (parentStackPanel as StackPanel).Parent;
            var parentTrackGrid = (parentTrackStackPanel as StackPanel).Parent;
            var parentSequencerTrackUI = (parentTrackGrid as Grid).Parent;
            var trackName = (parentSequencerTrackUI as SequencerTrackUI).Name;

            // Extract the track ID for the button that triggered the loading event.
            trackName = trackName.Substring(14);
            int selectedTrack = 0;
            selectedTrack = int.Parse(trackName);

            // Iterate through the UI elements and deactivate the checkboxes.
            for (int i = 0; i < 8; i++)
            {
                // Get the respective item, update the active state and store it back again.
                SequencerSlot slotItemForUpdate = this.globalSequencerDataInstance.getSlotAtPosition(selectedTrack, i);
                slotItemForUpdate.active = true;

                // Get the current slot activity button.
                Button slotActivateButtonElement = (Button)this.FindName("Slot" + i.ToString() + "Active");
                this.ActivateSlot(slotActivateButtonElement, null);

                // Create a new slot item to fill.
                SequencerSlot newSlotItem = new SequencerSlot();

                // Fill the slot items with empty data.
                newSlotItem.active = false;
                newSlotItem.videoFile = null;
                newSlotItem.videoMediaSource = null;
                newSlotItem.thumbnail = null;

                // Store new slot information in global squencer data.
                this.globalSequencerDataInstance.setSlotAtPosition(selectedTrack, i, newSlotItem);

                // Clear the slot button.
                Button videoSlotToClear = (Button)this.FindName("Slot" + i.ToString() + "Button");
                videoSlotToClear.Background = new SolidColorBrush(Color.FromArgb(255, 44, 44, 44));
            }
        }
    }
}
