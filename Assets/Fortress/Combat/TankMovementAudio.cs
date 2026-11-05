using UnityEngine;
namespace Fortress
{
    /// <summary>Only committed locomotion produces sound; spawns, falling and menu previews stay silent.</summary>
    public sealed class TankMovementAudio : MonoBehaviour
    {
        public MatchController match;
        public TankAudioSettings settings;
        public AudioSource engine,tracks;
        public float Level { get; private set; }
        float remaining,speed;
        int generation,player;
        void OnEnable()
        {
            if(match==null)return;
            generation=match.Generation;player=match.ActivePlayerIndex;
            match.TankMoved+=Moved;match.StateChanged+=StateChanged;
        }
        void OnDisable()
        {
            if(match!=null){match.TankMoved-=Moved;match.StateChanged-=StateChanged;}
            StopImmediate();
        }
        void StateChanged()
        {
            if(match.Generation!=generation || match.ActivePlayerIndex!=player || match.State!=MatchState.Aim)
                StopImmediate();
            generation=match.Generation;player=match.ActivePlayerIndex;
        }
        bool Allowed => isActiveAndEnabled && match!=null && match.isActiveAndEnabled && settings!=null &&
            match.shots!=null && !match.shots.GameplaySuspended && match.shots.InputEnabled &&
            !match.shots.IsFiring && match.State==MatchState.Aim && Time.timeScale>0 && !AudioListener.pause;
        void Moved(int index,float metersPerSecond)
        {
            if(!Allowed || metersPerSecond<=0)return;
            speed=Mathf.Clamp01(metersPerSecond/Mathf.Max(.01f,settings.referenceSpeed));
            remaining=settings.movementLease;
        }
        void LateUpdate(){TickAudio(Time.deltaTime);}
        public void TickAudio(float dt)
        {
            if(!Allowed){StopImmediate();return;}
            if(dt<=0 || float.IsNaN(dt) || float.IsInfinity(dt))return;
            bool moving=remaining>0;
            remaining=Mathf.Max(0,remaining-dt);
            Level=Mathf.MoveTowards(Level,moving?1:0,dt/Mathf.Max(.01f,moving?settings.fadeIn:settings.fadeOut));
            Apply(engine,settings.engineLoop,settings.engineVolume,Mathf.Lerp(settings.enginePitch.x,settings.enginePitch.y,speed));
            Apply(tracks,settings.trackLoop,settings.trackVolume,Mathf.Lerp(settings.trackPitch.x,settings.trackPitch.y,speed));
        }
        void Apply(AudioSource source,AudioClip clip,float volume,float pitch)
        {
            if(source==null)return;
            source.volume=Level*volume;source.pitch=pitch;
            if(Level<=0 || clip==null){source.Stop();return;}
            if(source.clip!=clip)source.clip=clip;
            if(!source.isPlaying)source.Play();
        }
        public void StopImmediate()
        {
            remaining=0;Level=0;
            if(engine!=null){engine.volume=0;engine.Stop();}
            if(tracks!=null){tracks.volume=0;tracks.Stop();}
        }
    }
}
