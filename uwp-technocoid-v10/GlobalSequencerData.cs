using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        // The video items in one track
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
        private SequencerTrack[] tracks = new SequencerTrack[2];

        /// <summary>
        /// Constructor.
        /// </summary>
        GlobalSequencerData()
        {
            tracks[0] = new SequencerTrack();
            tracks[1] = new SequencerTrack();

            for (int i = 0; i < 8; i++)
            {
                tracks[0].slots[i] = new SequencerSlot();
                tracks[1].slots[i] = new SequencerSlot();
            }
        }

        /// <summary>
        /// Stores a new video item into a given sequencer position.
        /// </summary>
        /// <param name="sequencerPosition">The position on the sequencer board to associate the video with.</param>
        /// <param name="newVideoItem">The video item to store.</param>
        public void setSlotAtPosition(int sequencerTrack, int sequencerPosition, SequencerSlot newVideoItem)
        {
            tracks[sequencerTrack].slots[sequencerPosition] = newVideoItem;
        }

        /// <summary>
        /// Gets the video item associated with the given sequencer position.
        /// </summary>
        /// <param name="sequencerPosition">The position on the sequencer board to associate the video with.</param>
        /// <returns></returns>
        public SequencerSlot getSlotAtPosition(int sequencerTrack, int sequencerPosition)
        {
            return tracks[sequencerTrack].slots[sequencerPosition];
        }

    }
}
