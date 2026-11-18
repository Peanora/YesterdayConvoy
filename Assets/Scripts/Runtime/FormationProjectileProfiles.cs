// Generated from retained original-prefab scalar fields; no file or Unity access.
// Sources: BossRoster original attack/core/legacy ranged projectile JSON and SOURCE records.
namespace YesterdayConvoy
{
    internal static class FormationProjectileProfiles
    {
        internal readonly struct Spec
        {
            internal readonly float Speed, RaiseHeight, Delay, NoRaiseHeightThreshold;
            internal readonly string MovementClass, Source;
            internal Spec(float speed,float raise,float delay,float threshold,string movement,string source)
            {Speed=speed;RaiseHeight=raise;Delay=delay;NoRaiseHeightThreshold=threshold;MovementClass=movement;Source=source;}
        }
        internal static bool TryFor(string artId,int nativePhase,out Spec spec)
        {
            spec=default;
            if(string.IsNullOrEmpty(artId)||nativePhase<0)return false;
            if(artId.StartsWith("CARD_",System.StringComparison.Ordinal))artId=artId.Substring(5);
            if(artId=="B18")artId="M14";else if(artId=="B06")artId="M32";
            else if(artId=="WIS_APRIL_G9")artId="WISADEL_APRIL";
            switch(artId)
            {
                case "B01":
                    spec=new Spec(5f,0.3f,0f,1.3f,"ParacurveMovement","B01/Default/attack/projectile_enemy_cqbw");return true;
                case "B05":
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","B05/Default/attack/projectile_enemy_shdbjg");return true;
                case "B07":
                    if(nativePhase>=1){spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","B07/Reborn/attack/projectile_enemy_ncrmcr");return true;}
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","B07/Default/attack/projectile_enemy_ncrmcr");return true;
                case "B08":
                    if(nativePhase>=1){spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","B08/M2/attack/projectile_enemy_fthlgj_2");return true;}
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","B08/Default/attack/projectile_enemy_fthlgj");return true;
                case "B09":
                    if(nativePhase>=1){spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","B09/m1/attack/projectile_enemy_dirctr_p2");return true;}
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","B09/Default/attack/projectile_enemy_dirctr");return true;
                case "B11":
                    if(nativePhase>=1){spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","B11/s2/attack/projectile_enemy_ltniak_2");return true;}
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","B11/s1/attack/projectile_enemy_ltniak_1");return true;
                case "B12":
                    if(nativePhase>=1){spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","B12/s4/attack/projectile_enemy_skzamy_2");return true;}
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","B12/s1/attack/projectile_enemy_skzamy_1");return true;
                case "B13":
                    if(nativePhase==0)return false;
                    if(nativePhase>=2){spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","B13/m2attack/attack/projectile_enemy_pope_fly");return true;}
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","B13/m1attack/attack/projectile_enemy_pope_walk");return true;
                case "B15":
                    if(nativePhase>=1){spec=new Spec(8f,0f,0f,0f,"AdvancedMovement","B15/P2/attack/projectile_enemy_hlystp_attack");return true;}
                    spec=new Spec(8f,0f,0f,0f,"AdvancedMovement","B15/P1/attack/projectile_enemy_hlystp_attack");return true;
                case "B23":
                    if(nativePhase>=1){spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","B23/M2/attack/projectile_enemy_crzjdg_a");return true;}
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","B23/Default/attack/projectile_enemy_crzjdg_a");return true;
                case "B26":
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","B26/YinMode+YangMode/attack/equivalent-scalars");return true;
                case "M14":
                    if(nativePhase>=1){spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","M14/Reborn/attack/projectile_frstar2");return true;}
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","M14/Default/attack/projectile_frstar2");return true;
                case "M32":
                    if(nativePhase==0)return false;
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","M32/P2/attack/projectile_enemy_mandra_p2");return true;
                case "WISADEL_APRIL":
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","WISADEL_APRIL/Default/attack/projectile_chr_wisdel_default");return true;
                case "M11":
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","M11/Default/attack/projectile_crossbow");return true;
                case "M12":
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","M12/Default/attack/projectile_enemy_snow_magic_ball");return true;
                case "M16":
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","M16/Default/attack/projectile_sotisp");return true;
                case "M26":
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","M26/Default/attack/projectile_enemy_mdsnpr");return true;
                case "M42":
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","M42/Default/attack/projectile_enemy_mdsnpr");return true;
                case "M36":
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","M36/Default/attack/projectile_enemy_dsmant");return true;
                case "M38":
                    spec=new Spec(10f,0f,0f,0f,"AdvancedMovement","M38/Default/attack/projectile_enemy_magic_ball");return true;
                default:return false;
            }
        }
    }
}
