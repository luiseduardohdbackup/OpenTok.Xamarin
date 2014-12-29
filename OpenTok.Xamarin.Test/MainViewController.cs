﻿using System;
using System.Diagnostics;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

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
        // ***                   https://dashboard.tokbox.com/projects  
        const string kApiKey = "";

        // Replace with your generated session ID
        const string kSessionId = @""; 
        // Replace with your generated token (use the Dashboard or an OpenTok server-side library)
        const string kToken = @"";     


        bool subscribeToSelf = true; // Change to false to subscribe to streams other than your own.

        public MainViewController() : base("MainViewController", null)
        {
        }

        public override void DidReceiveMemoryWarning()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning();
			
            // Release any cached data, images, etc that aren't in use.
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
			
            _session = new OTSession(kApiKey, kSessionId, new SessionDelegate(this));

            DoConnect();
        }

        private void UpdateSubscriber()
        {
            foreach(var streamId in _session.Streams)
            {
                var stream = (OTStream)streamId.Value;

                if (!Equals(stream.Connection.ConnectionId, _session.Connection.ConnectionId))
                {
                    _subscriber = new OTSubscriber(stream, new SubDelegate(this));
                    break;
                }
            }
        }
        private void DoConnect()
        {
            OTError error = null;

            _session.ConnectWithToken (kToken, error);

            if (error != null)
            {
                this.ShowAlert(error.Description);
            }
        }

        private void ShowAlert(string message)
        {
            var alertView = new UIAlertView("Message from video session",
                message,
                new AlertViewDelegate(),
                "OK");

            alertView.Show();
        }

        private void DoPublish()
        {
            _publisher = new OTPublisher(new PubDelegate(this));
            _publisher.Name = UIDevice.CurrentDevice.Name;

            OTError error = null;

            _session.Publish(_publisher, error);

            if (error != null)
            {
                this.ShowAlert(error.Description);
            }

            View.AddSubview(_publisher.View);
            _publisher.View.Frame = new RectangleF(0, 0, widgetWidth, widgetHeight);
        }

        private class AlertViewDelegate : UIAlertViewDelegate
        {
            public override void Clicked(UIAlertView alertview, int buttonIndex)
            {

            }
            public override void Canceled(UIAlertView alertView)
            {

            }
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

            public override void DidFail(OTSession session, OTError error)
            {
                Debug.WriteLine("SessionDidFail");
                var msg = string.Format("There was an error connecting to session {0}", session.SessionId);
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
                Debug.WriteLine("DidCreateConnection ({0})", connection.ConnectionId);
            }

            public override void ConnectionDestroyed(OTSession session, OTConnection connection)
            {
                Debug.WriteLine("DidDropConnection ({0})", connection.ConnectionId);
            }

            public override void StreamCreated(OTSession session, OTStream stream)
            {
                Debug.WriteLine("DidReceiveStream ({0})", stream.StreamId);

                // See the declaration of subscribeToSelf above
                if((_this.subscribeToSelf && Equals(stream.Connection.ConnectionId, _this._session.Connection.ConnectionId))
                    ||
                    (!_this.subscribeToSelf && !Equals(stream.Connection.ConnectionId, _this._session.Connection.ConnectionId))
                )
                {
                    if(_this._subscriber == null)
                    {
                        _this._subscriber = new OTSubscriber(stream, new SubDelegate(_this));
                    }
                }
            }

            public override void StreamDestroyed(OTSession session, OTStream stream)
            {
                Debug.WriteLine("DidDropStream ({0})", stream.StreamId);
                if(_this._subscriber != null)
                    Debug.WriteLine("_subscriber.Stream.StreamId ({0})", _this._subscriber.Stream.StreamId);

                if(!_this.subscribeToSelf 
                    && _this._subscriber != null
                    && Equals(_this._subscriber.Stream.StreamId, stream.StreamId))
                {
                    _this._subscriber.Dispose();
                    _this._subscriber = null;
                    _this.UpdateSubscriber();
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


            public override void DidChangeVideoDimensions(OTStream stream, SizeF dimensions)
            {

            }

            public override void DidConnectToStream(OTSubscriber subscriber)
            {

            }

            public override void DidFail(OTSubscriber subscriber, OTError error)
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

            public override void DidChangeCameraPosition(OTPublisher publisher, MonoTouch.AVFoundation.AVCaptureDevicePosition cameraPosition)
            {

            }

            public override void DidFail(OTPublisher publisher, OTError error)
            {
                Debug.WriteLine("Publisher DidFail {0}", error);
                _this.ShowAlert("There was an error publishing");
            }


            public override void StreamCreated(OTPublisher publisher, OTStream stream)
            {

            }
            public override void StreamDestroyed(OTPublisher publisher, OTStream stream)
            {

            }
        }
    }
}

