/*
* Farseer Physics Engine based on Box2D.XNA port:
* Copyright (c) 2010 Ian Qvist
* 
* Box2D.XNA port of Box2D:
* Copyright (c) 2009 Brandon Furtwangler, Nathan Furtwangler
*
* Original source Box2D:
* Copyright (c) 2006-2009 Erin Catto http://www.gphysics.com 
* 
* This software is provided 'as-is', without any express or implied 
* warranty.  In no event will the authors be held liable for any damages 
* arising from the use of this software. 
* Permission is granted to anyone to use this software for any purpose, 
* including commercial applications, and to alter it and redistribute it 
* freely, subject to the following restrictions: 
* 1. The origin of this software must not be misrepresented; you must not 
* claim that you wrote the original software. If you use this software 
* in a product, an acknowledgment in the product documentation would be 
* appreciated but is not required. 
* 2. Altered source versions must be plainly marked as such, and must not be 
* misrepresented as being the original software. 
* 3. This notice may not be removed or altered from any source distribution. 
*/

using System;
using FarseerPhysics.Collision;
using FarseerPhysics.Common;
using FarseerPhysics.DebugViews;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Dynamics.Joints;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using cocos2d;
using Random = System.Random;

namespace FarseerPhysics.TestBed.Framework
{
    public class KeyboardManager
    {
        internal KeyboardState _newKeyboardState;
        internal KeyboardState _oldKeyboardState;

        public bool IsNewKeyPress(Keys key)
        {
            if (_newKeyboardState.IsKeyDown(key) && _oldKeyboardState.IsKeyUp(key))
            {
                return true;
            }

            return false;
        }

        public bool IsKeyDown(Keys key)
        {
            return _newKeyboardState.IsKeyDown(key);
        }

        internal bool IsKeyUp(Keys key)
        {
            return _newKeyboardState.IsKeyUp(key);
        }
    }

    public static class Rand
    {
        public static Random Random = new Random(0x2eed2eed);

        /// <summary>
        /// Random number in range [-1,1]
        /// </summary>
        /// <returns></returns>
        public static float RandomFloat()
        {
            return (float)(Random.NextDouble() * 2.0 - 1.0);
        }

        /// <summary>
        /// Random floating point number in range [lo, hi]
        /// </summary>
        /// <param name="lo">The lo.</param>
        /// <param name="hi">The hi.</param>
        /// <returns></returns>
        public static float RandomFloat(float lo, float hi)
        {
            float r = (float)Random.NextDouble();
            r = (hi - lo) * r + lo;
            return r;
        }
    }

    public class GameSettings
    {
        public bool Pause;
        public bool SingleStep;
    }

    public struct TestEntry
    {
        public Func<Test> CreateFcn;
        public string Name;
    }

    public class Test
    {
        internal DebugViewXNA DebugView;
        internal int StepCount;
        internal int TextLine;
        internal World World;
        private FixedMouseJoint _fixedMouseJoint;

        protected Test()
        {
            World = new World(new Vector2(0.0f, -10.0f));

            TextLine = 30;

            World.JointRemoved += JointRemoved;
            World.ContactManager.PreSolve += PreSolve;
            World.ContactManager.PostSolve += PostSolve;
            World.ContactManager.BeginContact += BeginContact;
            World.ContactManager.EndContact += EndContact;

            StepCount = 0;
        }

        public virtual void Initialize()
        {
            Settings.EnableDiagnostics = true;
            DebugView = new DebugViewXNA(World);
            DebugView.LoadContent(DrawManager.graphicsDevice, CCApplication.SharedApplication.Content);
        }

        protected virtual void JointRemoved(Joint joint)
        {
            if (_fixedMouseJoint == joint)
            {
                _fixedMouseJoint = null;
            }
        }

        public void DrawTitle(int x, int y, string title)
        {
            DebugView.DrawString(x, y, title);
        }

        public virtual void Update(GameSettings settings, GameTime gameTime)
        {
            // added
            float timeStep = Math.Min((float)gameTime.ElapsedGameTime.TotalMilliseconds * 0.001f, (1f / 30f));

            if (settings.Pause)
            {
                if (settings.SingleStep)
                {
                    settings.SingleStep = false;
                }
                else
                {
                    timeStep = 0.0f;
                }

                DebugView.DrawString(50, TextLine, "****PAUSED****");
                TextLine += 15;
            }

            World.Step(timeStep);

            if (timeStep > 0.0f)
            {
                ++StepCount;
            }
        }

        
        public virtual void Keyboard(KeyboardManager keyboardManager)
        {
        }
        

        public virtual void Gamepad(GamePadState state, GamePadState oldState)
        {
        }

        public virtual void Mouse(MouseState state, MouseState oldState)
        {
            var p = DrawManager.ScreenToWorld(state.X, state.Y);
            Vector2 position = new Vector2(p.X, p.Y);

            if (state.LeftButton == ButtonState.Released && oldState.LeftButton == ButtonState.Pressed)
            {
                MouseUp();
            }
            else if (state.LeftButton == ButtonState.Pressed && oldState.LeftButton == ButtonState.Released)
            {
                MouseDown(position);
            }

            MouseMove(position);
        }
        

        public void MouseDown(Vector2 p)
        {
            if (_fixedMouseJoint != null)
            {
                return;
            }

            Fixture fixture = World.TestPoint(p);

            if (fixture != null)
            {
                Body body = fixture.Body;
                _fixedMouseJoint = new FixedMouseJoint(body, p);
                _fixedMouseJoint.MaxForce = 1000.0f * body.Mass;
                World.AddJoint(_fixedMouseJoint);
                body.Awake = true;
            }
        }

        public void MouseUp()
        {
            if (_fixedMouseJoint != null)
            {
                World.RemoveJoint(_fixedMouseJoint);
                _fixedMouseJoint = null;
            }
        }

        public void MouseMove(Vector2 p)
        {
            if (_fixedMouseJoint != null)
            {
                _fixedMouseJoint.WorldAnchorB = p;
            }
        }

        // Callbacks for derived classes.
        protected virtual bool BeginContact(Contact contact)
        {
            return true;
        }

        protected virtual void EndContact(Contact contact)
        {
        }

        protected virtual void PreSolve(Contact contact, ref Manifold oldManifold)
        {
        }

        protected virtual void PostSolve(Contact contact, ContactConstraint impulse)
        {
        }
    }
}