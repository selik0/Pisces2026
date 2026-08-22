using System;
using System.Collections.Generic;
using System.Text;

namespace GameEngine
{
    /// <summary>
    /// 红点树，以「int Id 链」定位节点。
    /// <para>
    /// Id 链表示从根节点到目标节点的层级，例如 <c>GetNode(MainId, MailId, UnreadId)</c>。<br/>
    /// 节点在首次访问时自动创建，无需预先声明。同一父节点下的子节点 Id 必须唯一。
    /// </para>
    ///
    /// <para><b>典型用法</b></para>
    /// <code>
    /// var tree = new RedDotTree();
    ///
    /// // 设置叶节点计数（自动向上冒泡）
    /// tree.SetCount(3, MainId, MailId, UnreadId);
    /// tree.SetCount(1, MainId, MailId, DraftId);
    ///
    /// // 读取汇总计数
    /// int total = tree.GetCount(MainId, MailId);   // = 4
    /// bool show  = tree.HasRedDot(MainId);         // = true
    ///
    /// // 重置整棵树
    /// tree.Reset();
    /// </code>
    /// </summary>
    public sealed class RedDotTree
    {
        /// <summary>虚拟根节点的 Id（0 保留，不允许用作实际节点 Id）</summary>
        private const int RootId = 0;

        private RedDotNode _root = new RedDotNode(RootId, null);

        // ── 节点访问 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 根据 Id 链获取节点，若节点不存在则自动创建整条链上的节点。
        /// 与 <see cref="TryGetNode"/> 一致，Id 必须大于 <see cref="RootId"/>（0 保留）。
        /// </summary>
        /// <param name="idChain">从根到目标的节点 Id 链，不可为空，且每个 Id 必须大于 0</param>
        /// <returns>对应的 <see cref="RedDotNode"/>；Id 链非法时记录错误并返回 null</returns>
        public RedDotNode GetNode(params int[] idChain)
        {
            if (idChain == null || idChain.Length == 0)
            {
                Log.Error("[RedDotTree] GetNode failed: idChain is null or empty");
                return null;
            }

            // 先整体校验再创建，非法 Id 不会产生部分节点残留
            for (int i = 0; i < idChain.Length; i++)
            {
                if (idChain[i] <= RootId)
                {
                    Log.Error($"[RedDotTree] GetNode failed: 非法节点 Id={idChain[i]}，Id 必须大于 {RootId}");
                    return null;
                }
            }

            RedDotNode current = _root;
            foreach (int id in idChain)
            {
                current = current.GetOrCreateChild(id);
            }

            return current;
        }

        /// <summary>
        /// 尝试获取已存在的节点，不存在时返回 null（不会自动创建）。
        /// </summary>
        public RedDotNode TryGetNode(params int[] idChain)
        {
            if (idChain == null || idChain.Length == 0)
            {
                Log.Error("[RedDotTree] TryGetNode failed: idChain is null or empty");
                return null;
            }

            var current = _root;
            foreach (var id in idChain)
            {
                if (id <= RootId)
                {
                    return null;
                }

                if (!current.Children.TryGetValue(id, out current))
                {
                    return null;
                }
            }

            return current;
        }

        /// <summary>
        /// 指定 Id 链节点的汇总计数是否 > 0。
        /// 若节点不存在则返回 false。
        /// </summary>
        public bool HasRedDot(params int[] idChain)
        {
            var node = TryGetNode(idChain);
            return node != null && node.HasRedDot;
        }

        // ── 重置 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 完全重置：清空所有节点、所有计数和所有监听器。
        /// </summary>
        public void Reset()
        {
            // 重建根节点即可丢弃整棵子树，旧节点失去引用后 GC 会回收
            _root = new RedDotNode(RootId, null);
            Log.Debug("[RedDotTree] Reset");
        }
    }
}
