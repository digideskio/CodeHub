using System;
using CodeHub.ViewControllers;
using UIKit;
using CodeHub.Utilities;
using CodeHub.Core.ViewModels.Notifications;
using Humanizer;
using CodeHub.DialogElements;
using GitHubSharp.Models;
using System.Reactive.Linq;
using ReactiveUI;

namespace CodeHub.Views
{
    public class NotificationsView : ViewModelCollectionDrivenDialogViewController<NotificationsViewModel>
    {
        private readonly UISegmentedControl _viewSegment;
        private readonly UIBarButtonItem _segmentBarButton;

        public NotificationsView()
        {
            _viewSegment = new UISegmentedControl(new object[] { "Unread", "Participating", "All" });
            _segmentBarButton = new UIBarButtonItem(_viewSegment);

            EmptyView = new Lazy<UIView>(() =>
                new EmptyListView(Octicon.Inbox.ToEmptyListImage(), "No new notifications."));
        }

        public override void ViewDidLoad()
        {
            Title = "Notifications";

            base.ViewDidLoad();

            _segmentBarButton.Width = View.Frame.Width - 10f;
            ToolbarItems = new [] { new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace), _segmentBarButton, new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace) };

            var checkButton = new UIBarButtonItem { Image = Images.Buttons.CheckButton };
            NavigationItem.RightBarButtonItem = checkButton;

            var weakVm = new WeakReference<NotificationsViewModel>(ViewModel);

            BindCollection(ViewModel.Notifications, x =>
            {
                var el = new StringElement(x.Subject.Title, x.UpdatedAt.Humanize(), UITableViewCellStyle.Subtitle) { Accessory = UITableViewCellAccessory.DisclosureIndicator };

                var subject = x.Subject.Type.ToLower();
                if (subject.Equals("issue"))
                    el.Image = Octicon.IssueOpened.ToImage();
                else if (subject.Equals("pullrequest"))
                    el.Image = Octicon.GitPullRequest.ToImage();
                else if (subject.Equals("commit"))
                    el.Image = Octicon.GitCommit.ToImage();
                else if (subject.Equals("release"))
                    el.Image = Octicon.Tag.ToImage();
                else
                    el.Image = Octicon.Alert.ToImage();

                el.Clicked.Subscribe(MakeCallback(weakVm, x));
                return el;
            });

            var o = Observable.FromEventPattern(t => ViewModel.ReadAllCommand.CanExecuteChanged += t, t => ViewModel.ReadAllCommand.CanExecuteChanged -= t);

            OnActivation(d =>
            {
                checkButton.GetClickedObservable().InvokeCommand(ViewModel.ReadAllCommand).AddTo(d);
                ViewModel.WhenAnyValue(x => x.IsMarking).SubscribeStatus("Marking...").AddTo(d);
                ViewModel.WhenAnyValue(x => x.ShownIndex).Subscribe(x => _viewSegment.SelectedSegment = (nint)x).AddTo(d);
                _viewSegment.GetChangedObservable().Subscribe(x => ViewModel.ShownIndex = x).AddTo(d);
                o.Subscribe(_ => NavigationItem.RightBarButtonItem.Enabled = ViewModel.ReadAllCommand.CanExecute(null)).AddTo(d);
            });
        }

        private static Action<object> MakeCallback(WeakReference<NotificationsViewModel> weakVm, NotificationModel model)
        {
            return new Action<object>(_ => weakVm.Get()?.GoToNotificationCommand.Execute(model));
        }

        protected override Section CreateSection(string text)
        {
            return new Section(new MarkReadSection(text, this, _viewSegment.SelectedSegment != 2));
        }

        private class MarkReadSection : UITableViewHeaderFooterView
        {
            readonly UIButton _button;
            public MarkReadSection(string text, NotificationsView parent, bool button)
                : base(new CoreGraphics.CGRect(0, 0, 320, 28f))
            {
                var weakVm = new WeakReference<NotificationsViewModel>(parent.ViewModel as NotificationsViewModel);
                TextLabel.Text = text;

                if (button)
                {
                    _button = new UIButton(UIButtonType.RoundedRect);
                    _button.SetImage(Images.Buttons.CheckButton, UIControlState.Normal);
                    //_button.Frame = new System.Drawing.RectangleF(320f - 42f, 1f, 26f, 26f);
                    _button.TintColor = UIColor.FromRGB(50, 50, 50);
                    _button.TouchUpInside += (sender, e) => weakVm.Get()?.ReadRepositoriesCommand.Execute(text);
                    Add(_button);
                }
            }

            public override void LayoutSubviews()
            {
                base.LayoutSubviews();

                if (_button != null)
                    _button.Frame = new CoreGraphics.CGRect(Frame.Width - 42f, 1, 26, 26);
            }
        }

        public override void ViewWillAppear(bool animated)
        {
            if (ToolbarItems != null)
                NavigationController.SetToolbarHidden(false, animated);
            base.ViewWillAppear(animated);
        }

        public override void ViewWillDisappear(bool animated)
        {
            if (ToolbarItems != null)
                NavigationController.SetToolbarHidden(true, animated);
            base.ViewWillDisappear(animated);
        }
    }
}

