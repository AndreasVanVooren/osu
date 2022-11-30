﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Graphics.Sprites;
using osuTK.Graphics;
using osu.Game.Graphics.UserInterface;
using System;
using osuTK;
using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterfaceV2;

namespace osu.Game.Overlays.Comments
{
    public abstract partial class CommentEditor : CompositeDrawable
    {
        private const int side_padding = 8;

        public Action<string>? OnCommit;

        /// <summary>
        /// Whether editor is waiting for submit action to complete.
        /// </summary>
        public bool IsSubmitting => CommitButton.IsLoadingSpinnerShown;

        protected abstract LocalisableString FooterText { get; }

        protected abstract LocalisableString CommitButtonText { get; }

        protected abstract LocalisableString TextBoxPlaceholder { get; }

        protected FillFlowContainer ButtonsContainer { get; private set; } = null!;

        protected readonly Bindable<string> Current = new Bindable<string>();

        protected EditorCommitButton CommitButton = null!;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            EditorTextBox textBox;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Masking = true;
            CornerRadius = 6;
            BorderThickness = 3;
            BorderColour = colourProvider.Background3;

            AddRangeInternal(new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background3
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Y,
                    RelativeSizeAxes = Axes.X,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        textBox = new EditorTextBox
                        {
                            Height = 40,
                            RelativeSizeAxes = Axes.X,
                            PlaceholderText = TextBoxPlaceholder,
                            Current = Current
                        },
                        new Container
                        {
                            Name = @"Footer",
                            RelativeSizeAxes = Axes.X,
                            Height = 40,
                            Padding = new MarginPadding { Horizontal = side_padding },
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold),
                                    Text = FooterText
                                },
                                ButtonsContainer = new FillFlowContainer
                                {
                                    Name = @"Buttons",
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(5, 0),
                                    Child = CommitButton = new EditorCommitButton
                                    {
                                        Text = CommitButtonText,
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Action = () =>
                                        {
                                            OnCommit?.Invoke(Current.Value);
                                            Current.Value = string.Empty;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });

            textBox.OnCommit += (_, _) =>
            {
                if (CommitButton.IsLoadingSpinnerShown)
                    return;

                CommitButton.TriggerClick();
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Current.BindValueChanged(text => CommitButton.IsBlocked.Value = string.IsNullOrEmpty(text.NewValue), true);
        }

        private partial class EditorTextBox : BasicTextBox
        {
            protected override float LeftRightPadding => side_padding;

            protected override Color4 SelectionColour => Color4.Gray;

            private OsuSpriteText placeholder = null!;

            public EditorTextBox()
            {
                Masking = false;
                TextContainer.Height = 0.4f;
            }

            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider colourProvider)
            {
                BackgroundUnfocused = BackgroundFocused = colourProvider.Background5;
                placeholder.Colour = colourProvider.Background3;
                BackgroundCommit = colourProvider.Background3;
            }

            protected override SpriteText CreatePlaceholder() => placeholder = new OsuSpriteText
            {
                Font = OsuFont.GetFont(weight: FontWeight.Regular),
            };

            protected override Drawable GetDrawableCharacter(char c) => new FallingDownContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = new OsuSpriteText { Text = c.ToString(), Font = OsuFont.GetFont(size: CalculatedTextSize) },
            };
        }

        protected sealed partial class EditorCommitButton : RoundedButton
        {
            private const int duration = 200;

            private readonly LoadingSpinner spinner;
            private readonly OsuSpriteText text = null!;

            public readonly BindableBool IsBlocked = new BindableBool();

            private bool isLoadingSpinnerShown;

            /// <summary>
            /// Whether loading spinner shown.
            /// </summary>
            public bool IsLoadingSpinnerShown
            {
                get => isLoadingSpinnerShown;
                set
                {
                    isLoadingSpinnerShown = value;
                    Enabled.Value = !value && !IsBlocked.Value;
                    spinner.FadeTo(value ? 1f : 0f, duration, Easing.OutQuint);
                    text.FadeTo(value ? 0f : 1f, duration, Easing.OutQuint);
                }
            }

            public EditorCommitButton()
            {
                Width = 100;
                Height = 30;
                Add(spinner = new LoadingSpinner
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(14),
                    Depth = -2,
                });
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                IsBlocked.BindValueChanged(e =>
                {
                    Enabled.Value = !IsLoadingSpinnerShown && !e.NewValue;
                }, true);
            }
        }
    }
}
