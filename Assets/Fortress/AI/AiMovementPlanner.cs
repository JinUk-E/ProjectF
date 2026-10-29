using UnityEngine;
namespace Fortress
{
    public readonly struct AiMovePlan
    {
        public readonly Vector2 Direction;
        public readonly Vector3 Destination;
        public readonly float Distance, Improvement;
        public AiMovePlan(Vector2 direction,Vector3 destination,float distance,float improvement)
        { Direction=direction;Destination=destination;Distance=distance;Improvement=improvement; }
    }
    public static class AiMovementPlanner
    {
        public static AiMovePlan Plan(MatchController match,AiSettings settings)
        {
            if(match==null || settings==null || !settings.enableMovement || match.Tanks==null || match.State!=MatchState.Aim)return default;
            var tank=match.Tanks[match.ActivePlayerIndex];var origin=tank.Rig.transform.position;origin.y=tank.GroundHeight;
            float baseline=Score(match,settings,origin),best=baseline;AiMovePlan result=default;
            int count=Mathf.Clamp(settings.moveCandidates,4,32);
            float limit=Mathf.Min(match.MovementRemaining,Mathf.Clamp(settings.maxMoveDistance,0,12));
            for(int i=0;i<count;i++)
            {
                float angle=i*Mathf.PI*2/count;var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                for(int step=1;step<=2;step++)
                {
                    float distance=match.PreviewMoveActiveTank(direction,limit*step*.5f,out var end);
                    if(distance<.1f)continue;
                    float score=Score(match,settings,end)-distance*Mathf.Max(0,settings.travelPenalty);
                    if(score<=best+Mathf.Max(0,settings.moveImprovement))continue;
                    best=score;result=new AiMovePlan(direction,end,distance,score-baseline);
                }
            }
            return result;
        }
        public static float Score(MatchController match,AiSettings settings,Vector3 ground)
        {
            var tank=match.Tanks[match.ActivePlayerIndex];var enemy=match.Tanks[1-match.ActivePlayerIndex];
            float clearance=ground.y-match.terrainMap.Field.WaterHeight;
            float score=Mathf.Min(Mathf.Max(0,settings.safeWaterClearance),clearance)*Mathf.Max(0,settings.safetyPreference);
            var center=ground+Vector3.up*tank.Height*.5f;
            if(match.cover!=null && match.cover.Shields(enemy.Center,center))score+=Mathf.Max(0,settings.coverPreference);
            float distance=Vector2.Distance(new Vector2(ground.x,ground.z),new Vector2(enemy.Center.x,enemy.Center.z));
            return score-Mathf.Abs(distance-settings.preferredRange)*Mathf.Max(0,settings.rangePreference);
        }
    }
}
