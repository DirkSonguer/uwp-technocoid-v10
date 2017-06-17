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


namespace uwp_technocoid_v10
{
    // This is a video item, representing one sequencer step
    public class VideoItem
    {
        // MediaSource for the video. This will be handed to the MediaPlayer
        public MediaSource videoMediaSource { get; set; }

        // Thumbnail for the video. This will be shown on the step button
        public StorageItemThumbnail videoThumbnail { get; set; }
    }

    // A sequencer track, assuming we will have multiple tracks at some point
    public class SequencerTrack
    {
        // The video items in one track
        public VideoItem[] videoItems = new VideoItem[12];
    }

    public sealed partial class MainPage : Page
    {
        // The main sequencer track object
        SequencerTrack uiSequencerData = new SequencerTrack();

        // The master timer, later running as a task in the thread pool
        static ThreadPoolTimer masterTimer;

        // The currently selected BPM count in different units
        public int currentBPM = 60;
        public int currentBPMinMS = 1000;
        public TimeSpan currentBPMinSpan = TimeSpan.FromSeconds(1);

        // The current sequencer cursor position
        public int currentSequencerPosition = 0;

        // Flag if the sequencer is currently running
        public bool isSequencerRunning = false;

        // Constructor of the main page
        public MainPage()
        {
            this.InitializeComponent();

            CreateUI();
            Window.Current.SizeChanged += UpdateUI;
        }

        // This will initially create the 
        public void CreateUI()
        {
            var newWindowBounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var newWindowScaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var newWindowSize = new Size(newWindowBounds.Width * newWindowScaleFactor, newWindowBounds.Height * newWindowScaleFactor);
            var newButtonSize = Convert.ToInt32(newWindowSize.Width / (12 * newWindowScaleFactor));

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

                textCurrentBPM.Text = "60";
                sliderCurrentBPM.Value = 60;

                uiSequencerData.videoItems[i] = new VideoItem();
            }
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


        private async void SequencerMainLoop(ThreadPoolTimer timer)
        {
            var dispatcher = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;
            await dispatcher.RunAsync(
             CoreDispatcherPriority.Normal, () =>
             {
                 int lastSequencerPosition = this.currentSequencerPosition;
                 this.currentSequencerPosition += 1;
                 if (this.currentSequencerPosition > 11) this.currentSequencerPosition = 0;

                 statusTextControl.Text = "Sequencer is at step " + this.currentSequencerPosition.ToString();

                 Button currentSequencerUIElement = (Button)this.FindName("_" + this.currentSequencerPosition.ToString());
                 statusTextControl.Text += " and button " + currentSequencerUIElement.Name;
                 currentSequencerUIElement.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Teal);

                 Button lastSequencerUIElement = (Button)this.FindName("_" + lastSequencerPosition.ToString());
                 lastSequencerUIElement.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Gray);

                 if (this.uiSequencerData.videoItems[this.currentSequencerPosition].videoMediaSource != null)
                 {
                     statusTextControl.Text += ", playing a media element";
                     mediaPlayerElement.MediaPlayer.Source = this.uiSequencerData.videoItems[this.currentSequencerPosition].videoMediaSource;
                     mediaPlayerElement.MediaPlayer.Play();
                 }
                 else
                 {
                     statusTextControl.Text += ", pausing";
                 }
             });
        }

        private void startSequencer_Click(object sender, RoutedEventArgs e)
        {
            if (this.isSequencerRunning)
            {
                this.currentSequencerPosition = 0;
                masterTimer.Cancel();

                for (int i = 0; i < 12; i++)
                {
                    Button addedButtonElement = (Button)this.FindName("_" + i.ToString());
                    addedButtonElement.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Gray);
                }

                startSequencer.Content = "\uE102";

                mediaPlayerElement.MediaPlayer.Pause();

                this.isSequencerRunning = false;
            }
            else
            {
                this.currentSequencerPosition = 0;
                masterTimer = ThreadPoolTimer.CreatePeriodicTimer(SequencerMainLoop, this.currentBPMinSpan);

                startSequencer.Content = "\uE103";

                mediaPlayerElement.MediaPlayer.Play();
                mediaPlayerElement.MediaPlayer.IsLoopingEnabled = true;

                this.isSequencerRunning = true;
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

                this.uiSequencerData.videoItems[i].videoMediaSource = MediaSource.CreateFromStorageFile(file);
                this.uiSequencerData.videoItems[i].videoThumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem);

                BitmapImage image = new BitmapImage();
                ImageBrush imagebrush = new ImageBrush();
                image.SetSource(this.uiSequencerData.videoItems[i].videoThumbnail);
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
            this.currentBPM = int.Parse(textCurrentBPM.Text);
            this.currentBPMinMS = Convert.ToInt32(60000 / this.currentBPM);
            this.currentBPMinSpan = new TimeSpan(0, 0, 0, 0, this.currentBPMinMS);

            if (this.isSequencerRunning)
            {
                masterTimer.Cancel();
                masterTimer = ThreadPoolTimer.CreatePeriodicTimer(SequencerMainLoop, this.currentBPMinSpan);
            }
        }
    }
}
