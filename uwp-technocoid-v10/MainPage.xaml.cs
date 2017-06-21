using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.Graphics.Display;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using System.ComponentModel;

namespace uwp_technocoid_v10
{
    public sealed partial class MainPage : Page
    {
        GlobalSequencerController globalSequencerControllerInstance;
        GlobalSequencerData globalSequencerDataInstance;
        GlobalEventHandler globalEventHandlerInstance;

        private bool isRunningInFullScreenView = false;

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
        public void CreateUI()
        {
            // Get current (new) window size and scale.
            var newWindowBounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var newWindowScaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var newWindowSize = new Size(newWindowBounds.Width * newWindowScaleFactor, newWindowBounds.Height * newWindowScaleFactor);
            var newButtonSize = Convert.ToInt32(newWindowSize.Width / (12 * newWindowScaleFactor));

            // 
            for (int i = 0; i < 12; i++)
            {
                sequencerTrack.Children.Add(new Button
                {
                    Content = "_" + i.ToString(),
                    Name = "_" + i.ToString(),
                    Background = new SolidColorBrush(Windows.UI.Colors.LightGray),
                    BorderThickness = new Thickness(5, 10, 5, 10),
                    BorderBrush = new SolidColorBrush(Windows.UI.Colors.Gray),
                });

                Button addedButtonElement = (Button)this.FindName("_" + i.ToString());
                addedButtonElement.Click += openVideo_Click;
                addedButtonElement.Width = newButtonSize;
                addedButtonElement.Height = newButtonSize;
            }

            textCurrentBPM.Text = "60";
            sliderCurrentBPM.Value = 60;
        }

        private void UpdateUI(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
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
        }

        private void SequencerTrigger(object currentSequencerPosition, PropertyChangedEventArgs e)
        {
            statusTextControl.Text = "Sequencer is at step " + currentSequencerPosition.ToString();

            int lastSequencerPosition = (int)currentSequencerPosition - 1;
            if (lastSequencerPosition < 0) lastSequencerPosition = 11;

            Button currentSequencerUIElement = (Button)this.FindName("_" + currentSequencerPosition.ToString());
            statusTextControl.Text += " and button " + currentSequencerUIElement.Name;
            currentSequencerUIElement.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Teal);

            Button lastSequencerUIElement = (Button)this.FindName("_" + lastSequencerPosition.ToString());
            lastSequencerUIElement.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Gray);

            if (!this.isRunningInFullScreenView)
            {
                VideoItem currentVideoItem = globalSequencerDataInstance.getVideoItemForPosition((int)currentSequencerPosition);
                if (currentVideoItem.videoMediaSource != null)
                {
                    statusTextControl.Text += ", playing a media element";
                    mediaPlayerElement.MediaPlayer.Source = currentVideoItem.videoMediaSource;
                    mediaPlayerElement.MediaPlayer.Play();
                }
                else
                {
                    statusTextControl.Text += ", pausing";
                }
            }
        }


        private void startSequencer_Click(object sender, RoutedEventArgs e)
        {
            if ("\uE102" == startSequencer.Content.ToString())
            {
                startSequencer.Content = "\uE103";
                mediaPlayerElement.MediaPlayer.IsLoopingEnabled = true;
                globalEventHandlerInstance.TriggerCurrentlyPlayingChanged(true);
            }
            else
            {
                startSequencer.Content = "\uE102";
                mediaPlayerElement.MediaPlayer.Pause();
                globalEventHandlerInstance.TriggerCurrentlyPlayingChanged(false);
            }
        }

        private async void openVideo_Click(object sender, RoutedEventArgs e)
        {
            // Create and open the file picker
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            openPicker.FileTypeFilter.Add(".mp4");
            openPicker.FileTypeFilter.Add(".mkv");
            openPicker.FileTypeFilter.Add(".avi");

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                String senderElementName = (sender as Button).Name.Substring(1);
                int i = 0;
                i = int.Parse(senderElementName);

                VideoItem newVideoItem = new VideoItem();

                newVideoItem.videoFile = file;
                newVideoItem.videoMediaSource = MediaSource.CreateFromStorageFile(file);
                newVideoItem.videoThumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem);
                this.globalSequencerDataInstance.setVideoItemAtPosition(i, newVideoItem);

                BitmapImage image = new BitmapImage();
                ImageBrush imagebrush = new ImageBrush();
                image.SetSource(newVideoItem.videoThumbnail);
                Button currentSequencerUIElement = (Button)this.FindName("_" + senderElementName);
                imagebrush.ImageSource = image;
                currentSequencerUIElement.Background = imagebrush;
            }
            else
            {
                // rootPage.NotifyUser("Operation cancelled.", NotifyType.ErrorMessage);
            }
        }

        private void sliderCurrentBPM_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            textCurrentBPM.Text = (sender as Slider).Value.ToString();
        }

        private void textCurrentBPM_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.globalSequencerControllerInstance.UpdateBPM(int.Parse(textCurrentBPM.Text));
        }

        private async void goExternal_Click(object sender, RoutedEventArgs e)
        {
            CoreApplicationView newView = CoreApplication.CreateNewView();
            int newViewId = 0;
            this.isRunningInFullScreenView = true;
            this.globalEventHandlerInstance.playerDispatcher = newView.Dispatcher;
            mediaPlayerElement.MediaPlayer.Pause();
            await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Frame frame = new Frame();
                frame.Navigate(typeof(FullScreenViewer), null);
                Window.Current.Content = frame;
                // You have to activate the window in order to show it later.
                Window.Current.Activate();

                newViewId = ApplicationView.GetForCurrentView().Id;
                var newWindowId = ApplicationView.GetApplicationViewIdForWindow(newView.CoreWindow);
            });
            bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
            statusTextControl.Text = "External view created";
        }
    }
}
