namespace YesterdayConvoy
{
    // Source: native-fx/runtime-additions/NATIVE-ATTACK-PROJECTILES.json.
    // All rows below have _stopWhenSourceInvalid=0 and native lifeTime=10.
    internal static class BossRosterProjectiles
    {
        internal readonly struct Source
        {internal readonly string Key;internal readonly float Speed;internal Source(string key,float speed){Key=key;Speed=speed;}internal bool Ranged=>Speed>0;}
        internal static Source For(string card,int phase,int region,string polarity,bool blocked)
        {
            switch(card)
            {
                case "CARD_B01":return new Source("projectile_enemy_cqbw",5);
                case "CARD_B05":return blocked?default:new Source("projectile_enemy_shdbjg",10);
                case "CARD_B07":return blocked?default:new Source("projectile_enemy_ncrmcr",10);
                case "CARD_B08":return blocked?default:new Source(phase>=3?"projectile_enemy_fthlgj_2":"projectile_enemy_fthlgj",10);
                case "CARD_B09":return new Source(phase>=2?"projectile_enemy_dirctr_p2":"projectile_enemy_dirctr",10);
                case "CARD_B11":return new Source(phase>=2?"projectile_enemy_ltniak_2":"projectile_enemy_ltniak_1",10);
                case "CARD_B12":return new Source(phase>=2?"projectile_enemy_skzamy_2":"projectile_enemy_skzamy_1",10);
                case "CARD_B13":return phase<2?default:new Source(phase>=3?"projectile_enemy_pope_fly":"projectile_enemy_pope_walk",10);
                case "CARD_B15":return new Source("projectile_enemy_hlystp_attack",8);
                case "CARD_B23":return blocked?default:new Source("projectile_enemy_crzjdg_a",10);
                case "CARD_B26":return new Source(polarity=="yin"?"projectile_enemy_dylbhm_yin":"projectile_enemy_dylbhm_yang",10);
                default:return default;
            }
        }
    }
}
