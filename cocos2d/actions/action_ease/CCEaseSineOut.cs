
using System;
using Microsoft.Xna.Framework;

namespace cocos2d
{
    public class CCEaseSineOut : CCActionEase
    {
        public override void Update(float time)
        {
            m_pOther.Update((float)Math.Sin(time * MathHelper.PiOver2));
        }

        public override CCFiniteTimeAction Reverse()
        {
            return CCEaseSineIn.Create((CCActionInterval) m_pOther.Reverse());
        }

        public override object Copy(ICopyable pZone)
        {
            CCEaseSineOut pCopy;

            if (pZone != null)
            {
                //in case of being called at sub class
                pCopy = pZone as CCEaseSineOut;
            }
            else
            {
                pCopy = new CCEaseSineOut();
            }

            pCopy.InitWithAction((CCActionInterval) (m_pOther.Copy()));

            return pCopy;
        }

        public new static CCEaseSineOut Create(CCActionInterval pAction)
        {
            var pRet = new CCEaseSineOut();
            pRet.InitWithAction(pAction);
            return pRet;
        }
    }
}