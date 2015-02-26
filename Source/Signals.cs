/*
 * Created by SharpDevelop.
 * User: Thomas
 * Date: 2015-02-25
 * Time: 21:03
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

using UnityEngine;
using Verse;
//using Verse.AI;
//using Verse.Sound;
//using Verse.Noise;
using RimWorld;
//using RimWorld.Planet;
//using RimWorld.SquadAI;

namespace Signals
{
	public class Building_SignalConduit : Building
	{
		
        CompSignal compSignal;
        	
		/// <summary>
        /// Do something after the object is spawned
        /// </summary>
        public override void SpawnSetup()
        {
            base.SpawnSetup();
            
            compSignal = GetComp<CompSignal>();
        }
        
	}
	
	public class Building_PowerRelay : RimWorld.Building_PowerSwitch
	{
		
        CompSignal compSignal;
        	
		/// <summary>
        /// Do something after the object is spawned
        /// </summary>
        public override void SpawnSetup()
        {
            base.SpawnSetup();
            
            compSignal = GetComp<CompSignal>();
        }
        public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (var gizmo in base.GetGizmos()) {
				
				//TODO: clean this up to be more a more specific filter for the "Toggle Power" command...
				if(!(gizmo is Command_Toggle)) yield return gizmo;
			}
		}
		
		public override void Tick()
		{
			base.Tick();
			
			if(compSignal.connectedNet == null)
			{
				SwitchOn = false;
			}
			else 
			{
				SwitchOn = compSignal.connectedNet.CurrentSignal();
			}
		}
	}
	
	public class Building_PowerSensor: Building
	{
	  	CompSignalSource compSignal;
	  	CompPower compPower;
        	
        public override void SpawnSetup()
        {
            base.SpawnSetup();
            
            compSignal = GetComp<CompSignalSource>();
            compPower = GetComp<CompPower>();
        }
        public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (var gizmo in base.GetGizmos()) {
				
				//TODO: clean this up to be more a more specific filter for the "Toggle Power" command...
				if(!(gizmo is Command_Toggle)) yield return gizmo;
			}
		}
		
		public override void Tick()
		{
			base.Tick();
			
			compSignal.OutputSignal = compPower.PowerNet.CurrentStoredEnergy() > 100;
		}
	}
	
	public class CompSignal : ThingComp
	{
		public SignalNet connectedNet;
		
		public IntVec3 Position { get; private set; }
		
		public override string CompInspectStringExtra()
		{
			if(this.connectedNet == null) return "No Signal Net";
			return "Signal: " + this.connectedNet.CurrentSignal() +
				"Signal Net: " + this.connectedNet;
		}
		
		public override void PostSpawnSetup()
		{
			base.PostSpawnSetup();
			
			TryConnectTo(IntRot.north);
			TryConnectTo(IntRot.east);
			TryConnectTo(IntRot.south);
			TryConnectTo(IntRot.west);
			
			if(connectedNet == null) 
			{
				connectedNet = new SignalNet(new List<CompSignal>{this});
				Log.Message(string.Format("Node {0} found no adjacent net, created net {1}",this.parent,connectedNet.NetID));
				
			}
			
			this.Position = this.parent.Position;
			
			SignalGrid.Register(this);
			
		}
		
		public override void PostDeSpawn()
		{
			base.PostDeSpawn();
			
			SignalGrid.Deregister(this);
			
			connectedNet.SplitNetAt(this);
		}
		
		public bool CanConnectTo(IntRot side)
		{
			//TODO: make variations that allow one-side or two-side (maybe 3-side too?) connections
			//TODO: account for rotated parent Thing also
			return true;
			
		}
		
		public CompSignal AdjacentNode(IntRot side)
		{
			if(!CanConnectTo(side)) return null;
			
			return SignalGrid.SignalNodeAt(this.parent.Position + side.FacingSquare,new IntRot((side.AsInt+2)%4));
		}
		
		internal void ConnectToNet(SignalNet net)
		{
			if(this.connectedNet == null)
			{
				net.RegisterNode(this);
			} else {
				Log.Message(string.Format("Merging Net {0} into Net {1}.",connectedNet.NetID,net.NetID));
				connectedNet.MergeIntoNet(net);
			}
		}
		
		public void TryConnectTo(IntRot rot)
		{
			Log.Message(string.Format("{0} trying to connect on {1}",this.parent,rot));
			var otherNode = AdjacentNode(rot);
			
			if(otherNode!=null)
			{
				if(otherNode.connectedNet != null)
				{
					this.ConnectToNet(otherNode.connectedNet);
					Log.Message(string.Format("Connected to net {0}",this.connectedNet.NetID));
				}
				else 	
				{
					Log.Warning(string.Format("Adjacent node {0} has null net!",otherNode.parent));
				}
			}
		}
		
	}
	
	public class CompSignalSource : CompSignal
	{
		public bool OutputSignal;
	}
}