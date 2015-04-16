/*
 * Created by SharpDevelop.
 * User: Thomas
 * Date: 2015-03-03
 * Time: 23:24
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

using Verse;


namespace Signals
{
	
	public static class SignalGrid
	{
		
		static List<CompSignal>[] signalGrid;
		
		static SignalGrid()
		{
			signalGrid = new List<CompSignal>[CellIndices.NumGridCells];
			
			foreach (var i in CellIndices.AllCellIndicesOnMap) {
				signalGrid[i] = new List<CompSignal>();
			}
		}
		
		private static void RegisterInCell(CompSignal cs, IntVec3 c)
		{
			if (!c.InBounds())
			{
				Log.Warning(string.Concat(new object[]
				{
					cs,
					" tried to register out of bounds at ",
					c,
					". Destroying."
				}));
				cs.parent.Destroy(DestroyMode.Vanish);
				return;
			}
			signalGrid[CellIndices.CellToIndex(c)].Add(cs);
		}
		private static void DeregisterInCell(CompSignal cs, IntVec3 c)
		{
			if (!c.InBounds())
			{
				Log.Error(cs + " tried to de-register out of bounds at " + c);
				return;
			}
			int num = CellIndices.CellToIndex(c);
			if (signalGrid[num].Contains(cs))
			{
				signalGrid[num].Remove(cs);
			}
		}
		
		public static void Register(CompSignal cs)
		{
			RegisterInCell(cs, cs.Position);
		}
		public static void Deregister(CompSignal cs)
		{
			DeregisterInCell(cs, cs.Position);
		}
		
		
		public static List<CompSignal> SignalListAt(IntVec3 c)
		{
			if (!c.InBounds())
			{
				Log.ErrorOnce("Got ThingsListAt out of bounds: " + c, 495287);
				return new List<CompSignal>();
			}
			return signalGrid[CellIndices.CellToIndex(c)];	
		}
		
		public static IEnumerable<CompSignal> SignalsAt(IntVec3 c)
		{
			foreach (var signal in SignalListAt(c)) {
				yield return signal;
			}
		}
		
		public static CompSignal SignalNodeAt(IntVec3 c, Rot4 side)
		{
			return SignalListAt(c).Find(s=>s.CanConnectTo(new Rot4((side.AsInt+4-s.parent.Rotation.AsInt)%4)));
		}
		
	}
}
