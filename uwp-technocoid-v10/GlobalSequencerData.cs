//---------------------------------------------------------------------------//
// Technocoid v10
// link: https://github.com/DirkSonguer/uwp-technocoid-v10
// authors: Dirk Songuer
//
// You should have received a copy of the MIT License
// along with this program called LICENSE.md
// If not, see <https://choosealicense.com/licenses/mit/>
//---------------------------------------------------------------------------//
using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace uwp_technocoid_v10
{
    /// <summary>
    /// This is a video item, representing one sequencer step.
    /// </summary>
    class SequencerSlot
    {
        // Flag if the slot is active or not.
        public bool active { get; set; }

        // MediaSource for the video. This will be handed to the MediaPlayers
        // to play.
        public Windows.Media.Core.MediaSource videoMediaSource { get; set; }

        // Thumbnail for the video. This will be shown on the controller UI.
        public StorageItemThumbnail thumbnail { get; set; }

        // Original file of the video. This is used when reloading all contents if
        // the media player is switched.
        public StorageFile videoFile { get; set; }
    }

    /// <summary>
    /// A sequencer track, consisting of multiple video items as steps.
    /// This assumes we will have multiple tracks at some point.
    /// </summary>
    class SequencerTrack
    {
        // The current opacity of the track player.
        public double opacity = 1.0;

        // The current opacity of the track player.
        public double playbackRate = 1.0;

        // Flag if the sequencer is currently selected.
        public bool selected = false;

        // The video items in one track.
        public SequencerSlot[] slots = new SequencerSlot[8];
    }

    /// <summary>
    /// Class that manages all data the sequencer uses.
    /// Mostly this is about the videos associated with the individual
    /// sequencer steps.
    /// </summary>
    class GlobalSequencerData
    {
        // This class is a singleton, allowing every UI class to access the same data.
        private static readonly GlobalSequencerData sequencerDataInstance = new GlobalSequencerData();
        public static GlobalSequencerData GetInstance()
        {
            return sequencerDataInstance;
        }

        // Instance for the first track of the sequencer.
        // Currently we don't have more than one track, but this will
        // change in the future.
        private SequencerTrack[] tracks = new SequencerTrack[4];

        // Defines the currently active track.
        // This is only used for use with MIDI, so on initialization no track is active.
        public int currentlyActiveTrack = -1;

        /// <summary>
        /// Constructor.
        /// </summary>
        GlobalSequencerData()
        {
            // Initialize tracks.
            tracks[0] = new SequencerTrack();
            tracks[1] = new SequencerTrack();
            tracks[2] = new SequencerTrack();
            tracks[3] = new SequencerTrack();

            // Initialize slots in tracks.
            for (int i = 0; i < 8; i++)
            {
                tracks[0].slots[i] = new SequencerSlot();
                tracks[1].slots[i] = new SequencerSlot();
                tracks[2].slots[i] = new SequencerSlot();
                tracks[3].slots[i] = new SequencerSlot();
            }
        }

        /// <summary>
        /// Stores a new video item into a given sequencer position.
        /// </summary>
        /// <param name="sequencerPosition">The position on the sequencer board to associate the video with</param>
        /// <param name="newVideoItem">The video item to store</param>
        public void setSlotAtPosition(int sequencerTrack, int sequencerPosition, SequencerSlot newVideoItem)
        {
            tracks[sequencerTrack].slots[sequencerPosition] = newVideoItem;
        }

        /// <summary>
        /// Gets the video item associated with the given sequencer position.
        /// </summary>
        /// <param name="sequencerPosition">The position on the sequencer board to associate the video with</param>
        /// <returns></returns>
        public SequencerSlot getSlotAtPosition(int sequencerTrack, int sequencerPosition)
        {
            return tracks[sequencerTrack].slots[sequencerPosition];
        }

        /// <summary>
        /// Set the opacity for the given track.
        /// </summary>
        /// <param name="sequencerTrack">The track to change the opacity for</param>
        /// <param name="newTrackOpacity">The new opacity value</param>
        public void setOpacityForTrack(int sequencerTrack, double newTrackOpacity)
        {
            if ((newTrackOpacity >= 0.0) && (newTrackOpacity <= 1.0))
            {
                tracks[sequencerTrack].opacity = newTrackOpacity;
            }
        }

        /// <summary>
        /// Get the opacity for the given track.
        /// </summary>
        /// <param name="sequencerTrack">Track to get the opacity value for</param>
        /// <returns></returns>
        public double getOpacityForTrack(int sequencerTrack)
        {
            return tracks[sequencerTrack].opacity;
        }

        /// <summary>
        /// Set the playback rate for the given track.
        /// </summary>
        /// <param name="sequencerTrack">The track to change the opacity for</param>
        /// <param name="newTrackOpacity">The new opacity value</param>
        public void setPlaybackRateForTrack(int sequencerTrack, double newTrackPlaybackRate)
        {
            // Playback rates can only be set within specific boundaries.
            if ((newTrackPlaybackRate >= 0.25) && (newTrackPlaybackRate <= 3.0))
            {
                tracks[sequencerTrack].playbackRate = newTrackPlaybackRate;
            }
        }

        /// <summary>
        /// Get the playback rate for the given track.
        /// </summary>
        /// <param name="sequencerTrack">Track to get the opacity value for</param>
        /// <returns></returns>
        public double getPlaybackRateForTrack(int sequencerTrack)
        {
            return tracks[sequencerTrack].playbackRate;
        }
    }
}
