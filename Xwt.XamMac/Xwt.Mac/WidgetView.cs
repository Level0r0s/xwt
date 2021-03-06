// 
// WidgetView.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//       Hywel Thomas <hywel.w.thomas@gmail.com>
// 
// Copyright (c) 2011 Xamarin Inc
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using AppKit;
using CoreGraphics;
using Xwt.Backends;

namespace Xwt.Mac
{
	/// <summary>
	/// Handles Events generated by NSView and TrackingArea
	/// and dispatches these using context and eventSink
	/// </summary>
	public class WidgetView: NSView, IViewObject
	{
		IWidgetEventSink eventSink;
		protected ApplicationContext context;

		NSTrackingArea trackingArea;	// Captures Mouse Entered, Exited, and Moved events

		public WidgetView (IWidgetEventSink eventSink, ApplicationContext context)
		{
			this.context = context;
			this.eventSink = eventSink;
			DrawsBackground = true;
		}

		public ViewBackend Backend { get; set; }

		public NSView View {
			get { return this; }
		}

		public bool DrawsBackground { get; set; }

		public override bool IsFlipped {
			get {
				return true;
			}
		}

		public override bool AcceptsFirstResponder ()
		{
			return Backend.CanGetFocus;
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			if (DrawsBackground) {
				CGContext ctx = NSGraphicsContext.CurrentContext.GraphicsPort;

				//fill BackgroundColor
				ctx.SetFillColor (Backend.Frontend.BackgroundColor.ToCGColor ());
				ctx.FillRect (Bounds);
			}
		}

		public override void UpdateTrackingAreas ()
		{
			if (trackingArea != null) {
				RemoveTrackingArea (trackingArea);
				trackingArea.Dispose ();
			}
			CGRect viewBounds = this.Bounds;
			var options = NSTrackingAreaOptions.MouseMoved | NSTrackingAreaOptions.ActiveInKeyWindow | NSTrackingAreaOptions.MouseEnteredAndExited;
			trackingArea = new NSTrackingArea (viewBounds, options, this, null);
			AddTrackingArea (trackingArea);
		}

		public override void RightMouseDown (NSEvent theEvent)
		{
			CGPoint p = this.ConvertPointFromEvent(theEvent);
			if (!Bounds.Contains(p))
				return;
			ButtonEventArgs args = new ButtonEventArgs ();
			args.X = p.X;
			args.Y = p.Y;
			args.Button = PointerButton.Right;
			context.InvokeUserCode (delegate {
				eventSink.OnButtonPressed (args);
			});
		}

		public override void RightMouseUp (NSEvent theEvent)
		{
			CGPoint p = this.ConvertPointFromEvent(theEvent);
			if (!Bounds.Contains(p))
				return;
			ButtonEventArgs args = new ButtonEventArgs ();
			args.X = p.X;
			args.Y = p.Y;
			args.Button = PointerButton.Right;
			context.InvokeUserCode (delegate {
				eventSink.OnButtonReleased (args);
			});
		}

		public override void MouseDown (NSEvent theEvent)
		{
			CGPoint p = this.ConvertPointFromEvent(theEvent);
			if (!Bounds.Contains(p))
				return;
			ButtonEventArgs args = new ButtonEventArgs ();
			args.X = p.X;
			args.Y = p.Y;
			args.Button = PointerButton.Left;
			context.InvokeUserCode (delegate {
				eventSink.OnButtonPressed (args);
			});
		}

		public override void MouseUp (NSEvent theEvent)
		{
			CGPoint p = this.ConvertPointFromEvent(theEvent);
			if (!Bounds.Contains(p))
				return;
			ButtonEventArgs args = new ButtonEventArgs ();
			args.X = p.X;
			args.Y = p.Y;
			args.Button = (PointerButton) (int)theEvent.ButtonNumber + 1;
			context.InvokeUserCode (delegate {
				eventSink.OnButtonReleased (args);
			});
		}

		public override void MouseEntered (NSEvent theEvent)
		{
			context.InvokeUserCode (delegate {
				eventSink.OnMouseEntered ();
			});
		}

		public override void MouseExited (NSEvent theEvent)
		{
			context.InvokeUserCode (delegate {
				eventSink.OnMouseExited ();
			});
		}

		public override void MouseMoved (NSEvent theEvent)
		{
			CGPoint p = this.ConvertPointFromEvent(theEvent);
			if (!Bounds.Contains(p))
				return;
			MouseMovedEventArgs args = new MouseMovedEventArgs ((long) TimeSpan.FromSeconds (theEvent.Timestamp).TotalMilliseconds, p.X, p.Y);
			context.InvokeUserCode (delegate {
				eventSink.OnMouseMoved (args);
			});
		}

		public override void MouseDragged (NSEvent theEvent)
		{
			CGPoint p = this.ConvertPointFromEvent(theEvent);
			if (!Bounds.Contains(p))
				return;
			MouseMovedEventArgs args = new MouseMovedEventArgs ((long) TimeSpan.FromSeconds (theEvent.Timestamp).TotalMilliseconds, p.X, p.Y);
			context.InvokeUserCode (delegate {
				eventSink.OnMouseMoved (args);
			});
		}

		public override void KeyDown (NSEvent theEvent)
		{
			var keyArgs = theEvent.ToXwtKeyEventArgs ();
			context.InvokeUserCode (delegate {
				eventSink.OnKeyPressed (keyArgs);
			});
			if (keyArgs.Handled)
				return;

			var textArgs = new TextInputEventArgs (theEvent.Characters);
			if (!String.IsNullOrEmpty(theEvent.Characters))
				context.InvokeUserCode (delegate {
					eventSink.OnTextInput (textArgs);
				});
			if (textArgs.Handled)
				return;

			base.KeyDown (theEvent);
		}

		public override void KeyUp (NSEvent theEvent)
		{
			var keyArgs = theEvent.ToXwtKeyEventArgs ();
			context.InvokeUserCode (delegate {
				eventSink.OnKeyReleased (keyArgs);
			});
			if (!keyArgs.Handled)
				base.KeyUp (theEvent);
		}

		public override void SetFrameSize (CGSize newSize)
		{
			bool changed = !newSize.Equals (Frame.Size);
			base.SetFrameSize (newSize);
			if (changed) {
				context.InvokeUserCode (delegate {
					eventSink.OnBoundsChanged ();
				});
			}
		}

		public override void ResetCursorRects ()
		{
			base.ResetCursorRects ();
			if (Backend.Cursor != null)
				AddCursorRect (Bounds, Backend.Cursor);
		}
	}
}

