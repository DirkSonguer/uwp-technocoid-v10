using System;
using System.ComponentModel;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

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

        /// <summary>
        /// Constructor.
        /// </summary>
        public FullScreenViewer()
        {
            this.InitializeComponent();

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

            // Get an instance to the sequencer data handler.
            this.globalSequencerDataInstance = GlobalSequencerData.GetInstance();

            // Mute all media players.
            mediaPlayerElementTrack0.MediaPlayer.IsMuted = true;
            mediaPlayerElementTrack1.MediaPlayer.IsMuted = true;
            mediaPlayerElementTrack2.MediaPlayer.IsMuted = true;
            mediaPlayerElementTrack3.MediaPlayer.IsMuted = true;
        }

        /// <summary>
        /// This is triggered if the sequencer progressed.
        /// </summary>
        /// <param name="currentSequencerPosition">The updated sequencer position as int.</param>
        /// <param name="e">PropertyChangedEventArgs.</param>
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
                         // If so, then bind the video source to the video player and start playing the new source.
                         currentMediaElement.MediaPlayer.Source = currentSlotItem.videoMediaSource;
                         currentMediaElement.MediaPlayer.Play();
                     }
                 }
             });
        }

        /// <summary>
        /// Start and stop the sequencer based on the parameter given.
        /// Note that this subscribes to the CurrentlyPlayingChanged in the event handler.
        /// </summary>
        /// <param name="isCurrentlyPlaying">This starts / stops the timer based on a bool.</param>
        /// <param name="e">PropertyChangedEventArgs.</param>
        private async void StartSequencer(object isCurrentlyPlaying, PropertyChangedEventArgs e)
        {
            // Get the correct thread for the media player UI.
            await this.globalEventHandlerInstance.playerDispatcher.RunAsync(
             CoreDispatcherPriority.Normal, () =>
             {
                 // Originally we started / stopped the players here,
                 // however this is done via the sequencer triggers now.
             });
        }

        /// <summary>
        /// A sequencer track changed its opacity.
        /// </summary>
        /// <param name="sequencerTrack">The sequencer track that changed its opacity.</param>
        /// <param name="e">PropertyChangedEventArgs.</param>
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
    }
}
