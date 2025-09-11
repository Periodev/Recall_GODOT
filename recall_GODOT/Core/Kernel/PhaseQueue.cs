using System;
using System.Collections.Generic;
using System.Diagnostics;
using CombatCore;

namespace CombatCore
{
	/// <summary>
	/// 回合內的四個執行時機
	/// </summary>
	public enum ExecuteQueue : byte
	{
		EnemyMark = 1,    	// 敵人下回執行
		Player = 2,          // 玩家執行  
		EnemyAct = 3,    	// 敵人執行
		TurnEnd = 4          // 回合結束執行
	}

	/// <summary>
	/// 帶執行上下文的 Intent 包裝
	/// 將純意圖 (Intent) 與執行上下文 (Actor, Reason) 分離
	/// </summary>
	public readonly struct QueuedIntent
	{
		/// <summary>執行者</summary>
		public Actor Actor { get; }
		
		/// <summary>意圖內容</summary>
		public Intent Intent { get; }
		
		/// <summary>執行原因（調試用）</summary>
		public string Reason { get; }

		public QueuedIntent(Actor actor, Intent intent, string reason = "")
		{
			Actor = actor;
			Intent = intent;
			Reason = reason;
		}

		public override string ToString() => $"{Actor?.DebugName}: {Intent} ({Reason})";
	}

	/// <summary>
	/// 簡單的 FIFO 隊列 - 不需要複雜優先級
	/// 每個執行時機使用一個獨立隊列
	/// </summary>
	public class PhaseQueue
	{
		private readonly Queue<QueuedIntent> _queue = new();

		/// <summary>
		/// 將 Intent 加入隊列
		/// </summary>
		public void Enqueue(Actor actor, Intent intent, string reason = "")
		{
			var queuedIntent = new QueuedIntent(actor, intent, reason);
			_queue.Enqueue(queuedIntent);

			Debug.Print($"[Enqueued]: {queuedIntent}");
		}

		/// <summary>
		/// 從隊列取出下一個 Intent
		/// </summary>
		public bool TryDequeue(out QueuedIntent intent)
		{
			intent = default;
			if (_queue.Count == 0) return false;

			intent = _queue.Dequeue();

			Debug.Print($"[Dequeued]: {intent}");
			return true;
		}

		/// <summary>
		/// 清空隊列
		/// </summary>
		public void Clear()
		{
			_queue.Clear();
		}

		/// <summary>
		/// 隊列中的項目數量
		/// </summary>
		public int Count => _queue.Count;

		/// <summary>
		/// 是否有待執行的 Intent
		/// </summary>
		public bool HasIntents => _queue.Count > 0;

		/// <summary>
		/// 除錯：顯示隊列內容
		/// </summary>
		public void DebugPrint(string queueName)
		{
#if DEBUG
			if (_queue.Count == 0)
			{
				Debug.Print($"[{queueName}] Empty");
				return;
			}

			Debug.Print($"[{queueName}] {_queue.Count} intents:");
			var temp = new List<QueuedIntent>();
			
			while (_queue.Count > 0)
			{
				var intent = _queue.Dequeue();
				temp.Add(intent);
				Debug.Print($"  - {intent}");
			}
			
			foreach (var intent in temp)
			{
				_queue.Enqueue(intent);
			}
#endif
		}
	}
}
