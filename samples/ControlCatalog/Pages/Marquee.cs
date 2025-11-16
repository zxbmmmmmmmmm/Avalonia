using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using HarfBuzzSharp;

namespace ControlCatalog.Pages;

public class Marquee : ContentControl
{
    public static readonly StyledProperty<bool> IsRunningProperty = AvaloniaProperty.Register<Marquee, bool>(
        nameof(IsRunning), true);

    public static readonly StyledProperty<Direction> DirectionProperty = AvaloniaProperty.Register<Marquee, Direction>(
        nameof(Direction), Direction.Right);

    public static readonly StyledProperty<double> SpeedProperty = AvaloniaProperty.Register<Marquee, double>(
        nameof(Speed), 60.0, coerce: OnCoerceSpeed);

    private Vector3DKeyFrameAnimation? _animation;

    private DateTimeOffset _animationStartTime = DateTimeOffset.UtcNow;
    private DateTimeOffset _animationStopTime = DateTimeOffset.UtcNow;

    // 已行进的总距离（像素）
    private double _distanceElapsed;
    // 最近一次运行阶段使用的速度（像素/秒）
    private double _lastSpeed;

    // 保留初始未动画偏移，避免暂停后因使用当前偏移作为“基准”而产生位置漂移
    private Vector3D? _initialOffset;
    private bool _hasInitialOffset;

    private static double OnCoerceSpeed(AvaloniaObject arg1, double arg2) => arg2 < 0 ? 0 : arg2;

    static Marquee()
    {
        ClipToBoundsProperty.OverrideDefaultValue<Marquee>(true);
        HorizontalContentAlignmentProperty.OverrideDefaultValue<Marquee>(HorizontalAlignment.Center);
        VerticalContentAlignmentProperty.OverrideDefaultValue<Marquee>(VerticalAlignment.Center);
        HorizontalContentAlignmentProperty.Changed.AddClassHandler<Marquee>((o, _) => o.InvalidatePresenterPosition());
        VerticalContentAlignmentProperty.Changed.AddClassHandler<Marquee>((o, _) => o.InvalidatePresenterPosition());
        IsRunningProperty.Changed.AddClassHandler<Marquee, bool>((o, args) => o.OnIsRunningChanged(args));
        SpeedProperty.Changed.AddClassHandler<Marquee, double>((o, args) => o.OnSpeedChanged(args));
    }

    private void OnIsRunningChanged(AvaloniaPropertyChangedEventArgs<bool> args)
    {
        if (Presenter is null || Presenter.Child is not { } control)
            return;

        var compositionVisual = ElementComposition.GetElementVisual(control);
        var presenterVisual = ElementComposition.GetElementVisual(Presenter);
        var compositor = compositionVisual!.Compositor;

        var containerSize = presenterVisual!.Size;
        var contentSize = compositionVisual.Size;

        double pathLength = Direction switch
        {
            Direction.Left or Direction.Right => containerSize.X + contentSize.X,
            Direction.Up or Direction.Down => containerSize.Y + contentSize.Y,
            _ => 0
        };

        if (args.NewValue.Value)
        {
            if (Speed <= 0 || pathLength <= 0)
                return;

            // 不再在恢复时重复结算距离（已在暂停时结算）
            if (!_hasInitialOffset)
            {
                _initialOffset = compositionVisual.Offset;
                _hasInitialOffset = true;
            }

            _lastSpeed = Speed;

            var progress = pathLength > 0 ? (_distanceElapsed % pathLength) / pathLength : 0.0;

            var baseOffset = _initialOffset ?? compositionVisual.Offset;
            var start = baseOffset;
            var end = baseOffset;

            switch (Direction)
            {
                case Direction.Left:
                    start = baseOffset with { X = baseOffset.X + containerSize.X };
                    end = baseOffset with { X = baseOffset.X - contentSize.X };
                    break;
                case Direction.Right:
                    start = baseOffset with { X = baseOffset.X - contentSize.X };
                    end = baseOffset with { X = baseOffset.X + containerSize.X };
                    break;
                case Direction.Up:
                    start = baseOffset with { Y = baseOffset.Y + containerSize.Y };
                    end = baseOffset with { Y = baseOffset.Y - contentSize.Y };
                    break;
                case Direction.Down:
                    start = baseOffset with { Y = baseOffset.Y - contentSize.Y };
                    end = baseOffset with { Y = baseOffset.Y + containerSize.Y };
                    break;
            }

            var durationSec = pathLength / Speed;
            var animation = compositor.CreateVector3DKeyFrameAnimation();
            animation.InsertKeyFrame(0f, start, new LinearEasing());
            animation.InsertKeyFrame(1f, end, new LinearEasing());
            animation.Duration = TimeSpan.FromSeconds(durationSec);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            animation.DelayTime = -TimeSpan.FromSeconds(progress * durationSec);

            compositionVisual.StartAnimation("Offset", animation);
            _animation = animation;
            _animationStartTime = DateTimeOffset.UtcNow;
        }
        else
        {
            var now = DateTimeOffset.UtcNow;
            if (now > _animationStartTime && _lastSpeed > 0)
                _distanceElapsed += (now - _animationStartTime).TotalSeconds * _lastSpeed;

            compositionVisual.StopAnimation("Offset");
            _animationStopTime = now;
        }
    }

    private void OnSpeedChanged(AvaloniaPropertyChangedEventArgs<double> args)
    {
        RebuildAnimationPreservingProgress(args.OldValue.GetValueOrDefault());
    }

    private void OnPresenterSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        RebuildAnimationPreservingProgress();
        if (!IsRunning)
            InvalidatePresenterPosition();
    }

    private void RebuildAnimationPreservingProgress(double? previousSpeedOverride = null)
    {
        if (Presenter is null || Presenter.Child is not { } control)
            return;

        var compositionVisual = ElementComposition.GetElementVisual(control);
        var presenterVisual = ElementComposition.GetElementVisual(Presenter);
        var compositor = compositionVisual!.Compositor;

        var containerSize = presenterVisual!.Size;
        var contentSize = compositionVisual.Size;
        var now = DateTimeOffset.UtcNow;

        if (IsRunning && _animationStartTime != default && now > _animationStartTime)
        {
            var elapsed = now - _animationStartTime;
            var usedSpeed = previousSpeedOverride ?? _lastSpeed;
            if (elapsed > TimeSpan.Zero && usedSpeed > 0)
                _distanceElapsed += elapsed.TotalSeconds * usedSpeed;
        }

        compositionVisual.StopAnimation("Offset");
        _animationStopTime = now;

        double pathLength = Direction switch
        {
            Direction.Left or Direction.Right => containerSize.X + contentSize.X,
            Direction.Up or Direction.Down => containerSize.Y + contentSize.Y,
            _ => 0
        };

        if (!IsRunning || Speed <= 0 || pathLength <= 0)
            return;

        var progress = (_distanceElapsed % pathLength) / pathLength;

        if (!_hasInitialOffset)
        {
            _initialOffset = compositionVisual.Offset;
            _hasInitialOffset = true;
        }

        var baseOffset = _initialOffset ?? compositionVisual.Offset;
        var start = baseOffset;
        var end = baseOffset;

        switch (Direction)
        {
            case Direction.Left:
                start = baseOffset with { X = baseOffset.X + containerSize.X };
                end = baseOffset with { X = baseOffset.X - contentSize.X };
                break;
            case Direction.Right:
                start = baseOffset with { X = baseOffset.X - contentSize.X };
                end = baseOffset with { X = baseOffset.X + containerSize.X };
                break;
            case Direction.Up:
                start = baseOffset with { Y = baseOffset.Y + containerSize.Y };
                end = baseOffset with { Y = baseOffset.Y - contentSize.Y };
                break;
            case Direction.Down:
                start = baseOffset with { Y = baseOffset.Y - contentSize.Y };
                end = baseOffset with { Y = baseOffset.Y + containerSize.Y };
                break;
        }

        _lastSpeed = Speed;
        var durationSec = pathLength / Speed;

        var animation = compositor.CreateVector3DKeyFrameAnimation();
        animation.InsertKeyFrame(0f, start, new LinearEasing());
        animation.InsertKeyFrame(1f, end, new LinearEasing());
        animation.Duration = TimeSpan.FromSeconds(durationSec);
        animation.IterationBehavior = AnimationIterationBehavior.Forever;
        animation.DelayTime = -TimeSpan.FromSeconds(progress * durationSec);

        compositionVisual.StartAnimation("Offset", animation);
        _animation = animation;
        _animationStartTime = DateTimeOffset.UtcNow;
    }

    private void UpdateAnimation()
    {
        // 已弃用：基于时间与新速度回推位置
    }

    public Marquee() { }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Presenter is not null)
            Presenter.SizeChanged += OnPresenterSizeChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Presenter is not null)
            Presenter.SizeChanged -= OnPresenterSizeChanged;
    }

    public bool IsRunning
    {
        get => GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    public Direction Direction
    {
        get => GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    private void InvalidatePresenterPosition()
    {
        if (Presenter is null)
            return;
        var layoutValues = GetLayoutValues();
        var location = UpdateLocation(layoutValues);
        if (location is null)
            return;
        Canvas.SetTop(Presenter, location.Value.top);
        Canvas.SetLeft(Presenter, location.Value.left);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var result = base.MeasureOverride(availableSize);
        var presenter = Presenter;
        if (presenter is null)
            return result;
        var size = presenter.DesiredSize;
        if (double.IsInfinity(result.Width) || result.Width == 0)
            result = result.WithWidth(size.Width);
        if (double.IsInfinity(result.Height) || result.Height == 0)
            result = result.WithHeight(size.Height);
        return result;
    }

    private (double top, double left)? UpdateLocation(LayoutValues values)
    {
        var horizontalOffset = values.Direction switch
        {
            Direction.Up or Direction.Down => GetHorizontalOffset(values.Bounds, values.PresenterSize, values.HorizontalAlignment),
            Direction.Left or Direction.Right => values.Left,
            _ => throw new NotImplementedException(),
        };
        var verticalOffset = values.Direction switch
        {
            Direction.Up or Direction.Down => values.Top,
            Direction.Left or Direction.Right => GetVerticalOffset(values.Bounds, values.PresenterSize, values.VerticalAlignment),
            _ => throw new NotImplementedException(),
        };
        if (double.IsNaN(horizontalOffset))
            horizontalOffset = 0.0;
        if (double.IsNaN(verticalOffset))
            verticalOffset = 0.0;
        var speed = values.Diff;
        var diff = values.Direction switch
        {
            Direction.Up => -speed,
            Direction.Down => speed,
            Direction.Left => -speed,
            Direction.Right => speed,
            _ => 0
        };
        switch (values.Direction)
        {
            case Direction.Up:
            case Direction.Down:
                verticalOffset += diff;
                break;
            case Direction.Left:
            case Direction.Right:
                horizontalOffset += diff;
                break;
        }
        switch (values.Direction)
        {
            case Direction.Down:
                if (verticalOffset > values.Bounds.Height)
                    verticalOffset = -values.PresenterSize.Height;
                break;
            case Direction.Up:
                if (verticalOffset < -values.PresenterSize.Height)
                    verticalOffset = values.Bounds.Height;
                break;
            case Direction.Right:
                if (horizontalOffset > values.Bounds.Width)
                    horizontalOffset = -values.PresenterSize.Width;
                break;
            case Direction.Left:
                if (horizontalOffset < -values.PresenterSize.Width)
                    horizontalOffset = values.Bounds.Width;
                break;
        }
        verticalOffset = Clamp(verticalOffset, -values.PresenterSize.Height, values.Bounds.Height);
        horizontalOffset = Clamp(horizontalOffset, -values.PresenterSize.Width, values.Bounds.Width);
        return (verticalOffset, horizontalOffset);
    }

    public static double Clamp(double value, double min, double max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double GetHorizontalOffset(Size bounds, Size presenterBounds, HorizontalAlignment horizontalAlignment)
        => horizontalAlignment switch
        {
            HorizontalAlignment.Left => 0,
            HorizontalAlignment.Center => (bounds.Width - presenterBounds.Width) / 2,
            HorizontalAlignment.Right => bounds.Width - presenterBounds.Width,
            _ => 0
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double GetVerticalOffset(Size bounds, Size presenterBounds, VerticalAlignment verticalAlignment)
        => verticalAlignment switch
        {
            VerticalAlignment.Top => 0,
            VerticalAlignment.Center => (bounds.Height - presenterBounds.Height) / 2,
            VerticalAlignment.Bottom => bounds.Height - presenterBounds.Height,
            _ => 0
        };

    private LayoutValues GetLayoutValues() => new LayoutValues
    {
        Bounds = Bounds.Size,
        PresenterSize = Presenter?.Bounds.Size ?? new Size(),
        Left = Presenter is null ? 0 : Canvas.GetLeft(Presenter),
        Top = Presenter is null ? 0 : Canvas.GetTop(Presenter),
        Diff = IsRunning ? Speed / 60.0 : 0,
        HorizontalAlignment = HorizontalContentAlignment,
        VerticalAlignment = VerticalContentAlignment,
        Direction = Direction
    };
}

struct LayoutValues
{
    public Size Bounds { get; set; }
    public Size PresenterSize { get; set; }
    public double Left { get; set; }
    public double Top { get; set; }
    public double Diff { get; set; }
    public Direction Direction { get; set; }
    public HorizontalAlignment HorizontalAlignment { get; set; }
    public VerticalAlignment VerticalAlignment { get; set; }
}

public enum Direction
{
    Left,
    Right,
    Up,
    Down,
}
