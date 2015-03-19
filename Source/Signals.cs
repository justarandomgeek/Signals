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
	public class CompSignalNS:CompSignal
	{
		public override bool CanConnectTo(IntRot side)
		{
			return side == IntRot.north || side == IntRot.south;
		}
	}
	public class CompSignalEW:CompSignal
	{
		public override bool CanConnectTo(IntRot side)
		{
			return side == IntRot.east || side == IntRot.west;
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