/*
 * Created by SharpDevelop.
 * User: Thomas
 * Date: 2015-02-25
 * Time: 21:03
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;

using Verse;

namespace Signals
{
	public class CompProperties_Signals : CompProperties
	{
		
		public string label;
		public int signalWidth = 1;
		public List<Rot4> connectSides;
		public IntVec3 offset;
		
		public CompProperties_Signals()
		{
			this.compClass = typeof(CompSignal);
		}
	}
	
	
	
	public class CompSignal: ThingComp
	{
		
		CompProperties_Signals PropsSig
		{
			get
			{
				return (CompProperties_Signals)this.props;
			}
		}
		
		public string Label { get {return PropsSig.label;}}
		
		public int SignalWidth { get {return PropsSig.signalWidth;}}
		
		public SignalNet[] ConnectedNets { get; set; }
		
		public IntVec3 Position
		{
			get
			{
				return this.parent.Position + this.PropsSig.offset;
			}
		}
		
		public override string CompInspectStringExtra()
		{
			if(ConnectedNets == null) return "No Signal Nets";
			
			string sides = "";
			for (int i = 0; i < 4; i++) {
				if(this.CanConnectTo(new Rot4(i))) sides += "NESW"[this.parent.Rotation.Rotate(i).AsInt];
			}
			
			List<SignalNet> nets = new List<SignalNet>(ConnectedNets);
			
			return string.Join("\n", nets.ConvertAll(n=>				
					string.Format("[{2}] Signal Net: {0} ({1})",
			    		n.NetID,
			    		n.CurrentSignal(),
			    		sides
			    	)).ToArray());
		}
		
		public override void PostSpawnSetup()
		{
			base.PostSpawnSetup();
			
			ConnectedNets = new SignalNet[SignalWidth];
			
			foreach (var r in PropsSig.connectSides??new List<int>{0,1,2,3}.ConvertAll(i=>new Rot4(i)) ) {
				Log.Message(string.Format("{0} trying to connect on {1}",this.parent,r));
				
				var otherNode = AdjacentNode(r);
				
				if(otherNode!=null)
				{
					if(otherNode.ConnectedNets != null)
					{
						this.ConnectToNets(otherNode.ConnectedNets);
					}
					else
					{
						Log.Warning(string.Format("Adjacent node {0} has null netlist!",otherNode.parent));
					}
				}
			}
			
			for (int i = 0; i < SignalWidth; i++) {
				if(ConnectedNets[i]==null)
				{
					ConnectedNets[i] = new SignalNet(this,i);
					Log.Message(string.Format("Created net {0} on {1}",ConnectedNets[i].NetID,i));
				}
			}
			
			SignalGrid.Register(this);
		}
		
		public override void PostDeSpawn()
		{
			base.PostDeSpawn();
			
			SignalGrid.Deregister(this);
			
			for (int i = 0; i < SignalWidth; i++) {
				ConnectedNets[i].SplitNetAt(this,i);
			}
		}
		
		public virtual bool CanConnectTo(Rot4 side)
		{
			return this.PropsSig.connectSides == null || this.PropsSig.connectSides.Contains(side);
		}
		
		public CompSignal AdjacentNode(Rot4 side)
		{
			if(!CanConnectTo(side)) return null;
			
			Rot4 absSide = side.Rotate(parent.Rotation);
						
			return SignalGrid.SignalNodeAt(this.Position + absSide.FacingCell,absSide.Rotate(2));
		}
		
		internal void ConnectToNets(SignalNet[] nets)
		{
			if( nets.Length != SignalWidth)
				Log.Message(string.Format("ConnectToNets with {0} nets. Expecting {1}", nets.Length, SignalWidth));
			
			for (int i = 0; i < SignalWidth; i++) {
				if(this.ConnectedNets[i] == null)
				{
					nets[i].RegisterNode(this,i);
					
				} else {
					Log.Message(string.Format("Merging Net {0} into Net {1}.",ConnectedNets[i].NetID,nets[i].NetID));
					ConnectedNets[i].MergeIntoNet(nets[i]);
				}
			}
		}
	}
	
	public class CompSignalSource : CompSignal
	{
		public BitArray OutputSignal {get;set;}
		
		
		public override void PostSpawnSetup()
		{
			base.PostSpawnSetup();
			
			OutputSignal = new BitArray(SignalWidth);
		}
		
		public override string CompInspectStringExtra()
		{
			if(this.ConnectedNets[0] == null) return "No Signal Net";
			
			var output = new int[1];
			
			OutputSignal.CopyTo(output,0);
						
			return string.Format("Output: {1:X}\n{0}", base.CompInspectStringExtra(),output[0]);
		}
	}
}