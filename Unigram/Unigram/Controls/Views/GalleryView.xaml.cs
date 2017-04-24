﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Telegram.Api.Helpers;
using Telegram.Api.Services.FileManager;
using Telegram.Api.TL;
using Template10.Common;
using Unigram.Converters;
using Unigram.ViewModels;
using Unigram.Views;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Unigram.Controls.Views
{
    public sealed partial class GalleryView : ContentDialogBase
    {
        public GalleryViewModelBase ViewModel => DataContext as GalleryViewModelBase;

        public BindConvert Convert => BindConvert.Current;

        private FrameworkElement _firstImage;
        private MediaPlayerElement _mediaPlayer;
        private MediaPlayerSurface _mediaSurface;

        private ImageView _surface;
        private SpriteVisual _surfaceVisual;

        private Visual _layerVisual;
        private Visual _topBarVisual;
        private Visual _botBarVisual;

        public GalleryView()
        {
            InitializeComponent();

            _mediaPlayer = new MediaPlayerElement { Style = Resources["yolo"] as Style };
            _mediaPlayer.AreTransportControlsEnabled = true;
            _mediaPlayer.TransportControls = Boh;
            _mediaPlayer.SetMediaPlayer(new MediaPlayer());

            _layerVisual = ElementCompositionPreview.GetElementVisual(Layer);
            _topBarVisual = ElementCompositionPreview.GetElementVisual(TopBar);
            _botBarVisual = ElementCompositionPreview.GetElementVisual(BotBar);

            _layerVisual.Opacity = 0;
            _topBarVisual.Offset = new Vector3(0, -48, 0);
            _botBarVisual.Offset = new Vector3(0, 48, 0);
        }

        protected override void OnBackRequestedOverride(object sender, HandledEventArgs e)
        {
            if (Flip.SelectedIndex == 0 && _firstImage != null && _firstImage.ActualWidth > 0)
            {
                var animation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("FullScreenPicture", _firstImage);
                if (animation != null)
                {
                    Prepare();

                    if (_layerVisual != null && _topBarVisual != null && _botBarVisual != null)
                    {
                        var batch = _layerVisual.Compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

                        var easing = ConnectedAnimationService.GetForCurrentView().DefaultEasingFunction;
                        var duration = ConnectedAnimationService.GetForCurrentView().DefaultDuration;

                        var animOpacity = _layerVisual.Compositor.CreateScalarKeyFrameAnimation();
                        animOpacity.InsertKeyFrame(0, 1, easing);
                        animOpacity.InsertKeyFrame(1, 0, easing);
                        animOpacity.Duration = duration;
                        _layerVisual.StartAnimation("Opacity", animOpacity);

                        var animTop = _layerVisual.Compositor.CreateVector3KeyFrameAnimation();
                        animTop.InsertKeyFrame(1, new Vector3(0, -48, 0), easing);
                        animTop.Duration = duration;
                        _topBarVisual.StartAnimation("Offset", animTop);

                        var animBot = _layerVisual.Compositor.CreateVector3KeyFrameAnimation();
                        animBot.InsertKeyFrame(1, new Vector3(0, 48, 0), easing);
                        animBot.Duration = duration;
                        _botBarVisual.StartAnimation("Offset", animBot);

                        batch.End();
                    }

                    animation.Completed += (s, args) =>
                    {
                        Hide();
                    };
                }
            }
            else
            {
                Hide();
            }

            e.Handled = true;
        }

        private void ImageView_ImageOpened(object sender, RoutedEventArgs e)
        {
            var image = sender as FrameworkElement;
            if (image != null)
            {
                image.Opacity = 1;
            }

            if (image.DataContext == ViewModel.SelectedItem)
            {
                _firstImage = image;

                var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("FullScreenPicture");
                if (animation != null)
                {
                    if (_layerVisual != null && _topBarVisual != null && _botBarVisual != null)
                    {
                        var batch = _layerVisual.Compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

                        var easing = ConnectedAnimationService.GetForCurrentView().DefaultEasingFunction;
                        var duration = ConnectedAnimationService.GetForCurrentView().DefaultDuration;

                        var animOpacity = _layerVisual.Compositor.CreateScalarKeyFrameAnimation();
                        animOpacity.InsertKeyFrame(0, 0, easing);
                        animOpacity.InsertKeyFrame(1, 1, easing);
                        animOpacity.Duration = duration;
                        _layerVisual.StartAnimation("Opacity", animOpacity);

                        var animTop = _layerVisual.Compositor.CreateVector3KeyFrameAnimation();
                        animTop.InsertKeyFrame(1, new Vector3(0, 0, 0), easing);
                        animTop.Duration = duration;
                        _topBarVisual.StartAnimation("Offset", animTop);

                        var animBot = _layerVisual.Compositor.CreateVector3KeyFrameAnimation();
                        animBot.InsertKeyFrame(1, new Vector3(0, 0, 0), easing);
                        animBot.Duration = duration;
                        _botBarVisual.StartAnimation("Offset", animBot);

                        batch.End();
                    }

                    Flip.Opacity = 1;
                    animation.TryStart(image);
                }
            }
        }

        private void ImageView_Unloaded(object sender, RoutedEventArgs e)
        {
            var image = sender as FrameworkElement;
            if (image != null)
            {
                image.Opacity = 0;
            }
        }

        private string ConvertDate(int value)
        {
            var date = Convert.DateTime(value);
            return string.Format("{0} at {1}", date.Date == DateTime.Now.Date ? "Today" : Convert.ShortDate.Format(date), Convert.ShortTime.Format(date));
        }

        private async void DownloadDocument_Click(object sender, RoutedEventArgs e)
        {
            var border = sender as TransferButton;
            var message = border.DataContext as GalleryMessageItem;
            var documentMedia = message.Message.Media as TLMessageMediaDocument;
            if (documentMedia != null)
            {
                var document = documentMedia.Document as TLDocument;
                if (document != null)
                {
                    var fileName = document.GetFileName();

                    if (File.Exists(FileUtils.GetTempFileName(fileName)))
                    {
                        Play(message);
                    }
                    else
                    {
                        if (documentMedia.DownloadingProgress > 0 && documentMedia.DownloadingProgress < 1)
                        {
                            var manager = UnigramContainer.Current.ResolveType<IDownloadDocumentFileManager>();
                            manager.CancelDownloadFile(document);

                            documentMedia.DownloadingProgress = 0;
                            border.Update();
                        }
                        else if (documentMedia.UploadingProgress > 0 && documentMedia.UploadingProgress < 1)
                        {
                            var manager = UnigramContainer.Current.ResolveType<IUploadDocumentManager>();
                            manager.CancelUploadFile(document.Id);

                            documentMedia.UploadingProgress = 0;
                            border.Update();
                        }
                        else
                        {
                            var manager = UnigramContainer.Current.ResolveType<IDownloadDocumentFileManager>();
                            var operation = manager.DownloadFileAsync(document.FileName, document.DCId, document.ToInputFileLocation(), document.Size);

                            documentMedia.DownloadingProgress = 0.02;
                            border.Update();

                            var download = await operation.AsTask(documentMedia.Download());
                            if (download != null)
                            {
                                border.Update();

                                Play(message);
                            }
                        }
                    }
                }
            }
        }

        private void Play(GalleryItem item)
        {
            var container = Flip.ContainerFromItem(item) as ContentControl;
            if (container != null && container.ContentTemplateRoot is Grid parent)
            {
                _surface = parent.FindName("Surface") as ImageView;

                _mediaPlayer.MediaPlayer.SetSurfaceSize(new Size(_surface.ActualWidth, _surface.ActualHeight));

                var swapchain = _mediaPlayer.MediaPlayer.GetSurface(_layerVisual.Compositor);
                var brush = _layerVisual.Compositor.CreateSurfaceBrush(swapchain.CompositionSurface);
                var size = new Vector2((float)_surface.ActualWidth, (float)_surface.ActualHeight);

                _surfaceVisual = _layerVisual.Compositor.CreateSpriteVisual();
                _surfaceVisual.Size = size;
                _surfaceVisual.Brush = brush;

                ElementCompositionPreview.SetElementChildVisual(_surface, _surfaceVisual);

                _mediaSurface = swapchain;
                _mediaPlayer.Source = MediaSource.CreateFromUri(item.GetVideoSource());
                _mediaPlayer.MediaPlayer.Play();
            }
        }

        private void Flip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_surface != null)
            {
                ElementCompositionPreview.SetElementChildVisual(_surface, null);
                _surface = null;
            }

            if (_surfaceVisual != null)
            {
                _surfaceVisual.Brush = null;
                _surfaceVisual = null;
            }

            _mediaPlayer.MediaPlayer.Pause();
            _mediaPlayer.Source = null;
        }
    }
}
