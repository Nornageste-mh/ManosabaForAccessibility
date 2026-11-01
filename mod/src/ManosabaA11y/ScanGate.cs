using System.Collections.Generic;
using UnityEngine;

namespace ManosabaA11y
{
    /// <summary>
    /// 节流小工具：把「每帧都要做一次全场景扫描」的事按名字限频。
    ///
    /// 为什么需要：0.0.0.12 的性能取证实测这台机器 **6~12 FPS、帧时间 80~370ms**，
    /// 而插件自己的每帧开销合计约 10~13ms（`ev≈5.7ms` + `screen≈3~10ms` + `uinav≈0.7ms`），
    /// 占掉一帧预算的 5~15%。其中一个主因是 `Resources.FindObjectsOfTypeAll<T>()`
    /// 被放在每帧路径上（它会分配并遍历整个场景的所有对象）。
    /// 把这类扫描限频到「人眼能分辨的变化速度」，收益是实打实的。
    /// </summary>
    internal static class ScanGate
    {
        private static readonly Dictionary<string, float> Next = new Dictionary<string, float>();

        /// <summary>到时间了返回 true，并把下次时间推到 now + interval。</summary>
        internal static bool Ready(string name, float interval)
        {
            float now = Time.unscaledTime;
            float at;
            if (Next.TryGetValue(name, out at) && now < at) return false;
            Next[name] = now + interval;
            return true;
        }

        /// <summary>场景切换/界面关闭时清掉，避免留着上个场景的计时。</summary>
        internal static void Reset(string name)
        {
            if (name == null) { Next.Clear(); return; }
            Next.Remove(name);
        }
    }
}
