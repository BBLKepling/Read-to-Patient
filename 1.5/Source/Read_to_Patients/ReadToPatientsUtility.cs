using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using System.Linq;

namespace Read_to_Patients
{
    public class ReadToPatientsUtility
    {
        private static readonly List<Thing> TmpCandidates = new List<Thing>();
        public static bool TryGetNovelToRead(Pawn pawn, out Book book)
        {
            book = null;
            TmpCandidates.Clear();
            TmpCandidates.AddRange(from thing in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Book)
                                   where IsValidBook(thing)
                                   select thing);
            TmpCandidates.AddRange(from thing in pawn.Map.listerThings.GetThingsOfType<Building_Bookcase>().SelectMany((Building_Bookcase x) => x.HeldBooks)
                                   where IsValidBook(thing)
                                   select thing);
            if (TmpCandidates.NullOrEmpty()) return false;
            book = (Book)TmpCandidates.RandomElement();
            TmpCandidates.Clear();
            return true;
            bool IsValidBook(Thing t)
            {
                return t is Book && !t.IsForbiddenHeld(pawn) && t.def == ThingDefOf.Novel && pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.None) && t.IsPoliticallyProper(pawn);
            }
        }
    }
}
