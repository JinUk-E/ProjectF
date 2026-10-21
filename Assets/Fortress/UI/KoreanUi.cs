namespace Fortress
{
    /// <summary>Presentation-only Korean names; asset identifiers and combat data stay unchanged.</summary>
    public static class KoreanUi
    {
        public static string RoleName(TankLoadout role)
        {
            if(role==null) return "전차 없음";
            switch(role.displayName)
            {
                case "MAIN BATTLE": return "주력 전차";
                case "SIEGE": return "공성 전차";
                case "EXCAVATOR": return "굴착 전차";
                case "SUPPORT": return "지원 자주포";
                case "HEAVY": return "중장갑 전차";
                default: return role.displayName;
            }
        }
        public static string RoleDescription(TankLoadout role)
        {
            if(role==null) return "";
            switch(role.displayName)
            {
                case "MAIN BATTLE": return "고폭탄으로 전선을 압박하는\n균형 잡힌 주력 전차";
                case "SIEGE": return "파쇄탄으로 건물과 엄폐물을\n무너뜨리는 공성 전차";
                case "EXCAVATOR": return "굴착탄으로 지형을 파괴해\n적의 이동을 제한하는 전차";
                case "SUPPORT": return "클러스터탄을 넓게 흩뿌려\n적을 견제하는 지원 자주포";
                case "HEAVY": return "주력 전차와 성능은 동일한\n중장갑 외형 파생형";
                default: return role.appearance!=null?role.appearance.description:"";
            }
        }
        public static string AmmoName(AmmoDefinition ammo)
        {
            if(ammo==null) return "탄약 없음";
            switch(ammo.purpose)
            {
                case AmmoPurpose.HighExplosive: return "고폭탄";
                case AmmoPurpose.Demolition: return "파쇄탄";
                case AmmoPurpose.Excavation: return "굴착탄";
                case AmmoPurpose.Cluster: return "클러스터탄";
                default: return ammo.displayName;
            }
        }
        public static string AmmoDescription(TankLoadout role)
        {
            if(role==null) return "";
            return AmmoName(role.basic)+" / 무제한"+(role.special!=null?"  +  "+AmmoName(role.special)+" / "+role.specialRounds+"발":"");
        }
    }
}
