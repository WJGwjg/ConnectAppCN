using System.Collections.Generic;
using ConnectApp.api;
using ConnectApp.models;
using ConnectApp.redux.actions;
using UnityEngine;

namespace ConnectApp.redux.reducers {
    public static class AppReducer {
        public static AppState Reduce(AppState state, object bAction) {
            switch (bAction) {
                case AddCountAction action: {
                    state.Count += action.number;
                    PlayerPrefs.SetInt("count", state.Count);
                    PlayerPrefs.Save();
                    break;
                }
                case LoginChangeEmailAction action: {
                    state.loginState.email = action.changeText;
                    break;
                }
                case LoginChangePasswordAction action: {
                    state.loginState.password = action.changeText;
                    break;
                }
                case ChatWindowShowAction action: {
                    state.eventState.showChatWindow = action.show;
                    break;
                }
                case ChatWindowStatusAction action: {
                    state.eventState.openChatWindow = action.status;
                    break;
                }
                case NavigatorToLiveAction action: {
                    state.eventState.detailId = action.eventId;
                    break;
                }
                case ClearEventDetailAction action: {
                    state.eventState.detailId = null;
                    break;
                }
                case LoginByEmailAction action: {
                    state.loginState.loading = true;
                    var email = state.loginState.email;
                    var password = state.loginState.password;
                    LoginApi.LoginByEmail(email, password)
                        .Then(loginInfo => {
                            StoreProvider.store.Dispatch(new LoginByEmailSuccessAction {loginInfo = loginInfo});
                        })
                        .Catch(error => {
                            state.loginState.loading = false;
                            Debug.Log(error);
                        });
                    break;
                }
                case LoginByEmailSuccessAction action: {
                    state.loginState.loading = false;
                    state.loginState.loginInfo = action.loginInfo;
                    state.loginState.isLoggedIn = true;
                    break;
                }
                case FetchArticlesAction action: {
                    state.articleState.articlesLoading = true;
                    ArticleApi.FetchArticles(action.pageNumber)
                        .Then((articlesResponse) => {
                            var articleList = new List<string>();
                            var articleDict = new Dictionary<string, Article>();
                            articlesResponse.items.ForEach((item) => {
                                articleList.Add(item.id);
                                articleDict.Add(item.id, item);
                            });
                            StoreProvider.store.Dispatch(new FetchArticleSuccessAction
                                {ArticleDict = articleDict, ArticleList = articleList});
                        })
                        .Catch(error => { Debug.Log(error); });
                    break;
                }
                case FetchArticleSuccessAction action: {
                    state.articleState.articleList = action.ArticleList;
                    state.articleState.articleDict = action.ArticleDict;
                    state.articleState.articlesLoading = false;
                    break;
                }
                case FetchArticleDetailAction action: {
                    ArticleApi.FetchArticleDetail(action.articleId)
                        .Then(() => { StoreProvider.store.Dispatch(new FetchArticleDetailSuccessAction()); })
                        .Catch(error => { Debug.Log(error); });
                    break;
                }
                case FetchArticleDetailSuccessAction action: {
                    break;
                }
                case LikeArticleAction action: {
                    ArticleApi.LikeArticle(action.articleId)
                        .Then(() => { StoreProvider.store.Dispatch(new LikeArticleSuccessAction()); })
                        .Catch(error => { Debug.Log(error); });
                    break;
                }
                case LikeArticleSuccessAction action: {
                    break;
                }
                case FetchArticleCommentsAction action: {
                    ArticleApi.FetchArticleComments(action.channelId, action.currOldestMessageId)
                        .Then(() => { StoreProvider.store.Dispatch(new FetchArticleCommentsSuccessAction()); })
                        .Catch(error => { Debug.Log(error); });
                    break;
                }
                case FetchArticleCommentsSuccessAction action: {
                    break;
                }
                case LikeCommentAction action: {
                    ArticleApi.LikeComment(action.messageId)
                        .Then(() => { StoreProvider.store.Dispatch(new LikeCommentSuccessAction()); })
                        .Catch(error => { Debug.Log(error); });
                    break;
                }
                case LikeCommentSuccessAction action: {
                    break;
                }
                case RemoveLikeCommentAction action: {
                    ArticleApi.RemoveLikeComment(action.messageId)
                        .Then(() => { StoreProvider.store.Dispatch(new RemoveLikeSuccessAction()); })
                        .Catch(error => { Debug.Log(error); });
                    break;
                }
                case RemoveLikeSuccessAction action: {
                    break;
                }
                case SendCommentAction action: {
                    ArticleApi.SendComment(action.channelId, action.content, action.nonce, action.parentMessageId)
                        .Then(() => { StoreProvider.store.Dispatch(new SendCommentSuccessAction()); })
                        .Catch(error => { Debug.Log(error); });
                    break;
                }
                case SendCommentSuccessAction action: {
                    break;
                }
                case FetchEventsAction action: {
                    state.eventState.eventsLoading = true;
                    EventApi.FetchEvents(action.pageNumber)
                        .Then(events => {
                            StoreProvider.store.Dispatch(new FetchEventsSuccessAction {events = events});
                        })
                        .Catch(error => {
                            state.eventState.eventsLoading = false;
                            Debug.Log(error);
                        });
                    break;
                }
                case FetchEventsSuccessAction action: {
                    state.eventState.eventsLoading = false;
                    action.events.ForEach(eventObj => {
                        state.eventState.events.Add(eventObj.id);
                        state.eventState.eventDict[eventObj.id] = eventObj;
                    });
                    break;
                }
                case FetchEventDetailAction action: {
                    state.eventState.eventDetailLoading = true;
                    EventApi.FetchLiveDetail(action.eventId)
                        .Then(eventObj => {
                            StoreProvider.store.Dispatch(new FetchEventDetailSuccessAction {eventObj = eventObj});
                        })
                        .Catch(error => {
                            state.eventState.eventDetailLoading = false;
                            Debug.Log(error);
                        });
                    break;
                }
                case FetchEventDetailSuccessAction action: {
                    state.eventState.eventDetailLoading = false;
                    state.eventState.eventDict[action.eventObj.id] = action.eventObj;
                    state.eventState.detailId = action.eventObj.id;
                    break;
                }
                case FetchNotificationsAction action: {
                    NotificationApi.FetchNotifications(action.pageNumber)
                        .Then(() => { StoreProvider.store.Dispatch(new FetchNotificationsSuccessAction()); })
                        .Catch(error => { Debug.Log(error); });
                    break;
                }
                case FetchNotificationsSuccessAction action: {
                    break;
                }
                case ReportItemAction action: {
                    ReportApi.ReportItem(action.itemId, action.itemType, action.reportContext)
                        .Then(() => { StoreProvider.store.Dispatch(new ReportItemSuccessAction()); })
                        .Catch(error => { Debug.Log(error); });
                    break;
                }
                case ReportItemSuccessAction action: {
                    break;
                }
            }

            return state;
        }
    }
}