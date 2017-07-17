using System;
using System.ComponentModel;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace uwp_technocoid_v10
{
    /// <summary>
    /// The page containing the full screen media video player element.
    /// </summary>
    public sealed partial class FullScreenViewer : Page
    {
        // Access to all global classes.
        GlobalSequencerController globalSequencerControllerInstance;
        GlobalSequencerData globalSequencerDataInstance;
        GlobalEventHandler globalEventHandlerInstance;

        // Cache to hold the currently playing video for each sequencer track (= video channel).
        String[] currentlyActiveVideoFile = new String[4];

        /// <summary>
        /// Constructor.
        /// </summary>
        public FullScreenViewer()
        {
            this.InitializeComponent();
            
            // Hide the standard title bar by removing the title bar and making the buttons transparent.
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationView.GetForCurrentView().TitleBar.ButtonBackgroundColor = Colors.Transparent;
            ApplicationView.GetForCurrentView().TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // Get an instance to the sequencer controller.
            this.globalSequencerControllerInstance = GlobalSequencerController.GetInstance();

            // Get an instance to the event handler.
            this.globalEventHandlerInstance = GlobalEventHandler.GetInstance();

            // Bind the SequencerTrigger() function to the SequencerPositionChanged event.
            this.globalEventHandlerInstance.SequencerPositionChanged += this.SequencerTrigger;

            // Bind the StartSequencer() function to the CurrentlyPlayingChanged event.
            this.globalEventHandlerInstance.CurrentlyPlayingChanged += this.StartSequencer;

            // Bind the ChangeOpacity() function to the TrackOpacityChanged event.
            this.globalEventHandlerInstance.TrackOpacityChanged += this.ChangeOpacity;

            // Bind the ChangeOpacity() function to the TrackOpacityChanged event.
            this.globalEventHandlerInstance.TrackPlaybackRateChanged += this.ChangePlaybackRate;

            // Bind the ChangeOpacity() function to the TrackOpacityChanged event.
            this.globalEventHandlerInstance.FullscreenModeChanged += this.ChangeFullscreen;

            // Get an instance to the sequencer data handler.
            this.globalSequencerDataInstance = GlobalSequencerData.GetInstance();

            // Subscribe to window close event.
            ApplicationView.GetForCurrentView().Consolidated += PlayerWindowClosed;

            // Mute and elevate all media players.
            mediaPlayerElementTrack0.MediaPlayer.IsMuted = true;
            mediaPlayerElementTrack0.MediaPlayer.RealTimePlayback = true;
            mediaPlayerElementTrack1.MediaPlayer.IsMuted = true;
            mediaPlayerElementTrack1.MediaPlayer.RealTimePlayback = true;
            mediaPlayerElementTrack2.MediaPlayer.IsMuted = true;
            mediaPlayerElementTrack2.MediaPlayer.RealTimePlayback = true;
            mediaPlayerElementTrack3.MediaPlayer.IsMuted = true;
            mediaPlayerElementTrack3.MediaPlayer.RealTimePlayback = true;
        }

        /// <summary>
        /// Player window has been closed. Exit the app and thus close the main window as a result.
        /// </summary>
        /// <param name="sender">ApplicationView</param>
        /// <param name="e">ApplicationViewConsolidatedEventArgs</param>
        private void PlayerWindowClosed(object sender, object e)
        {
            Application.Current.Exit();
        }

        /// <summary>
        /// This is triggered if the sequencer progressed.
        /// </summary>
        /// <param name="currentSequencerPosition">The updated sequencer position as int</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private async void SequencerTrigger(object currentSequencerPosition, PropertyChangedEventArgs e)
        {
            // Get the correct thread for the media player UI.
            await this.globalEventHandlerInstance.playerDispatcher.RunAsync(
             CoreDispatcherPriority.Normal, () =>
             {
                 // Iterate through all 4 media players.
                 for (int i = 0; i < 4; i++)
                 {
                     // Get the media player element for the current track.
                     MediaPlayerElement currentMediaElement = (MediaPlayerElement)this.FindName("mediaPlayerElementTrack" + i.ToString());

                     // Get the video item for the current sequencer position.
                     SequencerSlot currentSlotItem = globalSequencerDataInstance.getSlotAtPosition(i, (int)currentSequencerPosition);

                     // Check if the current step has a video in it.
                     if ((currentSlotItem.videoMediaSource != null) && (currentSlotItem.active))
                     {
                         // CHeck if the video in the next step is another video than the one currently playing.
                         if ((this.currentlyActiveVideoFile[i] == null) || (this.currentlyActiveVideoFile[i] != currentSlotItem.videoFile.Name))
                         {
                             // If so, then bind the video source to the video player and start playing the new source.
                             currentMediaElement.MediaPlayer.Source = currentSlotItem.videoMediaSource;
                             currentMediaElement.MediaPlayer.Play();
                             this.currentlyActiveVideoFile[i] = currentSlotItem.videoFile.Name;
                         }
                         else
                         {
                             // If it's the same video, just reset the video position and start playing.
                             currentMediaElement.MediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
                             currentMediaElement.MediaPlayer.Play();
                         }
                     }
                 }
             });
        }

        /// <summary>
        /// Start and stop the sequencer based on the parameter given.
        /// Note that this subscribes to the CurrentlyPlayingChanged in the event handler.
        /// </summary>
        /// <param name="isCurrentlyPlaying">This starts / stops the timer based on a bool</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private async void StartSequencer(object isCurrentlyPlaying, PropertyChangedEventArgs e)
        {
            // Get the correct thread for the media player UI.
            await this.globalEventHandlerInstance.playerDispatcher.RunAsync(
             CoreDispatcherPriority.Normal, () =>
             {
                 if (!(bool)isCurrentlyPlaying)
                 {
                     // Iterate through all video players.
                     for (int i = 0; i < 4; i++)
                     {
                         // Get the media player element for the current track and stop it.
                         MediaPlayerElement currentMediaElement = (MediaPlayerElement)this.FindName("mediaPlayerElementTrack" + i.ToString());
                         currentMediaElement.MediaPlayer.Pause();
                     }
                 }
             });
        }

        /// <summary>
        /// A sequencer track changed its opacity.
        /// </summary>
        /// <param name="sequencerTrack">The sequencer track that changed its opacity</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private async void ChangeOpacity(object sequencerTrack, PropertyChangedEventArgs e)
        {
            // Get the track to change opacity for.
            double newTrackOpacity = this.globalSequencerDataInstance.getOpacityForTrack((int)sequencerTrack);

            // Get the correct thread for the media player UI.
            await this.globalEventHandlerInstance.playerDispatcher.RunAsync(
             CoreDispatcherPriority.Normal, () =>
             {
                 // Get the media player for the respective track and change its opacity.
                 MediaPlayerElement currentMediaPlayerUIElement = (MediaPlayerElement)this.FindName("mediaPlayerElementTrack" + (int)sequencerTrack);
                 if (currentMediaPlayerUIElement != null)
                 {
                     currentMediaPlayerUIElement.Opacity = newTrackOpacity;
                 }
             });
        }

        /// <summary>
        /// A sequencer track changed its playback rate.
        /// </summary>
        /// <param name="sequencerTrack">The sequencer track that changed its playback rate</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private async void ChangePlaybackRate(object sequencerTrack, PropertyChangedEventArgs e)
        {
            // Get the track to change opacity for.
            double newTrackPlaybackRate = this.globalSequencerDataInstance.getPlaybackRateForTrack((int)sequencerTrack);

            // Get the correct thread for the media player UI.
            await this.globalEventHandlerInstance.playerDispatcher.RunAsync(
             CoreDispatcherPriority.Normal, () =>
             {
                 // Get the media player for the respective track and change its opacity.
                 MediaPlayerElement currentMediaPlayerUIElement = (MediaPlayerElement)this.FindName("mediaPlayerElementTrack" + (int)sequencerTrack);
                 if (currentMediaPlayerUIElement != null)
                 {
                     currentMediaPlayerUIElement.MediaPlayer.PlaybackSession.PlaybackRate = newTrackPlaybackRate;
                 }
             });
        }

        /// <summary>
        /// The user wants to toggle the fullscreen mode.
        /// TODO: Implement feature.
        /// </summary>
        /// <param name="requestedFullscreenMode">Fullscreen toggle flag</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private async void ChangeFullscreen(object requestedFullscreenMode, PropertyChangedEventArgs e)
        {
            // Get the correct thread for the media player UI.
            await this.globalEventHandlerInstance.playerDispatcher.RunAsync(
             CoreDispatcherPriority.Normal, () =>
             {
                 /*
                 var view = ApplicationView.GetForCurrentView();

                 var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
                 var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
                 var size = new Size(bounds.Width * scaleFactor, bounds.Height * scaleFactor);

                 size.Height -= 550;
                 size.Width -= 500;

                 if (view.TryResizeView(size))
                 {

                 }
                 else
                 {

                 }
                 */
             });
        }
    }
}
