/*
 * Created by SharpDevelop.
 * User: Thomas
 * Date: 2015-02-28
 * Time: 18:49
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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
		
		public List<CompSignal> nodes = new List<CompSignal>();
		public List<CompSignalSource> sources = new List<CompSignalSource>();
		public int NetID { get; private set; }
		
		
		public SignalNet()
		{
			NetID = lastID++;
		}
		
		public SignalNet(IEnumerable<CompSignal> newNodes) : this()
		{
			foreach (var node in newNodes) {
				RegisterNode(node);
			}
			
		}
		
		public SignalNet(CompSignal newNode) : this(new List<CompSignal>{newNode})
		{

		}

		public void RegisterNode(CompSignal node)
		{
			
			if(node.connectedNet==this)
			{
				Log.Warning("Tried to register " + node + " on net it's already on!");
				return;
			}
			
			if(node.connectedNet!=null)
			{
				//Log.Warning("Tried to register node that's already on another net! Merging nets instead...");
				//node.connectedNet.MergeIntoNet(this);
				
				Log.Warning(string.Format(
					"Tried to register {0} onto net {1}, which is already on net {2}! Transferring node instead...",
					node,this.NetID,node.connectedNet.NetID));
				node.connectedNet.DeregisterNode(node);
				
				return;
			}
				
			
			// register the new node
			this.nodes.Add(node);
			
			// inform the node of it's new Net
			node.connectedNet = this;
			
			// If node is a Source, add it there too..
			var source = node as CompSignalSource;
			if(source != null)
			{
				sources.Add(source);
			}
		}		
		public void DeregisterNode(CompSignal node)
		{
			// register the new node
			this.nodes.Remove(node);
			
			// inform the node of it's new Net
			node.connectedNet = null;
			
			// If node is a Source, add it there too..
			var source = node as CompSignalSource;
			if(source != null) sources.Remove(source);
		}
		
		
		public bool CurrentSignal()
		{
			bool sig = false;
			
			foreach (var source in sources) {
				sig |= source.OutputSignal;
			}
			
			return sig;
		}
		
		public override string ToString()
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("SIGNALNET: " + this.NetID);
			stringBuilder.AppendLine("  Nodes: ");
			foreach (var node in this.nodes)
			{
				stringBuilder.AppendLine("      " + node.parent);
			}
			stringBuilder.AppendLine("  Sources: ");
			foreach (var source in this.sources)
			{
				stringBuilder.AppendLine("      " + source.parent);
			}
			return stringBuilder.ToString();
		}
		
		public void MergeIntoNet(SignalNet newNet)
		{
			
			foreach (var node in new List<CompSignal>(nodes))
			{
				this.DeregisterNode(node);
				newNet.RegisterNode(node);
			}
		}
		
		public List<SignalNet> SplitNetAt(CompSignal node)
		{
			
			Log.Message(string.Format("Splitting net {0} at {1}.",this.NetID,node.parent));
			
			var spawnedNets = new List<SignalNet>();
			
			this.DeregisterNode(node);
			
			if(this.nodes.Count == 0) return spawnedNets;
			
			for (int r = 0; r < 4; r++) {
				var adjacentNode = node.AdjacentNode(new IntRot(r));
				if(adjacentNode!=null && adjacentNode.connectedNet == this)
				{
					var newNet = SignalNet.FromContiguousNodes(adjacentNode);
					Log.Message(string.Format("Created new net {0} on {1}",newNet.NetID,new IntRot(r)));
					spawnedNets.Add(newNet);
				}
			}
			
			return spawnedNets;
			
		}
		 
		public static SignalNet FromContiguousNodes(CompSignal root)
		{
			
			Log.Message(string.Format("Searching for contiguous nodes from {0}...",root.parent));
			
			var rootNet = root.connectedNet;
			
			rootNet.DeregisterNode(root);
			
			var newNet = new SignalNet(root);
			
			for (int r = 0; r < 4; r++) {
				
				var adjacentNode = root.AdjacentNode(new IntRot(r));
				if(adjacentNode!=null && adjacentNode.connectedNet == rootNet)
				{
					Log.Message(string.Format("Found {0} on {1}...",adjacentNode.parent,new IntRot(r)));
					SignalNet.FromContiguousNodes(adjacentNode).MergeIntoNet(newNet);
				}
			}
			
			Log.Message(string.Format("Created net {0} with {1} nodes.",newNet.NetID,newNet.nodes.Count));
			
			return newNet;
			
		}

	}
}
