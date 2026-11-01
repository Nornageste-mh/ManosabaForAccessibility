using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ManosabaA11y
{
    /// <summary>
    /// 用游戏自己的新 Input System（InputSystem 包）读键盘。
    ///
    /// 为什么需要这一路：
    ///   本作 Player Settings 只启用了新 Input System，旧的 <c>UnityEngine.Input</c> 一读就抛
    ///   <c>InvalidOperationException</c>（UIPROBE 实测 legacyInput=FAIL）；平台层剩下的兜底是
    ///   Win32 <c>GetAsyncKeyState</c>（GlobalKeys）。0.0.0.5 实测发现：自动化用
    ///   <c>keybd_event</c> 注入的按键**进不了** GetAsyncKeyState（同进程自测
    ///   <c>sawKey=False</c>），于是「退格重读 / 沉默 / 导航组合键」在自动化环境里全部读不到。
    ///   而新 Input System 读的是游戏自己收到的设备状态，真实玩家按的键一定在里面。
    ///
    /// 设计约束：
    ///   - 只做「读」，不注入、不改游戏输入配置；
    ///   - 只覆盖配置里可能写到的键（字母/数字/功能键/常用编辑键），认不出就返回 false，
    ///     不猜、不抢游戏按键；
    ///   - 与平台层的 <see cref="KeyEdge"/>/<see cref="Hotkey"/> 是**并集**关系：
    ///     任一路说「按下了」就算按下。
    /// </summary>
    internal static class InputSysKeys
    {
        private static readonly Dictionary<KeyCode, Key> Map = new Dictionary<KeyCode, Key>
        {
            { KeyCode.Backspace, Key.Backspace },
            { KeyCode.Tab, Key.Tab },
            { KeyCode.Return, Key.Enter },
            { KeyCode.Equals, Key.Equals },
            { KeyCode.KeypadEnter, Key.NumpadEnter },
            { KeyCode.Escape, Key.Escape },
            { KeyCode.Space, Key.Space },
            { KeyCode.UpArrow, Key.UpArrow },
            { KeyCode.DownArrow, Key.DownArrow },
            { KeyCode.LeftArrow, Key.LeftArrow },
            { KeyCode.RightArrow, Key.RightArrow },
            { KeyCode.PageUp, Key.PageUp },
            { KeyCode.PageDown, Key.PageDown },
            { KeyCode.Home, Key.Home },
            { KeyCode.End, Key.End },
            { KeyCode.Insert, Key.Insert },
            { KeyCode.Delete, Key.Delete },

            { KeyCode.Alpha0, Key.Digit0 }, { KeyCode.Alpha1, Key.Digit1 },
            { KeyCode.Alpha2, Key.Digit2 }, { KeyCode.Alpha3, Key.Digit3 },
            { KeyCode.Alpha4, Key.Digit4 }, { KeyCode.Alpha5, Key.Digit5 },
            { KeyCode.Alpha6, Key.Digit6 }, { KeyCode.Alpha7, Key.Digit7 },
            { KeyCode.Alpha8, Key.Digit8 }, { KeyCode.Alpha9, Key.Digit9 },
            { KeyCode.Keypad0, Key.Numpad0 }, { KeyCode.Keypad1, Key.Numpad1 },
            { KeyCode.Keypad2, Key.Numpad2 }, { KeyCode.Keypad3, Key.Numpad3 },
            { KeyCode.Keypad4, Key.Numpad4 }, { KeyCode.Keypad5, Key.Numpad5 },
            { KeyCode.Keypad6, Key.Numpad6 }, { KeyCode.Keypad7, Key.Numpad7 },
            { KeyCode.Keypad8, Key.Numpad8 }, { KeyCode.Keypad9, Key.Numpad9 },

            { KeyCode.F1, Key.F1 }, { KeyCode.F2, Key.F2 }, { KeyCode.F3, Key.F3 },
            { KeyCode.F4, Key.F4 }, { KeyCode.F5, Key.F5 }, { KeyCode.F6, Key.F6 },
            { KeyCode.F7, Key.F7 }, { KeyCode.F8, Key.F8 }, { KeyCode.F9, Key.F9 },
            { KeyCode.F10, Key.F10 }, { KeyCode.F11, Key.F11 }, { KeyCode.F12, Key.F12 },

            { KeyCode.A, Key.A }, { KeyCode.B, Key.B }, { KeyCode.C, Key.C },
            { KeyCode.D, Key.D }, { KeyCode.E, Key.E }, { KeyCode.F, Key.F },
            { KeyCode.G, Key.G }, { KeyCode.H, Key.H }, { KeyCode.I, Key.I },
            { KeyCode.J, Key.J }, { KeyCode.K, Key.K }, { KeyCode.L, Key.L },
            { KeyCode.M, Key.M }, { KeyCode.N, Key.N }, { KeyCode.O, Key.O },
            { KeyCode.P, Key.P }, { KeyCode.Q, Key.Q }, { KeyCode.R, Key.R },
            { KeyCode.S, Key.S }, { KeyCode.T, Key.T }, { KeyCode.U, Key.U },
            { KeyCode.V, Key.V }, { KeyCode.W, Key.W }, { KeyCode.X, Key.X },
            { KeyCode.Y, Key.Y }, { KeyCode.Z, Key.Z },
        };

        /// <summary>新 Input System 是否可用（设备拿得到）。</summary>
        internal static bool Available
        {
            get
            {
                try { return Keyboard.current != null; }
                catch { return false; }
            }
        }

        /// <summary>这一帧是否刚按下（边沿）。认不出的键返回 false。</summary>
        internal static bool Pressed(KeyCode kc)
        {
            try
            {
                Keyboard kb = Keyboard.current;
                if (kb == null) return false;
                Key k;
                if (!Map.TryGetValue(kc, out k)) return false;
                return kb[k].wasPressedThisFrame;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>现在是否按住。</summary>
        internal static bool Held(KeyCode kc)
        {
            try
            {
                Keyboard kb = Keyboard.current;
                if (kb == null) return false;
                Key k;
                if (!Map.TryGetValue(kc, out k)) return false;
                return kb[k].isPressed;
            }
            catch
            {
                return false;
            }
        }

        internal static bool AltHeld
        {
            get
            {
                try
                {
                    Keyboard kb = Keyboard.current;
                    return kb != null && (kb.leftAltKey.isPressed || kb.rightAltKey.isPressed);
                }
                catch { return false; }
            }
        }

        internal static bool CtrlHeld
        {
            get
            {
                try
                {
                    Keyboard kb = Keyboard.current;
                    return kb != null && (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed);
                }
                catch { return false; }
            }
        }

        internal static bool ShiftHeld
        {
            get
            {
                try
                {
                    Keyboard kb = Keyboard.current;
                    return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
                }
                catch { return false; }
            }
        }

        /// <summary>
        /// 与 <c>Hotkey.Down(Hotkey.Spec)</c> 同语义的一路读取：写了哪个修饰键就要求哪个，
        /// 没写的一律要求「没按住」（避免抢游戏自己的键）。
        /// </summary>
        internal static bool Down(KeyCode key, bool alt, bool ctrl, bool shift)
        {
            if (!Pressed(key)) return false;
            if (alt != AltHeld) return false;
            if (ctrl != CtrlHeld) return false;
            if (shift != ShiftHeld) return false;
            return true;
        }
    }
}
