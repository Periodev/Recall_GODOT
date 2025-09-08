using System;
using System.Linq;
using CombatCore;

namespace CombatCore.Recall
{
	public partial class EchoStore
	{
		public const int Capacity = 5;
		private readonly List<Echo> _echos = new(Capacity);

		public int Count => _echos.Count;
		public bool IsFull => _echos.Count >= Capacity;

		public IReadOnlyList<Echo> Items => _echos; // 不特別保護；需要時再改 AsReadOnly()

		public FailCode TryAdd(Echo echo)
		{
			if (IsFull) return FailCode.EchoSlotsFull; // 滿了不 pop，直接 fail
			
			// Generate unique random ID to avoid duplicates
			var uniqueId = GenerateUniqueId();
			var uniqueEcho = new Echo
			{
				Id = uniqueId,
				RecipeId = echo.RecipeId,
				Name = echo.Name,
				RecipeLabel = echo.RecipeLabel,
				Summary = echo.Summary,
				CostAP = echo.CostAP,
				Op = echo.Op,
				TargetType = echo.TargetType
			};
			
			_echos.Add(uniqueEcho);
			return FailCode.None;
		}
		
		private int GenerateUniqueId()
		{
			int id;
			var random = new Random();
			do
			{
				id = random.Next(int.MinValue, int.MaxValue);
			} while (_echos.Any(e => e.Id == id));
			
			return id;
		}

		public FailCode TryRemoveAt(int index)
		{
			if ((uint)index >= (uint)_echos.Count) return FailCode.BadIndex;
			_echos.RemoveAt(index); // 自動左移：前密後空
			return FailCode.None;
		}

		public FailCode TryRemove(Echo echo)
		{
			for (int i = 0; i < _echos.Count; i++)
			{
				if (_echos[i].Id == echo.Id)
				{
					_echos.RemoveAt(i);
					return FailCode.None;
				}
			}
			return FailCode.BadIndex; // Echo not found
		}

		public void Clear() => _echos.Clear();

		/// 產生固定 5 格的槽位陣列：前段為 Echo 實例，後段為 null 代表空槽。
		/// UI 直接渲染 5 格即可，無洞。
		public Echo?[] ToSlots()
		{
			var slots = new Echo?[Capacity];
			for (int i = 0; i < _echos.Count; i++)
				slots[i] = _echos[i];
			// 其餘維持 null 作為 Empty
			return slots;
		}

		// 可選安全讀取
		public bool TryGet(int index, out Echo? echo)
		{
			if ((uint)index >= (uint)_echos.Count) { echo = null; return false; }
			echo = _echos[index]; return true;
		}
	}
}
