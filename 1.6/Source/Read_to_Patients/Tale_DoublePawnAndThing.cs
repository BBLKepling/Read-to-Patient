using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Grammar;

namespace Read_to_Patients
{
    public class Tale_DoublePawnAndThing : Tale_DoublePawn
    {
        public TaleData_Thing thingData;

        public Tale_DoublePawnAndThing()
        {
        }

        public Tale_DoublePawnAndThing(Pawn firstPawn, Pawn secondPawn, Thing item)
            : base(firstPawn, secondPawn)
        {
            thingData = TaleData_Thing.GenerateFrom(item);
        }

        public override bool Concerns(Thing th)
        {
            if (!base.Concerns(th))
            {
                return th.thingIDNumber == thingData.thingID;
            }
            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref thingData, "thingData");
        }

        protected override IEnumerable<Rule> SpecialTextGenerationRules(Dictionary<string, string> outConstants)
        {
            foreach (Rule item in base.SpecialTextGenerationRules(outConstants))
            {
                yield return item;
            }
            foreach (Rule rule in thingData.GetRules("THING"))
            {
                yield return rule;
            }
        }

        public override void GenerateTestData()
        {
            base.GenerateTestData();
            thingData = TaleData_Thing.GenerateRandom();
        }
    }
}
