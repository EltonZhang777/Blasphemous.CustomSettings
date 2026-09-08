using Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;
using Gameplay.UI.Others.Buttons;
using Rewired;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Blasphemous.CustomSettings.Components;

/// <summary>
/// Owns vertical GAME-menu navigation while the injected option is present.
/// Rewired keeps horizontal/submit/cancel handling; this component routes the complete
/// vertical ring after the Rewired vertical axis has been suspended.
/// </summary>
[DefaultExecutionOrder(-1000)]
internal sealed class ModNavigationController : MonoBehaviour
{
    private const float NavigationThreshold = 0.3f;

    private readonly List<EventsButton> _buttons = [];

    private Player _player;
    private string _verticalAxis;
    private float _repeatDelay;
    private float _inputActionsPerSecond;
    private int _lastDirection;
    private float _nextRepeatTime;

    internal void Configure(
        IList<EventsButton> buttons,
        string verticalAxis,
        float repeatDelay,
        float inputActionsPerSecond)
    {
        _buttons.Clear();
        if (buttons != null)
        {
            foreach (EventsButton button in buttons)
            {
                if (button != null)
                    _buttons.Add(button);
            }
        }

        _verticalAxis = verticalAxis;
        _repeatDelay = Mathf.Max(0f, repeatDelay);
        _inputActionsPerSecond = Mathf.Max(1f, inputActionsPerSecond);
        ResetInputState();
        enabled = _buttons.Count > 1 && !string.IsNullOrEmpty(_verticalAxis);

        ModLogExtensions.DebugIfDebugBuild($"[CustomSettings] DIAG manual navigation configured nodes={_buttons.Count} axis={_verticalAxis} repeatDelay={_repeatDelay} inputRate={_inputActionsPerSecond}");
    }

    internal void DisableNavigation()
    {
        enabled = false;
        _buttons.Clear();
        _verticalAxis = null;
        ResetInputState();
    }

    private void OnDestroy()
    {
        SettingsMenuInjector.ExitGameMenu();
    }

    private void Update()
    {
        SettingsMenuInjector.SuspendCompetingCustomInputs();

        if (!ReInput.isReady)
            return;

        _player ??= ReInput.players.GetPlayer(0);
        if (_player == null || EventSystem.current == null || string.IsNullOrEmpty(_verticalAxis))
            return;

        float axis = _player.GetAxisRaw(_verticalAxis);
        int direction = axis < -NavigationThreshold
            ? 1
            : axis > NavigationThreshold
                ? -1
                : 0;

        bool shouldMove = false;
        if (direction == 0)
        {
            _nextRepeatTime = 0f;
        }
        else if (direction != _lastDirection)
        {
            shouldMove = true;
            // Rewired uses the repeat rate when repeatDelay is zero. Do the same here;
            // scheduling the next action at the current time causes one held press to
            // move again on the immediately following frame.
            _nextRepeatTime = Time.unscaledTime + GetFirstRepeatDelay();
        }
        else if (Time.unscaledTime >= _nextRepeatTime)
        {
            shouldMove = true;
            _nextRepeatTime = Time.unscaledTime + 1f / _inputActionsPerSecond;
        }

        if (shouldMove)
            Move(direction);

        _lastDirection = direction;
    }

    internal bool IsNavigationTarget(GameObject target)
    {
        if (target == null)
            return false;

        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i] != null && _buttons[i].gameObject == target)
                return true;
        }

        return false;
    }

    private float GetFirstRepeatDelay()
    {
        return _repeatDelay > 0f
            ? _repeatDelay
            : 1f / _inputActionsPerSecond;
    }

    private void Move(int direction)
    {
        EventSystem eventSystem = EventSystem.current;
        GameObject current = eventSystem.currentSelectedGameObject;
        int currentIndex = -1;
        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i].gameObject == current)
            {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex < 0)
            return;

        int targetIndex = (currentIndex + direction) % _buttons.Count;
        if (targetIndex < 0)
            targetIndex += _buttons.Count;

        EventsButton target = _buttons[targetIndex];
        if (target == null || !target.IsActive() || !target.IsInteractable())
            return;

        eventSystem.SetSelectedGameObject(target.gameObject);
        ModLogExtensions.DebugIfDebugBuild($"[CustomSettings] DIAG routed navigation direction={(direction > 0 ? "Down" : "Up")} from={current.name} to={target.name} selected={(eventSystem.currentSelectedGameObject != null ? eventSystem.currentSelectedGameObject.name : "null")} frame={Time.frameCount}");
    }

    private void ResetInputState()
    {
        _player = null;
        _lastDirection = 0;
        _nextRepeatTime = 0f;
    }
}