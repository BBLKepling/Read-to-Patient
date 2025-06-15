using RimWorld;
using Verse;

namespace Read_to_Patients
{
    [DefOf]
    public class ReadToPatientsDefOf
    {
        static ReadToPatientsDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ReadToPatientsDefOf));
        }
        public static InteractionDef BBLK_PatientStory;
        public static JobDef BBLK_Job_PatientListen;
        public static JobDef BBLK_Job_PatientStory;
        public static TaleDef BBLK_PatientStory_Tale;
    }
}
