using cocos2d;

namespace tests
{
    public class Effect1 : EffectAdvanceTextLayer
    {
        public override void OnEnter()
        {
            base.OnEnter();

            CCNode target = GetChildByTag(EffectAdvanceScene.kTagBackground);

            // To reuse a grid the grid size and the grid type must be the same.
            // in this case:
            //     Lens3D is Grid3D and it's size is (15,10)
            //     Waves3D is Grid3D and it's size is (15,10)

            CCSize size = CCDirector.SharedDirector.WinSize;
            CCActionInterval lens = CCLens3D.Create(new CCPoint(size.Width / 2, size.Height / 2), 240, new CCGridSize(15, 10), 0.0f);
            CCActionInterval waves = CCWaves3D.Create(18, 15, new CCGridSize(15, 10), 10);

            CCFiniteTimeAction reuse = CCReuseGrid.Create(1);
            CCActionInterval delay = new CCDelayTime (8);

            CCActionInterval orbit = new CCOrbitCamera(5, 1, 2, 0, 180, 0, -90);
            CCFiniteTimeAction orbit_back = orbit.Reverse();

            target.RunAction(new CCRepeatForever ((CCSequence.FromActions(orbit, orbit_back))));
            target.RunAction(CCSequence.FromActions(lens, delay, reuse, waves));
        }

        public override string title()
        {
            return "Lens + Waves3d and OrbitCamera";
        }
    }
}