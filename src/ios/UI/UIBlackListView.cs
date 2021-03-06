﻿using MonoTouch.UIKit;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using RedBit.CCAdmin;
using AlertView;
using RedBit;

namespace BackChannel
{
	public class UIBlackListView : UIView
	{

		public UIBlackListView() { }

		public override RectangleF Frame
		{
			get
			{
				return base.Frame;
			}
			set
			{
				base.Frame = value;
				// layout the controls
				Layout();
			}
		}

		private BlackListViewSource ViewSource { get{return this._tableView.Source as BlackListViewSource;}}

		public Action<BlacklistItem> ItemClicked {get;set;}

		private UIBlackListTableView _tableView;
	
		void Layout()
		{
			if (_tableView == null) {
				_tableView = new UIBlackListTableView ();
				var t = this.Frame;
				_tableView.Frame = new RectangleF (0, 0, UIScreen.MainScreen.ApplicationFrame.Width, UIScreen.MainScreen.ApplicationFrame.Height - 44); // 44 is height of nav bar
				//				_tableView.ScrollEnabled = false;
				_tableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
				_tableView.BackgroundColor = UIColor.FromRGB (242, 242, 242); //UIColor.FromPatternImage (UIImage.FromFile("images/login/loginBg.png"));

				// add a swipe handler for options
				var s = new UISwipeGestureRecognizer (SwipeHandler);
				s.Direction = UISwipeGestureRecognizerDirection.Left;
				_tableView.AddGestureRecognizer (s);
				s = new UISwipeGestureRecognizer (SwipeHandler);
				s.Direction = UISwipeGestureRecognizerDirection.Right;
				_tableView.AddGestureRecognizer (s);

				// wure up the remove row
				_tableView.DeleteItem = (item) => {

					var alert = MBAlertView.AlertWithBody ("Are you sure you want to delete this black list item? You cannot undo this.", "No", null);
					alert.AddButtonWithText ("Yes", MBAlertViewItemType.Destructive, () => {
						// call server to delete the tweet
						Helper.Default.ShowHud("deleting ...");
						Api.Default.DeleteBlackListItem (item.Value, (result) => {
							this.InvokeOnMainThread(()=>{
								if (result.Result == "ok") {
									var index = this.ViewSource.RemoveItem (item);
									this._tableView.DeleteRows (new MonoTouch.Foundation.NSIndexPath[]{MonoTouch.Foundation.NSIndexPath.Create (0,index)}, UITableViewRowAnimation.Fade);
									Helper.Default.HideHud();
								}
								else{
									Helper.Default.HideHud("Could not delete, try again.");
								}
							});
						});
					});
					alert.AddToDisplayQueue ();
				};

				// add to view
				this.AddSubview (_tableView);
			} 
   		}

		private UIView _lastcell = null;
		private void SwipeHandler(UISwipeGestureRecognizer args){
			if(args.State == UIGestureRecognizerState.Ended){
				// find the cell
				var swipeLocation = args.LocationInView(_tableView);
				var index = _tableView.IndexPathForRowAtPoint(swipeLocation);
				var cell = _tableView.CellAt(index);

				var view = ((BlackListTableViewCell)cell).ContentView2;
				if (args.Direction == UISwipeGestureRecognizerDirection.Left) {
					if(_lastcell != null){
						_lastcell.Animate2 (0.2, ()=>AnimateCellRight(_lastcell), ()=>AnimateCellLeft(view));
					}
					_lastcell = view;
					view.Animate2 (0.2, ()=>AnimateCellLeft(view));
				}
				else if(args.Direction == UISwipeGestureRecognizerDirection.Right){
					_lastcell = null;
					view.Animate2 (0.2, ()=>AnimateCellRight(view));
				}
			}
		}

		private void AnimateCellRight(UIView view){
			view.Frame = new RectangleF(
				0,
				view.Frame.Y,
				view.Frame.Width,
				view.Frame.Height);
		}

		private void AnimateCellLeft(UIView view){
			view.Frame = new RectangleF(
				-(view.Frame.Width / 2),
				view.Frame.Y,
				view.Frame.Width,
				view.Frame.Height);
		}

		public void LoadContent(){
			Helper.Default.ShowHud ("loading black list");
			Api.Default.GetBlacklistAsync ((results)=>{
				InvokeOnMainThread(()=> {
					if(results.Error == null){
						_tableView.Source = new BlackListViewSource(results.Results.ToList());
						(_tableView.Source as BlackListViewSource).RowClicked += RowClickHandler;
						_tableView.ReloadData();
						Helper.Default.HideHud();
					}
					else{
						Helper.Default.HideHud(null, MBAlertViewHUDType.ExclamationMark,0);
						var alert = MBAlertView.AlertWithBody ("Unable to get black list :( Error: " + results.Error.Message + ".", "Try Again", ()=>{
							this.BeginInvokeOnMainThread(LoadContent);
						});
						alert.AddToDisplayQueue ();
					}
				});
			});
		}

		private void RowClickHandler(object sender, RedBit.RowClickedEventArgs<BlacklistItem> e){
			if (ItemClicked != null)
				ItemClicked (e.Item);
		}


	}

	public class UIBlackListTableView : UITableView
	{
		public UIBlackListTableView(){
		}

		public Action<BlacklistItem> DeleteItem { get; set; }
	}

}
