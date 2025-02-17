using System.Diagnostics;

namespace cocos2d
{
    public class CCParticleBatchNode : CCNode, ICCTextureProtocol
    {
        public const int kCCParticleDefaultCapacity = 500;

        public readonly CCTextureAtlas TextureAtlas = new CCTextureAtlas();
        private CCBlendFunc m_tBlendFunc;

        #region ICCTextureProtocol Members

        public CCTexture2D Texture
        {
            get { return TextureAtlas.Texture; }
            set
            {
                TextureAtlas.Texture = value;

                // If the new texture has No premultiplied alpha, AND the blendFunc hasn't been changed, then update it
                if (value != null && ! value.HasPremultipliedAlpha &&
                    (m_tBlendFunc.Source == CCMacros.CCDefaultSourceBlending && m_tBlendFunc.Destination == CCMacros.CCDefaultDestinationBlending))
                {
                    m_tBlendFunc.Source = OGLES.GL_SRC_ALPHA;
                    m_tBlendFunc.Destination = OGLES.GL_ONE_MINUS_SRC_ALPHA;
                }
            }
        }

        public CCBlendFunc BlendFunc
        {
            get { return m_tBlendFunc; }
            set { m_tBlendFunc = value; }
        }

        #endregion

        /*
         * creation with CCTexture2D
         */

        public static CCParticleBatchNode Create(CCTexture2D tex)
        {
            return Create(tex, kCCParticleDefaultCapacity);
        }

        public static CCParticleBatchNode Create(CCTexture2D tex, int capacity /* = kCCParticleDefaultCapacity*/)
        {
            var p = new CCParticleBatchNode();
            p.InitWithTexture(tex, capacity);
            return p;
        }

        /*
         * creation with File Image
         */

        public static CCParticleBatchNode Create(string imageFile, int capacity /* = kCCParticleDefaultCapacity*/)
        {
            var p = new CCParticleBatchNode();
            p.InitWithFile(imageFile, capacity);
            return p;
        }

        /*
         * init with CCTexture2D
         */

        public bool InitWithTexture(CCTexture2D tex, int capacity)
        {
            TextureAtlas.InitWithTexture(tex, capacity);

            // no lazy alloc in this node
            m_pChildren = new RawList<CCNode>(capacity);

            m_tBlendFunc.Source = CCMacros.CCDefaultSourceBlending;
            m_tBlendFunc.Destination = CCMacros.CCDefaultDestinationBlending;

            //setShaderProgram(CCShaderCache::sharedShaderCache().programForKey(kCCShader_PositionTextureColor));

            return true;
        }

        /*
         * init with FileImage
         */

        public bool InitWithFile(string fileImage, int capacity)
        {
            CCTexture2D tex = CCTextureCache.SharedTextureCache.AddImage(fileImage);
            return InitWithTexture(tex, capacity);
        }

        // CCParticleBatchNode - composition

        // override visit.
        // Don't call visit on it's children
        public override void Visit()
        {
            // CAREFUL:
            // This visit is almost identical to CCNode#visit
            // with the exception that it doesn't call visit on it's children
            //
            // The alternative is to have a void CCSprite#visit, but
            // although this is less mantainable, is faster
            //
            if (!m_bIsVisible)
            {
                return;
            }

            //kmGLPushMatrix();
            DrawManager.PushMatrix();

            if (m_pGrid != null && m_pGrid.Active)
            {
                m_pGrid.BeforeDraw();
                TransformAncestors();
            }

            Transform();

            Draw();

            if (m_pGrid != null && m_pGrid.Active)
            {
                m_pGrid.AfterDraw(this);
            }

            //kmGLPopMatrix();
            DrawManager.PopMatrix();
        }

        // override addChild:
        public override void AddChild(CCNode child, int zOrder, int tag)
        {
            Debug.Assert(child != null, "Argument must be non-null");
            Debug.Assert(child is CCParticleSystem, "CCParticleBatchNode only supports CCQuadParticleSystems as children");
            var pChild = (CCParticleSystem) child;
            Debug.Assert(pChild.Texture.Name == TextureAtlas.Texture.Name, "CCParticleSystem is not using the same texture id");

            // If this is the 1st children, then copy blending function
            if (m_pChildren.Count == 0)
            {
                BlendFunc = pChild.BlendFunc;
            }

            Debug.Assert(m_tBlendFunc.Source == pChild.BlendFunc.Source && m_tBlendFunc.Destination == pChild.BlendFunc.Destination,
                         "Can't add a PaticleSystem that uses a differnt blending function");

            //no lazy sorting, so don't call super addChild, call helper instead
            int pos = AddChildHelper(pChild, zOrder, tag);

            //get new atlasIndex
            int atlasIndex;

            if (pos != 0)
            {
                var p = (CCParticleSystem) m_pChildren[pos - 1];
                atlasIndex = p.AtlasIndex + p.TotalParticles;
            }
            else
            {
                atlasIndex = 0;
            }

            InsertChild(pChild, atlasIndex);

            // update quad info
            pChild.BatchNode = this;
        }

        // don't use lazy sorting, reordering the particle systems quads afterwards would be too complex
        // XXX research whether lazy sorting + freeing current quads and calloc a new block with size of capacity would be faster
        // XXX or possibly using vertexZ for reordering, that would be fastest
        // this helper is almost equivalent to CCNode's addChild, but doesn't make use of the lazy sorting
        private int AddChildHelper(CCParticleSystem child, int z, int aTag)
        {
            Debug.Assert(child != null, "Argument must be non-nil");
            Debug.Assert(child.Parent == null, "child already added. It can't be added again");

            if (m_pChildren == null)
            {
                m_pChildren = new RawList<CCNode>(4);
            }

            //don't use a lazy insert
            int pos = SearchNewPositionInChildrenForZ(z);

            m_pChildren.Insert(pos, child);

            child.Tag = aTag;
            child.m_nZOrder = z;

            child.Parent = this;

            if (m_bIsRunning)
            {
                child.OnEnter();
                child.OnEnterTransitionDidFinish();
            }
            return pos;
        }

        // Reorder will be done in this function, no "lazy" reorder to particles
        public override void ReorderChild(CCNode child, int zOrder)
        {
            Debug.Assert(child != null, "Child must be non-null");
            Debug.Assert(child is CCParticleSystem,
                         "CCParticleBatchNode only supports CCQuadParticleSystems as children");
            Debug.Assert(m_pChildren.Contains(child), "Child doesn't belong to batch");

            var pChild = (CCParticleSystem) (child);

            if (zOrder == child.ZOrder)
            {
                return;
            }

            // no reordering if only 1 child
            if (m_pChildren.Count > 1)
            {
                int newIndex = 0, oldIndex = 0;

                GetCurrentIndex(ref oldIndex, ref newIndex, pChild, zOrder);

                if (oldIndex != newIndex)
                {
                    // reorder m_pChildren.array
                    m_pChildren.RemoveAt(oldIndex);
                    m_pChildren.Insert(newIndex, pChild);

                    // save old altasIndex
                    int oldAtlasIndex = pChild.AtlasIndex;

                    // update atlas index
                    UpdateAllAtlasIndexes();

                    // Find new AtlasIndex
                    int newAtlasIndex = 0;
                    for (int i = 0; i < m_pChildren.count; i++)
                    {
                        var node = (CCParticleSystem) m_pChildren.Elements[i];
                        if (node == pChild)
                        {
                            newAtlasIndex = pChild.AtlasIndex;
                            break;
                        }
                    }

                    // reorder textureAtlas quads
                    TextureAtlas.MoveQuadsFromIndex(oldAtlasIndex, pChild.TotalParticles, newAtlasIndex);

                    pChild.UpdateWithNoTime();
                }
            }

            pChild.m_nZOrder = zOrder;
        }

        private void GetCurrentIndex(ref int oldIndex, ref int newIndex, CCNode child, int z)
        {
            bool foundCurrentIdx = false;
            bool foundNewIdx = false;

            int minusOne = 0;
            int count = m_pChildren.count;

            for (int i = 0; i < count; i++)
            {
                CCNode node = m_pChildren.Elements[i];

                // new index
                if (node.m_nZOrder > z && ! foundNewIdx)
                {
                    newIndex = i;
                    foundNewIdx = true;

                    if (foundCurrentIdx && foundNewIdx)
                    {
                        break;
                    }
                }

                // current index
                if (child == node)
                {
                    oldIndex = i;
                    foundCurrentIdx = true;

                    if (! foundNewIdx)
                    {
                        minusOne = -1;
                    }

                    if (foundCurrentIdx && foundNewIdx)
                    {
                        break;
                    }
                }
            }

            if (! foundNewIdx)
            {
                newIndex = count;
            }

            newIndex += minusOne;
        }

        private int SearchNewPositionInChildrenForZ(int z)
        {
            int count = m_pChildren.count;

            for (int i = 0; i < count; i++)
            {
                CCNode child = m_pChildren.Elements[i];
                if (child.m_nZOrder > z)
                {
                    return i;
                }
            }
            return count;
        }

        // override removeChild:
        public override void RemoveChild(CCNode child, bool cleanup)
        {
            // explicit nil handling
            if (child == null)
            {
                return;
            }

            Debug.Assert(child is CCParticleSystem,
                         "CCParticleBatchNode only supports CCQuadParticleSystems as children");
            Debug.Assert(m_pChildren.Contains(child), "CCParticleBatchNode doesn't contain the sprite. Can't remove it");

            var pChild = (CCParticleSystem) child;
            base.RemoveChild(pChild, cleanup);

            // remove child helper
            TextureAtlas.RemoveQuadsAtIndex(pChild.AtlasIndex, pChild.TotalParticles);

            // after memmove of data, empty the quads at the end of array
            TextureAtlas.FillWithEmptyQuadsFromIndex(TextureAtlas.TotalQuads, pChild.TotalParticles);

            // paticle could be reused for self rendering
            pChild.BatchNode = null;

            UpdateAllAtlasIndexes();
        }

        public void RemoveChildAtIndex(int index, bool doCleanup)
        {
            RemoveChild(m_pChildren[index], doCleanup);
        }

        public override void RemoveAllChildrenWithCleanup(bool doCleanup)
        {
            for (int i = 0; i < m_pChildren.count; i++)
            {
                ((CCParticleSystem) m_pChildren.Elements[i]).BatchNode = null;
            }

            base.RemoveAllChildrenWithCleanup(doCleanup);

            TextureAtlas.RemoveAllQuads();
        }

        public override void Draw()
        {
            //CC_PROFILER_STOP("CCParticleBatchNode - draw");

            if (TextureAtlas.TotalQuads == 0)
            {
                return;
            }

            for (int i = 0; i < m_pChildren.count; i++)
            {
                //((CCParticleSystem) m_pChildren.Elements[i]).updateQuadsWithParticles();
            }


            //CC_NODE_DRAW_SETUP();

            //ccGLBlendFunc( m_tBlendFunc.src, m_tBlendFunc.dst );
            DrawManager.BlendFunc(m_tBlendFunc);

            TextureAtlas.DrawQuads();

            //CC_PROFILER_STOP("CCParticleBatchNode - draw");
        }


        private void IncreaseAtlasCapacityTo(int quantity)
        {
            CCLog.Log("cocos2d: CCParticleBatchNode: resizing TextureAtlas capacity from [{0}] to [{1}].",
                      TextureAtlas.Capacity,
                      quantity);

            if (!TextureAtlas.ResizeCapacity(quantity))
            {
                // serious problems
                CCLog.Log("cocos2d: WARNING: Not enough memory to resize the atlas");
                Debug.Assert(false, "XXX: CCParticleBatchNode #increaseAtlasCapacity SHALL handle this assert");
            }
        }

        //sets a 0'd quad into the quads array
        public void DisableParticle(int particleIndex)
        {
            CCV3F_C4B_T2F_Quad[] quads = TextureAtlas.m_pQuads.Elements;
            TextureAtlas.Dirty = true;

            quads[particleIndex].BottomRight.Vertices = CCVertex3F.Zero;
            quads[particleIndex].TopRight.Vertices = CCVertex3F.Zero;
            quads[particleIndex].TopLeft.Vertices = CCVertex3F.Zero;
            quads[particleIndex].BottomLeft.Vertices = CCVertex3F.Zero;
        }

        // CCParticleBatchNode - add / remove / reorder helper methods

        // add child helper
        private void InsertChild(CCParticleSystem pSystem, int index)
        {
            pSystem.AtlasIndex = index;

            if (TextureAtlas.TotalQuads + pSystem.TotalParticles > TextureAtlas.Capacity)
            {
                IncreaseAtlasCapacityTo(TextureAtlas.TotalQuads + pSystem.TotalParticles);

                // after a realloc empty quads of textureAtlas can be filled with gibberish (realloc doesn't perform calloc), insert empty quads to prevent it
                TextureAtlas.FillWithEmptyQuadsFromIndex(TextureAtlas.Capacity - pSystem.TotalParticles,
                                                         pSystem.TotalParticles);
            }

            // make room for quads, not necessary for last child
            if (pSystem.AtlasIndex + pSystem.TotalParticles != TextureAtlas.TotalQuads)
            {
                TextureAtlas.MoveQuadsFromIndex(index, index + pSystem.TotalParticles);
            }

            // increase totalParticles here for new particles, update method of particlesystem will fill the quads
            TextureAtlas.IncreaseTotalQuadsWith(pSystem.TotalParticles);

            UpdateAllAtlasIndexes();
        }

        //rebuild atlas indexes
        private void UpdateAllAtlasIndexes()
        {
            int index = 0;

            for (int i = 0; i < m_pChildren.count; i++)
            {
                var child = (CCParticleSystem) m_pChildren.Elements[i];
                child.AtlasIndex = index;
                index += child.TotalParticles;
            }
        }

        // CCParticleBatchNode - CocosNodeTexture protocol

        private void UpdateBlendFunc()
        {
            if (!TextureAtlas.Texture.HasPremultipliedAlpha)
            {
                m_tBlendFunc.Source = OGLES.GL_SRC_ALPHA;
                m_tBlendFunc.Destination = OGLES.GL_ONE_MINUS_SRC_ALPHA;
            }
        }
    }
}