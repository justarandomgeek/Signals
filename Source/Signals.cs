﻿/*
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
	
	
	public class SignalBridge : List<CompSignal.SubNode>, IExposable
	{
		static int nextID;
		
		public int id = nextID++;
		private Thing parent;
		public Thing Parent{get{return parent;}}
		
		public SignalBridge(Thing parent): base()
		{
			this.parent = parent;
		}
		
		public SignalBridge(): base()
		{}
		
		public void ExposeData()
		{
			Scribe_References.LookReference(ref parent,"parent");
			
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				Scribe.EnterNode("nodes");
				if (this == null)
				{
					Scribe.WriteAttribute("IsNull", "True");
				}
				else
				{
					foreach (var node in this)
					{
						String rebuild = node.RebuildLabel();
						Scribe_Values.LookValue(ref rebuild,"li");
					}
				}
				Scribe.ExitNode();
			}
			else if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				System.Xml.XmlNode xmlNode = Scribe.curParent["nodes"];
				if (xmlNode == null)
				{
					return;
				}
				System.Xml.XmlAttribute xmlAttribute = xmlNode.Attributes["IsNull"];
				if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true")
				{
					return;
				}
				
				
				foreach (System.Xml.XmlNode subNode2 in xmlNode.ChildNodes)
				{
					string rebuild = ScribeExtractor.ValueFromNode(subNode2,"");
					
					string label = rebuild.Split(':')[0];
					int idx = int.Parse(rebuild.Split(':')[1]);
					
					this.Add(new CompSignal.SubNode(null,label,idx));
						
				}
			} else if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
			{
				foreach (var node in this.ListFullCopy()) {
					this.Remove(node);
					node.ReParent((ThingWithComps)parent);
					this.Add(node);
				}
			}
		}
		
		
	}
	public static class SignalBridgeManager
	{
		private readonly static List<SignalBridge> signalBridges = new List<SignalBridge>();
		
		public static List<SignalBridge> FindAllBridges(Predicate<SignalBridge> p)
		{
			return signalBridges.FindAll(p);
		}
		
		public static SignalBridge FindBridge(Predicate<SignalBridge> p)
		{
			return signalBridges.Find(p);
		}
		
		public static void Register(SignalBridge sb)
		{
			
			signalBridges.Add(sb);
			var basenet = sb[0].ConnectedNet;
			foreach (var node in sb) {
				if(node.ConnectedNet != basenet)
				{
					node.ConnectedNet.MergeIntoNet(basenet);
				}
			}
		}
		
		public static void Deregister(SignalBridge sb)
		{
			signalBridges.Remove(sb);
		}
		
	}
	
	public class CompSignal: ThingComp
	{
		public struct SubNode
		{
			
			private Thing nodeparent;
			private string nodelabel;
			public CompSignal Node{
				get
				{
					Log.Message(string.Format("Getting Node from {0}:{1}",nodeparent,nodelabel));
					return (nodeparent as ThingWithComps).GetSignal(nodelabel);
				}
			}
			
			private int idx;
			public int Index{get{return idx;}}
			
			public string RebuildLabel()
			{
				return nodelabel + ":" + idx;
			}
			
			public void ReParent(ThingWithComps parent)
			{
				nodeparent =  parent;
			}
			
			public SubNode(CompSignal node, int idx)
			{
				this.nodeparent = node.parent;
				this.nodelabel = node.Label;
				this.idx = idx;
			}
			
			public SubNode(ThingWithComps parent, String label, int idx)
			{
				this.nodeparent = parent;
				this.nodelabel = label;
				this.idx = idx;
			}
			
			public SignalNet ConnectedNet { get { return Node.ConnectedNets[Index]; } }
			
			public IEnumerable<SubNode> AdjacentNodes()
			{
				foreach (var r in Node.PropsSig.connectSides??new List<int>{0,1,2,3}.ConvertAll(i=>new Rot4(i)) ) {
					var node = Node.AdjacentNode(r);
					if(node != null && node.SignalWidth == Node.SignalWidth) yield return node[Index];
				}
				
				var thisNode = this;
				var bridge = SignalBridgeManager.FindBridge(sb=>sb.Contains(thisNode));
				if(bridge!=null)
				{
					foreach (var subnode in bridge) {
						yield return subnode;
					}
				}				
			}			
			
		}
		
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
		
		public SubNode this[int idx]
		{
			get
			{
				return new SubNode(this,idx);
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
			
			return string.Format("{0} Nets: {1}",
			                     sides,
			                     string.Join("/", nets.ConvertAll(n=>n.NetID.ToString()).ToArray()));
		}
		
		public override void PostSpawnSetup()
		{
			base.PostSpawnSetup();
			
			ConnectedNets = new SignalNet[SignalWidth];
			
			foreach (var r in PropsSig.connectSides??new List<int>{0,1,2,3}.ConvertAll(i=>new Rot4(i)) ) {
				
				var otherNode = AdjacentNode(r);
				
				if(otherNode!=null && otherNode.SignalWidth == SignalWidth)
				{
					ConnectToNets(otherNode.ConnectedNets);
				}
			}
			
			for (int i = 0; i < SignalWidth; i++) {
				if(ConnectedNets[i]==null)
				{
					ConnectedNets[i] = new SignalNet(this[i]);
					Log.Message(string.Format("Created net {0} on {1}",ConnectedNets[i].NetID,i));
				}
			}
			
			SignalGrid.Register(this);
		}
		
		public override void PostDeSpawn()
		{
			base.PostDeSpawn();
			
			SignalGrid.Deregister(this);
			
			var bridges = SignalBridgeManager.FindAllBridges(sb=> sb.FindAll(n=>n.Node==this).Count == 0);
			foreach(var bridge in bridges)
			{
				bridge.RemoveAll(n=>n.Node==this);
				
				if(bridge.Count <2) SignalBridgeManager.Deregister(bridge);
			}
				
			
			for (int i = 0; i < SignalWidth; i++) {
				ConnectedNets[i].SplitNetAt(this[i]);
			}
		}
		
		public virtual bool CanConnectTo(Rot4 side)
		{
			return this.PropsSig.connectSides == null || this.PropsSig.connectSides.Contains(side);
		}
		
		CompSignal AdjacentNode(Rot4 side)
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