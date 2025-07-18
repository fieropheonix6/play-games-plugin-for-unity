// <copyright file="PlayGamesPlatform.cs" company="Google Inc.">
// Copyright (C) 2014 Google Inc. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>

#if UNITY_ANDROID

namespace GooglePlayGames
{
    using System;
    using System.Collections.Generic;
    using GooglePlayGames.BasicApi;
    using GooglePlayGames.BasicApi.Events;
    using GooglePlayGames.BasicApi.Nearby;
    using GooglePlayGames.BasicApi.SavedGame;
    using GooglePlayGames.OurUtils;
    using UnityEngine;
    using UnityEngine.SocialPlatforms;

    /// <summary>
    /// Provides access to the Google Play Games platform. This is an implementation of
    /// UnityEngine.SocialPlatforms.ISocialPlatform. Activate this platform by calling
    /// the <see cref="Activate" /> method, then authenticate by calling
    /// the <see cref="Authenticate" /> method. After authentication
    /// completes, you may call the other methods of this class. This is not a complete
    /// implementation of the ISocialPlatform interface. Methods lacking an implementation
    /// or whose behavior is at variance with the standard are noted as such.
    /// </summary>
    public class PlayGamesPlatform : ISocialPlatform
    {
        /// <summary>Singleton instance</summary>
        private static volatile PlayGamesPlatform sInstance = null;

        /// <summary>status of nearby connection initialization.</summary>
        private static volatile bool sNearbyInitializePending;

        /// <summary>Reference to the nearby client.</summary>
        /// <remarks>This is static since it can be used without using play game services.</remarks>
        private static volatile INearbyConnectionClient sNearbyConnectionClient;

        /// <summary>The local user.</summary>
        private PlayGamesLocalUser mLocalUser = null;

        /// <summary>Reference to the platform specific implementation.</summary>
        private IPlayGamesClient mClient = null;

        /// <summary>the default leaderboard we show on ShowLeaderboardUI</summary>
        private string mDefaultLbUi = null;

        /// <summary>the mapping table from alias to leaderboard/achievement id.</summary>
        private Dictionary<string, string> mIdMap = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="GooglePlayGames.PlayGamesPlatform"/> class.
        /// </summary>
        /// <param name="client">Implementation client to use for this instance.</param>
        internal PlayGamesPlatform(IPlayGamesClient client)
        {
            this.mClient = Misc.CheckNotNull(client);
            this.mLocalUser = new PlayGamesLocalUser(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GooglePlayGames.PlayGamesPlatform"/> class.
        /// </summary>
        private PlayGamesPlatform()
        {
            GooglePlayGames.OurUtils.Logger.d("Creating new PlayGamesPlatform");
            this.mLocalUser = new PlayGamesLocalUser(this);
        }

        /// <summary>
        /// Gets or sets a value indicating whether debug logs are enabled. This property
        /// may be set before calling <see cref="Activate" /> method.
        /// </summary>
        /// <returns>
        /// <c>true</c> if debug log enabled; otherwise, <c>false</c>.
        /// </returns>
        public static bool DebugLogEnabled
        {
            get { return GooglePlayGames.OurUtils.Logger.DebugLogEnabled; }

            set { GooglePlayGames.OurUtils.Logger.DebugLogEnabled = value; }
        }

        /// <summary>
        /// Gets the singleton instance of the Play Games platform.
        /// </summary>
        /// <returns>
        /// The instance.
        /// </returns>
        public static PlayGamesPlatform Instance
        {
            get
            {
                if (sInstance == null)
                {
                  OurUtils.Logger.d("Initializing the PlayGamesPlatform instance.");
                  sInstance =
                      new PlayGamesPlatform(PlayGamesClientFactory.GetPlatformPlayGamesClient());
                }

                return sInstance;
            }
        }

        /// <summary>
        /// Gets the nearby connection client.  NOTE: Can be null until the nearby client
        /// is initialized.  Call InitializeNearby to use callback to be notified when initialization
        /// is complete.
        /// </summary>
        /// <value>The nearby.</value>
        public static INearbyConnectionClient Nearby
        {
            get
            {
                if (sNearbyConnectionClient == null && !sNearbyInitializePending)
                {
                    sNearbyInitializePending = true;
                    InitializeNearby(null);
                }

                return sNearbyConnectionClient;
            }
        }

        /// <summary>Gets the saved game client object.</summary>
        /// <value>The saved game client.</value>
        public ISavedGameClient SavedGame
        {
            get { return mClient.GetSavedGameClient(); }
        }

        /// <summary>Gets the events client object.</summary>
        /// <value>The events client.</value>
        public IEventsClient Events
        {
            get { return mClient.GetEventsClient(); }
        }

        /// <summary>
        /// Gets the local user.
        /// </summary>
        /// <returns>
        /// The local user.
        /// </returns>
        public ILocalUser localUser
        {
            get { return mLocalUser; }
        }

        /// <summary>
        /// Initializes the nearby connection platform.
        /// </summary>
        /// <remarks>This call initializes the nearby connection platform.  This
        /// is independent of the Play Game Services initialization.  Multiple
        /// calls to this method are ignored.
        /// </remarks>
        /// <param name="callback">Callback invoked when  complete.</param>
        public static void InitializeNearby(Action<INearbyConnectionClient> callback)
        {
            OurUtils.Logger.d("Calling InitializeNearby!");
            if (sNearbyConnectionClient == null)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                NearbyConnectionClientFactory.Create(client => {
                    OurUtils.Logger.d("Nearby Client Created!!");
                    sNearbyConnectionClient = client;
                    if (callback != null) {
                        callback.Invoke(client);
                    }
                    else {
                        OurUtils.Logger.d("Initialize Nearby callback is null");
                    }
                });
#else
                sNearbyConnectionClient = new DummyNearbyConnectionClient();
                if (callback != null)
                {
                    callback.Invoke(sNearbyConnectionClient);
                }

#endif
            }
            else if (callback != null)
            {
                OurUtils.Logger.d("Nearby Already initialized: calling callback directly");
                callback.Invoke(sNearbyConnectionClient);
            }
            else
            {
                OurUtils.Logger.d("Nearby Already initialized");
            }
        }

        /// <summary>
        /// Activates the Play Games platform as the implementation of Social.Active.
        /// After calling this method, you can call methods on Social.Active. For
        /// example, <c>Social.Active.Authenticate()</c>.
        /// </summary>
        /// <returns>The singleton <see cref="PlayGamesPlatform" /> instance.</returns>
        public static PlayGamesPlatform Activate()
        {
            GooglePlayGames.OurUtils.Logger.d("Activating PlayGamesPlatform.");

            Social.Active = PlayGamesPlatform.Instance;
            GooglePlayGames.OurUtils.Logger.d(
                "PlayGamesPlatform activated: " + Social.Active);
            return PlayGamesPlatform.Instance;
        }

        /// <summary>
        /// Specifies that the ID <c>fromId</c> should be implicitly replaced by <c>toId</c>
        /// on any calls that take a leaderboard or achievement ID.
        /// </summary>
        /// <remarks> After a mapping is
        /// registered, you can use <c>fromId</c> instead of <c>toId</c> when making a call.
        /// For example, the following two snippets are equivalent:
        /// <c>ReportProgress("Cfiwjew894_AQ", 100.0, callback);</c>
        /// is equivalent to:
        /// <c>AddIdMapping("super-combo", "Cfiwjew894_AQ");</c>
        /// <c>ReportProgress("super-combo", 100.0, callback);</c>
        /// </remarks>
        /// <param name='fromId'>
        /// The identifier to map.
        /// </param>
        /// <param name='toId'>
        /// The identifier that <c>fromId</c> will be mapped to.
        /// </param>
        public void AddIdMapping(string fromId, string toId)
        {
            mIdMap[fromId] = toId;
        }

        /// <summary>
        /// Returns the result of the automatic sign-in attempt. Play Games SDK automatically
        /// prompts users to sign in when the game is started. This API is useful for understanding
        /// if your game has access to Play Games Services and should be used when your game is
        /// started in order to conditionally enable or disable your Play Games Services
        /// integration.
        /// </summary>
        /// <param name="callback">The callback to call when authentication finishes.</param>
        public void Authenticate(Action<SignInStatus> callback)
        {
            mClient.Authenticate(callback);
        }

        /// <summary>
        ///  Provided for compatibility with ISocialPlatform.
        /// </summary>
        /// <seealso cref="Authenticate(Action&lt;bool&gt;,bool)"/>
        /// <param name="unused">Unused parameter for this implementation.</param>
        /// <param name="callback">Callback invoked when complete.</param>
        public void Authenticate(ILocalUser unused, Action<bool> callback)
        {
            Authenticate(status => callback(status == SignInStatus.Success));
        }

        /// <summary>
        ///  Provided for compatibility with ISocialPlatform.
        /// </summary>
        /// <seealso cref="Authenticate(Action&lt;bool&gt;,bool)"/>
        /// <param name="unused">Unused parameter for this implementation.</param>
        /// <param name="callback">Callback invoked when complete.</param>
        public void Authenticate(ILocalUser unused, Action<bool, string> callback)
        {
            Authenticate(status => callback(status == SignInStatus.Success, status.ToString()));
        }

        /// <summary>
        /// Manually requests that your game performs sign in with Play Games Services.
        /// </summary>
        /// <remarks>
        /// Note that a sign-in attempt will be made automatically when your game's application
        /// started. For this reason most games will not need to manually request to perform sign-in
        /// unless the automatic sign-in attempt failed and your game requires access to Play Games
        /// Services.
        /// </remarks>
        /// <param name="callback"></param>
        public void ManuallyAuthenticate(Action<SignInStatus> callback) {
          mClient.ManuallyAuthenticate(callback);
        }

        /// <summary>
        /// Determines whether the user is authenticated.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the user is authenticated; otherwise, <c>false</c>.
        /// </returns>
        public bool IsAuthenticated()
        {
            return mClient != null && mClient.IsAuthenticated();
        }

        /// <summary>
        /// Requests server-side access to Player Games Services for the currently signed in player.
        /// </summary>
        /// When requested an authorization code is returned that can be used by your game-server to
        /// exchange for an access token and conditionally a refresh token (when
        /// <c>forceRefreshToken</c> is true). The access token may then be used by your game-server to
        /// access the Play Games Services web APIs. This is commonly used to complete a sign-in flow
        /// by verifying the Play Games Services player id.
        ///
        /// <p>If <c>forceRefreshToken</c> is true, when exchanging the authorization code a refresh
        /// token will be returned in addition to the access token. The refresh token allows the
        /// game-server to request additional access tokens, allowing your game-server to continue
        /// accesses Play Games Services while the user is not actively playing your app. </p>
        /// <param name="forceRefreshToken">If set to <c>true</c>, a refresh token will be returned along with the access token.</param>
        /// <param name="callback">The callback to invoke with the server authorization code.</param>
        public void RequestServerSideAccess(bool forceRefreshToken, Action<string> callback)
        {
            Misc.CheckNotNull(callback);

            if (!IsAuthenticated())
            {
                OurUtils.Logger.e("RequestServerSideAccess() can only be called after authentication.");
                InvokeCallbackOnGameThread(callback, null);
                return;
            }

            mClient.RequestServerSideAccess(forceRefreshToken, callback);
        }

        /// <summary>
        /// Requests server-side access to Player Games Services for the currently signed in player.
        /// </summary>
        /// When requested an authorization code is returned that can be used by your game-server to
        /// exchange for an access token and conditionally a refresh token (when
        /// <c>forceRefreshToken</c> is true). The access token may then be used by your game-server to
        /// access the Play Games Services web APIs. This is commonly used to complete a sign-in flow
        /// by verifying the Play Games Services player id.
        ///
        /// <p>If <c>forceRefreshToken</c> is true, when exchanging the authorization code a refresh
        /// token will be returned in addition to the access token. The refresh token allows the
        /// game-server to request additional access tokens, allowing your game-server to continue
        /// accesses Play Games Services while the user is not actively playing your app. </p>
        /// <param name="forceRefreshToken">If set to <c>true</c>, a refresh token will be returned along with the access token.</param>
        /// <param name="scopes">The OAuth 2.0 scopes to request access to.</param>
        /// <param name="callback">The callback to invoke with the AuthResponse.</param>
        public void RequestServerSideAccess(bool forceRefreshToken, List<AuthScope> scopes, Action<AuthResponse> callback)
        {
            Misc.CheckNotNull(callback);

            if (!IsAuthenticated())
            {
                OurUtils.Logger.e("RequestServerSideAccess() can only be called after authentication.");
                InvokeCallbackOnGameThread(callback, null);
                return;
            }

            mClient.RequestServerSideAccess(forceRefreshToken, scopes, callback);
        }

        /// <summary>
        /// Requests access to the recall API.
        /// </summary>
        /// <param name="callback">The callback to invoke with the recall access.</param>
        public void RequestRecallAccess(Action<RecallAccess> callback)
        {
            Misc.CheckNotNull(callback);

            mClient.RequestRecallAccessToken(callback);
        }

        /// <summary>
        /// Loads the users.
        /// </summary>
        /// <param name="userIds">User identifiers.</param>
        /// <param name="callback">Callback invoked when complete.</param>
        public void LoadUsers(string[] userIds, Action<IUserProfile[]> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "GetUserId() can only be called after authentication.");
                callback(new IUserProfile[0]);

                return;
            }

            mClient.LoadUsers(userIds, callback);
        }

        /// <summary>
        /// Returns the user's Google ID.
        /// </summary>
        /// <returns>
        /// The user's Google ID. No guarantees are made as to the meaning or format of
        /// this identifier except that it is unique to the user who is signed in.
        /// </returns>
        public string GetUserId()
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "GetUserId() can only be called after authentication.");
                return "0";
            }

            return mClient.GetUserId();
        }

        /// <summary>
        /// Gets the player stats.
        /// </summary>
        /// <param name="callback">Callback invoked when completed.</param>
        public void GetPlayerStats(Action<CommonStatusCodes, PlayerStats> callback)
        {
            if (mClient != null && mClient.IsAuthenticated())
            {
                mClient.GetPlayerStats(callback);
            }
            else
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "GetPlayerStats can only be called after authentication.");

                callback(CommonStatusCodes.SignInRequired, new PlayerStats());
            }
        }

        /// <summary>
        /// Returns the user's display name.
        /// </summary>
        /// <returns>
        /// The user display name. For example, "Bruno Oliveira"
        /// </returns>
        public string GetUserDisplayName()
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "GetUserDisplayName can only be called after authentication.");
                return string.Empty;
            }

            return mClient.GetUserDisplayName();
        }

        /// <summary>
        /// Returns the user's avatar URL if they have one.
        /// </summary>
        /// <returns>
        /// The URL, or null if the user is not authenticated or does not have
        /// an avatar.
        /// </returns>
        public string GetUserImageUrl()
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "GetUserImageUrl can only be called after authentication.");
                return null;
            }

            return mClient.GetUserImageUrl();
        }

        /// <summary>
        /// Reports the progress of an achievement (reveal, unlock or increment). This method attempts
        /// to implement the expected behavior of <c>ISocialPlatform.ReportProgress</c> as closely as possible,
        /// as described below. Although this method works with incremental achievements for compatibility
        /// purposes, calling this method for incremental achievements is not recommended,
        /// since the Play Games API exposes incremental achievements in a very different way
        /// than the interface presented by <c>ISocialPlatform.ReportProgress</c>. The implementation of this
        /// method for incremental achievements attempts to produce the correct result, but may be
        /// imprecise. If possible, call <see cref="IncrementAchievement" /> instead.
        /// </summary>
        /// <param name='achievementID'>
        /// The ID of the achievement to unlock, reveal or increment. This can be a raw Google Play
        /// Games achievement ID (alphanumeric string), or an alias that was previously configured
        /// by a call to <see cref="AddIdMapping" />.
        /// </param>
        /// <param name='progress'>
        /// Progress of the achievement. If the achievement is standard (not incremental), then
        /// a progress of 0.0 will reveal the achievement and 100.0 will unlock it. Behavior of other
        /// values is undefined. If the achievement is incremental, then this value is interpreted
        /// as the total percentage of the achievement's progress that the player should have
        /// as a result of this call (regardless of the progress they had before). So if the
        /// player's previous progress was 30% and this call specifies 50.0, the new progress will
        /// be 50% (not 80%).
        /// </param>
        /// <param name='callback'>
        /// Callback that will be called to report the result of the operation: <c>true</c> on
        /// success, <c>false</c> otherwise.
        /// </param>
        public void ReportProgress(string achievementID, double progress, Action<bool> callback)
        {
            callback = ToOnGameThread(callback);
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "ReportProgress can only be called after authentication.");
                callback.Invoke(false);

                return;
            }

            // map ID, if it's in the dictionary
            GooglePlayGames.OurUtils.Logger.d("ReportProgress, " + achievementID + ", " + progress);
            achievementID = MapId(achievementID);

            // if progress is 0.0, we just want to reveal it
            if (progress < 0.000001)
            {
                GooglePlayGames.OurUtils.Logger.d(
                    "Progress 0.00 interpreted as request to reveal.");
                mClient.RevealAchievement(achievementID, callback);
                return;
            }

            mClient.LoadAchievements(ach =>
            {
                for (int i = 0; i < ach.Length; i++)
                {
                    if (ach[i].Id == achievementID)
                    {
                        if (ach[i].IsIncremental)
                        {
                            GooglePlayGames.OurUtils.Logger.d("Progress " + progress +
                                                              " interpreted as incremental target (approximate).");

                            if (progress >= 0.0 && progress <= 1.0)
                            {
                                // in a previous version, incremental progress was reported by using the range [0-1]
                                GooglePlayGames.OurUtils.Logger.w(
                                    "Progress " + progress +
                                    " is less than or equal to 1. You might be trying to use values in the range of [0,1], while values are expected to be within the range [0,100]. If you are using the latter, you can safely ignore this message.");
                            }

                            mClient.SetStepsAtLeast(achievementID, progressToSteps(progress, ach[i].TotalSteps), callback);
                        }
                        else
                        {
                            if (progress >= 100)
                            {
                                // unlock it!
                                GooglePlayGames.OurUtils.Logger.d("Progress " + progress + " interpreted as UNLOCK.");
                                mClient.UnlockAchievement(achievementID, callback);
                            }
                            else
                            {
                                // not enough to unlock
                                GooglePlayGames.OurUtils.Logger.d(
                                    "Progress " + progress + " not enough to unlock non-incremental achievement.");
                                callback.Invoke(false);
                            }
                        }

                        return;
                    }
                }

                // Achievement not found
                GooglePlayGames.OurUtils.Logger.e("Unable to locate achievement " + achievementID);
                callback.Invoke(false);
            });
        }

        /// <summary>
        /// Converts a progress value to a number of steps.
        /// </summary>
        /// <param name='progress'>
        /// The progress value.
        /// </param>
        /// <param name='totalSteps'>
        /// The total number of steps.
        /// </param>
        /// <returns>
        /// The number of steps.
        /// </returns>
        internal static int progressToSteps(double progress, int totalSteps) {
            return (progress >= 100.0) ? totalSteps : (int) (progress * totalSteps / 100.0);
        }

        /// <summary>
        /// Reveals the achievement with the passed identifier. This is a Play Games extension of the ISocialPlatform API.
        /// </summary>
        /// <remarks>If the operation succeeds, the callback
        /// will be invoked on the game thread with <c>true</c>. If the operation fails, the
        /// callback will be invoked with <c>false</c>. This operation will immediately fail if
        /// the user is not authenticated (the callback will immediately be invoked with
        /// false). If the achievement is already in a revealed state, this call will
        /// succeed immediately.
        /// </remarks>
        /// <param name='achievementID'>
        /// The ID of the achievement to increment. This can be a raw Google Play
        /// Games achievement ID (alphanumeric string), or an alias that was previously configured
        /// by a call to <see cref="AddIdMapping" />.
        /// </param>
        /// <param name='callback'>
        /// The callback to call to report the success or failure of the operation. The callback
        /// will be called with <c>true</c> to indicate success or <c>false</c> for failure.
        /// </param>
        public void RevealAchievement(string achievementID, Action<bool> callback = null)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "RevealAchievement can only be called after authentication.");
                if (callback != null)
                {
                    callback.Invoke(false);
                }

                return;
            }

            // map ID, if it's in the dictionary
            GooglePlayGames.OurUtils.Logger.d(
                "RevealAchievement: " + achievementID);
            achievementID = MapId(achievementID);
            mClient.RevealAchievement(achievementID, callback);
        }

        /// <summary>
        /// Unlocks the achievement with the passed identifier. This is a Play Games extension of the ISocialPlatform API.
        /// </summary>
        /// <remarks>If the operation succeeds, the callback
        /// will be invoked on the game thread with <c>true</c>. If the operation fails, the
        /// callback will be invoked with false. This operation will immediately fail if
        /// the user is not authenticated (the callback will immediately be invoked with
        /// <c>false</c>). If the achievement is already unlocked, this call will
        /// succeed immediately.
        /// </remarks>
        /// <param name='achievementID'>
        /// The ID of the achievement to increment. This can be a raw Google Play
        /// Games achievement ID (alphanumeric string), or an alias that was previously configured
        /// by a call to <see cref="AddIdMapping" />.
        /// </param>
        /// <param name='callback'>
        /// The callback to call to report the success or failure of the operation. The callback
        /// will be called with <c>true</c> to indicate success or <c>false</c> for failure.
        /// </param>
        public void UnlockAchievement(string achievementID, Action<bool> callback = null)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "UnlockAchievement can only be called after authentication.");
                if (callback != null)
                {
                    callback.Invoke(false);
                }

                return;
            }

            // map ID, if it's in the dictionary
            GooglePlayGames.OurUtils.Logger.d(
                "UnlockAchievement: " + achievementID);
            achievementID = MapId(achievementID);
            mClient.UnlockAchievement(achievementID, callback);
        }

        /// <summary>
        /// Increments an achievement. This is a Play Games extension of the ISocialPlatform API.
        /// </summary>
        /// <param name='achievementID'>
        /// The ID of the achievement to increment. This can be a raw Google Play
        /// Games achievement ID (alphanumeric string), or an alias that was previously configured
        /// by a call to <see cref="AddIdMapping" />.
        /// </param>
        /// <param name='steps'>
        /// The number of steps to increment the achievement by.
        /// </param>
        /// <param name='callback'>
        /// The callback to call to report the success or failure of the operation. The callback
        /// will be called with <c>true</c> to indicate success or <c>false</c> for failure.
        /// </param>
        public void IncrementAchievement(string achievementID, int steps, Action<bool> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "IncrementAchievement can only be called after authentication.");
                if (callback != null)
                {
                    callback.Invoke(false);
                }

                return;
            }

            // map ID, if it's in the dictionary
            GooglePlayGames.OurUtils.Logger.d(
                "IncrementAchievement: " + achievementID + ", steps " + steps);
            achievementID = MapId(achievementID);
            mClient.IncrementAchievement(achievementID, steps, callback);
        }

        /// <summary>
        /// Set an achievement to have at least the given number of steps completed.
        /// Calling this method while the achievement already has more steps than
        /// the provided value is a no-op. Once the achievement reaches the
        /// maximum number of steps, the achievement is automatically unlocked,
        /// and any further mutation operations are ignored.
        /// </summary>
        /// <param name='achievementID'>
        /// The ID of the achievement to increment. This can be a raw Google Play
        /// Games achievement ID (alphanumeric string), or an alias that was previously configured
        /// by a call to <see cref="AddIdMapping" />.
        /// </param>
        /// <param name='steps'>
        /// The number of steps to increment the achievement by.
        /// </param>
        /// <param name='callback'>
        /// The callback to call to report the success or failure of the operation. The callback
        /// will be called with <c>true</c> to indicate success or <c>false</c> for failure.
        /// </param>
        public void SetStepsAtLeast(string achievementID, int steps, Action<bool> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "SetStepsAtLeast can only be called after authentication.");
                if (callback != null)
                {
                    callback.Invoke(false);
                }

                return;
            }

            // map ID, if it's in the dictionary
            GooglePlayGames.OurUtils.Logger.d(
                "SetStepsAtLeast: " + achievementID + ", steps " + steps);
            achievementID = MapId(achievementID);
            mClient.SetStepsAtLeast(achievementID, steps, callback);
        }

        /// <summary>
        /// Loads the Achievement descriptions.
        /// </summary>
        /// <param name="callback">The callback to receive the descriptions</param>
        public void LoadAchievementDescriptions(Action<IAchievementDescription[]> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "LoadAchievementDescriptions can only be called after authentication.");
                if (callback != null)
                {
                    callback.Invoke(null);
                }

                return;
            }

            mClient.LoadAchievements(ach =>
            {
                IAchievementDescription[] data = new IAchievementDescription[ach.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = new PlayGamesAchievement(ach[i]);
                }

                callback.Invoke(data);
            });
        }

        /// <summary>
        /// Loads the achievement state for the current user.
        /// </summary>
        /// <param name="callback">The callback to receive the achievements</param>
        public void LoadAchievements(Action<IAchievement[]> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e("LoadAchievements can only be called after authentication.");
                callback.Invoke(null);

                return;
            }

            mClient.LoadAchievements(ach =>
            {
                IAchievement[] data = new IAchievement[ach.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = new PlayGamesAchievement(ach[i]);
                }

                callback.Invoke(data);
            });
        }

        /// <summary>
        /// Creates an achievement object which may be subsequently used to report an
        /// achievement.
        /// </summary>
        /// <returns>
        /// The achievement object.
        /// </returns>
        public IAchievement CreateAchievement()
        {
            return new PlayGamesAchievement();
        }

        /// <summary>
        /// Reports a score to a leaderboard.
        /// </summary>
        /// <param name='score'>
        /// The score to report.
        /// </param>
        /// <param name='board'>
        /// The ID of the leaderboard on which the score is to be posted. This may be a raw
        /// Google Play Games leaderboard ID or an alias configured through a call to
        /// <see cref="AddIdMapping" />.
        /// </param>
        /// <param name='callback'>
        /// The callback to call to report the success or failure of the operation. The callback
        /// will be called with <c>true</c> to indicate success or <c>false</c> for failure.
        /// </param>
        public void ReportScore(long score, string board, Action<bool> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e("ReportScore can only be called after authentication.");
                if (callback != null)
                {
                    callback.Invoke(false);
                }

                return;
            }

            GooglePlayGames.OurUtils.Logger.d("ReportScore: score=" + score + ", board=" + board);
            string leaderboardId = MapId(board);
            mClient.SubmitScore(leaderboardId, score, callback);
        }

        /// <summary>
        /// Submits the score for the currently signed-in player
        /// to the leaderboard associated with a specific id
        /// and metadata (such as something the player did to earn the score).
        /// </summary>
        /// <param name="score">Score to report.</param>
        /// <param name="board">leaderboard id.</param>
        /// <param name="metadata">metadata about the score.</param>
        /// <param name="callback">Callback invoked upon completion.</param>
        public void ReportScore(long score, string board, string metadata, Action<bool> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e("ReportScore can only be called after authentication.");
                if (callback != null)
                {
                    callback.Invoke(false);
                }

                return;
            }

            GooglePlayGames.OurUtils.Logger.d("ReportScore: score=" + score +
                                              ", board=" + board +
                                              " metadata=" + metadata);
            string leaderboardId = MapId(board);
            mClient.SubmitScore(leaderboardId, score, metadata, callback);
        }

        /// <summary>
        /// Loads the scores relative the player.
        /// </summary>
        /// <remarks>This returns the 25
        /// (which is the max results returned by the SDK per call) scores
        /// that are around the player's score on the Public, all time leaderboard.
        /// Use the overloaded methods which are specific to GPGS to modify these
        /// parameters.
        /// </remarks>
        /// <param name="leaderboardId">Leaderboard Id</param>
        /// <param name="callback">Callback to invoke when completed.</param>
        public void LoadScores(string leaderboardId, Action<IScore[]> callback)
        {
            LoadScores(
                leaderboardId,
                LeaderboardStart.PlayerCentered,
                mClient.LeaderboardMaxResults(),
                LeaderboardCollection.Public,
                LeaderboardTimeSpan.AllTime,
                (scoreData) => callback(scoreData.Scores));
        }

        /// <summary>
        /// Loads the scores using the provided parameters. This call may fail when trying to load friends with
        /// ResponseCode.ResolutionRequired if the user has not share the friends list with the game. In this case, use
        /// AskForLoadFriendsResolution to request access.
        /// </summary>
        /// <param name="leaderboardId">Leaderboard identifier.</param>
        /// <param name="start">Start either top scores, or player centered.</param>
        /// <param name="rowCount">Row count. the number of rows to return.</param>
        /// <param name="collection">Collection. social or public</param>
        /// <param name="timeSpan">Time span. daily, weekly, all-time</param>
        /// <param name="callback">Callback to invoke when completed.</param>
        public void LoadScores(
            string leaderboardId,
            LeaderboardStart start,
            int rowCount,
            LeaderboardCollection collection,
            LeaderboardTimeSpan timeSpan,
            Action<LeaderboardScoreData> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e("LoadScores can only be called after authentication.");
                callback(new LeaderboardScoreData(
                    leaderboardId,
                    ResponseStatus.NotAuthorized));
                return;
            }

            mClient.LoadScores(
                leaderboardId,
                start,
                rowCount,
                collection,
                timeSpan,
                callback);
        }

        /// <summary>
        /// Loads more scores. This call may fail when trying to load friends with
        /// ResponseCode.ResolutionRequired if the user has not share the friends list with the game. In this case, use
        /// AskForLoadFriendsResolution to request access.
        /// </summary>
        /// <remarks>This is used to load the next "page" of scores. </remarks>
        /// <param name="token">Token used to recording the loading.</param>
        /// <param name="rowCount">Row count.</param>
        /// <param name="callback">Callback invoked when complete.</param>
        public void LoadMoreScores(
            ScorePageToken token,
            int rowCount,
            Action<LeaderboardScoreData> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e("LoadMoreScores can only be called after authentication.");
                callback(
                    new LeaderboardScoreData(
                        token.LeaderboardId,
                        ResponseStatus.NotAuthorized));
                return;
            }

            mClient.LoadMoreScores(token, rowCount, callback);
        }

        /// <summary>
        /// Returns a leaderboard object that can be configured to
        /// load scores.
        /// </summary>
        /// <returns>The leaderboard object.</returns>
        public ILeaderboard CreateLeaderboard()
        {
            return new PlayGamesLeaderboard(mDefaultLbUi);
        }

        /// <summary>
        /// Shows the standard Google Play Games achievements user interface,
        /// which allows the player to browse their achievements.
        /// </summary>
        public void ShowAchievementsUI()
        {
            ShowAchievementsUI(null);
        }

        /// <summary>
        /// Shows the standard Google Play Games achievements user interface,
        /// which allows the player to browse their achievements.
        /// </summary>
        /// <param name="callback">If non-null, the callback is invoked when
        /// the achievement UI is dismissed</param>
        public void ShowAchievementsUI(Action<UIStatus> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e("ShowAchievementsUI can only be called after authentication.");
                return;
            }

            GooglePlayGames.OurUtils.Logger.d("ShowAchievementsUI callback is " + callback);
            mClient.ShowAchievementsUI(callback);
        }

        /// <summary>
        /// Shows the standard Google Play Games leaderboards user interface,
        /// which allows the player to browse their leaderboards. If you have
        /// configured a specific leaderboard as the default through a call to
        /// <see cref="SetDefaultLeaderboardForUI" />, the UI will show that
        /// specific leaderboard only. Otherwise, a list of all the leaderboards
        /// will be shown.
        /// </summary>
        public void ShowLeaderboardUI()
        {
            GooglePlayGames.OurUtils.Logger.d("ShowLeaderboardUI with default ID");
            ShowLeaderboardUI(MapId(mDefaultLbUi), null);
        }

        /// <summary>
        /// Shows the standard Google Play Games leaderboard UI for the given
        /// leaderboard.
        /// </summary>
        /// <param name='leaderboardId'>
        /// The ID of the leaderboard to display. This may be a raw
        /// Google Play Games leaderboard ID or an alias configured through a call to
        /// <see cref="AddIdMapping" />.
        /// </param>
        public void ShowLeaderboardUI(string leaderboardId)
        {
            if (leaderboardId != null)
            {
                leaderboardId = MapId(leaderboardId);
            }

            ShowLeaderboardUI(leaderboardId, LeaderboardTimeSpan.AllTime, null);
        }

        /// <summary>
        /// Shows the leaderboard UI and calls the specified callback upon
        /// completion.
        /// </summary>
        /// <param name="leaderboardId">leaderboard ID, can be null meaning all leaderboards.</param>
        /// <param name="callback">Callback to call.  If null, nothing is called.</param>
        public void ShowLeaderboardUI(string leaderboardId, Action<UIStatus> callback)
        {
            ShowLeaderboardUI(leaderboardId, LeaderboardTimeSpan.AllTime, callback);
        }

        /// <summary>
        /// Shows the leaderboard UI and calls the specified callback upon
        /// completion.
        /// </summary>
        /// <param name="leaderboardId">leaderboard ID, can be null meaning all leaderboards.</param>
        /// <param name="span">Timespan to display scores in the leaderboard.</param>
        /// <param name="callback">Callback to call.  If null, nothing is called.</param>
        public void ShowLeaderboardUI(
            string leaderboardId,
            LeaderboardTimeSpan span,
            Action<UIStatus> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e("ShowLeaderboardUI can only be called after authentication.");
                if (callback != null)
                {
                    callback(UIStatus.NotAuthorized);
                }

                return;
            }

            GooglePlayGames.OurUtils.Logger.d("ShowLeaderboardUI, lbId=" +
                                              leaderboardId + " callback is " + callback);
            mClient.ShowLeaderboardUI(leaderboardId, span, callback);
        }

        /// <summary>
        /// Sets the default leaderboard for the leaderboard UI. After calling this
        /// method, a call to <see cref="ShowLeaderboardUI" /> will show only the specified
        /// leaderboard instead of showing the list of all leaderboards.
        /// </summary>
        /// <param name='lbid'>
        /// The ID of the leaderboard to display on the default UI. This may be a raw
        /// Google Play Games leaderboard ID or an alias configured through a call to
        /// <see cref="AddIdMapping" />.
        /// </param>
        public void SetDefaultLeaderboardForUI(string lbid)
        {
            GooglePlayGames.OurUtils.Logger.d("SetDefaultLeaderboardForUI: " + lbid);
            if (lbid != null)
            {
                lbid = MapId(lbid);
            }

            mDefaultLbUi = lbid;
        }

        /// <summary>
        /// Loads the friends that also play this game.  See loadConnectedPlayers.
        /// </summary>
        /// <remarks>This is a callback variant of LoadFriends.  When completed,
        /// the friends list set in the user object, so they can accessed via the
        /// friends property as needed.
        /// </remarks>
        /// <param name="user">The current local user</param>
        /// <param name="callback">Callback invoked when complete.</param>
        public void LoadFriends(ILocalUser user, Action<bool> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "LoadScores can only be called after authentication.");
                if (callback != null)
                {
                    callback(false);
                }

                return;
            }

            mClient.LoadFriends(callback);
        }

        /// <summary>
        /// Loads the leaderboard based on the constraints in the leaderboard
        /// object.
        /// </summary>
        /// <param name="board">The leaderboard object.  This is created by
        /// calling CreateLeaderboard(), and then initialized appropriately.</param>
        /// <param name="callback">Callback invoked when complete.</param>
        public void LoadScores(ILeaderboard board, Action<bool> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e("LoadScores can only be called after authentication.");
                if (callback != null)
                {
                    callback(false);
                }

                return;
            }

            LeaderboardTimeSpan timeSpan;
            switch (board.timeScope)
            {
                case TimeScope.AllTime:
                    timeSpan = LeaderboardTimeSpan.AllTime;
                    break;
                case TimeScope.Week:
                    timeSpan = LeaderboardTimeSpan.Weekly;
                    break;
                case TimeScope.Today:
                    timeSpan = LeaderboardTimeSpan.Daily;
                    break;
                default:
                    timeSpan = LeaderboardTimeSpan.AllTime;
                    break;
            }

            ((PlayGamesLeaderboard) board).loading = true;
            GooglePlayGames.OurUtils.Logger.d("LoadScores, board=" + board +
                                              " callback is " + callback);
            mClient.LoadScores(
                board.id,
                LeaderboardStart.PlayerCentered,
                board.range.count > 0 ? board.range.count : mClient.LeaderboardMaxResults(),
                board.userScope == UserScope.FriendsOnly ? LeaderboardCollection.Social : LeaderboardCollection.Public,
                timeSpan,
                (scoreData) => HandleLoadingScores(
                    (PlayGamesLeaderboard) board, scoreData, callback));
        }

        /// <summary>
        /// Check if the leaderboard is currently loading.
        /// </summary>
        /// <returns><c>true</c>, if loading was gotten, <c>false</c> otherwise.</returns>
        /// <param name="board">The leaderboard to check for loading in progress</param>
        public bool GetLoading(ILeaderboard board)
        {
            return board != null && board.loading;
        }

        /// <summary>
        /// Shows the Player Profile UI for the given user identifier.
        /// </summary>
        /// <param name="userId">User Identifier.</param>
        /// <param name="otherPlayerInGameName">
        /// The game's own display name of the player referred to by userId.
        /// </param>
        /// <param name="currentPlayerInGameName">
        /// The game's own display name of the current player.
        /// </param>
        /// <param name="callback">Callback invoked upon completion.</param>
        public void ShowCompareProfileWithAlternativeNameHintsUI(string userId,
            string otherPlayerInGameName,
            string currentPlayerInGameName,
            Action<UIStatus> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "ShowCompareProfileWithAlternativeNameHintsUI can only be called after authentication.");
                InvokeCallbackOnGameThread(callback, UIStatus.NotAuthorized);

                return;
            }

            GooglePlayGames.OurUtils.Logger.d(
                "ShowCompareProfileWithAlternativeNameHintsUI, userId=" + userId + " callback is " +
                callback);
            mClient.ShowCompareProfileWithAlternativeNameHintsUI(userId, otherPlayerInGameName,
                currentPlayerInGameName, callback);
        }

        /// <summary>
        /// Returns if the user has allowed permission for the game to access the friends list.
        /// </summary>
        /// <param name="forceReload">If <c>true</c>, this call will clear any locally cached data and
        /// attempt to fetch the latest data from the server. Normally, this should be set to
        /// <c>false</c> to gain advantages of data caching.</param>
        /// <param name="callback">Callback invoked upon completion.</param>
        public void GetFriendsListVisibility(bool forceReload,
            Action<FriendsListVisibilityStatus> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "GetFriendsListVisibility can only be called after authentication.");
                InvokeCallbackOnGameThread(callback, FriendsListVisibilityStatus.NotAuthorized);
                return;
            }

            GooglePlayGames.OurUtils.Logger.d("GetFriendsListVisibility, callback is " + callback);
            mClient.GetFriendsListVisibility(forceReload, callback);
        }

        /// <summary>
        /// Shows the appropriate platform-specific friends sharing UI.
        /// <param name="callback">The callback to invoke when complete. If null,
        /// no callback is called. </param>
        /// </summary>
        public void AskForLoadFriendsResolution(Action<UIStatus> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "AskForLoadFriendsResolution can only be called after authentication.");
                InvokeCallbackOnGameThread(callback, UIStatus.NotAuthorized);
                return;
            }

            GooglePlayGames.OurUtils.Logger.d("AskForLoadFriendsResolution callback is " + callback);
            mClient.AskForLoadFriendsResolution(callback);
        }

        /// <summary>
        /// Gets status of the last call to load friends.
        /// </summary>
        public LoadFriendsStatus GetLastLoadFriendsStatus()
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "GetLastLoadFriendsStatus can only be called after authentication.");
                return LoadFriendsStatus.NotAuthorized;
            }

            return mClient.GetLastLoadFriendsStatus();
        }

        /// <summary>
        /// Loads the first page of the user's friends.
        /// </summary>
        /// <param name="pageSize">
        /// The number of entries to request for this initial page. Note that if cached
        /// data already exists, the returned buffer may contain more than this size, but it is
        /// guaranteed to contain at least this many if the collection contains enough records.
        /// </param>
        /// <param name="forceReload">
        /// If true, this call will clear any locally cached data and attempt to
        /// fetch the latest data from the server. This would commonly be used for something like a
        /// user-initiated refresh. Normally, this should be set to <c>false</c> to gain advantages of data caching.
        /// </param>
        /// <param name="callback">Callback invoked upon completion with the status.</param>
        public void LoadFriends(int pageSize, bool forceReload,
            Action<LoadFriendsStatus> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "LoadFriends can only be called after authentication.");
                InvokeCallbackOnGameThread(callback, LoadFriendsStatus.NotAuthorized);
                return;
            }

            mClient.LoadFriends(pageSize, forceReload, callback);
        }

        /// <summary>
        /// Loads the friends list page
        /// </summary>
        /// <param name="pageSize">
        /// The number of entries to request for this initial page. Note that if cached
        /// data already exists, the returned buffer may contain more than this size, but it is
        /// guaranteed to contain at least this many if the collection contains enough records.
        /// </param>
        /// <param name="callback"></param>
        public void LoadMoreFriends(int pageSize, Action<LoadFriendsStatus> callback)
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.e(
                    "LoadMoreFriends can only be called after authentication.");
                InvokeCallbackOnGameThread(callback, LoadFriendsStatus.NotAuthorized);
                return;
            }

            mClient.LoadMoreFriends(pageSize, callback);
        }

        /// <summary>
        /// Handles the processing of scores during loading.
        /// </summary>
        /// <param name="board">leaderboard being loaded</param>
        /// <param name="scoreData">Score data.</param>
        /// <param name="callback">Callback invoked when complete.</param>
        internal void HandleLoadingScores(
            PlayGamesLeaderboard board,
            LeaderboardScoreData scoreData,
            Action<bool> callback)
        {
            bool ok = board.SetFromData(scoreData);
            if (ok && !board.HasAllScores() && scoreData.NextPageToken != null)
            {
                int rowCount = board.range.count - board.ScoreCount;

                // need to load more scores
                mClient.LoadMoreScores(
                    scoreData.NextPageToken,
                    rowCount,
                    (nextScoreData) =>
                        HandleLoadingScores(board, nextScoreData, callback));
            }
            else
            {
                callback(ok);
            }
        }

        /// <summary>
        /// Internal implmentation of getFriends.Gets the friends.
        /// </summary>
        /// <returns>The friends.</returns>
        internal IUserProfile[] GetFriends()
        {
            if (!IsAuthenticated())
            {
                GooglePlayGames.OurUtils.Logger.d("Cannot get friends when not authenticated!");
                return new IUserProfile[0];
            }

            return mClient.GetFriends();
        }

        /// <summary>
        /// Maps the alias to the identifier.
        /// </summary>
        /// <param name="id">Alias to map</param>
        /// <remarks>This maps an aliased ID to the actual id.  The intent of
        /// this method is to allow easy to read constants to be used instead of
        /// the generated ids.
        /// </remarks>
        /// <returns>The identifier, or null if not found.</returns>
        private string MapId(string id)
        {
            if (id == null)
            {
                return null;
            }

            if (mIdMap.ContainsKey(id))
            {
                string result = mIdMap[id];
                GooglePlayGames.OurUtils.Logger.d("Mapping alias " + id + " to ID " + result);
                return result;
            }

            return id;
        }

        private static void InvokeCallbackOnGameThread<T>(Action<T> callback, T data)
        {
            if (callback == null)
            {
                return;
            }

            PlayGamesHelperObject.RunOnGameThread(() => { callback(data); });
        }

        private static Action<T> ToOnGameThread<T>(Action<T> toConvert)
        {
            if (toConvert == null)
            {
                return delegate { };
            }

            return (val) => PlayGamesHelperObject.RunOnGameThread(() => toConvert(val));
        }
    }
}
#endif
