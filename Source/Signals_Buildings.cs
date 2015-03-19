using System;
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
	
	public class Command_ToggleInvert : Command_Toggle
	{
		public Command_ToggleInvert(Building_LogicBuffer l)
		{
			this.isActive = ()=>l.InvertOutput;
			this.toggleAction = ()=>{
				l.InvertOutput ^= true;
			};
		}
		
		public Command_ToggleInvert(Building_LogicGate l)
		{
			this.isActive = ()=>l.InvertOutput;
			this.toggleAction = ()=>{
				l.InvertOutput ^= true;
			};
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
	
	public class Building_LogicBuffer: Building
	{
		CompSignalSource sigOutput;
		CompSignalOther sigInput;
		
		public bool InvertOutput { get; set; }
		
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
}