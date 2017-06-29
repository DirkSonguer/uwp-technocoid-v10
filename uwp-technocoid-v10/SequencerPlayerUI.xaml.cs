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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace uwp_technocoid_v10
{
    public sealed partial class SequencerPlayerUI : UserControl
    {
        // Access to all global classes.
        GlobalSequencerController globalSequencerControllerInstance;
        GlobalSequencerData globalSequencerDataInstance;
        GlobalEventHandler globalEventHandlerInstance;

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
        /// TODO!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        private void resetSequencer(object sender, RoutedEventArgs e)
        {
            this.globalEventHandlerInstance.NotifyCurrentlyPlayingChanged(false);
            this.globalEventHandlerInstance.NotifyCurrentlyPlayingChanged(true);
        }
    }
}
