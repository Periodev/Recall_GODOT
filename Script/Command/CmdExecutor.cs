using Godot;
using System;
using System.Collections.Generic;

namespace CombatCore.Command
{
	public interface ICmdExecutor
	{
		CmdLog ExecuteAll(IEnumerable<AtomicCmd> cmds);
		// 之後擴充：ExecuteOrSchedule(...), ExecuteOrDiscard(...)
	}

	public class CmdExecutor : ICmdExecutor
	{
		public CmdLog ExecuteAll(IEnumerable<AtomicCmd> cmds)
		{
			if (cmds is null) throw new ArgumentNullException(nameof(cmds));

			var log = new CmdLog();
			foreach (var c in cmds)
			{
				int delta = c.Execute();           // 僅觸碰 Component，無規則
				log.Record(c, delta);
			}
			return log;
		}
	}

	public sealed class CmdLog
	{
		public readonly struct Entry
		{
			public Entry(AtomicCmd cmd, int delta) { Cmd = cmd; Delta = delta; }
			public AtomicCmd Cmd { get; }
			public int Delta { get; }              // 主要資源變動量（例如 HP 傷害）
		}

		private readonly List<Entry> _items = new();
		public void Record(AtomicCmd cmd, int delta) => _items.Add(new Entry(cmd, delta));
		public IReadOnlyList<Entry> Items => _items;
		public int Count => _items.Count;
	}
}
