using System.Collections.Generic;
using ConnectApp.Components;
using ConnectApp.Constants;
using ConnectApp.Main;
using ConnectApp.Models.ActionModel;
using ConnectApp.Models.State;
using ConnectApp.Models.ViewModel;
using ConnectApp.redux.actions;
using ConnectApp.Utils;
using RSG;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.Redux;
using Unity.UIWidgets.service;
using Unity.UIWidgets.widgets;

namespace ConnectApp.screens {
    public class HistoryArticleScreenConnector : StatelessWidget {
        public override Widget build(BuildContext context) {
            return new StoreConnector<AppState, HistoryScreenViewModel>(
                converter: state => new HistoryScreenViewModel {
                    articleHistory = state.articleState.articleHistory,
                    isLoggedIn = state.loginState.isLoggedIn
                },
                builder: (context1, viewModel, dispatcher) => {
                    var actionModel = new HistoryScreenActionModel {
                        pushToLogin = () => dispatcher.dispatch(new MainNavigatorPushToAction {
                            routeName = MainNavigatorRoutes.Login
                        }),
                        pushToArticleDetail = id =>
                            dispatcher.dispatch(new MainNavigatorPushToArticleDetailAction {articleId = id}),
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
                        shareToWechat = (type, title, description, linkUrl, imageUrl) => dispatcher.dispatch<IPromise>(
                            Actions.shareToWechat(type, title, description, linkUrl, imageUrl)),
                        deleteArticleHistory = id =>
                            dispatcher.dispatch(new DeleteArticleHistoryAction {articleId = id})
                    };
                    return new HistoryArticleScreen(viewModel, actionModel);
                }
            );
        }
    }

    public class HistoryArticleScreen : StatelessWidget {
        public HistoryArticleScreen(
            HistoryScreenViewModel viewModel = null,
            HistoryScreenActionModel actionModel = null,
            Key key = null
        ) : base(key: key) {
            this.viewModel = viewModel;
            this.actionModel = actionModel;
        }

        readonly HistoryScreenViewModel viewModel;
        readonly HistoryScreenActionModel actionModel;

        readonly CustomDismissibleController _controller = new CustomDismissibleController();

        public override Widget build(BuildContext context) {
            if (this.viewModel.articleHistory.Count == 0) {
                return new BlankView("哎呀，还没有任何文章记录", "image/default-history");
            }

            return new Container(
                color: CColors.Background,
                child: new CustomScrollbar(
                    ListView.builder(
                        physics: new AlwaysScrollableScrollPhysics(),
                        itemCount: this.viewModel.articleHistory.Count,
                        itemBuilder: this._buildArticleCard
                    )
                )
            );
        }

        Widget _buildArticleCard(BuildContext context, int index) {
            var article = this.viewModel.articleHistory[index: index];
            var linkUrl = CStringUtils.JointProjectShareLink(projectId: article.id);
            return CustomDismissible.builder(
                Key.key(value: article.id),
                new ArticleCard(
                    article: article,
                    () => this.actionModel.pushToArticleDetail(obj: article.id),
                    () => ShareManager.showArticleShareView(
                        true,
                        isLoggedIn: this.viewModel.isLoggedIn,
                        () => {
                            Clipboard.setData(new ClipboardData(text: linkUrl));
                            CustomDialogUtils.showToast("复制链接成功", Icons.check_circle_outline);
                        },
                        () => this.actionModel.pushToLogin(),
                        () => this.actionModel.pushToBlock(article.id),
                        () => this.actionModel.pushToReport(article.id, ReportType.article),
                        type => {
                            CustomDialogUtils.showCustomDialog(
                                child: new CustomLoadingDialog()
                            );
                            string imageUrl = CImageUtils.SizeTo200ImageUrl(article.thumbnail.url);
                            this.actionModel.shareToWechat(arg1: type, arg2: article.title,
                                    arg3: article.subTitle, arg4: linkUrl, arg5: imageUrl)
                                .Then(onResolved: CustomDialogUtils.hiddenCustomDialog)
                                .Catch(_ => CustomDialogUtils.hiddenCustomDialog());
                        }
                    ),
                    fullName: article.fullName,
                    index == 0,
                    new ObjectKey(value: article.id)
                ),
                new CustomDismissibleDrawerDelegate(),
                secondaryActions: new List<Widget> {
                    new GestureDetector(
                        onTap: () => this.actionModel.deleteArticleHistory(obj: article.id),
                        child: new Container(
                            color: CColors.Separator2,
                            width: 80,
                            alignment: Alignment.center,
                            child: new Container(
                                width: 44,
                                height: 44,
                                alignment: Alignment.center,
                                decoration: new BoxDecoration(
                                    CColors.White,
                                    borderRadius: BorderRadius.circular(22)
                                ),
                                child: new Icon(Icons.delete_outline, size: 28, color: CColors.Error)
                            )
                        )
                    )
                },
                controller: this._controller
            );
        }
    }
}