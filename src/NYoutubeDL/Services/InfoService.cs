// Copyright 2021 Brian Allred
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

namespace NYoutubeDL.Services
{
    #region Using

    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Models;
    using NYoutubeDL.Helpers;
    using Options;

    #endregion

    /// <summary>
    /// Service containing logic for retrieving information about videos / playlists
    /// </summary>
    internal static class InfoService
    {
        /// <summary>
        ///     Asynchronously retrieve video / playlist information
        /// </summary>
        /// <param name="ydl">
        ///     The client
        /// </param>
        /// <param name="cancellationToken">
        ///     The cancellation token
        /// </param>
        /// <returns>
        ///     An object containing the download information
        /// </returns>
        internal static async Task<DownloadInfo> GetDownloadInfoAsync(this YoutubeDLP ydl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ydl.VideoUrl))
            {
                return null;
            }

            List<DownloadInfo> infos = new List<DownloadInfo>();

            // Save the original options and set the ones we need
            string originalOptions = ydl.Options.Serialize();
            SetInfoOptions(ydl);

            // Save the original event delegates and clear the event handler
            Delegate[] originalStdOutputEventDelegates = null;
            if (ydl.stdOutputEvent != null)
            {
                originalStdOutputEventDelegates = ydl.stdOutputEvent.GetInvocationList();
                ydl.stdOutputEvent = null;
            }

            Delegate[] originalStdOutputClosedEventDelegates = null;
            if (ydl.stdOutputClosedEvent != null)
            {
                originalStdOutputClosedEventDelegates = ydl.stdOutputClosedEvent.GetInvocationList();
                ydl.stdOutputClosedEvent = null;
            }

            // Local function for easier event handling
            void ParseInfoJson(object sender, string output)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    infos.Add(DownloadInfo.CreateDownloadInfo(output));
                }
            }

            var outputClosed = false;
            void ReveiveOutputClosed(object sender, EventArgs output)
            {
                outputClosed = true;
            }

            ydl.StandardOutputEvent += ParseInfoJson;
            ydl.StandardOutputClosedEvent += ReveiveOutputClosed;

            // Set up the command
            PreparationService.SetupPrepare(ydl);

            // Download the info
            await DownloadService.DownloadAsync(ydl, cancellationToken);

            while ((!ydl.process.HasExited || !outputClosed) && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1);
            }

            if (!cancellationToken.IsCancellationRequested && infos.Count > 0)
            {
                // Set the info object
                ydl.Info = infos.Count > 1 ? new MultiDownloadInfo(infos) : infos[0];
            }

            // Set options back to what they were
            ydl.Options = Options.Deserialize(originalOptions);

            // Clear the event handler
            ydl.stdOutputEvent = null;
            ydl.stdOutputClosedEvent = null;

            // Re-subscribe to each event delegate
            if (originalStdOutputEventDelegates != null)
            {
                foreach (Delegate del in originalStdOutputEventDelegates)
                {
                    ydl.StandardOutputEvent += (EventHandler<string>)del;
                }
            }

            if (originalStdOutputClosedEventDelegates != null)
            {
                foreach (Delegate del in originalStdOutputClosedEventDelegates)
                {
                    ydl.StandardOutputClosedEvent += (EventHandler)del;
                }
            }

            return infos.Count > 0 ? ydl.Info : null;
        }

        /// <summary>
        ///     Synchronously retrieve video / playlist information
        /// </summary>
        /// <param name="ydl">
        ///     The client
        /// </param>
        /// <param name="cancellationToken">
        ///     The cancellation token
        /// </param>
        /// <returns>
        ///     An object containing the download information
        /// </returns>
        internal static DownloadInfo GetDownloadInfo(this YoutubeDLP ydl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ydl.VideoUrl))
            {
                return null;
            }

            List<DownloadInfo> infos = new List<DownloadInfo>();

            // Save the original options and set the ones we need
            string originalOptions = ydl.Options.Serialize();
            SetInfoOptions(ydl);

            // Save the original event delegates and clear the event handler
            Delegate[] originalStdOutputEventDelegates = null;
            if (ydl.stdOutputEvent != null)
            {
                originalStdOutputEventDelegates = ydl.stdOutputEvent.GetInvocationList();
                ydl.stdOutputEvent = null;
            }
            Delegate[] originalStdOutputClosedEventDelegates = null;
            if (ydl.stdOutputClosedEvent != null)
            {
                originalStdOutputEventDelegates = ydl.stdOutputClosedEvent.GetInvocationList();
                ydl.stdOutputClosedEvent = null;
            }

            // Local function for easier event handling
            void ParseInfoJson(object sender, string output)
            {
                infos.Add(DownloadInfo.CreateDownloadInfo(output));
            }

            var outputClosed = false;
            void ReveiveOutputClosed(object sender, EventArgs output)
            {
                outputClosed = true;
            }

            ydl.StandardOutputEvent += ParseInfoJson;
            ydl.StandardOutputClosedEvent += ReveiveOutputClosed;

            // Set up the command
            PreparationService.SetupPrepare(ydl);

            // Download the info
            DownloadService.Download(ydl, cancellationToken);

            while ((!ydl.process.HasExited || !outputClosed) && !cancellationToken.IsCancellationRequested)
            {
                Thread.Sleep(1);
            }

            if (!cancellationToken.IsCancellationRequested && infos.Count > 0)
            {
                // Set the info object
                ydl.Info = infos.Count > 1 ? new MultiDownloadInfo(infos) : infos[0];
            }

            // Set options back to what they were
            ydl.Options = Options.Deserialize(originalOptions);

            // Clear the event handler
            ydl.stdOutputEvent = null;
            ydl.stdOutputClosedEvent = null;

            // Re-subscribe to each event delegate
            if (originalStdOutputEventDelegates != null)
            {
                foreach (Delegate del in originalStdOutputEventDelegates)
                {
                    ydl.StandardOutputEvent += (EventHandler<string>)del;
                }
            }
            if (originalStdOutputClosedEventDelegates != null)
            {
                foreach (Delegate del in originalStdOutputClosedEventDelegates)
                {
                    ydl.StandardOutputClosedEvent += (EventHandler)del;
                }
            }

            return infos.Count > 0 ? ydl.Info : null;
        }

        /// <summary>
        ///     Asynchronously retrieve video / playlist information
        /// </summary>
        /// <param name="ydl">
        ///     The client
        /// </param>
        /// <param name="url">
        ///     URL of video / playlist
        /// </param>
        /// <param name="cancellationToken">
        ///     The cancellation token
        /// </param>
        /// <returns>
        ///     An object containing the download information
        /// </returns>
        internal static async Task<DownloadInfo> GetDownloadInfoAsync(this YoutubeDLP ydl, string url, CancellationToken cancellationToken)
        {
            ydl.VideoUrl = url;
            await GetDownloadInfoAsync(ydl, cancellationToken);
            return ydl.Info;
        }

        /// <summary>
        ///     Synchronously retrieve video / playlist information
        /// </summary>
        /// <param name="ydl">
        ///     The client
        /// </param>
        /// <param name="url">
        ///     URL of video / playlist
        /// </param>
        /// <param name="cancellationToken">
        ///     The cancellation token
        /// </param>
        /// <returns>
        ///     An object containing the download information
        /// </returns>
        internal static DownloadInfo GetDownloadInfo(this YoutubeDLP ydl, string url, CancellationToken cancellationToken)
        {
            ydl.VideoUrl = url;
            return GetDownloadInfo(ydl, cancellationToken);
        }

        private static void SetInfoOptions(YoutubeDLP ydl)
        {
            Options infoOptions = new Options
            {
                VerbositySimulationOptions =
                {
                    DumpSingleJson = true,
                    Simulate = true
                },
                GeneralOptions =
                {
                    FlatPlaylist = !ydl.RetrieveAllInfo,
                    IgnoreErrors = true,
                },
                AuthenticationOptions =
                {
                    Username = ydl.Options.AuthenticationOptions.Username,
                    Password = ydl.Options.AuthenticationOptions.Password,
                    NetRc = ydl.Options.AuthenticationOptions.NetRc,
                    VideoPassword = ydl.Options.AuthenticationOptions.VideoPassword,
                    TwoFactor = ydl.Options.AuthenticationOptions.TwoFactor
                },
                VideoFormatOptions =
                {
                    FormatAdvanced = ydl.Options.VideoFormatOptions.FormatAdvanced
                },
                WorkaroundsOptions =
                {
                    UserAgent = ydl.Options.WorkaroundsOptions.UserAgent
                },
                VideoSelectionOptions =
                {
                    NoPlaylist = ydl.Options.VideoSelectionOptions.NoPlaylist
                },
				SubtitleOptions = {
					AllSubs = ydl.Options.SubtitleOptions.AllSubs,
					SubFormat = ydl.Options.SubtitleOptions.SubFormat,
					WriteSub = ydl.Options.SubtitleOptions.WriteSub,
					WriteAutoSub = ydl.Options.SubtitleOptions.WriteAutoSub,
				}
            };

            if (ydl.Options.VideoFormatOptions.Format != Enums.VideoFormat.undefined)
            {
                infoOptions.VideoFormatOptions.Format = ydl.Options.VideoFormatOptions.Format;
            }

            ydl.Options = infoOptions;
        }
    }
}