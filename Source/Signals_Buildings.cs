using System;
using System.Collections;
using System.Collections.Generic;

using Verse;
using RimWorld;

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
			
			if(compSignal.ConnectedNets[0] == null)
			{
				SwitchOn = false;
			}
			else 
			{
				SwitchOn = compSignal.ConnectedNets[0].CurrentSignal();
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
				compSignal.OutputSignal[0] = false;
				return;
			}
			
			compSignal.OutputSignal[0] = compPower.PowerNet.CurrentStoredEnergy() > 100;
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
			
			compSignal.OutputSignal[0] = Find.ThingGrid.CellContains(this.Position,EntityCategory.Item);
		}
	}
	
	public class Command_ToggleInvert : Command_Toggle
	{
		public Command_ToggleInvert(ILogicInvertable l)
		{
			this.isActive = ()=>l.InvertOutput;
			this.toggleAction = l.ToggleInvert;
		}
		
		public override string Desc {
			get {
				return "Toggle Inverting the output signal.";
			}
		}
		
		public override string Label {
			get {
				return "toggle invert";
			}
		}
		
	}
	
	
	public interface ILogicInvertable
	{
		bool InvertOutput { get; set; }
		void ToggleInvert();
	}
	
	public class Building_LogicBuffer: Building, ILogicInvertable
	{
		CompSignalSource sigOutput;
		CompSignal sigInput;
		
		public bool InvertOutput { get; set; }
		public void ToggleInvert()
		{
			InvertOutput ^= true;
		}
		
		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (var gizmo in base.GetGizmos()) {
				yield return gizmo;
			}
			
			yield return new Command_ToggleInvert(this);
			
		}
		
		public override void SpawnSetup()
        {
            base.SpawnSetup();
            
            sigOutput = GetComp<CompSignalSource>();
            sigInput = GetComp<CompSignal>();
        }
		
		public override void Tick()
		{
			base.Tick();
			
			for (int i = 0; i < sigOutput.SignalWidth; i++) {
				sigOutput.OutputSignal[i] = sigInput.ConnectedNets[i].CurrentSignal() ^ InvertOutput;
			}
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
	
	public abstract class Building_LogicGate: Building, ILogicInvertable
	{
		CompSignalSource sigOutput;
		CompSignal sigInA;
		CompSignal sigInB;
		CompSignal sigInC;
		
		public bool InvertOutput { get; set; }
		public void ToggleInvert()
		{
			InvertOutput ^= true;
		}
		
		protected abstract bool GateFunction(bool? A, bool? B, bool? C);
		
		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (var gizmo in base.GetGizmos()) {
				yield return gizmo;
			}
			
			yield return new Command_ToggleInvert(this);
			
		}
		
		public override void SpawnSetup()
        {
            base.SpawnSetup();
            
            sigOutput = this.GetSignal("OUT") as CompSignalSource;
            sigInA = this.GetSignal("A");
            sigInB = this.GetSignal("B");
            sigInC = this.GetSignal("C");
        }
		
		public override void Tick()
		{
			base.Tick();
			
			for (int i = 0; i < sigOutput.SignalWidth; i++) {
				bool? A,B,C;
				
				A = sigInA.ConnectedNets[i].nodes.Count>1?(bool?)sigInA.ConnectedNets[i].CurrentSignal():null;
				B = sigInB.ConnectedNets[i].nodes.Count>1?(bool?)sigInB.ConnectedNets[i].CurrentSignal():null;
				C = sigInC.ConnectedNets[i].nodes.Count>1?(bool?)sigInC.ConnectedNets[i].CurrentSignal():null;
				
				sigOutput.OutputSignal[i] = GateFunction(A,B,C) ^ InvertOutput;
			}
		}
	}
}