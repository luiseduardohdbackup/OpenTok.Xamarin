﻿using System;
using System.Diagnostics;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using OpenTok;

namespace OpenTok.Xamarin.Test
{
    public partial class MainViewController : UIViewController
    {
        OTSession _session;
        OTPublisher _publisher;
        OTSubscriber _subscriber;

        static readonly float widgetHeight = 240;
        static readonly float widgetWidth = 320;

        // *** Fill the following variables using your own Project info from the Dashboard  ***
        // *** https://dashboard.tokbox.com/projects  
        const string _apiKey = "45117962";
        const string _sessionId = @"2_MX40NTExNzk2Mn5-MTQxOTk1NzExOTM3OX5LZHdFTHJaOS8vYVdKbGRNMXZnRnB5Tm5-fg"; 
        const string _token = @"T1==cGFydG5lcl9pZD00NTExNzk2MiZzaWc9MTNjNzI1MzllM2Q4NWFiODAyZmNjZjg4NWE3MTcwMzU0ODdmNjAzYzpyb2xlPXB1Ymxpc2hlciZzZXNzaW9uX2lkPTJfTVg0ME5URXhOemsyTW41LU1UUXhPVGsxTnpFeE9UTTNPWDVMWkhkRlRISmFPUzh2WVZkS2JHUk5NWFpuUm5CNVRtNS1mZyZjcmVhdGVfdGltZT0xNDE5OTU3MTU4Jm5vbmNlPTAuMjkwMDY3Mjg4NzY3NjUyNjQmZXhwaXJlX3RpbWU9MTQyMjU0OTA4MA==";     

        bool _subscribeToSelf = true; // Change to false to subscribe to streams other than your own.

        public MainViewController() : base("MainViewController", null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            _session = new OTSession(_apiKey, _sessionId, new SessionDelegate(this));

            DoConnect();
        }

        private void DoConnect()
        {
            OTError error;

            _session.ConnectWithToken (_token, out error);

            if (error != null)
            {
                this.ShowAlert(error.Description);
            }
        }

        /**
         * Sets up an instance of OTPublisher to use with this session. OTPubilsher
         * binds to the device camera and microphone, and will provide A/V streams
         * to the OpenTok session.
         */
        private void DoPublish()
        {
            _publisher = new OTPublisher(new PubDelegate(this), UIDevice.CurrentDevice.Name);

            OTError error;

            _session.Publish(_publisher, out error);

            if (error != null)
            {
                this.ShowAlert(error.Description);
            }

            _publisher.View.Frame = new RectangleF(0, 0, widgetWidth, widgetHeight);

            View.AddSubview(_publisher.View);
        }

        /**
         * Cleans up the publisher and its view. At this point, the publisher should not
         * be attached to the session any more.
         */
        private void CleanupPublisher()
        {
            _publisher.View.RemoveFromSuperview();
            _publisher = null;

            // This is a good place to notify user that publishing has stopped
        }

        /**
         * Instantiates a subscriber for the given stream and asynchronously begins the
         * process to begin receiving A/V content for this stream. Unlike doPublish, 
         * this method does not add the subscriber to the view hierarchy. Instead, we 
         * add the subscriber only after it has connected and begins receiving data.
         */
        private void DoSubscribe(OTStream stream)
        {
            _subscriber = new OTSubscriber(stream, new SubDelegate(this));

            OTError error;

            _session.Subscribe(_subscriber, out error);

            if (error != null)
            {
                this.ShowAlert(error.Description);
            }
        }

        /**
         * Cleans the subscriber from the view hierarchy, if any.
         * NB: You do *not* have to call unsubscribe in your controller in response to
         * a streamDestroyed event. Any subscribers (or the publisher) for a stream will
         * be automatically removed from the session during cleanup of the stream.
         */
        private void CleanupSubscriber()
        {
            _subscriber.View.RemoveFromSuperview();
            _subscriber = null;
        }

        private class SessionDelegate : OTSessionDelegate
        {
            private MainViewController _this;

            public SessionDelegate(MainViewController This)
            {
                _this = This;
            }

            public override void DidConnect(OTSession session)
            {
                Debug.WriteLine("Did Connect {0}", session.SessionId);

                _this.DoPublish();
            }

            public override void DidFailWithError(OTSession session, OTError error)
            {
                var msg = string.Format("There was an error connecting to session {0}", session.SessionId);

                Debug.WriteLine("SessionDidFail ({0})", msg);

                _this.ShowAlert(msg);
            }

            public override void DidDisconnect(OTSession session)
            {
                var msg = string.Format("Session disconnected: ({0})", session.SessionId);

                Debug.WriteLine("DidDisconnect ({0})", msg);

                _this.ShowAlert(msg);
            }

            public override void ConnectionCreated(OTSession session, OTConnection connection)
            {
                Debug.WriteLine("ConnectionCreated ({0})", connection.ConnectionId);
            }

            public override void ConnectionDestroyed(OTSession session, OTConnection connection)
            {
                Debug.WriteLine("ConnectionDestroyed ({0})", connection.ConnectionId);

                if (_this._subscriber.Stream.Connection.ConnectionId == connection.ConnectionId)
                {
                    _this.CleanupSubscriber();
                }
            }

            public override void StreamCreated(OTSession session, OTStream stream)
            {
                Debug.WriteLine("StreamCreated ({0})", stream.StreamId);

                if(_this._subscriber == null && !_this._subscribeToSelf)
                {
                    _this.DoSubscribe(stream);
                }
            }

            public override void StreamDestroyed(OTSession session, OTStream stream)
            {
                Debug.WriteLine("StreamDestroyed ({0})", stream.StreamId);

                if (_this._subscriber.Stream.StreamId == stream.StreamId)
                {
                    _this.CleanupSubscriber();
                }
            }
        }

        private class SubDelegate : OTSubscriberDelegate
        {
            private MainViewController _this;

            public SubDelegate(MainViewController This)
            {
                _this = This;
            }

            public override void DidConnectToStream(OTSubscriber subscriber)
            {
                _this._subscriber.View.Frame = new RectangleF(0, widgetHeight, widgetWidth, widgetHeight);

                _this.View.AddSubview(_this._subscriber.View);
            }

            public override void DidFailWithError(OTSubscriber subscriber, OTError error)
            {
                Debug.WriteLine("Subscriber {0} DidFailWithError {1}", subscriber.Stream.StreamId, error);

                var msg = string.Format("There was an error subscribing to stream {0}", subscriber.Stream.StreamId);

                _this.ShowAlert(msg);
            }
        }

        private class PubDelegate : OTPublisherDelegate
        {
            private MainViewController _this;

            public PubDelegate(MainViewController This)
            {
                _this = This;
            }

            public override void DidFailWithError(OTPublisher publisher, OTError error)
            {
                Debug.WriteLine("Publisher DidFail {0}", error);

                _this.ShowAlert("There was an error publishing");

                _this.CleanupPublisher();
            }


            public override void StreamCreated(OTPublisher publisher, OTStream stream)
            {
                // If Subscribe To Self is true: Our own publisher is now visible to
                // all participants in the OpenTok session. We will attempt to subscribe to
                // our own stream. Expect to see a slight delay in the subscriber video and
                // an echo of the audio coming from the device microphone.
                if (_this._subscriber == null && _this._subscribeToSelf)
                {
                    _this.DoSubscribe(stream);
                }
            }
            public override void StreamDestroyed(OTPublisher publisher, OTStream stream)
            {
                if (_this._subscriber.Stream.StreamId == stream.StreamId)
                {
                    _this.CleanupSubscriber();
                }

                _this.CleanupPublisher();
            }
        }

        private void ShowAlert(string message)
        {
            var alert = new UIAlertView ("Alert", message, null, "Ok", null);

            alert.Show ();
        }
    }
}

