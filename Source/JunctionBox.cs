using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Verse;
using RimWorld;
using UnityEngine;

namespace Signals
{
	
	public class Building_JunctionBox: Building
	{
		public List<SignalBridge> connections;
		
		CompSignal[] signals;
		
		public override void SpawnSetup()
        {
            base.SpawnSetup();
            
            Log.Message(string.Format("SpawnSetup on {0}",this));
            
            signals = new CompSignal[]{
            	this.GetSignal("N"),
            	this.GetSignal("E"),
            	this.GetSignal("S"),
            	this.GetSignal("W"),
            };
            
            if(connections == null)
            {
            	connections = new List<SignalBridge>(18);
	            for (int i = 0; i <= 8; i++) {
            		var b = new SignalBridge(this);
	            	foreach (var s in signals) {
	            		if(s.SignalWidth>i)
	            		{
	            			b.Add(s[i]);
	            		}
	            	}
            		connections.Add(b);
	            	
	            	SignalBridgeManager.Register(connections[i]);
	            }
            } else {
            	foreach (var b in connections) {
            		if (b != null) {
            			SignalBridgeManager.Register(b);
            		}
            	}
            }
            
            
        }
		
		public override void ExposeData()
		{
			Log.Message(string.Format("ExposeData on {0}",this));
			
			base.ExposeData();
			
			Scribe_Collections.LookList(ref connections,"connections",LookMode.Deep);
		}
		
	}

	public class ITab_JunctionBox : ITab
	{
		static readonly Vector2 WinSize = new Vector2(370f, 480f);
		
		protected Building_JunctionBox SelBox
		{
			get
			{
				return (Building_JunctionBox)base.SelThing;
			}
		}
		
		public ITab_JunctionBox()
		{
			this.size = WinSize;
			this.labelKey = "TabJunctionBox";
		}
		protected override void FillTab()
		{
			//Rect rect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
			// fill in the tab when it opens...
		}
		//public override void TabUpdate()
		//{
			//???
		//}
		
		
	}
}