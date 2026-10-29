using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ManosabaA11y
{
    /// <summary>
    /// 无人值守实机测试专用的**输入注入**（只在 MANOSABA_A11Y_INJECT=1 + TestMode 下被调用）。
    ///
    /// 为什么需要它：本作是 Unity 6 + 新输入系统（Naninovel 的 InputManager 在初始化时
    /// 会找 EventSystem 上的 InputSystemUIInputModule 来切换输入模式），输入是**每帧轮询**
    /// 当前设备状态，而不是窗口消息驱动。实机 FPS 只有 ~6（delta≈0.16s），
    /// 用 Win32 mouse_event 注入的「按下→抬起」只隔 60~80ms，常常整段落在一帧之内，
    /// 轮询时设备状态已经回到「没按」，于是点击被吞（run17/18/19/21 全卡在第一句）。
    ///
    /// 这里改为把状态事件直接塞进 InputSystem 的事件队列：
    ///   InputSystem.QueueStateEvent(Mouse.current, new MouseState { buttons = 1, position = ... })
    /// 下一帧轮询必然看到「按住」，从而让引擎走它自己的正常继续流程——
    /// 不猜测游戏内部 API、不改游戏逻辑，只是替代「人手按一下」。
    ///
    /// ★ 发布版不会走到这里：Plugin 只有 MANOSABA_A11Y_TEST=1 时才创建 TestDriver。
    /// </summary>
    internal static class InputInject
    {
        private static bool _pressed;
        private static float _releaseAt;
        private static Vector2 _lastPos;
        private static int _clicks;

        private static bool _keyDown;
        private static Key _key;
        private static float _keyReleaseAt;
        private static int _keys;

        internal static int Clicks { get { return _clicks; } }
        internal static int Keys { get { return _keys; } }

        /// <summary>
        /// 注入一次键盘按下（用 KeyboardState，而不是 Win32 按键消息）。
        /// 本作 Naninovel 的「继续」绑定同时接受鼠标点击与键盘，
        /// 但鼠标点击在 6 FPS 下常被 EventSystem/采样器漏掉，键盘是备用通道。
        /// </summary>
        internal static void PressKey(Key key, float holdSeconds = 0.25f)
        {
            try
            {
                Keyboard kb = Keyboard.current;
                if (kb == null) { A11yHost.Diag("INJECT: Keyboard.current 为 null，无法注入按键"); return; }
                KeyboardState st = new KeyboardState();
                st.Press(key);
                InputSystem.QueueStateEvent<KeyboardState>(kb, st);
                _keyDown = true;
                _key = key;
                _keyReleaseAt = Time.unscaledTime + holdSeconds;
                _keys++;
                A11yHost.Diag("INJECT key #" + _keys + " " + key + " down frame=" + Time.frameCount);
            }
            catch (Exception e) { A11yHost.Diag("INJECT PressKey: " + e.Message); }
        }

        /// <summary>注入一次鼠标左键点击。position 用 Unity 屏幕坐标（左下角原点）。</summary>
        internal static void Click(float x, float y, float holdSeconds = 0.28f)
        {
            try
            {
                Mouse mouse = Mouse.current;
                if (mouse == null)
                {
                    A11yHost.Diag("INJECT: Mouse.current 为 null，无法注入");
                    return;
                }

                MouseState state = new MouseState();
                state.position = new Vector2(x, y);
                state.delta = Vector2.zero;
                state.scroll = Vector2.zero;
                state.buttons = 1; // bit0 = 左键
                state.clickCount = 1;

                InputSystem.QueueStateEvent<MouseState>(mouse, state);
                _pressed = true;
                _releaseAt = Time.unscaledTime + holdSeconds;
                _lastPos = state.position;
                _clicks++;
                A11yHost.Diag("INJECT click #" + _clicks + " pos=" + x + "," + y + " frame=" + Time.frameCount);
            }
            catch (Exception e)
            {
                A11yHost.Diag("INJECT click: " + e.Message);
            }
        }

        /// <summary>由 TestDriver.Update 每帧调用：按住到时间后补一个「抬起」。</summary>
        internal static void Tick()
        {
            if (_keyDown && Time.unscaledTime >= _keyReleaseAt)
            {
                try
                {
                    Keyboard kb = Keyboard.current;
                    if (kb != null)
                    {
                        KeyboardState st = new KeyboardState();
                        st.Release(_key);
                        InputSystem.QueueStateEvent<KeyboardState>(kb, st);
                        A11yHost.Diag("INJECT key release #" + _keys + " " + _key + " frame=" + Time.frameCount);
                    }
                }
                catch (Exception e) { A11yHost.Diag("INJECT key release: " + e.Message); }
                _keyDown = false;
            }

            if (!_pressed) return;
            if (Time.unscaledTime < _releaseAt) return;
            try
            {
                Mouse mouse = Mouse.current;
                if (mouse == null) { _pressed = false; return; }
                MouseState state = new MouseState();
                state.position = _lastPos;
                state.buttons = 0;
                InputSystem.QueueStateEvent<MouseState>(mouse, state);
                A11yHost.Diag("INJECT release #" + _clicks + " frame=" + Time.frameCount);
            }
            catch (Exception e)
            {
                A11yHost.Diag("INJECT release: " + e.Message);
            }
            _pressed = false;
        }
    }
}
