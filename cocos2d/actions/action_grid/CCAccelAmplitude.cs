
using System;

namespace cocos2d
{
    public class CCAccelAmplitude : CCActionInterval
    {
        protected float m_fRate;
        protected CCActionInterval m_pOther;

        public float Rate
        {
            get { return m_fRate; }
            set { m_fRate = value; }
        }

        public bool InitWithAction(CCAction pAction, float duration)
        {
            if (base.InitWithDuration(duration))
            {
                m_fRate = 1.0f;
                m_pOther = pAction as CCActionInterval;

                return true;
            }

            return false;
        }

        public override void StartWithTarget(CCNode target)
        {
            base.StartWithTarget(target);
            m_pOther.StartWithTarget(target);
        }

        public override void Update(float time)
        {
            ((m_pOther)).AmplitudeRate = (float) Math.Pow(time, m_fRate);
            m_pOther.Update(time);
        }

        public override CCFiniteTimeAction Reverse()
        {
            return Create(m_pOther.Reverse(), m_fDuration);
        }

        public static CCAccelAmplitude Create(CCAction pAction, float duration)
        {
            var pRet = new CCAccelAmplitude();
            pRet.InitWithAction(pAction, duration);
            return pRet;
        }
    }
}