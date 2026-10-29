// ============================================================================
// 《魔法少女ノ魔女裁判》逐作区（UiNav 的 partial class 补充）
//
// 这个文件是 A11yFramework 模板 UiNav.Game.cs.template 的**原样片段**：
// 模板本身只是 class 体（NameAlias / InputFieldRoleName），没有 using / namespace /
// class 声明，必须由逐作骨架自己包起来才能编译。
//
// ★ 别名表与输入框角色的每一条都必须有依据（来自反编译实查或 probe 场景树），
//   不许猜对象名。依据写在下面各段注释里。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace ManosabaA11y
{
    internal static partial class UiNav
    {
        /// <summary>
        /// 设置面板里那些「美术字标签」的对象名 → 中文。
        ///
        /// 上一作里一批控件的文字是**画在图片上**的，TMP 里一个字都没有，
        /// 所以只能退回对象名 —— 而对象名是 Gallery / Options 这种。
        /// 这里按对象名给出中文。每一条都要有依据，不要凭感觉往里加：
        ///
        ///   · 标题旋转菜单的五个按钮，名字与动作的对应是从
        ///     ClockwiseMenuController.Awake 里实查的：
        ///       SetButtonAction(0, handler.QuitGame)
        ///       SetButtonAction(1, handler.OpenSettings)
        ///       SetButtonAction(2, handler.StartGame)
        ///       SetButtonAction(3, handler.LoadSave)
        ///       SetButtonAction(4, handler.OpenGallery)
        ///     对象名则是 probe 的场景树转储里实查的：
        ///       Canvas/Panel/Background/Menu/Buttons 下依次是
        ///       Gallery / Load / Start / Options / Exit。
        ///
        ///   · 画廊页签名（CG / 视频 / 音乐 / 语音）本身是 TMP 文字，
        ///     不需要别名 —— 别名只在控件子树里**一个字都没有**时才会被用到
        ///     （见 TextOf），所以不会盖掉正常按钮的朗读。
        /// </summary>
        private static readonly Dictionary<string, string> NameAlias =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // ---- 标题旋转菜单（纯图片按钮）----
            { "Start",   "开始游戏" },
            { "Load",    "读取存档" },
            { "Gallery", "画廊" },
            { "Options", "系统设置" },
            { "Exit",    "退出游戏" },

            // ---- 通用确认框（文字画在图片上）----
            { "Yes",     "确定" },
            { "No",      "取消" },
            { "Confirm", "确定" },
            { "Cancel",  "取消" },
        };

        // ================= 姓名输入框 =================
        //
        // level2（开局输入姓名）的两个输入框在场景里**没有任何标签文字**：
        // 行名画在图片上，TMP 里读不到；对象名一个叫 FirstText（InputField）、
        // 另一个叫 Third —— 后者尤其看不出是「名」。
        //
        // 唯一可靠的依据是游戏自己的 NameInputManagerTMP（反编译）：
        //     public TMP_InputField surnameField1;   ← 姓
        //     public TMP_InputField nameField1;      ← 名
        // Unity 按声明顺序序列化，解析 level2 的组件字节确认这两个字段
        // 分别指向场景里 FirstText（InputField）和 Third（恰好就是仅有的
        // 两个激活输入框，Second / Fourth 未激活）。
        //
        // 这里用反射读，读不到就一路回退（占位提示 → 同行标签 → 对象名），
        // 不让插件因为游戏改版而失效。

        private static Type _nameMgrType;
        private static bool _nameMgrProbed;
        private static FieldInfo _surnameField;
        private static FieldInfo _nameField;

        private static string InputFieldRoleName(TMP_InputField inf)
        {
            try
            {
                if (!_nameMgrProbed)
                {
                    _nameMgrProbed = true;
                    _nameMgrType = Type.GetType("NameInputManagerTMP, Assembly-CSharp");
                    if (_nameMgrType == null)
                    {
                        // 游戏程序集名万一不是 Assembly-CSharp，就遍历已加载程序集
                        Assembly[] all = AppDomain.CurrentDomain.GetAssemblies();
                        for (int i = 0; i < all.Length && _nameMgrType == null; i++)
                        {
                            try { _nameMgrType = all[i].GetType("NameInputManagerTMP", false); }
                            catch { }
                        }
                    }
                    if (_nameMgrType != null)
                    {
                        _surnameField = _nameMgrType.GetField("surnameField1");
                        _nameField = _nameMgrType.GetField("nameField1");
                    }
                }
                if (_nameMgrType == null || _surnameField == null || _nameField == null) return "";

                int id = inf.GetInstanceID();
                string cached;
                if (_inputRoles.TryGetValue(id, out cached)) return cached;

                string role = "";
                // IL2CPP 改造（相对模板的 3 处改动，都是 interop 强制要求）：
                //  1) Resources.FindObjectsOfTypeAll 的参数是 Il2CppSystem.Type，
                //     不是 System.Type —— 必须用 Il2CppType.From 转；
                //  2) `as Component` 在 IL2CPP 下**恒返回 null**（陷阱 I1），
                //     要改用 TryCast<T>()，否则整段名字输入框角色判定静默失效；
                //  3) 见下方 As<T> helper。
                UnityEngine.Object[] found =
                    Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.From(_nameMgrType));
                for (int i = 0; i < found.Length && role.Length == 0; i++)
                {
                    Component c = As<Component>(found[i]);
                    if (c == null) continue;
                    if (ReferenceEquals(_surnameField.GetValue(c), inf)) role = "姓氏";
                    else if (ReferenceEquals(_nameField.GetValue(c), inf)) role = "名字";
                }
                _inputRoles[id] = role;
                return role;
            }
            catch { return ""; }
        }

        /// <summary>
        /// IL2CPP 下唯一安全的向下转型：<c>as</c> / <c>is</c> 对 interop 代理类型恒失败。
        /// 返回 null 表示「不是这个类型 / 对象已销毁」，调用方按不可用处理。
        /// </summary>
        private static T As<T>(UnityEngine.Object o)
            where T : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
        {
            if (o == null) return null;
            try { return ((Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase)o).TryCast<T>(); }
            catch { return null; }
        }
    }
}
