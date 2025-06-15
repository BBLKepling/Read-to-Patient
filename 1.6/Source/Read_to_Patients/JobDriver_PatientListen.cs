using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Read_to_Patients
{
    public class JobDriver_PatientListen : JobDriver_LayDownAwake
    {
        public override bool CanSleep => false;
        public override bool LookForOtherJobs => false;
        private Pawn Reader => (Pawn)job.GetTarget(TargetIndex.B).Thing;
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFailCondition(() => Reader.CurJob.def != ReadToPatientsDefOf.BBLK_Job_PatientStory);
            foreach (Toil toil in base.MakeNewToils())
            {
                if (toil.debugName != "LayDown") yield return toil;
                else
                {
                    toil.socialMode = RandomSocialMode.Off;
                    toil.handlingFacing = true;
                    toil.AddPreTickAction(delegate
                    {
                        pawn.rotationTracker.FaceCell(job.GetTarget(TargetIndex.B).Cell);
                    });
                    yield return toil;
                }
            }
        }
        public override string GetReport()
        {
            return "BBLK_MedicalBooks_Listen".Translate(Reader.Named("PAWN"));
        }
    }
}
