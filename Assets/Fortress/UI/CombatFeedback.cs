using TMPro;
using UnityEngine;
namespace Fortress
{
    /// <summary>Actual resolved damage, once per tank and shot family; independent of predicted damage.</summary>
    [DefaultExecutionOrder(300)]
    public sealed class CombatFeedback : MonoBehaviour
    {
        public MatchController match;
        public CombatFeedbackSettings settings;
        public Camera viewCamera;
        public RectTransform canvasRect;
        public TMP_Text[] damageLabels;
        public TMP_Text hitLabel;
        public float RemainingSeconds { get; private set; }
        public ShotResolution LastResolution { get; private set; }
        MatchController subscribedMatch;
        FlatShotController subscribedShots;
        readonly Vector3[] positions=new Vector3[2];
        readonly bool[] visible=new bool[2];
        readonly Color[] colors=new Color[2];
        int generation;
        float shakeTime;
        Vector3 shakeOffset,basePosition,shakenPosition;
        bool offsetApplied;
        bool Suspended => Time.timeScale<=0 || (match!=null && match.shots!=null && match.shots.GameplaySuspended);
        void Awake()
        {
            if(viewCamera==null)viewCamera=Camera.main;
            var reset=gameObject.AddComponent<CombatFeedbackCameraReset>();reset.owner=this;
        }
        void OnEnable()
        {
            if(match==null)return;
            subscribedMatch=match;generation=match.Generation;
            match.ShotResolved+=Show;match.StateChanged+=MatchChanged;
            subscribedShots=match.shots;
            if(subscribedShots!=null)subscribedShots.ShotCancelled+=Clear;
            Clear();
        }
        void OnDisable()
        {
            if(subscribedMatch!=null) { subscribedMatch.ShotResolved-=Show;subscribedMatch.StateChanged-=MatchChanged; }
            if(subscribedShots!=null)subscribedShots.ShotCancelled-=Clear;
            subscribedMatch=null;subscribedShots=null;Clear();
        }
        void MatchChanged()
        {
            if(match==null)return;
            if(generation!=match.Generation) { generation=match.Generation;Clear(); }
            if(match.State==MatchState.Ended)Clear();
        }
        void Show(ShotResolution result)
        {
            if(settings==null)return;
            LastResolution=result;generation=result.Generation;
            RemainingSeconds=Mathf.Max(.1f,settings.lifetimeSeconds);shakeTime=0;
            bool direct=false,damaged=false,drowned=false;
            for(int i=0;i<2;i++)
            {
                var value=result.ForTank(i);positions[i]=value.Position+Vector3.up*settings.worldHeight;
                visible[i]=value.TotalDamage>0 || value.Drowned;
                colors[i]=value.DirectHit?settings.directColor:value.ExplosionDamage>0?settings.damageColor:settings.fallColor;
                direct|=value.DirectHit;damaged|=value.TotalDamage>0;drowned|=value.Drowned;
                if(damageLabels==null || i>=damageLabels.Length || damageLabels[i]==null)continue;
                damageLabels[i].text=value.TotalDamage>0?"−"+value.TotalDamage.ToString("0.#"):"";
                if(value.FallDamage>0)damageLabels[i].text="합계 "+damageLabels[i].text+"\n<size=65%>낙하 "+value.FallDamage.ToString("0.#")+" 포함</size>";
                if(value.Drowned)damageLabels[i].text+="\n침수";
                damageLabels[i].color=colors[i];damageLabels[i].enabled=visible[i];
            }
            if(hitLabel!=null)
            {
                hitLabel.text=drowned?"침수!":direct?"직격!":damaged?"피해 확인":"";
                hitLabel.color=direct?settings.directColor:settings.damageColor;hitLabel.enabled=damaged || drowned;
            }
            if(damaged)shakeTime=Mathf.Max(0,settings.shakeSeconds);
        }
        public void Clear()
        {
            RemoveShake();RemainingSeconds=0;shakeTime=0;
            for(int i=0;i<2;i++)visible[i]=false;
            if(damageLabels!=null)foreach(var label in damageLabels)if(label!=null)label.enabled=false;
            if(hitLabel!=null)hitLabel.enabled=false;
        }
        // Remove our prior offset before the orbit/follow LateUpdate computes this frame's base pose.
        public void PrepareCameraFrame() { RemoveShake(); }
        void LateUpdate() { TickPresentation(Time.deltaTime); }
        public void TickPresentation(float deltaTime)
        {
            RemoveShake();
            if(settings==null || RemainingSeconds<=0 || Suspended || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))return;
            RemainingSeconds=Mathf.Max(0,RemainingSeconds-Mathf.Max(0,deltaTime));
            if(RemainingSeconds<=0) { Clear();return; }
            float age=Mathf.Max(.1f,settings.lifetimeSeconds)-RemainingSeconds;
            float alpha=Mathf.Clamp01(RemainingSeconds/Mathf.Min(.5f,settings.lifetimeSeconds));
            for(int i=0;i<2;i++)
            {
                if(damageLabels==null || i>=damageLabels.Length || damageLabels[i]==null)continue;
                var label=damageLabels[i];bool onScreen=false;
                if(visible[i] && viewCamera!=null && canvasRect!=null)
                {
                    Vector3 screen=viewCamera.WorldToScreenPoint(positions[i]);
                    onScreen=screen.z>0 && screen.x>=0 && screen.x<=Screen.width && screen.y>=0 && screen.y<=Screen.height;
                    if(onScreen && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,null,out var local))
                        label.rectTransform.anchoredPosition=local+Vector2.up*(settings.risePixels*age/Mathf.Max(.1f,settings.lifetimeSeconds));
                }
                label.enabled=onScreen;var color=colors[i];color.a*=alpha;label.color=color;
            }
            if(hitLabel!=null) { var color=hitLabel.color;color.a=alpha;hitLabel.color=color; }
            if(shakeTime<=0 || viewCamera==null)return;
            shakeTime=Mathf.Max(0,shakeTime-Mathf.Max(0,deltaTime));
            float amount=Mathf.Min(.3f,Mathf.Max(0,settings.shakeDistance))*shakeTime/Mathf.Max(.01f,settings.shakeSeconds);
            float phase=age*settings.shakeFrequency;
            shakeOffset=(viewCamera.transform.right*Mathf.Sin(phase)+viewCamera.transform.up*Mathf.Sin(phase*1.7f))*(amount*.5f);
            basePosition=viewCamera.transform.position;shakenPosition=basePosition+shakeOffset;
            viewCamera.transform.position=shakenPosition;offsetApplied=true;
        }
        void RemoveShake()
        {
            // Cancellation or another camera owner may already have replaced our pose.
            if(offsetApplied && viewCamera!=null && (viewCamera.transform.position-shakenPosition).sqrMagnitude<.0000001f)
                viewCamera.transform.position=basePosition;
            offsetApplied=false;shakeOffset=Vector3.zero;
        }
    }
    // Runs before input can launch a shot and snapshot the camera; the presentation applies after camera LateUpdate.
    [DefaultExecutionOrder(-1000)]
    public sealed class CombatFeedbackCameraReset : MonoBehaviour
    {
        public CombatFeedback owner;
        void Update() { if(owner!=null)owner.PrepareCameraFrame(); }
    }
}
