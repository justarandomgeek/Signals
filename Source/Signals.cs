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
        
		public override void Tick()
		{
			base.Tick();
			
			if(compPower.PowerNet == null)
			{
				Log.Message(string.Format("{0} connected PowerNet is null - Probably harmless on first tick", this));
				compSignal.OutputSignal = false;
				return;
			}
			
			compSignal.OutputSignal = compPower.PowerNet.CurrentStoredEnergy() > 100;
		}
	}
	
	public class Building_PressurePlate: Building
	{
	  	CompSignalSource compSignal;
	  	
        public override void SpawnSetup()
        {
            base.SpawnSetup();
            
            compSignal = GetComp<CompSignalSource>();
        }
        
		public override void Tick()
		{
			base.Tick();
			
			compSignal.OutputSignal = Find.ThingGrid.CellContains(this.Position,EntityCategory.Item);
		}
	}
	
	public class Building_LogicBuffer: Building
	{
		CompSignalSource sigOutput;
		CompSignalOther sigInput;
		
		public bool InvertOutput { get; set; }
		
		public override void SpawnSetup()
        {
            base.SpawnSetup();
            
            sigOutput = GetComp<CompSignalSourceN>();
            sigInput = GetComp<CompSignalOther>();
        }
		
		public override void Tick()
		{
			base.Tick();
			
			sigOutput.OutputSignal = sigInput.connectedNet.CurrentSignal() ^ InvertOutput;
		}
	}
	
	
	public class Building_LogicGateAND: Building_LogicGate
	{
		protected override bool GateFunction(bool? A, bool? B, bool? C)
		{
			return (A??true) && (B??true) && (C??true);
		}
		
	}
	
	public class Building_LogicGateXOR: Building_LogicGate
	{
		protected override bool GateFunction(bool? A, bool? B, bool? C)
		{
			return (A??false) ^ (B??false) ^ (C??false);
		}
		
	}
	
	public abstract class Building_LogicGate: Building
	{
		CompSignalSource sigOutput;
		CompSignal sigInA;
		CompSignal sigInB;
		CompSignal sigInC;
		
		public bool InvertOutput { get; set; }
		
		protected abstract bool GateFunction(bool? A, bool? B, bool? C);
		
		public override void SpawnSetup()
        {
            base.SpawnSetup();
            
            sigOutput = GetComp<CompSignalSourceN>();
            sigInA = GetComp<CompSignalE>();
            sigInB = GetComp<CompSignalS>();
            sigInC = GetComp<CompSignalW>();
        }
		
		public override void Tick()
		{
			base.Tick();
			
			bool? A,B,C;
			
			A = sigInA.connectedNet.nodes.Count>1?(bool?)sigInA.connectedNet.CurrentSignal():null;
			B = sigInB.connectedNet.nodes.Count>1?(bool?)sigInB.connectedNet.CurrentSignal():null;
			C = sigInC.connectedNet.nodes.Count>1?(bool?)sigInC.connectedNet.CurrentSignal():null;
			
			sigOutput.OutputSignal = GateFunction(A,B,C) ^ InvertOutput;
		}
	}
	
	
	
	// Sided Signals for directional devices
	public class CompSignalSourceN:CompSignalSource
	{
		public override bool CanConnectTo(IntRot side)
		{
			return side == IntRot.north;
		}
	}
	public class CompSignalN:CompSignal
	{
		public override bool CanConnectTo(IntRot side)
		{
			return side == IntRot.north;
		}
	}
	public class CompSignalE:CompSignal
	{
		public override bool CanConnectTo(IntRot side)
		{
			return side == IntRot.east;
		}
	}
	public class CompSignalS:CompSignal
	{
		public override bool CanConnectTo(IntRot side)
		{
			return side == IntRot.south;
		}
	}
	public class CompSignalW:CompSignal
	{
		public override bool CanConnectTo(IntRot side)
		{
			return side == IntRot.west;
		}
	}
	
	
	// Connect to anything not claimed by a direction piece already...
	public class CompSignalOther:CompSignal
	{
		
		public override bool CanConnectTo(IntRot side)
		{
			switch (side.AsInt) {
				case 0:
					if(this.parent.GetComp<CompSignalSourceN>()!=null || 
					   this.parent.GetComp<CompSignalN>()!=null) return false;
					break;
				case 1:
					if(//this.parent.GetComp<CompSignalSourceE>()!=null || 
					   this.parent.GetComp<CompSignalE>()!=null) return false;
					break;
				case 2:
					if(//this.parent.GetComp<CompSignalSourceS>()!=null ||
					   this.parent.GetComp<CompSignalS>()!=null) return false;
					break;
				case 3:
					if(//this.parent.GetComp<CompSignalSourceW>()!=null || 
					   this.parent.GetComp<CompSignalW>()!=null) return false;
					break;
			}
			
			return true;
		}
	}
	
	public class CompSignal : ThingComp
	{
		public SignalNet connectedNet;
		
		public IntVec3 Position { get; private set; }
		
		public override string CompInspectStringExtra()
		{
			if(this.connectedNet == null) return "No Signal Net";
			return string.Format("Signal Net: {0} ({1})", this.connectedNet.NetID, this.connectedNet.CurrentSignal());
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
		
		public virtual bool CanConnectTo(IntRot side)
		{
			//TODO: make variations that allow one-side or two-side (maybe 3-side too?) connections
			//TODO: account for rotated parent Thing also
			return true;
			
		}
		
		public CompSignal AdjacentNode(IntRot side)
		{
			if(!CanConnectTo(side)) return null;
			
			IntRot absSide= new IntRot((side.AsInt + this.parent.Rotation.AsInt)%4);
			
			return SignalGrid.SignalNodeAt(this.parent.Position + absSide.FacingSquare,new IntRot((absSide.AsInt+2)%4));
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
		
		public override string CompInspectStringExtra()
		{
			if(this.connectedNet == null) return "No Signal Net";
			return string.Format("Output: {2}\nSignal Net: {0} ({1})", this.connectedNet.NetID, this.connectedNet.CurrentSignal(),OutputSignal);
		}
	}
}