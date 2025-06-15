using RimWorld;
using Verse.AI;
using Verse;
using System.Collections.Generic;

namespace Read_to_Patients
{
    public class WorkGiver_PatientStory : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
        }
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            // if (!InteractionUtility.CanInitiateInteraction(pawn)) return true;
            List<Pawn> list = pawn.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].InBed())
                {
                    return false;
                }
            }
            return true;
        }
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return t is Pawn sick && SickPawnVisitUtility.CanVisit(pawn, sick, JoyCategory.VeryLow) && ReadToPatientsUtility.TryGetNovelToRead(pawn, out _);
        }
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Pawn sick) || !ReadToPatientsUtility.TryGetNovelToRead(pawn, out Book book)) return null;
            return JobMaker.MakeJob(ReadToPatientsDefOf.BBLK_Job_PatientStory, sick.CurJob.GetTarget(TargetIndex.A), sick, book);
        }
    }
}
