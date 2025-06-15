using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Read_to_Patients
{
    public class JobDriver_PatientStory : JobDriver
    {
        private bool hasInInventory;

        private bool carrying;

        private bool joyDesire;

        private const TargetIndex PatientInd = TargetIndex.B;

        private const TargetIndex BookInd = TargetIndex.C;

        private Thing Bed => job.GetTarget(TargetIndex.A).Thing;

        private Pawn Patient => (Pawn)job.GetTarget(TargetIndex.B).Thing;

        private Book Book => (Book)job.GetTarget(TargetIndex.C).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(PatientInd), job, 1, -1, null, errorOnFailed) && pawn.Reserve(job.GetTarget(BookInd), job, 1, 1, null, errorOnFailed);
        }
        public override void Notify_Starting()
        {
            base.Notify_Starting();
            job.count = 1;
            hasInInventory = pawn.inventory != null && pawn.inventory.Contains(Book);
            carrying = pawn?.carryTracker.CarriedThing == Book;
            joyDesire = pawn.needs?.joy != null;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref hasInInventory, "hasInInventory", defaultValue: false);
            Scribe_Values.Look(ref carrying, "carrying", defaultValue: false);
            Scribe_Values.Look(ref joyDesire, "joyDesire", defaultValue: false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            SetFinalizerJob(delegate (JobCondition condition)
            {
                if (!pawn.IsCarryingThing(Book))
                {
                    return null;
                }
                if (condition != JobCondition.Succeeded)
                {
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out var _);
                    return null;
                }
                TaleRecorder.RecordTale(ReadToPatientsDefOf.BBLK_PatientStory_Tale, pawn, Patient, Book);
                return HaulAIUtility.HaulToStorageJob(pawn, Book, false);
            });
            AddFailCondition(() => !Patient.health.capacities.CapableOf(PawnCapacityDefOf.Hearing));
            AddFailCondition(() => !Patient.InBed());
            AddFailCondition(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
            AddFailCondition(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight));
            AddFailCondition(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking));
            Toil failIfNoBook = FailIfNoBook();
            yield return Toils_Jump.JumpIf(failIfNoBook, () => !Book.Position.IsValid || pawn.IsCarryingThing(Book));
            if (!carrying)
            {
                if (hasInInventory)
                {
                    yield return Toils_Misc.TakeItemFromInventoryToCarrier(pawn, TargetIndex.C);
                }
                else
                {
                    yield return Toils_Goto.GotoCell(Book.PositionHeld, PathEndMode.ClosestTouch).FailOnDestroyedOrNull(TargetIndex.C).FailOnSomeonePhysicallyInteracting(TargetIndex.C);
                    yield return Toils_Haul.StartCarryThing(TargetIndex.C, canTakeFromInventory: true);
                }
            }
            yield return failIfNoBook;
            yield return Toils_Goto.GotoCell(Patient.PositionHeld, PathEndMode.ClosestTouch).FailOnDestroyedOrNull(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return GoToChair();
            yield return ReadToil();
        }
        protected Toil FailIfNoBook()
        {
            Toil toil = ToilMaker.MakeToil("FailIfNoBook");
            toil.FailOn(() => !pawn.IsCarryingThing(Book));
            return toil;
        }
        protected Toil ReadToil()
        {
            Toil toil = ToilMaker.MakeToil("Read");
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = job.def.joyDuration;
            toil.handlingFacing = true;
            toil.socialMode = RandomSocialMode.Off;
            toil.initAction = delegate
            {
                Book.IsOpen = true;
                pawn.pather.StopDead();
                job.showCarryingInspectLine = false;
                pawn.rotationTracker.FaceCell(job.GetTarget(TargetIndex.B).Cell);
                Patient.jobs.StopAll();
                Job newjob = JobMaker.MakeJob(ReadToPatientsDefOf.BBLK_Job_PatientListen, Bed, pawn);
                Patient.jobs.StartJob(newjob);
            };
            toil.tickIntervalAction = delegate (int delta)
            {
                if (Patient.CurJobDef != ReadToPatientsDefOf.BBLK_Job_PatientListen)
                {
                    pawn.jobs.curDriver.EndJobWith(JobCondition.InterruptForced);
                }
                bool hashTick = pawn.IsHashIntervalTick(600);
                if (hashTick && pawn.RaceProps.Humanlike)
                {
                    pawn.interactions.TryInteractWith(Patient, ReadToPatientsDefOf.BBLK_PatientStory);
                }
                pawn.GainComfortFromCellIfPossible(delta);
                if (joyDesire && pawn.CurJob != null)
                {
                    JoyUtility.JoyTickCheckEnd(pawn, delta, JoyTickFullJoyAction.None, Book.JoyFactor * BookUtility.GetReadingBonus(pawn));
                }
                if (JoyUtility.JoyTickCheckEnd(Patient, delta, JoyTickFullJoyAction.EndJob, Book.JoyFactor * BookUtility.GetReadingBonus(Patient)) || ticksLeftThisToil <= 0)
                {
                    pawn.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
                }
                if (hashTick)
                {
                    pawn.jobs.CheckForJobOverride(9.1f);
                }
            };
            toil.AddEndCondition(() => BookUtility.CanReadBook(Book, pawn, out var _) ? JobCondition.Ongoing : JobCondition.InterruptForced);
            toil.AddFinishAction(delegate
            {
                Book.IsOpen = false;
                job.showCarryingInspectLine = true;
                if (Patient.CurJobDef == ReadToPatientsDefOf.BBLK_Job_PatientListen)
                {
                    Patient.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
                if (Patient.health.State == PawnHealthState.Down)
                {
                    Patient.jobs.StopAll();
                    Patient.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, Bed));
                }
            });
            return toil;
        }
        protected Toil GoToChair()
        {
            Toil toil = ToilMaker.MakeToil("GoToChair");
            toil.initAction = delegate
            {
                IntVec3 readingSpot = pawn.Position;
                Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell, TraverseParms.For(pawn), 3f, (Thing t) => IsValidReadingChair(t) && (int)t.Position.GetDangerFor(pawn, t.Map) <= (int)readingSpot.GetDangerFor(pawn, pawn.Map));
                if (thing != null)
                {
                    Toils_Ingest.TryFindFreeSittingSpotOnThing(thing, pawn, out readingSpot);
                    pawn.ReserveSittableOrSpot(readingSpot, toil.actor.CurJob);
                    pawn.Map.pawnDestinationReservationManager.Reserve(pawn, pawn.CurJob, readingSpot);
                }
                pawn.pather.StartPath(readingSpot, PathEndMode.OnCell);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
        }
        public bool IsValidReadingChair(Thing t)
        {
            if (t.def.building == null ||
                t.GetRoom() != Patient.GetRoom() ||
                !t.def.building.isSittable ||
                !Toils_Ingest.TryFindFreeSittingSpotOnThing(t, pawn, out var _) ||
                t.IsForbidden(pawn) ||
                !pawn.CanReserve(t) ||
                !t.IsSociallyProper(pawn) ||
                t.IsBurning() ||
                t.HostileTo(pawn)
                ) return false;
            return true;
        }
    }
}
