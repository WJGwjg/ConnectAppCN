using System;
using System.Collections.Generic;
using ConnectApp.Components;
using ConnectApp.Components.pull_to_refresh;
using ConnectApp.Constants;
using ConnectApp.Main;
using ConnectApp.Models.ActionModel;
using ConnectApp.Models.Model;
using ConnectApp.Models.State;
using ConnectApp.Models.ViewModel;
using ConnectApp.redux.actions;
using ConnectApp.Utils;
using RSG;
using Unity.UIWidgets.animation;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.gestures;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.Redux;
using Unity.UIWidgets.scheduler;
using Unity.UIWidgets.service;
using Unity.UIWidgets.ui;
using Unity.UIWidgets.widgets;
using UnityEngine;
using Avatar = ConnectApp.Components.Avatar;
using Color = Unity.UIWidgets.ui.Color;
using Transform = Unity.UIWidgets.widgets.Transform;

namespace ConnectApp.screens {
    public class UserDetailScreenConnector : StatelessWidget {
        public UserDetailScreenConnector(
            string userId,
            bool isSlug = false,
            Key key = null
        ) : base(key: key) {
            this.userId = userId;
            this.isSlug = isSlug;
        }

        readonly string userId;
        readonly bool isSlug;

        public override Widget build(BuildContext context) {
            return new StoreConnector<AppState, UserDetailScreenViewModel>(
                converter: state => {
                    var user = this.fetchUser(this.userId, state.userState.userDict, state.userState.slugDict);
                    var currentUserId = state.loginState.loginInfo.userId ?? "";
                    var followMap = state.followState.followDict.ContainsKey(key: currentUserId)
                        ? state.followState.followDict[key: currentUserId]
                        : new Dictionary<string, bool>();
                    return new UserDetailScreenViewModel {
                        userLoading = state.userState.userLoading,
                        userArticleLoading = state.userState.userArticleLoading,
                        user = user,
                        articleDict = state.articleState.articleDict,
                        followMap = followMap,
                        currentUserId = currentUserId,
                        isLoggedIn = state.loginState.isLoggedIn
                    };
                },
                builder: (context1, viewModel, dispatcher) => {
                    var actionModel = new UserDetailScreenActionModel {
                        startFetchUserProfile = () => dispatcher.dispatch(new StartFetchUserProfileAction()),
                        fetchUserProfile = () => dispatcher.dispatch<IPromise>(Actions.fetchUserProfile(this.userId)),
                        startFetchUserArticle = () => dispatcher.dispatch(new StartFetchUserArticleAction()),
                        fetchUserArticle = (userId, pageNumber) =>
                            dispatcher.dispatch<IPromise>(Actions.fetchUserArticle(userId, pageNumber)),
                        startFollowUser = userId =>
                            dispatcher.dispatch(new StartFollowUserAction {followUserId = userId}),
                        followUser = userId => dispatcher.dispatch<IPromise>(Actions.fetchFollowUser(userId)),
                        startUnFollowUser = userId => dispatcher.dispatch(new StartUnFollowUserAction
                            {unFollowUserId = userId}),
                        unFollowUser = userId => dispatcher.dispatch<IPromise>(Actions.fetchUnFollowUser(userId)),
                        mainRouterPop = () => dispatcher.dispatch(new MainNavigatorPopAction()),
                        pushToLogin = () => dispatcher.dispatch(new MainNavigatorPushToAction {
                            routeName = MainNavigatorRoutes.Login
                        }),
                        pushToArticleDetail = id => dispatcher.dispatch(
                            new MainNavigatorPushToArticleDetailAction {
                                articleId = id
                            }
                        ),
                        pushToReport = (reportId, reportType) => dispatcher.dispatch(
                            new MainNavigatorPushToReportAction {
                                reportId = reportId,
                                reportType = reportType
                            }
                        ),
                        pushToBlock = articleId => {
                            dispatcher.dispatch(new BlockArticleAction {articleId = articleId});
                            dispatcher.dispatch(new DeleteArticleHistoryAction {articleId = articleId});
                        },
                        pushToUserFollowing = (userId, initialPage) => dispatcher.dispatch(
                            new MainNavigatorPushToUserFollowingAction {
                                userId = userId,
                                initialPage = initialPage
                            }
                        ),
                        pushToUserFollower = userId => dispatcher.dispatch(
                            new MainNavigatorPushToUserFollowerAction {
                                userId = userId
                            }
                        ),
                        pushToEditPersonalInfo = userId => dispatcher.dispatch(
                            new MainNavigatorPushToEditPersonalInfoAction {
                                userId = userId
                            }
                        ),
                        shareToWechat = (type, title, description, linkUrl, imageUrl) => dispatcher.dispatch<IPromise>(
                            Actions.shareToWechat(type, title, description, linkUrl, imageUrl))
                    };
                    return new UserDetailScreen(viewModel, actionModel);
                }
            );
        }

        User fetchUser(string userId, Dictionary<string, User> userDict, Dictionary<string, string> slugDict) {
            if (userDict.ContainsKey(userId)) {
                return userDict[userId];
            }

            if (this.isSlug && slugDict.ContainsKey(userId)) {
                return userDict[slugDict[userId]];
            }

            return null;
        }
    }

    public class UserDetailScreen : StatefulWidget {
        public UserDetailScreen(
            UserDetailScreenViewModel viewModel = null,
            UserDetailScreenActionModel actionModel = null,
            Key key = null
        ) : base(key: key) {
            this.viewModel = viewModel;
            this.actionModel = actionModel;
        }

        public readonly UserDetailScreenViewModel viewModel;
        public readonly UserDetailScreenActionModel actionModel;

        public override State createState() {
            return new _UserDetailScreenState();
        }
    }

    class _UserDetailScreenState : State<UserDetailScreen>, TickerProvider, RouteAware {
        const float headerHeight = 256;
        const float _transformSpeed = 0.005f;
        int _articlePageNumber;
        RefreshController _refreshController;
        float _factor = 1;
        bool _isHaveTitle;
        bool _hideNavBar;
        float _topPadding;
        Animation<RelativeRect> _animation;
        AnimationController _controller;

        public override void initState() {
            base.initState();
            StatusBarManager.statusBarStyle(true);
            this._articlePageNumber = 1;
            this._refreshController = new RefreshController();
            this._isHaveTitle = false;
            this._hideNavBar = true;
            this._controller = new AnimationController(
                duration: TimeSpan.FromMilliseconds(100),
                vsync: this
            );
            RelativeRectTween rectTween = new RelativeRectTween(
                RelativeRect.fromLTRB(0, 44, 0, 0),
                RelativeRect.fromLTRB(0, 0, 0, 0)
            );
            this._animation = rectTween.animate(this._controller);
            SchedulerBinding.instance.addPostFrameCallback(_ => {
                this.widget.actionModel.startFetchUserProfile();
                this.widget.actionModel.fetchUserProfile();
                this.widget.actionModel.startFetchUserArticle();
            });
        }

        public override void didChangeDependencies() {
            base.didChangeDependencies();
            Router.routeObserve.subscribe(this, (PageRoute) ModalRoute.of(this.context));
        }

        public override void dispose() {
            Router.routeObserve.unsubscribe(this);
            base.dispose();
        }

        public Ticker createTicker(TickerCallback onTick) {
            return new Ticker(onTick, () => $"created by {this}");
        }

        void _scrollListener() {
            var scrollController = this._refreshController.scrollController;
            if (scrollController.offset < 0) {
                this._factor = 1 + scrollController.offset.abs() * _transformSpeed;
                this.setState(() => { });
            }
            else {
                if (this._factor != 1) {
                    this.setState(() => this._factor = 1);
                }
            }
        }

        bool _onNotification(ScrollNotification notification) {
            var pixels = notification.metrics.pixels;

            if (pixels >= 44 + this._topPadding) {
                if (this._hideNavBar) {
                    this.setState(() => this._hideNavBar = false);
                    StatusBarManager.statusBarStyle(false);
                }
            }
            else {
                if (!this._hideNavBar) {
                    this.setState(() => this._hideNavBar = true);
                    StatusBarManager.statusBarStyle(true);
                }
            }

            if (pixels > headerHeight - 24 - (44 + this._topPadding)) {
                if (!this._isHaveTitle) {
                    this._controller.forward();
                    this.setState(() => this._isHaveTitle = true);
                }
            }
            else {
                if (this._isHaveTitle) {
                    this._controller.reverse();
                    this.setState(() => this._isHaveTitle = false);
                }
            }

            return true;
        }

        void _onRefresh(bool up) {
            if (up) {
                this._articlePageNumber = 1;
            }
            else {
                this._articlePageNumber++;
            }
            this.widget.actionModel.fetchUserArticle(arg1: this.widget.viewModel.user.id, arg2: this._articlePageNumber)
                .Then(() => this._refreshController.sendBack(up: up, up ? RefreshStatus.completed : RefreshStatus.idle))
                .Catch(_ => this._refreshController.sendBack(up: up, mode: RefreshStatus.failed));
        }

        public override Widget build(BuildContext context) {
            if (this._topPadding != MediaQuery.of(context).padding.top &&
                Application.platform != RuntimePlatform.Android) {
                this._topPadding = MediaQuery.of(context).padding.top;
            }

            Widget content;
            if (this.widget.viewModel.userLoading && this.widget.viewModel.user == null) {
                content = new GlobalLoading();
            }
            else if (this.widget.viewModel.user == null || this.widget.viewModel.user.errorCode == "ResourceNotFound") {
                content = new BlankView(
                    "用户不存在",
                    "image/default-following"
                );
            }
            else {
                content = this._buildUserContent(context: context);
            }

            return new Container(
                color: CColors.White,
                child: new CustomSafeArea(
                    top: false,
                    bottom: false,
                    child: new Stack(
                        children: new List<Widget> {
                            content,
                            this._buildNavigationBar()
                        }
                    )
                )
            );
        }

        Widget _buildNavigationBar() {
            Widget titleWidget = new Container();
            if (this._isHaveTitle) {
                var user = this.widget.viewModel.user ?? new User();
                titleWidget = new Row(
                    children: new List<Widget> {
                        new Expanded(
                            child: new Text(
                                user.fullName ?? user.name,
                                style: CTextStyle.PXLargeMedium,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis
                            )
                        ),
                        new SizedBox(width: 8),
                        this._buildFollowButton(true),
                        new SizedBox(width: 16)
                    }
                );
            }

            var hasUser = !(this.widget.viewModel.user == null ||
                            this.widget.viewModel.user.errorCode == "ResourceNotFound");
            return new Positioned(
                left: 0,
                top: 0,
                right: 0,
                height: 44 + this._topPadding,
                child: new Container(
                    padding: EdgeInsets.only(top: this._topPadding),
                    decoration: new BoxDecoration(
                        this._hideNavBar ? CColors.Transparent : CColors.White,
                        border: new Border(
                            bottom: new BorderSide(this._isHaveTitle ? CColors.Separator2 : CColors.Transparent))
                    ),
                    child: new Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        crossAxisAlignment: CrossAxisAlignment.center,
                        children: new List<Widget> {
                            new GestureDetector(
                                onTap: () => this.widget.actionModel.mainRouterPop(),
                                child: new CustomButton(
                                    padding: EdgeInsets.only(16, 8, 8, 8),
                                    onPressed: () => this.widget.actionModel.mainRouterPop(),
                                    child: new Icon(
                                        icon: Icons.arrow_back,
                                        size: 24,
                                        color: hasUser
                                            ? (this._hideNavBar ? CColors.White : CColors.Icon)
                                            : CColors.Icon
                                    )
                                )
                            ),
                            new Expanded(
                                child: new Stack(
                                    fit: StackFit.expand,
                                    children: new List<Widget> {
                                        new PositionedTransition(
                                            rect: this._animation,
                                            child: titleWidget
                                        )
                                    }
                                )
                            )
                        }
                    )
                )
            );
        }

        Widget _buildUserContent(BuildContext context) {
            var articleIds = this.widget.viewModel.user.articleIds;
            var articlesHasMore = this.widget.viewModel.user.articlesHasMore ?? false;
            var userArticleLoading = this.widget.viewModel.userArticleLoading && articleIds == null;
            int itemCount;
            if (userArticleLoading) {
                itemCount = 3;
            }
            else {
                if (articleIds == null) {
                    itemCount = 3;
                }
                else {
                    var articleCount = articlesHasMore ? articleIds.Count : articleIds.Count + 1;
                    itemCount = 2 + (articleIds.Count == 0 ? 1 : articleCount);
                }
            }

            return new Container(
                color: CColors.Background,
                child: new CustomScrollbar(
                    new SmartRefresher(
                        controller: this._refreshController,
                        enablePullDown: false,
                        enablePullUp: articlesHasMore,
                        onRefresh: this._onRefresh,
                        onNotification: this._onNotification,
                        child: ListView.builder(
                            physics: new AlwaysScrollableScrollPhysics(),
                            itemCount: itemCount,
                            itemBuilder: (cxt, index) => {
                                if (index == 0) {
                                    return Transform.scale(
                                        scale: this._factor,
                                        child: this._buildUserInfo()
                                    );
                                }

                                if (index == 1) {
                                    return _buildUserArticleTitle();
                                }

                                if (userArticleLoading && index == 2) {
                                    var height = MediaQuery.of(context: context).size.height - headerHeight - 44;
                                    return new Container(
                                        height: height,
                                        child: new GlobalLoading()
                                    );
                                }

                                if ((articleIds == null || articleIds.Count == 0) && index == 2) {
                                    var height = MediaQuery.of(context: context).size.height - headerHeight - 44;
                                    return new Container(
                                        height: height,
                                        child: new BlankView(
                                            "哎呀，暂无已发布的文章",
                                            "image/default-article"
                                        )
                                    );
                                }

                                if (index == itemCount - 1 && !articlesHasMore) {
                                    return new EndView();
                                }

                                var articleId = articleIds[index - 2];
                                if (!this.widget.viewModel.articleDict.ContainsKey(key: articleId)) {
                                    return new Container();
                                }

                                var article = this.widget.viewModel.articleDict[key: articleId];
                                var linkUrl = CStringUtils.JointProjectShareLink(projectId: article.id);
                                return new ArticleCard(
                                    article: article,
                                    () => this.widget.actionModel.pushToArticleDetail(obj: article.id),
                                    () => ShareManager.showArticleShareView(
                                        this.widget.viewModel.currentUserId != article.userId,
                                        isLoggedIn: this.widget.viewModel.isLoggedIn,
                                        () => {
                                            Clipboard.setData(new ClipboardData(text: linkUrl));
                                            CustomDialogUtils.showToast("复制链接成功", Icons.check_circle_outline);
                                        },
                                        () => this.widget.actionModel.pushToLogin(),
                                        () => this.widget.actionModel.pushToBlock(article.id),
                                        () => this.widget.actionModel.pushToReport(article.id, ReportType.article),
                                        type => {
                                            CustomDialogUtils.showCustomDialog(
                                                child: new CustomLoadingDialog()
                                            );
                                            string imageUrl = CImageUtils.SizeTo200ImageUrl(article.thumbnail.url);
                                            this.widget.actionModel.shareToWechat(arg1: type, arg2: article.title,
                                                    arg3: article.subTitle, arg4: linkUrl, arg5: imageUrl)
                                                .Then(onResolved: CustomDialogUtils.hiddenCustomDialog)
                                                .Catch(_ => CustomDialogUtils.hiddenCustomDialog());
                                        },
                                        () => this.widget.actionModel.mainRouterPop()
                                    ),
                                    this.widget.viewModel.user.fullName ?? this.widget.viewModel.user.name,
                                    key: new ObjectKey(value: article.id)
                                );
                            }
                        )
                    )
                )
            );
        }

        Widget _buildUserInfo() {
            var user = this.widget.viewModel.user ?? new User();
            Widget titleWidget = new Container();
            if (user.title != null && user.title.isNotEmpty()) {
                titleWidget = new Text(
                    data: user.title,
                    style: new TextStyle(
                        height: 1.46f,
                        fontSize: 14,
                        fontFamily: "Roboto-Regular",
                        color: CColors.BgGrey
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis
                );
            }

            return new CoverImage(
                coverImage: user.coverImage,
                height: headerHeight,
                new Container(
                    padding: EdgeInsets.only(16, 0, 16, 24),
                    child: new Column(
                        mainAxisAlignment: MainAxisAlignment.end,
                        children: new List<Widget> {
                            new Row(
                                children: new List<Widget> {
                                    new Container(
                                        margin: EdgeInsets.only(right: 16),
                                        child: Avatar.User(
                                            user: user,
                                            80,
                                            true
                                        )
                                    ),
                                    new Expanded(
                                        child: new Column(
                                            crossAxisAlignment: CrossAxisAlignment.start,
                                            children: new List<Widget> {
                                                new Text(
                                                    user.fullName ?? user.name,
                                                    style: CTextStyle.H4White,
                                                    maxLines: 1,
                                                    overflow: TextOverflow.ellipsis
                                                ),
                                                titleWidget
                                            }
                                        )
                                    )
                                }
                            ),
                            new Container(
                                margin: EdgeInsets.only(top: 16),
                                child: new Row(
                                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                    children: new List<Widget> {
                                        new Row(
                                            children: new List<Widget> {
                                                _buildFollowCount(
                                                    "关注",
                                                    $"{(user.followingUsersCount ?? 0) + (user.followingTeamsCount ?? 0)}",
                                                    () =>
                                                        this.widget.actionModel.pushToUserFollowing(
                                                            arg1: this.widget.viewModel.user.id, 0)
                                                ),
                                                new SizedBox(width: 16),
                                                _buildFollowCount(
                                                    "粉丝",
                                                    $"{user.followCount ?? 0}",
                                                    () =>
                                                        this.widget.actionModel.pushToUserFollower(
                                                            obj: this.widget.viewModel.user.id)
                                                )
                                            }
                                        ),
                                        this._buildFollowButton()
                                    }
                                )
                            )
                        }
                    )
                )
            );
        }

        static Widget _buildUserArticleTitle() {
            return new Container(
                padding: EdgeInsets.only(16),
                height: 44,
                decoration: new BoxDecoration(
                    color: CColors.White,
                    border: new Border(
                        bottom: new BorderSide(
                            color: CColors.Separator2
                        )
                    )
                ),
                alignment: Alignment.centerLeft,
                child: new Text("文章", style: CTextStyle.PLargeTitle)
            );
        }

        static Widget _buildFollowCount(string title, string subTitle, GestureTapCallback onTap) {
            return new GestureDetector(
                onTap: onTap,
                child: new Container(
                    height: 32,
                    alignment: Alignment.center,
                    color: CColors.Transparent,
                    child: new Row(
                        children: new List<Widget> {
                            new Text(data: title, style: CTextStyle.PRegularWhite),
                            new SizedBox(width: 2),
                            new Text(
                                data: subTitle,
                                style: new TextStyle(
                                    height: 1.27f,
                                    fontSize: 20,
                                    fontFamily: "Roboto-Bold",
                                    color: CColors.White
                                )
                            )
                        }
                    )
                )
            );
        }

        Widget _buildFollowButton(bool isTop = false) {
            if (this.widget.viewModel.isLoggedIn
                && this.widget.viewModel.currentUserId == this.widget.viewModel.user.id) {
                if (isTop) {
                    return new Container();
                }

                return new CustomButton(
                    padding: EdgeInsets.zero,
                    child: new Container(
                        width: 100,
                        height: 32,
                        alignment: Alignment.center,
                        decoration: new BoxDecoration(
                            color: CColors.Transparent,
                            borderRadius: BorderRadius.all(4),
                            border: Border.all(
                                color: CColors.White
                            )
                        ),
                        child: new Text("编辑资料", style: CTextStyle.PMediumWhite)
                    ),
                    onPressed: () => {
                        if (this.widget.viewModel.isLoggedIn) {
                            if (this.widget.viewModel.user.jobRoleMap == null) {
                                return;
                            }

                            this.widget.actionModel.pushToEditPersonalInfo(this.widget.viewModel.user.id);
                        }
                        else {
                            this.widget.actionModel.pushToLogin();
                        }
                    }
                );
            }

            bool isFollow = false;
            string followText = "关注";
            Color followBgColor = CColors.PrimaryBlue;
            GestureTapCallback onTap = () => {
                this.widget.actionModel.startFollowUser(this.widget.viewModel.user.id);
                this.widget.actionModel.followUser(this.widget.viewModel.user.id);
            };
            if (this.widget.viewModel.isLoggedIn
                && this.widget.viewModel.followMap.ContainsKey(key: this.widget.viewModel.user.id)) {
                isFollow = true;
                followText = "已关注";
                followBgColor = CColors.Transparent;
                onTap = () => {
                    ActionSheetUtils.showModalActionSheet(
                        new ActionSheet(
                            title: "确定不再关注？",
                            items: new List<ActionSheetItem> {
                                new ActionSheetItem("确定", type: ActionType.normal,
                                    () => {
                                        this.widget.actionModel.startUnFollowUser(this.widget.viewModel.user.id);
                                        this.widget.actionModel.unFollowUser(this.widget.viewModel.user.id);
                                    }),
                                new ActionSheetItem("取消", type: ActionType.cancel)
                            }
                        )
                    );
                };
            }

            Widget buttonChild;
            bool isEnable;
            if (this.widget.viewModel.user.followUserLoading ?? false) {
                buttonChild = new CustomActivityIndicator(
                    loadingColor: isTop ? LoadingColor.black : LoadingColor.white,
                    size: LoadingSize.small
                );
                isEnable = false;
            }
            else {
                buttonChild = new Text(
                    data: followText,
                    style: isTop
                        ? new TextStyle(
                            fontSize: 14,
                            fontFamily: "Roboto-Medium",
                            color: isFollow ? new Color(0xFF959595) : CColors.PrimaryBlue
                        )
                        : CTextStyle.PMediumWhite
                );
                isEnable = true;
            }

            if (isTop) {
                return new CustomButton(
                    padding: EdgeInsets.zero,
                    child: new Container(
                        width: 60,
                        height: 28,
                        alignment: Alignment.center,
                        decoration: new BoxDecoration(
                            color: CColors.Transparent,
                            borderRadius: BorderRadius.circular(14),
                            border: isFollow
                                ? Border.all(color: CColors.Disable2)
                                : Border.all(color: CColors.PrimaryBlue)
                        ),
                        child: buttonChild
                    ),
                    onPressed: () => {
                        if (!isEnable) {
                            return;
                        }

                        if (this.widget.viewModel.isLoggedIn) {
                            onTap();
                        }
                        else {
                            this.widget.actionModel.pushToLogin();
                        }
                    }
                );
            }

            return new CustomButton(
                padding: EdgeInsets.zero,
                child: new Container(
                    width: 100,
                    height: 32,
                    alignment: Alignment.center,
                    decoration: new BoxDecoration(
                        color: followBgColor,
                        borderRadius: BorderRadius.all(4),
                        border: isFollow ? Border.all(CColors.White) : null
                    ),
                    child: buttonChild
                ),
                onPressed: () => {
                    if (!isEnable) {
                        return;
                    }

                    if (this.widget.viewModel.isLoggedIn) {
                        onTap();
                    }
                    else {
                        this.widget.actionModel.pushToLogin();
                    }
                }
            );
        }

        public void didPopNext() {
            StatusBarManager.statusBarStyle(isLight: this._hideNavBar);
        }

        public void didPush() {
        }

        public void didPop() {
        }

        public void didPushNext() {
        }
    }
}