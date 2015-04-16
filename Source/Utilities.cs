using System.Collections.Generic;
using System.Reflection;
using Verse;


namespace Signals
{
	static class Utilities
	{
		public static List<T> GetComps<T>(this ThingWithComps t) where T:class
		{
			return t.AllComps.ConvertAll(c=>c as T).FindAll(c=>c!=null);
		}
		
		public static Rot4 Rotate(this Rot4 I, Rot4 R)
		{
			return I.Rotate(R.AsInt);
		}
		public static Rot4 Rotate(this Rot4 I, int r)
		{
			return new Rot4((I.AsInt+r)%4);
		}
		
		public static CompSignal GetSignal(this ThingWithComps t, Rot4 side)
		{
			return t.GetComps<CompSignal>().Find(csb=>csb.CanConnectTo(side));
		}
		
		public static CompSignal GetSignal(this ThingWithComps t, string label)
		{
			return t.GetComps<CompSignal>().Find(cs=> cs.Label == label);
		}
	}
}