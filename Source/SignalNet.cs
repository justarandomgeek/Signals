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
	public class SignalNet
	{
		private static int lastID;
		
		public List<CompSignal.SubNode> nodes = new List<CompSignal.SubNode>();
		public List<Tuple<CompSignalSource,int>> sources = new List<Tuple<CompSignalSource,int>>();
		public int NetID { get; private set; }
		
		
		public SignalNet()
		{
			NetID = lastID++;
		}
		
		public SignalNet(IEnumerable<CompSignal.SubNode> newNodes) : this()
		{
			foreach (var node in newNodes) {
				RegisterNode(node);
			}
			
		}
		
		public SignalNet(CompSignal.SubNode newNode) : this(new List<CompSignal.SubNode>{newNode}){}
		
		public void RegisterNode(CompSignal.SubNode node)
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
			this.nodes.Add(node[idx]);
			
			// inform the node of it's new Net
			node.ConnectedNets[idx] = this;
		
			// If node is a Source, add it there too..
			var source = node as CompSignalSource;
			if(source != null)
			{
				sources.Add(new Tuple<CompSignalSource, int>(source,idx));
			}
			
		}		
		
		public void DeregisterNode(CompSignal.SubNode node)
		{
			DeregisterNode(node.Node,node.Index);
		}
		public void DeregisterNode(CompSignal node, int idx)
		{
			// register the new node
			this.nodes.Remove(node[idx]);
			
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

		public List<SignalNet> SplitNetAt(CompSignal.SubNode node)
		{
			Log.Message(string.Format("Splitting net {0} at {1}.",this.NetID,node.Node.parent));
			
			var spawnedNets = new List<SignalNet>();
			
			this.DeregisterNode(node);
			
			if(this.nodes.Count == 0) return spawnedNets;
			
			foreach (var adjacentNode in node.AdjacentNodes()) {
				if(adjacentNode.Node!=null && adjacentNode.ConnectedNet == this)
				{
					var newNet = SignalNet.FromContiguousNodes(adjacentNode);
					spawnedNets.Add(newNet);
				}
			}
			
			return spawnedNets;
		}
		 
		public static SignalNet FromContiguousNodes(CompSignal.SubNode root)
		{
			Log.Message(string.Format("Searching for contiguous nodes from {0}...",root.Node.parent));
			
			var rootNet = root.ConnectedNet;
			
			rootNet.DeregisterNode(root);
			
			var newNet = new SignalNet(root);
			
			foreach (var adjacentNode in root.AdjacentNodes()) {
				if(adjacentNode.Node!=null && adjacentNode.ConnectedNet == rootNet)
				{
					SignalNet.FromContiguousNodes(adjacentNode).MergeIntoNet(newNet);
				}
			}
			
			Log.Message(string.Format("Created net {0} with {1} nodes.",newNet.NetID,newNet.nodes.Count));
			
			return newNet;
		}

	}
}
