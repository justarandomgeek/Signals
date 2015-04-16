/*
 * Created by SharpDevelop.
 * User: Thomas
 * Date: 2015-02-28
 * Time: 18:49
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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
	public struct SignalSubNode
	{
		public readonly CompSignal Node;
		public readonly int Index;
		
		public SignalSubNode(CompSignal node, int idx)
		{
			this.Node = node;
			this.Index = idx;
		}
		
		public SignalNet ConnectedNet { get { return Node.ConnectedNets[Index]; } }
		public SignalSubNode AdjacentNode(Rot4 r)
		{
			return new SignalSubNode(Node.AdjacentNode(r),Index);
		}
	}
	
	public class SignalNet
	{
		private static int lastID;
		
		public List<SignalSubNode> nodes = new List<SignalSubNode>();
		public List<Tuple<CompSignalSource,int>> sources = new List<Tuple<CompSignalSource,int>>();
		public int NetID { get; private set; }
		
		
		public SignalNet()
		{
			NetID = lastID++;
		}
		
		public SignalNet(IEnumerable<SignalSubNode> newNodes) : this()
		{
			foreach (var node in newNodes) {
				RegisterNode(node);
			}
			
		}
		
		public SignalNet(SignalSubNode newNode) : this(new List<SignalSubNode>{newNode}){}
		public SignalNet(CompSignal newNode,int idx) : this(new SignalSubNode(newNode,idx)){}
		
		public void RegisterNode(SignalSubNode node)
		{
			RegisterNode(node.Node,node.Index);
		}
		public void RegisterNode(CompSignal node,int idx)
		{
			if(node.ConnectedNets[idx] == this)
			{
				Log.Warning("Tried to register " + node + ":" + idx + " on net it's already on!");
				return;
			}
			
			if(node.ConnectedNets[idx]!=null)
			{
				Log.Warning(string.Format(
					"Tried to register {0} onto net {1}, which is already on net {2}! Transferring node instead...",
					node,this.NetID,node.ConnectedNets[idx].NetID));
				node.ConnectedNets[idx].DeregisterNode(node,idx);
				
				return;
			}
				
			
			// register the new node
			this.nodes.Add(new SignalSubNode(node,idx));
			
			// inform the node of it's new Net
			node.ConnectedNets[idx] = this;
		
			// If node is a Source, add it there too..
			var source = node as CompSignalSource;
			if(source != null)
			{
				sources.Add(new Tuple<CompSignalSource, int>(source,idx));
			}
			
		}		
		
		public void DeregisterNode(SignalSubNode node)
		{
			DeregisterNode(node.Node,node.Index);
		}
		public void DeregisterNode(CompSignal node, int idx)
		{
			// register the new node
			this.nodes.Remove(new SignalSubNode(node,idx));
			
			// inform the node of it's new Net
			node.ConnectedNets[idx] = null;
			
			// If node is a Source, remove it there too..
			var source = node as CompSignalSource;
			if(source != null) sources.Remove(new Tuple<CompSignalSource, int>(source,idx));
		}
		
		
		public bool CurrentSignal()
		{
			bool sig = false;
			
			foreach (var source in sources) {
				sig |= source.First.OutputSignal[source.Second];
			}
			
			return sig;
		}
		
		public void MergeIntoNet(SignalNet newNet)
		{
			foreach (var node in nodes.ListFullCopy())
			{
				this.DeregisterNode(node);
				newNet.RegisterNode(node);
			}
		}

		public List<SignalNet> SplitNetAt(CompSignal node,int idx)
		{
			return SplitNetAt(new SignalSubNode(node,idx));
		}
		public List<SignalNet> SplitNetAt(SignalSubNode node)
		{
			Log.Message(string.Format("Splitting net {0} at {1}.",this.NetID,node.Node.parent));
			
			var spawnedNets = new List<SignalNet>();
			
			this.DeregisterNode(node);
			
			if(this.nodes.Count == 0) return spawnedNets;
			
			for (int r = 0; r < 4; r++) {
				var adjacentNode = node.AdjacentNode(new Rot4(r));
				if(adjacentNode.Node!=null && adjacentNode.ConnectedNet == this)
				{
					var newNet = SignalNet.FromContiguousNodes(adjacentNode);
					Log.Message(string.Format("Created new net {0} on {1}",newNet.NetID,new Rot4(r)));
					spawnedNets.Add(newNet);
				}
			}
			
			return spawnedNets;
		}
		 
		public static SignalNet FromContiguousNodes(CompSignal root, int idx)
		{
			return FromContiguousNodes(new SignalSubNode(root,idx));
		}
		public static SignalNet FromContiguousNodes(SignalSubNode root)
		{
			Log.Message(string.Format("Searching for contiguous nodes from {0}...",root.Node.parent));
			
			var rootNet = root.ConnectedNet;
			
			rootNet.DeregisterNode(root);
			
			var newNet = new SignalNet(root);
			
			for (int r = 0; r < 4; r++) {
				
				var adjacentNode = root.AdjacentNode(new Rot4(r));
				if(adjacentNode.Node!=null && adjacentNode.ConnectedNet == rootNet)
				{
					Log.Message(string.Format("Found {0} on {1}...",adjacentNode.Node.parent,new Rot4(r)));
					SignalNet.FromContiguousNodes(adjacentNode).MergeIntoNet(newNet);
				}
			}
			
			Log.Message(string.Format("Created net {0} with {1} nodes.",newNet.NetID,newNet.nodes.Count));
			
			return newNet;
		}

	}
}
