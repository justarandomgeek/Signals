using System.Collections.Generic;
using System.Reflection;
using Verse;


namespace Signals
{
	static class Utilities
	{
		// This is a an evil hack allowing me to get the full list of comps on a ThingWithComponents.
		// This will probably break when Tynan releases updates.
		// This is useful becuase it means that CompSignal can look for all (I)CompSignal derivatives that
		// exist, and not offer itself for linking on the sides taken by those other components.		
		public static List<T> GetComps<T>(this ThingWithComponents t) where T:class
		{
			return t.GetComps().ConvertAll(c=>c as T).FindAll(c=>c!=null);
		}
		
		public static List<ThingComp> GetComps(this ThingWithComponents t)
		{
			return (List<ThingComp>)typeof(ThingWithComponents).GetField("comps", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(t);
		}
		
		public static IntRot Rotate(this IntRot I, IntRot R)
		{
			return I.Rotate(R.AsInt);
		}
		public static IntRot Rotate(this IntRot I, int r)
		{
			return new IntRot((I.AsInt+r)%4);
		}
		
		public static CompSignal GetSignal(this ThingWithComponents t, IntRot side)
		{
			return t.GetComps<CompSignal>().Find(csb=>csb.CanConnectTo(side));
		}
		
		public static CompSignal GetSignal(this ThingWithComponents t, string label)
		{
			return t.GetComps<CompSignal>().Find(cs=> cs.Label == label);
		}
	}
}