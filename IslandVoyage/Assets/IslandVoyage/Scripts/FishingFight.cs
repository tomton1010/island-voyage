using UnityEngine;

namespace IslandVoyage
{
    public enum FightOutcome { Playing, Landable, LineBroken, HookLost }

    // The same finite, time-based fight model drives gameplay and the rule checks.
    public sealed class FishingFight
    {
        public float Tension {get;private set;}=.42f;
        public float Stamina {get;private set;}=1;
        public float LineOut {get;private set;}
        public float Pull {get;private set;}
        public bool Surging {get;private set;}
        public FightOutcome Outcome {get;private set;}
        public float Elapsed {get;private set;}
        public float Overload {get;private set;}
        public float Slack {get;private set;}
        readonly float difficulty,period,phase;
        public FishingFight(int species,float distance,float variation)
        {
            difficulty=new[]{.18f,.35f,.28f,.24f,.12f,.32f,.75f,.52f}[species];
            LineOut=Mathf.Clamp(distance,4,14);period=5.2f-difficulty;phase=variation;
        }
        public void Advance(float dt,bool reel,float waveScale)
        {
            if(Outcome!=FightOutcome.Playing || dt<=0)return;
            // Substeps keep hold/release outcomes consistent at low frame rates.
            while(dt>0){float step=Mathf.Min(dt,.025f);Step(step,reel,waveScale);dt-=step;if(Outcome!=FightOutcome.Playing)break;}
        }
        void Step(float dt,bool reel,float waveScale)
        {
            Elapsed+=dt;
            float cycle=Mathf.Repeat(Elapsed+phase,period);
            Surging=cycle<1.65f;
            Pull=(.20f+difficulty*.20f)+(Surging?Mathf.Sin(cycle/1.65f*Mathf.PI)*.72f:0);
            float roughness=Mathf.Max(0,waveScale-1)*.016f*Mathf.Sin(Elapsed*2.4f);
            Tension=Mathf.Clamp01(Tension+dt*(reel?.15f+Pull*.12f+difficulty*.035f:-.24f+Pull*.045f)+dt*roughness);
            Overload=Tension>.9f?Overload+dt:Mathf.Max(0,Overload-dt*2);
            Slack=Tension<.12f?Slack+dt:Mathf.Max(0,Slack-dt*2);
            if(Overload>=.65f){Outcome=FightOutcome.LineBroken;return;}
            if(Slack>=2.1f || Elapsed>=75){Outcome=FightOutcome.HookLost;return;}
            float grip=Mathf.InverseLerp(.12f,.28f,Tension)*(1-Mathf.InverseLerp(.78f,.95f,Tension));
            if(reel)
            {
                LineOut=Mathf.Max(1.9f,LineOut-dt*1.55f*(1-Pull*.35f)*grip);
                Stamina=Mathf.Max(0,Stamina-dt*(.16f-difficulty*.035f)*grip);
            }
            else
            {
                LineOut=Mathf.Min(19,LineOut+dt*(Surging?.24f*Pull:0));
                Stamina=Mathf.Min(1,Stamina+dt*.007f);
            }
            if(LineOut<=2.4f && Stamina<=.12f)Outcome=FightOutcome.Landable;
        }
    }
}
