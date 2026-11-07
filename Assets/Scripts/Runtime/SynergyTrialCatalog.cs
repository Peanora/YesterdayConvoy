using System;
using System.Collections.Generic;
using System.Linq;

namespace YesterdayConvoy
{
    public static class SynergyTrialCatalog
    {
        public sealed class Entry
        {
            public readonly string Id,Title,SceneName,Tag;
            public readonly int MaximumTier;
            readonly string[] members;
            public IReadOnlyList<string> Members=>Array.AsReadOnly(members);
            public string ScenePath=>"Assets/Scenes/SynergyTrials/"+SceneName+".unity";
            public string DefinitionPath=>"Assets/Data/SynergyTrials/"+Id+".asset";
            public string EncounterPath=>"Assets/Data/SynergyTrials/Encounters/"+Id+"-defenders.asset";
            internal Entry(string id,string title,string sceneName,string tag,int maximum,params string[] roster)
            {Id=id;Title=title;SceneName=sceneName;Tag=tag;MaximumTier=maximum;members=roster;}
        }
        static readonly Entry[] entries={
            new Entry("reunion","整合运动","ReunionTrial","整合运动",8,"CARD_M07","CARD_M08","CARD_M09","CARD_M10","CARD_M15","CARD_M16","CARD_M11","CARD_M21"),
            new Entry("snow","雪怪小队","SnowTrial","雪怪小队",5,"CARD_M06","CARD_M11","CARD_M12","CARD_M13","CARD_M14"),
            new Entry("infection","感染生物","InfectionTrial","感染生物",6,"CARD_ORIGINIUM_SLUG","CARD_M02","CARD_M03","CARD_M04","CARD_M05","CARD_M06"),
            new Entry("guerrilla","游击队","GuerrillaTrial","游击队",6,"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19"),
            new Entry("mudrock","泥岩小队","MudrockCrewTrial","泥岩小队",6,"CARD_M26","CARD_M27","CARD_M42","CARD_M43","CARD_M28"),
            new Entry("herd","牧群","HerdTrial","牧群",4,"CARD_M21","CARD_M22","CARD_M23","CARD_M24","CARD_M25"),
            new Entry("deep-pool","深池","DeepPoolTrial","深池",4,"CARD_M29","CARD_M30","CARD_M31","CARD_M32"),
            new Entry("marine","海嗣","MarineTrial","海嗣",4,"CARD_M33","CARD_M34","CARD_M35","CARD_M36"),
            new Entry("sarkaz","萨卡兹","SarkazTrial","萨卡兹",9,"CARD_M19","CARD_M24","CARD_M26","CARD_M27","CARD_M37","CARD_M38"),
        };
        public static IReadOnlyList<Entry> Entries=>Array.AsReadOnly(entries);
        public static Entry Find(string id)=>entries.FirstOrDefault(e=>string.Equals(e.Id,id,StringComparison.Ordinal));
    }
}
