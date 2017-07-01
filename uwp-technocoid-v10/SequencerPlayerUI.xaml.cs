using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace uwp_technocoid_v10
{
    public sealed partial class SequencerPlayerUI : UserControl
    {
        // Access to all global classes.
        GlobalSequencerController globalSequencerControllerInstance;
        GlobalSequencerData globalSequencerDataInstance;
        GlobalEventHandler globalEventHandlerInstance;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SequencerPlayerUI()
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
        /// User pressed the play button.
        /// Start / stop the sequencer and change the button icon accordingly.
        /// </summary>
        /// <param name="sender">The button for the play functionality as Button.</param>
        /// <param name="e">RoutedEventArgs</param>
        private void startSequencer(object sender, RoutedEventArgs e)
        {
            if ("\uE102" == startSequencerButton.Content.ToString())
            {
                startSequencerButton.Content = "\uE103";
                this.globalEventHandlerInstance.NotifyCurrentlyPlayingChanged(true);
            }
            else
            {
                startSequencerButton.Content = "\uE102";
                this.globalEventHandlerInstance.NotifyCurrentlyPlayingChanged(false);
            }
        }

        /// <summary>
        /// User pressed reset button.
        /// This will reset the sequencer, effectively restarting it from position 0.
        /// </summary>
        /// <param name="sender">The button for the reset functionality as Button.</param>
        /// <param name="e">RoutedEventArgs</param>
        private void resetSequencer(object sender, RoutedEventArgs e)
        {
            this.globalEventHandlerInstance.NotifyCurrentlyPlayingChanged(false);
            this.globalEventHandlerInstance.NotifyCurrentlyPlayingChanged(true);
        }
    }
}
