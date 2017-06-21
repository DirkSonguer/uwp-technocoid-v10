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

            // Get an instance to the event handler and subscribe to the SequencerPositionChanged event.
            this.globalEventHandlerInstance = GlobalEventHandler.GetInstance();
            this.globalEventHandlerInstance.SequencerPositionChanged += this.SequencerTrigger;

            // Get an instance to the sequencer data handler.
            this.globalSequencerDataInstance = GlobalSequencerData.GetInstance();
        }

        /// <summary>
        /// This is triggered if the sequencer progressed.
        /// </summary>
        /// <param name="currentSequencerPosition">The updated sequencer position as int.</param>
        /// <param name="e">PropertyChangedEventArgs.</param>
        private async void SequencerTrigger(object currentSequencerPosition, PropertyChangedEventArgs e)
        {
            await this.globalEventHandlerInstance.playerDispatcher.RunAsync(
             CoreDispatcherPriority.Normal, () =>
             {
                 // Get the video item for the current sequencer position.
                 VideoItem currentVideoItem = globalSequencerDataInstance.getVideoItemForPosition((int)currentSequencerPosition);

                 // Check if the current step has a video in it.
                 if (currentVideoItem.videoMediaSource != null)
                 {
                     // If so, then bind the video source to the video player and start playing the new source.
                     mediaPlayerElementFullScreen.MediaPlayer.Source = currentVideoItem.videoMediaSource;
                     mediaPlayerElementFullScreen.MediaPlayer.Play();
                 }
             });
        }
    }
}
